using CommandLine;

namespace churn.CommandLineOptions
{
    public class AnalyzeOptions
    {
        [Option("days", HelpText = "Number of days back to analyze", Required = false)]
        public int? Days { get; set; }
    }
}
