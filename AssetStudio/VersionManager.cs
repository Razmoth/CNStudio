using System.Collections.Generic;
using System.Linq;

namespace AssetStudio
{
    public static class VersionManager
    {
        public static Dictionary<int, string> Versions = new Dictionary<int, string>();

        static VersionManager()
        {
            Versions.Add(0, "Global/KR");
            Versions.Add(1, "CN/JP/TW");
        }

        public static string GetVersion(int ver)
        {
            if (Versions.TryGetValue(ver, out var version))
            {
                return version;
            }

            return null;
        }
        public static string[] GetVersionNames() => Versions.Values.ToArray();
        public static string SupportedVersions() => $"Supported Versions:\n{string.Join("\n", Versions.Select(x => $"{x.Key} ({x.Value})"))}";
    }
}
