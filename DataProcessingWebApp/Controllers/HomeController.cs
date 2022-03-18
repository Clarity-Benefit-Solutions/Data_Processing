using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DataProcessingWebApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            try
            {
                ViewBag.Title = "Home Page";
            }
            catch (Exception e)
            {
                Debug.Print(e.ToString());
            }

            return View();
        }
    }
}
