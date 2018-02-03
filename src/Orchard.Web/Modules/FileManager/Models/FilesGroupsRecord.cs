using System;
using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Records;

namespace FileManager.Models
{
    public class FilesGroupsRecord : ContentPartRecord
    {
        public virtual FileRecord FileRecord { get; set; }
        public virtual GroupRecord GroupRecord { get; set; }
    }



    public class FilesGroupsPart : ContentPart<FilesGroupsRecord>
    {
        public FileRecord FileRecord 
        {
            get { return Record.FileRecord; }
            set { Record.FileRecord = value; } 
        }

        public GroupRecord GroupRecord
        {
            get { return Record.GroupRecord; }
            set { Record.GroupRecord = value; }
        }

    }
}
