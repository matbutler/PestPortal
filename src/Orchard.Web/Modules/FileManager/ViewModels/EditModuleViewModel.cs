using System.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FileManager.Models;
using System.Web.Mvc;


namespace FileManager.ViewModels
{

    public class EditModuleViewModel
    {
        public List<SelectListItem> GroupList { get; set; }
        public int? selectedGroup { get; set; }
        public int? selectedShowType { get; set; }
        public List<SelectListItem> ShowTypes { get; set; }
        public bool HideGroups { get; set; }
        public bool HideFilterPanel { get; set; }
    }
}
