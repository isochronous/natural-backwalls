using HarmonyLib;
using ProcGenGame;

namespace NaturalBackwalls
{
	/// <summary>
	/// The cluster generator builds every world's settings first (where CloneInToNewWorld_Patch does
	/// its work), then marks the starting world, then generates the worlds one by one. This prefix
	/// runs at the start of each world's generation, when the public isStartingWorld flag is set,
	/// and for a non-starting world clears the backwall noise the mod enabled, when the option is off.
	/// It reads only public members and undoes only the mod's own change.
	/// </summary>
	[HarmonyPatch(typeof(WorldGen), nameof(WorldGen.GenerateOffline))]
	public static class WorldGen_GenerateOffline_Patch
	{
		public static void Prefix(WorldGen __instance)
		{
			try
			{
				if (__instance.isStartingWorld || CloneInToNewWorld_Patch.OtherPlanetoids)
					return;
				CloneInToNewWorld_Patch.ClearBackwallNoise(__instance.Settings?.world);
			}
			catch (System.Exception e)
			{
				Debug.LogWarning("[NaturalBackwalls] Starting-world check skipped: " + e.Message);
			}
		}
	}
}
