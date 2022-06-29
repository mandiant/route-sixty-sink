/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using RouteSixtySink.Outputters;
using System.Linq;
using RouteSixtySink.Helpers;
using static RouteSixtySink.Core.RouteFinderCore;

namespace RouteSixtySink.Core
{
    public static class RouteFinderCoreAttributeRouting
    {

        internal static string GetNestedRoutePrefixes(TypeDef type)
        {
            CustomAttribute routeAttribute = null;

            foreach (var nestedType in type.GetImmediateAndNestedBaseTypes()){
                routeAttribute = Constants.RouteAttributes.Select(x => nestedType.CustomAttributes.Find(x)).FirstOrDefault(t => t is not null);
                if (routeAttribute is not null){
                    break;
                }
            }

            string routePrefix = (routeAttribute is not null) ? routeAttribute.ConstructorArguments[0].Value.ToString() : "";
            if (!String.IsNullOrEmpty(routePrefix)) { Logger.Verbose("p", routePrefix); }
            return routePrefix;
        }

        internal static string FindRequestContentType(MethodDef method)
        {
            CAArgument? consumeAttributeArgument = method.CustomAttributes?.Find(Constants.ConsumesAttribute)?.ConstructorArguments?[0] ?? null;
            string consumeAttributeValue = (consumeAttributeArgument is not null) ? consumeAttributeArgument.Value.ToString() : "";
            return consumeAttributeValue;
        }

        internal static List<(string, string)> FindAuthorization(dynamic type, string level)
        {
            CustomAttribute anonAuthAttribute = type.CustomAttributes.Find(Constants.AllowAnonymousAttribute);
            CustomAttributeCollection customAttributeCollection = type.CustomAttributes;
            var authorizedAttributes = customAttributeCollection.FindAll(Constants.AuthorizeAttribute);
            bool explicitAuthorizationSet = false;
            var authorizationList = new List<(string, string)>();
            string anonAuthorization = (anonAuthAttribute is not null) ? "AllowAnonymous" : null;

            authorizationList.AddIfNotNullString(anonAuthorization);

            foreach (var attribute in authorizedAttributes)
            {
#nullable enable
                string? roles = (attribute.GetProperty("Roles") is not null) ? attribute.GetProperty("Roles").Value.ToString() : null;
                string? policyConstructorArg = (attribute.ConstructorArguments.Any() && attribute.ConstructorArguments[0].Value is not null) ? attribute.ConstructorArguments[0].Value.ToString() : null;
                string? policy = (attribute.GetProperty("Policy") is not null) ? attribute.GetProperty("Policy").Value.ToString() : null;
                string? scheme = (attribute.GetProperty("AuthenticationSchemes") is not null) ? attribute.GetProperty("AuthenticationSchemes").Value.ToString() : null;
#nullable disable

                authorizationList.AddIfNotNullString(roles, "Role");
                authorizationList.AddIfNotNullString(policyConstructorArg, "Policy");
                authorizationList.AddIfNotNullString(policy, "Policy");
                authorizationList.AddIfNotNullString(scheme, "Scheme");

                if (roles != null || policy != null || scheme != null || policyConstructorArg != null) { explicitAuthorizationSet = true; }
            }

            if (!explicitAuthorizationSet && authorizedAttributes.Any())
            {
                authorizationList.Add(("Authenticated Users", "Role"));
            }

            if (authorizedAttributes.Any() && !String.IsNullOrEmpty(anonAuthorization))
            {
                Logger.Warning(level + "misconfig", "Anonymous allowed on an authorized route. This may signify a misconfiguration.");
            }

            return authorizationList;
        }

        internal static bool IsOnlyAttributeRouted(TypeDef type)
        {
            var targetAttributes = new List<string>() { Constants.AreaAttributeString, Constants.ApiControllerAttribute};
            targetAttributes.AddRange(Constants.RouteAttributes);
            var inheritedCustomAttribute = type.GetImmediateAndNestedBaseTypes().SelectMany(t => t.CustomAttributes).Select(t => t.ToString()).Where(t => targetAttributes.Contains(t));

            bool attributeRouting = inheritedCustomAttribute.Any();

            return attributeRouting;
        }

