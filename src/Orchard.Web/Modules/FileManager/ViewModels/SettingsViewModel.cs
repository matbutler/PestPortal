using System.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FileManager.Models;
using System.Web.Mvc;


namespace FileManager.ViewModels
{

    public class SettingsViewModel
    {
        public string PictureExtensions { get; set; }
        public string DocumentsExtensions { get; set; }
        public string VideoExtensions { get; set; }
        public string UploadAllowedFileTypeWhitelist { get; set; }
    }
}
