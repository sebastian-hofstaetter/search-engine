// ==++==
// 
//   
//    Copyright (c) 2006 Microsoft Corporation.  All rights reserved.
//   
//    The use and distribution terms for this software are contained in the file
//    named license.txt, which can be found in the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by the
//    terms of this license.
//   
//    You must not remove this notice, or any other, from this software.
//   
// 
// ==--==
/*============================================================
**
** Class:  Dictionary
**
** Purpose: Generic hash table implementation
**
** 
===========================================================*/

namespace SearchEngine.Indexing
{

    using System;
    using System.Collections;
    using System.Diagnostics;    
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Collections.Generic;
    using SearchEngine.Indexing;

    [Serializable()]    
    [System.Runtime.InteropServices.ComVisible(false)]
    public class FastDictionary<TValue>: IDictionary<char[],TValue>, IDictionary, ISerializable, IDeserializationCallback  {
    
        private struct Entry {
            public int hashCode;    // Lower 31 bits of hash code, -1 if unused
            public int next;        // Index of next entry, -1 if last
            public char[] key;           // Key of entry
            public TValue value;         // Value of entry
        }

        private int[] buckets;
        private Entry[] entries;
        private int count;
        private int version;
        private int freeList;
        private int freeCount;
        private CharComparer comparer;
        private KeyCollection keys;
        private ValueCollection values;
        private Object _syncRoot;
        
        private SerializationInfo m_siInfo; //A temporary variable which we need during deserialization.        

        // constants for serialization
        private const String VersionName = "Version";
        private const String HashSizeName = "HashSize";  // Must save buckets.Length
        private const String KeyValuePairsName = "KeyValuePairs";
        private const String ComparerName = "Comparer";

        public FastDictionary(): this(1000, null) {}

        public FastDictionary(int capacity): this(capacity, null) {}

        public FastDictionary(CharComparer comparer): this(0, comparer) {}

        public FastDictionary(int capacity, CharComparer comparer) {
            if (capacity < 0) throw new ArgumentOutOfRangeException("Capacity");
            if (capacity > 0) Initialize(capacity);
            if (comparer == null) comparer = new CharComparer();
            this.comparer = comparer;
        }

        public FastDictionary(IDictionary<char[],TValue> dictionary): this(dictionary, null) {}

        public FastDictionary(IDictionary<char[],TValue> dictionary, CharComparer comparer):
            this(dictionary != null? dictionary.Count: 0, comparer) {

            if( dictionary == null) {
                throw new ArgumentNullException("Dictionary");
            }

            foreach (KeyValuePair<char[],TValue> pair in dictionary) {
                Add(pair.Key, pair.Value);
            }
        }

        protected FastDictionary(SerializationInfo info, StreamingContext context) {
            //We can't do anything with the keys and values until the entire graph has been deserialized
            //and we have a resonable estimate that GetHashCode is not going to fail.  For the time being,
            //we'll just cache this.  The graph is not valid until OnDeserialization has been called.
            m_siInfo = info; 
        }
            
        public CharComparer Comparer {
            get {
                return comparer;                
            }               
        }
        
        public int Count {
            get { return count - freeCount; }
        }

        public KeyCollection Keys {
            get {
                if (keys == null) keys = new KeyCollection(this);
                return keys;
            }
        }

        ICollection<char[]> IDictionary<char[], TValue>.Keys {
            get {                
                if (keys == null) keys = new KeyCollection(this);                
                return keys;
            }
        }

        public ValueCollection Values {
            get {
                if (values == null) values = new ValueCollection(this);
                return values;
            }
        }

        ICollection<TValue> IDictionary<char[], TValue>.Values {
            get {                
                if (values == null) values = new ValueCollection(this);
                return values;
            }
        }

        public TValue this[char[] key] {
            get {
                int i = FindEntry(key);
                if (i >= 0) return entries[i].value;
                throw new KeyNotFoundException();
            }
            set {
                Insert(key, value, false);
            }
        }

        public void Add(char[] key, TValue value) {
            Insert(key, value, true);
        }

        void ICollection<KeyValuePair<char[], TValue>>.Add(KeyValuePair<char[], TValue> keyValuePair) {
            Add(keyValuePair.Key, keyValuePair.Value);
        }

