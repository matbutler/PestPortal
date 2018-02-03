using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orchard.ContentManagement.Records;

namespace River.SecureFileStorage.Models
{
    public class SecureFileStoragePartRecord : ContentPartRecord
    {
        public virtual string FileName { get; set; }
    }
}
