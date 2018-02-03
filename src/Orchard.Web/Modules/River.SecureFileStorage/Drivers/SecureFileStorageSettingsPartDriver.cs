using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Localization;
using River.SecureFileStorage.Models;

namespace River.SecureFileStorage.Drivers
{
	public class SecureFileStorageSettingsPartDriver : ContentPartDriver<SecureFileStorageSettingsPart> {
		private const string TemplateName = "Parts/SecureFileStorageSettings";

        public SecureFileStorageSettingsPartDriver( )
        {
			T = NullLocalizer.Instance;
		}

		public Localizer T { get; set; }

        protected override string Prefix { get { return "SecureFileStorageSettings"; } }
		
		//GET
        protected override DriverResult Editor( SecureFileStorageSettingsPart part, dynamic shapeHelper )
        {
			return ContentShape("Parts_SecureFileStorageSettings_Edit",
					() => shapeHelper.EditorTemplate(
						TemplateName: TemplateName,
						Model: part,
						Prefix: Prefix));
		}

		//POST
        protected override DriverResult Editor( SecureFileStorageSettingsPart part, IUpdateModel updater, dynamic shapeHelper )
        {
			updater.TryUpdateModel(part, Prefix, null, null);
			return Editor(part, shapeHelper);
		}
	}
}