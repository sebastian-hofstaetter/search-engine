using System.Collections.Generic;

namespace SearchEngine.Indexing
{
    /// <summary>
    /// Base class for read & write specialised index classes
    /// </summary>
    public class IndexBase
    {
        /// <summary>
        /// The options, by which this index is/was created
        /// </summary>
        public IndexOptions Options { get; }

        /// <summary>
        /// List of document ids - implicit document index = index in this list
        /// </summary>
        protected readonly List<string> _documents;

        /// <summary>
        /// Number of terms in the document, same index as <see cref="_documents"/>
        /// </summary>
        protected readonly List<int> _documentLength;

        public IndexBase(IndexOptions options)
        {
            Options = options;

            _documents = new List<string>();
            _documentLength = new List<int>();
        }

        public IndexBase(IndexOptions options, int initialDocumentCapacity)
        {
            Options = options;

            _documents = new List<string>(initialDocumentCapacity);
            _documentLength = new List<int>(initialDocumentCapacity);
        }
    }
}