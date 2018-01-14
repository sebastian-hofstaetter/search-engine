using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SearchEngine.Indexing;

namespace SearchEngine.Query
{
    public interface IScorer
    {
        List<(string documents, float score)> Score(List<(string,int)> keywords);
    }
}
