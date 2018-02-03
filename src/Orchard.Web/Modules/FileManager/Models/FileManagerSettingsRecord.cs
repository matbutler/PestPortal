using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Records;

namespace FileManager.Models
{
    public class FileManagerSettingsRecord : ContentPartRecord
    {
        public virtual string FileExtensions { get; set; }
        public virtual int SystemType { get; set; }
    }

    public class FileManagerSettingsPart : ContentPart<FileManagerSettingsRecord>
    {
        [Required]
        public string FileExtensions
        {
            get { return Record.FileExtensions; }
            set { Record.FileExtensions = value; }
        }

        [Required]
        public int SystemType
        {
            get { return Record.SystemType; }
            set { Record.SystemType = value; }
        }
    }


    public enum FileManagerSettingsTypes
    {
        none = 0,
        pictures = 1,
        video = 2,
        documents = 3,
    }

    

}