using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.Indexing
{
    /// <summary>
    /// Index used for querying data, can be serialized from disk
    /// </summary>
    public sealed class ReadableIndex : IndexBase
    {
        private readonly FastDictionary<List<PostingEntry>> _store;

        private double average=-1;

        public ReadableIndex(IndexOptions options, int initialDocumentCapacity, int initialWordCapacity) : base(options, initialDocumentCapacity)
        {
            _store = new FastDictionary<List<PostingEntry>>(initialWordCapacity);
        }

        /// <summary>
        /// Returns the raw <see cref="PostingEntry"/> lists per keyword
        /// </summary>
        /// <returns>Return list keeps order of input set</returns>
        public List<(string keyword, IReadOnlyList<PostingEntry> entries)> GetPostingLists(List<string> keywords)
        {
            var result = new List<(string, IReadOnlyList<PostingEntry>)>();

            foreach (var word in keywords)
            {
                if (_store.TryGetValue(word.ToCharArray(), out var list))
                {
                    result.Add((word, list));
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the count of all docs in the index
        /// </summary>
        public int GetTotalDocumentCount()
        {
            return _documents.Count;
        }

        public double GetAverageDocumentLength()
        {
                if (this.average==-1){

                        average=_documentLength.Average();
                }
                return average;
        }

        /// <summary>
        /// Returns the length of a document
        /// </summary>
        public int GetDocumentLength(int documentIndex)
        {
            return _documentLength[documentIndex];
        }

        /// <summary>
        /// Returns the name of the document registered in the index
        /// </summary>
        public string GetDocumentFromIndex(int documentIndex)
        {
            return _documents[documentIndex];
        }

        public static ReadableIndex Deserialize(Stream stream)
        {
            var options = (IndexOptions)(new BinaryFormatter()).Deserialize(stream);

            var reader = new BinaryReader(stream);

            int documents = reader.ReadInt32();
            int storeSize = reader.ReadInt32();

            var index = new ReadableIndex(options, documents, storeSize);

            // documents
            for (var i = 0; i < documents; i++)
            {
                index._documents.Add(reader.ReadString());
            }
            for (var i = 0; i < documents; i++)
            {
                index._documentLength.Add(reader.ReadInt32());
            }

            // store
            for (int n = 0; n < storeSize; n++)
            {
                var keyLength = reader.ReadInt32();
                var key = reader.ReadChars(keyLength);

                var listSize = reader.ReadInt32();
                var list = new List<PostingEntry>(listSize);
                for (int l = 0; l < listSize; l++)
                {
                    list.Add(new PostingEntry(reader.ReadInt32(), reader.ReadUInt16()));
                }

                index._store.Add(key, list);
            }

            return index;
        }
    }
}
