using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Records;

namespace FileManager.Models
{
    public class GroupRecord : ContentPartRecord
    {
        public GroupRecord()
        {
            //Childs = new List<GroupRecord>();
            //Files = new List<FilesGroupsRecord>();
        }
        public virtual string Name { get; set; }
        public virtual bool Active { get; set; }
        public virtual string Alias { get; set; }
        public virtual int Wlft { get; set; }
        public virtual int Wrgt { get; set; }
        public virtual GroupRecord Parent { get; set; }
        public virtual string Description { get; set; }
        public virtual DateTime? CreateDate { get; set; }
        public virtual DateTime? UpdateDate { get; set; }
        public virtual int Level { get; set; }
        public virtual int? CreatorId { get; set; }
    }

    public class GroupPart : ContentPart<GroupRecord>
    {
        [Required]
        public string Name
        {
            get { return Record.Description; }
            set { Record.Description = value; }
        }

        
        public string Description
        {
            get { return Record.Description; }
            set { Record.Description = value; }
        }

        public string Alias
        {
            get { return Record.Alias; }
            set { Record.Alias = value; }
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

        public int Wlft
        {
            get { return Record.Wlft; }
            set { Record.Wlft = value; }
        }

        public int Wrgt
        {
            get { return Record.Wrgt; }
            set { Record.Wrgt = value; }
        }

        public bool Active 
        { 
            get { return Record.Active; } 
            set { Record.Active = value; }  
        }

        public int? CreatorId
        {
            get { return Record.CreatorId; }
            set { Record.CreatorId = value; }
        }

        public GroupRecord Parent
        {
            get { return Record.Parent; }
            set { Record.Parent = value; }
        }

        public int Level
        {
            get { return Record.Level; }
            set { Record.Level = value; }
        }
    }
}
