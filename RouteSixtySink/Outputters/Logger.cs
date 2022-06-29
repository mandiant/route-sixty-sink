/*Copyright (C) 2022 Mandiant, Inc. All Rights Reserved.*/

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace RouteSixtySink.Outputters
{
    public static class Logger
    {
        public static bool ConsoleEnabled = true;
        public static string Verbosity = "VVEW";

        public static void Sink(string assembly, string type, List<string> callGraph, Dictionary<string, string> sink)
        {
            int tabStart = 0;
            StackTrace st = new StackTrace(1, true);
            IEnumerable<StackFrame> stFrames = st.GetFrames();
            var matches = stFrames.Where(x => x.GetMethod().Name.Contains("AddRouteDataToDictionary"));
            if (matches.Any()){
                tabStart = 3;
            }

            Tuple<string,string,ConsoleColor> m = new Tuple<string,string,ConsoleColor>("","",Console.ForegroundColor);
            for (int i = 0; i < callGraph.Count - 1; i++)
            {
                string node = callGraph[i];
                m = FormatLoggerMessage(tabStart + 2, ConsoleColor.Red, "Method", node);
                Console.ForegroundColor = m.Item3;
                Console.Write(m.Item1);
                Console.ResetColor();
                Console.Write(m.Item2);
            }
            m = FormatLoggerMessage(tabStart + 3, ConsoleColor.Magenta, "Sink", callGraph[callGraph.Count - 1]);
            Console.ForegroundColor = m.Item3;
            Console.Write(m.Item1);
            Console.ResetColor();
            Console.Write(m.Item2);
            m = FormatLoggerMessage(tabStart + 4, ConsoleColor.Magenta, "", sink["category"], sink["sink"]);
            Console.ForegroundColor = m.Item3;
            Console.Write(m.Item1);
            Console.ResetColor();
            Console.Write(m.Item2);
        }

        public static void Verbose(string type, string message, string extra = "", string extra2 = "", string extra3 = "")
        {

            Tuple<string,string,ConsoleColor> m = new Tuple<string,string,ConsoleColor>("","",Console.ForegroundColor);

            if (ConsoleEnabled || Writer.LogOutputToFile)
            {
                var types = new Dictionary<string, Action>()
                    {
                        { "c", () => {m = FormatLoggerMessage(1,ConsoleColor.DarkGreen,"Controller",message,extra);} },
                        { "page", () => {m = FormatLoggerMessage(0,ConsoleColor.DarkGreen,"Page",message,extra);} },
                        { "p", () => {m = FormatLoggerMessage(2,ConsoleColor.Yellow,"Prefix",message,extra);} },
                        { "assembly", () => {m = FormatLoggerMessage(0,ConsoleColor.Yellow,"Assembly",message,extra);} },
                        { "assembly2", () => {m = FormatLoggerMessage(1,ConsoleColor.Yellow,"Assembly",message,extra);} },
                        { "class", () => {m = FormatLoggerMessage(2,ConsoleColor.Red,"Class",message,extra);} },
                        { "r", () => {m = FormatLoggerMessage(3,ConsoleColor.Red,"Route",message,extra);} },
                        { "s", () => {m = FormatLoggerMessage(4,ConsoleColor.DarkBlue,"Sink",message,extra);} },
                        { "info", () => {m = FormatLoggerMessage(0,ConsoleColor.Blue,"Info",message,extra,extra2);} },

                    };

                var typesVV = new Dictionary<string, Action>()
                    {
                        { "ct", () => {m = FormatLoggerMessage(6,ConsoleColor.Magenta,"Content-Type",message,extra);} },
                        { "ro", () => {m = FormatLoggerMessage(2,ConsoleColor.DarkCyan,extra,message);} },
                        { "roa", () => {m = FormatLoggerMessage(3,ConsoleColor.DarkCyan,extra,message);} },
                        { "act", () => {m = FormatLoggerMessage(4,ConsoleColor.Blue,"Action",message,extra);} },
                    };

                // Format message by setting message value "m" based on type specified in the Logger() call

                if (types.ContainsKey(type) && Verbosity.ToLower().Contains("v"))
                {
                    types[type]();
                }
                if (typesVV.ContainsKey(type) && Verbosity.ToLower().Contains("vv"))
                {
                    typesVV[type]();
                }
            }

            bool enableLogging = Verbosity.ToLower().Contains("v") && message != null;

            if (ConsoleEnabled.Equals(true) && enableLogging)
            {
                Console.ForegroundColor = m.Item3;
                Console.Write(m.Item1);
                Console.ResetColor();
                Console.Write(m.Item2);
            }
            if (Writer.LogOutputToFile.Equals(true) && enableLogging)
            {
                Writer.AppendFile(m.Item1);
                Writer.AppendFile(m.Item2);
            }
        }

        public static void Error(string message, string extra = "", string extra2 = "", string extra3 = "")
        {
            Tuple<string,string,ConsoleColor> m = FormatLoggerMessage(0, ConsoleColor.Red, "Error", message, extra, extra2);
            if (ConsoleEnabled.Equals(true) && Verbosity.ToLower().Contains("e"))
            {
                Console.ForegroundColor = m.Item3;
                Console.Write(m.Item1);
                Console.ResetColor();
                Console.Write(m.Item2);
            }
            if (Writer.LogOutputToFile.Equals(true))
            {
                Writer.AppendFile(m.Item1);
                Writer.AppendFile(m.Item2);
            }
        }

        public static void Debug(string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            string calleeInfo = memberName + ":" + sourceLineNumber;
            Tuple<string,string,ConsoleColor> m = FormatLoggerMessage(0, ConsoleColor.Red, "Debug", message, calleeInfo);
            if (ConsoleEnabled.Equals(true) && Verbosity.ToLower().Contains("d"))
            {
                Console.ForegroundColor = m.Item3;
                Console.Write(m.Item1);
                Console.ResetColor();
                Console.Write(m.Item2);
            }
            if (Writer.LogOutputToFile.Equals(true))
            {
                Writer.AppendFile(m.Item1);
                Writer.AppendFile(m.Item2);
            }
        }

        public static void Warning(string type, string message, string extra = "")
        {
            Tuple<string,string,ConsoleColor> m = new Tuple<string,string,ConsoleColor>("","",Console.ForegroundColor);
            var types = new Dictionary<string, Action>(){
                { "actionmisconfig", () => {m = FormatLoggerMessage(2,ConsoleColor.Yellow,"Potential Misconfiguration",message,extra);} },
                { "controllermisconfig", () => {m = FormatLoggerMessage(1,ConsoleColor.Yellow,"Potential Misconfiguration",message,extra);} },
                };

            if (types.ContainsKey(type) && Verbosity.ToLower().Contains("w"))
            {
                types[type]();
            }


            if (ConsoleEnabled.Equals(true))
            {
                Console.ForegroundColor = m.Item3;
                Console.Write(m.Item1);
                Console.ResetColor();
                Console.Write(m.Item2);
            }
            if (Writer.LogOutputToFile.Equals(true))
            {
                Writer.AppendFile(m.Item1);
                Writer.AppendFile(m.Item2);
            }
        }
        public static Tuple<string, string, ConsoleColor> FormatLoggerMessage(int tabNum, ConsoleColor color, string type, string message, string extra = "", string extra2 = "", string extra3 = "")
        {

            string tabs = new('\t', tabNum);

            extra = String.IsNullOrEmpty(extra) ? "" : String.Format("[{0}] ", extra);
            extra2 = String.IsNullOrEmpty(extra2) ? "" : String.Format("[{0}] ", extra2);
            extra3 = String.IsNullOrEmpty(extra3) ? "" : String.Format("[{0}] ", extra3);
            string messageHeader = String.Format("{0}{1} [+] {2}", tabs, type, extra);
            string messageBody = String.Format("{0}{1}{2}\n", extra2, extra3, message);

            return new Tuple<string,string,ConsoleColor>(messageHeader, messageBody, color);
        }
    }
}