using System;
using Orchard.ContentManagement.Records;

namespace ClientFiles.Models
{
    public class CustomerRecord : ContentPartRecord {
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual string Title { get; set; }
        public virtual DateTime CreatedAt { get; set; }
    }
}
