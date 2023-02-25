namespace AssetStudio
{
    public enum ExportListType
    {
        XML
    }
    
    public static class ExportListTypeExtensions
    {
        public static string GetExtension(this ExportListType type)
        {
            switch (type)
            {
                case ExportListType.XML:
                    return ".xml";
                default:
                    throw new System.NotImplementedException();
            }
        }
    }
}
