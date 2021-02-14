using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using RuntimeTestResults.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace RuntimeTestResults.Data
{
    public class KustoContext : IDisposable
    {
        // Kusto usage instructions:
        // https://docs.microsoft.com/en-us/azure/data-explorer/kusto/api/netfx/about-kusto-data
        // https://github.com/Azure/azure-kusto-samples-dotnet/blob/master/client/HelloKusto/Program.cs

        private const string Cluster = "https://engsrvprod.kusto.windows.net";
        private const string Database = "engineeringdata";

        private readonly KustoConnectionStringBuilder _builder;
        private readonly ICslQueryProvider _provider;

        public IEnumerable<Repository> RemoteRepositories
        {
            get
            {
                using IDataReader reader = ExecuteQuery(Repository.Query);
                var repositories = new List<Repository>();
                while (reader.Read())
                {
                    var repository = new Repository() { Name = reader.GetString(0) };
                    repositories.Add(repository);
                }
                return repositories;
            }
        }

        public KustoContext()
        {
            _builder = new KustoConnectionStringBuilder(Cluster, Database).WithAadUserPromptAuthentication();
            _provider = KustoClientFactory.CreateCslQueryProvider(_builder);
        }

        public void Dispose()
        {
            _provider.Dispose();
            GC.SuppressFinalize(this);
        }

        public IEnumerable<TestResult> GetTestResults(Job job)
        {
            string query = string.Format(TestResult.Query, job.JobId);
            using IDataReader r = ExecuteQuery(query);

            var testResults = new List<TestResult>();
            while (r.Read())
            {
                int i = 0;
                var tr = new TestResult();

                tr.Arguments = r.GetString(i++);
                tr.Attempt = r.GetString(i++);
                tr.Duration = r.GetDouble(i++);
                tr.Exception = r.GetString(i++);
                tr.JobId = r.GetInt64(i++);
                tr.JobName = r.GetString(i++);
                tr.Message = r.GetString(i++);
                tr.Method = r.GetString(i++);
                tr.Reason = r.GetString(i++);
                tr.Result = r.GetString(i++);
                tr.StackTrace = r.GetString(i++);
                tr.Traits = r.GetString(i++);
                tr.Type = r.GetString(i++);
                tr.WorkItemFriendlyName = r.GetString(i++);
                tr.WorkItemName = r.GetString(i++);

                testResults.Add(tr);
            }
            return testResults;
        }

        public IEnumerable<Job> GetJobs(Repository repository, DateTime from, DateTime to)
        {
            string query = string.Format(Job.Query, repository.Name, from, to);
            using IDataReader r = ExecuteQuery(query);

            var jobs = new List<Job>();
            while (r.Read())
            {
                int i = 0;
                var j = new Job();

                j.Attempt = r.GetString(i++);
                j.Branch = r.GetString(i++);
                j.Finished = r.GetDateTime(i++);
                j.InitialItems = r.GetInt32(i++);
                j.ItemsBadExit = r.GetInt32(i++);
                j.ItemsError = r.GetInt32(i++);
                j.ItemsFail = r.GetInt32(i++);
                j.ItemsNotRun = r.GetInt32(i++);
                j.ItemsPass = r.GetInt32(i++);
                j.ItemsPassedOnRetry = r.GetInt32(i++);
                j.ItemsWarning = r.GetInt32(i++);
                j.JobId = r.GetInt64(i++);
                j.Properties = r.GetString(i++);
                j.QueueAlias = r.GetString(i++);
                j.Queued = r.GetDateTime(i++);
                j.Repository = new Repository() { Name = r.GetString(i++) };
                j.Source = r.GetString(i++);
                j.Started = r.GetDateTime(i++);
                j.TeamProject = r.GetString(i++);
                j.TestsFail = r.GetInt32(i++);
                j.TestsPass = r.GetInt32(i++);
                j.TestsPassedOnRetry = r.GetInt32(i++);
                j.TestsSkip = r.GetInt32(i++);
                j.TotalItems = r.GetInt32(i++);
                j.Type = r.GetString(i++);

                jobs.Add(j);
            }
            return jobs;
        }

        /// <summary>
        /// Returns a disposable data reader object containing the enumerable results of the Kusto query.
        /// </summary>
        private IDataReader ExecuteQuery(string query)
        {
            var properties = new ClientRequestProperties() { ClientRequestId = Guid.NewGuid().ToString() };
            return _provider.ExecuteQuery(query, properties);
        }
    }
}
