using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace AssetStudio
{
    public static class CABManager
    {
        public static Dictionary<string, string> CABMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void BuildPGRMap(List<string> files)
        {
            Logger.Info(string.Format("Building PGRMap"));
            try
            {
                int collisions = 0;
                CABMap.Clear();
                Progress.Reset();
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    using (var reader = new FileReader(file))
                    {
                        var bundleFile = new BundleFile(reader);
                        if (bundleFile.fileList == null)
                        {
                            continue;
                        }
                        foreach (var cab in bundleFile.fileList)
                        {
                            var dummyPath = Path.Combine(Path.GetDirectoryName(reader.FullPath), cab.fileName);
                            using (var cabReader = new FileReader(dummyPath, cab.stream))
                            {
                                if (cabReader.FileType == FileType.AssetsFile)
                                {
                                    if (CABMap.ContainsKey(cab.path))
                                    {
                                        collisions++;
                                        continue;
                                    }
                                    var assetsFile = new SerializedFile(cabReader, null);
                                    var dependencies = assetsFile.m_Externals.Select(x => x.fileName).ToList();
                                    CABMap.Add(cab.path, file);
                                }
                            }
                        }
                    }
                    Logger.Info($"[{i + 1}/{files.Count}] Processed {Path.GetFileName(file)}");
                    Progress.Report(i + 1, files.Count);
                }

                CABMap = CABMap.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
                var outputFile = new FileInfo($"PGRMap.bin");

                using (var binaryFile = outputFile.Create())
                using (var writer = new BinaryWriter(binaryFile))
                {
                    writer.Write(CABMap.Count);
                    foreach (var cab in CABMap)
                    {
                        writer.Write(cab.Key);
                        writer.Write(cab.Value);
                    }
                }
                Logger.Info($"PGRMap build successfully, {collisions} collisions found !!");
            }
            catch (Exception e)
            {
                Logger.Warning($"PGRMap was not build, {e.Message}");
            }
        }
        public static void LoadPGRMap()
        {
            Logger.Info(string.Format("Loading PGRMap"));
            try
            {
                CABMap.Clear();
                using (var binaryFile = File.OpenRead("PGRMap.bin"))
                using (var reader = new BinaryReader(binaryFile))
                {
                    var count = reader.ReadInt32();
                    CABMap = new Dictionary<string, string>(count, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < count; i++)
                    {
                        var cab = reader.ReadString();
                        var path = reader.ReadString();
                        CABMap.Add(cab, path);
                    }
                }
                Logger.Info(string.Format("Loaded PGRMap !!"));
            }
            catch (Exception e)
            {
                Logger.Warning($"PGRMap was not loaded, {e.Message}");
            }
        }
    }
}
