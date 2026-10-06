using System.Collections.Generic;
using HarmonyLib;
using ProcGenGame;

namespace NaturalBackwalls
{
	/// <summary>
	/// Worldgen features (a subworld's carved rooms, lakes and geodes) claim their cells before the
	/// terrain pass, and TerrainCell.ApplyBackground writes backwalls only over the unclaimed cells.
	/// So in the game every feature room is bare, and in rock biomes most open space is feature
	/// rooms: the slime biome's caves are TallRoom and BlobRoom features. This postfix runs the same
	/// backwall placement over the feature cells, for biomes the mod configures (the Aquatic pack's
	/// own stay as the pack made them).
	/// </summary>
	[HarmonyPatch(typeof(TerrainCell), "ApplyBackground")]
	public static class TerrainCell_ApplyBackground_Patch
	{
		private static readonly System.Reflection.FieldInfo FeaturePointsField = AccessTools.Field(typeof(TerrainCell), "featureSpawnPoints");

		public static void Postfix(TerrainCell __instance, WorldGen worldGen, Chunk world, TerrainCell.ISimDataSetter simDataSetter, float temperatureMin, float temperatureRange)
		{
			if (!__instance.spawnBackwall || !CloneInToNewWorld_Patch.FeatureBackwalls)
				return;
			string biome = __instance.node?.GetBiome();
			if (biome == null || !CloneInToNewWorld_Patch.ModdedBiomes.Contains(biome))
				return;
			ElementBandConfiguration bands = worldGen.Settings.GetElementBandForBiome(biome + "_backwall");
			HashSet<int> points = FeaturePointsField?.GetValue(__instance) as HashSet<int>;
			if (bands == null || points == null)
				return;
			foreach (int cell in points)
			{
				Vector2I pos = Grid.CellToXY(cell);
				worldGen.GetElementForBackwallBiomePoint(world, bands, pos, out Element element, out Sim.PhysicsData pd, out Sim.DiseaseCell _);
				if (!element.IsSolid)
					continue;
				pd.mass += pd.mass * 0.2f * (world.density[pos.x + world.size.x * pos.y] - 0.5f);
				float temperature = temperatureMin;
				if (element.lowTempTransition != null && temperatureMin < element.lowTemp)
					temperature = element.lowTemp;
				pd.temperature = temperature + world.heatOffset[cell] * temperatureRange;
				simDataSetter.SetSimBackwall(cell, element, pd);
			}
		}
	}
}
