using System.Collections.Generic;

namespace NaturalBackwalls
{
	/// <summary>
	/// A family of worldgen biome files that share one backwall material setting. Grouping is by
	/// biome file (the key the game uses for element bands), not by subworld zone, because the
	/// backwall band table is looked up per biome.
	/// </summary>
	public sealed class BiomeGroup
	{
		/// <summary>Stable id used as the options key.</summary>
		public string Id { get; }
		public string Title { get; }
		/// <summary>DLC that must be active for this group to matter, or null for the base game.</summary>
		public string Dlc { get; }
		/// <summary>Scoped worldgen biome file paths, e.g. "biomes/Forest" or "expansion1::biomes/Forest".</summary>
		public string[] BiomeFiles { get; }
		/// <summary>Individual biome keys that belong here although their file does not (Misc entries).</summary>
		public string[] ExtraBiomes { get; }
		/// <summary>Solid elements found in these biomes' band tables; the dropdown choices.</summary>
		public string[] Materials { get; }
		public string DefaultMaterial { get; }
		/// <summary>Default fraction of cells with a backwall when this is not the starting biome.</summary>
		public float DefaultCoverage { get; }
		/// <summary>True for biomes the game already gives backwalls (the Aquatic Planet Pack's); their vanilla data is kept while the settings stay at the defaults.</summary>
		public bool Vanilla { get; }

		public BiomeGroup(string id, string title, string dlc, string[] biomeFiles, string[] materials, string defaultMaterial,
			string[] extraBiomes = null, float defaultCoverage = 0.4f, bool vanilla = false)
		{
			DefaultCoverage = defaultCoverage;
			Vanilla = vanilla;
			Id = id;
			Title = title;
			Dlc = dlc;
			BiomeFiles = biomeFiles;
			Materials = materials;
			DefaultMaterial = defaultMaterial;
			ExtraBiomes = extraBiomes ?? new string[0];
		}
	}

	public static class BiomeGroups
	{
		private const string SO = DlcManager.EXPANSION1_ID;
		private const string Frosty = DlcManager.DLC2_ID;
		private const string Prehistoric = DlcManager.DLC4_ID;
		private const string Aquatic = DlcManager.DLC5_ID;

