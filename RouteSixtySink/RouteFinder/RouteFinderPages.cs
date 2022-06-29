/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using System;
using System.IO;
using System.Collections.Generic;
using dnlib.DotNet;
using System.Text.RegularExpressions;
using RouteSixtySink.Outputters;
using RouteSixtySink.Models;
using RouteSixtySink.Discovery;
using RouteSixtySink.Helpers;

namespace RouteSixtySink.RouteFinder
{
    public static class RouteFinderPages
    {
        public static List<string> Pages { get; set; }
        public static List<TypeDef> Types { get; set; }
        public static List<Route> Routes { get; set; }
        public static string DLLDirectory { get; set; }
        public static string PagesDirectory { get; set; }

        public static void Run(List<string> assemblies, List<string> pages, string dllDirectory, string pagesDirectory)
        {
            // Perform class discovery
            ClassDiscovery.Discover(assemblies);

            Pages = pages;
            DLLDirectory = dllDirectory;
            PagesDirectory = pagesDirectory;
            Routes = new List<Route> { };
            Types = new List<TypeDef>();

            foreach (string page in Pages)
            {
                string pageRoute = GetPageRoute(page);
                Logger.Verbose("page", pageRoute);

                string pageClass = GetPageClass(page);

                // Get name of DLL for the class we're looking at and prettify it
                string pageDLL = ClassDiscovery.GetDLLName(pageClass);
                string dllName = pageDLL == null ? "NOT FOUND" : AssemblyHelper.CleanDLLName(pageDLL, DLLDirectory);
                Logger.Verbose("assembly2", dllName);

                FindSinks(pageClass);
            }
            Writer.WriteCSV(Routes);
        }
        private static string GetPageClass(string page)
        {
            string pageContents = System.IO.File.ReadAllText(page);

            // Find the class that implements the page we're currently looking at
            Regex regex = new("[iI]nherits=\"(.*?)\"");
            var result = regex.Match(pageContents);

            if (result.Length == 0)
            {
                regex = new Regex("[cC]lass=\"(.*?)\"");
                result = regex.Match(pageContents);
            }

            if (result.Length == 0)
            {
                /*
                * If neither regex matches, the page does not inherit from a class. If
                * this is the case, we can just show the page as a route and move on.
                */
                return null;
            }
            else
            {
                return result.Groups[1].ToString();
            }
        }

        private static string GetPageRoute(string page)
        {
            // Get rid of the unimportant directory structure and output page
            string pageRoute = page.Replace(PagesDirectory, "");
            pageRoute = pageRoute.Trim('/');

            if (String.IsNullOrEmpty(pageRoute))
            {
                pageRoute = Path.GetFileName(page);
            }

            return pageRoute;
        }

        private static void FindSinks(string pageClass)
        {
            TypeDef type = ClassDiscovery.GetType(pageClass);

            if (type == null || !type.HasMethods) { return; }

            foreach (MethodDef method in type.Methods)
            {
                SinkFinder.SinkFinder.Run(method.FullName, type);
            }
        }
    }
}