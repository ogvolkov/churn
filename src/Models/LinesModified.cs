using System;

namespace churn.Models
{
    public class LinesModified: IChangeSegment
    {
        public LinesModified(LinesRange originalFileRange, LinesRange modifiedFileRange)
        {
            OriginalFileRange = originalFileRange;
            ModifiedFileRange = modifiedFileRange;
        }

        public LinesRange OriginalFileRange { get; set; }

        public LinesRange ModifiedFileRange { get; set; }

        public int LinesCountChange => ModifiedFileRange.LinesCount - OriginalFileRange.LinesCount;

        public int AffectedLinesCount => Math.Max(ModifiedFileRange.LinesCount, OriginalFileRange.LinesCount);
    }
}
