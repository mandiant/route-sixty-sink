/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using System.Text.RegularExpressions;
namespace RouteSixtySink.Helpers
{
    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            return string.IsNullOrEmpty(value) ? value : value.Length <= maxLength ? value : value[..maxLength];
        }

        public static string FindLastThenLower(this string str, string replacement)
        {

            str = Regex.Replace(str, replacement + "$", replacement.ToLower(), RegexOptions.IgnoreCase);
            return str;
        }
    }
}
