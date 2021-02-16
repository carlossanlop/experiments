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
        private readonly DatabaseContext _db;
        private readonly KustoContext _kusto;
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            _db = new DatabaseContext();
            _kusto = new KustoContext();
        }
        public new void Dispose()
        {
            _db.Dispose();
            _kusto.Dispose();
            base.Dispose();
        }
        public IActionResult Index()
        {
            ViewBag.Repositories = _db.Repositories;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult GetRepoJobs(string repositoryName, DateTime from, DateTime to)
        {
            Repository repository = _db.Repositories.First(r => r.Name == repositoryName);
            var jobs = _kusto.GetJobs(repository, from, to);
            return Json(jobs);
        }
    }
}
