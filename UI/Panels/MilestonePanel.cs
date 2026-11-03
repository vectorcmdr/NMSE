using NMSE.Core;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.Controls;
using NMSE.UI.Dialogs;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

public partial class MilestonePanel : UserControl
{
    private const int GuildLabelKindRank = 0;
    private const int GuildLabelKindPromo = 1;
    private const int SectionWidth = 440;
    private const int SectionGap = 12;
    private const int MaxGridWidth = (SectionWidth + SectionGap) * 2 + 8;

    /// <summary>
    /// Maps a stat Id to all controls that display it. Shared stats have one control in each
    /// in-game menu location; edits are synchronised across them.
    /// </summary>
    private readonly Dictionary<string, List<InvariantNumericTextBox>> _fields =
        new(StringComparer.Ordinal);

    private readonly List<(Label Label, string LocKey)> _localisedLabels = new();
    private readonly List<(Label Label, string LocKey)> _guildLocalisedLabels = new();
    private readonly Dictionary<string, PictureBox> _sectionIcons = new(StringComparer.Ordinal);
    private readonly Dictionary<(string, int), Label> _guildRankLabels = new();
    private readonly Dictionary<int, Control> _placementControls = new();
    private readonly Dictionary<MilestoneTab, Panel> _tabScrollPanels = new();
    private readonly Dictionary<string, Panel> _otherCategoryPanels = new(StringComparer.Ordinal);

    private readonly Dictionary<string, int> _rawMilestoneValues = new(StringComparer.Ordinal);

    private readonly ToolTip _toolTip = new();

    private ListBox? _otherCategoryList;

    private readonly List<string> _otherCategoryKeys = new();
    private string? _currentOtherSection;

    private IconManager? _iconManager;
    private bool _loading;
    private bool _syncingFields;

    /// <summary>Raised when the user modifies any milestone stat value.</summary>
    public event EventHandler? DataModified;

    /// <summary>Raised when the user requests navigation to a JSON path in the Raw JSON Editor.</summary>
    public event EventHandler<GoToJsonEventArgs>? GoToJsonRequested;

    public MilestonePanel()
    {
        InitializeComponent();
        BuildCatalogUi();
    }

    public void SetIconManager(IconManager? iconManager)
    {
        _iconManager = iconManager;
        LoadSectionIcons();
    }

    private void LoadSectionIcons()
    {
        if (_iconManager == null) return;

        foreach (var pair in _sectionIcons)
        {
            string? filename = MilestoneCatalog.IconForSection(pair.Key);
            if (filename == null) continue;
            var icon = _iconManager.GetIcon(filename);
            if (icon != null)
                pair.Value.Image = icon;
        }
    }

    private void BuildCatalogUi(
        IReadOnlyDictionary<string, int>? preserveValues = null,
        int selectedTab = 0,
        string? otherSection = null)
    {
        var values = preserveValues ?? _fields.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Count > 0 ? (int)(pair.Value[0].NumericValue ?? 0) : 0,
            StringComparer.Ordinal);

        bool previousLoading = _loading;
        _loading = true;
        SuspendLayout();
        _tabControl.SuspendLayout();
        try
        {
            foreach (TabPage oldPage in _tabControl.TabPages)
                oldPage.Dispose();
            _tabControl.TabPages.Clear();
            _fields.Clear();
            _localisedLabels.Clear();
            _guildLocalisedLabels.Clear();
            _guildRankLabels.Clear();
            _placementControls.Clear();
            _tabScrollPanels.Clear();
            _sectionIcons.Clear();
            _otherCategoryList = null;

            _otherCategoryKeys.Clear();
            _currentOtherSection = null;

            foreach (MilestoneTab tab in MilestoneCatalog.TabOrder)
            {
                var page = new TabPage { Text = UiStrings.Get(MilestoneCatalog.TabLabelKey(tab)) };

                if (tab == MilestoneTab.OtherStats)
                    BuildOtherTab(page, otherSection);
                else
                    BuildMenuTab(page, tab);

                _tabControl.TabPages.Add(page);
            }

            if (selectedTab >= 0 && selectedTab < _tabControl.TabPages.Count)
                _tabControl.SelectedIndex = selectedTab;

            foreach (var pair in values)
            {
                if (_fields.TryGetValue(pair.Key, out var list))
                {
                    foreach (var nud in list)
                        nud.NumericValue = pair.Value;
                }
            }

            UpdateAllGuildRanks(values);
        }
        finally
        {
            _tabControl.ResumeLayout();
            ResumeLayout(true);
            _loading = previousLoading;
        }

