using AssetStudio;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static AssetStudioCLI.Exporter;

namespace AssetStudioCLI
{
    public enum ExportListType
    {
        XML
    }

    internal static class Studio
    {
        public static AssetsManager assetsManager = new AssetsManager();
        public static List<AssetItem> exportableAssets = new List<AssetItem>();

        public static int ExtractFolder(string path, string savePath)
        {
            int extractedCount = 0;
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var fileOriPath = Path.GetDirectoryName(file);
                var fileSavePath = fileOriPath.Replace(path, savePath);
                extractedCount += ExtractFile(file, fileSavePath);
            }
            return extractedCount;
        }

        public static int ExtractFile(string[] fileNames, string savePath)
        {
            int extractedCount = 0;
            for (var i = 0; i < fileNames.Length; i++)
            {
                var fileName = fileNames[i];
                extractedCount += ExtractFile(fileName, savePath);
            }
            return extractedCount;
        }

        public static int ExtractFile(string fileName, string savePath)
        {
            int extractedCount = 0;
            var reader = new FileReader(fileName);
            if (reader.FileType == FileType.BundleFile)
                extractedCount += ExtractBundleFile(reader, savePath);
            else if (reader.FileType == FileType.WebFile)
                extractedCount += ExtractWebDataFile(reader, savePath);
            else
                reader.Dispose();
            return extractedCount;
        }

        private static int ExtractBundleFile(FileReader reader, string savePath)
        {
            Logger.Info($"Decompressing {reader.FileName} ...");
            var bundleFile = new BundleFile(reader);
            reader.Dispose();
            if (bundleFile.fileList.Length > 0)
            {
                var extractPath = Path.Combine(savePath, reader.FileName + "_unpacked");
                return ExtractStreamFile(extractPath, bundleFile.fileList);
            }
            return 0;
        }

        private static int ExtractWebDataFile(FileReader reader, string savePath)
        {
            Logger.Info($"Decompressing {reader.FileName} ...");
            var webFile = new WebFile(reader);
            reader.Dispose();
            if (webFile.fileList.Length > 0)
            {
                var extractPath = Path.Combine(savePath, reader.FileName + "_unpacked");
                return ExtractStreamFile(extractPath, webFile.fileList);
            }
            return 0;
        }