        internal static List<(string, string)> FindControllerAttributes(TypeDef type)
        {
            List<(string value, string type)> authorizations = FindAuthorization(type, "controller");
            authorizations.ForEach(x => Logger.Verbose("ro", x.value, x.type));

            return authorizations;
        }

        internal static bool IsAction(MethodDef method)
        {
            if (method.Name == ".ctor") { return false; }
            if (method.IsGetter || method.IsSetter) { return false; }
            if (method.Access.ToString() != "Public") { return false; }
            if (method.CustomAttributes.FindAll(Constants.NonActionAttributeString).Any()) { return false; }
            return true;
        }

        internal static List<string> FindRouteAttributeOnAction(MethodDef method, TypeDef type, string routePrefixString, string pathBasePrefix)
        {

            List<CustomAttribute> actionRouteAttributes = new();
            Constants.RouteAttributes.ForEach(x => actionRouteAttributes.AddRange(method.CustomAttributes.FindAll(x)));

            //IEnumerable<CustomAttribute> actionRouteAttributes = method.CustomAttributes.FindAll(Constants.RouteAttributeString);
            var routeAttributes = new List<string>();

            foreach (CustomAttribute actionRouteAttribute in actionRouteAttributes)
            {
                var routePath = actionRouteAttribute.ConstructorArguments[0];
                string routeString = UTF8String.ToSystemString((UTF8String)routePath.Value);

                string fullRoute = (!routeString.StartsWith("/") && !routeString.StartsWith("~")) ? pathBasePrefix + "/" + routePrefixString + "/" + routeString : pathBasePrefix + "/" + routeString;
                fullRoute = CleanTemplateTokens(fullRoute, type, method.Name);
                routeAttributes.Append(fullRoute);
            }
            return routeAttributes;

        }

        internal static List<string> GetAcceptVerbs(MethodDef method)
        {
            var acceptVerbsAttributes = method.CustomAttributes.FindAll(Constants.AcceptVerbsAttribute);
            var acceptVerbsList = acceptVerbsAttributes.SelectMany(x => x.ConstructorArguments.Where(x => CollectionHelpers.IsList(x.Value)).SelectMany(a => ((List<CAArgument>)a.Value).Select(a => a.Value.ToString()))).Distinct().ToList();
            acceptVerbsList.AddRange(acceptVerbsAttributes.SelectMany(x => x.ConstructorArguments.Where(x => !CollectionHelpers.IsList(x.Value)).Select(a => a.Value.ToString())).Distinct().ToList());

            return acceptVerbsList;
        }

        internal static string GetArea(dynamic methodOrType)
        {
            string typeReference = methodOrType.GetType().ToString();
            string areaName = methodOrType.CustomAttributes?.Find(Constants.AreaAttributeString)?.ConstructorArguments?[0]?.Value.ToString() ?? "";
            areaName = (String.IsNullOrEmpty(areaName) && typeReference.EndsWith("TypeDefMD")) ? GetInheritedArea(methodOrType) : areaName;

            return areaName;
        }

        internal static bool IsAttributeRoute(MethodDef method)
        {

            bool isAttributeRoute = false;

            // If [Route] attribute on action, it is attribute routed

            isAttributeRoute =  Constants.RouteAttributes.Where(x => method.CustomAttributes.FindAll(x).Any()).Any() || isAttributeRoute;

            // If [HttpMethod] attribute on action and has constructor arguments, it is attribute routed

            foreach (List<string> list in Constants.MethodMap)
            {
                string attributeName = list[0];
                string httpVerb = list[1];

                IEnumerable<CustomAttribute> actionHTTPMethodAttributes = method.CustomAttributes.FindAll(attributeName);
                isAttributeRoute = actionHTTPMethodAttributes.Any(x => x.ConstructorArguments.Any()) || isAttributeRoute;
            }

            return isAttributeRoute;
        }

