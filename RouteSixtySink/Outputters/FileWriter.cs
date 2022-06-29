/*Copyright (C) 2022 Mandiant, Inc. All Rights Reserved.*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using RouteSixtySink.Models;
namespace RouteSixtySink.Outputters

{
    public static class Writer
    {
        public static string OutputDirectory = Path.Combine(Environment.CurrentDirectory, "Output/");
        public static string OutputFile = "log-" + DateTime.Now.ToString().Replace(" ", "-").Replace("/", "").Replace(":", "-");
        public static bool CSVOutputToFile;
        public static bool LogOutputToFile;
        public static bool FileCreated = false;
        public static void WriteCSV(List<Route> classRoutes)

        {
            if (CSVOutputToFile)
            {

                using TextWriter writer = new StreamWriter(Path.ChangeExtension(Path.Combine(OutputDirectory, OutputFile), ".csv"), true, System.Text.Encoding.UTF8);
                var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);

                if (!FileCreated)
                {
                    csv.WriteHeader<CSVRecord>();
                    FileCreated = true;
                }
                csv.NextRecord();

                foreach (var route in classRoutes)
                {
                    try
                    {
                        string sinks = "";
                        try
                        {
                            sinks = string.Join(",", route.Sinks);
                        }
                        catch
                        {
                        }
                        var csvRecord = new CSVRecord(route.Path, route.Assembly, route.Authorization, route.ControllerName, route.MethodAuthorization, route.HTTPMethod, route.ContentType, sinks);
                        csv.WriteRecord(csvRecord);
                        csv.NextRecord();
                    }
                    catch
                    {
                    }
                }
            }

        }

        public static void AppendFile(string message)
        {
            using StreamWriter sw = File.AppendText(Path.ChangeExtension(Path.Combine(OutputDirectory, OutputFile), ".txt"));
            sw.Write(message);
        }
    }
}