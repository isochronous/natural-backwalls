using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Klei;
using ProcGen;
using ProcGenGame;
using Path = System.IO.Path;

namespace NaturalBackwalls
{
	/// <summary>
	/// Writes the mod's current settings as a plain worldgen overlay: a mod-style folder of YAML
	/// files (subworlds with a backwallNoise line, biome files with "_backwall" tables, the shipped
	/// noise trees) that an external renderer such as onimaxxing.com/worldgen or the BiomePreview
	/// tool can layer over the game's files. The feature-room pass is runtime code with no YAML
	/// equivalent, so an external render shows feature caves bare.
	/// Files are the game's own, copied and edited at the text level, so everything else in them
	/// survives verbatim.
	/// </summary>
	public static class ExportOverlay
	{
		public static string TargetFolder => Path.Combine(Util.RootFolder(), "NaturalBackwalls-overlay");

		public static void Run()
		{
			try
			{
				WorldGen.WaitForPendingLoadSettings();
				if (SettingsCache.subworlds == null || SettingsCache.subworlds.Count == 0)
				{
					Debug.LogWarning("[NaturalBackwalls] Export: worldgen settings are not loaded yet");
					return;
				}
				string target = TargetFolder;
				if (Directory.Exists(target))
					Directory.Delete(target, true);
				Directory.CreateDirectory(target);

				Options options = Options.Load();
				var bands = SettingsCache.biomes.BiomeBackgroundElementBandConfigurations;
				// No single world here: starting-biome coverage is applied per world at generation time,
				// so the export uses each biome's own coverage.
				var plans = CloneInToNewWorld_Patch.Plan(SettingsCache.subworlds, bands, new HashSet<string>(), options, out int skipped);

				int subworldFiles = 0;
				var biomeTables = new Dictionary<string, Dictionary<string, ElementBandConfiguration>>(); // biome file -> key -> table
				foreach (CloneInToNewWorld_Patch.SubworldPlan plan in plans)
				{
					if (plan.Tree != null)
					{
						WriteSubworld(target, plan.Name, plan.Tree);
						subworldFiles++;
					}
					foreach (KeyValuePair<string, ElementBandConfiguration> band in plan.Bands)
					{
						string biomeKey = band.Key.Substring(0, band.Key.Length - CloneInToNewWorld_Patch.Suffix.Length);
						int slash = biomeKey.LastIndexOf('/');
						if (slash <= 0)
							continue; // not a "file/Key" biome name; nothing to write a file for
						string file = biomeKey.Substring(0, slash), key = biomeKey.Substring(slash + 1);
						if (!biomeTables.TryGetValue(file, out var tables))
							biomeTables[file] = tables = new Dictionary<string, ElementBandConfiguration>();
						tables[key + CloneInToNewWorld_Patch.Suffix] = band.Value;
					}
				}
				foreach (KeyValuePair<string, Dictionary<string, ElementBandConfiguration>> pair in biomeTables)
					WriteBiomeFile(target, pair.Key, pair.Value);
				int trees = CopyNoiseTrees(target);
				File.WriteAllText(Path.Combine(target, "README.txt"),
					"Natural Backwalls worldgen overlay, exported " + System.DateTime.Now.ToString("s") + ".\n"
					+ "Mod-style worldgen folder: layer it over the game's files (onimaxxing.com/worldgen \"Upload YAML\", or BiomePreview's overlay folder).\n"
					+ "Subworlds carry the backwall noise tree, biome files the _backwall band tables, worldgen/noise the shipped trees.\n"
					+ "Not representable here: the feature-room pass (feature caves show bare) and the starting-biome coverage (per-biome coverage is used).\n");
				Debug.Log("[NaturalBackwalls] Exported overlay to " + target + ": " + subworldFiles + " subworld files, " + biomeTables.Count + " biome files, " + trees + " noise trees (" + skipped + " surface subworlds skipped)");
			}
			catch (Exception e)
			{
				Debug.LogWarning("[NaturalBackwalls] Export failed: " + e);
			}
		}

