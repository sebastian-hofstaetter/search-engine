using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.Indexing
{
    public class CharComparer : IEqualityComparer<char[]>
    {
        public bool Equals(char[] x, char[] y)
        {
            return Equals(x, y, y.Length);
        }

        public bool Equals(char[] x, char[] buffer, int bufferLength)
        {
            if(x.Length != bufferLength)
            {
                return false;
            }

            for (int i = 0; i < bufferLength; i++)
            {
                if(x[i] != buffer[i])
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(char[] obj)
        {
            return GetHashCode(obj, obj.Length);
        }

        public int GetHashCode(char[] buffer, int length)
        {
            // from http://stackoverflow.com/a/425184
            unchecked
            {
                var result = 0;
                for (var i = 0; i < length; i++)
                {
                    result = (result * 31) ^ buffer[i];
                }
                return result;
            }
        }
    }
}
