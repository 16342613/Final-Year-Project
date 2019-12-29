using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
                if(printIndex == true)
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

        public static void PrintListList(List<List<object>> toPrint, bool printInTextFile = false)
        {
            
        }

        public static void PrintDictionary(Dictionary<object, object> toPrint, bool printInTextFile = false)
        {
            // TODO
        }
    }
}

