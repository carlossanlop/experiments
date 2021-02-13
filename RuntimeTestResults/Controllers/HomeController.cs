using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RuntimeTestResults.Data;
using RuntimeTestResults.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

            ViewBag.Repositories = db.Repositories.Where(r => !string.IsNullOrWhiteSpace(r.Name));

            //DateTime from = DateTime.Now - TimeSpan.FromHours(1);
            //DateTime to = DateTime.Now;

            //var jobs = db.GetJobs(from, to);

            //var results = new List<TestResult>();
            //foreach (Job job in jobs)
            //{
            //    results.AddRange(db.GetTestResults(job));
            //}

            //ViewBag.Results = results;

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
