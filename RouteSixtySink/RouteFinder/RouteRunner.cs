/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using System;
using System.Collections.Generic;
using RouteSixtySink.Core;
using RouteSixtySink.Helpers;
using System.Net.Http;
using RouteSixtySink.Outputters;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace RouteSixtySink.RouteRunner
{
    public class ResultData
    {
        public ResultData(string url, int statuscode, string verb)
        {
            URL = url;
            StatusCode = statuscode;
            Verb = verb;
        }
        public string URL { get; set; }
        public int StatusCode { get; set; }
        public string Verb { get; set; }
    }
    public static class RouteRunner
    {

        internal static bool DoDelete = false;

        internal static HttpClient Client;

        internal static string ServiceEndpoint = "https://localhost:5001";

        internal static List<int> FailStatusCodes = new() { 405, 404, 302, 301 };

        private static List<Task<ResultData>> Results = new();

        public static void RunRouteRunner()
        {

            HttpClientHandler clientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
            };
            Client = new HttpClient(clientHandler);

            var actions = new Dictionary<string, Func<string, Task<ResultData>>>()
            {
            { "*", async (uri) => await SendRequest(uri,"*")  },
            { "get", async (uri) => await SendRequest(uri,"get") },
            { "options", async(uri) => await SendRequest(uri,"options") },
            { "head", async (uri) => await SendRequest(uri,"head") },
            { "delete",async  (uri) => await SendRequest(uri,"delete") },
            { "put", async (uri) => await SendRequest(uri,"put") },
            { "post", async (uri) => await SendRequest(uri,"post") },
            { "patch", async (uri) => await SendRequest(uri,"patch") },
            };

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(String.Format("\n\n👟  Invoking RouteRunner on discovered routes!"));
            Console.ResetColor();

            Console.WriteLine(String.Format("\n🙅  Failure indicators are status codes {0}.\n", String.Join(", ", FailStatusCodes)));

            RouteFinderCore.Routes.ForEach(x =>
            {
                Task<ResultData> routedata = actions[x.HTTPMethod.ToLower()](x.Path);
                if (!Equals(routedata.Result, default(ResultData)))
                {
                    Results.Add(routedata);
                }
            }
            );

            Console.WriteLine(String.Format("📭  {0}/{1} requests sent!\n", Results.Count, RouteFinderCore.Routes.Count));
            Console.WriteLine(String.Format("📫  {0}/{1} requests returned a 'failure' status code.\n", Results.Where(x => FailStatusCodes.Contains(x.Result.StatusCode)).Count(), Results.Count));

            Results.Where(x => !FailStatusCodes.Contains(x.Result.StatusCode)).ForEach(x => Logger.Verbose("info", x.Result.URL, x.Result.Verb, x.Result.StatusCode.ToString()));
            Results.Where(x => FailStatusCodes.Contains(x.Result.StatusCode)).ForEach(x => Logger.Error(x.Result.URL, x.Result.Verb, x.Result.StatusCode.ToString()));

        }

        private static string RemoveIdTokenFromMapDefault(string uri)
        {
            return uri.Replace("/{id?}", "");
        }

        private static string ReplaceRouteToken(string uri)
        {

            Dictionary<string, string> constraintMap = new()
            {
                { "guid", "899d7017-5cb4-42e3-ba91-8c775185db09" },
                { "bool", "true" },
                { "int", "1" },
                { "datetime", "2016-12-31" },
                { "decimal", "49.99" },
                { "double", "1.234" },
                { "float", "1.234" },
                { "long", "1234" },
                { "alpha", "Rick" }
            };

            constraintMap.ForEach(x => uri = Regex.Replace(uri, "\\{.*?:" + x.Key + "\\}", x.Value));

            uri = RemoveIdTokenFromMapDefault(uri);

            return uri;
        }

        private static async Task<ResultData> SendRequest(string uri, string method)
        {
            string[] httpMethodsWithBody = { "POST", "PUT" };

            var request = new HttpRequestMessage();
            var httpMethodString = method.ToUpper();
            string allIdentifier = null;

            if (method == "*")
            {
                httpMethodString = "GET";
                allIdentifier = "*";
            }

            var httpMethod = new HttpMethod(httpMethodString);

            string URL = ServiceEndpoint + ReplaceRouteToken(uri);

            try
            {
                request = new HttpRequestMessage(httpMethod, URL);
            }
            catch (Exception e)
            {
                Logger.Error(String.Format("Encountered error when creating HTTP(s) request: {0}\n\t{1}", URL, e.Message));
                return default;
            }
            if (httpMethodsWithBody.Contains(method))
            {
                request.Content = JsonContent.Create(new { Name = "FakeData" });
            }

            httpMethodString = (!String.IsNullOrEmpty(allIdentifier)) ? httpMethodString + "*" : httpMethodString;

            try
            {
                var response = await Client.SendAsync(request);
                int statusCode = (int)response.StatusCode;
                return new ResultData(URL, statusCode, httpMethodString);
            }
            catch (Exception e)
            {
                Logger.Error("Invalid uri", String.Format("Encountered error when sending HTTP(s) request: {0}\n\t{1}", URL, e.Message));
            }

            return default;
        }
    }
}

