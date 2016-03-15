using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace churn.Tfs
{
    public class IgnoredItemsMatcher
    {
        private readonly ICollection<Regex> regexes = new List<Regex>();

        public IgnoredItemsMatcher()
        {
            foreach (var ignoreWildcard in Properties.Settings.Default.IgnoredPaths)
            {
                var regexBody = Regex.Escape(ignoreWildcard).Replace(@"\*", ".*").Replace(@"\?", ".");
                string pattern = $"^{regexBody}$";
                var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

                regexes.Add(regex);
            }
        }

        public bool IsIgnored(string path)
        {
            return regexes.Any(regex => regex.IsMatch(path));
        }
    }
}
