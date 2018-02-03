using System.Collections.Generic;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.UI.Admin.Notification;
using Orchard.UI.Notify;
using River.SecureFileStorage.Models;

namespace River.SecureFileStorage.Services
{
	public class MissingSettingsBanner : INotificationProvider {
		private readonly IOrchardServices _orchardServices;

		public MissingSettingsBanner(IOrchardServices orchardServices) {
			_orchardServices = orchardServices;
			T = NullLocalizer.Instance;
		}

		public Localizer T { get; set; }

        public IEnumerable<NotifyEntry> GetNotifications( )
        {
            var secureFileStorageSettings = _orchardServices.WorkContext.CurrentSite.As<SecureFileStorageSettingsPart>( );
            if ( secureFileStorageSettings == null || string.IsNullOrWhiteSpace( secureFileStorageSettings.RootStorageDir ) )
            {
                yield return new NotifyEntry { Message = T( "The Secure File Storage settings need to be configured." ), Type = NotifyType.Warning };
            }
        }
	}
}