using System;
using System.Collections.Generic;

namespace churn.Models
{
    public class Changeset
    {
        public int ChangesetId { get; set; }

        public string Author { get; set; }

        public DateTimeOffset Date { get; set; }

        public List<ChangedFile> ChangedFiles { get; set; }        
    }
}
