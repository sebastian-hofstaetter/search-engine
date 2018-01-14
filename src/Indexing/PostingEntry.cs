namespace SearchEngine.Indexing
{
    public struct PostingEntry
    {
        public int documentIndex;
        public ushort termFrequency;

        public PostingEntry(int index, ushort frequency)
        {
            documentIndex = index;
            termFrequency = frequency;
        }
    }
}