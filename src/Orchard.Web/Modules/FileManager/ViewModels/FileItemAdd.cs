using System.Collections.Generic;
using FileManager.Models;

namespace FileManager.ViewModels 
{
    public class FileItemAddViewModel
    {
        public int Gid { get; set; }
        public long MaxRequestLength { get; set; }
        public bool ExtractZip { get; set; }
        public List<GroupRecord> BreadRecord { get; set; }
        public string UploadAllowedFileTypeWhitelist { get; set; }
    }
}
