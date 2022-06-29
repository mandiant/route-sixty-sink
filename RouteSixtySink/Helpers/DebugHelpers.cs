/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using System.Collections.Generic;
using RouteSixtySink.Outputters;
using System.Reflection;
using System;
using System.Linq;

namespace RouteSixtySink.Helpers
{
    public static class ReflectionHelpers
    {
        public static void DumpPropertiesAndFields(dynamic obj)
        {
            Type myType = obj.GetType();
            IList<FieldInfo> fields = new List<FieldInfo>(myType.GetFields());
            var props = myType.GetProperties().Where(p => !p.GetIndexParameters().Any());

            foreach (FieldInfo field in fields)
            {
                object fieldValue = field.GetValue(obj);
                Logger.Debug("Property Name: " + field + "\n\t" + fieldValue + "\n");
            }

            foreach (PropertyInfo prop in props)
            {
                object propValue = prop.GetValue(obj);
                Logger.Debug("Field Name: " + prop + "\n\t" + propValue + "\n");
            }
        }
        public static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }

        public static string GetPropertyValue(this object src, string propName)
        {
            string value = "";
            try
            {
                value = src.GetType().GetProperty(propName).GetValue(src, null).ToString();
            }
            catch { }
            return value;
        }

    }
}

