using System.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FileManager.Models;
using System.Web.Mvc;


namespace FileManager.ViewModels
{

    public class DisplayModuleViewModel
    {
        public GroupRecord GroupRoot { get; set; }
        public List<FileDisplayEntry> Files { get; set; }
        public List<List<GroupRecord>> Groups {get; set;}
        public List<int> SelectedPath { get; set; }
        public bool AccessDenied { get; set; }
        public string SearchText { get; set; }
        public SelectedOrder SelectedOrder { get; set; }
        public bool HideGroups { get; set; }
        public bool HideFilterPanel { get; set; }
        public int? ShowType { get; set; }
        public GroupRecord SelectedGroup { get; set; }
    }

    public class SelectedOrder 
    {
        public bool Name { get; set; }
        public bool Size { get; set; }
        public bool CreateDate { get; set; }
        public bool UpdateDate { get; set; } 
    }

    public class FileDisplayEntry
    {
        public FileRecord File { get; set; }
        public string Path { get; set; }
        public FileManagerSettingsTypes FileType { get; set; }
        public SearchFileParams SearchParams { get; set; }
    }

    public class SearchFileParams
    {
        public int Gid { get; set; }
        public string PathToCurentRoot { get; set; }
    }
}
