using System;
using System.IO;
using System.Linq;
using AssetStudio;
using static AssetStudioCLI.Studio;

namespace AssetStudioCLI
{
    public class Program
    {
        public static void Main(string[] args) => CommandLine.Init(args);

        public static void Run(Options o)
        {
            try
            {
                if (!CNUnityKeyManager.TryGetEntry(o.KeyIndex, out var cnunity))
                {
                    Console.WriteLine("Invalid key index !!");
                    Console.WriteLine($"Available Options: \n{CNUnityKeyManager.ToString()}");
                    return;
                }

                CNUnity.SetKey(cnunity);
                Logger.Info($"[CNUnity] Selected Key is {cnunity}");

                if (!o.Silent)
                {
                    Logger.Default = new ConsoleLogger();
                }

                Logger.Info("Scanning for files");
                var files = o.Input.Attributes == FileAttributes.Directory ? Directory.GetFiles(o.Input.FullName, $"*.*", SearchOption.AllDirectories).OrderBy(x => x.Length).ToArray() : new string[] { o.Input.FullName };
                Logger.Info(string.Format("Found {0} file(s)", files.Count()));

                if (o.MapOp.Equals(MapOpType.None))
                {
                    var i = 0;
                    foreach (var file in files)
                    {
                        assetsManager.LoadFiles(file);
                        if (assetsManager.assetsFileList.Count > 0)
                        {
                            BuildAssetData(o.TypeFilter, o.NameFilter, o.ContainerFilter, ref i);
                            ExportAssets(o.Output.FullName, exportableAssets, o.GroupAssetsType);
                        }
                        exportableAssets.Clear();
                        assetsManager.Clear();
                    }
                }
                if (o.MapOp.HasFlag(MapOpType.CABMap))
                {
                    AssetsHelper.BuildCABMap(files, o.CABName);
                }
                if (o.MapOp.HasFlag(MapOpType.AssetMap))
                {
                    if (files.Length == 1)
                    {
                        throw new Exception("Unable to build AssetMap with input_path as a file !!");
                    }
                    var assets = AssetsHelper.BuildAssetMap(files, o.TypeFilter, o.NameFilter, o.ContainerFilter);
                    if (!o.Output.Exists)
                    {
                        o.Output.Create();
                    }
                    AssetsHelper.ExportAssetsMap(assets, o.MapName, o.Output.FullName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}