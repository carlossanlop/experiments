using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RuntimeTestResults.Data;
using RuntimeTestResults.Models;
using System;
using System.Diagnostics;

namespace RuntimeTestResults.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            //using var db = new DatabaseContext();
            //ViewBag.Repositories = db.Repositories.ToList();

            using var db = new KustoContext();

            DateTime from = DateTime.Now - TimeSpan.FromHours(1);
            DateTime to = DateTime.Now;

            ViewBag.Jobs = db.GetJobs(from, to);

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
