using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orchard.ContentManagement.Records;
using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement;

namespace FileManager.Models
{
    public class PermissionRecord : ContentPartRecord
    {
        public virtual int RoleId { get; set; }
        public virtual int RecordId { get; set; }
        public virtual int SystemType { get; set; }
    }
    public class PermissionPart : ContentPart<PermissionRecord>
    {
        [Required]
        public int RoleId
        {
            get { return Record.RoleId; }
            set { Record.RoleId = value; }
        }

        [Required]
        public int RecordId
        {
            get { return Record.RecordId; }
            set { Record.RecordId = value; }
        }

        [Required]
        public int SystemType
        {
            get { return Record.SystemType; }
            set { Record.SystemType = value; }
        }
    }

    public enum PermisionRecordType
    {
        None = 0,
        GroupRecordType = 1,
        FileRecordType = 2
    }
}
