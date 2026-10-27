namespace WellLogs.Core.Model
{
    public sealed class Issue
    {
        public Issue(IssueSeverity severity, string code, string message, int? lineNumber = null)
        {
            Severity = severity;
            Code = code;
            Message = message;
            LineNumber = lineNumber;
        }

        public IssueSeverity Severity { get;  }
        public string Code { get;  }
        public string Message { get;  }
        public int? LineNumber { get; }

        public override string ToString()
        {
            string line = LineNumber.HasValue ? " (line " + LineNumber.Value + ")" : "";
            return "[" + Severity + "]" + Code + ": " + Message + line;
        }
    }
}