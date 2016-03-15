namespace churn.Data
{
    public class Change
    {
        public int Id { get; set; }

        public Location OriginalFileLocation { get; set; }

        public Location ModifiedFileLocation { get; set; }            
    }
}
