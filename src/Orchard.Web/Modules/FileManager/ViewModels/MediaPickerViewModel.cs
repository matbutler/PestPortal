using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web;
using FileManager.Models;

namespace FileManager.ViewModels
{
    public class MediaPickerViewModel
    {
        public List<GroupEntry> GroupRecords { get; set; }
        public List<FilePickerEntry> FileRecords { get; set; }
        public List<GroupRecord> BreadRecord { get; set; }
        //public UserIndexOptions Options { get; set; }
        //public dynamic Pager { get; set; }
        public int Gid { get; set; }
        public bool UploadUrl { get; set; }
    }

    //public class UserIndexOptions
    //{
    //    public string SearchText { get; set; }
    //    public FileGroupsAdminIndexOrderBy OrderBy { get; set; }
    //}



    public class FilePickerEntry
    {
        public FileRecord File { get; set; }
        public string PublicPath { get; set; }
        public FileManagerSettingsTypes FileType { get; set; }
    }


}