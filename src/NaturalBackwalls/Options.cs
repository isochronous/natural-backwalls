using System.Collections.Generic;
using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace NaturalBackwalls
{
	/// <summary>
	/// Mod options: one row per biome group (material dropdown plus coverage), created dynamically
	/// for the groups the active DLCs make relevant and stored in dictionaries keyed by group id.
	/// Read again for every world generated, so changes need no restart.
	/// </summary>
	[JsonObject(MemberSerialization.OptIn)]
	[ConfigFile(SharedConfigLocation: true)]
	public sealed class Options : IOptions
	{
		public const string NoMaterial = "None";

		[Option("Backwalls behind carved caves and lakes", "Worldgen carves feature rooms (caves, lakes, geodes) into a biome after claiming their cells, and the game's own backwall pass skips claimed cells, so those rooms would stay bare. On, they get backwalls from the same noise as the rest of the biome.", "General")]
		[JsonProperty]
		public bool FeatureBackwalls { get; set; } = true;

		[Option("Starting biome coverage", "Coverage in whatever biome the colony starts in, replacing that biome's own value. The Aquatic Planet Pack fully walls its beach, hence 1.", "Starting biome", Format = "F2")]
		[Limit(0.0, 1.0)]
		[JsonProperty]
		public float StartCoverage { get; set; } = 1f;

		/// <summary>Material element id per biome group id; missing means the group's default.</summary>
		[JsonProperty]
		public Dictionary<string, string> Materials { get; set; } = new Dictionary<string, string>();

		/// <summary>Coverage fraction (0 to 1) per biome group id when it is not the starting biome; missing means the group's default.</summary>
		[JsonProperty]
		public Dictionary<string, float> Coverages { get; set; } = new Dictionary<string, float>();

		public string MaterialFor(BiomeGroup group)
		{
			if (Materials != null && Materials.TryGetValue(group.Id, out string chosen) && !string.IsNullOrEmpty(chosen))
				return chosen;
			return group.DefaultMaterial;
		}

		public float CoverageFor(BiomeGroup group)
		{
			if (Coverages != null && Coverages.TryGetValue(group.Id, out float coverage))
				return UnityEngine.Mathf.Clamp01(coverage);
			return group.DefaultCoverage;
		}

		public IEnumerable<IOptionsEntry> CreateOptions()
		{
			foreach (BiomeGroup group in BiomeGroups.All)
				if (BiomeGroups.IsAvailable(group))
					yield return new BiomeOptionsEntry(group, this);
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
