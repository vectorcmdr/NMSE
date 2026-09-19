using System.Globalization;
using System.Linq;
using NMSE.Core;
using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.Controls;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

/// <summary>
/// Panel for managing player bases and storage containers.
/// Contains two inner tabbed panels: Bases and Storage.
/// </summary>
public partial class BasePanel : UserControl
{
    public event EventHandler? DataModified;

    /// <summary>Raised when the user requests navigation to a JSON path in the Raw JSON Editor.</summary>
    internal event EventHandler<GoToJsonEventArgs>? GoToJsonRequested;

    public BasePanel()
    {
        InitializeComponent();
        _basesSubPanel.DataModified += (s, e) => DataModified?.Invoke(this, EventArgs.Empty);
        _basesSubPanel.GoToJsonRequested += (s, e) => GoToJsonRequested?.Invoke(this, e);
        _storageSubPanel.GoToJsonRequested += (s, e) => GoToJsonRequested?.Invoke(this, e);
        _chestsSubPanel.GoToJsonRequested += (s, e) => GoToJsonRequested?.Invoke(this, e);
        _chestsSubPanel.DataModified += (s, e) => DataModified?.Invoke(this, EventArgs.Empty);
        _spaceStationSubPanel.DataModified += (s, e) => DataModified?.Invoke(this, EventArgs.Empty);
        _spaceStationSubPanel.GoToJsonRequested += (s, e) => GoToJsonRequested?.Invoke(this, e);
        _spaceStationSubPanel.GoToBaseRequested += (s, dataIndex) =>
        {
            _innerTabs.SelectedTab = _basesPage;
            _basesSubPanel.SelectBase(dataIndex);
        };
    }

    public void SetDatabase(GameItemDatabase? database)
    {
        _storageSubPanel.SetDatabase(database);
        _chestsSubPanel.SetDatabase(database);
    }

    public void SetIconManager(IconManager? iconManager)
    {
        _storageSubPanel.SetIconManager(iconManager);
        _chestsSubPanel.SetIconManager(iconManager);
    }

    public void LoadData(JsonObject saveData)
    {
        _basesSubPanel.LoadData(saveData);
        _storageSubPanel.LoadData(saveData);
        _chestsSubPanel.LoadData(saveData);
        _spaceStationSubPanel.LoadData(saveData);
    }

    public void SaveData(JsonObject saveData)
    {
        _basesSubPanel.SaveData(saveData);
        _storageSubPanel.SaveData(saveData);
        _chestsSubPanel.SaveData(saveData);
        _spaceStationSubPanel.SaveData(saveData);
    }

    public void ApplyUiLocalisation()
    {
        _basesPage.Text = UiStrings.Get("base.tab_bases");
        _chestsPage.Text = UiStrings.Get("base.tab_chests");
        _storagePage.Text = UiStrings.Get("base.tab_storage");
        _spaceStationPage.Text = UiStrings.Get("base.tab_systems");
        _basesSubPanel.ApplyUiLocalisation();
        _chestsSubPanel.ApplyUiLocalisation();
        _storageSubPanel.ApplyUiLocalisation();
        _spaceStationSubPanel.ApplyUiLocalisation();
    }
}

/// <summary>
/// NPC race lookup.
/// Maps NPC resource filenames to race names.
/// </summary>
internal static class NpcRace
{
    private static readonly Dictionary<string, string> RaceByFilename = new(StringComparer.OrdinalIgnoreCase)
    {
        { "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCVYKEEN.SCENE.MBIN", "Vy'keen" },
        { "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCKORVAX.SCENE.MBIN", "Korvax" },
        { "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCGEK.SCENE.MBIN", "Gek" },
        { "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCFOURTH.SCENE.MBIN", "Fourth Race" },
        { "MODELS/PLANETS/NPCS/WARRIOR/WARRIOR.SCENE.MBIN", "Vy'keen (Old)" },
        { "MODELS/PLANETS/NPCS/EXPLORER/EXPLORERIPAD.SCENE.MBIN", "Korvax (Old)" },
        { "MODELS/PLANETS/NPCS/LOWERORDER/LOWERORDER.SCENE.MBIN", "Gek (Old)" },
        { "MODELS/PLANETS/NPCS/FOURTHRACE/FOURTHRACE.SCENE.MBIN", "Fourth Race (Old)" },
    };

    public static string Lookup(string? filename)
    {
        if (string.IsNullOrEmpty(filename)) return "";
        return RaceByFilename.TryGetValue(filename, out var race) ? race : "";
    }

    public static IReadOnlyDictionary<string, string> GetAll() => RaceByFilename;
    public static string? GetFilename(string raceName)
        => RaceByFilename.FirstOrDefault(kvp => kvp.Value == raceName).Key;

    internal sealed class RaceItem
    {
        public string InternalName { get; }
        public string DisplayName { get; }
        public RaceItem(string internalName, string displayName) { InternalName = internalName; DisplayName = displayName; }
        public override string ToString() => DisplayName;
    }

    public static RaceItem[] GetRaceItems()
    {
        return RaceByFilename.Values
            .Select(r => new RaceItem(r, NpcRaceLocKeys.GetLocalised(r)))
            .ToArray();
    }
}

/// <summary>
/// Names for the five standard base NPC worker roles (indices 0-4 in NPCWorkers array).
/// </summary>
internal static class NpcWorkerNames
{
    private static readonly string[] WorkerLocKeys =
    {
        "base.worker_armorer", "base.worker_farmer", "base.worker_overseer",
        "base.worker_technician", "base.worker_scientist"
    };

    public static string Get(int index) => index >= 0 && index < WorkerLocKeys.Length
        ? UiStrings.Get(WorkerLocKeys[index])
        : UiStrings.Format("base.worker_n", index);
}

internal static class NpcRaceLocKeys
{
    private static readonly Dictionary<string, string> RaceKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Vy'keen"] = "common.race_vykeen",
        ["Korvax"] = "common.race_korvax",
        ["Gek"] = "common.race_gek",
        ["Fourth Race"] = "common.race_fourth",
        ["Vy'keen (Old)"] = "common.race_vykeen_old",
        ["Korvax (Old)"] = "common.race_korvax_old",
        ["Gek (Old)"] = "common.race_gek_old",
        ["Fourth Race (Old)"] = "common.race_fourth_old",
    };

    public static string GetLocalised(string raceName)
    {
        if (RaceKeys.TryGetValue(raceName, out var key))
            return UiStrings.Get(key);
        return raceName;
    }
}

/// <summary>
/// Bases sub-panel: base list with reorder support, name, items count, NPC management,
/// and export/import/move base computer buttons.
/// </summary>
internal class BasesSubPanel : UserControl
{
    public event EventHandler? DataModified;

    /// <summary>Raised when the user requests navigation to a JSON path in the Raw JSON Editor.</summary>
    internal event EventHandler<GoToJsonEventArgs>? GoToJsonRequested;

    // NPC section
    private readonly ComboBox _npcSelector;
    private readonly ComboBox _npcRaceCombo;
    private readonly TextBox _npcSeed;
    private readonly Button _generateNpcSeedBtn;

    // Base list (left column)
    private readonly ListBox _baseList;
    private readonly Button _toTopBtn;
    private readonly Button _moveUpBtn;
    private readonly Button _moveDownBtn;
    private readonly Button _toBottomBtn;
    private readonly Label _baseListTitle;
    private readonly Label _moveOrderLabel;

    // Base Info section (right column)
    private readonly TextBox _baseName;
    private TextBox _baseItems = null!;
    private string? _pendingBaseName;

    // NPC Summon
    private readonly Button _summonWorkerBtn;

    // Buttons
    private readonly Button _exportBtn;
    private readonly Button _importBtn;
    private readonly Button _moveBaseComputerBtn;
    private readonly Button _deleteBaseBtn;
    private readonly Button _sortAlphaAscBtn;
    private readonly Button _sortAlphaDescBtn;
    private readonly Button _clearTerrainEditsBtn;
    private readonly Button _clearAllTerrainEditsBtn;
    private readonly Button _clearAllTerrainExceptBasesBtn;
    private Button _gotoBasesListBtn = null!;
    private Button _gotoNpcWorkersBtn = null!;

    // Labels for localisation
    private readonly Label _npcTitle;
    private readonly Label _baseTitle;
    private Label? _npcLabel;
    private Label? _raceLabel;
    private Label? _seedLabel;
    private Label? _nameLabel;
    private Label? _itemsLabel;

    // Freighter rooms section (in Objects tab)
    private Label _freighterRoomsTitle = null!;
    private ListBox _freighterRoomList = null!;
    private Panel _objectsFreighterPanel = null!;

    // State
    private bool _loading;
    private JsonObject? _playerState;
    private readonly List<NpcWorkerItem> _npcWorkers = new();
    private readonly List<BaseInfoItem> _baseInfoItems = new();
    private readonly Random _rng = new();

    // Objects tab
    private readonly DoubleBufferedTabControl _rightTabs;
    private readonly TabPage _infoTab;
    private readonly TabPage _objectsTab;

    // Objects tab - base fields (editable)
    private InvariantNumericTextBox _objBaseVersion = null!;
    private InvariantNumericTextBox _objOriginalBaseVersion = null!;
    private TextBox _objGalacticAddress = null!;
    private InvariantNumericTextBox _objPositionX = null!;
    private InvariantNumericTextBox _objPositionY = null!;
    private InvariantNumericTextBox _objPositionZ = null!;
    private InvariantNumericTextBox _objForwardX = null!;
    private InvariantNumericTextBox _objForwardY = null!;
    private InvariantNumericTextBox _objForwardZ = null!;
    private InvariantNumericTextBox _objUserData = null!;
    private InvariantNumericTextBox _objLastUpdateTimestamp = null!;
    private TextBox _objRID = null!;
    private InvariantNumericTextBox _objScreenshotAtX = null!;
    private InvariantNumericTextBox _objScreenshotAtY = null!;
    private InvariantNumericTextBox _objScreenshotAtZ = null!;
    private InvariantNumericTextBox _objScreenshotPosX = null!;
    private InvariantNumericTextBox _objScreenshotPosY = null!;
    private InvariantNumericTextBox _objScreenshotPosZ = null!;
    private CheckBox _objIsReported = null!;
    private CheckBox _objIsFeatured = null!;
    private TextBox _objAutoPower = null!;

    // Objects tab - base fields (read-only)
    private TextBox _objBaseType = null!;
    private TextBox _objOwnerLID = null!;
    private TextBox _objOwnerUID = null!;
    private TextBox _objOwnerUSN = null!;
    private TextBox _objOwnerPTK = null!;
    private TextBox _objOwnerTS = null!;
    private TextBox _objLastEditedById = null!;
    private TextBox _objLastEditedByUsername = null!;
    private TextBox _objGameMode = null!;
    private TextBox _objDifficulty = null!;
    private TextBox _objPlatformToken = null!;

    // Objects tab - labels (for localisation)
    private readonly List<(string key, Label control)> _objectsLabels = new();

    // Objects tab - object list
    private TextBox _objectSearchBox = null!;
    private Button _objectSearchClearBtn = null!;
    private ListBox _objectList = null!;

    // Objects tab - object detail fields
    private InvariantNumericTextBox _objectDetailTimestamp = null!;
    private TextBox _objectDetailObjectID = null!;
    private InvariantNumericTextBox _objectDetailUserData = null!;
    private InvariantNumericTextBox _objectDetailPositionX = null!;
    private InvariantNumericTextBox _objectDetailPositionY = null!;
    private InvariantNumericTextBox _objectDetailPositionZ = null!;
    private InvariantNumericTextBox _objectDetailUpX = null!;
    private InvariantNumericTextBox _objectDetailUpY = null!;
    private InvariantNumericTextBox _objectDetailUpZ = null!;
    private InvariantNumericTextBox _objectDetailAtX = null!;
    private InvariantNumericTextBox _objectDetailAtY = null!;
    private InvariantNumericTextBox _objectDetailAtZ = null!;

    // Objects tab - detail labels (for localisation)
    private readonly List<(string key, Label control)> _objectDetailLabels = new();

    public BasesSubPanel()
    {
        DoubleBuffered = true;
        SuspendLayout();

        // --- Header strip for GOTO buttons ---
        var basesHeaderStrip = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 1
        };
        basesHeaderStrip.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        basesHeaderStrip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        basesHeaderStrip.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

