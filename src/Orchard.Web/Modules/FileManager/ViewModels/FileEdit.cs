using System.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FileManager.Models;
using Orchard.Users.Models;

namespace FileManager.ViewModels
{

    public class FileEditViewModel
    {
        public FileRecord File { get; set; }
        public string FilePath { get; set; }
        public List<GroupRecord> BreadRecord { get; set; }
        public UserPart Creator { get; set; }
        public FileManagerSettingsTypes FileType { get; set; }
        public List<RoleEntry> SystemRoles { get; set; }
    }

    public class RoleEntry
    {
        public bool IsChecked { get; set; }
        public RoleRecord Role { get; set; }
    }

    public class RoleRecord
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }
}
