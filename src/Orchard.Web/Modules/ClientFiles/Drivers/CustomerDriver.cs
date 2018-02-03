using ClientFiles.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;

namespace ClientFiles.Drivers
{
    public class CustomerDriver : ContentPartDriver<CustomerPart>
    {
        protected override DriverResult Editor(CustomerPart part, dynamic shapeHelper)
        {
            return ContentShape("Parts_Customer_Edit", () => shapeHelper.EditorTemplate(TemplateName: "Parts/Customer", Model: part, Prefix: Prefix));
        }

        protected override DriverResult Editor(CustomerPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            updater.TryUpdateModel(part, Prefix, null, null);

            var user = part.User;
            updater.TryUpdateModel(user, Prefix, new[] { "Email" }, null);

            return Editor(part, shapeHelper);
        }
    }
}