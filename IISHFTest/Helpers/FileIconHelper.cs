namespace IISHFTest.Helpers
{
    public class FileIconHelper
    {
        private static readonly Dictionary<string, string> ExtensionIconMap = new Dictionary<string, string>
        {
            { "pdf", "fa-light fa-file-pdf " },
            { "doc", "fa-light fa-file-word " },
            { "docx", "fa-light fa-file-word " },
            { "xls", "ffa-sharp fa-light fa-file-excel " },
            { "xlsx", "ffa-sharp fa-light fa-file-excel " }
        };

        public static string GetIcon(string fileUrl)
        {
            string fileExtension = fileUrl.Split('.').Last().ToLower();
            return ExtensionIconMap.TryGetValue(fileExtension, out string icon) ? icon : string.Empty;
        }
    }
}
