using System.Collections.Generic;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using UnityEngine;

namespace NaturalBackwalls
{
	/// <summary>
	/// One dropdown per subworld zone: the noise pattern its backwall patches follow. Bound straight
	/// to the Options instance, like the biome rows, since PLib never reads or writes dynamic entries.
	/// </summary>
	public sealed class PatternOptionsEntry : OptionsEntry
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

		private const int WidthInCharacters = 24;

		private readonly PatternGroup group;
		private readonly Options options;
		private readonly List<Choice> choices = new List<Choice>();
		private Choice chosen;
		private GameObject comboBox;

		public override object Value
		{
			get => chosen?.Id;
			set
			{
				chosen = choices[0];
				foreach (Choice c in choices)
					if (c.Id == (value as string)) { chosen = c; break; }
				Update();
			}
		}

		public PatternOptionsEntry(PatternGroup group, Options options)
			: base(group.Id, new OptionAttribute(group.Title, "Biomes: " + group.Biomes, "Patterns"))
		{
			this.group = group;
			this.options = options;
			if (group.Vanilla)
				choices.Add(new Choice(Patterns.Vanilla.Id, Patterns.Vanilla.Title, Patterns.Vanilla.Description));
			foreach (Pattern p in Patterns.All)
				choices.Add(new Choice(p.Id, p.Title + (p.Id == group.DefaultPattern ? " (default)" : ""), p.Description));
			Value = options.PatternFor(group).Id;
		}

		public override GameObject GetUIComponent()
		{
			comboBox = new PComboBox<Choice>("Pattern")
			{
				BackColor = PUITuning.Colors.ButtonPinkStyle,
				InitialItem = chosen,
				Content = choices,
				EntryColor = PUITuning.Colors.ButtonBlueStyle,
				TextStyle = PUITuning.Fonts.TextLightStyle,
				OnOptionSelected = OnSelected,
			}.SetMinWidthInCharacters(WidthInCharacters).Build();
			Update();
			return comboBox;
		}

		public override void ReadFrom(object settings)
		{
			if (settings is Options o)
				Value = o.PatternFor(group).Id;
		}

		public override bool WriteTo(object settings)
		{
			if (!(settings is Options o))
				return false;
			if (o.PatternIds == null)
				o.PatternIds = new Dictionary<string, string>();
			string id = chosen?.Id ?? group.DefaultPattern;
			bool changed = !o.PatternIds.TryGetValue(group.Id, out string old) || old != id;
			o.PatternIds[group.Id] = id;
			return changed;
		}

		private void Update()
		{
			if (comboBox != null && chosen != null)
				PComboBox<Choice>.SetSelectedItem(comboBox, chosen);
		}

		private void OnSelected(GameObject _, Choice selected)
		{
			if (selected == null)
				return;
			chosen = selected;
			WriteTo(options);
		}
	}
}
