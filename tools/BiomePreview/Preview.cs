using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Klei;
using LibNoiseDotNet.Graphics.Tools.Noise;
using LibNoiseDotNet.Graphics.Tools.Noise.Builder;
using ProcGen;
using Path = System.IO.Path;

namespace BiomePreview
{
	/// <summary>
	/// Everything about a subworld that changes the preview: its noise trees and cave tags. Many
	/// subworlds share one of these, so the catalog groups them per biome.
	/// </summary>
	public sealed class NoiseVariant
	{
		public string BiomeNoise, OverrideNoise, BackwallNoise;
		public bool IgnoreCaves, VoidSliver;
		public List<string> Subworlds = new List<string>();

		public bool Caves => OverrideNoise != null && !IgnoreCaves;

		public string Key => BiomeNoise + "|" + (Caves ? OverrideNoise + (VoidSliver ? "+sliver" : "") : "-") + "|" + (BackwallNoise ?? "-");

		public string Label
		{
			get
			{
				string s = Short(BiomeNoise);
				s += Caves ? ", caves " + Short(OverrideNoise) : ", no caves";
				if (BackwallNoise != null) s += ", backwall " + Short(BackwallNoise);
				return s + "  (" + Subworlds.Count + (Subworlds.Count == 1 ? " subworld)" : " subworlds)");
			}
		}

		private static string Short(string tree) => tree.Substring(tree.LastIndexOf('/') + 1);
	}

	/// <summary>
	/// A biome key with every other key that generates the same preview: same solid-or-open band
	/// thresholds (the elements may differ), same vanilla backwall table, same noise setups.
	/// </summary>
	public sealed class BiomeEntry
	{
		public string Key;
		public List<string> Aliases = new List<string>();
		public List<NoiseVariant> Variants;

		public string Label => Aliases.Count == 0 ? Key : Key + "   (= " + string.Join(", ", Aliases.Select(a => a.Substring(a.LastIndexOf('/') + 1))) + ")";
	}

	/// <summary>
	/// Mirrors the parts of WorldGen.WriteOverWorldNoise and TerrainCell.ApplyBackground that decide
	/// solid-vs-open and backwall-vs-none for one biome: the noise trees sampled over the window
	/// (normalised over it, as the game normalises over every cell that uses the tree), the biome's
	/// element bands as cumulative thresholds, the cave override thresholds, and the "_backwall"
	/// band list (vanilla, or a solid band of the given coverage plus Vacuum; which solid is
	/// irrelevant to the shape).
	/// </summary>
	public sealed class Preview
	{
		/// <summary>Any solid will do for the backwall band; the preview only cares that it is solid.</summary>
		public const string AnySolid = "Granite";

		private readonly string streamingAssets;
		private readonly HashSet<string> solids;
		private readonly float caveMax, caveSliver;
		private Dictionary<string, List<NoiseVariant>> catalog;
		private List<BiomeEntry> entries;

		public Preview(string streamingAssets)
		{
			this.streamingAssets = streamingAssets;
			solids = LoadSolids();
			string defaults = File.ReadAllText(Path.Combine(streamingAssets, "worldgen", "defaults.yaml"));
			caveMax = ReadFloat(defaults, "CaveOverrideMaxValue", 0.65f);
			caveSliver = ReadFloat(defaults, "CaveOverrideSliverValue", 0.97f);
		}

		/// <summary>Every biome key used by some subworld, with the distinct noise setups it is generated under.</summary>
		public Dictionary<string, List<NoiseVariant>> Catalog()
		{
			if (catalog != null)
				return catalog;
			catalog = new Dictionary<string, List<NoiseVariant>>();
			foreach (string subworld in ListSubworlds())
			{
				string text = File.ReadAllText(ResolveYaml(subworld));
				NoiseVariant v = ReadVariant(text);
				foreach (string biome in ListBiomes(text))
				{
					if (!catalog.TryGetValue(biome, out List<NoiseVariant> variants))
						catalog[biome] = variants = new List<NoiseVariant>();
					NoiseVariant same = variants.FirstOrDefault(x => x.Key == v.Key);
					if (same == null)
					{
						same = new NoiseVariant { BiomeNoise = v.BiomeNoise, OverrideNoise = v.OverrideNoise, BackwallNoise = v.BackwallNoise, IgnoreCaves = v.IgnoreCaves, VoidSliver = v.VoidSliver };
						variants.Add(same);
					}
					same.Subworlds.Add(subworld);
				}
			}
			foreach (List<NoiseVariant> variants in catalog.Values)
				variants.Sort((a, b) => b.Subworlds.Count.CompareTo(a.Subworlds.Count));
			return catalog;
		}

