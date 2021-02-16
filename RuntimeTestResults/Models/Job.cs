using System;

namespace RuntimeTestResults.Models
{
    public class Job
    {
        public string Attempt { get; set; }
        public string Branch { get; set; }
        public DateTime Finished { get; set; }
        public int InitialItems { get; set; }
        public int ItemsBadExit { get; set; }
        public int ItemsError { get; set; }
        public int ItemsFail { get; set; }
        public int ItemsNotRun { get; set; }
        public int ItemsPass { get; set; }
        public int ItemsPassedOnRetry { get; set; }
        public int ItemsWarning { get; set; }
        public long JobId { get; set; }
        public string Properties { get; set; }
        public string QueueAlias { get; set; }
        public DateTime Queued { get; set; }
        public Repository Repository { get; set; }
        public string Source { get; set; }
        public DateTime Started { get; set; }
        public string TeamProject { get; set; }
        public int TestsFail { get; set; }
        public int TestsPass { get; set; }
        public int TestsPassedOnRetry { get; set; }
        public int TestsSkip { get; set; }
        public int TotalItems { get; set; }
        public string Type { get; set; }

        // Custom get-only properties
        public string FinishedShort => Finished.ToString("yyyy/MM/dd HH:mm");
        public string QueuedShort => Queued.ToString("yyyy/MM/dd HH:mm");
        public string StartedShort => Started.ToString("yyyy/MM/dd HH:mm");
        public double Passrate
        {
            get
            {
                int total = TestsFail + TestsPass;
                
                if (total == 0)
                {
                    return 0;
                }

                return TestsPass * 100 / total;
            }
        }

        public string Color => "rgba(255, 0, 0, 0.5)";

        internal static readonly string Query = @"Jobs | project
                Attempt,
                Branch,
                Finished,
                InitialItems,
                ItemsBadExit,
                ItemsError,
                ItemsFail,
                ItemsNotRun,
                ItemsPass,
                ItemsPassedOnRetry,
                ItemsWarning,
                JobId,
                Properties,
                QueueAlias,
                Queued,
                Repository,
                Source,
                Started,
                TeamProject,
                TestsFail,
                TestsPass,
                TestsPassedOnRetry,
                TestsSkip,
                TotalItems,
                Type
            | where Branch == ""refs/heads/master""
                and Repository == ""{0}""
                and TeamProject == ""public""
                and Type startswith ""test/functional/cli""
                and Started >= todatetime(""{1}"")
                and Started <= todatetime(""{2}"")";
    }
}
