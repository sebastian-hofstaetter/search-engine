using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace SearchEngine.Indexing
{
    public class PostingList
    {
        private int _length = 1;
        private int _lastDocumentIndex;

        private PostingEntry _first;
        private PostingEntry[] _entryBuffer;

        private readonly ArrayPool<PostingEntry> _pool;

        public PostingList(ArrayPool<PostingEntry> pool,int firstElementDocumentIndex)
        {
            _pool = pool;
            _first.termFrequency = 1;
            _first.documentIndex = firstElementDocumentIndex;
            _lastDocumentIndex = firstElementDocumentIndex;
        }

        public void Add(int documentIndex)
        {
            if(_length == 1)
            {
                if (documentIndex == _lastDocumentIndex)
                {
                    _first.termFrequency += 1;
                }
                else
                {
                    _entryBuffer = _pool.Rent(16);
                    _entryBuffer[0].documentIndex = documentIndex;
                    _entryBuffer[0].termFrequency = 1;
                    _length++;
                    _lastDocumentIndex = documentIndex;
                }
                return;
            }

            if(documentIndex == _lastDocumentIndex)
            {
                _entryBuffer[_length - 2].termFrequency += 1;
            }
            else
            {
                if(_length == _entryBuffer.Length)
                {
                    int newLength;
                    if (_length > 5_000) // slower growth = less memory usage ??
                    {
                        newLength = _length + (_length / 2);
                    }
                    else
                    {
                        newLength = _length * 2;
                    }

                    ResizeEntryBuffer(newLength);
                }

                _entryBuffer[_length - 1].documentIndex = documentIndex;
                _entryBuffer[_length - 1].termFrequency = 1;
                _length++;

                _lastDocumentIndex = documentIndex;
            }
        }

        private void ResizeEntryBuffer(int newLength)
        {
            var updated = _pool.Rent(newLength);

            Array.Copy(_entryBuffer, 0, updated, 0, _length - 1);
            _pool.Return(_entryBuffer);

            _entryBuffer = updated;
        }

        public void Append(PostingList list, int documentIndexOffset)
        {
            //
            // prepare buffer
            //
            var newLength = _length + list._length;

            if(_entryBuffer == null)
            {
                _entryBuffer = _pool.Rent(newLength - 1);
            }
            else if (newLength - 1 > _entryBuffer.Length)
            {
                ResizeEntryBuffer(newLength - 1);
            }

            //
            // merge entries
            //
            _entryBuffer[_length - 1] = list._first;
            if (list._length > 1)
            {
                Array.Copy(list._entryBuffer, 0, _entryBuffer, _length, list._length - 1);
            }

            //
            // add offset to old values in new location
            //
            for (int i = _length - 1; i < newLength - 1; i++)
            {
                _entryBuffer[i].documentIndex += documentIndexOffset;
            }

            // set state fields
            _length = newLength;
            _lastDocumentIndex = _entryBuffer[_length - 2].documentIndex;
        }

        public void IncreaseDocumentIndex(int documentIndexOffset)
        {
            _first.documentIndex += documentIndexOffset;

            if (_length == 1)
            {
                return;
            }

            for (int i = 0; i < _length - 1; i++)
            {
                _entryBuffer[i].documentIndex += documentIndexOffset;
            }
            _lastDocumentIndex = _entryBuffer[_length - 2].documentIndex;
        }

        public int GetLength()
        {
            return _length;
        }

        public List<PostingEntry> ToList()
        {
            var result = new List<PostingEntry>(_length);
            result.Add(_first);
            for (int i = 0; i < _length - 1; i++)
            {
                result.Add(_entryBuffer[i]);
            }
            return result;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(_length);

            writer.Write(_first.documentIndex);
            writer.Write(_first.termFrequency);

            var l = _length - 1;
            for (int i = 0; i < l; i++)
            {
                writer.Write(_entryBuffer[i].documentIndex);
                writer.Write(_entryBuffer[i].termFrequency);
            }
        }
    }
}