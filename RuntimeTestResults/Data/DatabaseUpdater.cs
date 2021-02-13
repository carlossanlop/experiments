using RuntimeTestResults.Models;
using System;
using System.Linq;

namespace RuntimeTestResults.Data
{
    public class DatabaseUpdater : IDisposable
    {
        private readonly KustoContext _kusto;
        private readonly DatabaseContext _db;

        public DatabaseUpdater()
        {
            _kusto = new KustoContext();
            _db = new DatabaseContext();
        }

        public void Dispose()
        {
            _kusto.Dispose();
            _db.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Update()
        {
            UpdateRepositories();
        }

        private void UpdateRepositories()
        {
            Console.WriteLine("Updating repositories...");

            var repositories = _kusto.Repositories;
            foreach (Repository repo in repositories)
            {
                if (!string.IsNullOrWhiteSpace(repo.Name))
                {
                    if (_db.Repositories.Any(r => r.Name == repo.Name))
                    {
                        Console.WriteLine($" - Skipping '{repo.Name}'.");
                    }
                    else
                    {
                        _db.Repositories.Add(repo);
                        Console.WriteLine($" - Adding '{repo.Name}'.");
                    }
                }
            }
            int saved = _db.SaveChanges();
            Console.WriteLine($"Total repositories downlodaded from Kusto: {repositories.Count()}");
            Console.WriteLine($"Total new repositories added to database: {saved}");
        }
    }
}
