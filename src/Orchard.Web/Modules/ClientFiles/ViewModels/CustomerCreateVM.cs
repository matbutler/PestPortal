using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientFiles.ViewModels
{
     public class CustomerCreateVM 
    {
        [StringLength(10), Display(Name = "Title")]
        public string Title { get; set; }

        [StringLength(50), Required, Display(Name = "Firstname")]
        public string FirstName { get; set; }

        [StringLength(50), Required, Display(Name = "Lastname")]
        public string LastName { get; set; }

        [StringLength(255), Required, DataType(DataType.EmailAddress), Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(255), Required, DataType(DataType.Password), Display(Name = "Password")]
        public string Password { get; set; }

        [StringLength(255), Required, DataType(DataType.Password), System.Web.Mvc.Compare("Password"), Display(Name = "Repeat password")]
        public string RepeatPassword { get; set; }
    }
}
