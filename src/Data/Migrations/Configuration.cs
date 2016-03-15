using System.Data.Entity.Migrations;

namespace churn.Data.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<Data.CodeHistoryContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            ContextKey = "churn.Data.CodeHistoryContext";
        }

        protected override void Seed(CodeHistoryContext context)
        {         
        }
    }
}