		/// <summary>
		/// Materials are the solids that appear in each group's biome band tables (gathered from the
		/// worldgen yaml of build 744825); unknown or non-solid names are dropped at runtime.
		/// </summary>
		public static readonly BiomeGroup[] All =
		{
			new BiomeGroup("Sandstone", "Sandstone", null,
				new[] { "biomes/Sedimentary", "expansion1::biomes/Sedimentary" },
				new[] { "SandStone", "Dirt", "Sand", "Cuprite", "Algae", "OxyRock", "Snow" }, "SandStone"),
			new BiomeGroup("Forest", "Forest", null,
				new[] { "biomes/Forest", "expansion1::biomes/Forest" },
				new[] { "Dirt", "IgneousRock", "AluminumOre", "OxyRock", "Snow" }, "Dirt"),
			new BiomeGroup("Frozen", "Tundra (frozen)", null,
				new[] { "biomes/Frozen", "expansion1::biomes/Frozen" },
				new[] { "Granite", "Ice", "Snow", "BrineIce", "DirtyIce", "SolidCarbonDioxide", "Wolframite", "Salt" }, "Granite",
				extraBiomes: new[] { "expansion1::biomes/Misc/DeadOasis" }),
			new BiomeGroup("Caustic", "Caustic (jungle)", null,
				new[] { "biomes/Jungle" },
				new[] { "IgneousRock", "Phosphorite", "IronOre", "Algae" }, "IgneousRock"),
			new BiomeGroup("Marsh", "Marsh", null,
				new[] { "biomes/HotMarsh" },
				new[] { "SedimentaryRock", "Clay", "SlimeMold", "GoldAmalgam", "Algae" }, "SedimentaryRock"),
			new BiomeGroup("Ocean", "Ocean (tide pool)", null,
				new[] { "biomes/Ocean" },
				new[] { "SedimentaryRock", "Sand", "Salt", "Granite", "BleachStone", "BrineIce" }, "SedimentaryRock"),
			new BiomeGroup("Oil", "Oily", null,
				new[] { "biomes/Oil" },
				new[] { "IgneousRock", "Lead", "SolidCrudeOil", "Fossil", "Granite", "Obsidian", "Sand" }, "IgneousRock",
				extraBiomes: new[] { "biomes/Misc/NaturalGasField" }),
			new BiomeGroup("Rust", "Rust", null,
				new[] { "biomes/Rust" },
				new[] { "MaficRock", "Rust", "IronOre", "Sulfur", "Snow" }, "MaficRock"),
			new BiomeGroup("Magma", "Magma and Niobium", null,
				new[] { "biomes/Magma", "expansion1::biomes/Magma", "dlc2::biomes/Magma", "expansion1::biomes/Niobium" },
				new[] { "Obsidian", "Granite", "IgneousRock", "Gold", "Niobium" }, "Obsidian"),
			new BiomeGroup("Barren", "Barren (regolith)", null,
				new[] { "biomes/Barren", "expansion1::biomes/Barren" },
				new[] { "Granite", "IgneousRock", "Obsidian", "SandStone", "Graphite", "IronOre", "Iron", "Fullerene" }, "Granite",
				extraBiomes: new[] { "expansion1::biomes/Misc/HardDust", "expansion1::biomes/Misc/SoftDust" }),
			new BiomeGroup("Swamp", "Swamp", SO,
				new[] { "expansion1::biomes/Swamp" },
				new[] { "SedimentaryRock", "Mud", "ToxicMud", "ToxicSand", "Cobaltite", "Dirt", "Phosphorite", "Fertilizer" }, "SedimentaryRock"),
			new BiomeGroup("Wasteland", "Wasteland", SO,
				new[] { "expansion1::biomes/Wasteland" },
				new[] { "IgneousRock", "Sulfur", "Cuprite", "SandStone", "Sand", "MaficRock", "Snow" }, "IgneousRock"),
			new BiomeGroup("Metallic", "Metallic", SO,
				new[] { "expansion1::biomes/Metallic" },
				new[] { "IgneousRock", "Dirt", "Cuprite", "GoldAmalgam", "AluminumOre", "Cobaltite", "OxyRock" }, "IgneousRock"),
			new BiomeGroup("Moo", "Moo", SO,
				new[] { "expansion1::biomes/Moo" },
				new[] { "IgneousRock", "Granite", "BleachStone", "SolidChlorine", "SolidCarbonDioxide" }, "IgneousRock"),
			new BiomeGroup("Radioactive", "Radioactive", SO,
				new[] { "expansion1::biomes/Radioactive" },
				new[] { "Granite", "Ice", "UraniumOre", "SolidChlorine", "SolidCarbonDioxide", "Snow", "Wolframite", "Dirt", "Rust", "Sulfur", "BleachStone" }, "Granite"),
			new BiomeGroup("Aquatic", "Aquatic (water asteroid)", SO,
				new[] { "expansion1::biomes/Aquatic" },
				new[] { "IgneousRock", "Graphite" }, "IgneousRock"),
			new BiomeGroup("CarrotQuarry", "Cool Pool (carrot quarry)", Frosty,
				new[] { "dlc2::biomes/CarrotQuarry" },
				new[] { "IgneousRock", "Ice", "IronOre", "Sucrose" }, "IgneousRock"),
			new BiomeGroup("IceCaves", "Ice Caves", Frosty,
				new[] { "dlc2::biomes/IceCaves", "dlc2::biomes/Misc" },
				new[] { "Granite", "Ice", "Snow", "SolidCarbonDioxide", "OxyRock", "Phosphorite", "Cinnabar", "Dirt", "CrushedIce" }, "Granite"),
			new BiomeGroup("SugarWoods", "Nectar (sugar woods)", Frosty,
				new[] { "dlc2::biomes/SugarWoods" },
				new[] { "Granite", "Ice", "Snow", "Phosphorite", "SolidMercury" }, "Granite"),
			new BiomeGroup("Garden", "Garden", Prehistoric,
				new[] { "dlc4::biomes/Garden" },
				new[] { "Shale", "NickelOre", "Dirt", "Peat", "Algae", "Fertilizer", "OxyRock" }, "Shale"),
			new BiomeGroup("Raptor", "Feather (raptor)", Prehistoric,
				new[] { "dlc4::biomes/Raptor" },
				new[] { "Granite", "IronOre", "BleachStone", "BrineIce", "IgneousRock", "Phosphorite" }, "Granite"),
			new BiomeGroup("Wetlands", "Wetlands", Prehistoric,
				new[] { "dlc4::biomes/Wetlands" },
				new[] { "IgneousRock", "Obsidian", "ToxicSand", "Sand", "GoldAmalgam" }, "IgneousRock"),
			// The Aquatic Planet Pack's own biomes: defaults are the pack's values (Beach is fully
			// walled in Salt, except its algae pockets in Dirt; Reef and Kelp Forest 0.2; Abyss 0.14).
			new BiomeGroup("Beach", "Beach (Aquatic pack)", Aquatic,
				new[] { "dlc5::biomes/Beach" },
				new[] { "Salt", "SiltStone", "Sand", "Dirt", "ZincOre", "Sulfur", "Algae", "OxyRock" }, "Salt",
				defaultCoverage: 1f, vanilla: true),
			new BiomeGroup("Reef", "Reef (Aquatic pack)", Aquatic,
				new[] { "dlc5::biomes/Reef" },
				new[] { "Coquina", "Sand", "Corallium", "Dirt", "ZincOre", "Algae", "Phosphorite" }, "Coquina",
				defaultCoverage: 0.2f, vanilla: true),
			new BiomeGroup("KelpForest", "Kelp Forest (Aquatic pack)", Aquatic,
				new[] { "dlc5::biomes/KelpForest" },
				new[] { "Granite", "Dirt", "ToxicMud", "IronOre" }, "Granite",
				defaultCoverage: 0.2f, vanilla: true),
			new BiomeGroup("Abyss", "Abyss (Aquatic pack)", Aquatic,
				new[] { "dlc5::biomes/Abyss" },
				new[] { "Basalt", "MurkyBrine", "Carbon", "Galena", "Diamond" }, "Basalt",
				defaultCoverage: 0.14f, vanilla: true),
		};

		private static Dictionary<string, BiomeGroup> byFile;
		private static Dictionary<string, BiomeGroup> byBiome;

		/// <summary>Finds the group for a biome key such as "expansion1::biomes/Forest/Core", or null.</summary>
		public static BiomeGroup ForBiome(string biomeKey)
		{
			if (byFile == null)
			{
				byFile = new Dictionary<string, BiomeGroup>();
				byBiome = new Dictionary<string, BiomeGroup>();
				foreach (BiomeGroup g in All)
				{
					foreach (string f in g.BiomeFiles) byFile[f] = g;
					foreach (string b in g.ExtraBiomes) byBiome[b] = g;
				}
			}
			if (byBiome.TryGetValue(biomeKey, out BiomeGroup group))
				return group;
			int slash = biomeKey.LastIndexOf('/');
			if (slash > 0 && byFile.TryGetValue(biomeKey.Substring(0, slash), out group))
				return group;
			return null;
		}

		public static bool IsAvailable(BiomeGroup group)
		{
			return group.Dlc == null || DlcManager.IsContentSubscribed(group.Dlc);
		}
	}
}
