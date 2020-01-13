using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using InputOutput;
using Global;

namespace Debugger
{
    public class DebugHelper
    {
        public DebugHelper()
        {
            // Empty constructor
        }

        public static void PrintList(List<object> toPrint, bool printIndex = false)
        {
            string printLine = "";

            for (int i = 0; i < toPrint.Count; i++)
            {
                if (printIndex == true)
                {
                    printLine += "<" + i + "> ";
                }

                printLine += toPrint[i] + " ; ";
            }

            Debug.Log(printLine);
        }

        public static void PrintArray(object[] toPrint, bool printIndex = false)
        {
            string printLine = "";

            for (int i = 0; i < toPrint.Length; i++)
            {
                if (printIndex == true)
                {
                    printLine += "<" + i + "> ";
                }

                printLine += toPrint[i] + " ; ";
            }

            Debug.Log(printLine);
        }

        public static void PrintListList(List<List<Vector3>> toPrint, bool printIndex = false, bool printInLog = false, bool clearLog = true)
        {
            if (printInLog == true)
            {
                IO_Stream io = new IO_Stream(Constants.logFile);
                List<string> linesToWrite = new List<string>();

                for (int i = 0; i < toPrint.Count; i++)
                {
                    linesToWrite.Add("");

                    if (printIndex == true)
                    {
                        linesToWrite[i] += " <" + i + "> ";
                    }

                    for (int j = 0; j < toPrint[i].Count; j++)
                    {
                        if (printIndex == true)
                        {
                            linesToWrite[i] += " <" + j + "> ";

                        }

                        linesToWrite[i] += toPrint[i][j] + " ; ";
                    }
                }

                linesToWrite.Add("\n\n");
                io.WriteLinesToFile(linesToWrite, clearLog);
            }
            else
            {
                // TODO
            }

        }

        public static void PrintConnectedVertices(Dictionary<Vector3, List<Vector3>> toPrint, bool printInLog = false, bool clearLog = true)
        {
            if (printInLog == true)
            {
                IO_Stream io = new IO_Stream(Constants.logFile);
                List<string> linesToWrite = new List<string>();
                linesToWrite.Add("\n\n");

                for (int i = 0; i < toPrint.Keys.Count; i++)
                {
                    linesToWrite.Add("");
                    linesToWrite[i] += "<< " + toPrint.ElementAt(i).Key + " >>";

                    for (int j = 0; j < toPrint.ElementAt(i).Value.Count; j++)
                    {
                        if (printInLog == true)
                        {
                            linesToWrite[i] += toPrint.ElementAt(i).Value[j] + " ; ";
                        }
                    }
                }

                linesToWrite.Add("\n\n");
                io.WriteLinesToFile(linesToWrite, clearLog);
            }
            else
            {
                // TODO
            }
        }
    }
}