		/// <summary>The catalog with identical-looking biome keys folded together, sorted by key.</summary>
		public List<BiomeEntry> Entries()
		{
			if (entries != null)
				return entries;
			var byShape = new Dictionary<string, BiomeEntry>();
			foreach (KeyValuePair<string, List<NoiseVariant>> pair in Catalog().OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
			{
				string signature = ShapeSignature(pair.Key) + "||" + string.Join(";", pair.Value.Select(v => v.Key).OrderBy(k => k));
				if (byShape.TryGetValue(signature, out BiomeEntry same))
					same.Aliases.Add(pair.Key);
				else
					byShape[signature] = new BiomeEntry { Key = pair.Key, Variants = pair.Value };
			}
			entries = byShape.Values.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase).ToList();
			return entries;
		}

		/// <summary>
		/// What the preview actually depends on in a biome's tables: where along the noise range the
		/// terrain switches between solid and open, and the vanilla backwall table's solid runs.
		/// </summary>
		private string ShapeSignature(string biomeKey)
		{
			Dictionary<string, ElementBandConfiguration> tables = LoadBiomeTables(biomeKey, out string tableKey);
			string Runs(ElementBandConfiguration table)
			{
				if (table == null) return "-";
				var copy = new ElementBandConfiguration(table.Select(g => new ElementGradient(g.content, g.bandSize, null)));
				copy.ConvertBandSizeToMaxSize();
				var parts = new List<string>();
				bool? lastSolid = null;
				foreach (ElementGradient g in copy)
				{
					bool solid = solids.Contains(g.content);
					if (lastSolid == solid) { parts[parts.Count - 1] = (solid ? "S" : "O") + g.maxValue.ToString("F4", CultureInfo.InvariantCulture); continue; }
					parts.Add((solid ? "S" : "O") + g.maxValue.ToString("F4", CultureInfo.InvariantCulture));
					lastSolid = solid;
				}
				return string.Join(",", parts);
			}
			tables.TryGetValue(tableKey, out ElementBandConfiguration terrain);
			tables.TryGetValue(tableKey + "_backwall", out ElementBandConfiguration backwall);
			return Runs(terrain) + "|" + Runs(backwall);
		}

		/// <summary>Every subworld with a biome noise tree, as scoped names ("dlc5::subworlds/reef/ReefBasic").</summary>
		public List<string> ListSubworlds()
		{
			var list = new List<string>();
			void Scan(string root, string scope)
			{
				string dir = Path.Combine(root, "worldgen", "subworlds");
				if (!Directory.Exists(dir)) return;
				foreach (string f in Directory.GetFiles(dir, "*.yaml", SearchOption.AllDirectories))
				{
					if (!File.ReadAllText(f).Contains("biomeNoise:")) continue;
					string rel = Path.GetRelativePath(Path.Combine(root, "worldgen"), f).Replace(Path.DirectorySeparatorChar, '/');
					list.Add(scope + rel.Substring(0, rel.Length - 5));
				}
			}
			Scan(streamingAssets, "");
			string dlc = Path.Combine(streamingAssets, "dlc");
			if (Directory.Exists(dlc))
				foreach (string d in Directory.GetDirectories(dlc))
					Scan(d, Path.GetFileName(d) + "::");
			list.Sort(StringComparer.OrdinalIgnoreCase);
			return list;
		}

