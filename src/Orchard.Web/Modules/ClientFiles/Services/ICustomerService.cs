using ClientFiles.Models;
using Orchard;
using Orchard.ContentManagement;

namespace ClientFiles.Services
{
    public interface ICustomerService : IDependency
    {
        CustomerPart CreateCustomer(string email, string password);
        IContentQuery<CustomerPart> GetCustomers();
        CustomerPart GetCustomer(int id);
    }
}