        private static int ExtractStreamFile(string extractPath, StreamFile[] fileList)
        {
            int extractedCount = 0;
            foreach (var file in fileList)
            {
                var filePath = Path.Combine(extractPath, file.path);
                var fileDirectory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(fileDirectory))
                {
                    Directory.CreateDirectory(fileDirectory);
                }
                if (!File.Exists(filePath))
                {
                    using (var fileStream = File.Create(filePath))
                    {
                        file.stream.CopyTo(fileStream);
                    }
                    extractedCount += 1;
                }
                file.stream.Dispose();
            }
            return extractedCount;
        }

        public static List<AssetEntry> BuildAssetMap(List<string> files, bool buildCABMap = false)
        {
            List<AssetEntry> assets = new List<AssetEntry>();
            Dictionary<long, string> NameLUT = new Dictionary<long, string>();
            List<(int, long)> PPtrLUT = new List<(int, long)>();
            int collisions = 0;

            if (buildCABMap)
            {
                Logger.Info(string.Format("Building PGRMap"));
                CABManager.CABMap.Clear();
            }

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
                                var assetsFile = new SerializedFile(cabReader, null);
                                if (buildCABMap)
                                {
                                    if (CABManager.CABMap.ContainsKey(cab.path))
                                    {
                                        collisions++;
                                        continue;
                                    }
                                    CABManager.CABMap.Add(cab.path, file);
                                }
                                var objects = assetsFile.m_Objects.Where(x => x.HasExportableType()).ToArray();
                                foreach (var obj in objects)
                                {
                                    var objectReader = new ObjectReader(assetsFile.reader, assetsFile, obj);
                                    objectReader.Reset();
                                    var asset = new AssetEntry()
                                    {
                                        SourcePath = reader.FullPath,
                                        PathID = objectReader.m_PathID,
                                        Type = objectReader.type
                                    };
                                    var exportable = true;
                                    switch (objectReader.type)
                                    {
                                        case ClassIDType.GameObject:
                                            var gameObject = new GameObject(objectReader);
                                            if (!NameLUT.ContainsKey(objectReader.m_PathID))
                                            {
                                                NameLUT.Add(objectReader.m_PathID, gameObject.m_Name);
                                            }
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
                                            var gameObjectPPtr = new PPtr<GameObject>(objectReader);
                                            PPtrLUT.Add((assets.Count, gameObjectPPtr.m_PathID));
                                            asset.Name = "AnimatorPlaceholder";
                                            break;
                                        default:
                                            asset.Name = objectReader.ReadAlignedString();
                                            break;
                                    }
                                    if (exportable)
                                    {
                                        assets.Add(asset);
                                    }
                                }
                            }
                        }
                    }
                }

                Logger.Info($"[{i + 1}/{files.Count}] Processed {Path.GetFileName(file)}");
            }

            if (buildCABMap)
            {
                CABManager.CABMap = CABManager.CABMap.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
                var outputFile = new FileInfo($"PGRMap.bin");

                if (!outputFile.Directory.Exists)
                    outputFile.Directory.Create();

                using (var binaryFile = outputFile.Create())
                using (var writer = new BinaryWriter(binaryFile))
                {
                    writer.Write(CABManager.CABMap.Count);
                    foreach (var cab in CABManager.CABMap)
                    {
                        writer.Write(cab.Key);
                        writer.Write(cab.Value);
                    }
                }
                Logger.Info($"PGRMap build successfully, {collisions} collisions found !!");
            }

            Logger.Info("Processing PPtr names");
            foreach (var pptr in PPtrLUT)
            {
                var asset = assets[pptr.Item1];
                if (NameLUT.TryGetValue(pptr.Item2, out var name))
                {
                    asset.Name = name;
                }
                else
                {
                    Logger.Warning($"No name found for pathID {pptr.Item2}, removing...");
                    assets.Remove(asset);
                }
            }

            return assets;
        }

        public static void BuildAssetData(ClassIDType[] formats, Regex[] filters)
        {
            string productName = null;
            var objectCount = assetsManager.assetsFileList.Sum(x => x.Objects.Count);
            var objectAssetItemDic = new Dictionary<AssetStudio.Object, AssetItem>(objectCount);
            var containers = new List<(PPtr<AssetStudio.Object>, string)>();
            int i = 0;
            foreach (var assetsFile in assetsManager.assetsFileList)
            {
                foreach (var asset in assetsFile.Objects)
                {
                    var assetItem = new AssetItem(asset);
                    objectAssetItemDic.Add(asset, assetItem);
                    assetItem.UniqueID = " #" + i;
                    assetItem.Text = "";
                    var exportable = false;
                    switch (asset)
                    {
                        case GameObject m_GameObject:
                            assetItem.Text = m_GameObject.m_Name;
                            break;
                        case Texture2D m_Texture2D:
                            if (!string.IsNullOrEmpty(m_Texture2D.m_StreamData?.path))
                                assetItem.FullSize = asset.byteSize + m_Texture2D.m_StreamData.size;
                            assetItem.Text = m_Texture2D.m_Name;
                            exportable = true;
                            break;
                        case AudioClip m_AudioClip:
                            if (!string.IsNullOrEmpty(m_AudioClip.m_Source))
                                assetItem.FullSize = asset.byteSize + m_AudioClip.m_Size;
                            assetItem.Text = m_AudioClip.m_Name;
                            break;
                        case VideoClip m_VideoClip:
                            if (!string.IsNullOrEmpty(m_VideoClip.m_OriginalPath))
                                assetItem.FullSize = asset.byteSize + (long)m_VideoClip.m_ExternalResources.m_Size;
                            assetItem.Text = m_VideoClip.m_Name;
                            break;
                        case Shader m_Shader:
                            assetItem.Text = m_Shader.m_ParsedForm?.m_Name ?? m_Shader.m_Name;
                            exportable = true;
                            break;
                        case Mesh _:
                        case TextAsset _:
                        case AnimationClip _:
                        case Font _:
                        case MovieTexture _:
                        case Sprite _:
                            assetItem.Text = ((NamedObject)asset).m_Name;
                            exportable = true;
                            break;
                        case Animator m_Animator:
                            if (m_Animator.m_GameObject.TryGet(out var gameObject))
                            {
                                assetItem.Text = gameObject.m_Name;
                            }
                            break;
                        case MonoBehaviour m_MonoBehaviour:
                            if (m_MonoBehaviour.m_Name == "" && m_MonoBehaviour.m_Script.TryGet(out var m_Script))
                            {
                                assetItem.Text = m_Script.m_ClassName;
                            }
                            else
                            {
                                assetItem.Text = m_MonoBehaviour.m_Name;
                            }
                            break;
                        case PlayerSettings m_PlayerSettings:
                            productName = m_PlayerSettings.productName;
                            break;
                        case AssetBundle m_AssetBundle:
                            foreach (var m_Container in m_AssetBundle.m_Container)
                            {
                                var preloadIndex = m_Container.Value.preloadIndex;
                                var preloadSize = m_Container.Value.preloadSize;
                                var preloadEnd = preloadIndex + preloadSize;
                                for (int k = preloadIndex; k < preloadEnd; k++)
                                {
                                    containers.Add((m_AssetBundle.m_PreloadTable[k], m_Container.Key));
                                }
                            }
                            assetItem.Text = m_AssetBundle.m_Name;
                            break;
                        case ResourceManager m_ResourceManager:
                            foreach (var m_Container in m_ResourceManager.m_Container)
                            {
                                containers.Add((m_Container.Value, m_Container.Key));
                            }
                            break;
                        case NamedObject m_NamedObject:
                            assetItem.Text = m_NamedObject.m_Name;
                            exportable = true;
                            break;
                    }
                    if (assetItem.Text == "")
                    {
                        assetItem.Text = assetItem.TypeString + assetItem.UniqueID;
                    }
                    var isMatchRegex = filters.Length > 0 ? filters.Any(x => x.IsMatch(assetItem.Text)) : true;
                    var isFilteredType = formats.Length > 0 ? formats.Contains(assetItem.Asset.type) : true;
                    if (isMatchRegex && isFilteredType && exportable)
                    {
                        exportableAssets.Add(assetItem);
                    }
                }
            }
            foreach ((var pptr, var container) in containers)
            {
                if (pptr.TryGet(out var obj))
                {
                    objectAssetItemDic[obj].Container = container;
                }
            }
            containers.Clear();
        }

        public static void ExportAssets(string savePath, List<AssetItem> toExportAssets)
        {
            int toExportCount = toExportAssets.Count;
            int exportedCount = 0;
            foreach (var asset in toExportAssets)
            {
                string exportPath;
                exportPath = Path.Combine(savePath, asset.TypeString);
                exportPath += Path.DirectorySeparatorChar;
                Logger.Info($"[{exportedCount}/{toExportCount}] Exporting {asset.TypeString}: {asset.Text}");
                try
                {
                    if (ExportConvertFile(asset, exportPath))
                    {
                        exportedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Export {asset.Type}:{asset.Text} error\r\n{ex.Message}\r\n{ex.StackTrace}");
                }
            }

            var statusText = exportedCount == 0 ? "Nothing exported." : $"Finished exporting {exportedCount} assets.";

            if (toExportCount > exportedCount)
            {
                statusText += $" {toExportCount - exportedCount} assets skipped (not extractable or files already exist)";
            }

            Logger.Info(statusText);
        }

        public static void ExportAssetsMap(string savePath, List<AssetEntry> toExportAssets, ExportListType exportListType)
        {
            string filename;
            switch (exportListType)
            {
                case ExportListType.XML:
                    filename = Path.Combine(savePath, "assets_map.xml");
                    var doc = new XDocument(
                        new XElement("Assets",
                            new XAttribute("filename", filename),
                            new XAttribute("createdAt", DateTime.UtcNow.ToString("s")),
                            toExportAssets.Select(
                                asset => new XElement("Asset",
                                    new XElement("Name", asset.Name),
                                    new XElement("Type", new XAttribute("id", (int)asset.Type), asset.Type.ToString()),
                                    new XElement("PathID", asset.PathID),
                                    new XElement("Source", asset.SourcePath)
                                )
                            )
                        )
                    );
                    doc.Save(filename);
                    break;
            }

            var statusText = $"Finished exporting asset list with {toExportAssets.Count()} items.";

            Logger.Info(statusText);

            Logger.Info($"AssetMap build successfully !!");
        }
    }
}
