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

		[Option("Backwalls on other planetoids", "Spaced Out: also give the asteroids you did not start on backwalls. Off matches the Aquatic Planet Pack, whose other planetoids have none.", "General")]
		[JsonProperty]
		public bool OtherPlanetoids { get; set; } = false;

		[Option("Starting biome coverage", "Coverage in whatever biome the colony starts in, replacing that biome's own value. The Aquatic Planet Pack fully walls its beach, hence 1.", "Starting biome", Format = "F2")]
		[Limit(0.0, 1.0)]
		[JsonProperty]
		public float StartCoverage { get; set; } = 1f;

		// Help above each dynamic section, as dynamic text blocks: static categories are sorted by name,
		// dynamic ones keep the order they are yielded in, after the static ones. PLib's text block does
		// not wrap on its own, so the line breaks are part of the string.
		public const string BiomesHelp = "Per biome: the backwall material, from the solids found there (None leaves it bare),\nand the coverage, a 0 to 1 band size used when it is not the starting biome.";
		public const string PatternsHelp = "Per subworld zone: the noise pattern the backwall patches follow. The pack's trees are shipped with the mod.\nThe kelp forest tree is about twice as dense as the others for the same coverage. Hover a zone for its biomes.";

		/// <summary>Material element id per biome group id; missing means the group's default.</summary>
		[JsonProperty]
		public Dictionary<string, string> Materials { get; set; } = new Dictionary<string, string>();

		/// <summary>Coverage fraction (0 to 1) per biome group id when it is not the starting biome; missing means the group's default.</summary>
		[JsonProperty]
		public Dictionary<string, float> Coverages { get; set; } = new Dictionary<string, float>();

		/// <summary>Backwall noise pattern id per subworld zone type; missing means the zone's default.</summary>
		[JsonProperty]
		public Dictionary<string, string> PatternIds { get; set; } = new Dictionary<string, string>();

		public Pattern PatternFor(PatternGroup group)
		{
			if (PatternIds != null && PatternIds.TryGetValue(group.Id, out string id))
			{
				Pattern chosen = Patterns.Find(id);
				if (chosen != null && (chosen.Tree != null || group.Vanilla))
					return chosen;
			}
			return Patterns.Find(group.DefaultPattern) ?? Patterns.All[0];
		}

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
			yield return new TextBlockOptionsEntry("BiomesHelp", new OptionAttribute(BiomesHelp, "", "Biomes"));
			foreach (BiomeGroup group in BiomeGroups.All)
				if (BiomeGroups.IsAvailable(group))
					yield return new BiomeOptionsEntry(group, this);
			yield return new TextBlockOptionsEntry("PatternsHelp", new OptionAttribute(PatternsHelp, "", "Patterns"));
			foreach (PatternGroup group in Patterns.Groups)
				if (group.IsAvailable)
					yield return new PatternOptionsEntry(group, this);
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
