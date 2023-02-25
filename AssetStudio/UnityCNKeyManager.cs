using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

namespace AssetStudio
{
    public static class CNUnityKeyManager
    {
        public const string KeysFileName = "Keys.json";

        private static List<CNUnity.Entry> Entries = new List<CNUnity.Entry>();

        static CNUnityKeyManager()
        {
            var str = File.ReadAllText(KeysFileName);
            Entries = JsonConvert.DeserializeObject<List<CNUnity.Entry>>(str);
        }

        public static void SaveEntries(List<CNUnity.Entry> entries)
        {
            Entries.Clear();
            Entries.AddRange(entries);

            var str = JsonConvert.SerializeObject(Entries);
            File.WriteAllText(KeysFileName, str);
        }

        public static void SetKey(int index)
        {
            if (TryGetEntry(index, out var cnunity))
            {
                if (CNUnity.SetKey(cnunity))
                {
                    Logger.Info($"[CNUnity] Selected Key is {cnunity}");
                }
                else
                {
                    Logger.Info($"[CNUnity] No Key is selected !!");
                }
            }
            else
            {
                Logger.Error("Invalid Key !!");
                Logger.Warning(GetEntries().Select(x => x.ToString()).ToString());
            }
        }

        public static bool TryGetEntry(int index, out CNUnity.Entry key)
        {
            try
            {
                if (index < 0 || index > Entries.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                key = Entries[index];
            }
            catch(Exception e)
            {
                Logger.Error($"[CNUnity] Invalid Index, check if list is not empty !!\n{e.Message}");
                key = null;
                return false;
            }

            return true;
        }
        public static CNUnity.Entry[] GetEntries() => Entries.ToArray();

        public new static string ToString() => string.Join("\n", GetEntries().Select((x, i) => $"{i}: {x.Name}"));
    }
}
