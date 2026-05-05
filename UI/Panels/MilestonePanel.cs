using NMSE.Core;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.Controls;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

public partial class MilestonePanel : UserControl
{
    /// <summary>
    /// Maps a stat Id to all TextBoxes that display it. Most stats map to a single entry;
    /// guild stats that also appear in the Kills/OtherStats columns have two entries so both
    /// are kept in sync on load.
    /// </summary>
    private readonly Dictionary<string, List<InvariantNumericTextBox>> _fields = new();

    private readonly List<(Label label, string locKey)> _localisedLabels = new();
    private IconManager? _iconManager;

    /// <summary>Raw (unclamped) milestone stat values read from JSON at load time, keyed by stat Id.</summary>
    private readonly Dictionary<string, int> _rawMilestoneValues = new();

    private readonly Dictionary<string, PictureBox> _sectionIcons = new();

    /// Guild rank labels: maps (statId, labelKind) -> Label.
    /// labelKind <see cref="GuildLabelKindRank"/> = rank progress ("3 / 11"), labelKind <see cref="GuildLabelKindPromo"/> = promotion-in label ("7").
    private const int GuildLabelKindRank = 0;
    private const int GuildLabelKindPromo = 1;
    private readonly Dictionary<(string, int), Label> _guildRankLabels = new();

    /// <summary>Language keys that need updating for guild rank labels (locKey, labelRef).</summary>
    private readonly List<(Label label, string locKey)> _guildLocalisedLabels = new();

    private static readonly Dictionary<string, string> SectionIconMap = MilestoneLogic.SectionIconMap;

    public MilestonePanel()
    {
        InitializeComponent();
    }

    public void SetIconManager(IconManager? iconManager)
    {
        _iconManager = iconManager;
        LoadSectionIcons();
    }

    private void LoadSectionIcons()
    {
        if (_iconManager == null) return;
        foreach (var kvp in _sectionIcons)
        {
            if (SectionIconMap.TryGetValue(kvp.Key, out string? filename))
            {
                var icon = _iconManager.GetIcon(filename);
                if (icon != null)
                    kvp.Value.Image = icon;
            }
        }
    }

