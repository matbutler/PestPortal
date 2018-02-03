using FileManager.Models;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;

namespace FileManager.Handlers
{
    public class FileManagerModuleHandler : ContentHandler
    {
        public FileManagerModuleHandler(IRepository<FileManagerRecord> repository)
        {
            Filters.Add(StorageFilter.For(repository));
        }
    }
}
