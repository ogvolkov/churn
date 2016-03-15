using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace churn.Data
{
    public class File
    {
        public int Id { get; set; }

        public ChangeType ChangeType { get; set; }

        public string ServerPath { get; set; }

        public List<Change> Changes { get; set; }
    }
}
