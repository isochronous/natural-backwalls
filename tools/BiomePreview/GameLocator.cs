using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace BiomePreview
{
	/// <summary>
	/// Finds the game's Managed folder (the assemblies the tool runs). In order: gamelibs.txt next to
	/// the exe (written by a development build), the ONI_GAME_FOLDER environment variable, the folder
	/// remembered from an earlier run, every Steam library on the machine, and finally a folder picker
	/// whose answer is remembered. Each candidate is checked for the assembly the tool needs.
	/// </summary>
	public static class GameLocator
	{
		private const string Probe = "Assembly-CSharp-firstpass.dll";
		private static readonly string SettingsFile = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BiomePreview", "gamefolder.txt");

		/// <summary>The Managed folder, or null when nothing was found and no picker was allowed.</summary>
		public static string FindManaged(bool allowPicker)
		{
			foreach (string candidate in Candidates())
			{
				string managed = AsManaged(candidate);
				if (managed != null)
					return managed;
			}
			if (!allowPicker)
				return null;
			using var dialog = new System.Windows.Forms.FolderBrowserDialog
			{
				Description = "Select the Oxygen Not Included game folder (the one containing OxygenNotIncluded.exe)",
				UseDescriptionForTitle = true,
			};
			if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return null;
			string picked = AsManaged(dialog.SelectedPath);
			if (picked == null)
				return null;
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile));
				File.WriteAllText(SettingsFile, dialog.SelectedPath);
			}
			catch (Exception)
			{
				// Not remembering the choice only means asking again next time.
			}
			return picked;
		}

		private static IEnumerable<string> Candidates()
		{
			string next = Path.Combine(AppContext.BaseDirectory, "gamelibs.txt");
			if (File.Exists(next))
				yield return File.ReadAllText(next).Trim();
			string env = Environment.GetEnvironmentVariable("ONI_GAME_FOLDER");
			if (!string.IsNullOrWhiteSpace(env))
				yield return env;
			if (File.Exists(SettingsFile))
				yield return File.ReadAllText(SettingsFile).Trim();
			foreach (string library in SteamLibraries())
				yield return Path.Combine(library, "steamapps", "common", "OxygenNotIncluded");
		}

		/// <summary>Accepts the game folder, its Data folder or the Managed folder itself; returns the Managed folder when it holds the assemblies.</summary>
		private static string AsManaged(string folder)
		{
			if (string.IsNullOrWhiteSpace(folder))
				return null;
			foreach (string candidate in new[]
			{
				folder,
				Path.Combine(folder, "Managed"),
				Path.Combine(folder, "OxygenNotIncluded_Data", "Managed"),
			})
			{
				if (File.Exists(Path.Combine(candidate, Probe)))
					return Path.GetFullPath(candidate);
			}
			return null;
		}

		/// <summary>Every Steam library folder, from the Steam install path and its libraryfolders.vdf.</summary>
		private static IEnumerable<string> SteamLibraries()
		{
			string steam = null;
			try
			{
				using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
				steam = key?.GetValue("SteamPath") as string;
			}
			catch (Exception)
			{
			}
			if (string.IsNullOrEmpty(steam))
				yield break;
			yield return steam;
			string vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
			if (!File.Exists(vdf))
				yield break;
			foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
				yield return m.Groups[1].Value.Replace("\\\\", "\\");
		}
	}
}