        // BuildCatalogUi creates new labels and tab pages after the initial
        // application-wide theme pass. Re-apply the theme to the rebuilt tree so
        // first-boot dark mode does not leave the new labels with default colours.
        ThemeApplicator.ApplyToControlTree(this);
    }

    private void BuildMenuTab(TabPage page, MilestoneTab tab)
    {
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var grid = CreateSectionGrid();

        foreach (string sectionKey in MilestoneCatalog.GetSections(tab))
        {
            var placements = MilestoneCatalog.GetSectionPlacements(tab, sectionKey).ToList();
            AddGridControl(grid, BuildSectionBox(sectionKey, subcategoryKey: null, placements));
        }

        scroll.Controls.Add(grid);
        scroll.SizeChanged += (_, _) => UpdateSectionGridWidth(grid, scroll);
        UpdateSectionGridWidth(grid, scroll);
        page.Controls.Add(scroll);
        _tabScrollPanels[tab] = scroll;
    }

    private void BuildOtherTab(TabPage page, string? requestedSection)
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 4,
        };
        bool splitInitialised = false;
        split.SizeChanged += (_, _) =>
        {
            if (splitInitialised || split.Width < 600) return;
            split.Panel1MinSize = 150;
            split.Panel2MinSize = 350;
            split.SplitterDistance = Math.Clamp(190, split.Panel1MinSize,
                split.Width - split.Panel2MinSize - split.SplitterWidth);
            splitInitialised = true;
        };

        var list = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            Margin = new Padding(0),
        };
        split.Panel1.Controls.Add(list);

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4),
        };
        split.Panel2.Controls.Add(content);

        page.Controls.Add(split);
        _otherCategoryList = list;

        foreach (string sectionKey in MilestoneCatalog.GetSections(MilestoneTab.OtherStats))
        {
            _otherCategoryKeys.Add(sectionKey);
            list.Items.Add(UiStrings.Get(sectionKey));

            Panel categoryPanel = BuildOtherCategoryPanel(sectionKey);
            categoryPanel.Dock = DockStyle.Fill;
            categoryPanel.Visible = false;
            content.Controls.Add(categoryPanel);
            _otherCategoryPanels[sectionKey] = categoryPanel;
        }

        list.SelectedIndexChanged += OnOtherCategorySelected;
        int index = requestedSection != null ? _otherCategoryKeys.IndexOf(requestedSection) : 0;
        if (index < 0) index = 0;
        if (list.Items.Count > 0)
        {
            list.SelectedIndex = index;
            ShowOtherCategory(_otherCategoryKeys[index]);
        }
    }

    private void OnOtherCategorySelected(object? sender, EventArgs e)
    {
        int index = _otherCategoryList?.SelectedIndex ?? -1;
        if (index < 0 || index >= _otherCategoryKeys.Count) return;
        ShowOtherCategory(_otherCategoryKeys[index]);
    }

    private void ShowOtherCategory(string sectionKey)
    {
        foreach (var panel in _otherCategoryPanels.Values)
            panel.Visible = false;

        if (_otherCategoryPanels.TryGetValue(sectionKey, out var selected))
        {
            selected.Visible = true;
            selected.BringToFront();
            selected.AutoScrollPosition = new Point(0, 0);
        }

        _currentOtherSection = sectionKey;
    }

    private Panel BuildOtherCategoryPanel(string sectionKey)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(4),
        };

        var placements = MilestoneCatalog
            .GetSectionPlacements(MilestoneTab.OtherStats, sectionKey)
            .ToList();

        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = CreateCategoryHeader(sectionKey);
        host.Controls.Add(header, 0, 0);

        var grid = CreateSectionGrid();

        var subcategories = MilestoneCatalog
            .GetSubcategories(MilestoneTab.OtherStats, sectionKey)
            .ToList();

        if (subcategories.Count == 0)
        {
            AddGridControl(grid, BuildSectionBox(sectionKey, subcategoryKey: null, placements, showHeader: false));
        }
        else
        {
            foreach (string subcategoryKey in subcategories)
            {
                var subPlacements = placements
                    .Where(p => p.SubcategoryKey == subcategoryKey)
                    .OrderBy(p => UiStrings.Get(p.GameNameKey), StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(p => p.Order)
                    .ToList();
                AddGridControl(grid, BuildSectionBox(sectionKey, subcategoryKey, subPlacements));
            }
        }

        host.Controls.Add(grid, 0, 1);
        panel.Controls.Add(host);
        panel.SizeChanged += (_, _) => UpdateSectionGridWidth(grid, panel);
        UpdateSectionGridWidth(grid, panel);
        return panel;
    }
    private static FlowLayoutPanel CreateSectionGrid()
    {
        var grid = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(4),
            Margin = new Padding(0),
            Height = 0,
        };
        return grid;
    }

    private static void AddGridControl(FlowLayoutPanel grid, Control control)
    {
        grid.Controls.Add(control);
    }

    private static void UpdateSectionGridWidth(FlowLayoutPanel grid, Control container)
    {
        int available = container.ClientSize.Width - container.Padding.Horizontal - 8;
        int width = Math.Clamp(available, SectionWidth, MaxGridWidth);
        grid.MaximumSize = new Size(width, 0);
        grid.MinimumSize = new Size(Math.Min(width, SectionWidth), 0);
        grid.Width = width;
        grid.Height = grid.GetPreferredSize(new Size(width, 0)).Height;
    }
    private Control CreateCategoryHeader(string sectionKey)
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 6),
            Padding = new Padding(0),
        };

        AddSectionIcon(panel, sectionKey);

        var label = new Label
        {
            Text = UiStrings.Get(sectionKey),
            AutoSize = true,
            Padding = new Padding(0, 3, 0, 0),
            Margin = new Padding(0),
        };
        FontManager.ApplyHeadingFont(label, 14);
        _localisedLabels.Add((label, sectionKey));
        panel.Controls.Add(label);
        return panel;
    }

    private Control BuildSectionBox(
        string sectionKey,
        string? subcategoryKey,
        IReadOnlyList<MilestonePlacement> placements,
        bool showHeader = true)
    {
        bool hasGuildRank = placements.Any(p => p.ShowGuildRank);
        int columnCount = hasGuildRank ? 3 : 2;

        var box = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(SectionWidth, 0),
            MaximumSize = new Size(SectionWidth, 0),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, SectionGap, SectionGap),
            Padding = new Padding(8),
        };

        var table = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = columnCount,
            RowCount = 0,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 144));
        if (hasGuildRank)
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        if (showHeader)
            AddSectionHeader(table, sectionKey, subcategoryKey, columnCount);

        foreach (var placement in placements)
        {
            AddPlacementRow(table, placement, columnCount);
        }

        box.Controls.Add(table);
        return box;
    }

    private void AddSectionHeader(
        TableLayoutPanel table,
        string sectionKey,
        string? subcategoryKey,
        int columnCount)
    {
        int row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 4),
            Padding = new Padding(0),
        };

        if (subcategoryKey == null)
            AddSectionIcon(header, sectionKey);

        var label = new Label
        {
            Text = UiStrings.Get(subcategoryKey ?? sectionKey),
            AutoSize = true,
            Padding = new Padding(0, 2, 0, 0),
            Margin = new Padding(0),
        };
        FontManager.ApplyHeadingFont(label, subcategoryKey == null ? 12 : 10);
        _localisedLabels.Add((label, subcategoryKey ?? sectionKey));
        header.Controls.Add(label);

        table.Controls.Add(header, 0, row);
        table.SetColumnSpan(header, columnCount);
    }

    private void AddSectionIcon(FlowLayoutPanel header, string sectionKey)
    {
        if (!_sectionIcons.ContainsKey(sectionKey))
        {
            var picture = new PictureBox
            {
                Size = new Size(20, 20),
                SizeMode = PictureBoxSizeMode.Zoom,
                Margin = new Padding(0, 2, 5, 0),
            };
            _sectionIcons[sectionKey] = picture;

            if (_iconManager != null)
            {
                string? filename = MilestoneCatalog.IconForSection(sectionKey);
                if (filename != null)
                {
                    var icon = _iconManager.GetIcon(filename);
                    if (icon != null) picture.Image = icon;
                }
            }
        }

        header.Controls.Add(_sectionIcons[sectionKey]);
    }

    private void AddPlacementRow(
        TableLayoutPanel table,
        MilestonePlacement placement,
        int columnCount)
    {
        int row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var label = new Label
        {
            Text = UiStrings.Get(placement.GameNameKey),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0),
            Padding = new Padding(4, 2, 8, 2),
        };
        _localisedLabels.Add((label, placement.GameNameKey));
        table.Controls.Add(label, 0, row);

        var nud = new InvariantNumericTextBox
        {
            Minimum = int.MinValue,
            Maximum = int.MaxValue,
            Width = 140,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 2, 0, 2),
        };
        table.Controls.Add(nud, 1, row);
        RegisterField(placement.StatId, nud);

        int placementIndex = Array.IndexOf(MilestoneCatalog.Placements, placement);
        if (placementIndex >= 0)
            _placementControls[placementIndex] = nud;

        string tooltip = BuildPlacementTooltip(placement);
        _toolTip.SetToolTip(label, tooltip);
        _toolTip.SetToolTip(nud, tooltip);

        if (placement.ShowGuildRank)
        {
            int maxRank = MilestoneLogic.GetGuildMaxRank(placement.StatId);
            var rankLabel = new Label
            {
                Text = $"0 / {maxRank}",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(6, 2, 0, 2),
            };
            table.Controls.Add(rankLabel, 2, row);
            _guildRankLabels[(placement.StatId, GuildLabelKindRank)] = rankLabel;

            int promoRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var promoName = new Label
            {
                Text = UiStrings.Get("milestone.guild_promo_in"),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = SystemColors.GrayText,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0),
                Padding = new Padding(12, 1, 0, 1),
            };
            _guildLocalisedLabels.Add((promoName, "milestone.guild_promo_in"));
            table.Controls.Add(promoName, 0, promoRow);

            var promoValue = new Label
            {
                Text = "-",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = SystemColors.GrayText,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0),
                Padding = new Padding(0, 1, 0, 1),
            };
            table.Controls.Add(promoValue, 1, promoRow);
            _guildRankLabels[(placement.StatId, GuildLabelKindPromo)] = promoValue;
        }
    }

    private string BuildPlacementTooltip(MilestonePlacement placement)
    {
        string tooltip = UiStrings.Format(
            "milestone.stat_tooltip",
            UiStrings.Get(placement.EditorNameKey),
            placement.StatId);

        if (placement.NoteKey != null)
            tooltip += "\n" + UiStrings.Get(placement.NoteKey);

        if (MilestoneCatalog.Stats.TryGetValue(placement.StatId, out var stat) && stat.IsShared)
        {
            var others = stat.Placements
                .Where(p => !ReferenceEquals(p, placement) && p != placement)
                .Select(FormatLocation)
                .Distinct(StringComparer.CurrentCulture)
                .ToList();
            if (others.Count > 0)
                tooltip += "\n" + UiStrings.Format("milestone.shared_with", string.Join("; ", others));
        }

        if (placement.EditorOnly && placement.NoteKey != "milestone.note_editor_only")
            tooltip += "\n" + UiStrings.Get("milestone.note_editor_only");

        return tooltip;
    }

    private string FormatLocation(MilestonePlacement placement)
    {
        string location = UiStrings.Get(MilestoneCatalog.TabLabelKey(placement.Tab))
            + " > " + UiStrings.Get(placement.SectionKey);
        if (placement.SubcategoryKey != null)
            location += " > " + UiStrings.Get(placement.SubcategoryKey);
        return location;
    }

    /// <summary>Registers a value control for a stat, including live shared-stat syncing.</summary>
    private void RegisterField(string statId, InvariantNumericTextBox nud)
    {
        if (!_fields.TryGetValue(statId, out var list))
        {
            list = new List<InvariantNumericTextBox>();
            _fields[statId] = list;
        }
        list.Add(nud);

        nud.NumericValueChanged += (_, _) =>
        {
            if (_loading || _syncingFields) return;

            int value = (int)(nud.NumericValue ?? 0);
            _syncingFields = true;
            try
            {
                foreach (var other in list)
                {
                    if (!ReferenceEquals(other, nud))
                        other.NumericValue = value;
                }

                UpdateGuildRankLabels(statId, value);
            }
            finally
            {
                _syncingFields = false;
            }

            DataModified?.Invoke(this, EventArgs.Empty);
        };
    }

    private void UpdateAllGuildRanks(IReadOnlyDictionary<string, int>? values)
    {
        foreach (var stat in MilestoneCatalog.Stats.Values)
        {
            if (!stat.Placements.Any(p => p.ShowGuildRank)) continue;
            int value = values != null && values.TryGetValue(stat.StatId, out int v)
                ? v
                : 0;
            UpdateGuildRankLabels(stat.StatId, value);
        }
    }

    private void UpdateGuildRankLabels(string statId, int value)
    {
        if (_guildRankLabels.TryGetValue((statId, GuildLabelKindRank), out var rankLabel))
        {
            int rank = MilestoneLogic.GetGuildRank(statId, value);
            int maxRank = MilestoneLogic.GetGuildMaxRank(statId);
            rankLabel.Text = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0} / {1}",
                rank,
                maxRank);
        }

        if (_guildRankLabels.TryGetValue((statId, GuildLabelKindPromo), out var promoLabel))
        {
            int nextIn = MilestoneLogic.GetGuildNextRankIn(statId, value);
            promoLabel.Text = nextIn < 0
                ? UiStrings.Get("milestone.guild_rank_max")
                : nextIn.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private static JsonArray? FindGlobalStats(JsonObject saveData) => MilestoneLogic.FindGlobalStats(saveData);

    public void LoadData(JsonObject saveData)
    {
        _loading = true;
        SuspendLayout();
        try
        {
            foreach (var list in _fields.Values)
            {
                foreach (var nud in list)
                    nud.NumericValue = 0;
            }

            _rawMilestoneValues.Clear();

            var entries = FindGlobalStats(saveData);
            if (entries == null) return;

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries.GetObject(i);
                if (entry == null) continue;
                string? id = entry.GetString("Id");
                if (id == null || !_fields.TryGetValue(id, out var list)) continue;

                int value = MilestoneLogic.ReadStatEntryValue(entry);
                _rawMilestoneValues[id] = value;
                foreach (var nud in list)
                {
                    nud.NumericValue = Math.Max(
                        (int)(nud.Minimum ?? int.MinValue),
                        Math.Min((int)(nud.Maximum ?? int.MaxValue), value));
                }
            }

            foreach (var stat in MilestoneCatalog.Stats.Values)
            {
                if (!stat.Placements.Any(p => p.ShowGuildRank)) continue;
                int value = _rawMilestoneValues.TryGetValue(stat.StatId, out int v) ? v : 0;
                UpdateGuildRankLabels(stat.StatId, value);
            }
        }
        finally
        {
            _loading = false;
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

            int? writtenValue = null;
            foreach (var nud in list)
            {
                int value = (int)(nud.NumericValue ?? 0);
                if (_rawMilestoneValues.TryGetValue(id, out int raw))
                {
                    int clamped = (int)Math.Max(
                        (int)(nud.Minimum ?? int.MinValue),
                        Math.Min((int)(nud.Maximum ?? int.MaxValue), raw));
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
        _findStatBtn.Text = UiStrings.Get("milestone.find_stat");
        _toolTip.SetToolTip(_gotoJsonBtn, UiStrings.Get("goto_json.tooltip"));

        var values = _fields.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Count > 0 ? (int)(pair.Value[0].NumericValue ?? 0) : 0,
            StringComparer.Ordinal);
        int selectedTab = _tabControl.SelectedIndex;
        string? otherSection = _currentOtherSection;

        BuildCatalogUi(values, selectedTab, otherSection);
    }

    private void OnFindStatClicked(object? sender, EventArgs e)
    {
        using var dialog = new MilestoneStatReferenceDialog(
            MilestoneCatalog.Placements,
            FormatLocation,
            key => UiStrings.Get(key));
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.SelectedPlacement == null)
            return;

        NavigateToPlacement(dialog.SelectedPlacement);
    }

    private void NavigateToPlacement(MilestonePlacement placement)
    {
        int tabIndex = Array.IndexOf(MilestoneCatalog.TabOrder, placement.Tab);
        if (tabIndex >= 0 && tabIndex < _tabControl.TabPages.Count)
            _tabControl.SelectedIndex = tabIndex;

        if (placement.Tab == MilestoneTab.OtherStats && _otherCategoryList != null)
        {
            int categoryIndex = _otherCategoryKeys.IndexOf(placement.SectionKey);
            if (categoryIndex >= 0)
                _otherCategoryList.SelectedIndex = categoryIndex;
        }

        Application.DoEvents();

        int placementIndex = Array.IndexOf(MilestoneCatalog.Placements, placement);
        if (placementIndex < 0 || !_placementControls.TryGetValue(placementIndex, out var control))
            return;

        Control? scrollParent = _tabScrollPanels.TryGetValue(placement.Tab, out var scroll)
            ? scroll
            : null;
        if (placement.Tab == MilestoneTab.OtherStats
            && _otherCategoryPanels.TryGetValue(placement.SectionKey, out var categoryPanel))
        {
            scrollParent = categoryPanel;
        }

        if (scrollParent is ScrollableControl scrollable)
            scrollable.ScrollControlIntoView(control);
        control.Focus();
        if (control is TextBox textBox)
            textBox.SelectAll();
        HighlightControl(control);
    }

    private static void HighlightControl(Control control)
    {
        Color original = control.BackColor;
        control.BackColor = ThemeManager.Effective == AppTheme.Dark
            ? Color.FromArgb(80, 75, 40)
            : Color.FromArgb(255, 245, 190);

        var timer = new System.Windows.Forms.Timer { Interval = 1500 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            timer.Dispose();
            if (!control.IsDisposed)
                control.BackColor = original;
        };
        timer.Start();
    }

    private void OnGoToJsonClicked(object? sender, EventArgs e)
    {
        GoToJsonRequested?.Invoke(this, new GoToJsonEventArgs("PlayerStateData", "Stats"));
    }
}
