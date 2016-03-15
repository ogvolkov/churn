namespace churn.Models
{
    public interface IChangeSegment
    {
        LinesRange OriginalFileRange { get; }

        LinesRange ModifiedFileRange { get; }

        int LinesCountChange { get; }

        int AffectedLinesCount { get; }
    }
}
