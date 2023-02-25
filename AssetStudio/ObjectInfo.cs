using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    public class ObjectInfo
    {
        public long byteStart;
        public uint byteSize;
        public int typeID;
        public int classID;
        public ushort isDestroyed;
        public byte stripped;

        public long m_PathID;
        public SerializedType serializedType;

        public static List<ObjectInfo> Filter(List<ObjectInfo> objects) => objects.Where(x => x.IsExportableType()).OrderBy(x => ExportableTypes.IndexOf((ClassIDType)x.typeID)).ToList();

        private bool IsExportableType() => ExportableTypes.Contains((ClassIDType)classID);

        private readonly static List<ClassIDType> ExportableTypes = new List<ClassIDType> { ClassIDType.GameObject, ClassIDType.Material, ClassIDType.Texture2D, ClassIDType.Shader, ClassIDType.TextAsset, ClassIDType.Font, ClassIDType.Sprite };

    }
}
