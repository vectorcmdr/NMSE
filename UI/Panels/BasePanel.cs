using System.Linq;
using NMSE.Core;
using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

/// <summary>
/// Panel for managing player bases and storage containers.
/// Contains two inner tabbed panels: Bases and Storage.
/// </summary>
public partial class BasePanel : UserControl
{
    public BasePanel()
    {
        InitializeComponent();
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
    }

    public void SaveData(JsonObject saveData)
    {
        _basesSubPanel.SaveData(saveData);
        _storageSubPanel.SaveData(saveData);
        _chestsSubPanel.SaveData(saveData);
    }

    public void ApplyUiLocalisation()
    {
        _basesPage.Text = UiStrings.Get("base.tab_bases");
        _chestsPage.Text = UiStrings.Get("base.tab_chests");
        _storagePage.Text = UiStrings.Get("base.tab_storage");
        _basesSubPanel.ApplyUiLocalisation();
        _chestsSubPanel.ApplyUiLocalisation();
        _storageSubPanel.ApplyUiLocalisation();
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
/// Bases sub-panel: base selector, name, items count, NPC management,
/// and export/import/move base computer buttons.
/// </summary>
internal class BasesSubPanel : UserControl
{
    // NPC section
    private readonly ComboBox _npcSelector;
    private readonly ComboBox _npcRaceCombo;
    private readonly TextBox _npcSeed;
    private readonly Button _generateNpcSeedBtn;

    // Base Info section
    private readonly ComboBox _baseSelector;
    private readonly TextBox _baseName;
    private readonly TextBox _baseItems;
    private string? _pendingBaseName;

    // NPC Summon
    private readonly Button _summonWorkerBtn;

    // Buttons
    private readonly Button _exportBtn;
    private readonly Button _importBtn;
    private readonly Button _moveBaseComputerBtn;
    private readonly Button _clearTerrainEditsBtn;

    // Labels for localisation
    private readonly Label _npcTitle;
    private readonly Label _baseTitle;
    private Label? _npcLabel;
    private Label? _raceLabel;
    private Label? _seedLabel;
    private Label? _baseLabel;
    private Label? _nameLabel;
    private Label? _itemsLabel;

    // State
    private JsonObject? _playerState;
    private readonly List<NpcWorkerItem> _npcWorkers = new();
    private readonly List<BaseInfoItem> _baseInfoItems = new();
    private readonly Random _rng = new();

    public BasesSubPanel()
    {
        DoubleBuffered = true;
        SuspendLayout();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 12,
            Padding = new Padding(10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        for (int i = 0; i < 11; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        int row = 0;

        // --- NPC Section ---
        _npcTitle = new Label
        {
            Text = UiStrings.Get("base.npc_header"),
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 4)
        };
        FontManager.ApplyHeadingFont(_npcTitle, 11);
        layout.Controls.Add(_npcTitle, 0, row);
        layout.SetColumnSpan(_npcTitle, 3);
        row++;

        _npcSelector = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _npcSelector.SelectedIndexChanged += OnNpcSelected;
        _npcLabel = AddRow(layout, UiStrings.Get("base.npc_label"), _npcSelector, row); row++;

        _npcRaceCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _npcRaceCombo.Items.AddRange(NpcRace.GetRaceItems());
        _npcRaceCombo.SelectedIndexChanged += OnNpcRaceChanged;
        _raceLabel = AddRow(layout, UiStrings.Get("base.npc_race_label"), _npcRaceCombo, row); row++;

        var seedPanel = new Panel { Dock = DockStyle.Fill, Height = 26 };
        _npcSeed = new TextBox { Dock = DockStyle.Fill };
        _generateNpcSeedBtn = new Button { Text = UiStrings.Get("common.generate"), Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(70, 0) };
        _generateNpcSeedBtn.Click += OnGenerateNpcSeed;
        seedPanel.Controls.Add(_npcSeed);
        seedPanel.Controls.Add(_generateNpcSeedBtn);
        _seedLabel = AddRow(layout, UiStrings.Get("base.npc_seed_label"), seedPanel, row); row++;

        // Summon NPC worker to selected base
        _summonWorkerBtn = new Button { Text = UiStrings.Get("base.summon_npc"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Enabled = false };
        _summonWorkerBtn.Click += OnSummonWorkerToBase;
        layout.Controls.Add(_summonWorkerBtn, 1, row);
        row++;

        // Separator
        var sep1 = new Label { AutoSize = false, Height = 8 };
        layout.Controls.Add(sep1, 0, row);
        layout.SetColumnSpan(sep1, 3);
        row++;

        // --- Base Info Section ---
        _baseTitle = new Label
        {
            Text = UiStrings.Get("base.base_header"),
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 4)
        };
        FontManager.ApplyHeadingFont(_baseTitle, 11);
        layout.Controls.Add(_baseTitle, 0, row);
        layout.SetColumnSpan(_baseTitle, 3);
        row++;

        _baseSelector = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _baseSelector.SelectedIndexChanged += OnBaseSelected;
        _baseLabel = AddRow(layout, UiStrings.Get("base.base_label"), _baseSelector, row); row++;

        _baseName = new TextBox { Dock = DockStyle.Fill };
        _baseName.Leave += OnBaseNameChanged;
        _nameLabel = AddRow(layout, UiStrings.Get("base.name_label"), _baseName, row); row++;

        _baseItems = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
        _itemsLabel = AddRow(layout, UiStrings.Get("base.items_label"), _baseItems, row); row++;

        // Buttons panel
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
        _clearTerrainEditsBtn = new Button { Text = UiStrings.Get("base.clear_terrain"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(140, 0), Enabled = false };
        _clearTerrainEditsBtn.Click += OnClearTerrainEdits;
        buttonPanel.Controls.Add(_exportBtn);
        buttonPanel.Controls.Add(_importBtn);
        buttonPanel.Controls.Add(_moveBaseComputerBtn);
        buttonPanel.Controls.Add(_clearTerrainEditsBtn);
        layout.Controls.Add(buttonPanel, 0, row);
        layout.SetColumnSpan(buttonPanel, 3);
        row++;

        Controls.Add(layout);
        ResumeLayout(false);
        PerformLayout();
    }

    public void LoadData(JsonObject saveData)
    {
        SuspendLayout();
        _npcSelector.BeginUpdate();
        _baseSelector.BeginUpdate();
        try
        {
        _npcSelector.Items.Clear();
        _npcWorkers.Clear();
        _baseSelector.Items.Clear();
        _baseInfoItems.Clear();
        _npcSeed.Text = "";
        _baseName.Text = "";
        _baseItems.Text = "";
        _exportBtn.Enabled = false;
        _importBtn.Enabled = false;
        _moveBaseComputerBtn.Enabled = false;
        _clearTerrainEditsBtn.Enabled = false;

        try
        {
            _playerState = saveData.GetObject("PlayerStateData");
            if (_playerState == null) return;

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

            // Load PersistentPlayerBases (only HomePlanetBase with BaseVersion >= 3)
            var bases = _playerState.GetArray("PersistentPlayerBases");
            if (bases != null)
            {
                for (int i = 0; i < bases.Length; i++)
                {
                    try
                    {
                        var baseObj = bases.GetObject(i);
                        string? baseType = null;
                        try { baseType = baseObj.GetString("BaseType.PersistentBaseTypes") ?? baseObj.GetString("BaseType"); }
                        catch { try { baseType = baseObj.GetString("BaseType"); } catch { } }

                        int baseVersion = 0;
                        try { baseVersion = baseObj.GetInt("BaseVersion"); } catch { }

                        if ("HomePlanetBase".Equals(baseType, StringComparison.OrdinalIgnoreCase) && baseVersion >= 3)
                        {
                            string name = baseObj.GetString("Name") ?? UiStrings.Format("base.fallback_base_name", i + 1);
                            int objectCount = 0;
                            try
                            {
                                var objects = baseObj.GetArray("Objects");
                                if (objects != null) objectCount = objects.Length;
                            }
                            catch { }

                            var item = new BaseInfoItem(name, baseObj, i, objectCount);
                            _baseInfoItems.Add(item);
                            _baseSelector.Items.Add(item);
                        }
                    }
                    catch { }
                }
            }

            if (_npcSelector.Items.Count > 0)
                _npcSelector.SelectedIndex = 0;
            if (_baseSelector.Items.Count > 0)
                _baseSelector.SelectedIndex = 0;
        }
        catch { }
        }
        finally
        {
            _baseSelector.EndUpdate();
            _npcSelector.EndUpdate();
            ResumeLayout(true);
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
            if (_baseSelector.SelectedItem is BaseInfoItem item && !string.IsNullOrEmpty(_pendingBaseName))
            {
                string currentName = item.Data.GetString("Name") ?? "";
                if (_pendingBaseName != currentName)
                {
                    item.Data.Set("Name", _pendingBaseName);
                    item.DisplayName = _pendingBaseName;
                    // Update combo box display
                    int idx = _baseSelector.SelectedIndex;
                    _baseSelector.SelectedIndexChanged -= OnBaseSelected;
                    _baseSelector.Items.RemoveAt(idx);
                    _baseSelector.Items.Insert(idx, item);
                    _baseSelector.SelectedIndex = idx;
                    _baseSelector.SelectedIndexChanged += OnBaseSelected;
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
                    seedArr.Set(1, newSeed);
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
        if (_baseSelector.SelectedItem is not BaseInfoItem baseItem) return;

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

            // Copy Position -> BaseOffset
            var position = baseItem.Data.Get("Position");
            if (position != null)
                worker.Set("BaseOffset", position);

            // Set FreighterBase flag
            worker.Set("FreighterBase", isFreighterBase);

            MessageBox.Show(this, 
                UiStrings.Format("base.summon_complete_msg", npcItem.ToString(), baseItem.DisplayName),
                UiStrings.Get("base.summon_complete_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("base.summon_failed", ex.Message), UiStrings.Get("common.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnBaseSelected(object? sender, EventArgs e)
    {
        if (_baseSelector.SelectedItem is not BaseInfoItem item)
        {
            _baseName.Text = "";
            _baseName.Enabled = false;
            _baseItems.Text = "";
            _exportBtn.Enabled = false;
            _importBtn.Enabled = false;
            _moveBaseComputerBtn.Enabled = false;
            _clearTerrainEditsBtn.Enabled = false;
            _pendingBaseName = null;
            UpdateSummonButtonState();
            return;
        }

        _baseName.Text = item.Data.GetString("Name") ?? "";
        _baseName.Enabled = true;
        _pendingBaseName = _baseName.Text;

        int objectCount = 0;
        try
        {
            var objects = item.Data.GetArray("Objects");
            if (objects != null) objectCount = objects.Length;
        }
        catch { }
        _baseItems.Text = objectCount.ToString();
        _exportBtn.Enabled = true;
        _importBtn.Enabled = true;
        _moveBaseComputerBtn.Enabled = true;
        _clearTerrainEditsBtn.Enabled = true;
        UpdateSummonButtonState();
    }

    private void UpdateSummonButtonState()
    {
        _summonWorkerBtn.Enabled = _npcSelector.SelectedItem is NpcWorkerItem
                                   && _baseSelector.SelectedItem is BaseInfoItem;
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
        if (_baseSelector.SelectedItem is not BaseInfoItem item) return;
        _pendingBaseName = _baseName.Text.Trim();
        // Update combo box display immediately
        if (!string.IsNullOrEmpty(_pendingBaseName))
        {
            item.DisplayName = _pendingBaseName;
            int idx = _baseSelector.SelectedIndex;
            _baseSelector.SelectedIndexChanged -= OnBaseSelected;
            _baseSelector.Items.RemoveAt(idx);
            _baseSelector.Items.Insert(idx, item);
            _baseSelector.SelectedIndex = idx;
            _baseSelector.SelectedIndexChanged += OnBaseSelected;
        }
    }

    private void OnExport(object? sender, EventArgs e)
    {
        if (_baseSelector.SelectedItem is not BaseInfoItem item) return;
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
        if (_baseSelector.SelectedItem is not BaseInfoItem item) return;
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
        if (_baseSelector.SelectedItem is not BaseInfoItem item) return;
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
        if (_baseSelector.SelectedItem is not BaseInfoItem item) return;
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
        if (_npcLabel != null) _npcLabel.Text = UiStrings.Get("base.npc_label");
        if (_raceLabel != null) _raceLabel.Text = UiStrings.Get("base.npc_race_label");
        if (_seedLabel != null) _seedLabel.Text = UiStrings.Get("base.npc_seed_label");
        if (_baseLabel != null) _baseLabel.Text = UiStrings.Get("base.base_label");
        if (_nameLabel != null) _nameLabel.Text = UiStrings.Get("base.name_label");
        if (_itemsLabel != null) _itemsLabel.Text = UiStrings.Get("base.items_label");
        _generateNpcSeedBtn.Text = UiStrings.Get("common.generate");
        _summonWorkerBtn.Text = UiStrings.Get("base.summon_npc");
        _exportBtn.Text = UiStrings.Get("base.export");
        _importBtn.Text = UiStrings.Get("base.import");
        _moveBaseComputerBtn.Text = UiStrings.Get("base.move_basecomp");
        _clearTerrainEditsBtn.Text = UiStrings.Get("base.clear_terrain");

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
        public int DataIndex { get; }
        public int ObjectCount { get; }

        public BaseInfoItem(string displayName, JsonObject data, int dataIndex, int objectCount)
        {
            DisplayName = displayName;
            Data = data;
            DataIndex = dataIndex;
            ObjectCount = objectCount;
        }

        public override string ToString() => DisplayName;
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

    // Tracks the current custom name per chest (empty = default)
    private readonly string[] _chestNames = new string[10];

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

            var nameRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 0, 0, 4)
            };
            nameRow.Controls.Add(_chestNameLabels[i]);
            nameRow.Controls.Add(_chestNameFields[i]);
            nameRow.Controls.Add(_chestRenameButtons[i]);
            nameRow.Controls.Add(_chestClearButtons[i]);

            _chestWarnings[i] = new Label
            {
                Text = UiStrings.Get("base.chest_warning"),
                ForeColor = Color.Red,
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

    private void EnsureActiveTabLoaded()
    {
        int idx = _storageTabs.SelectedIndex;
        if (idx < 0) idx = 0; // Default to first tab before handle is created
        if (idx < 10 && !_chestLoaded[idx])
        {
            _chestLoaded[idx] = true;
            _chestGrids[idx].LoadInventory(_pendingInventories[idx]);
        }
    }

    private void OnTabSelected(object? sender, EventArgs e)
    {
        int idx = _storageTabs.SelectedIndex;
        if (idx >= 0 && idx < 10 && !_chestLoaded[idx])
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

        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

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

    private GameItemDatabase? _database;
    private IconManager? _iconManager;

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
            page.Controls.Add(parentOverride ?? grid);
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
                ForeColor = Color.Red,
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 6)
            };
            wrapper.Controls.Add(grid);
            wrapper.Controls.Add(spacer);
            wrapper.Controls.Add(_freighterRefundWarning);

            var page = new TabPage(UiStrings.Get("base.storage_freighter_refund"));
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
    /// <summary>Background colour for the currently selected tab header.</summary>
    private static readonly Color SelectedTabColor = Color.FromArgb(212, 242, 255);

    public DoubleBufferedTabControl()
    {
        SetStyle(
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint,
            true);

        DrawMode = TabDrawMode.OwnerDrawFixed;
        DrawItem += OnDrawTabItem;
    }

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
        Invalidate(true);  // recursive - invalidates all child controls
        Update();           // paints synchronously so there's no flash
    }

    /// <summary>
    /// Owner-draw handler: paints selected tabs with <see cref="SelectedTabColor"/>
    /// and unselected tabs with the system default tab background.
    /// </summary>
    private void OnDrawTabItem(object? sender, DrawItemEventArgs e)
    {
        bool isSelected = (e.Index == SelectedIndex);
        var bounds = GetTabRect(e.Index);
        var page = TabPages[e.Index];

        // Fill the tab header background
        Color backColor = isSelected ? SelectedTabColor : SystemColors.Control;
        using (var brush = new SolidBrush(backColor))
            e.Graphics.FillRectangle(brush, bounds);

        // Draw the tab text centred, preserving literal "&" characters
        var textColor = page.ForeColor == Color.Empty ? SystemColors.ControlText : page.ForeColor;
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
}
