
/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using CommandLine;
using System.Collections.Generic;
using dnlib.DotNet;
using RouteSixtySink.Outputters;
using RouteSixtySink.Discovery;
using RouteSixtySink.Helpers;

namespace RouteSixtySink.Commands
{
    [Verb("sinkfinder", HelpText = "Runs SinkFinder without route finding capability")]
    class RunSinkFinderCommand : Options,ICommand
    {
        [Option('d', "dependencies", Default = "", Required = false, HelpText = "DLL or directory of DLLs to be used as dependencies for sinkfinding.")]
        public string Dependencies { get; set; }

        [Option('i', "input", Default = "", Required = true, HelpText = "PE (EXE or DLL) or directory of PEs in which to search for sinks.")]
        public string Input { get; set; }

        public void Execute()
        {
            ExecuteGlobals();
            // Parse the starting assembly or directory of assemblies
            List<string> StartingAssemblies = AssemblyHelper.GetAssemblies(Input, true);
            List<string> Assemblies = new();

            if (!Dependencies.Equals(""))
            {
                // Parse the assembly or directory of assemblies
                Assemblies = AssemblyHelper.GetAssemblies(Dependencies);
            }

            List<string> AssembliesToParse = new();
            List<string> AssembliesToDiscover = new();

            if (Assemblies.Count != 0)
            {
                AssembliesToDiscover.AddRange(StartingAssemblies);
                AssembliesToDiscover.AddRange(Assemblies);
            }
            else
            {
                AssembliesToDiscover = StartingAssemblies;
            }

            AssembliesToParse = StartingAssemblies;

            // Discover classes from assemblies
            ClassDiscovery.Discover(AssembliesToDiscover);

            foreach (var assembly in AssembliesToParse)
            {
                Logger.Verbose("assembly", AssemblyHelper.CleanDLLName(assembly, Input));
                // Begin parsing DLL
                ModuleContext modCtx = ModuleDef.CreateModuleContext();
                ModuleDefMD module = null;
                try
                {
                    module = ModuleDefMD.Load(assembly, modCtx);
                }
                catch
                {
                    continue;
                }
                foreach (TypeDef type in module.GetTypes())
                {

                    foreach (MethodDef method in type.Methods)
                    {
                        Dictionary<List<string>, Dictionary<string, string>> sinks = new();

                        sinks = SinkFinder.SinkFinder.Run(method.FullName, type);
                    }
                }
            }
        }
    }
}