        bool ICollection<KeyValuePair<char[], TValue>>.Contains(KeyValuePair<char[], TValue> keyValuePair) {
            int i = FindEntry(keyValuePair.Key);
            if( i >= 0 && EqualityComparer<TValue>.Default.Equals(entries[i].value, keyValuePair.Value)) {
                return true;
            }
            return false;
        }

        bool ICollection<KeyValuePair<char[], TValue>>.Remove(KeyValuePair<char[], TValue> keyValuePair) {
            int i = FindEntry(keyValuePair.Key);
            if( i >= 0 && EqualityComparer<TValue>.Default.Equals(entries[i].value, keyValuePair.Value)) {
                Remove(keyValuePair.Key);
                return true;
            }
            return false;
        }

        public void Clear() {
            if (count > 0) {
                for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
                Array.Clear(entries, 0, count);
                freeList = -1;
                count = 0;
                freeCount = 0;
                version++;
            }
        }

        public bool ContainsKey(char[] key) {
            return FindEntry(key) >= 0;
        }

        public bool ContainsValue(TValue value) {
            if (value == null) {
                for (int i = 0; i < count; i++) {
                    if (entries[i].hashCode >= 0 && entries[i].value == null) return true;
                }
            }
            else {
                EqualityComparer<TValue> c = EqualityComparer<TValue>.Default;
                for (int i = 0; i < count; i++) {
                    if (entries[i].hashCode >= 0 && c.Equals(entries[i].value, value)) return true;
                }
            }
            return false;
        }

        private void CopyTo(KeyValuePair<char[],TValue>[] array, int index) {
            if (array == null) {
                throw new ArgumentNullException("array");
                
            }
            
            if (index < 0 || index > array.Length ) {
                throw new ArgumentOutOfRangeException("index", "ArgumentOutOfRange_NeedNonNegNum");
            }

            if (array.Length - index < Count) {
                throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
            }

            int count = this.count;
            Entry[] entries = this.entries;
            for (int i = 0; i < count; i++) {
                if (entries[i].hashCode >= 0) {
                    array[index++] = new KeyValuePair<char[],TValue>(entries[i].key, entries[i].value);
                }
            }
        }

        public Enumerator GetEnumerator() {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }

