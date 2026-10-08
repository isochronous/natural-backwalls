using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Klei;
using ProcGen;

namespace NaturalBackwalls
{
	/// <summary>
	/// Backwalls are plain worldgen data: a subworld opts in with a backwallNoise tree, and the
	/// biome band table "<biome>_backwall" picks the element from that noise (non-solids mean no
	/// backwall). The Aquatic Planet Pack ships both for its own biomes; this patch adds them for
	/// everything else, on the per-world copy of the settings, right before the game computes the
	/// band thresholds. The decision itself lives in Plan(), which the YAML export reuses.
	/// </summary>
	[HarmonyPatch(typeof(SettingsCache), nameof(SettingsCache.CloneInToNewWorld))]
	public static class CloneInToNewWorld_Patch
	{
		/// <summary>Base-game tree the Aquatic pack itself uses for two of its subworlds; the fallback when a shipped tree is missing.</summary>
		public const string FallbackNoise = "noise/SandstoneStrange";
		public const string Suffix = "_backwall";

		private static readonly MethodInfo SetBackwallNoise =
			typeof(SubWorld).GetProperty(nameof(SubWorld.backwallNoise)).GetSetMethod(true);

		private static readonly Dictionary<string, string> resolvedTrees = new Dictionary<string, string>();

		/// <summary>Biome keys whose backwall table this mod built for the world being generated (not vanilla ones).</summary>
		public static readonly HashSet<string> ModdedBiomes = new HashSet<string>();
		/// <summary>Whether feature rooms get backwalls too; read from the options per world.</summary>
		public static bool FeatureBackwalls = true;
		/// <summary>Whether worlds other than the starting one get backwalls; read from the options per world.</summary>
		public static bool OtherPlanetoids = false;
		/// <summary>Subworlds this mod enabled, per world, so the starting-world check can undo them.</summary>
		public static readonly Dictionary<ProcGen.World, List<SubWorld>> EnabledSubworlds = new Dictionary<ProcGen.World, List<SubWorld>>();

		/// <summary>What Plan() decided for one subworld.</summary>
		public sealed class SubworldPlan
		{
			public string Name;
			public SubWorld Subworld;
			/// <summary>Tree to set, or null to leave the subworld's own noise (or none) as is.</summary>
			public string Tree;
			/// <summary>Band tables built for this subworld's biomes (keys carry the "_backwall" suffix); vanilla tables kept are not listed.</summary>
			public readonly Dictionary<string, ElementBandConfiguration> Bands = new Dictionary<string, ElementBandConfiguration>();
			public bool Enabled;
		}

		public static void Postfix(MutatedWorldData worldData)
		{
			// Any failure here must leave worldgen as the game and other mods set it up.
			try
			{
				Apply(worldData);
			}
			catch (System.Exception e)
			{
				Debug.LogWarning("[NaturalBackwalls] Skipped (worldgen left untouched): " + e);
			}
		}

		private static void Apply(MutatedWorldData worldData)
		{
			if (worldData?.subworlds == null || worldData.biomes == null)
				return;
			Options options = Options.Load();
			ModdedBiomes.Clear();
			FeatureBackwalls = options.FeatureBackwalls;
			OtherPlanetoids = options.OtherPlanetoids;
			List<SubWorld> enabledHere = new List<SubWorld>();
			if (worldData.world != null)
				EnabledSubworlds[worldData.world] = enabledHere;
			Dictionary<string, ElementBandConfiguration> bands = worldData.biomes.BiomeBackgroundElementBandConfigurations;
			HashSet<string> startBiomes = StartBiomes(worldData);
			int enabled = 0;
			foreach (SubworldPlan plan in Plan(worldData.subworlds, bands, startBiomes, options, out int skipped))
			{
				foreach (KeyValuePair<string, ElementBandConfiguration> band in plan.Bands)
				{
					bands[band.Key] = band.Value;
					if (HasSolid(band.Value))
						ModdedBiomes.Add(band.Key.Substring(0, band.Key.Length - Suffix.Length));
				}
				if (plan.Tree != null)
					SetBackwallNoise.Invoke(plan.Subworld, new object[] { plan.Tree });
				if (plan.Enabled)
				{
					enabledHere.Add(plan.Subworld);
					enabled++;
				}
			}
			Debug.Log("[NaturalBackwalls] Backwalls enabled in " + enabled + " subworlds, "
				+ startBiomes.Count + " starting biome(s) at coverage " + options.StartCoverage);
		}

