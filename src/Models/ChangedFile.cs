using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace churn.Models
{
    public class ChangedFile
    {
        public ChangeType ChangeType { get; set; }

        public string ServerPath { get; set; }

        public List<IChangeSegment> ChangeSegments { get; set; }        
    }
}
