using System.Web.Mvc;
using Orchard;
using Orchard.ContentManagement;
using River.SecureFileStorage.Models;
using Orchard.Security;
using Orchard.Core.Contents;


namespace River.SecureFileStorage.Controllers
{
    [Authorize]
    public class SecureFileController : Controller
    {

        private IOrchardServices _orchardServices;
        private IAuthorizationServiceEventHandler _authorisationEventHandler;

        public SecureFileController( IOrchardServices orchardServices, IAuthorizationServiceEventHandler authorisationEventHandler )
        {
            _orchardServices = orchardServices;
            _authorisationEventHandler = authorisationEventHandler;
        }

        public ActionResult Index( int id )
        {

            var contentItem = _orchardServices.ContentManager.Get( id );

            var secureStoragePart = contentItem.As<SecureFileStoragePart>( );

            var secureStorageSettings = _orchardServices.WorkContext.CurrentSite.As<SecureFileStorageSettingsPart>( );

            if ( secureStoragePart != null && secureStorageSettings != null )
            {

                // check the permissions
                CheckAccessContext context = new CheckAccessContext( ) { Permission = Permissions.ViewContent, Content = contentItem, User = _orchardServices.WorkContext.CurrentUser };
                _authorisationEventHandler.Complete( context );

                if ( context.Granted )
                {
                    return File( secureStorageSettings.RootStorageDir + secureStoragePart.FileName,
                        "application/octet-stream",
                        secureStoragePart.FileName );
                }
                else
                    return new HttpNotFoundResult( );
            }
            else
            {
                return new HttpNotFoundResult( );
            }
        }

    }
}