		var basesGotoPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			AutoSize = true,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
		};
        basesHeaderStrip.Controls.Add(basesGotoPanel, 2, 0);

        // --- Outer two-column layout ---
        var outerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10)
        };
        outerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var outerContent = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0)
        };
        outerContent.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // left: base list
        outerContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // right: NPC + info
        outerContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        outerLayout.Controls.Add(basesHeaderStrip, 0, 0);

        // --- Left column: base list + reorder arrows ---
        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(0, 0, 12, 0),
            MinimumSize = new Size(417, 0)
        };
        leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));    // list
        leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));        // arrows
        leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));              // title
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));          // list + arrows

        _baseListTitle = new Label
        {
            Text = UiStrings.Get("base.base_list_title"),
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 4)
        };
        FontManager.ApplyHeadingFont(_baseListTitle, 11);
        leftLayout.Controls.Add(_baseListTitle, 0, 0);
        leftLayout.SetColumnSpan(_baseListTitle, 2);

        _baseList = new ListBox
        {
            Dock = DockStyle.Fill,
            SelectionMode = SelectionMode.One,
            IntegralHeight = false
        };
        _baseList.SelectedIndexChanged += OnBaseSelected;
        leftLayout.Controls.Add(_baseList, 0, 1);

        var arrowPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(4, 0, 0, 0)
        };
        _moveOrderLabel = new Label
        {
            Text = UiStrings.Get("base.move_base_in_list"),
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 4)
        };
        _toTopBtn = new Button
        {
            Text = UiStrings.Get("base.move_base_to_top"),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Enabled = false
        };
        _moveUpBtn = new Button
        {
            Text = UiStrings.Get("base.move_base_up"),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Enabled = false
        };
        _moveDownBtn = new Button
        {
            Text = UiStrings.Get("base.move_base_down"),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Enabled = false
        };
        _toBottomBtn = new Button
        {
            Text = UiStrings.Get("base.move_base_to_bottom"),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Enabled = false
        };
        _toTopBtn.Click += OnMoveBaseToTop;
        _moveUpBtn.Click += OnMoveBaseUp;
        _moveDownBtn.Click += OnMoveBaseDown;
        _toBottomBtn.Click += OnMoveBaseToBottom;
        _sortAlphaAscBtn = new Button
        {
            Text = UiStrings.Get("base.sort_az"),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Enabled = false
        };
        _sortAlphaAscBtn.Click += OnSortAlphaAsc;
        _sortAlphaDescBtn = new Button
        {
            Text = UiStrings.Get("base.sort_za"),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Enabled = false
        };
        _sortAlphaDescBtn.Click += OnSortAlphaDesc;
        arrowPanel.Controls.Add(_moveOrderLabel);
        arrowPanel.Controls.Add(_toTopBtn);
        arrowPanel.Controls.Add(_moveUpBtn);
        arrowPanel.Controls.Add(_moveDownBtn);
        arrowPanel.Controls.Add(_toBottomBtn);
        arrowPanel.Controls.Add(_sortAlphaAscBtn);
        arrowPanel.Controls.Add(_sortAlphaDescBtn);
        leftLayout.Controls.Add(arrowPanel, 1, 1);

        outerContent.Controls.Add(leftLayout, 0, 0);

        // --- Right column: Tabbed Info + Objects ---
        _rightTabs = new DoubleBufferedTabControl { Dock = DockStyle.Fill };
        _infoTab = new TabPage("Info");
        _objectsTab = BuildObjectsTab();

        // --- Right column: NPC section + Base Info section + Freighter Rooms ---
        var rightLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            RowCount = 13,
            Padding = new Padding(0)
        };
        rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        for (int i = 0; i < 12; i++)
            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        int row = 0;

        // --- NPC Section ---
        _npcTitle = new Label
        {
            Text = UiStrings.Get("base.npc_header"),
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 4)
        };
        FontManager.ApplyHeadingFont(_npcTitle, 11);
        rightLayout.Controls.Add(_npcTitle, 0, row);
        rightLayout.SetColumnSpan(_npcTitle, 3);
        row++;

        _npcSelector = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _npcSelector.SelectedIndexChanged += OnNpcSelected;
        _npcLabel = AddRow(rightLayout, UiStrings.Get("base.npc_label"), _npcSelector, row); row++;

        _npcRaceCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _npcRaceCombo.Items.AddRange(NpcRace.GetRaceItems());
        _npcRaceCombo.SelectedIndexChanged += OnNpcRaceChanged;
        _raceLabel = AddRow(rightLayout, UiStrings.Get("base.npc_race_label"), _npcRaceCombo, row); row++;

        var seedPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
        };
        seedPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        seedPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        seedPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _npcSeed = new TextBox { Dock = DockStyle.Fill };
        _npcSeed.Leave += (s, e) =>
        {
            // Immediately apply seed change to underlying data and fire DataModified
            if (_npcSelector.SelectedItem is NpcWorkerItem npcItem)
            {
                try
                {
                    var normalizedNpcSeed = SeedHelper.NormalizeSeed(_npcSeed.Text);
                    if (normalizedNpcSeed != null)
                    {
                        var seedArr = npcItem.Data.GetArray("ResourceElement.Seed")
                                      ?? npcItem.Data.GetObject("ResourceElement")?.GetArray("Seed");
                        if (seedArr != null && seedArr.Length > 1)
                        {
                            seedArr.Set(1, normalizedNpcSeed);
                            DataModified?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
                catch { }
            }
        };
        _npcSeed.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _npcSeed.Parent?.Focus(); }
        };
        _generateNpcSeedBtn = new Button { Text = UiStrings.Get("common.generate"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(70, 0), Anchor = AnchorStyles.Top | AnchorStyles.Bottom };
        _generateNpcSeedBtn.Click += OnGenerateNpcSeed;
        seedPanel.Controls.Add(_npcSeed, 0, 0);
        seedPanel.Controls.Add(_generateNpcSeedBtn, 1, 0);
        _seedLabel = AddRow(rightLayout, UiStrings.Get("base.npc_seed_label"), seedPanel, row); row++;

        // Summon NPC worker to selected base
        _summonWorkerBtn = new Button { Text = UiStrings.Get("base.summon_npc"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Enabled = false };
        _summonWorkerBtn.Click += OnSummonWorkerToBase;
        rightLayout.Controls.Add(_summonWorkerBtn, 1, row);
        row++;

        // Separator
        var sep1 = new Label { AutoSize = false, Height = 8 };
        rightLayout.Controls.Add(sep1, 0, row);
        rightLayout.SetColumnSpan(sep1, 3);
        row++;

        // --- Base Info Section ---
        _baseTitle = new Label
        {
            Text = UiStrings.Get("base.base_header"),
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 4)
        };
        FontManager.ApplyHeadingFont(_baseTitle, 11);
        rightLayout.Controls.Add(_baseTitle, 0, row);
        rightLayout.SetColumnSpan(_baseTitle, 3);
        row++;

        _baseName = new TextBox { Dock = DockStyle.Fill };
        _baseName.Leave += OnBaseNameChanged;
        _baseName.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _baseName.Parent?.Focus(); }
        };
        _nameLabel = AddRow(rightLayout, UiStrings.Get("base.name_label"), _baseName, row); row++;

        // Buttons panel row 1: Export / Import / Move Base Computer
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 6, 0, 0)
        };
        _exportBtn = new Button { Text = UiStrings.Get("base.export"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(80, 0), Enabled = false };
        _exportBtn.Click += OnExport;
        _importBtn = new Button { Text = UiStrings.Get("base.import"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(80, 0), Enabled = false };
        _importBtn.Click += OnImport;
        _moveBaseComputerBtn = new Button { Text = UiStrings.Get("base.move_basecomp"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(140, 0), Enabled = false };
        _moveBaseComputerBtn.Click += OnMoveBaseComputer;
        _deleteBaseBtn = new Button { Text = UiStrings.Get("base.delete_base"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(90, 0), Enabled = false };
        _deleteBaseBtn.Click += OnDeleteBase;
        buttonPanel.Controls.Add(_exportBtn);
        buttonPanel.Controls.Add(_importBtn);
        buttonPanel.Controls.Add(_moveBaseComputerBtn);
        buttonPanel.Controls.Add(_deleteBaseBtn);
		_gotoBasesListBtn = new Button
		{
			FlatStyle = FlatStyle.Flat,
			FlatAppearance = { BorderColor = ThemeManager.Effective == AppTheme.Dark ? Color.FromArgb(100, 100, 100) : SystemColors.ControlDark, BorderSize = 1 },
			Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular, GraphicsUnit.Point),
			Size = new Size(28, 24),
			Text = "\U0001F4D1",
			Margin = new Padding(1, 3, 1, 1),
			Cursor = Cursors.Hand,
		};
        _gotoBasesListBtn.Click += (_, _) =>
        {
            if (_baseList.SelectedItem is BaseInfoItem baseItem)
                GoToJsonRequested?.Invoke(this, new GoToJsonEventArgs("PlayerStateData", "PersistentPlayerBases", $"[{baseItem.DataIndex}]"));
        };
        basesGotoPanel.Controls.Add(_gotoBasesListBtn);

		_gotoNpcWorkersBtn = new Button
		{
			FlatStyle = FlatStyle.Flat,
			FlatAppearance = { BorderColor = ThemeManager.Effective == AppTheme.Dark ? Color.FromArgb(100, 100, 100) : SystemColors.ControlDark, BorderSize = 1 },
			Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular, GraphicsUnit.Point),
			Size = new Size(28, 24),
			Text = "\U0001F4D1",
			Margin = new Padding(1, 3, 1, 1),
			Cursor = Cursors.Hand,
		};
        _gotoNpcWorkersBtn.Click += (_, _) => GoToJsonRequested?.Invoke(this, new GoToJsonEventArgs("PlayerStateData", "NPCWorkers"));
        basesGotoPanel.Controls.Add(_gotoNpcWorkersBtn);
        rightLayout.Controls.Add(buttonPanel, 0, row);
        rightLayout.SetColumnSpan(buttonPanel, 3);
        row++;

        // Buttons panel row 2: Terrain edit clearing
        var terrainButtonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 2, 0, 0)
        };
        _clearTerrainEditsBtn = new Button { Text = UiStrings.Get("base.clear_terrain"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(140, 0), Enabled = false };
        _clearTerrainEditsBtn.Click += OnClearTerrainEdits;
        _clearAllTerrainEditsBtn = new Button { Text = UiStrings.Get("base.clear_all_terrain"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(140, 0), Enabled = false };
        _clearAllTerrainEditsBtn.Click += OnClearAllTerrainEdits;
        _clearAllTerrainExceptBasesBtn = new Button { Text = UiStrings.Get("base.clear_all_terrain_except_bases"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(140, 0), Enabled = false };
        _clearAllTerrainExceptBasesBtn.Click += OnClearAllTerrainExceptBases;
        terrainButtonPanel.Controls.Add(_clearTerrainEditsBtn);
        terrainButtonPanel.Controls.Add(_clearAllTerrainEditsBtn);
        terrainButtonPanel.Controls.Add(_clearAllTerrainExceptBasesBtn);
        rightLayout.Controls.Add(terrainButtonPanel, 0, row);
        rightLayout.SetColumnSpan(terrainButtonPanel, 3);
        row++;

        var baseFieldsPanel = BuildBaseFieldsPanel();
        rightLayout.Controls.Add(baseFieldsPanel, 0, row);
        rightLayout.SetColumnSpan(baseFieldsPanel, 3);

        var infoScrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        infoScrollPanel.Controls.Add(rightLayout);
        _infoTab.Controls.Add(infoScrollPanel);
        _rightTabs.TabPages.Add(_infoTab);
        _rightTabs.TabPages.Add(_objectsTab);
        outerContent.Controls.Add(_rightTabs, 1, 0);

        outerLayout.Controls.Add(outerContent, 0, 1);

        Controls.Add(outerLayout);
        ResumeLayout(false);
        PerformLayout();
    }

    public void LoadData(JsonObject saveData)
    {
        _loading = true;
        SuspendLayout();
        _npcSelector.BeginUpdate();
        _baseList.BeginUpdate();
        try
        {
        _npcSelector.Items.Clear();
        _npcWorkers.Clear();
        _baseList.Items.Clear();
        _baseInfoItems.Clear();
        _npcSeed.Text = "";
        _baseName.Text = "";
        _baseItems.Text = "";
        _exportBtn.Enabled = false;
        _importBtn.Enabled = false;
        _moveBaseComputerBtn.Enabled = false;
        _deleteBaseBtn.Enabled = false;
        _sortAlphaAscBtn.Enabled = false;
        _sortAlphaDescBtn.Enabled = false;
        _clearTerrainEditsBtn.Enabled = false;
        _clearAllTerrainEditsBtn.Enabled = false;
        _clearAllTerrainExceptBasesBtn.Enabled = false;
        ClearObjectFields();

        try
        {
            _playerState = saveData.GetObject("PlayerStateData");
            if (_playerState == null) return;

            _clearAllTerrainEditsBtn.Enabled = true;
            _clearAllTerrainExceptBasesBtn.Enabled = true;

            // Load NPCWorkers (up to 5: Armorer, Farmer, Overseer, Technician, Scientist)
            var npcWorkers = _playerState.GetArray("NPCWorkers");
            if (npcWorkers != null)
            {
                for (int i = 0; i < npcWorkers.Length && i < 5; i++)
                {
                    try
                    {
                        var npc = npcWorkers.GetObject(i);
                        bool hired = false;
                        try { hired = npc.GetBool("HiredWorker"); } catch { }
                        if (hired)
                        {
                            string workerName = NpcWorkerNames.Get(i);
                            var item = new NpcWorkerItem(npc, i);
                            _npcWorkers.Add(item);
                            _npcSelector.Items.Add(item);
                        }
                    }
                    catch { }
                }
            }

            // Load PersistentPlayerBases (HomePlanetBase, FreighterBase, PlayerSpaceStationBase,
            // PlayerShipBase corvette bases and PlayerSpaceBase asteroid bases with BaseVersion >= 3)
            var bases = _playerState.GetArray("PersistentPlayerBases");
            if (bases != null)
            {
                for (int i = 0; i < bases.Length; i++)
                {
                    try
                    {
                        var baseObj = bases.GetObject(i);
                        var kind = SpaceStationLogic.GetBaseKind(baseObj);
                        bool isHome = kind == SpaceStationLogic.BaseKind.Home;
                        bool isFreighter = kind == SpaceStationLogic.BaseKind.Freighter;
                        bool isStation = kind == SpaceStationLogic.BaseKind.SpaceStation;
                        bool isCorvette = kind == SpaceStationLogic.BaseKind.Corvette;
                        bool isSpace = kind == SpaceStationLogic.BaseKind.SpaceBase;

                        int baseVersion = 0;
                        try { baseVersion = baseObj.GetInt("BaseVersion"); } catch { }

                        if ((isHome || isFreighter || isStation || isCorvette || isSpace) && baseVersion >= 3)
                        {
                            string name;
                            if (isFreighter)
                                name = _playerState.GetString("PlayerFreighterName") ?? UiStrings.Format("base.fallback_base_name", i + 1);
                            else if (isCorvette)
                                name = SpaceStationLogic.ResolveCorvetteBaseName(_playerState, baseObj)
                                       ?? baseObj.GetString("Name")
                                       ?? UiStrings.Format("base.fallback_base_name", i + 1);
                            else
                                name = baseObj.GetString("Name") ?? UiStrings.Format("base.fallback_base_name", i + 1);
                            int objectCount = 0;
                            try
                            {
                                var objects = baseObj.GetArray("Objects");
                                if (objects != null) objectCount = objects.Length;
                            }
                            catch { }

                            var item = new BaseInfoItem(name, baseObj, i, objectCount, isFreighter, isStation, isCorvette, isSpace);
                            _baseInfoItems.Add(item);
                            _baseList.Items.Add(item);
                        }
                    }
                    catch { }
                }
            }

            if (_npcSelector.Items.Count > 0)
                _npcSelector.SelectedIndex = 0;
            if (_baseList.Items.Count > 0)
                _baseList.SelectedIndex = 0;
        }
        catch { }
        }
        finally
        {
            _baseList.EndUpdate();
            _npcSelector.EndUpdate();
            ResumeLayout(true);
            _loading = false;
        }
    }

    public void SaveData(JsonObject saveData)
    {
        try
        {
            if (_npcSelector.SelectedItem is NpcWorkerItem npcItem && _npcRaceCombo.SelectedItem is NpcRace.RaceItem raceItem)
            {
                string? filename = NpcRace.GetFilename(raceItem.InternalName);
                if (!string.IsNullOrEmpty(filename))
                {
                    var resourceElement = npcItem.Data.GetObject("ResourceElement");
                    if (resourceElement != null)
                        resourceElement.Set("Filename", filename);
                    else
                        npcItem.Data.Set("ResourceElement.Filename", filename);
                }
            }
        }
        catch { }

        // Save NPC seed changes
        try
        {
            if (_npcSelector.SelectedItem is NpcWorkerItem npcItem)
            {
                var normalizedNpcSeed = SeedHelper.NormalizeSeed(_npcSeed.Text);
                if (normalizedNpcSeed != null)
                {
                    var seedArr = npcItem.Data.GetArray("ResourceElement.Seed")
                                  ?? npcItem.Data.GetObject("ResourceElement")?.GetArray("Seed");
                    if (seedArr != null && seedArr.Length > 1)
                        seedArr.Set(1, normalizedNpcSeed);
                }
            }
        }
        catch { }

        try
        {
            // Apply pending base name change
            if (_baseList.SelectedItem is BaseInfoItem item && !string.IsNullOrEmpty(_pendingBaseName))
            {
                string currentName = item.Data.GetString("Name") ?? "";
                if (_pendingBaseName != currentName)
                {
                    item.Data.Set("Name", _pendingBaseName);
                    item.DisplayName = _pendingBaseName;
                    // Refresh list display
                    int idx = _baseList.SelectedIndex;
                    _baseList.SelectedIndexChanged -= OnBaseSelected;
                    _baseList.Items.RemoveAt(idx);
                    _baseList.Items.Insert(idx, item);
                    _baseList.SelectedIndex = idx;
                    _baseList.SelectedIndexChanged += OnBaseSelected;
                }
            }
        }
        catch { }
    }

    private void OnNpcSelected(object? sender, EventArgs e)
    {
        if (_npcSelector.SelectedItem is not NpcWorkerItem item) return;
        try
        {
            // Race
            string filename = "";
            try
            {
                filename = item.Data.GetString("ResourceElement.Filename")
                           ?? item.Data.GetObject("ResourceElement")?.GetString("Filename")
                           ?? "";
            }
            catch { }
            string raceName = NpcRace.Lookup(filename);
            SelectRaceByInternalName(raceName);

            // Seed
            string seed = "";
            try
            {
                var seedArr = item.Data.GetArray("ResourceElement.Seed")
                              ?? item.Data.GetObject("ResourceElement")?.GetArray("Seed");
                if (seedArr != null && seedArr.Length > 1)
                    seed = seedArr.Get(1)?.ToString() ?? "";
            }
            catch { }
            _npcSeed.Text = seed;
        }
        catch { }
        UpdateSummonButtonState();
    }

    private void OnNpcRaceChanged(object? sender, EventArgs e)
    {
        if (_npcSelector.SelectedItem is NpcWorkerItem item && _npcRaceCombo.SelectedItem is NpcRace.RaceItem raceItem)
        {
            string? filename = NpcRace.GetFilename(raceItem.InternalName);
            if (!string.IsNullOrEmpty(filename))
            {
                var resourceElement = item.Data.GetObject("ResourceElement");
                if (resourceElement != null)
                    resourceElement.Set("Filename", filename);
                else
                    item.Data.Set("ResourceElement.Filename", filename);
                if (!_loading)
                    DataModified?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void OnGenerateNpcSeed(object? sender, EventArgs e)
    {
        byte[] bytes = new byte[8];
        _rng.NextBytes(bytes);
        string newSeed = "0x" + BitConverter.ToString(bytes).Replace("-", "");
        _npcSeed.Text = newSeed;

        // Apply immediately to the underlying data
        if (_npcSelector.SelectedItem is NpcWorkerItem item)
        {
            try
            {
                var seedArr = item.Data.GetArray("ResourceElement.Seed")
                              ?? item.Data.GetObject("ResourceElement")?.GetArray("Seed");
                if (seedArr != null && seedArr.Length > 1)
                {
                    seedArr.Set(1, newSeed);
                    DataModified?.Invoke(this, EventArgs.Empty);
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Summons the selected NPC worker to the selected base.
    /// Sets the worker's BaseUA to the base's GalacticAddress,
    /// BaseOffset to the base's Position, and FreighterBase flag.
    /// </summary>
    private void OnSummonWorkerToBase(object? sender, EventArgs e)
    {
        if (_npcSelector.SelectedItem is not NpcWorkerItem npcItem) return;
        if (_baseList.SelectedItem is not BaseInfoItem baseItem) return;

        var result = MessageBox.Show(this, 
            UiStrings.Format("base.summon_worker_confirm", npcItem.ToString(), baseItem.DisplayName),
            UiStrings.Get("base.summon_worker_title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        try
        {
            // Determine if the base is a freighter base
            string? baseType = null;
            try
            {
                baseType = baseItem.Data.GetString("BaseType.PersistentBaseTypes")
                           ?? baseItem.Data.GetString("BaseType");
            }
            catch { }

            bool isFreighterBase = "FreighterBase".Equals(baseType, StringComparison.OrdinalIgnoreCase);

            // Get NPCWorkers array from playerState and the worker entry by index
            var npcWorkers = _playerState?.GetArray("NPCWorkers");
            if (npcWorkers == null || npcItem.Index >= npcWorkers.Length) return;
            var worker = npcWorkers.GetObject(npcItem.Index);

            // Copy GalacticAddress -> BaseUA
            var galacticAddress = baseItem.Data.Get("GalacticAddress");
            if (galacticAddress != null)
                worker.Set("BaseUA", galacticAddress);

            // Copy Position -> BaseOffset.
            // BaseOffset requires 4 elements (x, y, z, 1.0), while Position has only 3.
            if (baseItem.Data.Get("Position") is JsonArray position)
            {
                var baseOffset = new JsonArray();
                baseOffset.Add(position.Get(0));
                baseOffset.Add(position.Get(1));
                baseOffset.Add(position.Get(2));
                baseOffset.Add(1.0);
                worker.Set("BaseOffset", baseOffset);
            }

            // Set FreighterBase flag
            worker.Set("FreighterBase", isFreighterBase);

            MessageBox.Show(this, 
                UiStrings.Format("base.summon_complete_msg", npcItem.ToString(), baseItem.DisplayName),
                UiStrings.Get("base.summon_complete_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            DataModified?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("base.summon_failed", ex.Message), UiStrings.Get("common.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnBaseSelected(object? sender, EventArgs e)
    {
        if (_baseList.SelectedItem is not BaseInfoItem item)
        {
            _baseName.Text = "";
            _baseName.Enabled = false;
            _baseItems.Text = "";
            _exportBtn.Enabled = false;
            _importBtn.Enabled = false;
            _moveBaseComputerBtn.Enabled = false;
            _deleteBaseBtn.Enabled = false;
            _clearTerrainEditsBtn.Enabled = false;
            _clearAllTerrainEditsBtn.Enabled = true;
            _clearAllTerrainExceptBasesBtn.Enabled = true;
            _pendingBaseName = null;
            UpdateSummonButtonState();
            UpdateMoveButtonStates();
            UpdateFreighterBaseControls(false);
            ClearObjectFields();
            return;
        }

        bool isFreighter = item.IsFreighterBase;
        bool isStation = item.IsStationBase;
        bool isCorvette = item.IsCorvetteBase;
        bool isSpace = item.IsSpaceBase;

        // For freighter bases, show the freighter name (read from PlayerFreighterName,
        // not the base entry which is always empty). Corvette bases show the resolved
        // ship name. For planetary, space station and space bases, show the base's own
        // Name field.
        if (isFreighter || isCorvette)
        {
            _baseName.Text = item.DisplayName;
            _baseName.Enabled = false;
            _pendingBaseName = null;
        }
        else
        {
            _baseName.Text = item.Data.GetString("Name") ?? "";
            _baseName.Enabled = true;
            _pendingBaseName = _baseName.Text;
        }

        int objectCount = 0;
        try
        {
            var objects = item.Data.GetArray("Objects");
            if (objects != null) objectCount = objects.Length;
        }
        catch { }
        _baseItems.Text = objectCount.ToString(CultureInfo.CurrentCulture);
        _exportBtn.Enabled = true;
        _importBtn.Enabled = true;
        bool isShipBase = isFreighter || isStation || isCorvette || isSpace;
        _moveBaseComputerBtn.Enabled = !isShipBase;
        _deleteBaseBtn.Enabled = true;
        _clearTerrainEditsBtn.Enabled = !isShipBase;
        _clearAllTerrainEditsBtn.Enabled = !isShipBase;
        _clearAllTerrainExceptBasesBtn.Enabled = !isShipBase;
        UpdateFreighterBaseControls(isFreighter);

        if (isFreighter)
        {
            var rooms = FreighterLogic.DetectFreighterRooms(item.Data);
            _freighterRoomList.BeginUpdate();
            _freighterRoomList.Items.Clear();
            foreach (var room in rooms)
                _freighterRoomList.Items.Add(room);
            _freighterRoomList.EndUpdate();
        }

        UpdateSummonButtonState();
        UpdateMoveButtonStates();
        LoadObjectFields();
    }

    /// <summary>
    /// Shows or hides the freighter-specific controls (freighter rooms list)
    /// based on whether the selected base is a freighter base.
    /// </summary>
    private void UpdateFreighterBaseControls(bool isFreighterBase)
    {
        _objectsFreighterPanel.Visible = isFreighterBase;
        if (!isFreighterBase)
            _freighterRoomList.Items.Clear();
    }

    private void UpdateSummonButtonState()
    {
        _summonWorkerBtn.Enabled = _npcSelector.SelectedItem is NpcWorkerItem
                                   && _baseList.SelectedItem is BaseInfoItem;
    }

    private void SelectRaceByInternalName(string raceName)
    {
        foreach (var item in _npcRaceCombo.Items)
        {
            if (item is NpcRace.RaceItem ri && ri.InternalName == raceName)
            {
                _npcRaceCombo.SelectedItem = ri;
                return;
            }
        }
        _npcRaceCombo.SelectedIndex = -1;
    }

    internal void RefreshNpcRaceCombo()
    {
        string? currentInternal = (_npcRaceCombo.SelectedItem as NpcRace.RaceItem)?.InternalName;
        _npcRaceCombo.BeginUpdate();
        _npcRaceCombo.Items.Clear();
        _npcRaceCombo.Items.AddRange(NpcRace.GetRaceItems());
        if (currentInternal != null) SelectRaceByInternalName(currentInternal);
        _npcRaceCombo.EndUpdate();
    }

    private void OnBaseNameChanged(object? sender, EventArgs e)
    {
        if (_loading) return;
        if (_baseList.SelectedItem is not BaseInfoItem item) return;
        string newName = _baseName.Text.Trim();
        if (newName == _pendingBaseName) return;
        _pendingBaseName = newName;
        // Write to underlying data immediately so it survives base switching / tab switching
        if (!string.IsNullOrEmpty(_pendingBaseName))
        {
            item.Data.Set("Name", _pendingBaseName);
            item.DisplayName = _pendingBaseName;
            int idx = _baseList.SelectedIndex;
            _baseList.SelectedIndexChanged -= OnBaseSelected;
            _baseList.Items.RemoveAt(idx);
            _baseList.Items.Insert(idx, item);
            _baseList.SelectedIndex = idx;
            _baseList.SelectedIndexChanged += OnBaseSelected;
        }
        DataModified?.Invoke(this, EventArgs.Empty);
    }

    private void OnExport(object? sender, EventArgs e)
    {
        if (_baseList.SelectedItem is not BaseInfoItem item) return;
        try
        {
            string defaultName = item.Data.GetString("Name") ?? "Base";
            var config = ExportConfig.Instance;
            var vars = new Dictionary<string, string>
            {
                ["base_name"] = defaultName
            };

            using var dialog = new SaveFileDialog
            {
                Filter = ExportConfig.BuildDialogFilter(config.BaseExt, "Base files"),
                DefaultExt = config.BaseExt.TrimStart('.'),
                FileName = ExportConfig.BuildFileName(config.BaseTemplate, config.BaseExt, vars),
                Title = UiStrings.Get("base.export_title")
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                item.Data.ExportToFile(dialog.FileName);
                MessageBox.Show(this, UiStrings.Get("base.export_success"), UiStrings.Get("base.export_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("base.export_failed", ex.Message), UiStrings.Get("common.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnImport(object? sender, EventArgs e)
    {
        if (_baseList.SelectedItem is not BaseInfoItem item) return;
        try
        {
            var config = ExportConfig.Instance;
            using var dialog = new OpenFileDialog
            {
                Filter = ExportConfig.BuildImportFilter(config.BaseExt, "Base files"),
                Title = UiStrings.Get("base.import_title")
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            var result = MessageBox.Show(this, 
                UiStrings.Get("base.import_confirm"),
                UiStrings.Get("base.confirm_import_title"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            var imported = JsonObject.ImportFromFile(dialog.FileName);

            // Copy imported base data: primarily the Objects array
            if (imported.Contains("Objects"))
            {
                item.Data.Set("Objects", imported.Get("Objects"));
            }
            if (imported.Contains("BaseVersion"))
            {
                item.Data.Set("BaseVersion", imported.Get("BaseVersion"));
            }
            if (imported.Contains("UserData"))
            {
                item.Data.Set("UserData", imported.Get("UserData"));
            }

            // Refresh display
            OnBaseSelected(this, EventArgs.Empty);
            DataModified?.Invoke(this, EventArgs.Empty);
            MessageBox.Show(this, UiStrings.Get("base.import_success"), UiStrings.Get("base.import_title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("base.import_failed", ex.Message), UiStrings.Get("common.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Moves the base computer (^BASE_FLAG) to the position of a user-selected target object.
    /// Uses a coordinate system transformation to properly re-anchor the base at the target
    /// location while keeping all objects at their correct world-space positions.
    /// Based on community knowledge of the NMS save format.
    /// </summary>
    private void OnMoveBaseComputer(object? sender, EventArgs e)
    {
        if (_baseList.SelectedItem is not BaseInfoItem item) return;
        try
        {
            var objects = item.Data.GetArray("Objects");
            if (objects == null || objects.Length == 0)
            {
                MessageBox.Show(this, 
                    UiStrings.Get("base.move_basecomp_warning"),
                    UiStrings.Get("base.move_basecomp_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Build list of candidate objects that can swap with the base computer
            var candidates = new List<BaseObjectItem>();
            for (int i = 0; i < objects.Length; i++)
            {
                try
                {
                    var obj = objects.GetObject(i);
                    string objectId = obj.GetString("ObjectID") ?? "";
                    if (!string.IsNullOrEmpty(objectId) && objectId != "^BASE_FLAG")
                    {
                        candidates.Add(new BaseObjectItem(objectId, obj, i));
                    }
                }
                catch { }
            }

            if (candidates.Count == 0)
            {
                MessageBox.Show(this, 
                    UiStrings.Get("base.move_basecomp_no_objects"),
                    UiStrings.Get("base.move_basecomp_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Show selection dialog
            using var selectForm = new Form
            {
                Text = UiStrings.Get("base.select_target"),
                Size = new Size(400, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            var listBox = new ListBox { Dock = DockStyle.Fill };
            foreach (var c in candidates)
                listBox.Items.Add(c);
            listBox.SelectedIndex = 0;

            var okBtn = new Button { Text = UiStrings.Get("common.ok"), DialogResult = DialogResult.OK, Dock = DockStyle.Bottom };
            selectForm.Controls.Add(listBox);
            selectForm.Controls.Add(okBtn);
            selectForm.AcceptButton = okBtn;

            if (selectForm.ShowDialog() != DialogResult.OK || listBox.SelectedItem is not BaseObjectItem target)
                return;

            // Find the base computer (^BASE_FLAG)
            JsonObject? baseFlag = null;
            for (int i = 0; i < objects.Length; i++)
            {
                try
                {
                    var obj = objects.GetObject(i);
                    if (obj.GetString("ObjectID") == "^BASE_FLAG")
                    {
                        baseFlag = obj;
                        break;
                    }
                }
                catch { }
            }

            if (baseFlag == null)
            {
                MessageBox.Show(this, UiStrings.Get("base.move_basecomp_not_found"), UiStrings.Get("common.error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Perform the full coordinate system transformation and position swap
            BaseLogic.MoveBaseComputer(item.Data, baseFlag, target.Data);
            OnBaseSelected(this, EventArgs.Empty);
            DataModified?.Invoke(this, EventArgs.Empty);
            MessageBox.Show(this, UiStrings.Get("base.move_basecomp_success"), UiStrings.Get("base.move_basecomp_success_title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("base.move_basecomp_failed", ex.Message), UiStrings.Get("common.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnClearTerrainEdits(object? sender, EventArgs e)
    {
        if (_baseList.SelectedItem is not BaseInfoItem item) return;
        if (_playerState == null) return;

        var result = MessageBox.Show(this,
            UiStrings.Format("base.clear_terrain_confirm", item.DisplayName),
            UiStrings.Get("base.clear_terrain_title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        try
        {
            int removed = BaseLogic.ClearTerrainEdits(_playerState, item.Data);
            if (removed > 0)
            {
                DataModified?.Invoke(this, EventArgs.Empty);
                MessageBox.Show(this,
                    UiStrings.Format("base.clear_terrain_success", removed),
                    UiStrings.Get("base.clear_terrain_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this,
                    UiStrings.Get("base.clear_terrain_none"),
                    UiStrings.Get("base.clear_terrain_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                UiStrings.Format("base.clear_terrain_failed", ex.Message),
                UiStrings.Get("common.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnClearAllTerrainEdits(object? sender, EventArgs e)
    {
        if (_playerState == null) return;

        var result = MessageBox.Show(this,
            UiStrings.Get("base.clear_all_terrain_confirm"),
            UiStrings.Get("base.clear_all_terrain_title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        try
        {
            int removed = BaseLogic.ClearAllTerrainEdits(_playerState);
            if (removed > 0)
            {
                DataModified?.Invoke(this, EventArgs.Empty);
                MessageBox.Show(this,
                    UiStrings.Format("base.clear_all_terrain_success", removed),
                    UiStrings.Get("base.clear_all_terrain_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this,
                    UiStrings.Get("base.clear_all_terrain_none"),
                    UiStrings.Get("base.clear_all_terrain_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                UiStrings.Format("base.clear_all_terrain_failed", ex.Message),
                UiStrings.Get("common.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnClearAllTerrainExceptBases(object? sender, EventArgs e)
    {
        if (_playerState == null) return;

        var result = MessageBox.Show(this,
            UiStrings.Get("base.clear_all_terrain_except_bases_confirm"),
            UiStrings.Get("base.clear_all_terrain_except_bases_title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        try
        {
            int removed = BaseLogic.ClearAllTerrainEditsExceptBases(_playerState);
            if (removed > 0)
            {
                DataModified?.Invoke(this, EventArgs.Empty);
                MessageBox.Show(this,
                    UiStrings.Format("base.clear_all_terrain_except_bases_success", removed),
                    UiStrings.Get("base.clear_all_terrain_except_bases_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this,
                    UiStrings.Get("base.clear_all_terrain_except_bases_none"),
                    UiStrings.Get("base.clear_all_terrain_except_bases_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                UiStrings.Format("base.clear_all_terrain_except_bases_failed", ex.Message),
                UiStrings.Get("common.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private TabPage BuildObjectsTab()
    {
        var tab = new TabPage("Objects");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // row 0: title
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // row 1: items
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // row 2: freighter panel
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // row 3: object split

        var objectsTitle = new Label
        {
            Text = UiStrings.Get("base.obj_heading"),
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 4)
        };
        FontManager.ApplyHeadingFont(objectsTitle, 11);
        _objectsLabels.Add(("base.obj_heading", objectsTitle));
        layout.Controls.Add(objectsTitle, 0, 0);

        _baseItems = new TextBox { Dock = DockStyle.Fill, Enabled = false };
        _itemsLabel = new Label { Text = UiStrings.Get("base.items_label"), AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 10, 0) };
        _objectsLabels.Add(("base.items_label", _itemsLabel));
        var itemsRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, AutoSize = true, Margin = Padding.Empty, Padding = Padding.Empty };
        itemsRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        itemsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        itemsRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        itemsRow.Controls.Add(_itemsLabel, 0, 0);
        itemsRow.Controls.Add(_baseItems, 1, 0);
        layout.Controls.Add(itemsRow, 0, 1);

        // Freighter rooms panel (visible only for freighter bases)
        _objectsFreighterPanel = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0)
        };
        _freighterRoomsTitle = new Label
        {
            Text = UiStrings.Get("base.freighter_rooms_header"),
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 4),
            Dock = DockStyle.Top
        };
        FontManager.ApplyHeadingFont(_freighterRoomsTitle, 11);
        _freighterRoomList = new ListBox
        {
            Dock = DockStyle.Top,
            SelectionMode = SelectionMode.None,
            IntegralHeight = false,
            MaximumSize = new Size(0, 160)
        };
        // WinForms Dock=Top reverse order: last added = topmost
        _objectsFreighterPanel.Controls.Add(_freighterRoomList);
        _objectsFreighterPanel.Controls.Add(_freighterRoomsTitle);
        _objectsFreighterPanel.Visible = false;
        layout.Controls.Add(_objectsFreighterPanel, 0, 2);

        var objectSplitPanel = BuildObjectSplitPanel();
        layout.Controls.Add(objectSplitPanel, 0, 3);

        tab.Controls.Add(layout);
        return tab;
    }

    private TableLayoutPanel BuildBaseFieldsPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 7,
            Padding = new Padding(0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // col 0: label
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // col 1: "X"
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));    // col 2: xField
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // col 3: "Y"
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));    // col 4: yField
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // col 5: "Z"
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));    // col 6: zField

        int row = 0;

        _objBaseVersion = new InvariantNumericTextBox { Width = 55, Anchor = AnchorStyles.Left };
        _objBaseVersion.NumericValueChanged += OnBaseFieldNumericChanged;
        AddObjFieldWide(panel, "base.obj_base_version", _objBaseVersion, row);
        row++;

        _objOriginalBaseVersion = new InvariantNumericTextBox { Width = 55, Anchor = AnchorStyles.Left };
        _objOriginalBaseVersion.NumericValueChanged += OnBaseFieldNumericChanged;
        AddObjFieldWide(panel, "base.obj_original_base_version", _objOriginalBaseVersion, row);
        row++;

        _objGalacticAddress = new TextBox { Dock = DockStyle.Fill };
        _objGalacticAddress.Leave += OnBaseFieldTextChanged;
        AddObjFieldHalf(panel, "base.obj_galactic_address", _objGalacticAddress, row);
        panel.SetColumnSpan(_objGalacticAddress, 2);
        row++;

        _objPositionX = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objPositionX.NumericValueChanged += OnBaseFieldNumericChanged;
        _objPositionY = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objPositionY.NumericValueChanged += OnBaseFieldNumericChanged;
        _objPositionZ = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objPositionZ.NumericValueChanged += OnBaseFieldNumericChanged;
        AddObjFieldVector(panel, "base.obj_position",
            "X", _objPositionX, "Y", _objPositionY, "Z", _objPositionZ, row);
        row++;

        _objForwardX = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objForwardX.NumericValueChanged += OnBaseFieldNumericChanged;
        _objForwardY = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objForwardY.NumericValueChanged += OnBaseFieldNumericChanged;
        _objForwardZ = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objForwardZ.NumericValueChanged += OnBaseFieldNumericChanged;
        AddObjFieldVector(panel, "base.obj_forward",
            "X", _objForwardX, "Y", _objForwardY, "Z", _objForwardZ, row);
        row++;

        _objUserData = new InvariantNumericTextBox { Width = 195, Anchor = AnchorStyles.Left };
        _objUserData.NumericValueChanged += OnBaseFieldNumericChanged;
        AddObjFieldWide(panel, "base.obj_user_data", _objUserData, row);
        row++;

        _objRID = new TextBox { Width = 195, Anchor = AnchorStyles.Left };
        _objRID.Leave += OnBaseFieldTextChanged;
        AddObjFieldWide(panel, "base.obj_rid", _objRID, row);
        row++;
        row++;

        _objLastUpdateTimestamp = new InvariantNumericTextBox { Width = 165, Anchor = AnchorStyles.Left };
        _objLastUpdateTimestamp.NumericValueChanged += OnBaseFieldNumericChanged;
        AddObjFieldWide(panel, "base.obj_last_update_timestamp", _objLastUpdateTimestamp, row);
        row++;

        _objAutoPower = new TextBox { Width = 195, Anchor = AnchorStyles.Left };
        _objAutoPower.Leave += OnBaseFieldTextChanged;
        AddObjFieldWide(panel, "base.obj_auto_power", _objAutoPower, row);
        row++;

        _objScreenshotAtX = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objScreenshotAtX.NumericValueChanged += OnBaseFieldNumericChanged;
        _objScreenshotAtY = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objScreenshotAtY.NumericValueChanged += OnBaseFieldNumericChanged;
        _objScreenshotAtZ = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objScreenshotAtZ.NumericValueChanged += OnBaseFieldNumericChanged;
        AddObjFieldVector(panel, "base.obj_screenshot_at",
            "X", _objScreenshotAtX, "Y", _objScreenshotAtY, "Z", _objScreenshotAtZ, row);
        row++;

        _objScreenshotPosX = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objScreenshotPosX.NumericValueChanged += OnBaseFieldNumericChanged;
        _objScreenshotPosY = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objScreenshotPosY.NumericValueChanged += OnBaseFieldNumericChanged;
        _objScreenshotPosZ = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objScreenshotPosZ.NumericValueChanged += OnBaseFieldNumericChanged;
        AddObjFieldVector(panel, "base.obj_screenshot_pos",
            "X", _objScreenshotPosX, "Y", _objScreenshotPosY, "Z", _objScreenshotPosZ, row);
        row++;

        _objIsReported = new CheckBox { AutoSize = true };
        _objIsReported.CheckedChanged += OnBaseFieldCheckedChanged;
        AddObjFieldPaired(panel, "base.obj_is_reported", _objIsReported,
            "base.obj_is_featured", _objIsFeatured = new CheckBox { AutoSize = true }, row);
        _objIsFeatured.CheckedChanged += OnBaseFieldCheckedChanged;
        row++;

        var readOnlySep = new Label { AutoSize = false, Height = 8 };
        panel.Controls.Add(readOnlySep, 0, row);
        panel.SetColumnSpan(readOnlySep, 7);
        row++;

        AddObjFieldPaired(panel, "base.obj_base_type",
            _objBaseType = new TextBox { Dock = DockStyle.Fill, Enabled = false },
            "base.obj_game_mode",
            _objGameMode = new TextBox { Dock = DockStyle.Fill, Enabled = false }, row);
        row++;

        AddObjFieldPaired(panel, "base.obj_difficulty",
            _objDifficulty = new TextBox { Dock = DockStyle.Fill, Enabled = false },
            "base.obj_platform_token",
            _objPlatformToken = new TextBox { Dock = DockStyle.Fill, Enabled = false }, row);
        row++;

        AddObjFieldPaired(panel, "base.obj_owner_lid",
            _objOwnerLID = new TextBox { Dock = DockStyle.Fill, Enabled = false },
            "base.obj_owner_uid",
            _objOwnerUID = new TextBox { Dock = DockStyle.Fill, Enabled = false }, row);
        row++;

        AddObjFieldPaired(panel, "base.obj_owner_usn",
            _objOwnerUSN = new TextBox { Dock = DockStyle.Fill, Enabled = false },
            "base.obj_owner_ptk",
            _objOwnerPTK = new TextBox { Dock = DockStyle.Fill, Enabled = false }, row);
        row++;

        AddObjFieldPaired(panel, "base.obj_owner_ts",
            _objOwnerTS = new TextBox { Dock = DockStyle.Fill, Enabled = false },
            "base.obj_last_edited_by_id",
            _objLastEditedById = new TextBox { Dock = DockStyle.Fill, Enabled = false }, row);
        row++;

        _objLastEditedByUsername = new TextBox { Dock = DockStyle.Fill, Enabled = false };
        AddObjFieldWide(panel, "base.obj_last_edited_by_username", _objLastEditedByUsername, row);

        return panel;
    }

    private TableLayoutPanel BuildObjectSplitPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 71));

        var listPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2
        };
        listPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        listPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        listPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        listPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _objectSearchBox = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = UiStrings.Get("base.objects_filter")
        };
        _objectSearchBox.TextChanged += OnObjectSearchChanged;
        listPanel.Controls.Add(_objectSearchBox, 0, 0);

        _objectSearchClearBtn = new Button
        {
            Text = "\u2715",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        _objectSearchClearBtn.Click += (_, _) => _objectSearchBox.Text = "";
        listPanel.Controls.Add(_objectSearchClearBtn, 1, 0);

        _objectList = new ListBox
        {
            Dock = DockStyle.Fill,
            SelectionMode = SelectionMode.One,
            IntegralHeight = false
        };
        _objectList.SelectedIndexChanged += OnObjectSelected;
        listPanel.Controls.Add(_objectList, 0, 1);
        listPanel.SetColumnSpan(_objectList, 2);

        panel.Controls.Add(listPanel, 0, 0);

        var detailPanel = BuildObjectDetailPanel();
        panel.Controls.Add(detailPanel, 1, 0);

        return panel;
    }

    private TableLayoutPanel BuildObjectDetailPanel()
    {
        // 7 columns: label | "X" | xField | "Y" | yField | "Z" | zField
        // Scalar fields span columns 1-6 as a single value.
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 7,
            RowCount = 7
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // col 0: label
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // col 1: "X"
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));    // col 2: xField
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // col 3: "Y"
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));    // col 4: yField
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // col 5: "Z"
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));    // col 6: zField
        for (int i = 0; i < 6; i++)
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        int row = 0;

        _objectDetailTimestamp = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objectDetailTimestamp.NumericValueChanged += OnObjectFieldNumericChanged;
        AddDetailFieldWide(panel, "base.obj_detail_timestamp", _objectDetailTimestamp, row);
        row++;

        _objectDetailObjectID = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objectDetailObjectID.Leave += OnObjectFieldTextChanged;
        AddDetailFieldWide(panel, "base.obj_detail_object_id", _objectDetailObjectID, row);
        row++;

        _objectDetailUserData = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objectDetailUserData.NumericValueChanged += OnObjectFieldNumericChanged;
        AddDetailFieldWide(panel, "base.obj_detail_user_data", _objectDetailUserData, row);
        row++;

        _objectDetailPositionX = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objectDetailPositionX.NumericValueChanged += OnObjectFieldNumericChanged;
        _objectDetailPositionY = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objectDetailPositionY.NumericValueChanged += OnObjectFieldNumericChanged;
        _objectDetailPositionZ = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objectDetailPositionZ.NumericValueChanged += OnObjectFieldNumericChanged;
        AddDetailVectorRow(panel, "base.obj_detail_position",
            "X", _objectDetailPositionX, "Y", _objectDetailPositionY, "Z", _objectDetailPositionZ, row);
        row++;

        _objectDetailUpX = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objectDetailUpX.NumericValueChanged += OnObjectFieldNumericChanged;
        _objectDetailUpY = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objectDetailUpY.NumericValueChanged += OnObjectFieldNumericChanged;
        _objectDetailUpZ = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objectDetailUpZ.NumericValueChanged += OnObjectFieldNumericChanged;
        AddDetailVectorRow(panel, "base.obj_detail_up",
            "X", _objectDetailUpX, "Y", _objectDetailUpY, "Z", _objectDetailUpZ, row);
        row++;

        _objectDetailAtX = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objectDetailAtX.NumericValueChanged += OnObjectFieldNumericChanged;
        _objectDetailAtY = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objectDetailAtY.NumericValueChanged += OnObjectFieldNumericChanged;
        _objectDetailAtZ = new InvariantNumericTextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _objectDetailAtZ.NumericValueChanged += OnObjectFieldNumericChanged;
        AddDetailVectorRow(panel, "base.obj_detail_at",
            "X", _objectDetailAtX, "Y", _objectDetailAtY, "Z", _objectDetailAtZ, row);

        return panel;
    }

    private void AddDetailFieldWide(TableLayoutPanel panel, string locKey, Control field, int row)
    {
        var label = new Label
        {
            Text = UiStrings.Get(locKey),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 3, 10, 0)
        };
        _objectDetailLabels.Add((locKey, label));
        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(field, 1, row);
        panel.SetColumnSpan(field, 6);
    }

    private void AddDetailVectorRow(TableLayoutPanel panel, string locKey,
        string xLabel, InvariantNumericTextBox xField,
        string yLabel, InvariantNumericTextBox yField,
        string zLabel, InvariantNumericTextBox zField, int row)
    {
        var label = new Label
        {
            Text = UiStrings.Get(locKey),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 3, 10, 0)
        };
        _objectDetailLabels.Add((locKey, label));
        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(new Label { Text = xLabel, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 3, 4, 0) }, 1, row);
        panel.Controls.Add(xField, 2, row);
        panel.Controls.Add(new Label { Text = yLabel, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(4, 3, 4, 0) }, 3, row);
        panel.Controls.Add(yField, 4, row);
        panel.Controls.Add(new Label { Text = zLabel, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(4, 3, 4, 0) }, 5, row);
        panel.Controls.Add(zField, 6, row);
    }

    private void AddObjField(TableLayoutPanel panel, string locKey, Control field, int row, int labelCol, int valueCol)
    {
        var label = new Label
        {
            Text = UiStrings.Get(locKey),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 5, 10, 0)
        };
        _objectsLabels.Add((locKey, label));
        panel.Controls.Add(label, labelCol, row);
        panel.Controls.Add(field, valueCol, row);
    }

    private void AddObjFieldWide(TableLayoutPanel panel, string locKey, Control field, int row)
    {
        var label = new Label
        {
            Text = UiStrings.Get(locKey),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 5, 10, 0)
        };
        _objectsLabels.Add((locKey, label));
        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(field, 1, row);
        panel.SetColumnSpan(field, 6);
    }

    private void AddObjFieldHalf(TableLayoutPanel panel, string locKey, Control field, int row)
    {
        var label = new Label
        {
            Text = UiStrings.Get(locKey),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 5, 10, 0)
        };
        _objectsLabels.Add((locKey, label));
        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(field, 1, row);
        panel.SetColumnSpan(field, 3);
    }

    private void AddObjFieldVector(TableLayoutPanel panel, string locKey,
        string xLabel, Control xField,
        string yLabel, Control yField,
        string zLabel, Control zField, int row)
    {
        var label = new Label
        {
            Text = UiStrings.Get(locKey),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 5, 10, 0)
        };
        _objectsLabels.Add((locKey, label));
        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(new Label { Text = xLabel, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 4, 0) }, 1, row);
        panel.Controls.Add(xField, 2, row);
        panel.Controls.Add(new Label { Text = yLabel, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(4, 5, 4, 0) }, 3, row);
        panel.Controls.Add(yField, 4, row);
        panel.Controls.Add(new Label { Text = zLabel, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(4, 5, 4, 0) }, 5, row);
        panel.Controls.Add(zField, 6, row);
    }

    private void AddObjFieldPaired(TableLayoutPanel panel,
        string locKey1, Control field1,
        string locKey2, Control field2, int row)
    {
        var label1 = new Label
        {
            Text = UiStrings.Get(locKey1),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 5, 10, 0)
        };
        _objectsLabels.Add((locKey1, label1));
        panel.Controls.Add(label1, 0, row);
        var wrap1 = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, Margin = Padding.Empty, Padding = Padding.Empty };
        wrap1.Controls.Add(field1);
        panel.Controls.Add(wrap1, 1, row);
        panel.SetColumnSpan(wrap1, 3);

        var label2 = new Label
        {
            Text = UiStrings.Get(locKey2),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 5, 10, 0)
        };
        _objectsLabels.Add((locKey2, label2));
        panel.Controls.Add(label2, 4, row);
        var wrap2 = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, Margin = Padding.Empty, Padding = Padding.Empty };
        wrap2.Controls.Add(field2);
        panel.Controls.Add(wrap2, 5, row);
        panel.SetColumnSpan(wrap2, 2);
    }


    private void LoadObjectFields()
    {
        if (_baseList.SelectedItem is not BaseInfoItem item)
        {
            ClearObjectFields();
            return;
        }

        bool wasLoading = _loading;
        var baseObj = item.Data;
        _loading = true;
        try
        {
            _objBaseVersion.NumericValue = GetDoubleSafe(baseObj, "BaseVersion");
            _objOriginalBaseVersion.NumericValue = GetDoubleSafe(baseObj, "OriginalBaseVersion");
            _objGalacticAddress.Text = GetGalacticAddressText(baseObj);
            LoadVector3(baseObj, "Position", _objPositionX, _objPositionY, _objPositionZ);
            LoadVector3(baseObj, "Forward", _objForwardX, _objForwardY, _objForwardZ);
            _objUserData.NumericValue = GetDoubleSafe(baseObj, "UserData");
            _objLastUpdateTimestamp.NumericValue = GetDoubleSafe(baseObj, "LastUpdateTimestamp");
            _objRID.Text = baseObj.GetString("RID") ?? "";
            LoadVector3(baseObj, "ScreenshotAt", _objScreenshotAtX, _objScreenshotAtY, _objScreenshotAtZ);
            LoadVector3(baseObj, "ScreenshotPos", _objScreenshotPosX, _objScreenshotPosY, _objScreenshotPosZ);
            _objIsReported.Checked = baseObj.GetBool("IsReported");
            _objIsFeatured.Checked = baseObj.GetBool("IsFeatured");
            _objAutoPower.Text = baseObj.GetString("AutoPowerSetting.BaseAutoPowerSetting") ?? "";

            _objBaseType.Text = baseObj.GetString("BaseType.PersistentBaseTypes") ?? "";
            _objGameMode.Text = baseObj.GetString("GameMode.PresetGameMode") ?? "";
            _objDifficulty.Text = baseObj.GetString("Difficulty.DifficultyPreset.DifficultyPresetType") ?? "";
            _objPlatformToken.Text = baseObj.GetString("PlatformToken") ?? "";
            _objOwnerLID.Text = baseObj.GetString("Owner.LID") ?? "";
            _objOwnerUID.Text = baseObj.GetString("Owner.UID") ?? "";
            _objOwnerUSN.Text = baseObj.GetString("Owner.USN") ?? "";
            _objOwnerPTK.Text = baseObj.GetString("Owner.PTK") ?? "";
            _objOwnerTS.Text = GetDoubleSafe(baseObj, "Owner.TS")?.ToString(CultureInfo.InvariantCulture) ?? "";
            _objLastEditedById.Text = baseObj.GetString("LastEditedById") ?? "";
            _objLastEditedByUsername.Text = baseObj.GetString("LastEditedByUsername") ?? "";

            LoadObjectList(baseObj);
        }
        finally
        {
            _loading = wasLoading;
        }
    }

    private void ClearObjectFields()
    {
        bool wasLoading = _loading;
        _loading = true;
        try
        {
            _objBaseVersion.NumericValue = null;
            _objOriginalBaseVersion.NumericValue = null;
            _objGalacticAddress.Text = "";
            _objPositionX.NumericValue = null;
            _objPositionY.NumericValue = null;
            _objPositionZ.NumericValue = null;
            _objForwardX.NumericValue = null;
            _objForwardY.NumericValue = null;
            _objForwardZ.NumericValue = null;
            _objUserData.NumericValue = null;
            _objLastUpdateTimestamp.NumericValue = null;
            _objRID.Text = "";
            _objScreenshotAtX.NumericValue = null;
            _objScreenshotAtY.NumericValue = null;
            _objScreenshotAtZ.NumericValue = null;
            _objScreenshotPosX.NumericValue = null;
            _objScreenshotPosY.NumericValue = null;
            _objScreenshotPosZ.NumericValue = null;
            _objIsReported.Checked = false;
            _objIsFeatured.Checked = false;
            _objAutoPower.Text = "";
            _objBaseType.Text = "";
            _objGameMode.Text = "";
            _objDifficulty.Text = "";
            _objPlatformToken.Text = "";
            _objOwnerLID.Text = "";
            _objOwnerUID.Text = "";
            _objOwnerUSN.Text = "";
            _objOwnerPTK.Text = "";
            _objOwnerTS.Text = "";
            _objLastEditedById.Text = "";
            _objLastEditedByUsername.Text = "";
            _objectList.Items.Clear();
            ClearObjectDetail();
        }
        finally
        {
            _loading = wasLoading;
        }
    }

    private void LoadObjectList(JsonObject baseObj)
    {
        _objectList.Items.Clear();
        var objects = baseObj.GetArray("Objects");
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            try
            {
                var obj = objects.GetObject(i);
                string objectId = obj.GetString("ObjectID") ?? "";
                var item = new BaseObjectItem(objectId, obj, i);
                _objectList.Items.Add(item);
            }
            catch { }
        }
    }

    private void LoadObjectDetail(JsonObject obj)
    {
        bool wasLoading = _loading;
        _loading = true;
        try
        {
            _objectDetailTimestamp.NumericValue = GetDoubleSafe(obj, "Timestamp");
            _objectDetailObjectID.Text = obj.GetString("ObjectID") ?? "";
            _objectDetailUserData.NumericValue = GetDoubleSafe(obj, "UserData");
            LoadVector3(obj, "Position", _objectDetailPositionX, _objectDetailPositionY, _objectDetailPositionZ);
            LoadVector3(obj, "Up", _objectDetailUpX, _objectDetailUpY, _objectDetailUpZ);
            LoadVector3(obj, "At", _objectDetailAtX, _objectDetailAtY, _objectDetailAtZ);
        }
        finally
        {
            _loading = wasLoading;
        }
    }

    private void ClearObjectDetail()
    {
        bool wasLoading = _loading;
        _loading = true;
        try
        {
            _objectDetailTimestamp.NumericValue = null;
            _objectDetailObjectID.Text = "";
            _objectDetailUserData.NumericValue = null;
            _objectDetailPositionX.NumericValue = null;
            _objectDetailPositionY.NumericValue = null;
            _objectDetailPositionZ.NumericValue = null;
            _objectDetailUpX.NumericValue = null;
            _objectDetailUpY.NumericValue = null;
            _objectDetailUpZ.NumericValue = null;
            _objectDetailAtX.NumericValue = null;
            _objectDetailAtY.NumericValue = null;
            _objectDetailAtZ.NumericValue = null;
        }
        finally
        {
            _loading = wasLoading;
        }
    }

    private void OnObjectSelected(object? sender, EventArgs e)
    {
        if (_objectList.SelectedItem is BaseObjectItem item)
            LoadObjectDetail(item.Data);
        else
            ClearObjectDetail();
    }

    private void OnObjectSearchChanged(object? sender, EventArgs e)
    {
        string filter = _objectSearchBox.Text.Trim();
        _objectList.SelectedIndexChanged -= OnObjectSelected;
        _objectList.BeginUpdate();
        var allItems = new List<BaseObjectItem>();
        for (int i = 0; i < _objectList.Items.Count; i++)
        {
            if (_objectList.Items[i] is BaseObjectItem objItem)
                allItems.Add(objItem);
        }

        _objectList.Items.Clear();
        foreach (var item in allItems)
        {
            if (string.IsNullOrEmpty(filter) ||
                item.ObjectId.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                _objectList.Items.Add(item);
            }
        }
        _objectList.EndUpdate();
        _objectList.SelectedIndexChanged += OnObjectSelected;
        ClearObjectDetail();
    }

    private void OnBaseFieldNumericChanged(object? sender, EventArgs e)
    {
        if (_loading) return;
        if (_baseList.SelectedItem is not BaseInfoItem item) return;
        if (sender is not InvariantNumericTextBox nud) return;

        var baseObj = item.Data;

        if (nud == _objBaseVersion && nud.NumericValue is double bv)
            baseObj.Set("BaseVersion", bv);
        else if (nud == _objOriginalBaseVersion && nud.NumericValue is double obv)
            baseObj.Set("OriginalBaseVersion", obv);
        else if (nud == _objPositionX && nud.NumericValue is double px)
            SetVectorComponent(baseObj, "Position", 0, px);
        else if (nud == _objPositionY && nud.NumericValue is double py)
            SetVectorComponent(baseObj, "Position", 1, py);
        else if (nud == _objPositionZ && nud.NumericValue is double pz)
            SetVectorComponent(baseObj, "Position", 2, pz);
        else if (nud == _objForwardX && nud.NumericValue is double fx)
            SetVectorComponent(baseObj, "Forward", 0, fx);
        else if (nud == _objForwardY && nud.NumericValue is double fy)
            SetVectorComponent(baseObj, "Forward", 1, fy);
        else if (nud == _objForwardZ && nud.NumericValue is double fz)
            SetVectorComponent(baseObj, "Forward", 2, fz);
        else if (nud == _objUserData && nud.NumericValue is double ud)
            baseObj.Set("UserData", ud);
        else if (nud == _objLastUpdateTimestamp && nud.NumericValue is double ts)
            baseObj.Set("LastUpdateTimestamp", ts);
        else if (nud == _objScreenshotAtX && nud.NumericValue is double sax)
            SetVectorComponent(baseObj, "ScreenshotAt", 0, sax);
        else if (nud == _objScreenshotAtY && nud.NumericValue is double say)
            SetVectorComponent(baseObj, "ScreenshotAt", 1, say);
        else if (nud == _objScreenshotAtZ && nud.NumericValue is double saz)
            SetVectorComponent(baseObj, "ScreenshotAt", 2, saz);
        else if (nud == _objScreenshotPosX && nud.NumericValue is double spx)
            SetVectorComponent(baseObj, "ScreenshotPos", 0, spx);
        else if (nud == _objScreenshotPosY && nud.NumericValue is double spy)
            SetVectorComponent(baseObj, "ScreenshotPos", 1, spy);
        else if (nud == _objScreenshotPosZ && nud.NumericValue is double spz)
            SetVectorComponent(baseObj, "ScreenshotPos", 2, spz);

        DataModified?.Invoke(this, EventArgs.Empty);
    }

    private void OnBaseFieldTextChanged(object? sender, EventArgs e)
    {
        if (_loading) return;
        if (_baseList.SelectedItem is not BaseInfoItem item) return;
        if (sender == null) return;

        var baseObj = item.Data;

        if (sender == _objGalacticAddress)
            baseObj.Set("GalacticAddress", _objGalacticAddress.Text);
        else if (sender == _objRID)
            baseObj.Set("RID", _objRID.Text);
        else if (sender == _objAutoPower)
        {
            var autoPower = baseObj.GetObject("AutoPowerSetting");
            if (autoPower != null)
                autoPower.Set("BaseAutoPowerSetting", _objAutoPower.Text);
            else
                baseObj.Set("AutoPowerSetting.BaseAutoPowerSetting", _objAutoPower.Text);
        }

        DataModified?.Invoke(this, EventArgs.Empty);
    }

    private void OnBaseFieldCheckedChanged(object? sender, EventArgs e)
    {
        if (_loading) return;
        if (_baseList.SelectedItem is not BaseInfoItem item) return;

        var baseObj = item.Data;

        if (sender == _objIsReported)
            baseObj.Set("IsReported", _objIsReported.Checked);
        else if (sender == _objIsFeatured)
            baseObj.Set("IsFeatured", _objIsFeatured.Checked);

        DataModified?.Invoke(this, EventArgs.Empty);
    }

    private void OnObjectFieldNumericChanged(object? sender, EventArgs e)
    {
        if (_loading) return;
        if (_objectList.SelectedItem is not BaseObjectItem item) return;
        if (sender is not InvariantNumericTextBox nud) return;

        var obj = item.Data;

        if (nud == _objectDetailTimestamp && nud.NumericValue is double val)
            obj.Set("Timestamp", val);
        else if (nud == _objectDetailUserData && nud.NumericValue is double ud)
            obj.Set("UserData", ud);
        else if (nud == _objectDetailPositionX && nud.NumericValue is double px)
            SetVectorComponent(obj, "Position", 0, px);
        else if (nud == _objectDetailPositionY && nud.NumericValue is double py)
            SetVectorComponent(obj, "Position", 1, py);
        else if (nud == _objectDetailPositionZ && nud.NumericValue is double pz)
            SetVectorComponent(obj, "Position", 2, pz);
        else if (nud == _objectDetailUpX && nud.NumericValue is double ux)
            SetVectorComponent(obj, "Up", 0, ux);
        else if (nud == _objectDetailUpY && nud.NumericValue is double uy)
            SetVectorComponent(obj, "Up", 1, uy);
        else if (nud == _objectDetailUpZ && nud.NumericValue is double uz)
            SetVectorComponent(obj, "Up", 2, uz);
        else if (nud == _objectDetailAtX && nud.NumericValue is double ax)
            SetVectorComponent(obj, "At", 0, ax);
        else if (nud == _objectDetailAtY && nud.NumericValue is double ay)
            SetVectorComponent(obj, "At", 1, ay);
        else if (nud == _objectDetailAtZ && nud.NumericValue is double az)
            SetVectorComponent(obj, "At", 2, az);

        DataModified?.Invoke(this, EventArgs.Empty);
    }

    private void OnObjectFieldTextChanged(object? sender, EventArgs e)
    {
        if (_loading) return;
        if (_objectList.SelectedItem is not BaseObjectItem item) return;
        if (sender == _objectDetailObjectID)
            item.Data.Set("ObjectID", _objectDetailObjectID.Text);

        DataModified?.Invoke(this, EventArgs.Empty);
    }

    private static double? GetDoubleSafe(JsonObject obj, string key)
    {
        try { return obj.GetDouble(key); }
        catch { return null; }
    }

    private static string GetGalacticAddressText(JsonObject obj)
    {
        try
        {
            var value = obj.Get("GalacticAddress");
            if (value == null) return "";
            return CoordinateHelper.NormalizeGalacticAddress(value);
        }
        catch { return ""; }
    }

    private static void LoadVector3(JsonObject obj, string key,
        InvariantNumericTextBox x, InvariantNumericTextBox y, InvariantNumericTextBox z)
    {
        try
        {
            var arr = obj.GetArray(key);
            if (arr != null && arr.Length >= 3)
            {
                x.NumericValue = arr.GetDouble(0);
                y.NumericValue = arr.GetDouble(1);
                z.NumericValue = arr.GetDouble(2);
                return;
            }
        }
        catch { }
        x.NumericValue = null;
        y.NumericValue = null;
        z.NumericValue = null;
    }

    private static void SetVectorComponent(JsonObject obj, string key, int index, double value)
    {
        var arr = obj.GetArray(key);
        if (arr == null)
        {
            arr = new JsonArray();
            for (int i = 0; i <= index; i++)
                arr.Add(0.0);
            obj.Set(key, arr);
        }
        while (arr.Length <= index)
            arr.Add(0.0);
        arr.Set(index, value);
    }

    private static Label AddRow(TableLayoutPanel layout, string label, Control field, int row)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 10, 0) };
        layout.Controls.Add(lbl, 0, row);
        layout.Controls.Add(field, 1, row);
        return lbl;
    }

    public void ApplyUiLocalisation()
    {
        _npcTitle.Text = UiStrings.Get("base.npc_header");
        _baseTitle.Text = UiStrings.Get("base.base_header");
        _freighterRoomsTitle.Text = UiStrings.Get("base.freighter_rooms_header");
        _baseListTitle.Text = UiStrings.Get("base.base_list_title");
        if (_npcLabel != null) _npcLabel.Text = UiStrings.Get("base.npc_label");
        if (_raceLabel != null) _raceLabel.Text = UiStrings.Get("base.npc_race_label");
        if (_seedLabel != null) _seedLabel.Text = UiStrings.Get("base.npc_seed_label");
        if (_nameLabel != null) _nameLabel.Text = UiStrings.Get("base.name_label");
        if (_itemsLabel != null) _itemsLabel.Text = UiStrings.Get("base.items_label");
        _generateNpcSeedBtn.Text = UiStrings.Get("common.generate");
        _summonWorkerBtn.Text = UiStrings.Get("base.summon_npc");
        _exportBtn.Text = UiStrings.Get("base.export");
        _importBtn.Text = UiStrings.Get("base.import");
        _moveBaseComputerBtn.Text = UiStrings.Get("base.move_basecomp");
        _deleteBaseBtn.Text = UiStrings.Get("base.delete_base");
        _sortAlphaAscBtn.Text = UiStrings.Get("base.sort_az");
        _sortAlphaDescBtn.Text = UiStrings.Get("base.sort_za");
        _clearTerrainEditsBtn.Text = UiStrings.Get("base.clear_terrain");
        _clearAllTerrainEditsBtn.Text = UiStrings.Get("base.clear_all_terrain");
        _clearAllTerrainExceptBasesBtn.Text = UiStrings.Get("base.clear_all_terrain_except_bases");
        _moveUpBtn.Text = UiStrings.Get("base.move_base_up");
        _moveDownBtn.Text = UiStrings.Get("base.move_base_down");
        _toTopBtn.Text = UiStrings.Get("base.move_base_to_top");
        _toBottomBtn.Text = UiStrings.Get("base.move_base_to_bottom");
        _moveOrderLabel.Text = UiStrings.Get("base.move_base_in_list");

        new ToolTip().SetToolTip(_gotoBasesListBtn, UiStrings.Format("goto_json.tooltip_section", _baseListTitle.Text));
        new ToolTip().SetToolTip(_gotoNpcWorkersBtn, UiStrings.Format("goto_json.tooltip_section", UiStrings.Get("goto_json.nav_npc_workers")));


        // Refresh NPC race combo with localised display names
        RefreshNpcRaceCombo();

        // Refresh NPC worker display names (ComboBox re-reads ToString() on Refresh)
        if (_npcSelector.Items.Count > 0)
        {
            var selIdx = _npcSelector.SelectedIndex;
            _npcSelector.BeginUpdate();
            // Force combo to refresh display text by re-reading ToString()
            var items = _npcSelector.Items.Cast<object>().ToArray();
            _npcSelector.Items.Clear();
            _npcSelector.Items.AddRange(items);
            if (selIdx >= 0 && selIdx < _npcSelector.Items.Count)
                _npcSelector.SelectedIndex = selIdx;
            _npcSelector.EndUpdate();
        }

        foreach (var (key, label) in _objectsLabels)
            label.Text = UiStrings.Get(key);
        foreach (var (key, label) in _objectDetailLabels)
            label.Text = UiStrings.Get(key);
        _objectSearchBox.PlaceholderText = UiStrings.Get("base.objects_filter");
        _infoTab.Text = UiStrings.Get("base.info_tab");
        _objectsTab.Text = UiStrings.Get("base.objects_tab");
    }

    /// <summary>
    /// Moves the selected player base one position up (toward lower array index),
    /// swapping it with the nearest preceding HomePlanetBase in the list.
    /// Other base types (ship bases, corvettes, etc.) are not disturbed.
    /// </summary>
    private void OnMoveBaseUp(object? sender, EventArgs e)
    {
        if (_baseList.SelectedItem is not BaseInfoItem selected) return;
        if (_playerState == null) return;

        var sorted = _baseInfoItems.OrderBy(x => x.DataIndex).ToList();
        int pos = sorted.IndexOf(selected);
        if (pos <= 0) return;

        var previous = sorted[pos - 1];
        var bases = _playerState.GetArray("PersistentPlayerBases");
        if (bases == null) return;

        BaseLogic.SwapPlayerBases(bases, selected.DataIndex, previous.DataIndex);

        int oldSelected = selected.DataIndex;
        selected.DataIndex = previous.DataIndex;
        previous.DataIndex = oldSelected;

        RefreshBaseList(selected);
        DataModified?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Moves the selected player base one position down (toward higher array index),
    /// swapping it with the nearest following HomePlanetBase in the list.
    /// Other base types (ship bases, corvettes, etc.) are not disturbed.
    /// </summary>
    private void OnMoveBaseDown(object? sender, EventArgs e)
    {
        if (_baseList.SelectedItem is not BaseInfoItem selected) return;
        if (_playerState == null) return;

        var sorted = _baseInfoItems.OrderBy(x => x.DataIndex).ToList();
        int pos = sorted.IndexOf(selected);
        if (pos >= sorted.Count - 1) return;

        var next = sorted[pos + 1];
        var bases = _playerState.GetArray("PersistentPlayerBases");
        if (bases == null) return;

        BaseLogic.SwapPlayerBases(bases, selected.DataIndex, next.DataIndex);

        int oldSelected = selected.DataIndex;
        selected.DataIndex = next.DataIndex;
        next.DataIndex = oldSelected;

        RefreshBaseList(selected);
        DataModified?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Moves the selected player base to the topmost array slot among all HomePlanetBases.
    /// </summary>
    private void OnMoveBaseToTop(object? sender, EventArgs e)
    {
        if (_baseList.SelectedItem is not BaseInfoItem selected) return;
        if (_playerState == null) return;

        var sorted = _baseInfoItems.OrderBy(x => x.DataIndex).ToList();
        int pos = sorted.IndexOf(selected);
        if (pos <= 0) return;

        var bases = _playerState.GetArray("PersistentPlayerBases");
        if (bases == null) return;

        while (pos > 0)
        {
            var previous = sorted[pos - 1];
            BaseLogic.SwapPlayerBases(bases, selected.DataIndex, previous.DataIndex);
            int oldIdx = selected.DataIndex;
            selected.DataIndex = previous.DataIndex;
            previous.DataIndex = oldIdx;
            sorted.RemoveAt(pos);
            sorted.Insert(pos - 1, selected);
            pos--;
        }

        RefreshBaseList(selected);
        DataModified?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Moves the selected player base to the bottommost array slot among all HomePlanetBases.
    /// </summary>
    private void OnMoveBaseToBottom(object? sender, EventArgs e)
    {
        if (_baseList.SelectedItem is not BaseInfoItem selected) return;
        if (_playerState == null) return;

        var sorted = _baseInfoItems.OrderBy(x => x.DataIndex).ToList();
        int pos = sorted.IndexOf(selected);
        if (pos >= sorted.Count - 1) return;

        var bases = _playerState.GetArray("PersistentPlayerBases");
        if (bases == null) return;

        while (pos < sorted.Count - 1)
        {
            var next = sorted[pos + 1];
            BaseLogic.SwapPlayerBases(bases, selected.DataIndex, next.DataIndex);
            int oldIdx = selected.DataIndex;
            selected.DataIndex = next.DataIndex;
            next.DataIndex = oldIdx;
            sorted.RemoveAt(pos);
            sorted.Insert(pos + 1, selected);
            pos++;
        }

        RefreshBaseList(selected);
        DataModified?.Invoke(this, EventArgs.Empty);
    }

    private void OnDeleteBase(object? sender, EventArgs e)
    {
        if (_baseList.SelectedItem is not BaseInfoItem selected) return;
        if (_playerState == null) return;

        string confirmKey = selected.IsFreighterBase
            ? "base.delete_freighter_base_confirm"
            : "base.delete_base_confirm";
        var result = MessageBox.Show(this,
            UiStrings.Format(confirmKey, selected.DisplayName),
            UiStrings.Get("base.delete_base_title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        try
        {
            var bases = _playerState.GetArray("PersistentPlayerBases");
            if (bases == null) return;

            bases.RemoveAt(selected.DataIndex);
            _baseInfoItems.Remove(selected);

            // Update DataIndex for items after the removed one
            foreach (var item in _baseInfoItems)
            {
                if (item.DataIndex > selected.DataIndex)
                    item.DataIndex--;
            }

            _baseList.SelectedIndexChanged -= OnBaseSelected;
            _baseList.BeginUpdate();
            _baseList.Items.Remove(selected);
            _baseList.EndUpdate();
            _baseList.SelectedIndexChanged += OnBaseSelected;

            if (_baseInfoItems.Count > 0)
                _baseList.SelectedIndex = 0;

            DataModified?.Invoke(this, EventArgs.Empty);
            UpdateMoveButtonStates();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                UiStrings.Format("base.delete_base_failed", ex.Message),
                UiStrings.Get("common.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnSortAlphaAsc(object? sender, EventArgs e)
    {
        if (_playerState == null) return;
        SortAlphaList(true);
    }

    private void OnSortAlphaDesc(object? sender, EventArgs e)
    {
        if (_playerState == null) return;
        SortAlphaList(false);
    }

    private void SortAlphaList(bool ascending)
    {
        if (_baseInfoItems.Count < 2) return;

        var bases = _playerState?.GetArray("PersistentPlayerBases");
        if (bases == null) return;

        var sorted = ascending
            ? _baseInfoItems.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase).ToList()
            : _baseInfoItems.OrderByDescending(x => x.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();

        // Reorder the JSON array to match the sorted display list
        for (int i = 0; i < sorted.Count; i++)
        {
            var item = sorted[i];
            int currentIdx = item.DataIndex;
            if (currentIdx != i)
            {
                BaseLogic.SwapPlayerBases(bases, currentIdx, i);
                item.DataIndex = i;
                // Update other items whose indices shifted
                foreach (var other in _baseInfoItems)
                {
                    if (other != item && other.DataIndex == i)
                        other.DataIndex = currentIdx;
                }
            }
        }

        // Refresh the list and keep the same selected item
        var selected = _baseList.SelectedItem as BaseInfoItem;
        RefreshBaseList(selected);
        DataModified?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Repopulates the base list box, sorted by array index, and re-selects the given item.
    /// Also updates the enabled state of the reorder buttons.
    /// </summary>
    private void RefreshBaseList(BaseInfoItem? keepSelected = null)
    {
        _baseList.SelectedIndexChanged -= OnBaseSelected;
        _baseList.BeginUpdate();
        try
        {
            _baseList.Items.Clear();
            foreach (var item in _baseInfoItems.OrderBy(x => x.DataIndex))
                _baseList.Items.Add(item);

            if (keepSelected != null)
            {
                int idx = _baseList.Items.IndexOf(keepSelected);
                if (idx >= 0)
                    _baseList.SelectedIndex = idx;
            }
        }
        finally
        {
            _baseList.EndUpdate();
            _baseList.SelectedIndexChanged += OnBaseSelected;
            UpdateMoveButtonStates();
        }
    }

    /// <summary>
    /// Selects the base with the given PersistentPlayerBases array index in the list.
    /// Used by the Space Station tab's "Go to Base" button.
    /// </summary>
    internal void SelectBase(int dataIndex)
    {
        var item = _baseInfoItems.FirstOrDefault(x => x.DataIndex == dataIndex);
        if (item == null) return;

        int idx = _baseList.Items.IndexOf(item);
        if (idx >= 0)
            _baseList.SelectedIndex = idx;
    }

    private void UpdateMoveButtonStates()
    {
        if (_baseList.SelectedItem is not BaseInfoItem selected)
        {
            _toTopBtn.Enabled = false;
            _moveUpBtn.Enabled = false;
            _moveDownBtn.Enabled = false;
            _toBottomBtn.Enabled = false;
            return;
        }

        var sorted = _baseInfoItems.OrderBy(x => x.DataIndex).ToList();
        int pos = sorted.IndexOf(selected);
        _toTopBtn.Enabled = pos > 0;
        _moveUpBtn.Enabled = pos > 0;
        _moveDownBtn.Enabled = pos < sorted.Count - 1;
        _toBottomBtn.Enabled = pos < sorted.Count - 1;
        _sortAlphaAscBtn.Enabled = sorted.Count >= 2;
        _sortAlphaDescBtn.Enabled = sorted.Count >= 2;
    }

    private sealed class NpcWorkerItem
    {
        public JsonObject Data { get; }
        public int Index { get; }

        public NpcWorkerItem(JsonObject data, int index)
        {
            Data = data;
            Index = index;
        }

        public override string ToString() => NpcWorkerNames.Get(Index);
    }

    private sealed class BaseInfoItem
    {
        public string DisplayName { get; set; }
        public JsonObject Data { get; }
        public int ObjectCount { get; }
        public bool IsFreighterBase { get; }
        public bool IsStationBase { get; }
        public bool IsCorvetteBase { get; }
        public bool IsSpaceBase { get; }

        // Tracks the entry's position in the PersistentPlayerBases array.
        // Updated in tandem with SwapPlayerBases calls so the list always reflects
        // the true array position. Only modified by BasesSubPanel's reorder methods.
        private int _dataIndex;
        public int DataIndex
        {
            get => _dataIndex;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                _dataIndex = value;
            }
        }

        public BaseInfoItem(string displayName, JsonObject data, int dataIndex, int objectCount,
            bool isFreighterBase = false, bool isStationBase = false, bool isCorvetteBase = false,
            bool isSpaceBase = false)
        {
            DisplayName = displayName;
            Data = data;
            DataIndex = dataIndex;
            ObjectCount = objectCount;
            IsFreighterBase = isFreighterBase;
            IsStationBase = isStationBase;
            IsCorvetteBase = isCorvetteBase;
            IsSpaceBase = isSpaceBase;
        }

        // The display includes the raw array index (e.g. "[7] My Cool Base Name") so
        // players can identify a base's position in PersistentPlayerBases while reordering.
        // Freighter, space station, corvette and space bases are suffixed with [F], [S],
        // [C] and [A] respectively to distinguish them.
        public override string ToString()
        {
            string marker = IsFreighterBase ? "[F] "
                : IsStationBase ? "[S] "
                : IsCorvetteBase ? "[C] "
                : IsSpaceBase ? "[A] "
                : "";
            return $"[{DataIndex}] {marker}{DisplayName}";
        }
    }

    private sealed class BaseObjectItem
    {
        public string ObjectId { get; }
        public JsonObject Data { get; }
        public int Index { get; }

        public BaseObjectItem(string objectId, JsonObject data, int index)
        {
            ObjectId = objectId;
            Data = data;
            Index = index;
        }

        public override string ToString() => ObjectId;
    }
}

/// <summary>
/// Chests sub-panel: displays Chest 0-9 inventories
/// from PersistentPlayerBases, each in its own tab with an InventoryGridPanel.
/// Uses lazy loading: only the visible tab's grid is loaded immediately,
/// others are deferred until their tab is selected.
/// </summary>
internal class ChestsSubPanel : UserControl
{
    private readonly TabControl _storageTabs;
    private readonly InventoryGridPanel[] _chestGrids;
    private readonly Label[] _chestWarnings;
    private readonly TabPage[] _chestPages;

    // Chest name editing controls
    private readonly Label[] _chestNameLabels;
    private readonly TextBox[] _chestNameFields;
    private readonly Button[] _chestRenameButtons;
    private readonly Button[] _chestClearButtons;

    private GameItemDatabase? _database;
    private IconManager? _iconManager;

    // Deferred inventory data for lazy-loading
    private readonly JsonObject?[] _pendingInventories = new JsonObject?[10];
    private readonly bool[] _chestLoaded = new bool[10];

    private readonly Button[] _chestGotoBtns = new Button[10];

    // Tracks the current custom name per chest (empty = default)
    private readonly string[] _chestNames = new string[10];

    // "All Chests" tab: sorts items across all 10 chests as one pool
    private TabPage _allChestsPage = null!;
    private ComboBox _allChestsSortCombo = null!;
    private NumericUpDown _allChestsPaddingInput = null!;
    private CheckBox _allChestsMergeOnlyCheckbox = null!;
    private Button _allChestsSortButton = null!;
    private Label _allChestsStatusLabel = null!;
    private Label _allChestsSortLabel = null!;
    private Label _allChestsPaddingLabel = null!;
    private Label _allChestsInfoLabel = null!;

    private JsonObject? _playerState;

    /// <summary>Raised when the user requests navigation to a JSON path in the Raw JSON Editor.</summary>
    internal event EventHandler<GoToJsonEventArgs>? GoToJsonRequested;

    /// <summary>Raised when a bulk edit (e.g. Sort All Chests) directly modifies the underlying save data.</summary>
    internal event EventHandler? DataModified;

    public ChestsSubPanel()
    {
        DoubleBuffered = true;
        SuspendLayout();

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            Padding = new Padding(0),
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _storageTabs = new DoubleBufferedTabControl { Dock = DockStyle.Fill };
        _chestGrids = new InventoryGridPanel[10];
        _chestWarnings = new Label[10];
        _chestPages = new TabPage[10];
        _chestNameLabels = new Label[10];
        _chestNameFields = new TextBox[10];
        _chestRenameButtons = new Button[10];
        _chestClearButtons = new Button[10];

        // --- "All Chests" tab: cross-chest sort (must be added first so it lands at index 0) ---
        _allChestsInfoLabel = new Label
        {
            Text = UiStrings.Get("base.all_chests_info"),
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, 10),
        };
        _allChestsSortLabel = new Label
        {
            Text = UiStrings.Get("base.all_chests_sort_label"),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 6, 4, 0),
        };
        _allChestsSortCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 200,
            Anchor = AnchorStyles.Left,
        };
        _allChestsSortCombo.Items.Add(UiStrings.Get("base.all_chests_sort_name"));
        _allChestsSortCombo.Items.Add(UiStrings.Get("base.all_chests_sort_type"));
        _allChestsSortCombo.Items.Add(UiStrings.Get("base.all_chests_sort_rarity"));
        _allChestsSortCombo.Items.Add(UiStrings.Get("base.all_chests_sort_type_then_rarity"));
        _allChestsSortCombo.SelectedIndex = 0;

        _allChestsPaddingLabel = new Label
        {
            Text = UiStrings.Get("base.all_chests_padding_label"),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 6, 4, 0),
        };
        _allChestsPaddingInput = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 119,
            Value = 0,
            Width = 70,
            Anchor = AnchorStyles.Left,
        };

        _allChestsMergeOnlyCheckbox = new CheckBox
        {
            Text = UiStrings.Get("base.all_chests_merge_only"),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 6, 0, 0),
        };
        _allChestsMergeOnlyCheckbox.CheckedChanged += (_, _) =>
        {
            bool mergeOnly = _allChestsMergeOnlyCheckbox.Checked;
            // Merge Only keeps every surviving stack in its current chest/slot - there's no
            // resort order and no front-to-back repacking, so neither control applies to it.
            _allChestsSortCombo.Enabled = !mergeOnly;
            _allChestsPaddingInput.Enabled = !mergeOnly;
        };

        _allChestsSortButton = new Button
        {
            Text = UiStrings.Get("base.all_chests_sort_button"),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(160, 28),
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 14, 0, 0),
        };
        _allChestsSortButton.Click += OnSortAllChestsClicked;

        _allChestsStatusLabel = new Label
        {
            Text = "",
            AutoSize = true,
            MaximumSize = new Size(560, 0),
            Dock = DockStyle.Top,
            Padding = new Padding(0, 10, 0, 0),
        };

        var allChestsForm = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 4,
            Dock = DockStyle.Top,
        };
        allChestsForm.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        allChestsForm.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        allChestsForm.Controls.Add(_allChestsSortLabel, 0, 0);
        allChestsForm.Controls.Add(_allChestsSortCombo, 1, 0);
        allChestsForm.Controls.Add(_allChestsPaddingLabel, 0, 1);
        allChestsForm.Controls.Add(_allChestsPaddingInput, 1, 1);
        allChestsForm.Controls.Add(_allChestsMergeOnlyCheckbox, 1, 2);
        allChestsForm.Controls.Add(_allChestsSortButton, 1, 3);

        var allChestsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        // Reverse add-order for Dock=Top children (see WinForms docking note above):
        // status label goes in last-visually so it's added first.
        allChestsPanel.Controls.Add(_allChestsStatusLabel);
        allChestsPanel.Controls.Add(allChestsForm);
        allChestsPanel.Controls.Add(_allChestsInfoLabel);

        _allChestsPage = new TabPage(UiStrings.Get("base.all_chests_tab"));
        _allChestsPage.Controls.Add(allChestsPanel);
        _storageTabs.TabPages.Add(_allChestsPage);

        for (int i = 0; i < 10; i++)
        {
            _chestNames[i] = "";

            _chestGrids[i] = new InventoryGridPanel { Dock = DockStyle.Fill };
            _chestGrids[i].SetIsStorageInventory(true);
            _chestGrids[i].SetIsChestInventory(true);
            _chestGrids[i].SetIsCargoInventory(true);
            _chestGrids[i].SetSortingEnabled(true);
            _chestGrids[i].SetInventoryGroup("Chest");

            // Container panel with name row + warning label above the inventory grid
            var chestPanel = new Panel { Dock = DockStyle.Fill };

            // --- Name editing row ---
            _chestNameLabels[i] = new Label
            {
                Text = UiStrings.Get("base.chest_name_label"),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 2, 0, 0)
            };
            _chestNameFields[i] = new TextBox
            {
                Width = 200,
                Anchor = AnchorStyles.Left,
                PlaceholderText = UiStrings.Get("base.chest_name_placeholder")
            };
            _chestRenameButtons[i] = new Button
            {
                Text = UiStrings.Get("base.chest_rename"),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(70, 0),
                Anchor = AnchorStyles.Left
            };
            _chestClearButtons[i] = new Button
            {
                Text = UiStrings.Get("base.chest_clear_name"),
                AutoSize = false,
                Width = 24,
                Anchor = AnchorStyles.Left
            };

            var nameRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 5,
                RowCount = 1,
                Padding = new Padding(0, 0, 0, 4)
            };
            nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            nameRow.Controls.Add(_chestNameLabels[i], 0, 0);
            nameRow.Controls.Add(_chestNameFields[i], 1, 0);
            nameRow.Controls.Add(_chestRenameButtons[i], 2, 0);
            nameRow.Controls.Add(_chestClearButtons[i], 3, 0);

            int chestJsonIdx = i; // capture for closure
		var gotoBtn = new Button
			{
				FlatStyle = FlatStyle.Flat,
				FlatAppearance = { BorderColor = ThemeManager.Effective == AppTheme.Dark ? Color.FromArgb(100, 100, 100) : SystemColors.ControlDark, BorderSize = 1 },
				Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular, GraphicsUnit.Point),
				Size = new Size(28, 24),
				Text = "\U0001F4D1",
				Margin = new Padding(1, 3, 1, 1),
				Anchor = AnchorStyles.Right,
				Cursor = Cursors.Hand,
			};
            gotoBtn.Click += (_, _) => GoToJsonRequested?.Invoke(this, new GoToJsonEventArgs("PlayerStateData", $"Chest{chestJsonIdx + 1}Inventory"));
            _chestGotoBtns[i] = gotoBtn;
            nameRow.Controls.Add(gotoBtn, 4, 0);

            _chestWarnings[i] = new Label
            {
                Text = UiStrings.Get("base.chest_warning"),
                ForeColor = ThemeManager.Effective == AppTheme.Dark ? ThemeColors.Dark.ErrorRed : Color.Red,
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 6)
            };

            // WinForms quirk: controls with Dock = Top are laid out in reverse
            // add-order (last added appears at the top of the container).
            // See: https://learn.microsoft.com/dotnet/desktop/winforms/controls/how-to-dock-controls
            // NOTE: Avalonia (cross-platform migration target) does NOT share this
            // behaviour - it stacks in add-order. Re-evaluate when migrating.
            // We add grid (fills remaining space), warning (middle), then nameRow (top).
            chestPanel.Controls.Add(_chestGrids[i]);
            chestPanel.Controls.Add(_chestWarnings[i]);
            chestPanel.Controls.Add(nameRow);

            _chestPages[i] = new TabPage(UiStrings.Format("base.chest_tab", i));
            _chestPages[i].Controls.Add(chestPanel);
            var chestCfg = ExportConfig.Instance;
            _chestGrids[i].SetMaxSupportedLabel("");
            _chestGrids[i].SetExportFileName($"Chest_{i}{chestCfg.ChestExt}");
            string chestExportFilter = ExportConfig.BuildDialogFilter(chestCfg.ChestExt, "Chest inventory");
            string chestImportFilter = ExportConfig.BuildImportFilter(chestCfg.ChestExt, "Chest inventory");
            _chestGrids[i].SetExportFileFilter(chestExportFilter, chestImportFilter, chestCfg.ChestExt.TrimStart('.'));
            _chestGrids[i].SetSuperchargeDisabled(true);
            _storageTabs.TabPages.Add(_chestPages[i]);

            // Wire up rename and clear buttons
            int chestIndex = i; // Capture for closure
            _chestRenameButtons[i].Click += (_, _) => RenameChest(chestIndex);
            _chestClearButtons[i].Click += (_, _) => ClearChestName(chestIndex);
            _chestNameFields[i].KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    _chestRenameButtons[chestIndex].PerformClick();
                }
            };
        }

        // Lazy-load grids when tab is selected
        _storageTabs.SelectedIndexChanged += OnTabSelected;

        // When the panel becomes visible (e.g. outer tab is switched to Chests),
        // ensure the active inner tab's grid is loaded.
        VisibleChanged += (_, _) => { if (Visible) EnsureActiveTabLoaded(); };

        mainLayout.Controls.Add(_storageTabs, 0, 0);
        Controls.Add(mainLayout);

        ResumeLayout(false);
        PerformLayout();
    }

    private void RenameChest(int idx)
    {
        string newName = _chestNameFields[idx].Text.Trim();
        _chestNames[idx] = newName;
        if (_pendingInventories[idx] != null)
            BaseLogic.SetChestName(_pendingInventories[idx], newName);
        UpdateChestTabTitle(idx);
    }

    private void ClearChestName(int idx)
    {
        _chestNameFields[idx].Text = "";
        _chestNames[idx] = "";
        if (_pendingInventories[idx] != null)
            BaseLogic.SetChestName(_pendingInventories[idx], "");
        UpdateChestTabTitle(idx);
    }

    private void UpdateChestTabTitle(int idx)
    {
        string baseLabel = UiStrings.Format("base.chest_tab", idx);
        _chestPages[idx].Text = BaseLogic.FormatChestTabTitle(baseLabel, _chestNames[idx]);
    }

    /// <summary>
    /// Maps a raw <see cref="_storageTabs"/> tab index to a chest index (0-9), or -1 if the
    /// raw index refers to the "All Chests" tab (index 0) or is out of range.
    /// Tab 0 is "All Chests" (no grid to load); Chest 0-9 occupy raw tab indices 1-10.
    /// </summary>
    private static int ChestIndexFromRawTabIndex(int rawTabIndex)
    {
        int idx = rawTabIndex - 1;
        return idx >= 0 && idx < 10 ? idx : -1;
    }

    private void EnsureActiveTabLoaded()
    {
        int idx = ChestIndexFromRawTabIndex(_storageTabs.SelectedIndex);
        if (idx < 0) return;
        if (!_chestLoaded[idx])
        {
            _chestLoaded[idx] = true;
            _chestGrids[idx].LoadInventory(_pendingInventories[idx]);
        }
    }

    private void OnTabSelected(object? sender, EventArgs e)
    {
        int idx = ChestIndexFromRawTabIndex(_storageTabs.SelectedIndex);
        if (idx >= 0 && !_chestLoaded[idx])
        {
            SuspendLayout();
            try
            {
                _chestLoaded[idx] = true;
                _chestGrids[idx].LoadInventory(_pendingInventories[idx]);
            }
            finally { ResumeLayout(true); }
        }
    }

    public void SetDatabase(GameItemDatabase? database)
    {
        _database = database;
        for (int i = 0; i < 10; i++)
            _chestGrids[i].SetDatabase(database);
    }

    public void SetIconManager(IconManager? iconManager)
    {
        _iconManager = iconManager;
        for (int i = 0; i < 10; i++)
            _chestGrids[i].SetIconManager(iconManager);
    }

    public void LoadData(JsonObject saveData)
    {
        // Reset deferred state
        for (int i = 0; i < 10; i++)
        {
            _pendingInventories[i] = null;
            _chestLoaded[i] = false;
            _chestNames[i] = "";
        }

        _playerState = null;
        _allChestsStatusLabel.Text = "";

        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;
            _playerState = playerState;

            for (int i = 0; i < 10; i++)
            {
                string key = $"Chest{i + 1}Inventory";
                _pendingInventories[i] = playerState.GetObject(key);

                // Read chest name and populate UI
                string name = BaseLogic.GetChestName(_pendingInventories[i]);
                _chestNames[i] = name;
                _chestNameFields[i].Text = name;
                UpdateChestTabTitle(i);
            }
        }
        catch { }

        // If visible now, load the active tab immediately.
        // Otherwise VisibleChanged will load it when the panel is first shown.
        if (Visible)
            EnsureActiveTabLoaded();
    }

    public void SaveData(JsonObject saveData)
    {
        // Only save grids that were actually loaded/visited. Unvisited tabs
        // still hold their original JSON data and don't need re-saving.
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            for (int i = 0; i < 10; i++)
            {
                if (!_chestLoaded[i]) continue; // Skip unvisited/unmodified grids
                string key = $"Chest{i + 1}Inventory";
                var chestInv = playerState.GetObject(key);
                if (chestInv != null)
                {
                    _chestGrids[i].SaveInventory(chestInv);
                }
            }
        }
        catch { }
    }

    public void ApplyUiLocalisation()
    {
        for (int i = 0; i < 10; i++)
        {
            UpdateChestTabTitle(i);
            _chestWarnings[i].Text = UiStrings.Get("base.chest_warning");
            _chestNameLabels[i].Text = UiStrings.Get("base.chest_name_label");
            _chestNameFields[i].PlaceholderText = UiStrings.Get("base.chest_name_placeholder");
            _chestRenameButtons[i].Text = UiStrings.Get("base.chest_rename");
            _chestClearButtons[i].Text = UiStrings.Get("base.chest_clear_name");
            _chestGrids[i].ApplyUiLocalisation();
        }
        for (int i = 0; i < 10; i++)
            new ToolTip().SetToolTip(_chestGotoBtns[i], UiStrings.Format("goto_json.tooltip_section", _chestPages[i].Text));

        _allChestsPage.Text = UiStrings.Get("base.all_chests_tab");
        _allChestsInfoLabel.Text = UiStrings.Get("base.all_chests_info");
        _allChestsSortLabel.Text = UiStrings.Get("base.all_chests_sort_label");
        _allChestsPaddingLabel.Text = UiStrings.Get("base.all_chests_padding_label");
        _allChestsMergeOnlyCheckbox.Text = UiStrings.Get("base.all_chests_merge_only");
        _allChestsSortButton.Text = UiStrings.Get("base.all_chests_sort_button");

        int prevSelection = _allChestsSortCombo.SelectedIndex;
        _allChestsSortCombo.Items.Clear();
        _allChestsSortCombo.Items.Add(UiStrings.Get("base.all_chests_sort_name"));
        _allChestsSortCombo.Items.Add(UiStrings.Get("base.all_chests_sort_type"));
        _allChestsSortCombo.Items.Add(UiStrings.Get("base.all_chests_sort_rarity"));
        _allChestsSortCombo.Items.Add(UiStrings.Get("base.all_chests_sort_type_then_rarity"));
        _allChestsSortCombo.SelectedIndex = prevSelection >= 0 ? prevSelection : 0;
    }

    /// <summary>
    /// Runs the selected All Chests operation: either a full sort (merges matching stacks,
    /// sorts by the selected field, and repacks across Chest 0-9 with the requested padding),
    /// or - when Merge Only is checked - an in-place merge that leaves every surviving stack in
    /// its current chest/slot and only removes now-redundant duplicate slots.
    /// </summary>
    private void OnSortAllChestsClicked(object? sender, EventArgs e)
    {
        if (_playerState == null || _database == null) return;

        if (_allChestsMergeOnlyCheckbox.Checked)
        {
            var mergeResult = InventoryBulkActions.MergeAllChestsInPlace(_playerState, _database);
            _allChestsStatusLabel.ForeColor = ThemeManager.Effective == AppTheme.Dark ? ThemeColors.Dark.SuccessGreen : Color.Green;
            _allChestsStatusLabel.Text = UiStrings.Format("base.all_chests_result_success", mergeResult.StacksRemaining, mergeResult.SlotsFreed);
        }
        else
        {
            var mode = _allChestsSortCombo.SelectedIndex switch
            {
                1 => ChestSortMode.Type,
                2 => ChestSortMode.Rarity,
                3 => ChestSortMode.TypeThenRarity,
                _ => ChestSortMode.Name,
            };
            int padding = (int)_allChestsPaddingInput.Value;

            var result = InventoryBulkActions.SortAllChests(_playerState, _database, mode, padding);

            if (!result.Success)
            {
                _allChestsStatusLabel.ForeColor = ThemeManager.Effective == AppTheme.Dark ? ThemeColors.Dark.ErrorRed : Color.Red;
                _allChestsStatusLabel.Text = UiStrings.Format("base.all_chests_result_insufficient", result.StacksPlaced, result.SlotsAvailable);
                return;
            }

            _allChestsStatusLabel.ForeColor = ThemeManager.Effective == AppTheme.Dark ? ThemeColors.Dark.SuccessGreen : Color.Green;
            _allChestsStatusLabel.Text = UiStrings.Format("base.all_chests_result_success", result.StacksPlaced, result.SlotsFreed);
        }

        // Force every chest tab to reload from the freshly updated data the next time it's
        // shown, and refresh whichever one is active right now so the change is visible
        // immediately without switching tabs away and back.
        for (int i = 0; i < 10; i++)
            _chestLoaded[i] = false;
        EnsureActiveTabLoaded();

        DataModified?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Storage sub-panel: Additional Storage inventories
/// from PersistentPlayerBases, each in its own tab with an InventoryGridPanel.
/// Uses lazy loading: only the visible tab's grid is loaded immediately,
/// others are deferred until their tab is selected.
/// </summary>
internal class StorageSubPanel : UserControl
{
    /// <summary>
    /// Bundles an inventory grid with its save-file key and lazy-load state.
    /// </summary>
    private class StorageTab
    {
        public InventoryGridPanel Grid { get; }
        public string LoadKey { get; }
        public string SaveKey { get; }
        public JsonObject? PendingInventory { get; set; }
        public bool Loaded { get; set; }

        public StorageTab(InventoryGridPanel grid, string loadKey, string saveKey)
        {
            Grid = grid;
            LoadKey = loadKey;
            SaveKey = saveKey;
        }
    }

    private readonly TabControl _storageTabs;
    private readonly List<StorageTab> _tabs = new();
    private readonly Label _freighterRefundWarning;
    private readonly List<Button> _storageGotoBtns = new();

    private GameItemDatabase? _database;
    private IconManager? _iconManager;

    /// <summary>Raised when the user requests navigation to a JSON path in the Raw JSON Editor.</summary>
    internal event EventHandler<GoToJsonEventArgs>? GoToJsonRequested;

    public StorageSubPanel()
    {
        DoubleBuffered = true;
        SuspendLayout();

        _storageTabs = new DoubleBufferedTabControl { Dock = DockStyle.Fill };

        // Helper to create a storage grid and register it as a tab
        var storageCfg = ExportConfig.Instance;
        void AddStorageTab(string tabName, string exportFile, string loadKey, string saveKey, string inventoryGroup = "Chest", Control? parentOverride = null)
        {
            var grid = new InventoryGridPanel { Dock = DockStyle.Fill };
            grid.SetIsStorageInventory(true);
            grid.SetIsCargoInventory(true);
            grid.SetSuperchargeDisabled(true);
            grid.SetInventoryGroup(inventoryGroup);
            grid.SetExportFileName(exportFile);
            grid.SetMaxSupportedLabel("");
            string storeExportFilter = ExportConfig.BuildDialogFilter(storageCfg.StorageExt, "Storage inventory");
            string storeImportFilter = ExportConfig.BuildImportFilter(storageCfg.StorageExt, "Storage inventory");
            grid.SetExportFileFilter(storeExportFilter, storeImportFilter, storageCfg.StorageExt.TrimStart('.'));

            var page = new TabPage(tabName);
            var storageKey = loadKey;
			var gotoBtn = new Button
			{
				FlatStyle = FlatStyle.Flat,
				FlatAppearance = { BorderColor = ThemeManager.Effective == AppTheme.Dark ? Color.FromArgb(100, 100, 100) : SystemColors.ControlDark, BorderSize = 1 },
				Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular, GraphicsUnit.Point),
				Size = new Size(28, 24),
				Text = "\U0001F4D1",
				Margin = new Padding(1, 3, 1, 1),
				Cursor = Cursors.Hand,
			};
			gotoBtn.Click += (_, _) => GoToJsonRequested?.Invoke(this, new GoToJsonEventArgs("PlayerStateData", storageKey));
			_storageGotoBtns.Add(gotoBtn);
            var headerPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0, 0, 5, 0),
            };
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			var storageGotoPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = true,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = false,
			};
            storageGotoPanel.Controls.Add(gotoBtn);
            headerPanel.Controls.Add(storageGotoPanel, 2, 0);
            var content = parentOverride ?? grid;
            content.Dock = DockStyle.Fill;
            page.Controls.Add(headerPanel);
            page.Controls.Add(content);
            _storageTabs.TabPages.Add(page);

            _tabs.Add(new StorageTab(grid, loadKey, saveKey));
        }

        AddStorageTab(UiStrings.Get("base.storage_ingredient"), $"Ingredient_Storage{storageCfg.StorageExt}",
            "CookingIngredientsInventory", "CookingIngredientsInventory");

        AddStorageTab(UiStrings.Get("base.storage_corvette_parts"), $"Corvette_Parts_Cache{storageCfg.StorageExt}",
            "CorvetteStorageInventory", "CorvetteStorageInventory");

        AddStorageTab(UiStrings.Get("base.storage_salvage_capsule"), $"Base_Salvage_Capsule{storageCfg.StorageExt}",
            "ChestMagicInventory", "ChestMagicInventory", "BaseCapsule");

        AddStorageTab(UiStrings.Get("base.storage_rocket"), $"Rocket{storageCfg.StorageExt}",
            "RocketLockerInventory", "RocketLockerInventory");

        AddStorageTab(UiStrings.Get("base.storage_fishing_platform"), $"Fishing_Platform{storageCfg.StorageExt}",
            "FishPlatformInventory", "FishPlatformInventory");

        AddStorageTab(UiStrings.Get("base.storage_fish_bait"), $"Fish_Bait{storageCfg.StorageExt}",
            "FishBaitBoxInventory", "FishBaitBoxInventory");

        AddStorageTab(UiStrings.Get("base.storage_food_unit"), $"Food_Unit{storageCfg.StorageExt}",
            "FoodUnitInventory", "FoodUnitInventory");

        // Freighter Refund needs a custom wrapper panel with a warning label
        {
            var grid = new InventoryGridPanel { Dock = DockStyle.Fill };
            grid.SetIsStorageInventory(true);
            grid.SetIsCargoInventory(true);
            grid.SetSuperchargeDisabled(true);
            grid.SetInventoryGroup("Freighter");
            grid.SetExportFileName($"Freighter_Refund{storageCfg.StorageExt}");
            grid.SetMaxSupportedLabel("");
            string refundExportFilter = ExportConfig.BuildDialogFilter(storageCfg.StorageExt, "Refund inventory");
            string refundImportFilter = ExportConfig.BuildImportFilter(storageCfg.StorageExt, "Refund inventory");
            grid.SetExportFileFilter(refundExportFilter, refundImportFilter, storageCfg.StorageExt.TrimStart('.'));

            var wrapper = new Panel { Dock = DockStyle.Fill };
            var spacer = new Panel { Height = 6, Dock = DockStyle.Top };
            _freighterRefundWarning = new Label
            {
                Text = UiStrings.Get("base.storage_freighter_refund_warning"),
                ForeColor = ThemeManager.Effective == AppTheme.Dark ? ThemeColors.Dark.ErrorRed : Color.Red,
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 6)
            };
            wrapper.Controls.Add(grid);
            wrapper.Controls.Add(spacer);
            wrapper.Controls.Add(_freighterRefundWarning);

            var page = new TabPage(UiStrings.Get("base.storage_freighter_refund"));
			var freighterGotoBtn = new Button
			{
				FlatStyle = FlatStyle.Flat,
				FlatAppearance = { BorderColor = ThemeManager.Effective == AppTheme.Dark ? Color.FromArgb(100, 100, 100) : SystemColors.ControlDark, BorderSize = 1 },
				Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular, GraphicsUnit.Point),
				Size = new Size(28, 24),
				Text = "\U0001F4D1",
				Margin = new Padding(1, 3, 1, 1),
				Cursor = Cursors.Hand,
			};
            freighterGotoBtn.Click += (_, _) => GoToJsonRequested?.Invoke(this, new GoToJsonEventArgs("PlayerStateData", "ChestMagic2Inventory"));
            _storageGotoBtns.Add(freighterGotoBtn);
            var freighterHeaderPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0, 0, 5, 0),
            };
            freighterHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            freighterHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            freighterHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			var freighterGotoPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = true,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = false,
			};
            freighterGotoPanel.Controls.Add(freighterGotoBtn);
            freighterHeaderPanel.Controls.Add(freighterGotoPanel, 2, 0);
            wrapper.Dock = DockStyle.Fill;
            page.Controls.Add(freighterHeaderPanel);
            page.Controls.Add(wrapper);
            _storageTabs.TabPages.Add(page);

            _tabs.Add(new StorageTab(grid, "ChestMagic2Inventory", "ChestMagic2Inventory"));
        }

        // Lazy-load grids when tab is selected
        _storageTabs.SelectedIndexChanged += OnTabSelected;

        // When the panel becomes visible (e.g. outer tab is switched to Storage),
        // ensure the active inner tab's grid is loaded.
        VisibleChanged += (_, _) => { if (Visible) EnsureActiveTabLoaded(); };

        Controls.Add(_storageTabs);
        ResumeLayout(false);
        PerformLayout();
    }

    private void EnsureActiveTabLoaded()
    {
        int idx = _storageTabs.SelectedIndex;
        if (idx < 0) idx = 0; // Default to first tab before handle is created
        if (idx < _tabs.Count && !_tabs[idx].Loaded)
        {
            _tabs[idx].Loaded = true;
            _tabs[idx].Grid.LoadInventory(_tabs[idx].PendingInventory);
        }
    }

    private void OnTabSelected(object? sender, EventArgs e)
    {
        int idx = _storageTabs.SelectedIndex;
        if (idx >= 0 && idx < _tabs.Count && !_tabs[idx].Loaded)
        {
            SuspendLayout();
            try
            {
                _tabs[idx].Loaded = true;
                _tabs[idx].Grid.LoadInventory(_tabs[idx].PendingInventory);
            }
            finally { ResumeLayout(true); }
        }
    }

    public void SetDatabase(GameItemDatabase? database)
    {
        _database = database;
        foreach (var tab in _tabs)
            tab.Grid.SetDatabase(database);
    }

    public void SetIconManager(IconManager? iconManager)
    {
        _iconManager = iconManager;
        foreach (var tab in _tabs)
            tab.Grid.SetIconManager(iconManager);
    }

    public void LoadData(JsonObject saveData)
    {
        // Reset deferred state
        foreach (var tab in _tabs)
        {
            tab.PendingInventory = null;
            tab.Loaded = false;
        }

        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            foreach (var tab in _tabs)
                tab.PendingInventory = playerState.GetObject(tab.LoadKey);
        }
        catch { }

        // If visible now, load the active tab immediately.
        // Otherwise VisibleChanged will load it when the panel is first shown.
        if (Visible)
            EnsureActiveTabLoaded();
    }

    public void SaveData(JsonObject saveData)
    {
        // Only save grids that were actually loaded/visited. Unvisited tabs
        // still hold their original JSON data and don't need re-saving.
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            foreach (var tab in _tabs)
            {
                if (!tab.Loaded) continue; // Skip unvisited/unmodified grids
                var inv = playerState.GetObject(tab.SaveKey);
                if (inv != null)
                    tab.Grid.SaveInventory(inv);
            }
        }
        catch { }
    }

    public void ApplyUiLocalisation()
    {
        if (_storageTabs.TabPages.Count >= 8)
        {
            _storageTabs.TabPages[0].Text = UiStrings.Get("base.storage_ingredient");
            _storageTabs.TabPages[1].Text = UiStrings.Get("base.storage_corvette_parts");
            _storageTabs.TabPages[2].Text = UiStrings.Get("base.storage_salvage_capsule");
            _storageTabs.TabPages[3].Text = UiStrings.Get("base.storage_rocket");
            _storageTabs.TabPages[4].Text = UiStrings.Get("base.storage_fishing_platform");
            _storageTabs.TabPages[5].Text = UiStrings.Get("base.storage_fish_bait");
            _storageTabs.TabPages[6].Text = UiStrings.Get("base.storage_food_unit");
            _storageTabs.TabPages[7].Text = UiStrings.Get("base.storage_freighter_refund");
            _freighterRefundWarning.Text = UiStrings.Get("base.storage_freighter_refund_warning");
        }

        foreach (var tab in _tabs)
            tab.Grid.ApplyUiLocalisation();

        for (int i = 0; i < _storageGotoBtns.Count; i++)
            new ToolTip().SetToolTip(_storageGotoBtns[i], UiStrings.Format("goto_json.tooltip_section", _storageTabs.TabPages[i].Text));
    }
}

/// <summary>
/// A TabControl subclass that eliminates flicker when switching between
/// already-populated tab pages.  Painting is completely suppressed while
/// the control transitions from one tab page to another (Selecting ->
/// Selected), then a single full repaint is forced so the new page
/// appears atomically.
///
/// Also provides owner-drawn tab headers so the selected tab gets a
/// distinctive background colour while unselected tabs use the system
/// default.  Uses <see cref="TextRenderer.DrawText(IDeviceContext, string, Font, Rectangle, Color, TextFormatFlags)"/>
/// with <see cref="TextFormatFlags.NoPrefix"/> so ampersand characters
/// in tab text are rendered literally instead of being treated as
/// mnemonic prefixes.
///
/// Cross-platform approach: hides the control during the tab switch
/// and shows it after, so no intermediate paint occurs.
/// </summary>
internal class DoubleBufferedTabControl : TabControl
{
    private bool _subscribed;

    private const int WM_ERASEBKGND = 0x0014;

    public DoubleBufferedTabControl()
    {
        SetStyle(
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint,
            true);

        DrawMode = TabDrawMode.OwnerDrawFixed;
        DrawItem += OnDrawTabItem;
        HandleCreated += OnHandleCreated;
        HandleDestroyed += OnHandleDestroyed;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_ERASEBKGND)
        {
            var p = ThemeManager.Effective == AppTheme.Dark
                ? ThemeColors.Dark
                : ThemeColors.Light;
            using var g = Graphics.FromHdc(m.WParam);
            g.Clear(p.Background);
            m.Result = (IntPtr)1;
            return;
        }
        base.WndProc(ref m);
    }

    private void OnHandleCreated(object? sender, EventArgs e)
    {
        if (_subscribed) return;
        ThemeManager.ThemeChanged += OnThemeChanged;
        _subscribed = true;
    }

    private void OnHandleDestroyed(object? sender, EventArgs e)
    {
        if (!_subscribed) return;
        ThemeManager.ThemeChanged -= OnThemeChanged;
        _subscribed = false;
    }

    private void OnThemeChanged() => Invalidate();

    /// <summary>Freeze painting just before the tab switch begins.</summary>
    protected override void OnSelecting(TabControlCancelEventArgs e)
    {
        SuspendLayout();
        base.OnSelecting(e);
    }

    /// <summary>Re-enable painting after the switch and force one full repaint.</summary>
    protected override void OnSelected(TabControlEventArgs e)
    {
        base.OnSelected(e);
        ResumeLayout(true);
        Invalidate(true);
        Update();
    }

    private void OnDrawTabItem(object? sender, DrawItemEventArgs e)
    {
        bool isSelected = (e.Index == SelectedIndex);
        var bounds = GetTabRect(e.Index);
        var page = TabPages[e.Index];

        var p = ThemeManager.Effective == AppTheme.Dark
            ? ThemeColors.Dark
            : ThemeColors.Light;

        Color backColor = isSelected ? p.TabSelectedBackground : p.TabBackground;
        using (var brush = new SolidBrush(backColor))
            e.Graphics.FillRectangle(brush, bounds);

        if (ThemeManager.Effective == AppTheme.Dark && !isSelected)
        {
            using var pen = new Pen(p.MenuBorder);
            e.Graphics.DrawLine(pen, bounds.Right - 1, bounds.Top + 4,
                bounds.Right - 1, bounds.Bottom - 4);
        }

        var textColor = page.ForeColor == Color.Empty
            ? p.TabForeground
            : page.ForeColor;
        var font = page.Font ?? Font;
        TextRenderer.DrawText(
            e.Graphics,
            page.Text,
            font,
            bounds,
            textColor,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPrefix);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _subscribed)
        {
            ThemeManager.ThemeChanged -= OnThemeChanged;
            _subscribed = false;
        }
        base.Dispose(disposing);
    }
}
