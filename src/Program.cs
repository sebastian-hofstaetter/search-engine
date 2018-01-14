using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SearchEngine.Indexing;
using SearchEngine.Query;

namespace SearchEngine
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(
                    "Usage:\nindex  ../../../Data/Files ../index-all.bin IndexOptions(CaseFolding,RemoveStopWords,DoStemming)\n" +
                    "search run-all ../../../Data/topicsTREC8Adhoc.txt ../index-all.bin QueryOptions(UseHeadline,UseDescription,UseNarrative)");
                //Console.Read();
                return;
            }

            if (args[0] == "index")
            {
                Index(args[1], args[2], IndexOptions.Parse(args[3]));
            }
            else if (args[0] == "search")
            {
                Search(args[1], args[2], args[3], QueryOptions.Parse(args[4]));
            }
        }

        public static void Index(string folder, string indexFile, IndexOptions options)
        {
            var totalTime = Stopwatch.StartNew();
            Console.WriteLine("Creating index: " + indexFile);
            Console.WriteLine(options);
            Console.WriteLine("-----------------------------------------------");

            var indexer = new Indexer();
            var (index, files, docs, size) = indexer.IndexAllParallel(options, folder);

            totalTime.Stop();

            Console.WriteLine("-----------------------------------------------");
            Console.WriteLine("Finished indexing after: " + totalTime.Elapsed);
            Console.WriteLine("Indexed files:           " + files);
            Console.WriteLine("Indexed documents:       " + docs);
            Console.WriteLine("Indexed size (mb):       " + size / 1_000_000d);
            Console.WriteLine("Total throughput (mb/s): " + (size / 1_000_000d) / (totalTime.ElapsedMilliseconds / 1_000d));

            //index.PrintStats();

            GC.Collect(2);

            Console.WriteLine("-----------------------------------------------");
            Console.WriteLine("Flushing index file to disk ...");

            totalTime.Restart();
            IndexSerialization.SerializeIndexToDisk(index, indexFile);
            totalTime.Stop();

            Console.WriteLine("Finished index writing after: " + totalTime.Elapsed);
            Console.Read();

        }

        private static object lockObject = new Object();

        public static void Search(string runName, string searchFile, string indexFile, QueryOptions queryOptions)
        {
            var totalTime = Stopwatch.StartNew();

            Console.WriteLine("Evaluating query with index: " + indexFile);
            Console.WriteLine(queryOptions);
            Console.WriteLine("-----------------------------------------------");
            Console.WriteLine("Reading index file from disk ...");

            totalTime.Restart();

            var index = IndexSerialization.DeserializeFromDisk(indexFile);

            totalTime.Stop();
            Console.WriteLine("Finished index reading after: " + totalTime.Elapsed);
            Console.WriteLine(index.Options);
            Console.WriteLine("-----------------------------------------------");
            Console.WriteLine("Parsing search topics ...");
            totalTime.Restart();

            var topics = TopicParser.ParseTopics(searchFile,queryOptions, index.Options);

            totalTime.Stop();
            Console.WriteLine("Finished topics parsing: " + totalTime.Elapsed);
            Console.WriteLine("Searching for all " + topics.Count+" topics");
            totalTime.Restart();

            IScorer[] scorerList = {
                new TfIdfScorer(index),
                new BM25Scorer(index, 1.2, 100, 0.75),
                new BM25ADPT(index, 100, 0.75),
            };
 
            var results = new List<(int topic, List<(string document, float score)>)>();


            for (int i = 0; i < scorerList.Length; i++)
            {
                var name = scorerList[i].GetType().Name;
                var resultFile = "results-" + runName + "-" + name + ".txt";
                Console.WriteLine("\nUsing scorer: " + name);

                results = new List<(int topic, List<(string document, float score)>)>();

                Parallel.ForEach(topics, (topic) =>
                {
                    var docs = scorerList[i].Score(topic.keywords);

                    lock (lockObject)
                    {
                        results.Add((topic.topic, docs));
                    }

                });

                totalTime.Stop();
                Console.WriteLine("Finished searching after: " + totalTime.Elapsed);
                Console.WriteLine("Writing results to disk: " + resultFile);
                totalTime.Restart();

                ResultPrinter(results.OrderBy(p => p.topic).ToList(), resultFile, runName);
            }

           // BooleanScorer scorer = new BooleanScorer(index);
           // foreach(var topic in topics)
           // {
           //     List<PostingEntryNode> result = scorer.Score(topics.ElementAt(1).keywords);
           //     foreach (var temp in result)
           //     {
           //         Console.WriteLine(temp.key.documentIndex);
           //     }
           //}

            Console.WriteLine("Finished scoring");

            Console.Read();        
        }

        private static void ResultPrinter(List<(int topic, List<(string document, float score)>)> results,string filePath,string runName)
        {
            using(var file = File.CreateText(filePath))
            {
                foreach (var topic in results)
                {
                    var rank = 1;
                    foreach (var line in topic.Item2)
                    {
                        file.WriteLine(topic.topic + " Q0 " + line.document + " " + rank + " " + line.score + " " + runName);
                        rank++;
                    }
                }
            }
        }
    }
}