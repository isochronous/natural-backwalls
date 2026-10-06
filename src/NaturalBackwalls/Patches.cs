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
	/// band thresholds.
	/// </summary>
	[HarmonyPatch(typeof(SettingsCache), nameof(SettingsCache.CloneInToNewWorld))]
	public static class CloneInToNewWorld_Patch
	{
		/// <summary>The mod's copy of the Aquatic pack's reef noise, so it works without that DLC.</summary>
		private const string ModNoise = "noise/NaturalBackwalls";
		/// <summary>Base-game tree the Aquatic pack itself uses for two of its subworlds.</summary>
		private const string FallbackNoise = "noise/SandstoneStrange";
		private const string Suffix = "_backwall";

		private static readonly MethodInfo SetBackwallNoise =
			typeof(SubWorld).GetProperty(nameof(SubWorld.backwallNoise)).GetSetMethod(true);

		private static string noiseName;

		/// <summary>Biome keys whose backwall table this mod built for the world being generated (not vanilla ones).</summary>
		public static readonly HashSet<string> ModdedBiomes = new HashSet<string>();
		/// <summary>Whether feature rooms get backwalls too; read from the options per world.</summary>
		public static bool FeatureBackwalls = true;

		public static void Postfix(MutatedWorldData worldData)
		{
			if (worldData?.subworlds == null || worldData.biomes == null)
				return;
			Options options = Options.Load();
			ModdedBiomes.Clear();
			FeatureBackwalls = options.FeatureBackwalls;
			Dictionary<string, ElementBandConfiguration> bands = worldData.biomes.BiomeBackgroundElementBandConfigurations;
			string noise = NoiseName();
			HashSet<string> startBiomes = StartBiomes(worldData);
			int enabled = 0, skipped = 0;
			foreach (KeyValuePair<string, SubWorld> pair in worldData.subworlds)
			{
				SubWorld subworld = pair.Value;
				if (subworld == null || subworld.biomes == null)
					continue;
				bool hasNoise = !string.IsNullOrEmpty(subworld.backwallNoise); // the Aquatic pack (or another mod)
				if (!hasNoise && IsSurface(pair.Key, subworld))
				{
					skipped++;
					continue;
				}
				bool anySolid = false;
				foreach (WeightedBiome biome in subworld.biomes)
				{
					string key = biome.name + Suffix;
					bool isStart = startBiomes.Contains(biome.name);
					if (bands.TryGetValue(key, out ElementBandConfiguration existing) && KeepVanilla(biome.name, isStart, options))
					{
						anySolid |= HasSolid(existing);
						continue;
					}
					ElementBandConfiguration config = Build(biome.name, isStart, options, out bool solid);
					bands[key] = config;
					if (solid) ModdedBiomes.Add(biome.name);
					anySolid |= solid;
				}
				if (hasNoise)
					continue;
				if (anySolid)
				{
					SetBackwallNoise.Invoke(subworld, new object[] { noise });
					enabled++;
				}
			}
			Debug.Log("[NaturalBackwalls] Backwalls enabled in " + enabled + " subworlds (" + skipped + " surface/space subworlds left open), "
				+ startBiomes.Count + " starting biome(s) at coverage " + options.StartCoverage + ", noise " + noise);
		}

		/// <summary>The biomes of the world's starting subworld; they get the starting-biome coverage.</summary>
		private static HashSet<string> StartBiomes(MutatedWorldData worldData)
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
		private static bool IsSurface(string name, SubWorld subworld)
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

		private static ElementBandConfiguration Build(string biomeName, bool isStartBiome, Options options, out bool solid)
		{
			ElementBandConfiguration config = new ElementBandConfiguration();
			BiomeGroup group = BiomeGroups.ForBiome(biomeName);
			string material = group != null ? options.MaterialFor(group) : null;
			Element element = material != null && material != Options.NoMaterial ? ElementLoader.FindElementByName(material) : null;
			if (element == null || !element.IsSolid)
			{
				config.Add(new ElementGradient(SimHashes.Vacuum.ToString(), 1f, null));
				solid = false;
				return config;
			}
			float coverage = isStartBiome ? UnityEngine.Mathf.Clamp01(options.StartCoverage) : options.CoverageFor(group);
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

		private static bool HasSolid(ElementBandConfiguration config)
		{
			foreach (ElementGradient band in config)
			{
				Element element = ElementLoader.FindElementByName(band.content);
				if (element != null && element.IsSolid)
					return true;
			}
			return false;
		}

		private static string NoiseName()
		{
			if (noiseName != null)
				return noiseName;
			string path = SettingsCache.RewriteWorldgenPathYaml(ModNoise);
			if (FileSystem.FileExists(path))
				noiseName = ModNoise;
			else
			{
				Debug.LogWarning("[NaturalBackwalls] " + path + " not found (is the mod's worldgen folder missing?); using " + FallbackNoise);
				noiseName = FallbackNoise;
			}
			return noiseName;
		}
	}
}
