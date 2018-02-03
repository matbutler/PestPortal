using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard;
using FileManager.Models;
using Orchard.Media.Models;

namespace FileManager.Services
{
    public interface ISettingsService : IDependency
    {
        IEnumerable<FileManagerSettingsRecord> GetFileSettings();
        FileManagerSettingsRecord GetFileSetting(int systemType);
        bool UpdateFileSettings(int systemType, string extensions);
        void CreateDefaultSettings();
        FileManagerSettingsTypes GetFileType(string fileExt);
        MediaSettingsPartRecord GetFileUploadFilesWhiteList();
        void UpdateFileUploadWhiteList(string whiteList);
    }
}