﻿using System.Web.Mvc;
using Orchard.Themes;

namespace ClientFiles.Controllers
{
    [Themed]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}