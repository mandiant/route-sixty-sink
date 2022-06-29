/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using CommandLine;
using System;
using System.Linq;
using System.Collections.Generic;
using RouteSixtySink.Helpers;
using RouteSixtySink.Core;

namespace RouteSixtySink.Commands
{
    [Verb("aspnetcore", HelpText = "Runs RouteFinder against a project using the ASP.NET Core MVC framework")]
    class RunRFASPNETCommand : Options,ICommand
    {
        [Option('d', "dependencies", Default = "", Required = false, HelpText = "DLL or directory of DLLs to be used as dependencies for sinkfinding.")]
        public string Dependencies { get; set; }

        [Option('i', "input", Default = "", Required = true, HelpText = "DLL/EXE or Directory of DLLs/Exes to parse.")]
        public string Input { get; set; }

        [Option("routerunner", Default = false, Required = false, HelpText = "Invoke RouteRunner to invoke limited validation of identified routes.")]
        public bool EnableRouteRunner { get; set; }

        [Option("do-delete", Default = false, Required = false, HelpText = "Determine if RouteRunner should process requests with the DELETE HTTP verb.")]
        public bool DoDelete { get; set; }

        [Option("no-conventional", Default = false, Required = false, HelpText = "Determine if RouteFinder should use experimental conventional routing parser.")]
        public bool DisableConventionalRouting { get; set; }

        [Option('e', "endpoint", Default = "", Required = false, HelpText = "Service endpoint for RouteRunner to target.")]
        public string ServiceEndpoint { get; set; }

        [Option("failcodes", Separator = ',', Required = false, HelpText = "Fail codes for route runner to identify invalid routes.")]
        public IEnumerable<int> FailStatusCodes { get; set; }

        public void Execute()
        {
            ExecuteGlobals();
            RouteFinderCore.DoConventionalRouting = DisableConventionalRouting ? false : true;

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

            RouteFinderCore.RunRFASPNETCore(AssembliesToParse);

            if (EnableRouteRunner)
            {
                if (DoDelete)
                {
                    RouteRunner.RouteRunner.DoDelete = true;
                }
                if (!String.IsNullOrEmpty(ServiceEndpoint))
                {
                    RouteRunner.RouteRunner.ServiceEndpoint = ServiceEndpoint;
                }
                // This is breaking route parsing when provided more than two intgers
                // if (FailStatusCodes.Any())
                // {
                //     Console.WriteLine(String.Join(",",FailStatusCodes));
                //     RouteRunner.RouteRunner.FailStatusCodes = FailStatusCodes.ToList();
                // }
                RouteRunner.RouteRunner.RunRouteRunner();
            }
        }
    }
}