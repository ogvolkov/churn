namespace churn.Models
{
    public class LinesRemoved: IChangeSegment
    {
        public LinesRemoved(LinesRange originalFileRange)
        {
            OriginalFileRange = originalFileRange;
        }

        public LinesRange OriginalFileRange { get; set; }

        public LinesRange ModifiedFileRange => null;

        public int LinesCountChange => -OriginalFileRange.LinesCount;

        public int AffectedLinesCount => OriginalFileRange.LinesCount;
    }
}
