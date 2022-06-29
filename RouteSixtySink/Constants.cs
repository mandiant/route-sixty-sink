
using System.Collections.Generic;
namespace RouteSixtySink

{
    public static class Constants
    {
        internal static readonly string RouteAttributeString = "Microsoft.AspNetCore.Mvc.RouteAttribute";
        internal static readonly List<string> RouteAttributes = new() { "Microsoft.AspNetCore.Mvc.RouteAttribute", "System.Web.Http.RouteAttribute","System.Web.Mvc.RouteAttribute"};
        internal static readonly string AreaAttributeString = "Microsoft.AspNetCore.Mvc.AreaAttribute";
        internal static readonly string ApiControllerAttribute = "Microsoft.AspNetCore.Mvc.ApiControllerAttribute";
        internal static readonly string NonActionAttributeString = "Microsoft.AspNetCore.Mvc.NonActionAttribute";
        internal static readonly string NonControllerAttributeString = "Microsoft.AspNetCore.Mvc.NonControllerAttribute";
        internal static readonly string ConsumesAttribute = "Microsoft.AspNetCore.Mvc.ConsumesAttribute";
        internal static readonly string AllowAnonymousAttribute = "Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute";
        internal static readonly string AuthorizeAttribute = "Microsoft.AspNetCore.Authorization.AuthorizeAttribute";
        internal static readonly string AcceptVerbsAttribute = "Microsoft.AspNetCore.Mvc.AcceptVerbsAttribute";
        internal static readonly string ActionTokenSquareBrackets = "[action]";
        internal static readonly List<string> ControllerClassList = new() { "Microsoft.AspNetCore.Mvc.Controller", "Microsoft.AspNetCore.Mvc.ControllerBase" };
        internal static readonly List<List<string>> MethodMap = new()
        {
            new List<string>() { "Microsoft.AspNetCore.Mvc.HttpGetAttribute", "GET",},
            new List<string>() { "Microsoft.AspNetCore.Mvc.HttpPostAttribute", "POST",},
            new List<string>() { "Microsoft.AspNetCore.Mvc.HttpPatchAttribute", "PATCH",},
            new List<string>() { "Microsoft.AspNetCore.Mvc.HttpPutAttribute", "PUT",},
            new List<string>() { "Microsoft.AspNetCore.Mvc.HttpOptionsAttribute", "OPTIONS",},
            new List<string>() { "Microsoft.AspNetCore.Mvc.HttpDeleteAttribute", "DELETE",},
            new List<string>() { "Microsoft.AspNetCore.Mvc.HttpHeadAttribute", "HEAD",},
            new List<string>() { "System.Web.Http.HttpGetAttribute", "GET",},
            new List<string>() { "System.Web.Http.HttpPostAttribute", "POST",},
            new List<string>() { "System.Web.Http.HttpPatchAttribute", "PATCH",},
            new List<string>() { "System.Web.Http.HttpPutAttribute", "PUT",},
            new List<string>() { "System.Web.Http.HttpOptionsAttribute", "OPTIONS",},
            new List<string>() { "System.Web.Http.HttpDeleteAttribute", "DELETE",},
            new List<string>() { "System.Web.Http.HttpHeadAttribute", "HEAD",},
            new List<string>() { "System.Web.Mvc.HttpGetAttribute", "GET",},
            new List<string>() { "System.Web.Mvc.HttpPostAttribute", "POST",},
            new List<string>() { "System.Web.Mvc.HttpPatchAttribute", "PATCH",},
            new List<string>() { "System.Web.Mvc.HttpPutAttribute", "PUT",},
            new List<string>() { "System.Web.Mvc.HttpOptionsAttribute", "OPTIONS",},
            new List<string>() { "System.Web.Mvc.HttpDeleteAttribute", "DELETE",},
            new List<string>() { "System.Web.Mvc.HttpHeadAttribute", "HEAD",},
        };
        internal static readonly List<string> MapCallSignatures = new() { "Microsoft.AspNetCore.Builder.ControllerEndpointRouteBuilderExtensions::MapDefaultControllerRoute(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)" };
        internal static readonly Dictionary<string, string> BodyDataLorumIpsum = new() { { "fake", "data" } };
        internal static readonly string AsciiArt = @"
   ___            __        _____      __           _____      __  
  / _ \___  __ __/ /____   / __(_)_ __/ /___ ______/ __(_)__  / /__
 / , _/ _ \/ // / __/ -_) _\ \/ /\ \ / __/ // /___/\ \/ / _ \/  '_/
/_/|_|\___/\_,_/\__/\__/ /___/_//_\_\\__/\_, /   /___/_/_//_/_/\_\ 
                                        /___/                      ";

    }
}

