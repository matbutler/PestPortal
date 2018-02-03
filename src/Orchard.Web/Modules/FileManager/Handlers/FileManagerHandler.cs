using FileManager.Models;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;

namespace FileManager.Handlers {
    public class FileManagerHandler : ContentHandler {
        public FileManagerHandler(IRepository<FileRecord> repository) {
            Filters.Add(StorageFilter.For(repository));
        }
    }
}
