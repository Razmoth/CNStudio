using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace AssetStudio
{
    public static class AssetsHelper
    {
        public const string CABMapName = "Maps";

        public static CancellationTokenSource tokenSource = new CancellationTokenSource();

        private static Dictionary<string, string> CABMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static AssetsManager assetsManager = new AssetsManager() { Silent = true, SkipProcess = true, ResolveDependencies = false };

        public static bool TryGet(string key, out string value) => CABMap.TryGetValue(key, out value);

        public static string[] GetMaps()
        {
            Directory.CreateDirectory(CABMapName);
            var files = Directory.GetFiles(CABMapName, "*.bin", SearchOption.TopDirectoryOnly);
            return files.Select(x => Path.GetFileNameWithoutExtension(x)).ToArray();
        }

        public static void BuildCABMap(string[] files, string mapName)
        {
            Logger.Info($"Processing...");
            try
            {
                CABMap.Clear();
                var collision = 0;
                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];
                    assetsManager.LoadFiles(file);
                    if (assetsManager.assetsFileList.Count > 0)
                    {
                        foreach (var assetsFile in assetsManager.assetsFileList)
                        {
                            if (tokenSource.IsCancellationRequested)
                            {
                                Logger.Info("Building CABMap has been aborted !!");
                                return;
                            }
                            var dependencies = assetsFile.m_Externals.Select(x => x.fileName).ToArray();

                            if (CABMap.ContainsKey(assetsFile.fileName))
                            {
                                collision++;
                                continue;
                            }
                            CABMap.Add(assetsFile.fileName, file);
                        }
                        Logger.Info($"Processed {Path.GetFileName(file)}");
                    }
                    assetsManager.Clear();
                }

                CABMap = CABMap.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
                var outputFile = Path.Combine(CABMapName, $"{mapName}.bin");

                Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

                using (var binaryFile = File.OpenWrite(outputFile))
                using (var writer = new BinaryWriter(binaryFile))
                {
                    writer.Write(CABMap.Count);
                    foreach (var kv in CABMap)
                    {
                        writer.Write(kv.Key);
                        writer.Write(kv.Value);
                    }
                }

                Logger.Info($"CABMap build successfully !! {collision} collisions found");
            }
            catch (Exception e)
            {
                Logger.Warning($"CABMap was not build, {e}");
            }
        }

        public static void LoadMap(string mapName)
        {
            Logger.Info($"Loading {mapName}");
            try
            {
                CABMap.Clear();
                using (var fs = File.OpenRead(Path.Combine(CABMapName, $"{mapName}.bin")))
                using (var reader = new BinaryReader(fs))
                {
                    var count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        var cab = reader.ReadString();
                        var path = reader.ReadString();
                        CABMap.Add(cab, path);
                    }
                }
                Logger.Info($"Loaded {mapName} !!");
            }
            catch (Exception e)
            {
                Logger.Warning($"{mapName} was not loaded, {e}");
            }
        }

        public static AssetEntry[] BuildAssetMap(string[] files, ClassIDType[] typeFilters = null, Regex[] nameFilters = null, Regex[] containerFilters = null)
        {
            var assets = new List<AssetEntry>();
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                assetsManager.LoadFiles(file);
                if (assetsManager.assetsFileList.Count > 0)
                {
                    var containers = new List<(PPtr<Object>, string)>();
                    var mihoyoBinDataNames = new List<(PPtr<Object>, string)>();
                    var objectAssetItemDic = new Dictionary<Object, AssetEntry>();
                    var animators = new List<(PPtr<Object>, AssetEntry)>();
                    foreach (var assetsFile in assetsManager.assetsFileList)
                    {
                        assetsFile.m_Objects = ObjectInfo.Filter(assetsFile.m_Objects);

                        foreach (var objInfo in assetsFile.m_Objects)
                        {
                            var objectReader = new ObjectReader(assetsFile.reader, assetsFile, objInfo);
                            var obj = new Object(objectReader);
                            var asset = new AssetEntry()
                            {
                                Source = file,
                                PathID = objectReader.m_PathID,
                                Type = objectReader.type,
                                Container = ""
                            };
                        
                            var exportable = true;
                            switch (objectReader.type)
                            {
                                case ClassIDType.AssetBundle:
                                    var assetBundle = new AssetBundle(objectReader);
                                    foreach (var m_Container in assetBundle.m_Container)
                                    {
                                        var preloadIndex = m_Container.Value.preloadIndex;
                                        var preloadSize = m_Container.Value.preloadSize;
                                        var preloadEnd = preloadIndex + preloadSize;
                                        for (int k = preloadIndex; k < preloadEnd; k++)
                                        {
                                            containers.Add((assetBundle.m_PreloadTable[k], m_Container.Key));
                                        }
                                    }
                                    obj = null;
                                    asset.Name = assetBundle.m_Name;
                                    exportable = false;
                                    break;
                                case ClassIDType.GameObject:
                                    var gameObject = new GameObject(objectReader);
                                    obj = gameObject;
                                    asset.Name = gameObject.m_Name;
                                    exportable = false;
                                    break;
                                case ClassIDType.Shader:
                                    asset.Name = objectReader.ReadAlignedString();
                                    if (string.IsNullOrEmpty(asset.Name))
                                    {
                                        var m_parsedForm = new SerializedShader(objectReader);
                                        asset.Name = m_parsedForm.m_Name;
                                    }
                                    break;
                                case ClassIDType.Animator:
                                    var component = new PPtr<Object>(objectReader);
                                    animators.Add((component, asset));
                                    break;
                                default:
                                    asset.Name = objectReader.ReadAlignedString();
                                    break;
                            }
                            if (obj != null)
                            {
                                objectAssetItemDic.Add(obj, asset);
                                assetsFile.AddObject(obj);
                            }
                            var isMatchRegex = nameFilters.IsNullOrEmpty() || nameFilters.Any(x => x.IsMatch(asset.Name) || asset.Type == ClassIDType.Animator);
                            var isFilteredType = typeFilters.IsNullOrEmpty() || typeFilters.Contains(asset.Type) || asset.Type == ClassIDType.Animator;
                            if (isMatchRegex && isFilteredType && exportable)
                            {
                                assets.Add(asset);
                            }
                        }
                    }
                    foreach ((var pptr, var asset) in animators)
                    {
                        if (pptr.TryGet<GameObject>(out var gameObject) && (nameFilters.IsNullOrEmpty() || nameFilters.Any(x => x.IsMatch(gameObject.m_Name))) && (typeFilters.IsNullOrEmpty() || typeFilters.Contains(asset.Type)))
                        {
                            asset.Name = gameObject.m_Name;
                        }
                    }
                    foreach ((var pptr, var container) in containers)
                    {
                        if (pptr.TryGet(out var obj))
                        {
                            var item = objectAssetItemDic[obj];
                            if (containerFilters.IsNullOrEmpty() || containerFilters.Any(x => x.IsMatch(container)))
                            {
                                item.Container = container;
                            }
                            else
                            {
                                assets.Remove(item);
                            }
                        }
                    }
                    Logger.Info($"Processed {Path.GetFileName(file)}");
                }
                assetsManager.Clear();
            }
            return assets.ToArray();
        }

        public static void ExportAssetsMap(AssetEntry[] toExportAssets, string name, string savePath)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

                Progress.Reset();

                string filename = Path.Combine(savePath, $"{name}.xml");
                var doc = new XDocument(
                    new XElement("Assets",
                        new XAttribute("filename", filename),
                        new XAttribute("createdAt", DateTime.UtcNow.ToString("s")),
                        toExportAssets.Select(
                            asset => new XElement("Asset",
                                new XElement("Name", asset.Name),
                                new XElement("Container", asset.Container),
                                new XElement("Type", new XAttribute("id", (int)asset.Type), asset.Type.ToString()),
                                new XElement("PathID", asset.PathID),
                                new XElement("Source", asset.Source)
                            )
                        )
                    )
                );
                doc.Save(filename);

                Logger.Info($"Finished exporting asset list with {toExportAssets.Length} items.");
                Logger.Info($"AssetMap build successfully !!");
            });
        }
    }
}
