/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using System;
using System.Collections.Generic;

namespace RouteSixtySink.Helpers
{
    public static class CollectionHelpers
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T element in source)
            {
                action(element);
            }
        }

        public static void AddIfNotNull<T>(this List<T> list, T item)
        {
            if (item != null) { list.Add(item); }
        }

        public static void AddIfNotNullString(this List<(string, string)> list, string item, string context = "")
        {
            if (item != null)
            {
                list.Add((item, context));
            }
        }

        public static bool IsList<T>(T item)
        {
            return item.GetType().IsGenericType && item.GetType().GetGenericTypeDefinition() == typeof(List<>);
        }
    }
}
