using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using churn.Models;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using NLog;
using Changeset = Microsoft.TeamFoundation.VersionControl.Client.Changeset;

namespace churn.Tfs
{
    public class TfsChangesRetriever
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly string path;
        private readonly IgnoredItemsMatcher ignoredItemsMatcher;
        private readonly VersionControlServer versionControl;

        public TfsChangesRetriever(string projectCollectionUrl, string path, IgnoredItemsMatcher ignoredItemsMatcher)
        {
            this.path = path;
            this.ignoredItemsMatcher = ignoredItemsMatcher;

            Logger.Info("Connecting to TFS...");

            var tfsCreds = new TfsClientCredentials(new WindowsCredential(), true);
            var projectCollection = new TfsTeamProjectCollection(new Uri(projectCollectionUrl), tfsCreds);            
            this.versionControl = projectCollection.GetService<VersionControlServer>();

            Logger.Info("Successfully connected to TFS");
        }

        public IEnumerable<Models.Changeset> Retrieve(int? startFromChangeset, int? resultsCount)
        {            
            Logger.Info("Retrieving {0} changesets ending on {1}", resultsCount, (startFromChangeset != null) ? startFromChangeset.ToString(): "latest");

            var itemSpec = new ItemSpec(path, RecursionType.Full);
            var queryParameters = new QueryHistoryParameters(itemSpec);            
            queryParameters.IncludeChanges = true;

            if (resultsCount.HasValue)
            {
                queryParameters.MaxResults = resultsCount.Value;
            }

            if (startFromChangeset.HasValue)
            {
                queryParameters.VersionEnd = new ChangesetVersionSpec(startFromChangeset.Value);
            }

            foreach (var changeSet in versionControl.QueryHistory(queryParameters))
            {
                Models.Changeset changeSetToReturn = null;
             
                try
                {
                    var changedFiles = ProcessChangeset(changeSet).ToList();

                    changeSetToReturn = new Models.Changeset
                    {
                        ChangesetId = changeSet.ChangesetId,
                        Author = changeSet.CommitterDisplayName,
                        Date = changeSet.CreationDate,
                        ChangedFiles = changedFiles
                    };
                }
                catch (Exception exception)
                {                    
                    Logger.Error(exception, "Error during processing changeset {0}", changeSet.ChangesetId);
                }

                if (changeSetToReturn != null)
                {
                    yield return changeSetToReturn;
                }
            }
        }

        private IEnumerable<ChangedFile> ProcessChangeset(Changeset changeSet)
        {
            Logger.Info("Processing changeset {0}", changeSet.ChangesetId);

            foreach (var change in changeSet.Changes)
            {
                if (change.Item.Encoding == RepositoryConstants.EncodingBinary)
                {
                    Logger.Debug("Skipping binary file {0}", change.Item.ServerItem);
                    continue;
                }

                if (ignoredItemsMatcher.IsIgnored(change.Item.ServerItem))
                {
                    Logger.Debug("Ignoring file {0} according to the settings", change.Item.ServerItem);
                    continue;
                }
                
                IEnumerable<IChangeSegment> changeSegments = Enumerable.Empty<IChangeSegment>();

                if ((change.ChangeType & ChangeType.Add) != 0)
                {
                    changeSegments = ProcessAdd(change);
                }
                else if ((change.ChangeType & ChangeType.Delete) != 0)
                {
                    var previousItem = GetPreviousItem(change);
                    changeSegments = ProcessDelete(change, previousItem);
                }
                else if ((change.ChangeType & ChangeType.Edit) != 0)
                {
                    var previousItem = GetPreviousItem(change);
                    changeSegments = ProcessEdit(change, previousItem);
                }

                yield return new ChangedFile
                {
                    ChangeType = change.ChangeType,
                    ServerPath = change.Item.ServerItem,
                    ChangeSegments = changeSegments.ToList()
                };
            }
        }

