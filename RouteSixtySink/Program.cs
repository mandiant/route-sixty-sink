/* Copyright (C) 2022 Mandiant, Inc. All Rights Reserved. */

using System;
using CommandLine;
using RouteSixtySink.Commands;

namespace RouteSixtySink
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Constants.AsciiArt + "\n");

            Parser CommandParser = new(settings => settings.IgnoreUnknownArguments = true);
            ParserResult<object> result = CommandParser
                .ParseArguments<
                    RunRFASPNETCommand,
                    RunRFPagesCommand,
                    RunSinkFinderCommand,
                    RunDiscoveryCommand
                >(args)
                .WithParsed<ICommand>(t => t.Execute() );
            result.WithNotParsed(errs => Options.HandleParseError(errs, result));
        }
    }
}