		/// <summary>Biome keys of a subworld file, heaviest first.</summary>
		public static List<string> ListBiomes(string yaml)
		{
			var found = new List<(string name, float weight)>();
			foreach (Match m in Regex.Matches(yaml, @"-\s*name:\s*(\S+)\s*\n\s*weight:\s*([0-9.]+)"))
				found.Add((m.Groups[1].Value, float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)));
			if (found.Count == 0)
				foreach (Match m in Regex.Matches(yaml, @"-\s*name:\s*((?:\w+::)?biomes/\S+)"))
					found.Add((m.Groups[1].Value, 1f));
			return found.OrderByDescending(f => f.weight).Select(f => f.name).Distinct().ToList();
		}

		/// <summary>Whether the biome's file carries a vanilla "_backwall" table (the Aquatic pack's do).</summary>
		public bool HasVanillaBackwall(string biomeKey)
		{
			return LoadBiomeTables(biomeKey, out string tableKey).ContainsKey(tableKey + "_backwall");
		}

		/// <summary>Command-line entry: a subworld file supplies the noise setup; the biome defaults to its heaviest.</summary>
		public Result Generate(string subworldPath, string biomeKey, bool vanillaBackwall, float coverage, int width, int height, int seed, float ox, float oy)
		{
			string text = File.ReadAllText(ResolveYaml(subworldPath));
			NoiseVariant v = ReadVariant(text);
			v.Subworlds.Add(subworldPath);
			if (biomeKey == null)
				biomeKey = ListBiomes(text).FirstOrDefault() ?? throw new InvalidOperationException("No biomes in subworld " + subworldPath);
			return Generate(v, biomeKey, vanillaBackwall, coverage, width, height, seed, ox, oy);
		}

		/// <summary>
		/// Area the backwall noise is normalised over, in cells. The game normalises a tree over every
		/// cell that uses it; the mod's backwall tree covers nearly the whole world, so a world-sized
		/// area (256x384 is the classic asteroid) is the right scope. The biome noise is normalised over
		/// the window instead, since in the game it covers only that biome's own subworlds.
		/// </summary>
		public int NormaliseWidth = 256, NormaliseHeight = 384;

		/// <param name="vanillaBackwall">Use the biome's own "_backwall" table when it has one; otherwise a solid band of the given coverage (0 = no backwall).</param>
		public Result Generate(NoiseVariant v, string biomeKey, bool vanillaBackwall, float coverage, int width, int height, int seed, float ox, float oy)
		{
			if (v.BiomeNoise == null)
				throw new InvalidOperationException("No biome noise tree");
			var r = new Result
			{
				Subworld = v.Subworlds.Count == 1 ? v.Subworlds[0] : v.Subworlds.Count + " subworlds",
				Biome = biomeKey, Coverage = coverage,
				BiomeNoise = v.BiomeNoise, OverrideNoise = v.Caves ? v.OverrideNoise : null, BackwallNoise = v.BackwallNoise,
				Width = width, Height = height, Seed = seed,
				Foreground = new byte[width * height], Backwall = new byte[width * height], ForegroundElement = new string[width * height],
			};
			if (v.OverrideNoise != null && v.IgnoreCaves) r.Notes.Add("Subworld ignores the cave override.");

			Dictionary<string, ElementBandConfiguration> tables = LoadBiomeTables(biomeKey, out string tableKey);
			if (!tables.TryGetValue(tableKey, out ElementBandConfiguration terrain))
				throw new InvalidOperationException("Biome table '" + tableKey + "' not found for " + biomeKey + " (keys: " + string.Join(", ", tables.Keys) + ")");
			terrain.ConvertBandSizeToMaxSize();

			ElementBandConfiguration backwall = null;
			if (vanillaBackwall && tables.TryGetValue(tableKey + "_backwall", out ElementBandConfiguration vanilla))
			{
				backwall = vanilla;
				r.Notes.Add("Backwall band: vanilla " + string.Join(", ", vanilla.Select(g => g.content + " " + g.bandSize.ToString("F2", CultureInfo.InvariantCulture))) + ".");
			}
			else if (coverage > 0f)
			{
				coverage = Math.Min(1f, coverage);
				backwall = new ElementBandConfiguration { new ElementGradient(AnySolid, coverage, null) };
				if (coverage < 1f) backwall.Add(new ElementGradient("Vacuum", 1f - coverage, null));
				r.Notes.Add("Backwall band: solid " + coverage.ToString("F2", CultureInfo.InvariantCulture) + ", Vacuum " + (1f - coverage).ToString("F2", CultureInfo.InvariantCulture) + ".");
			}
			else
				r.Notes.Add("No backwall band.");
			backwall?.ConvertBandSizeToMaxSize();
			string backwallNoise = v.BackwallNoise;
			if (backwall != null && backwallNoise == null)
			{
				backwallNoise = "noise/NaturalBackwalls";
				r.BackwallNoise = backwallNoise + " (mod default)";
			}

			float[] baseMap = Noise(v.BiomeNoise, width, height, seed, ox, oy, 0, 0);
			float[] overMap = v.Caves ? Noise(v.OverrideNoise, width, height, seed, ox, oy, 0, 0) : null;
			float[] bwMap = backwall != null ? Noise(backwallNoise, width, height, seed, ox, oy, NormaliseWidth, NormaliseHeight) : null;
			if (bwMap != null && (NormaliseWidth > width || NormaliseHeight > height))
				r.Notes.Add("Backwall noise normalised over " + Math.Max(NormaliseWidth, width) + "x" + Math.Max(NormaliseHeight, height) + " cells, window cut from its centre.");

			for (int i = 0; i < width * height; i++)
			{
				string element = Pick(baseMap[i], terrain);
				bool solid = solids.Contains(element);
				if (solid && overMap != null && overMap[i] > caveMax)
				{
					solid = false;
					element = v.VoidSliver && overMap[i] > caveSliver ? "Void" : "Vacuum";
				}
				r.Foreground[i] = (byte)(solid ? 1 : 0);
				r.ForegroundElement[i] = element;
				if (bwMap != null)
					r.Backwall[i] = (byte)(solids.Contains(Pick(bwMap[i], backwall)) ? 1 : 0);
			}
			r.Cells = width * height;
			for (int i = 0; i < r.Cells; i++)
			{
				if (r.Foreground[i] == 0) r.OpenCells++;
				if (r.Backwall[i] == 1) { r.BackwallCells++; if (r.Foreground[i] == 0) r.VisibleBackwalls++; }
			}
			return r;
		}

