using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using Orchard.ContentManagement;
using FileManager.Models;
using Orchard;

namespace FileManager.Services
{
    public interface IFileManagerService : IDependency
    {
        IEnumerable<FileManagerRecord> GetModules();
        FileManagerRecord GetModule(int moduleId);
        bool UpdateModule(int moduleId, int groupId, int ShowType, bool HideGroups, bool HideFilterPanel);
        bool UpdateModule(int moduleId, GroupRecord group, int ShowType, bool HideGroups, bool HideFilterPanel);
        bool UpdateModule(int moduleId, FileManagerRecord newModule);
        bool DeleteModule(int moduleId);
        
    }
}