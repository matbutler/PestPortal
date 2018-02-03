using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web;
using FileManager.Models;

namespace FileManager.ViewModels
{
    public class FileGroupIndexViewModel
    {
        public List<GroupEntry> GroupRecords { get; set; }
        public List<FileEntry> FileRecords { get; set; }
        public List<GroupRecord> BreadRecord { get; set; }
        public FileGroupsAdminIndexBulkAction BulkAction { get; set; }
        public UserIndexOptions Options { get; set; }
        public dynamic Pager { get; set; }
        public int Gid { get; set; }
        public List<SelectListItem> PasteGroupList { get; set; }
        public int? SelectedPasteGroup { get; set; }
        public bool CopySelected { get; set; }
        public bool SelectAll { get; set; }
        public bool DeselectAll { get; set; }
        public int TotalItemsCount { get; set; }

        public bool SelectAllChanged { get; set; }
    }

    public class SelectedEntries
    {
        public string SelectedGroupIds { get; set; }
        public string SelectedFileIds { get; set; }
    }

    public class UserIndexOptions
    {
        public string SearchText { get; set; }
        public FileGroupsAdminIndexOrderBy OrderBy { get; set; }
        public bool OrderDesc { get; set; }
    }



    public class FileEntry
    {
        public FileRecord File { get; set; }
        public bool IsChecked { get; set; }
        public int parentGid { get; set; }
        public SearchFileDisplayParams SearchParams { get; set; }
    }

    public class GroupEntry
    {
        public GroupRecord Group { get; set; }
        public bool IsChecked { get; set; }
        public long Size { get; set; }
        public int parentGid { get; set; }
        public SearchFileDisplayParams SearchParams { get; set; }
    }

    public enum FileGroupsAdminIndexBulkAction
    {
        None,
        Delete,
        Copy,
        Publish,
        Unpublish
    }

    public enum FileGroupsAdminIndexOrderBy
    {
        Name,
        CreateDate,
        UpdateDate,
        Size,
    }
}