		private static NoiseVariant ReadVariant(string subworldYaml)
		{
			return new NoiseVariant
			{
				BiomeNoise = ReadString(subworldYaml, "biomeNoise"),
				OverrideNoise = ReadString(subworldYaml, "overrideNoise"),
				BackwallNoise = ReadString(subworldYaml, "backwallNoise"),
				IgnoreCaves = Regex.IsMatch(subworldYaml, @"^\s*-\s*IgnoreCaveOverride\s*$", RegexOptions.Multiline),
				VoidSliver = Regex.IsMatch(subworldYaml, @"^\s*-\s*CaveVoidSliver\s*$", RegexOptions.Multiline),
			};
		}

		/// <summary>WorldGen.GetElementFromBiomeElementTable: first band whose threshold the value is under, else the last.</summary>
		private static string Pick(float value, ElementBandConfiguration table)
		{
			for (int i = 0; i < table.Count; i++)
				if (value < table[i].maxValue)
					return table[i].content;
			return table[table.Count - 1].content;
		}

		/// <summary>
		/// WorldGen.BuildNoiseSource + BuildNoiseMap + Normalise for one tree. With a normalisation area
		/// larger than the window, the noise is built and normalised over that area and the window is
		/// cut from its centre; otherwise it is normalised over the window itself.
		/// </summary>
		private float[] Noise(string treeName, int width, int height, int seed, float ox, float oy, int normWidth, int normHeight)
		{
			int fullW = Math.Max(width, normWidth), fullH = Math.Max(height, normHeight);
			float[] full = NoiseArea(treeName, fullW, fullH, seed, ox, oy);
			if (fullW == width && fullH == height)
				return full;
			int x0 = (fullW - width) / 2, y0 = (fullH - height) / 2;
			float[] window = new float[width * height];
			for (int y = 0; y < height; y++)
				Array.Copy(full, x0 + (y0 + y) * fullW, window, y * width, width);
			return window;
		}

