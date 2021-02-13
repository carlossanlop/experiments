using Microsoft.EntityFrameworkCore;
using RuntimeTestResults.Models;
using System;
using System.Collections.Generic;

namespace RuntimeTestResults.Data
{
    public class DatabaseContext : DbContext
    {
        private const string ConnectionString = "Data Source=RuntimeTestResults.db";

        public DbSet<Repository> Repositories { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(ConnectionString);
        }
    }
}
