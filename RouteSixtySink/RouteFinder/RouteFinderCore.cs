/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using RouteSixtySink.Models;
using RouteSixtySink.Outputters;
using System.Text.RegularExpressions;
using RouteSixtySink.Discovery;
using System.Linq;
using static RouteSixtySink.Core.RouteFinderCoreAttributeRouting;
using static RouteSixtySink.Core.RouteFinderConventionalRouting;
using RouteSixtySink.Helpers;
namespace RouteSixtySink.Core
{
    public static class RouteFinderCore
    {
        public static List<Route> Routes = new() { };

        internal static bool DoConventionalRouting;

        /// <summary>Main method to invoke RouteFinderCore</summary>
        public static void RunRFASPNETCore(List<string> assemblies)
        {
            ClassDiscovery.Discover(assemblies);

             List<RouteTemplate> routeTemplates = new();

            if (DoConventionalRouting){
                routeTemplates = FindConventionalRouteTemplates();
            }

            ClassDiscovery.MethodExploredDict = new Dictionary<string, bool>();
            SinkFinder.SinkFinder.Sinks = new Dictionary<List<string>, Dictionary<string, string>>();

            foreach (string assembly in assemblies)
            {
                var module = AssemblyHelper.LoadAssembly(assembly);
                if (module is not null)
                {
                    Routes.AddRange(FindRoutes(module, assembly, routeTemplates));
                    Writer.WriteCSV(Routes);
                }
            }
        }

        private static bool IsController(TypeDef type)
        {
            if (ShouldSkipController(type)) { return true; }
            if (CheckControllerBaseType(type)) { return true; }
            if (Convert.ToString(type).EndsWith("Controller")) { return true; }
            if (type.CustomAttributes.Find("Microsoft.AspNetCore.Mvc.ApiControllerAttribute") != null) { return true; }
            if (type.CustomAttributes.Find("Microsoft.AspNetCore.Mvc.ControllerAttribute") != null) { return true; }

            return false;
        }

        private static List<Route> FindRoutes(ModuleDefMD module, string assembly, List<RouteTemplate> routeTemplates)
        {
            List<Route> routes = new() { };

            bool assemblySeen = false;

            // Scan assembly ahead of time for relevant middleware in Program/Startup

            var mainClasses = module.GetTypes().Where(t => t.GetFullTypeNameCleaned().EndsWith(".Startup") || t.GetFullTypeNameCleaned().EndsWith(".Program"));

            string pathBasePrefix = FindUsePathBase(mainClasses);

            foreach (TypeDef type in module.GetTypes())
            {
                List<Dictionary<string, dynamic>> routeDataList = new() { };
                string typeFullName = type.GetFullTypeNameCleaned();

                if (!IsController(type)) { continue; }

                if (!assemblySeen)
                {
                    string assemblyClean = AssemblyHelper.CleanDLLNamePartial(assembly);
                    assemblySeen = true;
                    Logger.Verbose("assembly", assemblyClean);
                }

                Logger.Verbose("c", typeFullName);

                // Parse Class-level attributes, such as [Authorize] to determine role constraints

                List<(string, string)> controllerAttributes = FindControllerAttributes(type);

                // Get all route prefixes from base classes

                string routePrefix = GetNestedRoutePrefixes(type);

                // Determine routing style of controller

                bool isOnlyAttributeRouted = IsOnlyAttributeRouted(type);

                // Identify all Action-level routes and add to routes list

                foreach (var method in type.Methods)
                {
                    if (!IsAction(method)) { continue; }

                    bool isAttributeRouteCheck = false;

                    isAttributeRouteCheck = isOnlyAttributeRouted || IsAttributeRoute(method);

                    if (isOnlyAttributeRouted || isAttributeRouteCheck)
                    {
                        routeDataList.AddRange(GetAttributeRouteFromAction(method, routePrefix, pathBasePrefix, type));
                    }
                    else if (DoConventionalRouting)
                    {
                        routeDataList.AddRange(GetConventionalRouteFromAction(method, type, routeTemplates, pathBasePrefix));
                    }
                }

                foreach (Dictionary<string, dynamic> routeData in routeDataList)
                {
                    Route route = new(routeData["fullRoute"], assembly, controllerAttributes, typeFullName, routeData["authorizations"], routeData["httpMethod"], routeData["contentType"], routeData["sinks"], routeData["action"]);
                    routes.Add(route);
                }
            }
            return routes;
        }

