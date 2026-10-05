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

		public static void Postfix(MutatedWorldData worldData)
		{
			if (worldData?.subworlds == null || worldData.biomes == null)
				return;
			Options options = Options.Load();
			Dictionary<string, ElementBandConfiguration> bands = worldData.biomes.BiomeBackgroundElementBandConfigurations;
			string noise = NoiseName();
			int enabled = 0, skipped = 0;
			foreach (KeyValuePair<string, SubWorld> pair in worldData.subworlds)
			{
				SubWorld subworld = pair.Value;
				if (subworld == null || subworld.biomes == null)
					continue;
				if (!string.IsNullOrEmpty(subworld.backwallNoise))
					continue; // the Aquatic pack (or another mod) already handles it
				if (IsSurface(pair.Key, subworld))
				{
					skipped++;
					continue;
				}
				bool anySolid = false;
				foreach (WeightedBiome biome in subworld.biomes)
				{
					string key = biome.name + Suffix;
					if (bands.TryGetValue(key, out ElementBandConfiguration existing))
					{
						anySolid |= HasSolid(existing);
						continue;
					}
					ElementBandConfiguration config = Build(biome.name, pair.Key, options, out bool solid);
					bands[key] = config;
					anySolid |= solid;
				}
				if (anySolid)
				{
					SetBackwallNoise.Invoke(subworld, new object[] { noise });
					enabled++;
				}
			}
			Debug.Log("[NaturalBackwalls] Backwalls enabled in " + enabled + " subworlds (" + skipped + " surface/space subworlds left open), noise " + noise);
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

		private static ElementBandConfiguration Build(string biomeName, string subworldName, Options options, out bool solid)
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
			float coverage = group.IsStartBiome || subworldName.Contains("Start") ? options.StartCoverage : options.Coverage;
			coverage = UnityEngine.Mathf.Clamp01(coverage);
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
