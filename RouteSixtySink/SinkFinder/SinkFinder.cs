/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using System;
using System.Diagnostics;
using System.Collections.Generic;
using dnlib.DotNet;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;
using RouteSixtySink.Outputters;
using RouteSixtySink.Helpers;
using RouteSixtySink.Discovery;
namespace RouteSixtySink.SinkFinder
{
    public static class SinkFinder
    {
        public static SinkConfig Config { get; set; }

        public static string SinkFile { get; set; }

        public static int RecurseDepth { get; set; }

        public static string CustomSinkString { get; set; }

        public static bool CustomQueryIsRegex { get; set; }

        public static Dictionary<List<string>, Dictionary<string, string>> Sinks { get; set; }

        public static Dictionary<List<string>, Dictionary<string, string>> Run(string methodName, TypeDef type)
        {
            List<string> routeToMethod = new();

            if (CustomSinkString != "")
            {
                SinkConfig customSinkConfig = new();
                customSinkConfig.Sinks = new List<Dictionary<string, string>>();
                Dictionary<string, string> customSink = new();
                customSink["sink"] = CustomSinkString;
                customSink["category"] = "Custom query string";
                customSink["score"] = null;
                customSink["regex"] = CustomQueryIsRegex.ToString();
                customSinkConfig.Sinks.Add(customSink);
                Config = customSinkConfig;
            }
            else
            {
                if (!File.Exists(SinkFile))
                {
                    throw new FileNotFoundException("JSON file containing sinks not found! Place in current directory or specify location using the -f flag.");
                }
                var json = File.ReadAllText(SinkFile);
                Config = JsonConvert.DeserializeObject<SinkConfig>(json);
            }
            SinkFinder.FindRouteToMethodRecursively(methodName, type, ref routeToMethod, 0);
            return Sinks;
        }

        public static bool ShouldExploreMethodCall(string classMethodName)
        {

            return !ClassDiscovery.MethodExploredDict.ContainsKey(classMethodName) || !ClassDiscovery.MethodExploredDict[classMethodName];
        }

        private static void UpdateSinkDictEntry(string methodName, List<string> sinkList, Dictionary<string, string> sink)
        {
            Sinks.TryAdd(sinkList, sink);
            if (ClassDiscovery.MethodSinkDict.ContainsKey(methodName) && !ClassDiscovery.MethodSinkDict[methodName].Item1.Contains(sinkList))
            {
                ClassDiscovery.MethodSinkDict[methodName].Item1.Add(sinkList);
            }
            else
            {
                List<List<string>> newSinkList = new();
                newSinkList.Add(sinkList);
                Tuple<List<List<string>>, Dictionary<string, string>> t = new(newSinkList, sink);
                ClassDiscovery.MethodSinkDict[methodName] = t;
            }
        }

        // Go through each method within the SinkList and update the dictionary for each method
        private static void UpdateSinkDict(List<string> sinkList, Dictionary<string, string> sink)
        {
            for (int i = 0; i < sinkList.Count() - 1; i++) // Don't do the instruction though
            {
                string node = sinkList[i];
                List<string> sinkListAfterCurrentNode = sinkList.GetRange(i, sinkList.Count() - i);
                UpdateSinkDictEntry(node, sinkListAfterCurrentNode, sink);
            }
        }

        private static void ReportSink(string type, string methodName, List<string> routeToSink, Dictionary<string, string> sink = null, dnlib.DotNet.Emit.Instruction instr = null, bool update = false)
        {
            if (instr != null)
            {
                routeToSink.Add(instr.Operand.ToString().Truncate(2000));
            }

            if (update)
            {
                UpdateSinkDict(routeToSink, sink);
            }

            if (sink != null)
            {
                Logger.Sink(ClassDiscovery.GetDLLName(type), type, routeToSink, sink);
            }
            else
            {
                Logger.Sink(ClassDiscovery.GetDLLName(type), type, routeToSink, GetPreviouslyFoundSink(methodName));
            }

            // Remove last element if needed (the sink)
            if (instr != null)
            {
                routeToSink.RemoveAt(routeToSink.Count - 1);
            }
        }

        private static bool DoesMethodLeadToSink(string methodName)
        {
            return ClassDiscovery.MethodSinkDict.ContainsKey(methodName);
        }

        private static Dictionary<string, string> GetPreviouslyFoundSink(string methodName)
        {
            return ClassDiscovery.MethodSinkDict[methodName].Item2;
        }

        private static List<List<string>> GetPreviouslyFoundSinks(string methodName)
        {
            return ClassDiscovery.MethodSinkDict[methodName].Item1;
        }

