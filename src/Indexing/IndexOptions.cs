using System;

namespace SearchEngine.Indexing
{
    [Serializable]
    public class IndexOptions
    {
        public bool CaseFolding { get; set; }

        public bool RemoveStopWords { get; set; }

        public bool DoStemming { get; set; }

        public override string ToString()
        {
            return "IndexOptions:\tCaseFolding = " + CaseFolding +
                "\n\t\tRemoveStopWords = " + RemoveStopWords +
                "\n\t\tDoStemming = "+ DoStemming;
        }

        /// <summary>
        /// Parses an options object from commandline args
        /// </summary>
        /// <param name="args">Example: "IndexOptions(CaseFolding,RemoveStopWords,DoStemming)" or "IndexOptions(CaseFolding)" etc... </param>
        /// <returns></returns>
        public static IndexOptions Parse(string args)
        {
            var raw = args.Substring(13, args.Length - 1 - 13); // remove IndexOptions( ..  and ... )
            var options = raw.Split(',');
            var index = new IndexOptions();

            foreach(var opt in options)
            {
                switch (opt)
                {
                    case "CaseFolding":
                        index.CaseFolding = true;
                        break;
                    case "RemoveStopWords":
                        index.RemoveStopWords = true;
                        break;
                    case "DoStemming":
                        index.DoStemming = true;
                        break;
                }
            }

            return index;
        }
    }
}
