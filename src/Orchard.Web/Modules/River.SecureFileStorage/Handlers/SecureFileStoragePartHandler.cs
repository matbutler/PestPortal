using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using River.SecureFileStorage.Models;

namespace River.SecureFileStorage.Handlers
{
    public class SecureFileStoragePartHandler : ContentHandler
    {
        public SecureFileStoragePartHandler( IRepository<SecureFileStoragePartRecord> repository )
        {
            Filters.Add( StorageFilter.For( repository ) );
        }
    }
}