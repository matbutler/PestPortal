using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Localization;
using River.SecureFileStorage.Models;

namespace River.SecureFileStorage.Drivers
{
    public class SecureFileStoragePartDriver : ContentPartDriver<SecureFileStoragePart>
    {
        private const string TemplateName = "Parts/SecureFileStorage";

        private IOrchardServices _orchardServices;

        public SecureFileStoragePartDriver( IOrchardServices orchardServices )
        {
            _orchardServices = orchardServices;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        protected override string Prefix { get { return "SecureFileStoragePart"; } }

        protected override DriverResult Display( SecureFileStoragePart part, string displayType, dynamic shapeHelper )
        {
            return ContentShape( "Parts_SecureFileStorage_SummaryAdmin", ( ) => shapeHelper.Parts_SecureFileStorage_SummaryAdmin( ) );
        }
        
        protected override DriverResult Editor( SecureFileStoragePart part, dynamic shapeHelper )
        {
            return ContentShape( "Parts_SecureFileStorage_Edit",
                    ( ) => shapeHelper.EditorTemplate(
                        TemplateName: TemplateName,
                        Model: part,
                        Prefix: Prefix ) );
        }

        protected override DriverResult Editor( SecureFileStoragePart part, IUpdateModel updater, dynamic shapeHelper )
        {
            var storageSettings = _orchardServices.WorkContext.CurrentSite.As<SecureFileStorageSettingsPart>( );

            if ( storageSettings == null || String.IsNullOrWhiteSpace( storageSettings.RootStorageDir ) )
            {
                updater.AddModelError( "FileName", T( "The Secure file location needs to be configured" ) );
            }
            else
            {
                if ( HttpContext.Current.Request.Files.Count != 1 )
                {
                    updater.AddModelError( "FileName", T( "Need to provide exactly 1 file." ) );
                }
                else
                {
                    var file = HttpContext.Current.Request.Files[ 0 ];
                    var filename = Path.GetFileName(file.FileName);

                    file.SaveAs( storageSettings.RootStorageDir + filename);
                    part.FileName = filename;
                }
            }

            return Editor( part, shapeHelper );
        }
    }
}