using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Orchard;
using Orchard.ContentManagement;

namespace River.SecureFileStorage.Models
{
    public class SecureFileStoragePart : ContentPart<SecureFileStoragePartRecord>
    {
        public string FileName { get { return Record.FileName; } set { Record.FileName = value; } }
    }
}