    private static TableLayoutPanel CreateColumnSection(int columnCount)
    {
        var section = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly,
            AutoScroll = true,
            Dock = DockStyle.Top,
            ColumnCount = columnCount,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(0),
        };
        for (int i = 0; i < columnCount; i++)
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        for (int i = 0; i < columnCount; i++)
        {
            var col = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 0,
                Padding = new Padding(4),
                Margin = new Padding(0),
            };
            col.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            col.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            section.Controls.Add(col, i, 0);
        }

        return section;
    }

    private static TableLayoutPanel CreateThreeColumnSection() => CreateColumnSection(3);
    private static TableLayoutPanel CreateFourColumnSection() => CreateColumnSection(4);

    private static TableLayoutPanel GetColumnPanel(TableLayoutPanel section, int colIndex)
    {
        return (TableLayoutPanel)section.GetControlFromPosition(colIndex, 0)!;
    }

    private void AddSectionTitle(TableLayoutPanel panel, string title, string? locKey = null)
    {
        int row = panel.RowCount++;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        var container = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };

        if (SectionIconMap.ContainsKey(title))
        {
            var iconBox = new PictureBox
            {
                Size = new Size(20, 20),
                SizeMode = PictureBoxSizeMode.Zoom,
                Margin = new Padding(0, 2, 4, 0),
            };
            _sectionIcons[title] = iconBox;
            container.Controls.Add(iconBox);
        }

        var label = new Label
        {
            Text = locKey != null ? UiStrings.Get(locKey) : title,
            Font = new Font(Control.DefaultFont.FontFamily, 9, FontStyle.Bold),
            AutoSize = true,
            Padding = new Padding(0, 2, 0, 0),
        };

        FontManager.ApplyHeadingFont(label, 12);

        if (locKey != null)
            _localisedLabels.Add((label, locKey));

        container.Controls.Add(label);

        panel.Controls.Add(container, 0, row);
        panel.SetColumnSpan(container, panel.ColumnCount);
    }

    /// <summary>Adds an empty spacer row for visual separation between sections within a column.</summary>
    private static void AddVerticalSpacer(TableLayoutPanel panel, int height = 12)
    {
        int row = panel.RowCount++;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
    }

    private void AddField(TableLayoutPanel panel, string locKey, string statId)
    {
        int row = panel.RowCount++;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        var label = new Label
        {
            Text = UiStrings.Get(locKey),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0),
            Padding = new Padding(4, 0, 0, 0),
            Height = 22,
            Width = 150,
        };
        _localisedLabels.Add((label, locKey));
        panel.Controls.Add(label, 0, row);

        var nud = new InvariantNumericTextBox
        {
            Minimum = int.MinValue,
            Maximum = int.MaxValue,
            Height = 22,
            Width = 110,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0),
        };
        panel.Controls.Add(nud, 1, row);
        RegisterField(statId, nud);
    }

    /// <summary>
    /// Adds a guild stat row: editable value box (col 1), rank label "x / Y" (col 2).
    /// A second row below shows a "Promotion In" read-only label.
    /// The <paramref name="panel"/> must have at least 3 columns (label, value, rank).
    /// </summary>
    private void AddGuildField(TableLayoutPanel panel, string locKey, string statId)
    {
        // Row: label | value | rank
        int row = panel.RowCount++;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        var label = new Label
        {
            Text = UiStrings.Get(locKey),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0),
            Padding = new Padding(4, 0, 0, 0),
            Height = 22,
            Width = 130,
        };
        _localisedLabels.Add((label, locKey));
        panel.Controls.Add(label, 0, row);

        var nud = new InvariantNumericTextBox
        {
            Minimum = int.MinValue,
            Maximum = int.MaxValue,
            Height = 22,
            Width = 80,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0),
        };
        panel.Controls.Add(nud, 1, row);
        RegisterField(statId, nud);

        int maxRank = MilestoneLogic.GetGuildMaxRank(statId);
        var rankLabel = new Label
        {
            Text = $"0 / {maxRank}",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(4, 0, 0, 0),
            Height = 22,
        };
        panel.Controls.Add(rankLabel, 2, row);
        _guildRankLabels[(statId, GuildLabelKindRank)] = rankLabel;

        // Row: "Promotion In" label (col 0) | value (col 1 only, aligned with value box)
        int promoRow = panel.RowCount++;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));

        var promoNameLabel = new Label
        {
            Text = UiStrings.Get("milestone.guild_promo_in"),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = SystemColors.GrayText,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0),
            Padding = new Padding(12, 0, 0, 0),
            Height = 20,
        };
        _guildLocalisedLabels.Add((promoNameLabel, "milestone.guild_promo_in"));
        panel.Controls.Add(promoNameLabel, 0, promoRow);

        var promoValLabel = new Label
        {
            Text = "-",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = SystemColors.GrayText,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0),
            Padding = new Padding(0),
            Height = 20,
        };
        panel.Controls.Add(promoValLabel, 1, promoRow);
        _guildRankLabels[(statId, GuildLabelKindPromo)] = promoValLabel;

        // Update rank labels whenever the value changes
        nud.NumericValueChanged += (_, _) => UpdateGuildRankLabels(statId, (int)(nud.NumericValue ?? 0));
    }

    /// <summary>Registers a TextBox for a stat ID, supporting multiple boxes per stat.</summary>
    private void RegisterField(string statId, InvariantNumericTextBox nud)
    {
        if (!_fields.TryGetValue(statId, out var list))
        {
            list = [];
            _fields[statId] = list;
        }
        list.Add(nud);
    }

    /// <summary>Updates the rank progress label and Promotion In label for a guild stat.</summary>
    private void UpdateGuildRankLabels(string statId, int value)
    {
        if (_guildRankLabels.TryGetValue((statId, GuildLabelKindRank), out var rankLbl))
        {
            int rank = MilestoneLogic.GetGuildRank(statId, value);
            int maxRank = MilestoneLogic.GetGuildMaxRank(statId);
            rankLbl.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} / {1}", rank, maxRank);
        }
        if (_guildRankLabels.TryGetValue((statId, GuildLabelKindPromo), out var promoLbl))
        {
            int nextIn = MilestoneLogic.GetGuildNextRankIn(statId, value);
            promoLbl.Text = nextIn < 0 ? UiStrings.Get("milestone.guild_rank_max") : nextIn.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private static JsonArray? FindGlobalStats(JsonObject saveData) => MilestoneLogic.FindGlobalStats(saveData);

    public void LoadData(JsonObject saveData)
    {
        SuspendLayout();
        try
        {
        foreach (var list in _fields.Values)
            foreach (var nud in list)
                nud.NumericValue = 0;

        _rawMilestoneValues.Clear();

        var entries = FindGlobalStats(saveData);
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries.GetObject(i);
            if (entry == null) continue;
            string? id = entry.GetString("Id");
            if (id == null || !_fields.TryGetValue(id, out var list)) continue;

            int val = MilestoneLogic.ReadStatEntryValue(entry);
            _rawMilestoneValues[id] = val;
            foreach (var nud in list)
                nud.NumericValue = Math.Max((int)(nud.Minimum ?? int.MinValue), Math.Min((int)(nud.Maximum ?? int.MaxValue), val));
        }

        // Update all guild rank displays after loading
        foreach (var kvp in _guildRankLabels)
        {
            if (kvp.Key.Item2 != GuildLabelKindRank) continue; // only trigger once per stat
            string statId = kvp.Key.Item1;
            int val = _rawMilestoneValues.TryGetValue(statId, out int v) ? v : 0;
            UpdateGuildRankLabels(statId, val);
        }
        }
        finally
        {
            ResumeLayout(true);
        }
    }

    public void SaveData(JsonObject saveData)
    {
        var entries = FindGlobalStats(saveData);
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries.GetObject(i);
            if (entry == null) continue;
            string? id = entry.GetString("Id");
            if (id == null || !_fields.TryGetValue(id, out var list)) continue;

            var valueObj = entry.GetObject("Value");
            if (valueObj == null) continue;

            // Use the value from whichever field was actually changed (any non-clamped value wins)
            int? writtenValue = null;
            foreach (var nud in list)
            {
                int value = (int)(nud.NumericValue ?? 0);
                if (_rawMilestoneValues.TryGetValue(id, out int raw))
                {
                    int clamped = (int)Math.Max((int)(nud.Minimum ?? int.MinValue), Math.Min((int)(nud.Maximum ?? int.MaxValue), raw));
                    if (value != clamped)
                    {
                        writtenValue = value;
                        break;
                    }
                }
                else
                {
                    writtenValue = value;
                    break;
                }
            }

            if (writtenValue.HasValue)
                MilestoneLogic.WriteStatEntryValue(entry, writtenValue.Value);
        }
    }

    public void ApplyUiLocalisation()
    {
        if (_tabControl.TabPages.Count >= 2)
        {
            _tabControl.TabPages[0].Text = UiStrings.Get("milestone.tab_main");
            _tabControl.TabPages[1].Text = UiStrings.Get("milestone.tab_other");
        }

        foreach (var (label, locKey) in _localisedLabels)
            label.Text = UiStrings.Get(locKey);

        foreach (var (label, locKey) in _guildLocalisedLabels)
            label.Text = UiStrings.Get(locKey);
    }
}
