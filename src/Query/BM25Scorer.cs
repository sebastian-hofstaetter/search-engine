using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SearchEngine.Indexing;

namespace SearchEngine.Query
{
    class BM25Scorer : IScorer
    {

        private readonly ReadableIndex _index;
        private readonly double k1;
        private readonly double k3;
        private readonly double b;

        public BM25Scorer(ReadableIndex index, double k1, double k3, double b)
        {
            _index = index;
            this.k1 = k1;
            this.k3 = k3;
            this.b = b;
        }

        public List<(string documents, float score)> Score(List<(string, int)> keywords)
        {

            var postings = _index.GetPostingLists(keywords.Select(k => k.Item1).ToList());

            // sum of scores per document index 
            var results = new Dictionary<int, float>();

            foreach (var posting in postings)
            {
                int queryTermFreuency = keywords.Find(k => k.Item1 == posting.keyword).Item2;

                // idf of term posting.word
                var inverseDocumentFrequency = Math.Log(
                    (_index.GetTotalDocumentCount() - posting.entries.Count + 0.5D)
                    / (posting.entries.Count + 0.5D));

                foreach (var occurence in posting.entries)
                {
                    // tf of word in document
                    var termFrequency = Math.Log(1 + occurence.termFrequency);

                    //bm25 from wikipedia

                    var documentTerm = 1 - b + b * (_index.GetDocumentLength(occurence.documentIndex) / _index.GetAverageDocumentLength());

                    var mainTermFreq = ((occurence.termFrequency * (k1 + 1)) / (occurence.termFrequency + k1 * documentTerm));
                    var queryTerms = (((k3 + 1) * queryTermFreuency) / (k3 + queryTermFreuency));

                    var bm25 = (float)(inverseDocumentFrequency * mainTermFreq * queryTerms);

                    if (results.TryGetValue(occurence.documentIndex, out var val))
                    {
                        results[occurence.documentIndex] = val + bm25;
                    }
                    else
                    {
                        results.Add(occurence.documentIndex, bm25);
                    }
                }
            }

            return results
                .Select(pair => (pair.Key, pair.Value))
                .OrderByDescending(pair => pair.Item2)
                .Take(1000)
                .Select(pair => (_index.GetDocumentFromIndex(pair.Item1), pair.Item2))
                .ToList();
        }
    }
}