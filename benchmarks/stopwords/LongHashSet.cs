// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System;
using System.Collections;

namespace stopwords
{
    /// <summary>
    /// Implementation notes:
    /// This uses an array-based implementation similar to Dictionary<T>, using a buckets array
    /// to map hash values to the Slots array. Items in the Slots array that hash to the same value
    /// are chained together through the "next" indices. 
    /// 
    /// The capacity is always prime; so during resizing, the capacity is chosen as the next prime
    /// greater than double the last capacity. 
    /// 
    /// The underlying data structures are lazily initialized. Because of the observation that, 
    /// in practice, hashtables tend to contain only a few elements, the initial capacity is
    /// set very small (3 elements) unless the ctor with a collection is used.
    /// 
    /// The +/- 1 modifications in methods that add, check for containment, etc allow us to 
    /// distinguish a hash code of 0 from an uninitialized bucket. This saves us from having to 
    /// reset each bucket to -1 when resizing. See Contains, for example.
    /// 
    /// </summary>
    public class LongHashSet
    {
        private const int Size = 64;

        // store lower 31 bits of hash code
        private const int Lower31BitMask = 0x7FFFFFFF;

        private int[] _buckets;
        private Slot[] _slots;
        private int _count;
        private int _lastIndex;
        private int _freeList;

        /// <summary>
        /// Implementation Notes:
        /// Since resizes are relatively expensive (require rehashing), this attempts to minimize 
        /// the need to resize by setting the initial capacity based on size of collection. 
        /// </summary>
        /// <param name="collection"></param>
        public LongHashSet(IEnumerable<long> collection)
        {
            _lastIndex = 0;
            _count = 0;
            _freeList = -1;
            _buckets = new int[Size];
            _slots = new Slot[Size];

            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            ICollection<long> coll = collection as ICollection<long>;

            if(coll.Count > Size){
                throw new ArgumentException(nameof(collection) + " can not be larger than "+ Size);
            }

            foreach (long item in collection)
            {
                AddIfNotPresent(item);
            }
        }


        /// <summary>
        /// Checks if this hashset contains the item
        /// </summary>
        /// <param name="item">item to check for containment</param>
        /// <returns>true if item contained; false if not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(long item)
        {
            int hashCode = ((int)item ^ (int)(item >> 32)) &  Lower31BitMask;
            
            // see note at "HashSet" level describing why "- 1" appears in for loop
            for (int i = _buckets[hashCode & (Size - 1)] - 1; i >= 0; i = _slots[i].next)
            {
                if (_slots[i].value == item) //_slots[i].hashCode == hashCode && 
                {
                    return true;
                }
            }

            // wasn't found
            return false;
        }

        /// <summary>
        /// Number of elements in this hashset
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        #region Helper methods

        /// <summary>
        /// Adds value to HashSet if not contained already
        /// Returns true if added and false if already present
        /// </summary>
        /// <param name="value">value to find</param>
        /// <returns></returns>
        private bool AddIfNotPresent(long value)
        {

            int hashCode = ((int)value ^ (int)(value >> 32)) & Lower31BitMask;// InternalGetHashCode(value);
            int bucket = hashCode % Size;

            for (int i = _buckets[bucket] - 1; i >= 0; i = _slots[i].next)
            {
                if (_slots[i].value == value)
                {
                    return false;
                }
            }

            int index;
            if (_freeList >= 0)
            {
                index = _freeList;
                _freeList = _slots[index].next;
            }
            else
            {
                if (_lastIndex == _slots.Length)
                {
                    //IncreaseCapacity();
                    // this will change during resize
                    bucket = hashCode % _buckets.Length;
                }
                index = _lastIndex;
                _lastIndex++;
            }
            //_slots[index].hashCode = hashCode;
            _slots[index].value = value;
            _slots[index].next = _buckets[bucket] - 1;
            _buckets[bucket] = index + 1;
            _count++;

            return true;
        }

        #endregion

        internal struct Slot
        {
            //internal int hashCode;      // Lower 31 bits of hash code, -1 if unused
            internal int next;          // Index of next entry, -1 if last
            internal long value;
        }
    }
}
