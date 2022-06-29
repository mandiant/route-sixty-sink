/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using CommandLine;
using System.Collections.Generic;
using System.IO;
using System;
using RouteSixtySink.Helpers;
using RouteSixtySink.RouteFinder;
namespace RouteSixtySink.Commands
{
    [Verb("aspnetpages", HelpText = "Runs RouteFinder against a project using the ASP.NET Pages MVC framework")]
    class RunRFPagesCommand : Options,ICommand
    {
        [Option('d', "dependencies", Default = "", Required = true, HelpText = "Directory containing assemblies to parse.")]
        public string Dependencies { get; set; }

        [Option('i', "input", Default = "", Required = true, HelpText = "File or directory containing ASPX, ASHX, or ASMX page files. For apps that don't use MVC. Used in conjunction with -d.")]
        public string Input { get; set; }

        public void Execute()
        {
            ExecuteGlobals();
            List<string> Assemblies = AssemblyHelper.GetAssemblies(Dependencies);

            if (!Input.Equals(""))
            {
                List<string> Pages = new() { };

                FileAttributes attr = File.GetAttributes(Input);

                if (attr.HasFlag(FileAttributes.Directory))
                {
                    var pages = Directory.GetFiles(Input, "*.as*x", SearchOption.AllDirectories);

                    foreach (string page in pages)
                    {
                        Pages.Add(page);
                    }
                }
                else
                {
                    Pages.Add(Input);
                }

                RouteFinderPages.Run(Assemblies, Pages, Dependencies, Input);
                return;
            }
            else
            {
                throw new ArgumentException("A page directory must be specified! (-p)");
            }
        }
    }
}