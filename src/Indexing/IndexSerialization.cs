using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.Indexing
{
    public static class IndexSerialization
    {
        public static void SerializeIndexToDisk(WriteableIndex index, string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Create))
            {
                index.Serialize(stream);
            }
        }

        public static ReadableIndex DeserializeFromDisk(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open))
            {
                return ReadableIndex.Deserialize(stream);
            }
        }
    }
}
