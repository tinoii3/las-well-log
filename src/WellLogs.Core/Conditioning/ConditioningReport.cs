using System.Collections.Generic;
using WellLogs.Core.Model;

namespace WellLogs.Core.Conditioning
{
    public sealed class ConditioningReport
    {
        private readonly List<Issue> _entries = new List<Issue>();

        public IReadOnlyList<Issue> Entries => _entries;

        public void Info(string code, string message)
        {
            _entries.Add(new Issue(IssueSeverity.Info, code, message));
        }

        public void Warn(string code, string message)
        {
            _entries.Add(new Issue(IssueSeverity.Warning, code, message));
        }

        public void Error(string code, string message)
        {
            _entries.Add(new Issue(IssueSeverity.Error, code, message));
        }
    }
}