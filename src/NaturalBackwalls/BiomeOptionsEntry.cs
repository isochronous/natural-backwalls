using System.Collections.Generic;
using System.Globalization;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using TMPro;
using UnityEngine;

namespace NaturalBackwalls
{
	/// <summary>
	/// One options row per biome group: a dropdown that picks the backwall material from that
	/// group's own solids (plus "None"), and a text box with the coverage fraction. Built the way
	/// PLib's enum dropdown and float field are, but with runtime choices.
	/// PLib's dialog never calls ReadFrom/WriteTo on dynamic entries (only on the attribute-declared
	/// ones), so this entry binds straight to the Options instance it was created from: it starts
	/// from that object's values and writes every change into it, and the dialog's OK then
	/// serialises the whole object.
	/// </summary>
	public sealed class BiomeOptionsEntry : OptionsEntry
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

		/// <summary>Width of every material dropdown, so the column lines up whatever the names are.</summary>
		private const int DropdownWidthInCharacters = 24;
		private const string CoverageFormat = "F2";

		private readonly BiomeGroup group;
		private readonly Options options;
		private readonly List<Choice> choices = new List<Choice>();
		private Choice chosen;
		private float coverage;
		private GameObject comboBox;
		private GameObject textField;

		/// <summary>The material id; coverage is exposed through Coverage.</summary>
		public override object Value
		{
			get => chosen?.Id;
			set
			{
				string id = value as string ?? Options.NoMaterial;
				chosen = choices[0];
				foreach (Choice c in choices)
				{
					if (c.Id == id)
					{
						chosen = c;
						break;
					}
				}
				Update();
			}
		}

		public float Coverage
		{
			get => coverage;
			set
			{
				coverage = Mathf.Clamp01(value);
				Update();
			}
		}

		public BiomeOptionsEntry(BiomeGroup group, Options options)
			: base(group.Id, new OptionAttribute(group.Title,
				"Backwall material behind the " + group.Title + " biome, chosen from the solids found there (\"None\" leaves it without backwalls), and the fraction of its cells that get one (0 to 1) when it is not the starting biome. The Aquatic Planet Pack uses 0.2 for its reef and kelp forest.",
				"Biomes"))
		{
			this.group = group;
			this.options = options;
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
			Value = options.MaterialFor(group);
			Coverage = options.CoverageFor(group);
		}

		public override GameObject GetUIComponent()
		{
			PComboBox<Choice> dropdown = new PComboBox<Choice>("Material")
			{
				BackColor = PUITuning.Colors.ButtonPinkStyle,
				InitialItem = chosen,
				Content = choices,
				EntryColor = PUITuning.Colors.ButtonBlueStyle,
				TextStyle = PUITuning.Fonts.TextLightStyle,
				ToolTip = "Backwall material",
				OnOptionSelected = OnSelected,
			}.SetMinWidthInCharacters(DropdownWidthInCharacters);
			dropdown.AddOnRealize(go => comboBox = go);
			PTextField field = new PTextField("Coverage")
			{
				OnTextChanged = OnTextChanged,
				ToolTip = "Coverage when this is not the starting biome: fraction of its cells that get a backwall, 0 to 1. Default " + group.DefaultCoverage.ToString(CoverageFormat, CultureInfo.InvariantCulture) + ".",
				Text = coverage.ToString(CoverageFormat, CultureInfo.InvariantCulture),
				MinWidth = 64,
				MaxLength = 6,
				Type = PTextField.FieldType.Float,
			};
			field.AddOnRealize(go => textField = go);
			GameObject row = new PPanel("BiomeRow")
			{
				Direction = PanelDirection.Horizontal,
				Spacing = 6,
				Alignment = TextAnchor.MiddleLeft,
			}.AddChild(dropdown).AddChild(field).Build();
			Update();
			return row;
		}

		public override void ReadFrom(object settings)
		{
			if (!(settings is Options o))
				return;
			Value = o.MaterialFor(group);
			Coverage = o.CoverageFor(group);
		}

		public override bool WriteTo(object settings)
		{
			if (!(settings is Options o))
				return false;
			if (o.Materials == null)
				o.Materials = new Dictionary<string, string>();
			if (o.Coverages == null)
				o.Coverages = new Dictionary<string, float>();
			string id = chosen?.Id ?? Options.NoMaterial;
			bool changed = !o.Materials.TryGetValue(group.Id, out string oldId) || oldId != id;
			changed |= !o.Coverages.TryGetValue(group.Id, out float oldCoverage) || oldCoverage != coverage;
			o.Materials[group.Id] = id;
			o.Coverages[group.Id] = coverage;
			return changed;
		}

		private void Update()
		{
			if (comboBox != null && chosen != null)
				PComboBox<Choice>.SetSelectedItem(comboBox, chosen);
			TMP_InputField input = textField != null ? textField.GetComponentInChildren<TMP_InputField>() : null;
			if (input != null)
				input.text = coverage.ToString(CoverageFormat, CultureInfo.InvariantCulture);
		}

		private void OnSelected(GameObject _, Choice selected)
		{
			if (selected == null)
				return;
			chosen = selected;
			WriteTo(options);
		}

		private void OnTextChanged(GameObject _, string text)
		{
			if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
				|| float.TryParse(text, out parsed))
				coverage = Mathf.Clamp01(parsed);
			Update();
			WriteTo(options);
		}
	}
}
