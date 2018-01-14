using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SearchEngine.Indexing;

namespace SearchEngine.Query
{
    class BM25ADPT : IScorer
    {

        private readonly ReadableIndex _index;
        private readonly double k3;
        private readonly double b;
        
        public BM25ADPT(ReadableIndex index, double k3, double b)
        {
            _index = index;
            this.k3 = k3;
            this.b = b;
        }

        //keywords-> alle keywords für ein topic
        public List<(string documents, float score)> Score(List<(string, int)> keywords)
        {
            foreach(var temp11 in keywords)
            {
            //    Console.WriteLine(temp11);
            }

            var temp = _index.GetPostingLists(keywords.Select(k => k.Item1).ToList());
            Dictionary<String, IReadOnlyList<PostingEntry>> keywordsWithDocuments=new Dictionary<string, IReadOnlyList<PostingEntry>>();
            foreach (var foo in temp)
            {
                keywordsWithDocuments.Add(foo.keyword, foo.entries);
            }


            // sum of scores per document index 
            var results = new Dictionary<int, float>();
            int counter = 0;
            int length = keywordsWithDocuments.Count();
            foreach (var term in keywordsWithDocuments)
            {
                // idf of term posting.word
                //Console.WriteLine("current keyword: " + term.Key + " DocumentFrequency: " + term.Value.Count);
                float inverseDocumentFrequency = (float)
                    Math.Log((_index.GetTotalDocumentCount() - term.Value.Count + 0.5D) / (term.Value.Count + 0.5D));

                int queryTermFreuency = keywords.Find(k => k.Item1 == term.Key).Item2;


                float[] IGs = new float[100];
                IGs[0] = IG(term, 0);
                for (int i = 1; i < IGs.Length; i++)
                {
                    IGs[i] = IG(term, i);
                    if (IGs[i - 1] > IGs[i])
                    {
                        float[] tempCopy = new float[i+1];
                        for (int j = 0; j < i; j++)
                        {
                            tempCopy[j] = IGs[j];
                        }
                        IGs = tempCopy;
                        break;
                    }
                }

                foreach(var temp1 in IGs)
                {
                   // Console.WriteLine("IG: "+temp1);
                }

                float k1 = getk1(IGs);
                //Console.WriteLine("k1: "+k1);

                foreach (var document in term.Value)
                {
                    var c = GetCD(document);
                    var ck = (c * (k1 + 1) / (k1 + c));

                    var bm25_adpt = queryTermFreuency * ck * IGs[1];

                    if (results.TryGetValue(document.documentIndex, out var val))
                    {
                        results[document.documentIndex] = val + bm25_adpt;
                    }
                    else
                    {
                        results.Add(document.documentIndex, bm25_adpt);
                    }

                }
                counter++;
            }

            return results
                .Select(pair => (pair.Key, pair.Value))
                .OrderByDescending(pair => pair.Item2)
                .Take(1000)
                .Select(pair => (_index.GetDocumentFromIndex(pair.Item1), pair.Item2))
                .ToList();
        }

        private float getdft(int t, KeyValuePair<String, IReadOnlyList<PostingEntry>> term, int DocumentFrequency)
        {
            if (t == 0)
            {
                return _index.GetTotalDocumentCount();
            }
            else if (t == 1)
            {
                return term.Value.Count;
            }
            else
            {
                int count = 0;
                foreach (var temp in term.Value)
                {
                    if (GetCD(temp) >= t - 0.5)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        private float GetCD(PostingEntry document)
        {
            return (float)(document.termFrequency / (1 - b + b * _index.GetDocumentLength(document.documentIndex) / _index.GetAverageDocumentLength()));
        }

        private float IG(KeyValuePair<String, IReadOnlyList<PostingEntry>> term, int t)
        {
            return (float)(-Math.Log((term.Value.Count + 0.5) / (_index.GetTotalDocumentCount() + 1), 2)
                        + Math.Log((getdft(t + 1, term, term.Value.Count) + 0.5) / (getdft(t, term, term.Value.Count) + 1), 2));
        }
        private float getk1(float[] IGs)
        {
            // igs is a list sorted by t due to calc process

            double k1_best = 0.2;
            double sqSum_best = double.MaxValue;

            for (double j = k1_best; j <= 2.5; j+=0.1)
            {
                //Console.WriteLine("new k1: "+j);
                double sqSum = 0;

                for (int t = 0; t < IGs.Length; t++)
                {
                    double foo = Math.Pow(((IGs[t] / IGs[1]) - (((j + 1) * t) / (j + t))), 2);
                    sqSum += foo;
                }
                //Console.WriteLine("sqSum: " + sqSum + " best: "+sqSum_best);
                if (sqSum < sqSum_best)
                {
                    sqSum_best = sqSum;
                    k1_best = j;
                }
            }
            
            return (float)k1_best;

        }

    }

}


