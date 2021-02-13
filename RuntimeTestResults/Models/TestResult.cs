namespace RuntimeTestResults.Models
{
    public class TestResult
    {
        public string Arguments { get; set; }
        public string Attempt { get; set; }
        public double Duration { get; set; }
        public string Exception { get; set; }
        public long JobId { get; set; }
        public string JobName { get; set; }
        public string Message { get; set; }
        public string Method { get; set; }
        public string Reason { get; set; }
        public string Result { get; set; }
        public string StackTrace { get; set; }
        public string Traits { get; set; }
        public string Type { get; set; }
        public string WorkItemFriendlyName { get; set; }
        public string WorkItemName { get; set; }

        internal static readonly string Query = @"TestResults | project
                Arguments,
                Attempt,
                Duration,
                Exception,
                JobId,
                JobName,
                Message,
                Method,
                Reason,
                Result,
                StackTrace,
                Traits,
                Type,
                WorkItemFriendlyName,
                WorkItemName
            | where JobId == {0}";
    }
}
