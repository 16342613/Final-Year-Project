using System.Collections;
using System.IO;
using System.Collections.Generic;

namespace InputOutput
{
    public class IO_Stream
    {
        private string filePath;

        public IO_Stream(string filePath)
        {
            this.filePath = filePath;
        }

        public void WriteLinesToFile(List<string> linesToWrite, bool clearFile = false)
        {
            if (clearFile == true)
            {
                File.WriteAllText(filePath, "");
            }

            File.WriteAllLines(filePath, linesToWrite);
        }
    }
}
