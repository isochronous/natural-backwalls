using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace NaturalBackwalls
{
	public sealed class NaturalBackwallsMod : UserMod2
	{
		public override void OnLoad(Harmony harmony)
		{
			base.OnLoad(harmony);
			PUtil.InitLibrary(false);
			new POptions().RegisterOptions(this, typeof(Options));
			Debug.Log("[NaturalBackwalls] Loaded version " + typeof(NaturalBackwallsMod).Assembly.GetName().Version);
		}
	}
}
