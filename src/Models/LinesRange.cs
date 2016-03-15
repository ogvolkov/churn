namespace churn.Models
{
    public class LinesRange
    {
        public readonly int Start;

        public readonly int PastEnd;

        public LinesRange(int start, int pastEnd)
        {
            this.Start = start;
            this.PastEnd = pastEnd;
        }        

        public int LinesCount => PastEnd - Start;

        public bool IsEmpty => PastEnd == Start;
    }
}
