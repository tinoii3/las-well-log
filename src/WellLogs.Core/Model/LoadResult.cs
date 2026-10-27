using System.Collections.Generic;
using System.Linq;

namespace WellLogs.Core.Model
{
    public sealed class LoadResult
    {
        private LoadResult(string filePath, LoadStatus status, Well? well, IReadOnlyList<Issue> issues)
        {
            FilePath = filePath;
            Status = status;
            Well = well;
            Issues = issues;
        }

        public string FilePath { get; }
        public LoadStatus Status { get; }
        public Well? Well { get; }
        public IReadOnlyList<Issue> Issues { get; }
        public bool IsLoaded => Well != null;

        public static LoadResult Success(string filePath, Well well, IEnumerable<Issue> issues)
        {
            List<Issue> list = issues.ToList();
            LoadStatus status = list.Any(i => i.Severity != IssueSeverity.Info) ? LoadStatus.OkWithWarnings : LoadStatus.Ok;

            return new LoadResult(filePath, status, well, list);
        }

        public static LoadResult Unsupported(string filePath, IEnumerable<Issue> issues)
        {
            return new LoadResult(filePath, LoadStatus.Unsupported, null, issues.ToList());
        }

        public static LoadResult Failed(string filePath, IEnumerable<Issue> issues)
        {
            return new LoadResult(filePath, LoadStatus.Failed, null, issues.ToList());
        }
    }
}