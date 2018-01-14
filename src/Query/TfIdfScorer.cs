using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SearchEngine.Indexing;

namespace SearchEngine.Query
{
    public class TfIdfScorer : IScorer
    {
        private readonly ReadableIndex _index;

        public TfIdfScorer(ReadableIndex index)
        {
            _index = index;
        }

        public List<(string documents, float score)> Score(List<(string, int)> keywords)
        {
            var postings = _index.GetPostingLists(keywords.Select(k => k.Item1).ToList());

            // sum of scores per document index 
            var results = new Dictionary<int , float>();

            foreach (var posting in postings)
            {
                // idf of term posting.word
                var inverseDocumentFrequency = Math.Log(_index.GetTotalDocumentCount() / posting.entries.Count);

                foreach (var occurence in posting.entries)
                {
                    // tf of word in document
                    var termFrequency = Math.Log(1 + occurence.termFrequency);

                    var tfIdf = (float)(termFrequency * inverseDocumentFrequency);

                    if (results.TryGetValue(occurence.documentIndex, out var val))
                    {
                        results[occurence.documentIndex] = val + tfIdf;
                    }
                    else
                    {
                        results.Add(occurence.documentIndex, tfIdf);
                    }
                }
            }

            return results
                .Select(pair => (pair.Key,pair.Value))
                .OrderByDescending(pair => pair.Item2)
                .Take(1000)
                .Select(pair => (_index.GetDocumentFromIndex(pair.Item1), pair.Item2))
                .ToList();
        }
    }
}