		private float[] NoiseArea(string treeName, int width, int height, int seed, float ox, float oy)
		{
			string path = ResolveYaml(treeName);
			if (!File.Exists(path) && treeName.StartsWith("noise/NaturalBackwalls"))
				path = Path.Combine(Path.GetDirectoryName(typeof(Preview).Assembly.Location) ?? ".", "..", "..", "..", "..", "src", "NaturalBackwalls", "worldgen", "noise", "NaturalBackwalls.yaml");
			if (!File.Exists(path))
				throw new FileNotFoundException("Noise tree not found", path);
			var tree = YamlIO.Parse<ProcGen.Noise.Tree>(File.ReadAllText(path), default(FileHandle), OnYamlError);
			if (tree == null)
				throw new InvalidOperationException("Could not parse noise tree " + path);
			var builder = new NoiseMapBuilderPlane(tree.settings.lowerBound.x, tree.settings.upperBound.x, tree.settings.lowerBound.y, tree.settings.upperBound.y, false);
			builder.SetSize(width, height);
			builder.SourceModule = tree.BuildFinalModule(seed);
			float zoom = tree.settings.zoom == 0f ? 0.01f : tree.settings.zoom;
			var map = new NoiseMap(width, height);
			builder.NoiseMap = map;
			builder.SetBounds(ox * zoom, (ox + width) * zoom, oy * zoom, (oy + height) * zoom);
			builder.Build();
			float[] data = new float[width * height];
			map.CopyTo(ref data);
			if (tree.settings.normalise)
			{
				float min = float.MaxValue, max = float.MinValue;
				foreach (float x in data) { if (x < min) min = x; if (x > max) max = x; }
				float range = max - min;
				if (range > 0f)
					for (int i = 0; i < data.Length; i++) data[i] = (data[i] - min) / range;
			}
			return data;
		}

		private Dictionary<string, ElementBandConfiguration> LoadBiomeTables(string biomeKey, out string tableKey)
		{
			int slash = biomeKey.LastIndexOf('/');
			string file = biomeKey.Substring(0, slash);
			tableKey = biomeKey.Substring(slash + 1);
			var settings = YamlIO.Parse<BiomeSettings>(File.ReadAllText(ResolveYaml(file)), default(FileHandle), OnYamlError);
			if (settings?.TerrainBiomeLookupTable?.add == null)
				throw new InvalidOperationException("Could not parse biome file " + file);
			return settings.TerrainBiomeLookupTable.add;
		}

		private static void OnYamlError(YamlIO.Error error, bool forceLogAsWarning)
		{
			Console.Error.WriteLine("yaml: " + error.message);
		}

		/// <summary>SettingsCache.RewriteWorldgenPathYaml: "dlc5::noise/x" -> StreamingAssets/dlc/dlc5/worldgen/noise/x.yaml.</summary>
		private string ResolveYaml(string scoped)
		{
			string scope = "", rest = scoped;
			int i = scoped.IndexOf("::", StringComparison.Ordinal);
			if (i >= 0) { scope = scoped.Substring(0, i); rest = scoped.Substring(i + 2); }
			string root = scope == "" ? streamingAssets : Path.Combine(streamingAssets, "dlc", scope);
			string path = Path.Combine(root, "worldgen", rest.Replace('/', Path.DirectorySeparatorChar));
			return path.EndsWith(".yaml") ? path : path + ".yaml";
		}

		private HashSet<string> LoadSolids()
		{
			var set = new HashSet<string>();
			foreach (string f in Directory.GetFiles(streamingAssets, "solid.yaml", SearchOption.AllDirectories))
				foreach (Match m in Regex.Matches(File.ReadAllText(f), @"elementId:\s*(\w+)"))
					set.Add(m.Groups[1].Value);
			return set;
		}

		private static string ReadString(string yaml, string key)
		{
			Match m = Regex.Match(yaml, @"^" + key + @":\s*(\S+)\s*$", RegexOptions.Multiline);
			return m.Success ? m.Groups[1].Value.Trim('"', '\'') : null;
		}

		private static float ReadFloat(string yaml, string key, float fallback)
		{
			Match m = Regex.Match(yaml, key + @":\s*([0-9.]+)");
			return m.Success ? float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : fallback;
		}
	}
}
