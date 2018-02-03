using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FileManager.Models;

namespace FileManager.ViewModels
{
    public class FileViewViewModel
    {
        public FileRecord File { get; set; }
        public string FilePath { get; set; }
        public DisplayType type { get; set; }
        public FileManagerSettingsTypes FileType { get; set; }
        public SearchFileDisplayParams SearchParams { get; set; }
    }

    public class SearchFileDisplayParams
    {
        public string UrlToGroup { get; set; }
        public string GroupName { get; set; }
    }

    public enum DisplayType
    {
        none = 0,
        inline = 1,
        elements = 2,
        edit = 3,
    }
}