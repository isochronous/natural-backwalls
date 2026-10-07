using System.Collections.Generic;

namespace NaturalBackwalls
{
	/// <summary>A noise tree the backwall band is sampled with: the shape and density of the patches.</summary>
	public sealed class Pattern
	{
		/// <summary>Keeps the subworld's own tree; only meaningful for biomes the game already backwalls.</summary>
		public const string VanillaId = "Vanilla";

		public string Id { get; }
		public string Title { get; }
		/// <summary>Scoped worldgen tree name, or null for the subworld's own tree.</summary>
		public string Tree { get; }
		public string Description { get; }

		public Pattern(string id, string title, string tree, string description)
		{
			Id = id; Title = title; Tree = tree; Description = description;
		}
	}

	/// <summary>
	/// A subworld zone type, the unit a noise pattern applies to: a backwall noise tree belongs to a
	/// subworld, not a biome, and every subworld declares one of these. The game's own zones (the
	/// Aquatic pack's) default to keeping their trees.
	/// </summary>
	public sealed class PatternGroup
	{
		public ProcGen.SubWorld.ZoneType Zone { get; }
		public string Id => Zone.ToString();
		public string Title { get; }
		public string Dlc { get; }
		public string DefaultPattern { get; }
		public bool Vanilla { get; }
		/// <summary>The biome families this zone's subworlds are built from, for the tooltip.</summary>
		public string Biomes { get; }

		public PatternGroup(ProcGen.SubWorld.ZoneType zone, string title, string dlc, string defaultPattern, string biomes, bool vanilla = false)
		{
			Zone = zone; Title = title; Dlc = dlc; DefaultPattern = defaultPattern; Biomes = biomes; Vanilla = vanilla;
		}

		public bool IsAvailable => Dlc == null || DlcManager.IsContentSubscribed(Dlc);
	}

	public static class Patterns
	{
		/// <summary>
		/// The Aquatic Planet Pack's trees, shipped with the mod under their own names so no DLC is
		/// needed, plus one base-game tree. Densities are what a 0.4 band covers in the preview tool.
		/// </summary>
		public static readonly Pattern[] All =
		{
			new Pattern("Reef", "Reef", "noise/NaturalBackwallsReef", "The pack's reef and beach pattern. Patches at about 45% of cells for a 0.4 band."),
			new Pattern("Kelp", "Kelp forest", "noise/NaturalBackwallsKelp", "The pack's kelp forest pattern: broad and dense. About 78% of cells for a 0.4 band, 52% for 0.2."),
			new Pattern("Abyss", "Abyss", "noise/NaturalBackwallsAbyss", "The pack's abyss (murky brine) pattern. About 45% of cells for a 0.4 band, in a different shape from the reef."),
			new Pattern("Beach", "Beach", "noise/NaturalBackwallsBeach", "The pack's beach terrain pattern. About 45% of cells for a 0.4 band."),
			new Pattern("Strange", "Strange (base game)", "noise/SandstoneStrange", "The base game's SandstoneStrange tree, which the pack uses for two subworlds. Dense: about 80% of cells for a 0.4 band."),
		};

		public static readonly Pattern Vanilla = new Pattern(Pattern.VanillaId, "As the game made it", null, "Keeps the subworld's own backwall noise tree.");

		/// <summary>Defaults: the dense kelp forest tree for lush and marshy zones, the abyss tree for hot and rocky ones, the reef elsewhere.</summary>
		public static readonly PatternGroup[] Groups =
		{
			new PatternGroup(ProcGen.SubWorld.ZoneType.Sandstone, "Sandstone", null, "Reef", "Sandstone, Barren (regolith)"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.Forest, "Forest", null, "Reef", "Forest"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.FrozenWastes, "Tundra (frozen)", null, "Reef", "Tundra (frozen), Moo"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.ToxicJungle, "Caustic (jungle)", null, "Kelp", "Caustic (jungle)"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.BoggyMarsh, "Marsh", null, "Kelp", "Marsh"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.Ocean, "Ocean (tide pool)", null, "Reef", "Ocean (tide pool)"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.OilField, "Oily", null, "Abyss", "Oily, Magma and Niobium"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.Rust, "Rust", null, "Abyss", "Rust, Metallic"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.MagmaCore, "Magma", null, "Abyss", "Magma and Niobium"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.Barren, "Barren (regolith)", null, "Abyss", "Barren (regolith), Aquatic (water asteroid)"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.Swamp, "Swamp", DlcManager.EXPANSION1_ID, "Kelp", "Swamp, Aquatic (water asteroid)"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.Wasteland, "Wasteland", DlcManager.EXPANSION1_ID, "Abyss", "Wasteland"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.Metallic, "Metallic", DlcManager.EXPANSION1_ID, "Abyss", "Metallic"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.Moo, "Moo", DlcManager.EXPANSION1_ID, "Reef", "Moo"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.Radioactive, "Radioactive", DlcManager.EXPANSION1_ID, "Abyss", "Radioactive"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.CarrotQuarry, "Cool Pool (carrot quarry)", DlcManager.DLC2_ID, "Reef", "Cool Pool (carrot quarry)"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.IceCaves, "Ice Caves", DlcManager.DLC2_ID, "Reef", "Ice Caves"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.SugarWoods, "Nectar (sugar woods)", DlcManager.DLC2_ID, "Kelp", "Nectar (sugar woods)"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.PrehistoricGarden, "Garden", DlcManager.DLC4_ID, "Kelp", "Garden"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.PrehistoricRaptor, "Feather (raptor)", DlcManager.DLC4_ID, "Reef", "Feather (raptor)"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.PrehistoricWetlands, "Wetlands", DlcManager.DLC4_ID, "Kelp", "Wetlands"),
			new PatternGroup(ProcGen.SubWorld.ZoneType.Beach, "Beach (Aquatic pack)", DlcManager.DLC5_ID, Pattern.VanillaId, "Beach (Aquatic pack)", vanilla: true),
			new PatternGroup(ProcGen.SubWorld.ZoneType.Reef, "Reef (Aquatic pack)", DlcManager.DLC5_ID, Pattern.VanillaId, "Reef (Aquatic pack)", vanilla: true),
			new PatternGroup(ProcGen.SubWorld.ZoneType.KelpForest, "Kelp Forest (Aquatic pack)", DlcManager.DLC5_ID, Pattern.VanillaId, "Kelp Forest (Aquatic pack)", vanilla: true),
			new PatternGroup(ProcGen.SubWorld.ZoneType.Abyss, "Abyss (Aquatic pack)", DlcManager.DLC5_ID, Pattern.VanillaId, "Abyss (Aquatic pack)", vanilla: true),
		};

		private static Dictionary<ProcGen.SubWorld.ZoneType, PatternGroup> byZone;

		public static PatternGroup GroupFor(ProcGen.SubWorld.ZoneType zone)
		{
			if (byZone == null)
			{
				byZone = new Dictionary<ProcGen.SubWorld.ZoneType, PatternGroup>();
				foreach (PatternGroup g in Groups) byZone[g.Zone] = g;
			}
			return byZone.TryGetValue(zone, out PatternGroup group) ? group : null;
		}

		private static Dictionary<string, Pattern> byId;

		public static Pattern Find(string id)
		{
			if (byId == null)
			{
				byId = new Dictionary<string, Pattern>();
				foreach (Pattern p in All) byId[p.Id] = p;
				byId[Vanilla.Id] = Vanilla;
			}
			return id != null && byId.TryGetValue(id, out Pattern pattern) ? pattern : null;
		}
	}
}
