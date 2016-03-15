using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using churn.Data.Migrations;
using churn.Models;
using NLog;

namespace churn.Data
{
    public class DatabaseService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public DatabaseService()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<CodeHistoryContext, Configuration>());
        }

        public void Save(IEnumerable<Models.Changeset> changes)
        {
            Logger.Info("Started saving changes into the database");

            using (var dbContext = new CodeHistoryContext())
            {
                foreach (var changeset in changes)
                {
                    Logger.Info("Saving changeset {0} into the database", changeset.ChangesetId);

                    var author = FindOrCreateAuthor(dbContext, changeset);

                    var changeSetDb = FindOrCreateChangeset(dbContext, changeset, author);

                    var filesToRemove = changeSetDb.Files
                        .Where(f => !changeset.ChangedFiles.Any(f2 => f.ServerPath.Equals(f2.ServerPath)))
                        .ToList();

                    foreach (var file in filesToRemove)
                    {
                        Logger.Debug("Removing file {0} from the database (ignored by new rules?)", file.ServerPath);
                        dbContext.Files.Remove(file);
                    }

                    foreach (var changedFile in changeset.ChangedFiles)
                    {
                        FindOrCreateFile(dbContext, changeSetDb, changedFile);
                    }

                    dbContext.SaveChanges();                    
                }
            }

            Logger.Info("Finished saving changes into the database");
        }
        
        public Statistics GetStatistics(int? days)
        {
            Logger.Info("Retrieving statistics from the database");

            using (var dbContext = new CodeHistoryContext())
            {
                IQueryable<Changeset> changesets = dbContext.Changesets;

                if (days.HasValue)
                {
                    var cutoffDate = DateTimeOffset.Now.AddDays(-days.Value);
                    changesets = changesets.Where(c => c.Date >= cutoffDate);
                }

                var authorStats =
                    (from changeSet in changesets
                    group changeSet by changeSet.Author
                    into authorChangesets
                    let filesChanges = authorChangesets.SelectMany(c => c.Files).SelectMany(f => f.Changes)
                    let filteredChanges = filesChanges.Where(c => Math.Abs(c.ModifiedFileLocation.Length - c.OriginalFileLocation.Length) < 1000)
                    select new
                    {
                        Author = authorChangesets.Key,

                        NetChange = filteredChanges
                            .Select(c => c.ModifiedFileLocation.Length - c.OriginalFileLocation.Length)
                            .DefaultIfEmpty().Sum(),

                        Affected = filteredChanges
                            .Select(c => c.ModifiedFileLocation.Length > c.OriginalFileLocation.Length ? c.ModifiedFileLocation.Length: c.OriginalFileLocation.Length)
                            .DefaultIfEmpty().Sum()
                    }).ToList();

                return new Statistics
                {
                    StartDate = changesets.Min(c => c.Date),
                    EndDate = changesets.Max(c => c.Date),
                    AuthorStats = authorStats.Select(it => new AuthorStats
                    {
                        Author = it.Author.Name,
                        NetLinesAdded = it.NetChange,
                        LinesAffected = it.Affected
                    }).ToList()
                };
            }            
        }

        private void FindOrCreateFile(CodeHistoryContext dbContext, Changeset changeSetDb, ChangedFile changedFile)
        {
            var fileDb = changeSetDb.Files.FirstOrDefault(f => f.ServerPath == changedFile.ServerPath);

            if (fileDb == null)
            {
                fileDb = new File
                {
                    ServerPath = changedFile.ServerPath,
                    ChangeType = changedFile.ChangeType,
                    Changes = new List<Change>()
                };

                changeSetDb.Files.Add(fileDb);
            }

            dbContext.Changes.RemoveRange(fileDb.Changes);

            fileDb.Changes = changedFile.ChangeSegments
                   .Select(segment => new Change
                   {
                       OriginalFileLocation = RangeToLocation(segment.OriginalFileRange),
                       ModifiedFileLocation = RangeToLocation(segment.ModifiedFileRange)
                   }
                   )
                   .ToList();
        }

        private Location RangeToLocation(LinesRange linesRange)
        {
            if (linesRange == null) return new Location { StartLine = 0, Length = 0 };

            return new Location
            {
                StartLine = linesRange.Start,
                Length = linesRange.PastEnd - linesRange.Start
            };
        }

        private Changeset FindOrCreateChangeset(CodeHistoryContext dbContext, Models.Changeset changeset, Author author)
        {
            var changeSetDb = dbContext.Changesets
                .Include(c => c.Files)
                .Include(c => c.Files.Select(f => f.Changes))
                .FirstOrDefault(a => a.VersionControlId == changeset.ChangesetId);

            if (changeSetDb == null)
            {
                changeSetDb = new Changeset
                {
                    VersionControlId = changeset.ChangesetId,
                    Author = author,
                    Date = changeset.Date,
                    Files = new List<File>()
                };

                Logger.Info("Adding changeset {0} to the database", changeset.ChangesetId);

                dbContext.Changesets.Add(changeSetDb);
            }
            return changeSetDb;
        }

        private Author FindOrCreateAuthor(CodeHistoryContext dbContext, Models.Changeset changeset)
        {
            var author = dbContext.Authors.FirstOrDefault(a => a.Name == changeset.Author);
            if (author == null)
            {
                author = new Author { Name = changeset.Author };
                dbContext.Authors.Add(author);

                Logger.Info("Adding author {0} to the database", changeset.Author);
            }
            return author;
        }
    }
}