		/// <summary>"dlc5::subworlds/reef/ReefBasic" -> "dlc/dlc5/worldgen/subworlds/reef/ReefBasic.yaml".</summary>
		private static string RelativePath(string scoped)
		{
			SettingsCache.GetDlcIdAndPath(scoped, out string dlcId, out string path);
			string dir = string.IsNullOrEmpty(dlcId) ? "" : Path.Combine("dlc", DlcManager.GetContentDirectoryName(dlcId));
			string rel = Path.Combine(dir, "worldgen", path.Replace('/', Path.DirectorySeparatorChar));
			return rel.EndsWith(".yaml") ? rel : rel + ".yaml";
		}

		/// <summary>Reads a worldgen file through the game's layered file system (mods included).</summary>
		private static string ReadWorldgen(string scoped)
		{
			string path = SettingsCache.RewriteWorldgenPathYaml(scoped);
			FileHandle handle = FileSystem.FindFileHandle(path);
			if (handle.source == null)
				return null;
			return FileSystem.ConvertToText(handle.source.ReadBytes(handle.full_path));
		}

		private static void WriteSubworld(string target, string name, string tree)
		{
			string text = ReadWorldgen(name);
			if (text == null)
				return;
			string line = "backwallNoise: " + tree;
			if (Regex.IsMatch(text, @"^backwallNoise:.*$", RegexOptions.Multiline))
				text = Regex.Replace(text, @"^backwallNoise:.*$", line, RegexOptions.Multiline);
			else if (Regex.IsMatch(text, @"^biomeNoise:.*$", RegexOptions.Multiline))
				text = Regex.Replace(text, @"^(biomeNoise:.*)$", "$1\n" + line, RegexOptions.Multiline);
			else
				text = line + "\n" + text;
			Save(target, RelativePath(name), text);
		}

		/// <summary>
		/// Appends (or replaces) the "_backwall" tables at the end of the biome file's "add:" mapping,
		/// which is the last mapping in the game's biome files.
		/// </summary>
		private static void WriteBiomeFile(string target, string file, Dictionary<string, ElementBandConfiguration> tables)
		{
			string text = ReadWorldgen(file);
			if (text == null)
				return;
			text = text.Replace("\r\n", "\n").TrimEnd('\n') + "\n";
			var sb = new StringBuilder(text);
			foreach (KeyValuePair<string, ElementBandConfiguration> pair in tables)
			{
				// Drop an existing block for the same key: "    Key_backwall:" followed by its "    - " / "      " lines.
				string pattern = @"^    " + Regex.Escape(pair.Key) + @":\n(?:    - .*\n|      .*\n)*";
				string current = sb.ToString();
				current = Regex.Replace(current, pattern, "", RegexOptions.Multiline);
				sb.Clear();
				sb.Append(current);
				sb.Append("    ").Append(pair.Key).Append(":\n");
				foreach (ElementGradient band in pair.Value)
				{
					sb.Append("    - content: ").Append(band.content).Append('\n');
					sb.Append("      bandSize: ").Append(band.bandSize.ToString("0.####", CultureInfo.InvariantCulture)).Append('\n');
				}
			}
			Save(target, RelativePath(file), sb.ToString());
		}

		private static int CopyNoiseTrees(string target)
		{
			int count = 0;
			foreach (Pattern pattern in Patterns.All)
			{
				if (pattern.Tree == null || !pattern.Tree.StartsWith("noise/NaturalBackwalls"))
					continue;
				string text = ReadWorldgen(pattern.Tree);
				if (text == null)
					continue;
				Save(target, RelativePath(pattern.Tree), text);
				count++;
			}
			return count;
		}

		private static void Save(string target, string relative, string text)
		{
			string path = Path.Combine(target, relative);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, text, new UTF8Encoding(false));
		}
	}
}
