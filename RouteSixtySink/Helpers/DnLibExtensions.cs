/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using System.Text.RegularExpressions;
using dnlib.DotNet;
using System.Linq;
using System.Collections.Generic;

namespace RouteSixtySink.Helpers
{
    public static class DnLibExtensions
    {
        internal static string GetTypeNameCleaned(this TypeDef type)
        {

            return Regex.Replace(type.Name, @"`[0-9]$", "");
        }

        internal static string GetFullTypeNameCleaned(this TypeDef type)
        {

            return Regex.Replace(type.FullName, @"`[0-9]$", "");
        }

        internal static IEnumerable<ITypeDefOrRef> GetNestedBaseTypes(this TypeDef type)
        {
            var sequence = Enumerable.Empty<ITypeDefOrRef>();

            ITypeDefOrRef lastBaseType = null;
            ITypeDefOrRef baseType = type.GetBaseType();

            while (baseType is not null && lastBaseType != baseType)
            {
                sequence = sequence.Append(baseType);
                lastBaseType = baseType;
                baseType = baseType.GetBaseType();
            }

            return sequence;
        }

        internal static IEnumerable<ITypeDefOrRef> GetImmediateAndNestedBaseTypes(this TypeDef type)
        {
            var sequence = Enumerable.Empty<ITypeDefOrRef>();

            sequence = sequence.Append(type);

            ITypeDefOrRef lastBaseType = null;
            ITypeDefOrRef baseType = type.GetBaseType();

            while (baseType is not null && lastBaseType != baseType)
            {
                sequence = sequence.Append(baseType);
                lastBaseType = baseType;
                baseType = baseType.GetBaseType();
            }

            return sequence;
        }
    }
}
