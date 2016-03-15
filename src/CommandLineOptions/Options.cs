using CommandLine;
using CommandLine.Text;

namespace churn.CommandLineOptions
{
    public class Options
    {
        public Options()
        {
            GetOptions = new GetOptions();    
        }

        [VerbOption("get", HelpText = "Get changes from the source control")]
        public GetOptions GetOptions { get; set; }

        [VerbOption("analyze", HelpText = "Analyze previously saved results")]
        public AnalyzeOptions AnalyzeOptions { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
