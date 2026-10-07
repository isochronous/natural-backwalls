using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace BiomePreview
{
	/// <summary>
	/// Rough single-biome worldgen preview without the game: the real noise trees and band tables
	/// give the foreground (solid or open per cell, with the cave carving) and the backwall mask.
	/// No overworld layout, POIs, borders, rivers or sim settle; one biome fills the whole window.
	/// </summary>
	public static class Program
	{
		[STAThread]
		public static int Main(string[] args)
		{
			// Resolve the game's assemblies from the folder the build recorded, before any type from them is touched.
			string libs = File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gamelibs.txt"))
				? File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gamelibs.txt")).Trim()
				: Environment.GetEnvironmentVariable("ONI_GAME_FOLDER") + @"\OxygenNotIncluded_Data\Managed";
			AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
			{
				string name = new AssemblyName(e.Name).Name;
				string path = Path.Combine(libs, name + ".dll");
				return File.Exists(path) ? Assembly.LoadFrom(path) : null;
			};
			try
			{
				string streamingAssets = Path.GetFullPath(Path.Combine(libs, @"..\StreamingAssets"));
				if (args.Length == 0)
					return Gui(streamingAssets);
				if (args[0] == "--smoke")
				{
					// Headless check of the form: build it, generate once, print the numbers.
					using var form = new MainForm(streamingAssets);
					form.Generate();
					Console.WriteLine(form.StatsText);
					return 0;
				}
				return Run(args, streamingAssets);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
				return 1;
			}
		}

		[STAThread]
		private static int Gui(string streamingAssets)
		{
			System.Windows.Forms.Application.EnableVisualStyles();
			System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.PerMonitorV2);
			System.Windows.Forms.Application.Run(new MainForm(streamingAssets));
			return 0;
		}

		private static int Run(string[] args, string streamingAssets)
		{
			var a = ParseArgs(args);
			if (!a.ContainsKey("subworld"))
			{
				Console.Error.WriteLine("(run without arguments for the GUI)");
				Console.Error.WriteLine("usage: BiomePreview --subworld <scoped path, e.g. subworlds/marsh/HotMarsh or dlc5::subworlds/kelpforest/KelpForestBasic>\n"
					+ "  [--biome <biome key, default: the subworld's heaviest>] [--coverage <0..1>, default: the biome's vanilla backwall band if any, else 0.4]\n"
					+ "  [--size WxH] [--seed N] [--offset X,Y] [--norm WxH, world size the backwall noise is normalised over, default 256x384, 0x0 = window] [--noise <tree, e.g. noise/NaturalBackwallsKelp>] [--out file.html]");
				return 2;
			}
			var preview = new Preview(streamingAssets);
			string subworld = a["subworld"];
			string biome = a.TryGetValue("biome", out string b) ? b : null;
			bool vanilla = !a.ContainsKey("coverage");
			float coverage = a.TryGetValue("coverage", out string c) ? float.Parse(c, CultureInfo.InvariantCulture) : 0.4f;
			int width = 160, height = 120;
			if (a.TryGetValue("size", out string size))
			{
				string[] wh = size.ToLowerInvariant().Split('x');
				width = int.Parse(wh[0]); height = int.Parse(wh[1]);
			}
			int seed = a.TryGetValue("seed", out string sd) ? int.Parse(sd) : 0;
			float ox = 0, oy = 0;
			if (a.TryGetValue("offset", out string off))
			{
				string[] xy = off.Split(',');
				ox = float.Parse(xy[0], CultureInfo.InvariantCulture); oy = float.Parse(xy[1], CultureInfo.InvariantCulture);
			}
			if (a.TryGetValue("norm", out string norm))
			{
				string[] nwh = norm.ToLowerInvariant().Split('x');
				preview.NormaliseWidth = int.Parse(nwh[0]); preview.NormaliseHeight = int.Parse(nwh[1]);
			}
			if (a.TryGetValue("noise", out string noise))
				preview.BackwallNoiseOverride = noise;
			Result r = preview.Generate(subworld, biome, vanilla, coverage, width, height, seed, ox, oy);
			string outPath = a.TryGetValue("out", out string o) ? o : "preview.html";
			File.WriteAllText(outPath, Html.Render(r), new UTF8Encoding(false));
			Console.WriteLine($"{r.Subworld} / {r.Biome}: {r.Width}x{r.Height}, open {r.OpenCells} ({100.0 * r.OpenCells / r.Cells:F1}%), "
				+ $"backwalls {r.BackwallCells} ({100.0 * r.BackwallCells / r.Cells:F1}%), visible {r.VisibleBackwalls} ({100.0 * r.VisibleBackwalls / r.Cells:F1}% of cells, "
				+ $"{(r.OpenCells > 0 ? 100.0 * r.VisibleBackwalls / r.OpenCells : 0):F1}% of open cells)");
			Console.WriteLine("wrote " + Path.GetFullPath(outPath));
			return 0;
		}

		private static Dictionary<string, string> ParseArgs(string[] args)
		{
			var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < args.Length; i++)
			{
				if (!args[i].StartsWith("--")) continue;
				string key = args[i].Substring(2);
				string value = i + 1 < args.Length && !args[i + 1].StartsWith("--") ? args[++i] : "true";
				d[key] = value;
			}
			return d;
		}
	}

	public sealed class Result
	{
		public string Subworld, Biome, BiomeNoise, OverrideNoise, BackwallNoise;
		public float Coverage;
		public int Width, Height, Seed;
		public byte[] Foreground;   // 1 = solid
		public byte[] Backwall;     // 1 = backwall present
		public string[] ForegroundElement; // element id per cell (for hover)
		public int Cells, OpenCells, BackwallCells, VisibleBackwalls;
		public List<string> Notes = new List<string>();
	}
}
