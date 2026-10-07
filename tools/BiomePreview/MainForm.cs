using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BiomePreview
{
	/// <summary>
	/// Left: the generation settings. Right: the rendered window (foreground, backwalls, outline).
	/// Bottom: the numbers. Generation is the same code the command line uses.
	/// </summary>
	public sealed class MainForm : Form
	{
		private readonly Preview preview;
		private readonly ComboBox biomeBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
		private readonly ComboBox variantBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
		private readonly Label variantLabel = new Label { Text = "Noise setup (this biome has several)", AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
		private readonly CheckBox vanillaBackwall = new CheckBox { Text = "Use the biome's vanilla backwall band", AutoSize = true };
		private readonly ComboBox patternBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
		private static readonly (string title, string tree)[] PatternList =
		{
			("Subworld's own / mod default (Reef)", null),
			("Reef (pack)", "noise/NaturalBackwallsReef"),
			("Kelp forest (pack)", "noise/NaturalBackwallsKelp"),
			("Abyss (pack)", "noise/NaturalBackwallsAbyss"),
			("Beach (pack)", "noise/NaturalBackwallsBeach"),
			("Strange (base game)", "noise/SandstoneStrange"),
		};
		private readonly NumericUpDown coverageBox = new NumericUpDown { Minimum = 0, Maximum = 1, DecimalPlaces = 2, Increment = 0.05m, Value = 0.4m, Width = 80 };
		private readonly NumericUpDown seedBox = new NumericUpDown { Minimum = 0, Maximum = int.MaxValue, Value = 1234, Width = 120 };
		private readonly NumericUpDown widthBox = new NumericUpDown { Minimum = 16, Maximum = 1024, Value = 200, Width = 70 };
		private readonly NumericUpDown heightBox = new NumericUpDown { Minimum = 16, Maximum = 1024, Value = 150, Width = 70 };
		private readonly NumericUpDown scaleBox = new NumericUpDown { Minimum = 1, Maximum = 12, Value = 4, Width = 60 };
		private readonly NumericUpDown normWidthBox = new NumericUpDown { Minimum = 0, Maximum = 2048, Value = 256, Width = 70 };
		private readonly NumericUpDown normHeightBox = new NumericUpDown { Minimum = 0, Maximum = 2048, Value = 384, Width = 70 };
		private readonly CheckBox hideForeground = new CheckBox { Text = "Hide foreground", AutoSize = true };
		private readonly CheckBox outline = new CheckBox { Text = "Outline backwalls through foreground", AutoSize = true };
		private readonly CheckBox autoGenerate = new CheckBox { Text = "Regenerate on every change", AutoSize = true, Checked = true };
		private readonly Button generateButton = new Button { Text = "Generate", Width = 120, Height = 32 };
		private readonly Button randomButton = new Button { Text = "Random seed", Width = 100 };
		private readonly Button saveButton = new Button { Text = "Save HTML...", Width = 120 };
		private readonly PictureBox picture = new PictureBox { SizeMode = PictureBoxSizeMode.AutoSize, BackColor = Color.Black };
		private readonly TextBox stats = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, Font = new Font("Consolas", 10f), BackColor = Color.FromArgb(30, 30, 34), ForeColor = Color.Gainsboro, BorderStyle = BorderStyle.None };

		private Result last;
		private bool loading;

		public MainForm(string streamingAssets)
		{
			preview = new Preview(streamingAssets);
			Text = "Biome preview";
			Width = 1400; Height = 900;
			BackColor = Color.FromArgb(40, 40, 44);
			ForeColor = Color.Gainsboro;

			var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
			Controls.Add(layout);

			var left = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(8) };
			layout.Controls.Add(left, 0, 0);
			layout.SetRowSpan(left, 2);
			AddRow(left, "Biome", biomeBox);
			left.Controls.Add(variantLabel);
			left.Controls.Add(variantBox);
			left.Controls.Add(vanillaBackwall);
			AddRow(left, "Backwall coverage (band size, 0 = none)", coverageBox);
			foreach (var p in PatternList) patternBox.Items.Add(p.title);
			patternBox.SelectedIndex = 0;
			AddRow(left, "Backwall noise pattern", patternBox);
			var seedRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
			seedRow.Controls.Add(seedBox); seedRow.Controls.Add(randomButton);
			AddRow(left, "Seed", seedRow);
			var sizeRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
			sizeRow.Controls.Add(widthBox); sizeRow.Controls.Add(new Label { Text = "x", AutoSize = true, Padding = new Padding(4, 6, 4, 0) }); sizeRow.Controls.Add(heightBox);
			AddRow(left, "Window (cells)", sizeRow);
			var normRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
			normRow.Controls.Add(normWidthBox); normRow.Controls.Add(new Label { Text = "x", AutoSize = true, Padding = new Padding(4, 6, 4, 0) }); normRow.Controls.Add(normHeightBox);
			AddRow(left, "World size for backwall noise (normalisation, 0 = window)", normRow);
			AddRow(left, "Scale (px per cell)", scaleBox);
			left.Controls.Add(hideForeground);
			left.Controls.Add(outline);
			left.Controls.Add(autoGenerate);
			left.Controls.Add(new Label { Height = 8 });
			left.Controls.Add(generateButton);
			left.Controls.Add(saveButton);

			var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Black };
			scroll.Controls.Add(picture);
			layout.Controls.Add(scroll, 1, 0);
			layout.Controls.Add(stats, 1, 1);

			biomeBox.SelectedIndexChanged += (s, e) => { if (!loading) { LoadVariants(); MaybeGenerate(); } };
			variantBox.SelectedIndexChanged += (s, e) => MaybeGenerate();
			vanillaBackwall.CheckedChanged += (s, e) => { coverageBox.Enabled = !(vanillaBackwall.Enabled && vanillaBackwall.Checked); MaybeGenerate(); };
			coverageBox.ValueChanged += (s, e) => MaybeGenerate();
			patternBox.SelectedIndexChanged += (s, e) => MaybeGenerate();
			seedBox.ValueChanged += (s, e) => MaybeGenerate();
			widthBox.ValueChanged += (s, e) => MaybeGenerate();
			heightBox.ValueChanged += (s, e) => MaybeGenerate();
			normWidthBox.ValueChanged += (s, e) => MaybeGenerate();
			normHeightBox.ValueChanged += (s, e) => MaybeGenerate();
			scaleBox.ValueChanged += (s, e) => Render();
			hideForeground.CheckedChanged += (s, e) => Render();
			outline.CheckedChanged += (s, e) => Render();
			generateButton.Click += (s, e) => Generate();
			randomButton.Click += (s, e) => { seedBox.Value = new Random().Next(1, int.MaxValue); if (!autoGenerate.Checked) Generate(); };
			saveButton.Click += (s, e) => SaveHtml();

			loading = true;
			biomeBox.DisplayMember = nameof(BiomeEntry.Label);
			int marsh = 0;
			foreach (BiomeEntry entry in preview.Entries())
			{
				if (entry.Key == "biomes/HotMarsh/Basic") marsh = biomeBox.Items.Count;
				biomeBox.Items.Add(entry);
			}
			biomeBox.SelectedIndex = marsh;
			LoadVariants();
			loading = false;
			Shown += (s, e) => Generate();
		}

		private static void AddRow(FlowLayoutPanel panel, string label, Control control)
		{
			panel.Controls.Add(new Label { Text = label, AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
			panel.Controls.Add(control);
		}

		private void LoadVariants()
		{
			bool wasLoading = loading; loading = true;
			variantBox.Items.Clear();
			if (biomeBox.SelectedItem is BiomeEntry entry)
			{
				variantBox.DisplayMember = nameof(NoiseVariant.Label);
				foreach (NoiseVariant v in entry.Variants)
					variantBox.Items.Add(v);
				variantBox.SelectedIndex = 0;
				bool several = variantBox.Items.Count > 1;
				variantBox.Visible = several; variantLabel.Visible = several;
				bool vanilla = preview.HasVanillaBackwall(entry.Key);
				vanillaBackwall.Enabled = vanilla;
				vanillaBackwall.Checked = vanilla;
				coverageBox.Enabled = !vanilla;
			}
			loading = wasLoading;
		}

		private void MaybeGenerate()
		{
			if (!loading && autoGenerate.Checked) Generate();
		}

		public void Generate()
		{
			if (!(biomeBox.SelectedItem is BiomeEntry entry) || !(variantBox.SelectedItem is NoiseVariant variant)) return;
			Cursor = Cursors.WaitCursor;
			try
			{
				preview.BackwallNoiseOverride = PatternList[Math.Max(0, patternBox.SelectedIndex)].tree;
				preview.NormaliseWidth = (int)normWidthBox.Value;
				preview.NormaliseHeight = (int)normHeightBox.Value;
				last = preview.Generate(variant, entry.Key, vanillaBackwall.Enabled && vanillaBackwall.Checked, (float)coverageBox.Value,
					(int)widthBox.Value, (int)heightBox.Value, (int)seedBox.Value, 0f, 0f);
				stats.Text = Summary(last);
				Render();
			}
			catch (Exception e)
			{
				stats.Text = e.ToString();
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		public string StatsText => stats.Text;

		private void Render()
		{
			if (last == null) return;
			int s = (int)scaleBox.Value, w = last.Width, h = last.Height;
			var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
			BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int[] pixels = new int[w * h];
			int solidColor = Color.FromArgb(76, 138, 58).ToArgb(), backwallColor = Color.FromArgb(90, 70, 48).ToArgb(), air = Color.FromArgb(169, 214, 245).ToArgb();
			bool showFg = !hideForeground.Checked;
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
				{
					int i = x + (h - 1 - y) * w; // game y goes up
					bool solid = last.Foreground[i] == 1, bw = last.Backwall[i] == 1;
					pixels[x + y * w] = showFg && solid ? solidColor : bw ? backwallColor : air;
				}
			System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
			bmp.UnlockBits(data);

			var scaled = new Bitmap(w * s, h * s, PixelFormat.Format32bppArgb);
			using (Graphics g = Graphics.FromImage(scaled))
			{
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.PixelOffsetMode = PixelOffsetMode.Half;
				g.DrawImage(bmp, 0, 0, w * s, h * s);
				if (outline.Checked && s >= 2)
				{
					using var pen = new Pen(Color.FromArgb(255, 211, 77), 1f);
					bool Bw(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && last.Backwall[x + (h - 1 - y) * w] == 1;
					for (int y = 0; y < h; y++)
						for (int x = 0; x < w; x++)
						{
							if (!Bw(x, y)) continue;
							float x0 = x * s + 0.5f, y0 = y * s + 0.5f, x1 = (x + 1) * s - 0.5f, y1 = (y + 1) * s - 0.5f;
							if (!Bw(x, y - 1)) g.DrawLine(pen, x0, y0, x1, y0);
							if (!Bw(x, y + 1)) g.DrawLine(pen, x0, y1, x1, y1);
							if (!Bw(x - 1, y)) g.DrawLine(pen, x0, y0, x0, y1);
							if (!Bw(x + 1, y)) g.DrawLine(pen, x1, y0, x1, y1);
						}
				}
			}
			bmp.Dispose();
			Image old = picture.Image;
			picture.Image = scaled;
			old?.Dispose();
		}

		private static string Summary(Result r)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"{r.Subworld}  /  {r.Biome}   seed {r.Seed}   {r.Width}x{r.Height}");
			sb.AppendLine($"biome noise {r.BiomeNoise}   override noise {r.OverrideNoise ?? "none"}   backwall noise {r.BackwallNoise ?? "none"}");
			foreach (string n in r.Notes) sb.AppendLine(n);
			sb.AppendLine();
			sb.AppendLine($"cells {r.Cells,7}");
			sb.AppendLine($"open  {r.OpenCells,7}  ({Pct(r.OpenCells, r.Cells)} of cells)");
			sb.AppendLine($"backwalls placed  {r.BackwallCells,7}  ({Pct(r.BackwallCells, r.Cells)} of cells)");
			sb.AppendLine($"backwalls visible {r.VisibleBackwalls,7}  ({Pct(r.VisibleBackwalls, r.Cells)} of cells, {Pct(r.VisibleBackwalls, r.OpenCells)} of open cells)");
			return sb.ToString();
		}

		private static string Pct(int a, int b) => b > 0 ? (100.0 * a / b).ToString("F1", CultureInfo.InvariantCulture) + "%" : "n/a";

		private void SaveHtml()
		{
			if (last == null) return;
			using var dialog = new SaveFileDialog { Filter = "HTML|*.html", FileName = (last.Biome.Replace("::", "_").Replace('/', '_')) + "-" + last.Seed + ".html" };
			if (dialog.ShowDialog(this) == DialogResult.OK)
				File.WriteAllText(dialog.FileName, Html.Render(last), new UTF8Encoding(false));
		}
	}
}
