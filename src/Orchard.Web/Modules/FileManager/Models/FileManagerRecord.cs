using System;
using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Records;

namespace FileManager.Models
{
    public class FileManagerRecord : ContentPartRecord
    {
        public virtual GroupRecord ParentGroup { get; set; }
        public virtual int? ShowType { get; set; }
        public virtual bool? HideGroups { get; set; }
        public virtual bool? HideFilterPanel { get; set; } 
    }

    public class FileManagerPart : ContentPart<FileManagerRecord>
    {
        [Required]
        public GroupRecord ParentGroup
        {
            get { return Record.ParentGroup; }
            set { Record.ParentGroup = value; }
        }

        [Required]
        public int? ShowType
        {
            get { return Record.ShowType; }
            set { Record.ShowType = value; }
        }

        [Required]
        public bool? HideGroups
        {
            get { return Record.HideGroups; }
            set { Record.HideGroups = value; }
        }

        [Required]
        public bool? HideFilterPanel
        {
            get { return Record.HideFilterPanel; }
            set { Record.HideFilterPanel = value; }
        }
    }
}