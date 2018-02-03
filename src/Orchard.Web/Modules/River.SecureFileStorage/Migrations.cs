using Orchard.Data.Migration;
using Orchard.ContentManagement.MetaData;
using Orchard.Core.Contents.Extensions;
using Orchard.Localization;
using River.SecureFileStorage.Models;
using Orchard.Indexing;

namespace River.SecureFileStorage
{
    public class Migrations : DataMigrationImpl
    {
        public Migrations( )
        {
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public int Create( )
        {
            SchemaBuilder.CreateTable( "SecureFileStorageSettingsPartRecord",
                table => table
                    .ContentPartRecord( )
                    .Column<string>( "RootStorageDir" )
                );

            SchemaBuilder.CreateTable( "SecureFileStoragePartRecord",
                table => table
                    .ContentPartRecord( )
                    .Column<string>( "FileName" )
                );

            ContentDefinitionManager.AlterPartDefinition( "SecureFileStoragePart", x => x
                .Attachable( ) );

            ContentDefinitionManager.AlterTypeDefinition( "SecureFile", x => x
                .Creatable( )
                .WithPart( "CommonPart" )
                .WithPart( "TitlePart" )
                .WithPart( "SecureFileStoragePart" )
                );

            return 1;

        }

        public int UpdateFrom1( )
        {
            ContentDefinitionManager.AlterTypeDefinition( "SecureFile", x => x.Creatable( )
                .WithPart( "ContentViewPermissionsPart")
            );

            return 2;

        }

        public int UpdateFrom2()
        {
            ContentDefinitionManager.AlterTypeDefinition("SecureFile", b => b
                .Indexed("ContentItems"));

            return 3;
        }


    }
}