        private static bool CheckControllerBaseType(TypeDef type)
        {
            ITypeDefOrRef last_derived = null;
            ITypeDefOrRef derived = type.GetBaseType();

            while (true)
            {
                if (derived == null || last_derived == derived)
                {
                    return false;
                }
                if (derived.FullName.ToLower().EndsWith("controller"))
                {
                    return true;
                }

                last_derived = derived;
                derived = derived.GetBaseType();
            }
        }

        internal static string FindUsePathBase(IEnumerable<TypeDef> types)
        {
            List<List<dnlib.DotNet.Emit.Instruction>> usePathBaseInstructions = new();
            string pathBasePrefix = "";

            foreach (TypeDef type in types)
            {
                foreach (var method in type.Methods)
                {
                    usePathBaseInstructions.AddRange(ClassDiscovery.RunRecursiveFindCalls(method.FullName, type, "UsePathBase"));
                    if (usePathBaseInstructions.Any())
                    {
                        List<dnlib.DotNet.Emit.Instruction> calleeMethodInstructions = usePathBaseInstructions[0];
                        int index = calleeMethodInstructions.FindIndex(x => x.Operand.GetPropertyValue("Name").ToString().ToLower() == "usepathbase");
                        int ldargIndex = calleeMethodInstructions.FindLastIndex(index, x => x.OpCode.ToString() == "ldarg.1");
                        var targetMethodInstructions = calleeMethodInstructions.Skip(ldargIndex).Take(index - ldargIndex);

                        // We only handle a single implementation of the method currently (This will change in the future)
                        if (targetMethodInstructions.Count() == 3)
                        {
                            var operand = targetMethodInstructions.Skip(1).First().Operand;
                            pathBasePrefix = operand.ToString();
                        }
                        return pathBasePrefix;
                    }
                }
            }
            return "";
        }

        public static string GetInheritedArea(TypeDef type)
        {
            var inheritedArea = type.GetNestedBaseTypes().Where(t => t.CustomAttributes.Find("Microsoft.AspNetCore.Mvc.AreaAttribute") is not null).Select(t => t.CustomAttributes.Find("Microsoft.AspNetCore.Mvc.AreaAttribute").ConstructorArguments?[0]).FirstOrDefault();
            string inheritedAreaString = (inheritedArea?.Value is not null) ? inheritedArea.Value.Value.ToString() : "";

            return inheritedAreaString;
        }

        internal static string CleanTemplateTokens(string routeRaw, TypeDef type, string action = "", string areaName = "")
        {
            // Change this to only replace last occurence of COntroller
            string controllerNameCleaned = type.GetTypeNameCleaned().ToString().FindLastThenLower("Controller");

            int controllerStringIndex = controllerNameCleaned.LastIndexOf("controller");
            controllerNameCleaned = controllerNameCleaned.EndsWith("controller") ? controllerNameCleaned[..controllerStringIndex] : controllerNameCleaned;

            //Replace tokens on conventional routed controller
            string routeCleaned = Regex.Replace(routeRaw, "{controller.*?}", controllerNameCleaned);
            routeCleaned = Regex.Replace(routeCleaned, "{action.*?}", action);

            //Replace tokens on attribute routed controller
            routeCleaned = routeCleaned.Replace("[controller]", controllerNameCleaned);
            routeCleaned = !String.IsNullOrEmpty(action) ? routeCleaned.Replace("[action]", action) : routeCleaned;
            routeCleaned = !String.IsNullOrEmpty(areaName) ? routeCleaned.Replace("[area]", areaName) : routeCleaned;

            // Clean extra slashes and repalce ~ with slash
            routeCleaned = routeCleaned.Replace("~", "/");
            routeCleaned = Regex.Replace(routeCleaned, @"\/+", "/");

            return routeCleaned;
        }

        internal static void AddRouteDataToDictionary(List<Dictionary<string, dynamic>> routeDataList, string fullRoute, List<(string value, string type)> authorizations, string contentType, string action,string methodFullName, TypeDef type, string httpMethod = "*")
        {
            authorizations.ForEach(x => Logger.Verbose("roa", x.value, x.type));
            Logger.Verbose("r", fullRoute, httpMethod);
            Logger.Verbose("act", action);

            Dictionary<List<string>, Dictionary<string, string>> sinks = SinkFinder.SinkFinder.Run(methodFullName, type);


            var routeData = new Dictionary<string, dynamic>
            {
                { "fullRoute", fullRoute },
                { "authorizations", authorizations },
                { "contentType", contentType },
                { "httpMethod", httpMethod },
                { "sinks", sinks },
                { "action", action }
            };
            routeDataList.Add(routeData);

  
        }

        internal static bool ShouldSkipController(TypeDef type)
        {
            return type.CustomAttributes.FindAll(Constants.NonControllerAttributeString).Any();
        }
    }
}

