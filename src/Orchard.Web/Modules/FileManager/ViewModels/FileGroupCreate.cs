using System.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FileManager.Models;

namespace FileManager.ViewModels {
    
    public class FileGroupCreateViewModel {
        [Required, DisplayName("Folder Name:")]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public List<GroupRecord> BreadRecord { get; set; }
        public List<RoleEntry> SystemRoles { get; set; }
    }
}
