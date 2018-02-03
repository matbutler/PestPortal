using System.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FileManager.Models;
using Orchard.Users.Models;

namespace FileManager.ViewModels
{

    public class FileGroupEditViewModel
    {
        public GroupRecord Group { get; set; }
        public List<GroupRecord> BreadRecord { get; set; }
        public UserPart Creator { get; set; }
        public List<RoleEntry> SystemRoles { get; set; }
    }
}
