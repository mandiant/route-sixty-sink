/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using dnlib.DotNet;
using RouteSixtySink.Outputters;
namespace RouteSixtySink.Helpers
{
    public static class AssemblyHelper
    {
        public static List<string> GetAssemblies(string str, bool includeExes = false)
        {
            List<string> Assemblies = new() { };

            System.IO.FileAttributes attr = File.GetAttributes(str);

            if (attr.HasFlag(System.IO.FileAttributes.Directory))
            {
                var dlls = Directory.GetFiles(str, "*.dll", SearchOption.AllDirectories);

                foreach (string dll in dlls)
                {
                    Assemblies.Add(dll);
                }

                if (includeExes)
                {
                    var exes = Directory.GetFiles(str, "*.exe", SearchOption.AllDirectories);

                    foreach (string exe in exes)
                    {
                        Assemblies.Add(exe);
                    }
                }
            }
            else // it's a file and not a directory
            {
                Assemblies.Add(str);
            }

            return Assemblies;
        }
        public static string CleanDLLName(string dll, string path)
        {
            string removalString = new FileInfo(path).Directory.FullName;
            // Get rid of the unimportant directory structure and output page
            string dllName = dll.Replace(removalString, "");
            dllName = dllName.Trim('/');
            return dllName;
        }

        public static string CleanDLLNamePartial(string path)
        {
            string[] pathArray = path.Split("/");
            if (pathArray.Length > 2)
            {
                path = String.Join("/", pathArray.TakeLast(3));
                path = path.Trim('/');
            }
            return path;
        }

        public static ModuleDefMD LoadAssembly(string assemblyPath)
        {
            ModuleContext modCtx = ModuleDef.CreateModuleContext();
            try
            {
                ModuleDefMD module = ModuleDefMD.Load(assemblyPath, modCtx);
                return module;
            }
            catch
            {
                Logger.Error("Error loading:\t" + assemblyPath);
            }
            return null;
        }
    }
}