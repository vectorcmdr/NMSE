using NMSE.Core;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

public partial class MilestonePanel : UserControl
{
    private readonly Dictionary<string, NumericUpDown> _fields = new();
    private readonly List<(Label label, string locKey)> _localisedLabels = new();
    private IconManager? _iconManager;

    /// <summary>Raw (unclamped) milestone stat values read from JSON at load time, keyed by stat Id.</summary>
    private readonly Dictionary<string, int> _rawMilestoneValues = new();

    private readonly Dictionary<string, PictureBox> _sectionIcons = new();

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
        panel.SetColumnSpan(container, 2);
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

        var nud = new NumericUpDown
        {
            Minimum = int.MinValue,
            Maximum = int.MaxValue,
            Height = 22,
            Width = 110,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0),
        };
        panel.Controls.Add(nud, 1, row);
        _fields[statId] = nud;
    }

    private static JsonArray? FindGlobalStats(JsonObject saveData) => MilestoneLogic.FindGlobalStats(saveData);

    public void LoadData(JsonObject saveData)
    {
        SuspendLayout();
        try
        {
        foreach (var nud in _fields.Values)
            nud.Value = 0;

        _rawMilestoneValues.Clear();

        var entries = FindGlobalStats(saveData);
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries.GetObject(i);
            if (entry == null) continue;
            string? id = entry.GetString("Id");
            if (id == null || !_fields.TryGetValue(id, out var nud)) continue;

            int val = MilestoneLogic.ReadStatEntryValue(entry);
            _rawMilestoneValues[id] = val;
            nud.Value = Math.Max(nud.Minimum, Math.Min(nud.Maximum, val));
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
            if (id == null || !_fields.TryGetValue(id, out var nud)) continue;

            var valueObj = entry.GetObject("Value");
            if (valueObj != null)
            {
                int value = (int)nud.Value;

                // Skip write if user didn't change the value from its clamped display
                if (_rawMilestoneValues.TryGetValue(id, out int raw))
                {
                    int clamped = (int)Math.Max(nud.Minimum, Math.Min(nud.Maximum, raw));
                    if (value == clamped)
                        continue; // Preserve original JSON value
                }

                MilestoneLogic.WriteStatEntryValue(entry, value);
            }
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
    }
}
