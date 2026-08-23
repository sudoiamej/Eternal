using System;

namespace Eternal.Models
{
    public class FileItemModel
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public long SizeBytes { get; set; }
        public string SizeFormatted { get; set; } = string.Empty;
        public DateTime DateModified { get; set; }
        public string Extension { get; set; } = string.Empty;
        public string IconName { get; set; } = "FileOutline";
        public string IconColor { get; set; } = "#888896";
        public bool IsHidden { get; set; }
        public bool IsSystem { get; set; }
        public bool IsReadOnly { get; set; }
        public string ItemType { get; set; } = "File";
    }
}