        public static void PrintList(List<string> ll)
        {
            foreach (string item in ll)
            {
                Logger.Debug("---------" + item);
            }
        }

        private static List<string> GetChainAfterMethod(ref List<string> routeToMethod, List<string> methodToSinks)
        {
            List<string> routeToMethodCopy = routeToMethod;

            // We don't want to concatenate the method that we're investigating to the end list
            string firstNode = methodToSinks[0];
            methodToSinks.RemoveAt(0);

            List<string> routeToSinks = routeToMethodCopy.Concat(methodToSinks).ToList();

            methodToSinks.Insert(0, firstNode);

            return routeToSinks;
        }

        public static bool ReportCachedFindings(string methodName, TypeDef type, ref List<string> routeToMethod)
        {
            if (DoesMethodLeadToSink(methodName))
            {
                List<List<string>> methodToSinks = GetPreviouslyFoundSinks(methodName);

                if (methodToSinks == null) { return false; }

                for (int i = 0; i < methodToSinks.Count; i++)
                {
                    List<string> routeToSink = GetChainAfterMethod(ref routeToMethod, methodToSinks[i]);
                    Dictionary<string, string> sink = Sinks[methodToSinks[i]];
                    ReportSink(type.FullName, methodName, routeToSink, sink);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsMethodSink(TypeDef type, string methodName, ref List<string> routeToMethod, dnlib.DotNet.Emit.Instruction instr)
        {
            bool sinksFound = false;
            foreach (var sink in Config.Sinks)
            {
                // Regex sink matching
                if (sink.ContainsKey("regex") && sink["regex"].ToLower() == "true")
                {
                    Regex regex = new(sink["sink"].ToLower());
                    Match match = regex.Match(instr.Operand.ToString().ToLower());
                    if (match.Success)
                    {
                        sinksFound = true;
                        ReportSink(type.FullName, methodName, routeToMethod, sink, instr, true);
                    }
                }

                // Regular sink matching
                else if (instr.Operand.ToString().ToLower().Contains(sink["sink"].ToLower()))
                {
                    sinksFound = true;
                    ReportSink(type.FullName, methodName, routeToMethod, sink, instr, true);
                }
            }
            return sinksFound;
        }

        public static void FindRouteToMethodRecursively(string methodName, TypeDef type, ref List<string> routeToMethod, int depth)
        {
            // Add the method we are investigating to the end of the list
            routeToMethod.Add(methodName);

            // Don't recurse more than the specified depth
            if (RecurseDepth != -1 && depth >= RecurseDepth) { return; }

            MethodDef method = MethodHelper.GetMethod(type, methodName);
            if (method == null || method.Body == null) { return; }

            if (!ShouldExploreMethodCall(methodName))
            {
                // Check if the method is cached, and if so, just report those sinks
                bool cachedFindingsReported = ReportCachedFindings(methodName, type, ref routeToMethod);
                return;
            }

            // Set the current method as explored!
            ClassDiscovery.MethodExploredDict[methodName] = true;

            foreach (dnlib.DotNet.Emit.Instruction instr in method.Body.Instructions)
            {
                if (instr is null || instr.Operand is null) { continue; }

                // Look at the instruction to determine whether it's a sink. If so, flag it and move on to the next instr
                bool sinksFound = IsMethodSink(type, methodName, ref routeToMethod, instr);
                // Don't recursive if either of these conditions
                if (sinksFound) { continue; }

                // Checks whether the instruction is a call to another method, whether in the current class or another
                // and returns the string encapsulating the `Class::MethodCall()`
                string classMethodName = GetClassMethodCall(instr);

                if (classMethodName != null && classMethodName != methodName) // Don't recurse if we're looking at the same method call again
                {

                    // Get the base classname and try to find it in our discovery dictionary
                    string className = ClassDiscovery.FullNameToClassName(classMethodName);
                    // Recurse!
                    TypeDef nextType = ClassDiscovery.GetType(className);

                    if (nextType != null)
                    {
                        FindRouteToMethodRecursively(classMethodName, nextType, ref routeToMethod, depth + 1);
                        routeToMethod.Remove(routeToMethod.Last());
                    }
                }
                // else { it's not a method call and we can end }

            }
        }

        public static string GetClassMethodCall(dnlib.DotNet.Emit.Instruction instr)
        {
            string[] callOpcodes = { "call", "calli", "callvirt", "newobj" };
            return callOpcodes.Contains(instr.OpCode.ToString()) ? instr.Operand.ToString() : null;
        }
    }

    public class SinkConfig
    {
        public List<Dictionary<string, string>> Sinks { get; set; }

    }
}