		/// <summary>
		/// Decides, for every subworld, which noise tree to give it and which "_backwall" band tables
		/// to add for its biomes, without changing anything. Existing tables are consulted (and kept
		/// when vanilla and unchanged) through <paramref name="existingBands"/>.
		/// </summary>
		public static List<SubworldPlan> Plan(IDictionary<string, SubWorld> subworlds, IDictionary<string, ElementBandConfiguration> existingBands,
			HashSet<string> startBiomes, Options options, out int skippedSurface)
		{
			var plans = new List<SubworldPlan>();
			var builtHere = new Dictionary<string, ElementBandConfiguration>();
			skippedSurface = 0;
			foreach (KeyValuePair<string, SubWorld> pair in subworlds)
			{
				SubWorld subworld = pair.Value;
				if (subworld == null || subworld.biomes == null)
					continue;
				bool hasNoise = !string.IsNullOrEmpty(subworld.backwallNoise); // the Aquatic pack (or another mod)
				if (!hasNoise && IsSurface(pair.Key, subworld))
				{
					skippedSurface++;
					continue;
				}
				var plan = new SubworldPlan { Name = pair.Key, Subworld = subworld };
				bool anySolid = false;
				foreach (WeightedBiome biome in subworld.biomes)
				{
					string key = biome.name + Suffix;
					bool isStart = startBiomes.Contains(biome.name);
					if (builtHere.TryGetValue(key, out ElementBandConfiguration already))
					{
						anySolid |= HasSolid(already);
						continue;
					}
					if (existingBands.TryGetValue(key, out ElementBandConfiguration existing) && KeepVanilla(biome.name, isStart, options))
					{
						anySolid |= HasSolid(existing);
						continue;
					}
					existingBands.TryGetValue(biome.name, out ElementBandConfiguration terrain);
					ElementBandConfiguration config = Build(biome.name, isStart, options, terrain, out bool solid);
					plan.Bands[key] = config;
					builtHere[key] = config;
					anySolid |= solid;
				}
				PatternGroup zone = Patterns.GroupFor(subworld.zoneType);
				Pattern pattern = zone != null ? options.PatternFor(zone) : null;
				if (hasNoise)
				{
					// The game's own backwalled subworld: only a pattern chosen away from "as the game made it" changes its tree.
					if (pattern?.Tree != null)
						plan.Tree = TreeName(pattern);
				}
				else if (anySolid)
				{
					plan.Tree = TreeName(pattern ?? Patterns.All[0]);
					plan.Enabled = true;
				}
				plans.Add(plan);
			}
			return plans;
		}

		/// <summary>Undoes the subworld enabling for a world that is not the starting one, when the option says so.</summary>
		public static void ClearBackwallNoise(ProcGen.World world)
		{
			if (world == null || !EnabledSubworlds.TryGetValue(world, out List<SubWorld> subworlds))
				return;
			foreach (SubWorld subworld in subworlds)
				SetBackwallNoise.Invoke(subworld, new object[] { null });
			EnabledSubworlds.Remove(world);
			Debug.Log("[NaturalBackwalls] Not the starting world: backwalls left off in " + subworlds.Count + " subworlds (option \"Backwalls on other planetoids\" is off)");
		}

		/// <summary>The pattern's tree if its file can be found, else the base-game fallback.</summary>
		public static string TreeName(Pattern pattern)
		{
			string tree = pattern.Tree ?? FallbackNoise;
			if (resolvedTrees.TryGetValue(tree, out string resolved))
				return resolved;
			string path = SettingsCache.RewriteWorldgenPathYaml(tree);
			if (!FileSystem.FileExists(path))
			{
				Debug.LogWarning("[NaturalBackwalls] " + path + " not found (is the mod's worldgen folder missing?); using " + FallbackNoise);
				resolved = FallbackNoise;
			}
			else
				resolved = tree;
			resolvedTrees[tree] = resolved;
			return resolved;
		}

		/// <summary>The biomes of the world's starting subworld; they get the starting-biome coverage.</summary>
		public static HashSet<string> StartBiomes(MutatedWorldData worldData)
		{
			HashSet<string> result = new HashSet<string>();
			string start = worldData.world?.startSubworldName;
			if (!string.IsNullOrEmpty(start) && worldData.subworlds.TryGetValue(start, out SubWorld subworld) && subworld?.biomes != null)
				foreach (WeightedBiome biome in subworld.biomes)
					result.Add(biome.name);
			return result;
		}

