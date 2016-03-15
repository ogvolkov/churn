using System;
using System.Linq;
using churn.CommandLineOptions;
using churn.Data;
using churn.Tfs;
using NLog;

namespace churn
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {            
            var options = new Options();
            string command = null;
            object commandOptions = null;

            var parseResult = CommandLine.Parser.Default.ParseArguments(args, options,
                (verb, subOptions) =>
                {
                    command = verb;
                    commandOptions = subOptions;
                });

            if (!parseResult)
            {
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }

            if (command == "get")
            {
                Get((GetOptions)commandOptions);
            }

            if (command == "analyze")
            {
                Analyze((AnalyzeOptions)commandOptions);
            }
        }

        private static void Analyze(AnalyzeOptions analyzeOptions)
        {
            Logger.Info("Analyzing statistics");

            var databaseService = new DatabaseService();
            var stats = databaseService.GetStatistics(analyzeOptions.Days);

            var days = (stats.EndDate - stats.StartDate).Days;
            Console.WriteLine($"Stats from {stats.StartDate: dd.MM.yyyy} to {stats.EndDate: dd.MM.yyyy}, {days} days in total");
            var tableFormat = "{0,30}{1,30}{2,30}";
            Console.WriteLine(tableFormat, "Author", "Lines net change", "Lines affected");
            Console.WriteLine("---------------------------------------------------------------------------------");

            var statsPerAuthor = stats.AuthorStats.Select(it => new
            {
                Author = it.Author,
                LinesCountChange = it.NetLinesAdded / days,
                LinesAffected = it.LinesAffected / days
            }).ToList();

            foreach (var stat in statsPerAuthor.OrderByDescending(s => s.LinesCountChange))
            {
                Console.WriteLine(tableFormat, $"{stat.Author}", $"{stat.LinesCountChange:F0}", $"{stat.LinesAffected:F0}");
            }

            var averageLineCountChange = statsPerAuthor.Average(it => it.LinesCountChange);
            var averageLinesAffected = statsPerAuthor.Average(it => it.LinesAffected);
            Console.WriteLine("---------------------------------------------------------------------------------");
            Console.WriteLine(tableFormat, "Average", $"{averageLineCountChange:F0}", $"{averageLinesAffected:F0}");

            Console.WriteLine();
            Logger.Info("Analysis completed");
        }

        private static void Get(GetOptions options)
        {
            Logger.Info("Retrieving changes from TFS");

            var retriever = new TfsChangesRetriever(options.Collection, options.Path, new IgnoredItemsMatcher());
            var changes = retriever.Retrieve(options.StartFromChangeset, options.MaxChangesets);            

            var databaseService = new DatabaseService();
            databaseService.Save(changes);

            Logger.Info("Finished retrieving changes from TFS");
        }
    }
}
