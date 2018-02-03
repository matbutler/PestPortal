using System;
using System.Data;
using ClientFiles.Models;
using Orchard.ContentManagement.MetaData;
using Orchard.Core.Common.Fields;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;
using Orchard.Users.Models;

namespace ClientFiles
{
    public class Migrations : DataMigrationImpl
    {

        public int Create()
        {
            SchemaBuilder.CreateTable("CustomerRecord", t => t
                .ContentPartRecord()
                .Column<string>("FirstName", c => c.WithLength(50))
                .Column<string>("LastName", c => c.WithLength(50))
                .Column<string>("Title", c => c.WithLength(10))
                .Column<DateTime>("CreatedAt", c => c.NotNull())
                );

            ContentDefinitionManager.AlterPartDefinition(typeof(CustomerPart).Name, p => p
                .Attachable()
                .WithField("Phone", f => f.OfType(typeof(TextField).Name))
                );


            ContentDefinitionManager.AlterTypeDefinition("Customer", t => t
                .WithPart(typeof(CustomerPart).Name)
                .WithPart(typeof(UserPart).Name)
                );
            return 1;
        }
    }
}