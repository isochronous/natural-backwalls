using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using ProcGen;

namespace NaturalBackwalls
{
	/// <summary>
	/// Logs, once per game load, how many cells of each zone type have a backwall and how many of
	/// those are visible (not behind a solid cell). The numbers are directly comparable with the
	/// BiomePreview tool's, so settings can be checked against the real generator.
	/// </summary>
	[HarmonyPatch(typeof(Game), "OnSpawn")]
	public static class Game_OnSpawn_Census
	{
		private sealed class Tally { public int Cells, Open, Backwalls, Visible; }

		public static void Postfix()
		{
			try
			{
				SubWorld.ZoneType[] zones = World.Instance?.zoneRenderData?.worldZoneTypes;
				if (zones == null || Grid.Element == null)
					return;
				var tallies = new SortedDictionary<string, Tally>();
				int count = System.Math.Min(zones.Length, Grid.CellCount);
				for (int cell = 0; cell < count; cell++)
				{
					string zone = zones[cell].ToString();
					if (!tallies.TryGetValue(zone, out Tally t))
						tallies[zone] = t = new Tally();
					Element element = Grid.Element[cell];
					bool solid = element != null && element.IsSolid;
					bool backwall = BackwallManager.HasBackwall(cell);
					t.Cells++;
					if (!solid) t.Open++;
					if (backwall) { t.Backwalls++; if (!solid) t.Visible++; }
				}
				var sb = new StringBuilder("[NaturalBackwalls] Backwall census (cells / open / backwalls / visible, % of open):");
				foreach (KeyValuePair<string, Tally> pair in tallies)
				{
					Tally t = pair.Value;
					if (t.Backwalls == 0 && t.Cells < 100) continue;
					sb.Append("\n  ").Append(pair.Key.PadRight(20)).Append(t.Cells.ToString().PadLeft(7)).Append(t.Open.ToString().PadLeft(8))
					  .Append(t.Backwalls.ToString().PadLeft(8)).Append(t.Visible.ToString().PadLeft(8))
					  .Append(t.Open > 0 ? (100.0 * t.Visible / t.Open).ToString("F1").PadLeft(8) + "%" : "      n/a");
				}
				Debug.Log(sb.ToString());
			}
			catch (System.Exception e)
			{
				Debug.LogWarning("[NaturalBackwalls] census skipped: " + e.Message);
			}
		}
	}
}
