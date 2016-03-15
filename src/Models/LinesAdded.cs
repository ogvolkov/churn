namespace churn.Models
{
    public class LinesAdded: IChangeSegment
    {
        public LinesAdded(LinesRange modifiedFileRange)
        {
            ModifiedFileRange = modifiedFileRange;
        }

        public LinesRange OriginalFileRange => null;

        public LinesRange ModifiedFileRange { get; }

        public int LinesCountChange => ModifiedFileRange.LinesCount;

        public int AffectedLinesCount => ModifiedFileRange.LinesCount;
    }
}
