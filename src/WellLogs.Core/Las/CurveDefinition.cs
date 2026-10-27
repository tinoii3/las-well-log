namespace WellLogs.Core.Las 
{ 
	public sealed class CurveDefinition
	{
		public CurveDefinition(string mnemonic, string unit, string description, bool isArray)
		{ 
			Mnemonic = mnemonic;
			Unit = unit;
			Description = description;
			IsArray = isArray;
		}

		public string Mnemonic { get; }
		public string Unit { get; }
		public string Description { get; }
		public bool IsArray { get; }

	}
}