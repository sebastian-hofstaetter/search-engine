using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.Indexing
{
    /// <summary>
    /// Index optimized for high performance index creation
    /// </summary>
    public sealed class WriteableIndex : IndexBase
    {
        // word, list of document indices
        private readonly FastDictionary<PostingList> _store;
        private readonly ArrayPool<PostingEntry> _pool;

        public WriteableIndex(IndexOptions options) : base(options)
        {
            _pool = ArrayPool<PostingEntry>.Create(300_000, 3_000);
            _store = new FastDictionary<PostingList>(389357); // prime number that should be large enough for our training data
        }

        /// <summary>
        /// Do not add the same document twice !
        /// </summary>
        /// <returns>Unique document number, that is used for indexing</returns>
        public int RegisterDocument(string document)
        {
            _documents.Add(document);
            _documentLength.Add(0);

            return _documents.Count - 1;
        }

        /// <summary>
        /// Checks if a document is already in the index, returns the documentIndex
        /// </summary>
        /// <returns>Unique document number, that is used for indexing; -1 if not found</returns>
        public int IsDocumentInIndex(string document)
        {
            return _documents.FindIndex(d => d == document) ;
        }

        /// <summary>
        /// Register the document before with <see cref="RegisterDocument"/> and use the returned documentIndex
        /// </summary>
        /// <param name="wordBuffer"></param>
        /// <param name="wordLength"></param>
        /// <param name="documentIndex"></param>
        public void AddWord(char[] wordBuffer, int wordLength, int documentIndex)
        {
            var index = _store.InitOrGetPositionFromBuffer(wordBuffer, wordLength);
            var list = _store.GetAtPosition(index);

            if (list != null)
            {
                list.Add(documentIndex);
            }
            else
            {
                _store.StoreAtPosition(index, new PostingList(_pool, documentIndex));
            }

            _documentLength[documentIndex] += 1;
        }

        /// <summary>
        /// Merges the given index into the called index. The given index gets changed and should be discarded  
        /// </summary>
        public WriteableIndex Merge(WriteableIndex other)
        {
            var offset = _documents.Count;

            // add documents to list
            _documents.AddRange(other._documents);
            _documentLength.AddRange(other._documentLength);

            // add dictionary (with added offset)
            foreach (var item in other._store)
            {
                // add to dict (existing or new)
                var index = _store.InitOrGetPosition(item.Key);
                var itemOld = _store.GetAtPosition(index);
                if (itemOld == null)
                {
                    item.Value.IncreaseDocumentIndex(offset);
                    _store.StoreAtPosition(index, item.Value);
                }
                else
                {
                    itemOld.Append(item.Value, offset);
                }
            }

            return this;
        }

        public void PrintStats()
        {
            var wordLengths = new Dictionary<int, int>();
            var postingLengths = new Dictionary<int, int>();

            var possibleStopWords = new List<(int,string)>();
            foreach (var item in _store)
            {
                if (wordLengths.TryGetValue(item.Key.Length, out var wordVal))
                {
                    wordLengths[item.Key.Length] = wordVal + 1;
                }
                else
                {
                    wordLengths[item.Key.Length] = 1;
                }

                if (item.Value.GetLength() > 100_000 && item.Key.Length <= 4)
                {
                    possibleStopWords.Add((item.Value.GetLength(),new String(item.Key)));
                }

                if (postingLengths.TryGetValue(item.Value.GetLength(), out var postVal))
                {
                    postingLengths[item.Value.GetLength()] = postVal + 1;
                }
                else
                {
                    postingLengths[item.Value.GetLength()] = 1;
                }

            }

            foreach (var item in possibleStopWords.OrderByDescending(x => x.Item1))
            {
                Console.WriteLine(item.Item1 + "\t" + item.Item2);
            }

            Console.WriteLine("words: " + _store.Count);
            Console.WriteLine("wordlengths: ");
            foreach (var item in wordLengths.OrderByDescending(x => x.Key).Take(100))
            {
                Console.WriteLine(item.Key + "\t" + item.Value);
            }

            Console.WriteLine("postinglengths:");
            foreach (var item in postingLengths.OrderBy(x => x.Key).Take(100))
            {
                Console.WriteLine(item.Key + "\t" + item.Value);
            }
        }

        public void Serialize(Stream stream)
        {
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(stream, Options);

            var writer = new BinaryWriter(stream);

            // sizes 

            writer.Write(_documents.Count);
            writer.Write(_store.Count);

            // documents 
            foreach (var d in _documents)
            {
                writer.Write(d);
            }
            foreach (var d in _documentLength)
            {
                writer.Write(d);
            }

            writer.Flush();

            // posting list
            foreach (var postingList in _store)
            {
                writer.Write(postingList.Key.Length);
                writer.Write(postingList.Key);

                postingList.Value.Serialize(writer);
            }

            writer.Flush();
        }
    }
}
