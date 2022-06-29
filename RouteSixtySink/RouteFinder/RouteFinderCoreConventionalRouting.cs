/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using System.Linq;
using RouteSixtySink.Discovery;
using RouteSixtySink.Models;
using RouteSixtySink.Helpers;
using static RouteSixtySink.Core.RouteFinderCore;
using static RouteSixtySink.Outputters.Logger;

namespace RouteSixtySink.Core
{
    public static class RouteFinderConventionalRouting
    {
        public static List<RouteTemplate> RunRecursiveFindMapCalls(string methodName, TypeDef type)
        {
            List<string> routeToMethod = new();

            return FindMapCalls(methodName, type, ref routeToMethod, 0);
        }

        internal static List<RouteTemplate> FindConventionalRouteTemplates()
        {
            if (!RouteFinderCore.DoConventionalRouting)
            {
                return new() { };
            }

            List<RouteTemplate> routeTemplates = new() { };

            var configurationClasses = ClassDiscovery.ClassNameToTypeDef.Where(x => x.Value.Item2.ToString().Contains(".Program") || x.Value.Item2.ToString().Contains(".Startup")).Select(x => x.Value.Item2);

            foreach (TypeDef clsType in configurationClasses)
            {
                foreach (MethodDef method in clsType.Methods)
                {
                    routeTemplates.AddRange(RunRecursiveFindMapCalls(method.FullName, clsType));
                }
            }
       
            if (!routeTemplates.Any())
            {
                Debug("No default conventional route templates identified! Falling back to the default template.");
                routeTemplates.Add(new("/{controller=Home}/{action=Index}/{id?}", "Index", "Home"));
            }

            return routeTemplates;
        }

        public static List<RouteTemplate> FindMapCalls(string methodName, TypeDef type, ref List<string> routeToMethod, int depth)
        {
            // Initialize RouteTemplate list to hold all identified routes

            List<RouteTemplate> routeTemplates = new() { };

            // Add the method we are investigating to the end of the list

            routeToMethod.Add(methodName);

            // Don't recurse more than the specified depth

            if (SinkFinder.SinkFinder.RecurseDepth != -1 && depth >= SinkFinder.SinkFinder.RecurseDepth) { return routeTemplates; }

            MethodDef method = MethodHelper.GetMethod(type, methodName);

            if (method == null || method.Body == null) { return routeTemplates; }

            if (!SinkFinder.SinkFinder.ShouldExploreMethodCall(methodName))
            {
                // Check if the method is cached, and if so, return and continue onto the next method
                bool cachedFindingsReported = SinkFinder.SinkFinder.ReportCachedFindings(methodName, type, ref routeToMethod);
                return routeTemplates;
            }

            // Set the current method as explored!
            ClassDiscovery.MethodExploredDict[methodName] = true;

            int index = 0;

            // Begin looking for Map call

            foreach (dnlib.DotNet.Emit.Instruction instr in method.Body.Instructions)
            {
                index += 1;
                if (instr is null || instr.Operand is null) { continue; }

                bool isMapCallReturn = IsMapCall(instr);

                // If MapCall found it attempts to build a conventional route using all the instructions from the encompassing call
                if (isMapCallReturn)
                {
                    BuildConventionalRoute(instr, ref routeTemplates);
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
                        FindMapCalls(classMethodName, nextType, ref routeToMethod, depth + 1);
                        routeToMethod.Remove(routeToMethod.Last());
                    }
                }
            }
            return routeTemplates;
        }

        private static bool IsMapCall(dnlib.DotNet.Emit.Instruction instr)
        {
            bool mapCall = false;
            foreach (var mapRouteCall in Constants.MapCallSignatures.Where(mapRouteCall => instr.Operand.ToString().ToLower().Contains(mapRouteCall.ToLower())))
            {
                Debug(String.Format("🔍 Found map call {0}! Attempting to parse!", mapRouteCall));
                mapCall = true;
            }
            return mapCall;
        }

        private static void BuildConventionalRoute(dnlib.DotNet.Emit.Instruction instr, ref List<RouteTemplate> routeTemplates)
        {
            if (instr.Operand.ToString().ToLower().Contains("mapdefaultcontroller"))
            {
                RouteTemplate routeTemplate = new("/{controller=Home}/{action=Index}/{id?}", "Index", "Home");
                routeTemplates.Add(routeTemplate);
            }
            return;
        }

        internal static List<Dictionary<string, dynamic>> GetConventionalRouteFromAction(MethodDef method, TypeDef type, List<RouteTemplate> routeTemplates, string pathBasePrefix)
        {
            var routeDataList = new List<Dictionary<string, dynamic>>();
            List<(string, string)> authorizations = RouteFinderCoreAttributeRouting.FindAuthorization(method, "action");
            string contentType = RouteFinderCoreAttributeRouting.FindRequestContentType(method);

            List<string> httpMethods = new() { };

            // Identify all HTTP verbs for route prior to creating route 

            foreach (List<string> list in Constants.MethodMap)
            {

                string attributeName = list[0];
                string httpVerb = list[1];

                IEnumerable<CustomAttribute> actionHTTPMethodAttributes = method.CustomAttributes.FindAll(attributeName);

                foreach (CustomAttribute actionHTTPMethodAttribute in actionHTTPMethodAttributes)
                {
                    httpMethods.Add(httpVerb);
                }
            }

            foreach (var routeTemplate in routeTemplates.Where(rt => rt.Pattern.Contains("{controller") && rt.Pattern.Contains("{action")))
            {
                string fullRoute = pathBasePrefix + CleanTemplateTokens(routeTemplate.Pattern, type, method.Name);
   
                if (httpMethods.Any())
                {
                    foreach (string httpMethod in httpMethods)
                    {
                        AddRouteDataToDictionary(routeDataList, fullRoute, authorizations, contentType, method.Name,method.FullName,type,httpMethod);
                    }
                    break;
                }
                else
                {
                    AddRouteDataToDictionary(routeDataList, fullRoute, authorizations, contentType, method.Name,method.FullName,type);
                    break;
                }
            }
            return routeDataList;
        }
    }
}
