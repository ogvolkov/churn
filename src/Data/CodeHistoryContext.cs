using System.Data.Entity;

namespace churn.Data
{
    public class CodeHistoryContext: DbContext
    {
        public DbSet<Author> Authors { get; set; }

        public DbSet<Changeset> Changesets { get; set; }

        public DbSet<File> Files { get; set; }

        public DbSet<Change> Changes { get; set; }

        public CodeHistoryContext() : base("CodeHistoryConnectionString")
        {            
        }
    }
}
