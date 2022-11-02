using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RestSharp;
using System.Data.OleDb;
using MajickSharp;
using WizardDashboard.Models;
namespace WizardDashboard.Controllers
{
    public class HomeController : Controller
    {
        private static OleDbCommand command;
        private static OleDbDataReader reader;
        private static OleDbConnection connection = new OleDbConnection("Provider=SQLOLEDB;" +
                "Data Source=MAJICKVPS\\SQLEXPRESS;" +
                "Initial Catalog=Wizard;" +
                "Integrated Security=SSPI;");
        [HttpGet]
        public ActionResult Index(string user_id = "")
        {
            //If the user ID isn't empty then we can show that they are logged in by pulling their token from the Database;
            return View();
        }
    }
}