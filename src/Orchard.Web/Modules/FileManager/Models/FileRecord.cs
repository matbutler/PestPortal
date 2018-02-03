using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Records;
//using Orchard.Users.Models;


namespace FileManager.Models
{
    public class FileRecord : ContentPartRecord
    {
        public FileRecord()
        {
            Groups = new List<FilesGroupsRecord>();
        }


        public virtual string Name { get; set; }
        public virtual bool Active { get; set; }
        public virtual string Alias { get; set; }
        public virtual string Description { get; set; }
        public virtual string Ext { get; set; }
        public virtual DateTime? CreateDate { get; set; }
        public virtual DateTime? UpdateDate { get; set; }
        public virtual long Size { get; set; }
        public virtual string Path { get; set; }
        public virtual IList<FilesGroupsRecord> Groups { get; set; }
        public virtual int? CreatorId { get; set; }

    }

    public class FilePart : ContentPart<FileRecord>
    {
        [Required]
        public string Name
        {
            get { return Record.Name; }
            set { Record.Name = value; }
        }

        [Required]
        public string Alias
        {
            get { return Record.Alias; }
            set { Record.Alias = value; }
        }

        public bool Active
        {
            get { return Record.Active; }
            set { Record.Active = value; }
        }
        
        public string Description
        {
            get { return Record.Description; }
            set { Record.Description = value; }
        }

        public DateTime? CreateDate
        {
            get { return Record.CreateDate; }
            set { Record.CreateDate = value; }
        }

        public DateTime? UpdateDate
        {
            get { return Record.UpdateDate; }
            set { Record.UpdateDate = value; }
        }

        public long Size
        {
            get { return Record.Size; }
            set { Record.Size = value; }
        }

        public string Ext
        {
            get { return Record.Ext; }
            set { Record.Ext = value; }
        }

        public string Path 
        {
            get { return Record.Path; }
            set { Record.Path = value; }
        }

        public int? CreatorId
        {
            get { return Record.CreatorId; }
            set { Record.CreatorId = value; }
        }

        public IEnumerable<GroupRecord> Groups
        {
            get
            {
                return Record.Groups.Where(x => x.FileRecord.Id == Record.Id).Select(r => r.GroupRecord);
            }
        }

    }
}
