using System;
using System.Collections.Generic;
using System.Data;
using FileManager.Models;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;
using Orchard;
using Orchard.ContentManagement;

namespace FileManager {

    public class Migrations : DataMigrationImpl {

        public int Create() {
            // Creating table FilesGroupsRecord
            SchemaBuilder.CreateTable("FilesGroupsRecord", table => table
                .ContentPartRecord()
                .Column("FileRecord_id", DbType.Int32)
                .Column("GroupRecord_id", DbType.Int32)
            );

            // Creating table GroupRecord
            SchemaBuilder.CreateTable("GroupRecord", table => table
                .ContentPartRecord()
                .Column("Name", DbType.String)
                .Column("Active", DbType.Boolean)
                .Column("Alias", DbType.String)
                .Column("Wlft", DbType.Int32)
                .Column("Wrgt", DbType.Int32)
                .Column("Description", DbType.String, c => c.Unlimited())
                .Column("CreateDate", DbType.DateTime)
                .Column("UpdateDate", DbType.DateTime)
                .Column("Parent_id", DbType.Int32)
                .Column("Level", DbType.Int32)
                .Column("CreatorId", DbType.Int32)
            );

            
            // Creating table FileRecord
            SchemaBuilder.CreateTable("FileRecord", table => table
                .ContentPartRecord()
                .Column("Name", DbType.String)
                .Column("Active", DbType.Boolean)
                .Column("Alias", DbType.String)
                .Column("Description", DbType.String, c => c.Unlimited())
                .Column("Ext", DbType.String)
                .Column("CreateDate", DbType.DateTime)
                .Column("UpdateDate", DbType.DateTime)
                .Column("Size", DbType.Int64)
                .Column("Path", DbType.String)
                .Column("CreatorId", DbType.Int32)
            );


            // Creating table FileManagerRecord
            SchemaBuilder.CreateTable("FileManagerRecord", table => table
                .ContentPartRecord()
                .Column("ParentGroup_id", DbType.Int32)
                .Column("ShowType", DbType.Int32)
                .Column("HideGroups", DbType.Boolean)
                .Column("HideFilterPanel", DbType.Boolean)
            );

            ContentDefinitionManager.AlterPartDefinition(
                typeof(FileManagerPart).Name, cfg => cfg.Attachable());


            ContentDefinitionManager.AlterTypeDefinition("Group",
               cfg => cfg
                   .WithPart("GroupPart")
                   .WithPart("CommonPart")
                );

            // Creating table FileManagerSettingsRecord
            SchemaBuilder.CreateTable("FileManagerSettingsRecord", table => table
                .ContentPartRecord()
                .Column("FileExtensions", DbType.String, c => c.Unlimited())
                .Column("SystemType", DbType.Int32)
            );

            // Creating table PermissionRecord
            SchemaBuilder.CreateTable("PermissionRecord", table => table
                .ContentPartRecord()
                .Column("RoleId", DbType.Int32)
                .Column("RecordId", DbType.Int32)
                .Column("SystemType", DbType.Int32)
            );

            return 1;
        }

    }
}