using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static AssetStudioCLI.Studio;
using AssetStudio;
using CommandLine;

namespace AssetStudioCLI 
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var parser = new Parser(config =>
            {
                config.AutoHelp = true;
                config.AutoVersion = true;
                config.CaseInsensitiveEnumValues = true;
                config.HelpWriter = Console.Out;
            });
            parser.ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                try
                {
                    Exporter.RetryIfExist = o.Override;

                    if (o.Verbose)
                    {
                        Logger.Default = new ConsoleLogger();
                    }

                    var inputPath = o.Input;
                    var outputPath = o.Output;
                    var types = o.Type.ToArray();
                    var filtes = o.Filter.ToArray();

                    var files = Directory.Exists(inputPath) ? Directory.GetFiles(inputPath, $"*.*", SearchOption.AllDirectories) : new string[] { inputPath };

                    if (o.Map)
                    {
                        var assets = BuildAssetMap(files.ToList(), true);
                        ExportAssetsMap(outputPath, assets, ExportListType.XML);
                        exportableAssets.Clear();
                        assetsManager.Clear();
                    }
                    else
                    {
                        foreach (var file in files)
                        {
                            assetsManager.LoadFiles(file);
                            BuildAssetData(types, filtes);
                            ExportAssets(outputPath, exportableAssets);
                            exportableAssets.Clear();
                            assetsManager.Clear();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            });

        }
    }

    public class Options
    {
        [Option('v', "verbose", HelpText = "Show log messages.")]
        public bool Verbose { get; set; }
        [Option('t', "type", HelpText = "Specify unity type(s).")]
        public IEnumerable<ClassIDType> Type { get; set; }
        [Option('f', "filter", HelpText = "Specify regex filter(s).")]
        public IEnumerable<Regex> Filter { get; set; }
        [Option('m', "map", HelpText = "Build CABMap/AssetMap.")]
        public bool Map { get; set; }
        [Option('o', "override", HelpText = "Export assets even if name already exist.")]
        public bool Override { get; set; }
        [Value(0, Required = true, MetaName = "input_path", HelpText = "Input file/folder.")]
        public string Input { get; set; }
        [Value(1, Required = true, MetaName = "output_path", HelpText = "Output folder.")]
        public string Output { get; set; }
    }
}