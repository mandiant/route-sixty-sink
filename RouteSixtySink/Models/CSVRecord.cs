/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */
using System.Collections.Generic;

namespace RouteSixtySink.Models
{
    public class CSVRecord
    {
        public string Path { get; set; }
        public string Assembly { get; set; }
        public List<(string, string)> Authorization { get; set; }
        public string ControllerName { get; set; }
        public List<(string, string)> MethodAuthorization { get; set; }
        public string HTTPMethod { get; set; }
        public string ContentType { get; set; }
        public string Sinks { get; set; }

        public CSVRecord(string Path, string Assembly, List<(string, string)> Authorization, string ControllerName, List<(string, string)> MethodAuthorization, string HTTPMethod, string ContentType, string Sinks)
        {
            this.Path = Path;
            this.Assembly = Assembly;
            this.Authorization = Authorization;
            this.ControllerName = ControllerName;
            this.MethodAuthorization = MethodAuthorization;
            this.HTTPMethod = HTTPMethod;
            this.ContentType = ContentType;
            this.Sinks = Sinks;
        }
    }
}