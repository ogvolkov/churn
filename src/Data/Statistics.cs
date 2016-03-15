using System;
using System.Collections.Generic;

namespace churn.Data
{
    public class Statistics
    {
        public DateTimeOffset StartDate { get; set; }

        public DateTimeOffset EndDate { get; set; }

        public List<AuthorStats> AuthorStats { get; set; }
    }
}
