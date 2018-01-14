using System;

namespace SearchEngine.Indexing
{
    [Serializable]
    public class QueryOptions
    {
        public bool UseHeadline { get; set; }

        public bool UseDescription { get; set; }

        public bool UseNarrative { get; set; }

        public override string ToString()
        {
            return "QueryOptions:\tUseHeadline = " + UseHeadline +
                "\n\t\tUseDescription = " + UseDescription +
                "\n\t\tUseNarrative = " + UseNarrative;
        }

        /// <summary>
        /// Parses an options object from commandline args
        /// </summary>
        /// <param name="args">Example: "QueryOptions(UseHeadline,UseDescription,UseNarrative)" or "QueryOptions(UseHeadline)" etc... </param>
        /// <returns></returns>
        public static QueryOptions Parse(string args)
        {
            var raw = args.Substring(13, args.Length - 1 - 13); // remove QueryOptions( ..  and ... )
            var options = raw.Split(',');
            var query = new QueryOptions();

            foreach(var opt in options)
            {
                switch (opt)
                {
                    case "UseHeadline":
                        query.UseHeadline = true;
                        break;
                    case "UseDescription":
                        query.UseDescription = true;
                        break;
                    case "UseNarrative":
                        query.UseNarrative = true;
                        break;
                }
            }

            return query;
        }
    }
}
