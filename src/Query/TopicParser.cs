using System;
using System.Collections.Generic;
using System.IO;
using SearchEngine.Indexing;
using SearchEngine.Util;

namespace SearchEngine.Query
{
    public class TopicParser
    {
        private static List<(string,int)> currentKeywords = new List<(string,int)>();
        private static IndexOptions indexOptions;

        private static string[] _stopWords =
        {
            "a","an","and","also","all","are","as","at","be","been","by","but","for","from",
            "have","has","had","he","in","is","it","its","more","new","not",
            "of","on","page","part","that","the","this",
            "to","s","was","were","will","with","1","2","3"
        };

        public static List<(int topic, List<(string, int)> keywords)> ParseTopics(string filePath,QueryOptions queryOptions,IndexOptions iOptions)
        {
            indexOptions = iOptions;
            var result = new List<(int, List<(string, int)>)>();

            var fileContents = File.ReadAllLines(filePath);

            var currentID = 0;
            string last = "";
            
            foreach(var line in fileContents)
            {
                // start of topic
                if (line.StartsWith("<num>"))
                {
                    currentID = int.Parse(line.Substring(14));
                }
                else if (line.StartsWith("<title>") && queryOptions.UseHeadline)
                {
                    ParseLine(line.Substring(8));
                }
                else if (line.StartsWith("<desc>"))
                {
                    last = "desc";
                }
                else if (line.StartsWith("<narr>"))
                {
                    last = "narr";
                }

                // end of topic
                else if (line.StartsWith("</top>"))
                {
                    result.Add((currentID, currentKeywords));
                    currentID = 0;
                    currentKeywords = new List<(string, int)>();
                }
                else
                {
                    if(last.Equals("desc") && queryOptions.UseDescription)
                    {
                        ParseLine(line);
                    }
                    else if(last.Equals("narr") && queryOptions.UseNarrative)
                    {
                        ParseLine(line);
                    }
                }
            }

            return result;
        }

        private static void ParseLine(String line)
        {
            foreach (var word in line.Split(',', ' ', '.', '(', ')', ';', ':', '?','/'))
            {
                if (!string.IsNullOrWhiteSpace(word) && !(word[0] == '<'))
                {
                    String result = ProcessWord(word);
                    if (!result.Equals(""))
                    {
                        var index = currentKeywords.FindIndex((e) => e.Item1 == result);
                        if(index > -1)
                        {
                            currentKeywords[index] = (currentKeywords[index].Item1, currentKeywords[index].Item2 + 1);
                        }
                        else
                        {
                            currentKeywords.Add((result,1));
                        }
                    }
                }
            }
        }

        private static String ProcessWord(String word)
        {
            String workingCopy = word;
            // todo to lowercase / uppercase
            if (indexOptions.CaseFolding)
            {
                workingCopy = workingCopy.ToLower();
            }
            // check if stemmed is stop word, do not search complete list if not necessary
            if (indexOptions.RemoveStopWords && (workingCopy.Length <= 4) && IsStopword(workingCopy))
            {
                return "";
            }

            // stem the word
            var stemmer = new Stemmer();
            char[] temp = workingCopy.ToCharArray();
            stemmer.add(temp,temp.Length);
            if (indexOptions.DoStemming)
            {
                stemmer.stem();
            }
            else
            {
                stemmer.doNotStem();
            }
            return stemmer.ToString();
        }

        private static bool IsStopword(String toCheck)
        {
            foreach(var temp in _stopWords)
            {
                if (toCheck.Equals(temp))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
