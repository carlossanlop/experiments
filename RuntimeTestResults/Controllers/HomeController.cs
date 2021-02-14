﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RuntimeTestResults.Data;
using RuntimeTestResults.Models;
using System.Diagnostics;

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
    }
}
