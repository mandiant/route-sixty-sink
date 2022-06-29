/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using System;
using System.Collections.Generic;
using static System.Console;
using dnlib.DotNet;
using RouteSixtySink.Outputters;
using RouteSixtySink.Helpers;
using System.Linq;
namespace RouteSixtySink.Discovery
{
    public static class ClassDiscovery
    {

        private static Dictionary<string, string> Results { get; set; }
        public static Dictionary<string, Tuple<string, TypeDef>> ClassNameToTypeDef { get; set; } // Dictionary<ClassName, (DLLName the class is from, TypeDef pointing to type)>
        public static Dictionary<string, bool> MethodExploredDict { get; set; } // Dictionary<ClassMethod Name, hasBeenExplored?>
        public static Dictionary<string, Tuple<List<List<string>>, Dictionary<string, string>>> MethodSinkDict { get; set; } // Dictionary<ClassMethod Name, routeToSink linked list>
        public static HashSet<string> MissingClasses { get; set; }

        public static string GetClassName(string classMethod)
        {
            return classMethod.Split("::")[0];
        }

        public static TypeDef FindClassInAssembly(string assemblyPath, string classname)
        {
            ModuleDefMD module = AssemblyHelper.LoadAssembly(assemblyPath);
            var types = module.GetTypes();
            TypeDef type = types.FirstOrDefault(x => x.Name == classname);

            return type;
        }

        public static string GetMethodName(string classMethod)
        {
            return classMethod.Split("::")[1];
        }

        public static string GetDLLName(string className)
        {
            string ret;
            try
            {
                ret = ClassNameToTypeDef[className].Item1;
            }
            catch
            {
                return null;
            }

            return ret;
        }

        public static string FullNameToClassName(string fullname)
        {
            return GetClassName(fullname.Split(' ')[1]);
        }

        public static TypeDef GetType(string className)
        {
            TypeDef ret;
            try
            {
                ret = ClassNameToTypeDef[className].Item2;
            }
            catch
            {
                MissingClasses.Add(className);
                return null;
            }

            return ret;
        }

        public static void Discover(List<string> Assemblies)
        {
            List<string> assembliesCopy = new();
            assembliesCopy.AddRange(Assemblies);
            ClassNameToTypeDef = new Dictionary<string, Tuple<string, TypeDef>>();
            MethodExploredDict = new Dictionary<string, bool>();
            MethodSinkDict = new Dictionary<string, Tuple<List<List<string>>, Dictionary<string, string>>>();
            MissingClasses = new HashSet<string>();
            SinkFinder.SinkFinder.Sinks = new Dictionary<List<string>, Dictionary<string, string>>();
            WriteLine("\n🔎  Discovering classes within provided DLLs...");
            // Also add common dependency assemblies that are specified in the Dependencies directory so
            // they can be followed if/when they are used

            List<string> dependencyAssemblies = new List<string>();

            try
            {
                dependencyAssemblies = AssemblyHelper.GetAssemblies("../Dependencies/");
                assembliesCopy.AddRange(dependencyAssemblies);
            }
            catch
            {

            }
            PopulateMaps(assembliesCopy);
            WriteLine("\n🛣️   Traveling down Route Sixty-Sink!\n");
        }

        private static void PopulateMaps(List<string> Assemblies)
        {
            foreach (var assembly in Assemblies)
            {
                // Begin parsing DLL
                ModuleContext modCtx = ModuleDef.CreateModuleContext();
                try
                {
                    ModuleDefMD module = ModuleDefMD.Load(assembly, modCtx);

                    foreach (TypeDef type in module.GetTypes())
                    {
                        if (type.FullName == "<Module>") { continue; }
                        try
                        {
                            ClassNameToTypeDef.Add(type.FullName, new Tuple<string, TypeDef>(assembly, type));
                        }
                        catch (Exception e)
                        {
                            if (e.GetType().FullName == "System.ArgumentException") { continue; }
                            Logger.Debug("Failed to add Type \"" + type.FullName + "\" to dictionary." + " (" + e + ")");
                        }
                    }
                }
                catch (Exception)
                {
                    
                }
            }
        }

        private static bool IsTargetCall(dnlib.DotNet.Emit.Instruction instr, string targetMethodName)
        {
            bool targetCall = false;
            if (instr.Operand.GetPropertyValue("Name").ToString().ToLower() == targetMethodName.ToLower())
            {
                Logger.Debug(String.Format("🔍 Found target call {0}!", targetMethodName));
                targetCall = true;
            }

            return targetCall;
        }

        internal static List<List<dnlib.DotNet.Emit.Instruction>> RunRecursiveFindCalls(string methodName, TypeDef type, string targetMethodName)
        {
            List<string> routeToMethod = new();
            List<List<dnlib.DotNet.Emit.Instruction>> instructionsLists = new();

            FindCalls(methodName, type, ref routeToMethod, 0, ref instructionsLists, targetMethodName);

            return instructionsLists;
        }

        private static void FindCalls(string methodName, TypeDef type, ref List<string> routeToMethod, int depth, ref List<List<dnlib.DotNet.Emit.Instruction>> instructionsLists, string targetMethodName)
        {

            // Add the method we are investigating to the end of the list

            routeToMethod.Add(methodName);

            // Don't recurse more than the specified depth

            if (SinkFinder.SinkFinder.RecurseDepth != -1 && depth >= SinkFinder.SinkFinder.RecurseDepth) { return; }

            MethodDef method = MethodHelper.GetMethod(type, methodName);

            if (method == null || method.Body == null) { return; }

            if (!SinkFinder.SinkFinder.ShouldExploreMethodCall(methodName))
            {
                // Check if the method is cached, and if so, return and continue onto the next method
                bool cachedFindingsReported = SinkFinder.SinkFinder.ReportCachedFindings(methodName, type, ref routeToMethod);
                return;
            }

            // Set the current method as explored!
            ClassDiscovery.MethodExploredDict[methodName] = true;

            // Begin looking for Map call

            foreach (dnlib.DotNet.Emit.Instruction instr in method.Body.Instructions)
            {
                if (instr is null || instr.Operand is null) { continue; }

                bool isTargetCallReturn = IsTargetCall(instr, targetMethodName);

                // If targetMethod is found, add to list of lists
                if (isTargetCallReturn)
                {
                    instructionsLists.Add(method.Body.Instructions.ToList());
                    continue;
                }

                // Checks whether the instruction is a call to another method, whether in the current class or another
                // and returns the string encapsulating the `Class::MethodCall()`
                string classMethodName = SinkFinder.SinkFinder.GetClassMethodCall(instr);

                if (classMethodName != null && classMethodName != methodName) // Don't recurse if we're looking at the same method call again
                {

                    // Get the base classname and try to find it in our discovery dictionary
                    string className = ClassDiscovery.FullNameToClassName(classMethodName);
                    // Recurse!
                    TypeDef nextType = ClassDiscovery.GetType(className);

                    if (nextType != null)
                    {
                        FindCalls(classMethodName, nextType, ref routeToMethod, depth + 1, ref instructionsLists, targetMethodName);
                        routeToMethod.Remove(routeToMethod.Last());
                    }
                }
            }
        }
    }
}