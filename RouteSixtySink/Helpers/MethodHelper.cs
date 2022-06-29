/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using System.Collections.Generic;
using System;
using dnlib.DotNet;
using RouteSixtySink.Outputters;

namespace RouteSixtySink.Helpers
{
    public static class MethodHelper
    {
        public static void GetPublicMethods(TypeDef type)
        {
            foreach (MethodDef method in type.Methods)
            {
                string methodAccess = method.Access.ToString();
                if (methodAccess == "Public" && !method.IsGetter && !method.IsSetter && (method.Parameters.Count > 0))
                {
                    List<string> parameterList = new();
                    foreach (var parameter in method.Parameters)
                    {
                        if (parameter.ToString().StartsWith("A_")) { continue; }
                        parameterList.Add(parameter.ToString());
                    }
                    if (parameterList.Count > 0)
                    {
                        Logger.Debug(String.Format("\tMethod: {0}::{1}\t\t{2}", type.ToString(), method.Name, String.Join(", ", parameterList)));
                    }
                }
            }
        }
        public static void GetMethods(TypeDef type)
        {
            foreach (MethodDef method in type.Methods)
            {
                Logger.Debug(String.Format("\tMethod: {0}::{1}", type.ToString(), method.Name));
            }
        }
        public static MethodDef GetMethod(TypeDef type, string methodName)
        {
            foreach (MethodDef method in type.Methods)
            {
                if (method.FullName == methodName)
                {
                    return method;
                }
            }
            return null;
        }
    }
}