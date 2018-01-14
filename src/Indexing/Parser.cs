using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SearchEngine.Indexing
{
    public class Parser
    {
        private byte[] _docStart = Encoding.UTF8.GetBytes("<DOCNO>");
        private byte[] _docNoEnd = Encoding.UTF8.GetBytes("</DOCNO>");
        private byte[] _docStop = Encoding.UTF8.GetBytes("</DOC>");

        public unsafe List<(string id, int from, int length)> ParseFileFast(byte* buffer, long len)
        {
            //var all = File.ReadAllBytes(file);

            var documents = new List<(string, int, int)>();

            var currentId = "";
            var haveDoc = false;
            var insideDocNo = false;

            var correctBufferOffset = 0;

            var currentContentStart = 0;

            //var currentContent = new StringBuilder();

            for (var i = 0; i < len; i++)
            {
                var currentByte = *(buffer + i);

                //
                // search for doc no start
                //
                if (!haveDoc)
                {
                    if (currentByte == _docStart[correctBufferOffset])
                    {
                        correctBufferOffset++;
                        if (correctBufferOffset == _docStart.Length)
                        {
                            haveDoc = true;
                            insideDocNo = true;
                            correctBufferOffset = 0;
                            currentContentStart = i + 1;
                        }
                    }
                    else
                    {
                        correctBufferOffset = 0;
                    }
                }
                else
                {
                    //
                    // search for doc no end
                    //
                    if (insideDocNo)
                    {
                        if (currentByte == _docNoEnd[correctBufferOffset])
                        {
                            correctBufferOffset++;
                            if (correctBufferOffset == _docNoEnd.Length)
                            {
                                insideDocNo = false;
                                correctBufferOffset = 0;

                                currentId = Encoding.ASCII.GetString(buffer + currentContentStart, i - currentContentStart - _docNoEnd.Length + 1).Trim();

                                currentContentStart = i + 1;
                            }
                        }
                        else
                        {
                            correctBufferOffset = 0;
                        }
                    }

                    //
                    // search for stop
                    //
                    else
                    {
                        if (currentByte == _docStop[correctBufferOffset])
                        {
                            correctBufferOffset++;
                            if (correctBufferOffset == _docStop.Length)
                            {
                                haveDoc = false;
                                correctBufferOffset = 0;

                                //var contents = Encoding.UTF8.GetString(all,
                                //                                       currentContentStart,
                                //                                       i - currentContentStart - _docStop.Length);

                                documents.Add((currentId, currentContentStart, i - currentContentStart - _docStop.Length));
                            }
                        }
                        else
                        {
                            correctBufferOffset = 0;
                        }
                    }
                }
            }

            return documents;
        }
    }
}