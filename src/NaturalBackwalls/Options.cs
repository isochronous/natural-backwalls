using System.Collections.Generic;
using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace NaturalBackwalls
{
	/// <summary>
	/// Mod options. The per-biome material choices are dynamic entries (one dropdown per biome
	/// group that the active DLCs make relevant), stored in a dictionary keyed by group id.
	/// Read again for every world generated, so changes need no restart.
	/// </summary>
	[JsonObject(MemberSerialization.OptIn)]
	[ConfigFile(SharedConfigLocation: true)]
	public sealed class Options : IOptions
	{
		public const string NoMaterial = "None";

		[Option("Coverage", "Fraction of the cells in a biome that get a backwall. The Aquatic Planet Pack uses 0.2 outside its beach.", "Coverage", Format = "F2")]
		[Limit(0.05, 1.0)]
		[JsonProperty]
		public float Coverage { get; set; } = 0.2f;

		[Option("Starting biome coverage", "Coverage in starting biomes (Sandstone, Forest, Swamp, Garden), which the Aquatic pack fully walls like its beach.", "Coverage", Format = "F2")]
		[Limit(0.0, 1.0)]
		[JsonProperty]
		public float StartCoverage { get; set; } = 1f;

		[JsonProperty]
		public Dictionary<string, string> Materials { get; set; } = new Dictionary<string, string>();

		/// <summary>The configured material for a group, or its default when never set.</summary>
		public string MaterialFor(BiomeGroup group)
		{
			if (Materials != null && Materials.TryGetValue(group.Id, out string chosen) && !string.IsNullOrEmpty(chosen))
				return chosen;
			return group.DefaultMaterial;
		}

		public IEnumerable<IOptionsEntry> CreateOptions()
		{
			foreach (BiomeGroup group in BiomeGroups.All)
				if (BiomeGroups.IsAvailable(group))
					yield return new MaterialOptionsEntry(group);
		}

		public void OnOptionsChanged()
		{
		}

		/// <summary>Reads the current options from disk (falls back to defaults).</summary>
		public static Options Load()
		{
			return POptions.ReadSettings<Options>() ?? new Options();
		}
	}
}
