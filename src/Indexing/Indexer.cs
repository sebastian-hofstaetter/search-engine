using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SearchEngine.Util;

namespace SearchEngine.Indexing
{
    public class Indexer
    {
        private static string[] _stopWords =
        {
            "a","an","and","also","all","are","as","at","be","been","by","but","for","from",
            "have","has","had","he","in","is","it","its","more","new","not",
            "of","on","page","part","that","the","this",
            "to","s","was","were","will","with","1","2","3"
        };

        private (long value,int strLength)[] _stopLongs = StringToLong(_stopWords);

        public unsafe (WriteableIndex index, int files, int docs, long size) IndexAllParallel(IndexOptions options, string folder)
        {
            var timer = Stopwatch.StartNew();

            var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
            Console.WriteLine("Found files: " + files.Length+" - took: " + timer.ElapsedMilliseconds + "ms");

            timer.Restart();

            var tasks = new List<Task<WriteableIndex>>();
            var parallel = Environment.ProcessorCount;

            var docsCount = 0;
            var fileCount = 0;
            var sizeSum = 0L;
            var sizeLocal = 0L;

            for (var p = 0; p < parallel; p++)
            {
                var taskNumber = p;
                tasks.Add(Task.Run(() =>
                {
                    var localIndex = new WriteableIndex(options);
                    var localStemmer = new Stemmer();
                    var localParser = new Parser();

                    var localPart = (files.Length / parallel);
                    var from = taskNumber * localPart;
                    var to = taskNumber == parallel - 1 ? files.Length : from + localPart;

                    for (var i = from; i < to; i++)
                    {
                        using (var mmf = MemoryMappedFile.CreateFromFile(files[i], FileMode.Open))
                        using (var accessor = mmf.CreateViewAccessor())
                        {
                            byte* buffer = null;
                            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref buffer);
                            var len = new FileInfo(files[i]).Length;

                            var docs = localParser.ParseFileFast(buffer, len);

                            Interlocked.Add(ref docsCount, docs.Count);
                            Interlocked.Add(ref sizeSum, len);
                            Interlocked.Add(ref sizeLocal, len);
                            
                            IndexDocuments(localIndex, localStemmer, buffer, docs);

                            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        }


                        if (Interlocked.Increment(ref fileCount) % 100 == 0)
                        {
                            Console.WriteLine(
                                "Finished: " + fileCount + " - "
                                + Math.Round((Interlocked.Read(ref sizeLocal) * 0.000001d)) + " mb - " +
                                + Math.Round((Interlocked.Read(ref sizeLocal) * 0.000001d) / (timer.ElapsedMilliseconds / 1000d), 2) + " mb/s");
                            timer.Restart();

                            Interlocked.Exchange(ref sizeLocal, 0);
                        }
                    }

                    Console.WriteLine("task finished: "+taskNumber);

                    return localIndex;
                }
                ));
            }

            var all = Task.WhenAll(tasks).Result;

            Console.WriteLine("Index building completed. Merging indices ...");

            var master = all[0];

            var mergeTime = Stopwatch.StartNew();

            for (var i = 1; i < all.Length; i++)
            {
                master.Merge(all[i]);
            }
            
            mergeTime.Stop();
            Console.WriteLine("Merge complete after: " + mergeTime.ElapsedMilliseconds + " ms");

            timer.Stop();

            return (master, fileCount, docsCount, sizeSum);
        }

        private unsafe void IndexDocuments(WriteableIndex index, Stemmer stemmer,byte* buffer, List<(string id, int from, int length)> docs)
        {
            Encoding uTF8 = Encoding.UTF8;

            char* wordBuffer = stackalloc char[100];

            foreach (var doc in docs)
            {
                var documentIndex = index.RegisterDocument(doc.id);
                var currentWordStart = doc.from;
                var until = doc.from + doc.length;

                for (var i = doc.from; i < until; i++)
                {
                    // 47 = '/' 58 = ':'59 = ';'  61 = '=' 63 = '?'
                    var b = *(buffer + i);
                    if (b <= 47 || b == 58 || b == 59 || b == 61 || b == 63)
                    {
                        // we have a split character

                        // - do we have to process a word ? 
                        // - or do we need to start with a word ?

                        if (currentWordStart < i)
                        {
                            if (buffer[currentWordStart] != 60 && buffer[i - 1] != 62) // 60 '<' Ignore xml tags
                            {
                                var charCount = uTF8.GetChars(buffer + currentWordStart, i - currentWordStart, wordBuffer, 100);

                                if (charCount <= 4) // needed for the stopword check
                                {
                                    for (int t = charCount; t <= 4; t++)
                                    {
                                        wordBuffer[t] = '\0';
                                    }
                                }

                                ProcessWord(wordBuffer, charCount, documentIndex, stemmer, index);
                            }
                        }
                        currentWordStart = i + 1;
                    }
                }
            }
        }

        private unsafe void ProcessWord(char* wordBuffer,int wordLength, int documentIndex, Stemmer stemmer, WriteableIndex index)
        {
            // todo to lowercase / uppercase
            if (index.Options.CaseFolding)
            {
                for (int i = 0; i < wordLength; i++)
                {
                    if (wordBuffer[i] <= 90 && wordBuffer[i] >= 65)
                    {
                        wordBuffer[i] += (char)32;
                    }
                }
            }

            // check if stemmed is stop word, do not search complete list if not necessary
            if (index.Options.RemoveStopWords && wordLength <= 4 && IsStopword(wordBuffer))
            {
                return;
            }

            // stem the word
            stemmer.add(wordBuffer, wordLength);

            if (index.Options.DoStemming)
            {
                stemmer.stem();
            }
            else
            {
                stemmer.doNotStem(); // for ease of use we just use the buffer of the stemmer (so we don't have to convert our cahr array again)
            }

            index.AddWord(stemmer.getResultBuffer(),stemmer.getResultLength(), documentIndex);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool IsStopword(char* toCheck)
        {
            long checkValue = *(long*)toCheck;

            for (var i = 0; i < _stopLongs.Length; i++)
            {
                if (_stopLongs[i].value - checkValue == 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static unsafe (long value, int strLength)[] StringToLong(string[] strings)
        {
            var list = new (long,int)[strings.Length];
            for (var i = 0; i < strings.Length; i++)
            {
                var s = strings[i];
                var chars = s.ToCharArray();
                if (chars.Length > 4)
                {
                    throw new Exception("Can not use strings > 4 ");
                }
                fixed (char* c = chars)
                {
                    var lp = (long*) c;
                    list[i] = (*lp, chars.Length);
                }
            }

            return list.OrderBy(tuple => tuple.Item1).ToArray();
        }
    }
}
