using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using River.SecureFileStorage.Models;

namespace River.SecureFileStorage.Handlers
{
    public class SecureFileStorageSettingsPartHandler : ContentHandler
    {
        public SecureFileStorageSettingsPartHandler( IRepository<SecureFileStorageSettingsPartRecord> repository )
        {
            Filters.Add( new ActivatingFilter<SecureFileStorageSettingsPart>( "Site" ) );
            Filters.Add( StorageFilter.For( repository ) );
        }
    }
}