		/// <summary>
		/// A subworld open to space must stay open: a backwalled cell is never "open to space"
		/// (Grid.IsCellOpenToSpace), which would break solar panels, rockets and meteor exposure.
		/// </summary>
		public static bool IsSurface(string name, SubWorld subworld)
		{
			if (subworld.zoneType == SubWorld.ZoneType.Space || subworld.zoneType == SubWorld.ZoneType.RocketInterior)
				return true;
			if (name.Contains("Surface") || name.Contains("/space/"))
				return true;
			foreach (WeightedBiome biome in subworld.biomes)
				if (biome.name.Contains("Surface") || biome.name.EndsWith("/Space"))
					return true;
			return false;
		}

		/// <summary>
		/// A biome the game already gives backwalls keeps its own band table as long as the group's
		/// material and coverage are still the vanilla defaults, so untouched settings mean untouched
		/// worldgen (the beach's algae pockets stay Dirt, for one). Changing either replaces the table.
		/// </summary>
		private static bool KeepVanilla(string biomeName, bool isStartBiome, Options options)
		{
			BiomeGroup group = BiomeGroups.ForBiome(biomeName);
			if (group == null)
				return true;
			if (!group.Vanilla)
				return false;
			float coverage = isStartBiome ? UnityEngine.Mathf.Clamp01(options.StartCoverage) : options.CoverageFor(group);
			return options.MaterialFor(group) == group.DefaultMaterial && UnityEngine.Mathf.Approximately(coverage, group.DefaultCoverage);
		}

		/// <summary>
		/// The band table for one biome. Known biomes use their group's settings; a biome from another
		/// mod (no group) gets its own most common solid at the other-mod coverage, when that option is on.
		/// </summary>
		private static ElementBandConfiguration Build(string biomeName, bool isStartBiome, Options options, ElementBandConfiguration terrain, out bool solid)
		{
			ElementBandConfiguration config = new ElementBandConfiguration();
			BiomeGroup group = BiomeGroups.ForBiome(biomeName);
			string material = group != null ? options.MaterialFor(group) : (options.OtherModBiomes ? DominantSolid(terrain) : null);
			Element element = material != null && material != Options.NoMaterial ? ElementLoader.FindElementByName(material) : null;
			if (element == null || !element.IsSolid)
			{
				config.Add(new ElementGradient(SimHashes.Vacuum.ToString(), 1f, null));
				solid = false;
				return config;
			}
			float coverage = isStartBiome ? UnityEngine.Mathf.Clamp01(options.StartCoverage)
				: group != null ? options.CoverageFor(group) : UnityEngine.Mathf.Clamp01(options.OtherModCoverage);
			if (coverage <= 0f)
			{
				config.Add(new ElementGradient(SimHashes.Vacuum.ToString(), 1f, null));
				solid = false;
				return config;
			}
			config.Add(new ElementGradient(material, coverage, null));
			if (coverage < 1f)
				config.Add(new ElementGradient(SimHashes.Vacuum.ToString(), 1f - coverage, null));
			solid = true;
			return config;
		}

		/// <summary>The solid with the largest total band in a biome's terrain table, skipping the indestructible ones; null if none.</summary>
		private static string DominantSolid(ElementBandConfiguration terrain)
		{
			if (terrain == null)
				return null;
			var totals = new Dictionary<string, float>();
			foreach (ElementGradient band in terrain)
			{
				if (band.content == null)
					continue;
				Element element = ElementLoader.FindElementByName(band.content);
				if (element == null || !element.IsSolid || element.id == SimHashes.Unobtanium || element.id == SimHashes.Katairite)
					continue;
				totals[band.content] = (totals.TryGetValue(band.content, out float t) ? t : 0f) + band.bandSize;
			}
			string best = null;
			float bestTotal = 0f;
			foreach (KeyValuePair<string, float> pair in totals)
				if (pair.Value > bestTotal) { best = pair.Key; bestTotal = pair.Value; }
			return best;
		}

		public static bool HasSolid(ElementBandConfiguration config)
		{
			foreach (ElementGradient band in config)
			{
				Element element = ElementLoader.FindElementByName(band.content);
				if (element != null && element.IsSolid)
					return true;
			}
			return false;
		}
	}
}
