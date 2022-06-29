/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */
namespace RouteSixtySink.Models
{
    public class RouteTemplate
    {
        public string Pattern { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }

        public RouteTemplate(string Pattern, string Action = "", string Controller = "")
        {
            this.Pattern = Pattern;
            this.Action = Action;
            this.Controller = Controller;
        }
    }
}