/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using CommandLine;
using System;
using System.Collections.Generic;
using dnlib.DotNet;
using RouteSixtySink.Outputters;
using RouteSixtySink.Discovery;
using RouteSixtySink.Helpers;
namespace RouteSixtySink.Commands
{
    [Verb("discovery", HelpText = "Runs discovery on provided DLLs")]
    class RunDiscoveryCommand : Options,ICommand
    {
        [Option('i', "input", Default = "", Required = true, HelpText = "DLL or directory of DLLs with which to discover classes.")]
        public string Input { get; set; }

        [Option('m', "methods", Default = false, Required = false, HelpText = "Discover methods, as well as classes.")]
        public bool Methods { get; set; }

        [Option('p', "public", Default = false, Required = false, HelpText = "Set to find only publicly-accessible methods.")]
        public bool Public { get; set; }

        public void Execute()
        {
            ExecuteGlobals();
            // Parse the assembly or directory of assemblies

            List<string> Assemblies = AssemblyHelper.GetAssemblies(Input);

            ClassDiscovery.Discover(Assemblies);

            foreach (string className in ClassDiscovery.ClassNameToTypeDef.Keys)
            {
                Console.WriteLine("Class: " + className + "\t\t\t\t" + ClassDiscovery.GetDLLName(className));
                if (Methods)
                {
                    TypeDef type = ClassDiscovery.GetType(className);
                    if (Public)
                    {
                        MethodHelper.GetPublicMethods(type);
                    }
                    else
                    {
                        MethodHelper.GetMethods(type);
                    }
                }
            }
        }
    }
}