using CommandLine;

namespace churn.CommandLineOptions
{
    public class GetOptions
    {
        [Option("collection", HelpText= "TFS collection to connect to, e.g. https://example.com/DefaultCollection", Required = true)]
        public string Collection { get; set; }

        [Option("path", HelpText = "Project path, e.g. $/project/branch", Required = true)]
        public string Path { get; set; }

        [Option("maxChangesets", HelpText = "Maximum number of changesets to retrieve", Required = false)]
        public int? MaxChangesets { get; set; }

        [Option("startFromChangeset", HelpText = "Start from this changeset", Required = false)]
        public int? StartFromChangeset { get; set; }
    }
}