        private Item GetPreviousItem(Change change)
        {            
            if (change.ChangeType.HasFlag(ChangeType.Rename))
            {
                Logger.Debug("Retrieving the previous item via QueryHistory");

                // renamed items receive different item id, so changeset - 1 trick does not work
                var spec = new ItemSpec(change.Item.ServerItem, RecursionType.None);
                var queryParameters = new QueryHistoryParameters(spec);
                queryParameters.MaxResults = 2;
                queryParameters.VersionEnd = new ChangesetVersionSpec(change.Item.ChangesetId);
                queryParameters.IncludeChanges = true;
                queryParameters.SlotMode = false; // track renames

                var changes = versionControl.QueryHistory(queryParameters).ToList();
                if (changes.Count > 1)
                {
                    return changes[1].Changes[0].Item;
                }
            }

            // when tracking deletes QueryHistory trick does not work, so use this instead
            Logger.Debug("Retrieving the previous item via GetItem ...ChangetsetId - 1");
            return versionControl.GetItem(change.Item.ItemId, change.Item.ChangesetId - 1, GetItemsOptions.IncludeSourceRenames);
        }

        private IEnumerable<IChangeSegment> ProcessEdit(Change change, Item prevItem)
        {
            Logger.Debug("Processing Edit change for {0}", change.Item.ServerItem);

            var previousVersionFileName = Path.GetTempFileName();
            var currentVersionFileName = Path.GetTempFileName();

            try
            {
                change.Item.DownloadFile(currentVersionFileName);

                prevItem.DownloadFile(previousVersionFileName);

                var diffSegment = Difference.DiffFiles(previousVersionFileName, FileType.Detect(previousVersionFileName, null),
                    currentVersionFileName, FileType.Detect(currentVersionFileName, null),
                    new DiffOptions { Flags = DiffOptionFlags.IgnoreWhiteSpace });

                int lastOriginalLine = 0;
                int lastModifiedLine = 0;

                while (diffSegment != null)
                {
                    var originalFileRange = new LinesRange(lastOriginalLine, diffSegment.OriginalStart);
                    var modifiedFileRange = new LinesRange(lastModifiedLine, diffSegment.ModifiedStart);

                    IChangeSegment changeSegment = null;
                    if (originalFileRange.IsEmpty)
                    {
                        if (!modifiedFileRange.IsEmpty)
                        {
                            changeSegment = new LinesAdded(modifiedFileRange);
                        }
                    }
                    else
                    {
                        if (!modifiedFileRange.IsEmpty)
                        {
                            changeSegment = new LinesModified(originalFileRange, modifiedFileRange);
                        }
                        else
                        {
                            changeSegment = new LinesRemoved(originalFileRange);
                        }
                    }

                    if (changeSegment != null)
                    {
                        yield return changeSegment;
                    }

                    lastOriginalLine = diffSegment.OriginalStart + diffSegment.OriginalLength;
                    lastModifiedLine = diffSegment.ModifiedStart + diffSegment.ModifiedLength;

                    diffSegment = diffSegment.Next;
                }
            }
            finally
            {
                File.Delete(previousVersionFileName);
                File.Delete(currentVersionFileName);
            }
        }

        private IEnumerable<IChangeSegment> ProcessDelete(Change change, Item prevItem)
        {            
            if (change.Item.ItemType != ItemType.File) yield break;
            Logger.Debug("Processing Delete change for {0}", change.Item.ServerItem);

            var fileName = Path.GetTempFileName();

            try
            {
                prevItem.DownloadFile(fileName);

                int linesCount = CountFileLines(fileName);
                var linesRange = new LinesRange(0, linesCount + 1);
                var linesRemoved = new LinesRemoved(linesRange);

                yield return linesRemoved;
            }
            finally
            {
                File.Delete(fileName);
            }
        }

        private IEnumerable<IChangeSegment> ProcessAdd(Change change)
        {
            if (change.Item.ItemType != ItemType.File) yield break;
            Logger.Debug("Processing Add change for {0}", change.Item.ServerItem);

            var fileName = Path.GetTempFileName();

            try
            {
                change.Item.DownloadFile(fileName);

                int linesCount = CountFileLines(fileName);
                var linesRange = new LinesRange(0, linesCount + 1);
                var linesAdded = new LinesAdded(linesRange);

                yield return linesAdded;
            }
            finally
            {
                File.Delete(fileName);
            }
        }

        private int CountFileLines(string fileName)
        {
            return File.ReadLines(fileName).Count();
        }
    }
}
