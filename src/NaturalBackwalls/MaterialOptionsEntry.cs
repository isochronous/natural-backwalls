using System.Collections.Generic;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using UnityEngine;

namespace NaturalBackwalls
{
	/// <summary>
	/// A dropdown that picks the backwall material of one biome group from that group's own
	/// solids, plus "None". Built the way PLib's enum dropdown is, but with runtime choices.
	/// </summary>
	public sealed class MaterialOptionsEntry : OptionsEntry
	{
		private sealed class Choice : ITooltipListableOption, IListableOption
		{
			public string Id { get; }
			private readonly string title;
			private readonly string tooltip;

			public Choice(string id, string title, string tooltip)
			{
				Id = id;
				this.title = title;
				this.tooltip = tooltip;
			}

			public string GetProperName() => title;
			public string GetToolTipText() => tooltip;
		}

		private readonly BiomeGroup group;
		private readonly List<Choice> choices = new List<Choice>();
		private Choice chosen;
		private GameObject comboBox;

		public override object Value
		{
			get => chosen?.Id;
			set
			{
				string id = value as string ?? Options.NoMaterial;
				foreach (Choice c in choices)
				{
					if (c.Id == id)
					{
						chosen = c;
						Update();
						return;
					}
				}
				chosen = choices[0];
				Update();
			}
		}

		public MaterialOptionsEntry(BiomeGroup group)
			: base(group.Id, new OptionAttribute(group.Title, "Backwall material behind the " + group.Title + " biome. The list holds the solids found in that biome. \"None\" leaves it without backwalls.", "Materials"))
		{
			this.group = group;
			choices.Add(new Choice(Options.NoMaterial, "None", "No natural backwall in this biome."));
			foreach (string id in group.Materials)
			{
				Element element = ElementLoader.FindElementByName(id);
				if (element == null || !element.IsSolid)
					continue;
				string name = element.name;
				if (id == group.DefaultMaterial)
					name += " (default)";
				choices.Add(new Choice(id, name, element.FullDescription(false)));
			}
			chosen = choices[0];
		}

		public override GameObject GetUIComponent()
		{
			int width = 0;
			foreach (Choice c in choices)
				width = Mathf.Max(width, c.GetProperName().Length);
			comboBox = new PComboBox<Choice>("Select")
			{
				BackColor = PUITuning.Colors.ButtonPinkStyle,
				InitialItem = chosen,
				Content = choices,
				EntryColor = PUITuning.Colors.ButtonBlueStyle,
				TextStyle = PUITuning.Fonts.TextLightStyle,
				OnOptionSelected = OnSelected,
			}.SetMinWidthInCharacters(width).Build();
			Update();
			return comboBox;
		}

		public override void ReadFrom(object settings)
		{
			if (settings is Options options)
				Value = options.MaterialFor(group);
		}

		public override bool WriteTo(object settings)
		{
			if (!(settings is Options options))
				return false;
			if (options.Materials == null)
				options.Materials = new Dictionary<string, string>();
			string id = chosen?.Id ?? Options.NoMaterial;
			bool changed = !options.Materials.TryGetValue(group.Id, out string old) || old != id;
			options.Materials[group.Id] = id;
			return changed;
		}

		private void Update()
		{
			if (comboBox != null && chosen != null)
				PComboBox<Choice>.SetSelectedItem(comboBox, chosen);
		}

		private void OnSelected(GameObject _, Choice selected)
		{
			if (selected != null)
				chosen = selected;
		}
	}
}
