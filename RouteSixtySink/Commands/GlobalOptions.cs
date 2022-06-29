/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using CommandLine;
using RouteSixtySink.Outputters;
using System;
using static System.Console;
using System.Collections.Generic;
using System.IO;
using RouteSixtySink.Helpers;
using CommandLine.Text;
using System.Linq;

namespace RouteSixtySink.Commands
{
    public class Options
    {
        // Universal arguments
        [Option('o', "output-directory", Default = "", Required = false, HelpText = "Provide path of location to write to. Default: ./Output.")]
        public string OutputDirectory { get; set; }

        [Option('v', "verbosity", Default = "VVEW", Required = false, HelpText = "Options - V,VV,E,W,D")]
        public string Verbosity { get; set; }

        [Option('n', "no-console", Default = false, Required = false, HelpText = "Yes or No")]
        public bool NoConsole { get; set; }

        [Option('o', "output-format", Default = "csv,json,log", Required = false, HelpText = "Options - csv,json,log,none")]
        public string OutputFormat { get; set; }

        [Option('l', "level", Default = -1, Required = false, HelpText = "Set the number of method calls to recurse for sinkfinder")]
        public int RecurseDepth { get; set; }

        [Option('s', "search-string", Default = "", Required = false, HelpText = "Search custom sink string")]
        public string CustomSinkString { get; set; }

        [Option('f', "sink-file", Default = "./sinks.json", Required = false, HelpText = "JSON file containing sinks to search for")]
        public string SinkFile { get; set; }

        [Option('r', "is-regex", Default = false, Required = false, HelpText = "Shows whether the custom query string is a regex")]
        public bool CustomQueryIsRegex { get; set; }

        public void ExecuteGlobals()
        {
            SinkFinder.SinkFinder.RecurseDepth = RecurseDepth;
            SinkFinder.SinkFinder.CustomSinkString = CustomSinkString;
            SinkFinder.SinkFinder.CustomQueryIsRegex = CustomQueryIsRegex;
            SinkFinder.SinkFinder.SinkFile = SinkFile;

            Logger.ConsoleEnabled = !NoConsole;

            if (!String.IsNullOrEmpty(OutputFormat))
            {
                if (!Directory.Exists(Writer.OutputDirectory))
                {
                    Directory.CreateDirectory(Writer.OutputDirectory);
                }

                var formats = new Dictionary<string, Action>()
                    {
                        { "log", () => { Writer.LogOutputToFile = true;} },
                        { "csv", () => { Writer.CSVOutputToFile = true;} },
                        { "none", () => { Writer.CSVOutputToFile = false; Writer.LogOutputToFile = false;} },
                    };
                foreach (var entry in formats.Where(entry => OutputFormat.ToString().ToLower().Contains(entry.Key)))
                {
                    formats[entry.Key]();
                }
            }
            Logger.Verbosity = String.IsNullOrEmpty(Verbosity) ? Logger.Verbosity : Verbosity;
        }

        public static void HandleParseError(IEnumerable<Error> errs, ParserResult<object> CommandParser)
        {
            foreach (var err in errs)
            {
                switch (err.Tag.ToString())
                {
                    default:
                        Logger.Error(String.Format("Unhandled exception occured at startup {0}", err.Tag.ToString()));
                        WriteLine("\n" + HelpText.AutoBuild(CommandParser, error => error, ex => ex, true));
                        break;
                    case "BadVerbSelectedError":
                        Logger.Error(String.Format("Invalid verb passed to command line: {0}", ReflectionHelpers.GetPropValue(err, "Token")));
                        WriteLine("\n" + HelpText.AutoBuild(CommandParser, error => error, ex => ex, true, 200));
                        break;
                    case "MissingRequiredOptionError":
                        dynamic NameInfo = ReflectionHelpers.GetPropValue(err, "NameInfo");
                        Logger.Error(String.Format(String.Format("Missing required argument: (-{0}) --{1}", NameInfo.ShortName, NameInfo.LongName)));
                        WriteLine("\n" + HelpText.AutoBuild(CommandParser, error => error, ex => ex));
                        break;
                    case "HelpVerbRequestedError":
                        WriteLine("\n" + HelpText.AutoBuild(CommandParser, error => error, ex => ex));
                        break;
                    case "HelpRequestedError":
                        WriteLine("\n" + HelpText.AutoBuild(CommandParser, error => error, ex => ex, true));
                        break;
                    case "VersionRequestedError":
                        WriteLine("\n" + HelpText.AutoBuild(CommandParser, error => error, ex => ex, true));
                        break;
                }
            }
        }
    }
}