        internal static List<Dictionary<string, dynamic>> GetAttributeRouteFromAction(MethodDef method, string routePrefixString, string pathBasePrefix, TypeDef type)
        {
            var routeDataList = new List<Dictionary<string, dynamic>>();
            string fullRoute = pathBasePrefix + "/" + routePrefixString;
            var areaName = (!String.IsNullOrEmpty(GetArea(method))) ? GetArea(method) : GetArea(type);
            List<(string, string)> authorizations = FindAuthorization(method, "action");
            string contentType = FindRequestContentType(method);
            var acceptVerbs = GetAcceptVerbs(method);
            var httpVerbsNoConstructorArguments = new List<string>();
            bool routeSeen = false;

            List<CustomAttribute> actionRouteAttributes = new();
            Constants.RouteAttributes.ForEach(x => actionRouteAttributes.AddRange(method.CustomAttributes.FindAll(x)));

            // Iterates through all [HTTPMethod] attributes and adds to RouteData Dictionary

            foreach (List<string> list in Constants.MethodMap)
            {
                string attributeName = list[0];
                string httpVerb = list[1];

                IEnumerable<CustomAttribute> actionHTTPMethodAttributes = method.CustomAttributes.FindAll(attributeName);

                foreach (CustomAttribute actionHTTPMethodAttribute in actionHTTPMethodAttributes)
                {
                    CAArgument httprouteArg;

                    string httpMethod = httpVerb;

                    if (actionHTTPMethodAttribute.ConstructorArguments.Any())
                    {
                        // If any HTTPMethod attributes have a constructor argument, the controller uses attribute-based routing
                        httprouteArg = actionHTTPMethodAttribute.ConstructorArguments[0];
                        string routeString = UTF8String.ToSystemString((UTF8String)httprouteArg.Value);

                        fullRoute = (!routeString.StartsWith("/") && !routeString.StartsWith("~")) ? pathBasePrefix + "/" + routePrefixString + "/" + routeString : pathBasePrefix + "/" + routeString;
                        fullRoute = CleanTemplateTokens(fullRoute, type, method.Name, areaName);
                        AddRouteDataToDictionary(routeDataList, fullRoute, authorizations, contentType, method.Name, method.FullName,type,httpMethod);
                        routeSeen = true;
                    }
                    else if (!String.IsNullOrEmpty(routePrefixString) && !actionRouteAttributes.Any()){
                        fullRoute = pathBasePrefix + "/" + routePrefixString;
                        fullRoute = CleanTemplateTokens(fullRoute, type, method.Name, areaName);
                        AddRouteDataToDictionary(routeDataList, fullRoute, authorizations, contentType,method.Name,method.FullName,type,httpMethod);
                        routeSeen = true;;
                    }
                    else{
                        httpVerbsNoConstructorArguments.Add(httpMethod);
                    }
                }
            }

             // Iterates through all [Route] attributes and adds to RouteData Dictionary

            foreach (CustomAttribute actionRouteAttribute in actionRouteAttributes)
            {
                var routePath = actionRouteAttribute.ConstructorArguments[0];
                string routeString = UTF8String.ToSystemString((UTF8String)routePath.Value);

                fullRoute = (!routeString.StartsWith("/") && !routeString.StartsWith("~")) ? pathBasePrefix + "/" + routePrefixString + "/" + routeString : pathBasePrefix + "/" + routeString;
                fullRoute = CleanTemplateTokens(fullRoute, type, method.Name, areaName);

                // Constrains Route to specific methods

                acceptVerbs.ForEach(httpMethod => AddRouteDataToDictionary(routeDataList, fullRoute, authorizations, contentType, method.Name, method.FullName,type,httpMethod));
                httpVerbsNoConstructorArguments.ForEach(httpMethod => AddRouteDataToDictionary(routeDataList, fullRoute, authorizations, contentType, method.Name, method.FullName,type,httpMethod));

                if (!acceptVerbs.Any() && !httpVerbsNoConstructorArguments.Any())
                {
                    AddRouteDataToDictionary(routeDataList, fullRoute, authorizations, contentType,method.Name,method.FullName,type);
                }
                routeSeen = true;
            }

            // If we are here, then no other route declaration was found, so apply prefix

            if (!String.IsNullOrEmpty(routePrefixString) && !routeSeen)
            {
                fullRoute = CleanTemplateTokens(fullRoute, type, method.Name, areaName);

                acceptVerbs.ForEach(httpMethod => AddRouteDataToDictionary(routeDataList, fullRoute, authorizations, contentType, method.Name, method.FullName,type,httpMethod));

                if (!acceptVerbs.Any() && !httpVerbsNoConstructorArguments.Any())
                {
                    AddRouteDataToDictionary(routeDataList, fullRoute, authorizations, contentType, method.Name,method.FullName,type);
                }
            }
            return routeDataList;
        }
    }
}

