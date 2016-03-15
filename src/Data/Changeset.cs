using System;
using System.Collections.Generic;

namespace churn.Data
{
    public class Changeset
    {
        public int Id { get; set; }

        public int VersionControlId { get; set; }

        public Author Author { get; set; }

        public DateTimeOffset Date { get; set; }

        public List<File> Files { get; set; }
    }
}