        IEnumerator<KeyValuePair<char[], TValue>> IEnumerable<KeyValuePair<char[], TValue>>.GetEnumerator() {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }        

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)] 		
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info==null) {
                throw new ArgumentNullException("info");
            }
            info.AddValue(VersionName, version);
            info.AddValue(ComparerName, comparer, typeof(CharComparer));
            info.AddValue(HashSizeName, buckets == null ? 0 : buckets.Length); //This is the length of the bucket array.
            if( buckets != null) {
                KeyValuePair<char[], TValue>[] array = new KeyValuePair<char[], TValue>[Count];
                CopyTo(array, 0);
                info.AddValue(KeyValuePairsName, array, typeof(KeyValuePair<char[], TValue>[]));
            }
        }

        private int FindEntry(char[] key) {
            if( key == null) {
                throw new ArgumentNullException("key");
            }

            if (buckets != null) {
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
                for (int i = buckets[hashCode % buckets.Length]; i >= 0; i = entries[i].next) {

                    if (entries[i].hashCode == hashCode)
                        if (comparer.Equals(entries[i].key, key))
                        return i;

                }
            }
            return -1;
        }

        private void Initialize(int capacity) {
            int size = HashHelpers.GetPrime(capacity);
            buckets = new int[size];
            for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
            entries = new Entry[size];
            freeList = -1;
        }

        public int InitOrGetPositionFromBuffer(char[] buffer,int keyLength)
        {
            var add = true;
            var value = default(TValue);

            if (buckets == null) Initialize(1000);
            int hashCode = comparer.GetHashCode(buffer,keyLength) & 0x7FFFFFFF;
            for (int i = buckets[hashCode % buckets.Length]; i >= 0; i = entries[i].next)
            {
                if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, buffer, keyLength))
                {
                    if (add)
                    {
                        return i;
                    }
                    entries[i].value = value;
                    version++;
                    return i;
                }
            }
            int index;
            if (freeCount > 0)
            {
                index = freeList;
                freeList = entries[index].next;
                freeCount--;
            }
            else
            {
                if (count == entries.Length) Resize();
                index = count;
                count++;
            }

            var keyArray = new char[keyLength];

            Array.Copy(buffer, keyArray, keyLength);

            int bucket = hashCode % buckets.Length;
            entries[index].hashCode = hashCode;
            entries[index].next = buckets[bucket];
            entries[index].key = keyArray;
            entries[index].value = value;
            buckets[bucket] = index;
            version++;

            return index;
        }

        public int InitOrGetPosition(char[] key)
        {
            return Insert(key, default(TValue), true);
        }

        public void StoreAtPosition(int pos, TValue value)
        {
            entries[pos].value = value;
            version++;
        }

        public TValue GetAtPosition(int pos)
        {
            return entries[pos].value;
        }

        private int Insert(char[] key, TValue value, bool add) {
            if( key == null ) {
                throw new ArgumentNullException("key");
            }

            if (buckets == null) Initialize(1000);
            int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
            for (int i = buckets[hashCode % buckets.Length]; i >= 0; i = entries[i].next) {
                if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key)) {
                    if (add) {
                        return i;
                    }
                    entries[i].value = value;
                    version++;
                    return i;
                }
            }
            int index;
            if (freeCount > 0) {
                index = freeList;
                freeList = entries[index].next;
                freeCount--;
            }
            else {
                if (count == entries.Length) Resize();
                index = count;
                count++;
            }
            int bucket = hashCode % buckets.Length;
            entries[index].hashCode = hashCode;
            entries[index].next = buckets[bucket];
            entries[index].key = key;
            entries[index].value = value;
            buckets[bucket] = index;
            version++;

            return index;
        }

        public virtual void OnDeserialization(Object sender) {            
            if (m_siInfo==null) {
                // It might be necessary to call OnDeserialization from a container if the container object also implements
                // OnDeserialization. However, remoting will call OnDeserialization again.
                // We can return immediately if this function is called twice. 
                // Note we set m_siInfo to null at the end of this method.
                return;
            }            
            
            int realVersion = m_siInfo.GetInt32(VersionName);
            int hashsize = m_siInfo.GetInt32(HashSizeName);
            comparer   = (CharComparer)m_siInfo.GetValue(ComparerName, typeof(CharComparer));
            
            if( hashsize != 0) {
                buckets = new int[hashsize];
                for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
                entries = new Entry[hashsize];
                freeList = -1;

                KeyValuePair<char[], TValue>[] array = (KeyValuePair<char[], TValue>[]) 
                    m_siInfo.GetValue(KeyValuePairsName, typeof(KeyValuePair<char[], TValue>[]));

                if (array==null) {
                    throw new SerializationException("Serialization_MissingKeyValuePairs");
                }

                for (int i=0; i<array.Length; i++) {
                    if ( array[i].Key == null) {
                        throw new SerializationException("Serialization_NullKey");
                    }
                    Insert(array[i].Key, array[i].Value, true);
                }
            }
            else {
                buckets = null;
            }

            version = realVersion;
            m_siInfo=null;
        }

        private void Resize() {
            int newSize = HashHelpers.GetPrime(count * 2);
            int[] newBuckets = new int[newSize];
            for (int i = 0; i < newBuckets.Length; i++) newBuckets[i] = -1;
            Entry[] newEntries = new Entry[newSize];
            Array.Copy(entries, 0, newEntries, 0, count);
            for (int i = 0; i < count; i++) {
                int bucket = newEntries[i].hashCode % newSize;
                newEntries[i].next = newBuckets[bucket];
                newBuckets[bucket] = i;
            }
            buckets = newBuckets;
            entries = newEntries;
        }

        public bool Remove(char[] key) {
            if(key == null) {
                throw new ArgumentNullException("key");
            }

            if (buckets != null) {
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
                int bucket = hashCode % buckets.Length;
                int last = -1;
                for (int i = buckets[bucket]; i >= 0; last = i, i = entries[i].next) {
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key)) {
                        if (last < 0) {
                            buckets[bucket] = entries[i].next;
                        }
                        else {
                            entries[last].next = entries[i].next;
                        }
                        entries[i].hashCode = -1;
                        entries[i].next = freeList;
                        entries[i].key = default(char[]);
                        entries[i].value = default(TValue);
                        freeList = i;
                        freeCount++;
                        version++;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool TryGetValue(char[] key, out TValue value) {
            int i = FindEntry(key);
            if (i >= 0) {
                value = entries[i].value;
                return true;
            }
            value = default(TValue);
            return false;
        }

        bool ICollection<KeyValuePair<char[],TValue>>.IsReadOnly {
            get { return false; }
        }

        void ICollection<KeyValuePair<char[],TValue>>.CopyTo(KeyValuePair<char[],TValue>[] array, int index) {
            CopyTo(array, index);
        }

        void ICollection.CopyTo(Array array, int index) {
            if (array == null) {
                throw new ArgumentNullException("array");
            }
            
            if (array.Rank != 1) {
                throw new ArgumentException("Arg_RankMultiDimNotSupported");
            }

            if( array.GetLowerBound(0) != 0 ) {
                throw new ArgumentException("Arg_NonZeroLowerBound");
            }
            
            if (index < 0 || index > array.Length) {
                throw new ArgumentOutOfRangeException("index", "ArgumentOutOfRange_NeedNonNegNum");
            }

            if (array.Length - index < Count) {
                throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
            }
            
            KeyValuePair<char[],TValue>[] pairs = array as KeyValuePair<char[],TValue>[];
            if (pairs != null) {
                CopyTo(pairs, index);
            }
            else if( array is DictionaryEntry[]) {
                DictionaryEntry[] dictEntryArray = array as DictionaryEntry[];
                Entry[] entries = this.entries;
                for (int i = 0; i < count; i++) {
                    if (entries[i].hashCode >= 0) {
                        dictEntryArray[index++] = new DictionaryEntry(entries[i].key, entries[i].value);
                    }
                }                
            }
            else {
                object[] objects = array as object[];
                if (objects == null) {
                    throw new ArgumentException("Argument_InvalidArrayType");
                }

                try {
                    int count = this.count;
                    Entry[] entries = this.entries;
                    for (int i = 0; i < count; i++) {
                        if (entries[i].hashCode >= 0) {
                            objects[index++] = new KeyValuePair<char[],TValue>(entries[i].key, entries[i].value);
                        }
                    }
                }
                catch(ArrayTypeMismatchException) {
                    throw new ArgumentException("Argument_InvalidArrayType");
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }
    
        bool ICollection.IsSynchronized {
            get { return false; }
        }

        object ICollection.SyncRoot { 
            get { 
                if( _syncRoot == null) {
                    System.Threading.Interlocked.CompareExchange(ref _syncRoot, new Object(), null);    
                }
                return _syncRoot; 
            }
        }

        bool IDictionary.IsFixedSize {
            get { return false; }
        }

        bool IDictionary.IsReadOnly {
            get { return false; }
        }

        ICollection IDictionary.Keys {
            get { return (ICollection)Keys; }
        }
    
        ICollection IDictionary.Values {
            get { return (ICollection)Values; }
        }
    
        object IDictionary.this[object key] {
            get { 
                if( IsCompatibleKey(key)) {                
                    int i = FindEntry((char[])key);
                    if (i >= 0) { 
                        return entries[i].value;                
                    }
                }
                return null; 
            }
            set {                 
                VerifyKey(key);
                VerifyValueType(value);
                this[(char[])key] = (TValue)value; 
            }
        }
    
        private static void VerifyKey(object key) {
            if( key == null) {
                throw new ArgumentNullException("key");                
            }

            if( !(key is char[]) ) {
                throw new ArgumentException("Invalid type", "key");
            }            
        }

        private static bool IsCompatibleKey(object key) {
            if( key == null) {
                throw new ArgumentNullException("key");                
            }
            
            return (key is char[]); 
        }

        private static void VerifyValueType(object value) {
            if( (value is TValue) || ( value == null && !typeof(TValue).IsValueType) ) {
                return;
            }
            throw new ArgumentException("Invalid type", "value");                    
        }

        void IDictionary.Add(object key, object value) {            
            VerifyKey(key);
            VerifyValueType(value);
            Add((char[])key, (TValue)value);
        }
    
        bool IDictionary.Contains(object key) {            
            if(IsCompatibleKey(key)) {
                return ContainsKey((char[])key);
            }
            return false;
        }
    
        IDictionaryEnumerator IDictionary.GetEnumerator() {
            return new Enumerator(this, Enumerator.DictEntry);
        }
    
        void IDictionary.Remove(object key) {            
            if(IsCompatibleKey(key)) {
                Remove((char[])key);
            }               
        }

        [Serializable()]                    
        public struct Enumerator: IEnumerator<KeyValuePair<char[],TValue>>,
            IDictionaryEnumerator
        {
            private FastDictionary<TValue> dictionary;
            private int version;
            private int index;
            private KeyValuePair<char[],TValue> current;
            private int getEnumeratorRetType;  // What should Enumerator.Current return?
            
            internal const int DictEntry = 1;
            internal const int KeyValuePair = 2;

            internal Enumerator(FastDictionary<TValue> dictionary, int getEnumeratorRetType) {
                this.dictionary = dictionary;
                version = dictionary.version;
                index = 0;
                this.getEnumeratorRetType = getEnumeratorRetType;
                current = new KeyValuePair<char[], TValue>();
            }

            public bool MoveNext() {
                if (version != dictionary.version) {
                    throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
                }

                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is Int32.MaxValue
                while ((uint)index < (uint)dictionary.count) {
                    if (dictionary.entries[index].hashCode >= 0) {
                        current = new KeyValuePair<char[], TValue>(dictionary.entries[index].key, dictionary.entries[index].value);
                        index++;
                        return true;
                    }
                    index++;
                }

                index = dictionary.count + 1;
                current = new KeyValuePair<char[], TValue>();
                return false;
            }

            public KeyValuePair<char[],TValue> Current {
                get { return current; }
            }

            public void Dispose() {
            }

            object IEnumerator.Current {
                get { 
                    if( index == 0 || (index == dictionary.count + 1)) {
                        throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");                        
                    }      

                    if (getEnumeratorRetType == DictEntry) {
                        return new System.Collections.DictionaryEntry(current.Key, current.Value);
                    } else {
                        return new KeyValuePair<char[], TValue>(current.Key, current.Value);
                    }
                }
            }

            void IEnumerator.Reset() {
                if (version != dictionary.version) {
                    throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
                }

                index = 0;
                current = new KeyValuePair<char[], TValue>();	
            }

            DictionaryEntry IDictionaryEnumerator.Entry {
                get { 
                    if( index == 0 || (index == dictionary.count + 1)) {
                         throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");                        
                    }                        
                    
                    return new DictionaryEntry(current.Key, current.Value); 
                }
            }

            object IDictionaryEnumerator.Key {
                get { 
                    if( index == 0 || (index == dictionary.count + 1)) {
                         throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");                        
                    }                        
                    
                    return current.Key; 
                }
            }

            object IDictionaryEnumerator.Value {
                get { 
                    if( index == 0 || (index == dictionary.count + 1)) {
                         throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");                        
                    }                        
                    
                    return current.Value; 
                }
            }
        }

        [DebuggerDisplay("Count = {Count}")]        
        [Serializable()]                            
        public sealed class KeyCollection: ICollection<char[]>, ICollection
        {
            private FastDictionary<TValue> dictionary;

            public KeyCollection(FastDictionary<TValue> dictionary) {
                if (dictionary == null) {
                    throw new ArgumentNullException("dictionary");
                }
                this.dictionary = dictionary;
            }

            public Enumerator GetEnumerator() {
                return new Enumerator(dictionary);
            }

            public void CopyTo(char[][] array, int index) {
                if (array == null) {
                    throw new ArgumentNullException("array");
                }

                if (index < 0 || index > array.Length) {
                    throw new ArgumentOutOfRangeException("index", "ArgumentOutOfRange_NeedNonNegNum");
                }

                if (array.Length - index < dictionary.Count) {
                    throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
                }
                
                int count = dictionary.count;
                Entry[] entries = dictionary.entries;
                for (int i = 0; i < count; i++) {
                    if (entries[i].hashCode >= 0) array[index++] = entries[i].key;
                }
            }

            public int Count {
                get { return dictionary.Count; }
            }

            bool ICollection<char[]>.IsReadOnly {
                get { return true; }
            }

            void ICollection<char[]>.Add(char[] item){
                throw new NotSupportedException("NotSupported_KeyCollectionSet");
            }
            
            void ICollection<char[]>.Clear(){
                throw new NotSupportedException("NotSupported_KeyCollectionSet");
            }

            bool ICollection<char[]>.Contains(char[] item){
                return dictionary.ContainsKey(item);
            }

            bool ICollection<char[]>.Remove(char[] item){
                throw new NotSupportedException("NotSupported_KeyCollectionSet");
            }
            
            IEnumerator<char[]> IEnumerable<char[]>.GetEnumerator() {
                return new Enumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return new Enumerator(dictionary);                
            }

            void ICollection.CopyTo(Array array, int index) {
                if (array==null) {
                    throw new ArgumentNullException("array");
                }

                if (array.Rank != 1) {
                    throw new ArgumentException("Arg_RankMultiDimNotSupported");
                }

                if( array.GetLowerBound(0) != 0 ) {
                    throw new ArgumentException("Arg_NonZeroLowerBound");
                }

                if (index < 0 || index > array.Length) {
                    throw new ArgumentOutOfRangeException("index", "ArgumentOutOfRange_NeedNonNegNum");
                }

                if (array.Length - index < dictionary.Count) {
                    throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
                }
                
                char[][] keys = array as char[][];
                if (keys != null) {
                    CopyTo(keys, index);
                }
                else {
                    object[] objects = array as object[];
                    if (objects == null) {
                        throw new ArgumentException("Argument_InvalidArrayType");
                    }
                                         
                    int count = dictionary.count;
                    Entry[] entries = dictionary.entries;
                    try {
                        for (int i = 0; i < count; i++) {
                            if (entries[i].hashCode >= 0) objects[index++] = entries[i].key;
                        }
                    }                    
                    catch(ArrayTypeMismatchException) {
                        throw new ArgumentException("Argument_InvalidArrayType");
                    }
                }
            }

            bool ICollection.IsSynchronized {
                get { return false; }
            }

            Object ICollection.SyncRoot { 
                get { return ((ICollection)dictionary).SyncRoot; }
            }

            [Serializable()]                    
            public struct Enumerator : IEnumerator<char[]>, System.Collections.IEnumerator
            {
                private FastDictionary<TValue> dictionary;
                private int index;
                private int version;
                private char[] currenchar;
            
                internal Enumerator(FastDictionary<TValue> dictionary) {
                    this.dictionary = dictionary;
                    version = dictionary.version;
                    index = 0;
                    currenchar = default(char[]);                    
                }

                public void Dispose() {
                }

                public bool MoveNext() {
                    if (version != dictionary.version) {
                        throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
                    }

                    while ((uint)index < (uint)dictionary.count) {
                        if (dictionary.entries[index].hashCode >= 0) {
                            currenchar = dictionary.entries[index].key;
                            index++;
                            return true;
                        }
                        index++;
                    }

                    index = dictionary.count + 1;
                    currenchar = default(char[]);
                    return false;
                }
                
                public char[] Current {
                    get {                        
                        return currenchar;
                    }
                }

                Object System.Collections.IEnumerator.Current {
                    get {                      
                        if( index == 0 || (index == dictionary.count + 1)) {
                             throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");                        
                        }                        
                        
                        return currenchar;
                    }
                }
                
                void System.Collections.IEnumerator.Reset() {
                    if (version != dictionary.version) {
                        throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");                        
                    }

                    index = 0;                    
                    currenchar = default(char[]);
                }
            }                        
        }

        [DebuggerDisplay("Count = {Count}")]
        [Serializable()]                                    
        public sealed class ValueCollection: ICollection<TValue>, ICollection
        {
            private FastDictionary<TValue> dictionary;

            public ValueCollection(FastDictionary<TValue> dictionary) {
                if (dictionary == null) {
                    throw new ArgumentNullException("dictionary");
                }
                this.dictionary = dictionary;
            }

            public Enumerator GetEnumerator() {
                return new Enumerator(dictionary);                
            }

            public void CopyTo(TValue[] array, int index) {
                if (array == null) {
                    throw new ArgumentNullException("array");
                }

                if (index < 0 || index > array.Length) {
                    throw new ArgumentOutOfRangeException("index", "ArgumentOutOfRange_NeedNonNegNum");
                }

                if (array.Length - index < dictionary.Count) {
                    throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
                }
                
                int count = dictionary.count;
                Entry[] entries = dictionary.entries;
                for (int i = 0; i < count; i++) {
                    if (entries[i].hashCode >= 0) array[index++] = entries[i].value;
                }
            }

            public int Count {
                get { return dictionary.Count; }
            }

            bool ICollection<TValue>.IsReadOnly {
                get { return true; }
            }

            void ICollection<TValue>.Add(TValue item){
                throw new NotSupportedException("NotSupported_ValueCollectionSet");
            }

            bool ICollection<TValue>.Remove(TValue item){
                throw new NotSupportedException("NotSupported_ValueCollectionSet");
            }

            void ICollection<TValue>.Clear(){
                throw new NotSupportedException("NotSupported_ValueCollectionSet");
            }

            bool ICollection<TValue>.Contains(TValue item){
                return dictionary.ContainsValue(item);
            }

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() {
                return new Enumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return new Enumerator(dictionary);                
            }

            void ICollection.CopyTo(Array array, int index) {
                if (array == null) {
                    throw new ArgumentNullException("array");
                }

                if (array.Rank != 1) {
                    throw new ArgumentException("Arg_RankMultiDimNotSupported");
                }

                if( array.GetLowerBound(0) != 0 ) {
                    throw new ArgumentException("Arg_NonZeroLowerBound");
                }

                if (index < 0 || index > array.Length) { 
                    throw new ArgumentOutOfRangeException("index", "ArgumentOutOfRange_NeedNonNegNum");
                }

                if (array.Length - index < dictionary.Count)
                    throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
                
                TValue[] values = array as TValue[];
                if (values != null) {
                    CopyTo(values, index);
                }
                else {
                    object[] objects = array as object[];
                    if (objects == null) {
                        throw new ArgumentException("Argument_InvalidArrayType");
                    }

                    int count = dictionary.count;
                    Entry[] entries = dictionary.entries;
                    try {
                        for (int i = 0; i < count; i++) {
                            if (entries[i].hashCode >= 0) objects[index++] = entries[i].value;
                        }
                    }
                    catch(ArrayTypeMismatchException) {
                        throw new ArgumentException("Argument_InvalidArrayType");
                    }
                }
            }

            bool ICollection.IsSynchronized {
                get { return false; }
            }

            Object ICollection.SyncRoot { 
                get { return ((ICollection)dictionary).SyncRoot; }
            }

            [Serializable()]                    
            public struct Enumerator : IEnumerator<TValue>, System.Collections.IEnumerator
            {
                private FastDictionary<TValue> dictionary;
                private int index;
                private int version;
                private TValue currentValue;
            
                internal Enumerator(FastDictionary<TValue> dictionary) {
                    this.dictionary = dictionary;
                    version = dictionary.version;
                    index = 0;
                    currentValue = default(TValue);
                }

                public void Dispose() {
                }

                public bool MoveNext() {                    
                    if (version != dictionary.version) {
                        throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
                    }
                    
                    while ((uint)index < (uint)dictionary.count) {
                        if (dictionary.entries[index].hashCode >= 0) {
                            currentValue = dictionary.entries[index].value;
                            index++;
                            return true;
                        }
                        index++;
                    }
                    index = dictionary.count + 1;
                    currentValue = default(TValue);
                    return false;
                }
                
                public TValue Current {
                    get {                        
                        return currentValue;
                    }
                }

                Object System.Collections.IEnumerator.Current {
                    get {                      
                        if( index == 0 || (index == dictionary.count + 1)) {
                             throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");                        
                        }                        
                        
                        return currentValue;
                    }
                }
                
                void System.Collections.IEnumerator.Reset() {
                    if (version != dictionary.version) {
                        throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
                    }
                    index = 0;                    
                    currentValue = default(TValue);
                }
            }
        }
    }

    internal static class HashHelpers
    {
        // Table of prime numbers to use as hash table sizes. 
        // The entry used for capacity is the smallest prime number in this aaray
        // that is larger than twice the previous capacity. 

        internal static readonly int[] primes = {
            3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
            1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
            17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
            187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
            1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369};

        internal static bool IsPrime(int candidate)
        {
            if ((candidate & 1) != 0)
            {
                int limit = (int)Math.Sqrt(candidate);
                for (int divisor = 3; divisor <= limit; divisor += 2)
                {
                    if ((candidate % divisor) == 0)
                        return false;
                }
                return true;
            }
            return (candidate == 2);
        }

        internal static int GetPrime(int min)
        {
            if (min < 0)
                throw new ArgumentException("min");

            for (int i = 0; i < primes.Length; i++)
            {
                int prime = primes[i];
                if (prime >= min) return prime;
            }

            //outside of our predefined table. 
            //compute the hard way. 
            for (int i = (min | 1); i < Int32.MaxValue; i += 2)
            {
                if (IsPrime(i))
                    return i;
            }
            return min;
        }
    } 
}
