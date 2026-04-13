using System.Diagnostics;
using System.IO.Compression;
using NMSE.Config;
using NMSE.Core;
using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.IO;
using NMSE.Models;
using NMSE.UI.Panels;

namespace NMSE.UI;

public partial class MainFormResources : Form
{
    public const string AppName = "NMSE (NO MAN'S SAVE EDITOR)";
    // VerMajor, VerMinor, VerPatch are generated from version.json into BuildInfo.g.cs
    public const string SuppGameRel = "6.32 Xeno Arena";
    public const string IconPath = "Resources/app/NMSE.ico";
    public const string GitHubUrl = "https://github.com/vectorcmdr/NMSE";
    public const string SponsorUrl = "https://github.com/sponsors/vectorcmdr";
    public const string GitHubCreatorUrl = "https://github.com/vectorcmdr";

    // Strips + buttons
    private readonly MenuStrip _menuStrip;
    private readonly ToolStrip _toolStrip;
    private readonly ToolStrip _toolStrip2;
    private readonly StatusStrip _statusStrip;
    private readonly TabControl _tabControl;
    private ToolStripMenuItem _languageMenu = null!;
    // Help menu item references for robust localisation
    // (avoids fragile hardcoded indices that break when items are reordered/added).
    private ToolStripMenuItem _helpMenu = null!;
    private ToolStripMenuItem _helpGitHubItem = null!;
    private ToolStripMenuItem _helpSponsorItem = null!;
    private ToolStripMenuItem _helpCheckUpdatesItem = null!;
    private ToolStripMenuItem _helpAboutItem = null!;
    private readonly ToolStripStatusLabel _statusLabel;
    private readonly ToolStripStatusLabel _itemCountLabel;
    private int _totalDatabaseItems;
    private ToolStripComboBox _directoryCombo;
    private ToolStripComboBox _saveSlotCombo;
    private ToolStripComboBox _saveFileCombo;
    private readonly ToolStripButton _loadButton;
    private readonly ToolStripButton _saveButton;

    // Tab panels
    private readonly MainStatsPanel _mainStatsPanel;
    private readonly ExosuitPanel _exosuitPanel;
    private readonly MultitoolPanel _multitoolPanel;
    private readonly StarshipPanel _shipPanel;
    private readonly FreighterPanel _freighterPanel;
    private readonly FrigatePanel _frigatePanel;
    private readonly ExocraftPanel _vehiclePanel;
    private readonly CompanionPanel _companionPanel;
    private readonly SquadronPanel _squadronPanel;
    private readonly FleetPanel _fleetPanel;
    private readonly BasePanel _basePanel;
    private readonly CataloguePanel _cataloguePanel;
    private readonly MilestonePanel _milestonePanel;
    private readonly SettlementPanel _settlementPanel;
    private readonly ByteBeatPanel _byteBeatPanel;
    private readonly AccountPanel _accountPanel;
    private readonly RecipePanel _recipePanel;
    private readonly ExportConfigPanel _exportConfigPanel;
    private readonly RawJsonPanel _rawJsonPanel;

    // Data
    private readonly GameItemDatabase _database = new();
    private readonly RecipeDatabase _recipeDatabase = new();
    private readonly LocalisationService _localisationService = new();
    private WordDatabase? _wordDatabase;
    private IconManager? _iconManager;
    private List<List<string>> _saveSlotFiles = new();
    private JsonObject? _currentSaveData;
    private string? _currentFilePath;
    private bool _hasUnsavedChanges;

    /// <summary>The detected platform of the currently loaded save directory.</summary>
    private SaveFileManager.Platform _detectedPlatform = SaveFileManager.Platform.Unknown;
    /// <summary>For Xbox saves: path to the containers.index file.</summary>
    private string? _xboxContainersIndexPath;
    /// <summary>For PS4 memory.dat saves: path to memory.dat and which slot indices map to which slot.</summary>
    private string? _ps4MemoryDatPath;
    /// <summary>For Xbox/PS4 memory.dat: maps combo index to slot identifier or slot index.</summary>
    private List<string>? _platformSlotIdentifiers;
    /// <summary>For Xbox: maps [slotComboIdx][fileComboIdx] to the Xbox slot identifier (e.g. "Slot1Auto").</summary>
    private List<List<string>>? _xboxFileIdentifiers;

    // Deferred panel loading: track which tabs have had LoadData called
    private readonly HashSet<int> _loadedTabIndices = new();

    /// <summary>Background icon preload task started during construction.</summary>
    private Task? _iconPreloadTask;

    /// <summary>Cached application icon so we can re-apply it after window style changes
    /// (e.g. the Opacity 0 to 1 transition that removes WS_EX_LAYERED).</summary>
    private Icon? _appIcon;

    /// <summary>Startup splash screen to close once the main form is visible.</summary>
    private SplashForm? _splashForm;

    /// <summary>Set the startup splash screen that will be closed once the main form is ready.
    /// Prefer passing the splash to the constructor so it is available during LoadDatabase().</summary>
    internal void SetSplash(SplashForm splash) => _splashForm = splash;

    public MainFormResources()
    {
        SuspendLayout();

        // Initialize components
        _menuStrip = new MenuStrip();
        _toolStrip = new ToolStrip();
        _toolStrip2 = new ToolStrip();
        _statusStrip = new StatusStrip();
        _tabControl = new DoubleBufferedTabControl();
        _statusLabel = new ToolStripStatusLabel("Ready");
        _itemCountLabel = new ToolStripStatusLabel("") { Alignment = ToolStripItemAlignment.Right };
        _directoryCombo = new ToolStripComboBox { AutoSize = false, Width = 550 };
        _saveSlotCombo = new ToolStripComboBox { AutoSize = false, Width = 300 };
        _saveFileCombo = new ToolStripComboBox { AutoSize = false, Width = 220 };
        _loadButton = new ToolStripButton("Load");
        _saveButton = new ToolStripButton("Save") { Enabled = false };

        // Create panels
        _mainStatsPanel = new MainStatsPanel();
        _exosuitPanel = new ExosuitPanel();
        _multitoolPanel = new MultitoolPanel();
        _shipPanel = new StarshipPanel();
        _freighterPanel = new FreighterPanel();
        _frigatePanel = new FrigatePanel();
        _vehiclePanel = new ExocraftPanel();
        _companionPanel = new CompanionPanel();
        _squadronPanel = new SquadronPanel();
        _fleetPanel = new FleetPanel(_freighterPanel, _frigatePanel, _squadronPanel);
        _basePanel = new BasePanel();
        _cataloguePanel = new CataloguePanel();
        _milestonePanel = new MilestonePanel();
        _settlementPanel = new SettlementPanel();
        _byteBeatPanel = new ByteBeatPanel();
        _accountPanel = new AccountPanel();
        _recipePanel = new RecipePanel();
        _exportConfigPanel = new ExportConfigPanel();
        _rawJsonPanel = new RawJsonPanel();

        // Embed Recipes as a sub-tab inside Discoveries
        _cataloguePanel.AddRecipeTab(_recipePanel);

        // Track unsaved changes from inventory grids
        _exosuitPanel.DataModified += (s, e) => _hasUnsavedChanges = true;
        _exosuitPanel.CrossInventoryTransferCompleted += OnExosuitCrossInventoryTransferCompleted;
        _multitoolPanel.DataModified += (s, e) => _hasUnsavedChanges = true;
        _shipPanel.DataModified += (s, e) => _hasUnsavedChanges = true;
        _shipPanel.CrossInventoryTransferCompleted += OnStarshipCrossInventoryTransferCompleted;
        _freighterPanel.DataModified += (s, e) => _hasUnsavedChanges = true;
        _vehiclePanel.DataModified += (s, e) => _hasUnsavedChanges = true;
        _cataloguePanel.DataModified += (s, e) => _hasUnsavedChanges = true;
        _accountPanel.DataModified += (s, e) => _hasUnsavedChanges = true;
        _companionPanel.ExosuitCargoModified += OnCompanionExosuitCargoModified;

        // Wire up Save Utilities reload event
        _mainStatsPanel.ReloadRequested += (s, e) =>
        {
            // Repopulate save slots and reload the current save file
            PopulateSaveSlots();
            if (_currentFilePath != null && File.Exists(_currentFilePath))
                LoadSaveData(_currentFilePath);
        };

        InitializeForm();
        InitializeMenus();
        InitializeToolbar();
        InitializeStatusBar();
        InitializeTabs();

        ResumeLayout(false);
        PerformLayout();
    }

    /// <summary>
    /// Performs heavy startup work (database loading, config, etc.).
    /// Must be called after the constructor and after SetSplash() so that
    /// the splash form receives progress updates.
    /// </summary>
    internal void PerformStartup()
    {
        LoadConfig();
        LoadDatabase();

        _splashForm?.SetProgress(93, "Applying language...");
        ApplyStartupLanguage();

        _splashForm?.SetProgress(96, "Detecting save slots...");
        PopulateSaveSlots();

        // Reveal the fully-rendered form once icon preloading finishes.
        // Opacity was set to 0 in LoadDatabase so the user never sees
        // the progressive one-by-one control rendering.
        Shown += async (_, _) =>
        {
            if (_iconPreloadTask != null)
            {
                _splashForm?.SetProgress(98, "Loading icons...");
                try { await _iconPreloadTask; }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Icon preload failed: {ex.Message}");
                }
            }

            // Mark fully ready just before the main window becomes visible.
            _splashForm?.SetProgress(100, "Ready");

            Opacity = 1;

            // Re-apply the icon AFTER the opacity change hack.
            // Setting Opacity from 0 to 1 removes WS_EX_LAYERED
            // from the native window style, which can cause Windows
            // to drop the taskbar icon.  Re-setting Form.Icon forces
            // a fresh WM_SETICON to the shell.
            if (_appIcon != null)
            {
                Icon = _appIcon;
                ShowIcon = true;
            }

            // Close the startup splash screen now that the main form is visible.
            // Disposal is handled by the using statement in Program.Main().
            if (_splashForm != null)
            {
                _splashForm.Close();
                _splashForm = null;
            }

            // Non-blocking background update check after startup
            _ = CheckForUpdateOnStartupAsync();
        };
    }

    private void InitializeForm()
    {
        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.Font;
        Text = $"{AppName} - Build {VerMajor}.{VerMinor}.{VerPatch} ({SuppGameRel})";
        ClientSize = new Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(800, 600);
        FormClosing += OnFormClosing;
        ResizeBegin += (_, _) => SuspendLayout();
        ResizeEnd += (_, _) => { ResumeLayout(true); Refresh(); };

        // Load the application icon for the window title bar and taskbar.
        // The icon is stored in _appIcon so it can be re-applied after the
        // Opacity 0->1 hack for WinForms window rendering quirks + JIT delays
        // Primary: load from the ICO file copied to the output directory.
        // Fallback: Properties.Resources.AppIcon (ResourceManager approach).
        _appIcon = LoadAppIcon();
        if (_appIcon != null)
        {
            Icon = _appIcon;
            ShowIcon = true;
        }

        // Set dock styles before adding controls
        _tabControl.Dock = DockStyle.Fill;

        // Add controls in proper z-order for WinForms docking engine.
        // Controls are processed in reverse z-order (last added = back = processed first).
        // Order: TabControl (Fill, front) -> ToolStrip (Top) -> MenuStrip (Top) -> StatusStrip (Bottom, back)
        Controls.Add(_tabControl);
        Controls.Add(_toolStrip2);
        Controls.Add(_toolStrip);
        Controls.Add(_menuStrip);
        Controls.Add(_statusStrip);
        MainMenuStrip = _menuStrip;
    }

    /// <summary>
    /// Loads the application icon using the most reliable method available.
    /// Primary: reads the ICO file from the output directory.
    /// Fallback: Properties.Resources.AppIcon via the compiled .resources blob.
    /// </summary>
    internal static Icon? LoadAppIcon()
    {
        // 1. Try the file on disk (copied to output by the build).
        try
        {
            string icoPath = Path.Combine(AppContext.BaseDirectory, IconPath);
            if (File.Exists(icoPath))
            {
                // Read into memory so the file is not locked.
                byte[] bytes = File.ReadAllBytes(icoPath);
                using var ms = new MemoryStream(bytes);
                return new Icon(ms);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"File-based icon load failed: {ex.Message}");
        }

        // 2. Fallback: Properties.Resources.AppIcon (ResourceManager).
        try
        {
            return Properties.Resources.AppIcon;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ResourceManager icon load failed: {ex.Message}");
        }

        return null;
    }

    private void InitializeMenus()
    {
        // File menu
        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Open Save Directory...", null, OnOpenDirectory, Keys.Control | Keys.O));
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Load Save File...", null, OnLoadFile, Keys.Control | Keys.L));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Save", null, OnSave, Keys.Control | Keys.S) { Enabled = false });
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Save &As...", null, OnSaveAs, Keys.Control | Keys.Shift | Keys.S) { Enabled = false });
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (_, _) => Close(), Keys.Alt | Keys.F4));
        _menuStrip.Items.Add(fileMenu);

        // Edit menu
        var editMenu = new ToolStripMenuItem("&Edit");
        editMenu.DropDownItems.Add(new ToolStripMenuItem("&Reload", null, OnReload, Keys.F5) { Enabled = false });
        editMenu.DropDownItems.Add(new ToolStripMenuItem("Restore Backup (&All)", null, OnRestoreBackup) { Enabled = false });
        editMenu.DropDownItems.Add(new ToolStripMenuItem("Restore Backup (&Single)", null, OnRestoreBackupSingle) { Enabled = false });
        _menuStrip.Items.Add(editMenu);

        // Tools menu
        var toolsMenu = new ToolStripMenuItem("&Tools");
        toolsMenu.DropDownItems.Add(new ToolStripMenuItem("&Export JSON...", null, OnExportJson) { Enabled = false });
        toolsMenu.DropDownItems.Add(new ToolStripMenuItem("&Import JSON...", null, OnImportJson) { Enabled = false });
        _menuStrip.Items.Add(toolsMenu);

        // Language menu (between Tools and Help)
        _languageMenu = new ToolStripMenuItem("&Language");
        foreach (var (_, tag) in LocalisationService.SupportedLanguages)
        {
            var langItem = new ToolStripMenuItem(tag);
            langItem.Click += OnLanguageSelected;
            _languageMenu.DropDownItems.Add(langItem);
        }
        _menuStrip.Items.Add(_languageMenu);

        // Help menu (store item references for robust localisation)
        _helpMenu = new ToolStripMenuItem("&Help");
        _helpGitHubItem = new ToolStripMenuItem("&GitHub Page", null, OnGitHub);
        _helpSponsorItem = new ToolStripMenuItem("&Sponsor Development", null, OnSponsor);
        _helpCheckUpdatesItem = new ToolStripMenuItem("Check for &Updates...", null, OnCheckForUpdates);
        _helpAboutItem = new ToolStripMenuItem("&About", null, OnAbout);
        _helpMenu.DropDownItems.Add(_helpGitHubItem);
        _helpMenu.DropDownItems.Add(new ToolStripSeparator());
        _helpMenu.DropDownItems.Add(_helpSponsorItem);
        _helpMenu.DropDownItems.Add(new ToolStripSeparator());
        _helpMenu.DropDownItems.Add(_helpCheckUpdatesItem);
        _helpMenu.DropDownItems.Add(new ToolStripSeparator());
        _helpMenu.DropDownItems.Add(_helpAboutItem);
        _menuStrip.Items.Add(_helpMenu);
    }

    private void InitializeToolbar()
    {
        // Row 1: Directory
        _toolStrip.Items.Add(new ToolStripLabel("Directory:"));
        _toolStrip.Items.Add(_directoryCombo);
        _toolStrip.Items.Add(new ToolStripButton("Browse...", null, OnBrowseDirectory));

        // Row 2: Save Slot, File, Load, Save
        _toolStrip2.Items.Add(new ToolStripLabel("Save Slot:"));
        _toolStrip2.Items.Add(_saveSlotCombo);
        _toolStrip2.Items.Add(new ToolStripLabel("File:"));
        _toolStrip2.Items.Add(_saveFileCombo);
        _toolStrip2.Items.Add(new ToolStripSeparator());
        _toolStrip2.Items.Add(_loadButton);
        _toolStrip2.Items.Add(_saveButton);

        _loadButton.Click += OnLoadSlot;
        _saveButton.Click += OnSave;
        _directoryCombo.SelectedIndexChanged += OnDirectoryComboChanged;
        _saveSlotCombo.SelectedIndexChanged += (_, _) => PopulateSaveFileCombo();
    }

    private readonly ToolStripProgressBar _progressBar = new ToolStripProgressBar() { Visible = false, Minimum = 0, Maximum = 100 };

    private void InitializeStatusBar()
    {
        _statusLabel.Spring = true;
        _statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _statusStrip.Items.Add(_progressBar);
        _statusStrip.Items.Add(_statusLabel);
        _statusStrip.Items.Add(_itemCountLabel);
    }

    private void InitializeTabs()
    {
        _tabControl.TabPages.Add(CreateTab("Player", _mainStatsPanel));             // 0
        _tabControl.TabPages.Add(CreateTab("Exosuit", _exosuitPanel));              // 1
        _tabControl.TabPages.Add(CreateTab("Multi-tools", _multitoolPanel));        // 2
        _tabControl.TabPages.Add(CreateTab("Starships", _shipPanel));               // 3
        _tabControl.TabPages.Add(CreateTab("Fleet", _fleetPanel));                  // 4
        _tabControl.TabPages.Add(CreateTab("Exocraft", _vehiclePanel));             // 5
        _tabControl.TabPages.Add(CreateTab("Companions", _companionPanel));         // 6
        _tabControl.TabPages.Add(CreateTab("Bases & Storage", _basePanel));         // 7
        _tabControl.TabPages.Add(CreateTab("Catalogue", _cataloguePanel));          // 8
        _tabControl.TabPages.Add(CreateTab("Milestones", _milestonePanel));         // 9
        _tabControl.TabPages.Add(CreateTab("Settlements", _settlementPanel));       // 10
        _tabControl.TabPages.Add(CreateTab("ByteBeats", _byteBeatPanel));           // 11
        _tabControl.TabPages.Add(CreateTab("Account Rewards", _accountPanel));      // 12
        _tabControl.TabPages.Add(CreateTab("Export Settings", _exportConfigPanel)); // 13
        _tabControl.TabPages.Add(CreateTab("Raw JSON Editor", _rawJsonPanel));      // 14

        // When the user switches to the Raw JSON tab, sync all panel data to
        // the in-memory JsonObject first so the editor reflects current edits.
        _tabControl.SelectedIndexChanged += OnTabChanged;
    }

    /// <summary>
    /// Syncs all panel data to the in-memory JsonObject and refreshes the Raw JSON tree.
    /// Called when the user switches to the Raw JSON tab so that value changes
    /// from other panels are visible in the editor.
    /// Also handles deferred panel loading: panels not loaded during initial
    /// LoadSaveData are loaded on first tab selection.
    /// </summary>
    private void OnTabChanged(object? sender, EventArgs e)
    {
        int idx = _tabControl.SelectedIndex;
        if (idx < 0 || _currentSaveData == null) return;

        // Deferred panel loading: if this tab hasn't been loaded yet, load it now.
        // Hide the content panel before loading and show it after. Because the
        // entire hide->load->show sequence executes within a single event handler,
        // no WM_PAINT is dispatched in between - the message loop never gets a
        // chance to paint the intermediate (empty / stale) state. When Visible
        // is set back to true the panel paints once in its fully-loaded state.
        if (!_loadedTabIndices.Contains(idx))
        {
            var content = GetTabContent(_tabControl.SelectedTab);

            if (content != null) content.Visible = false;
            try
            {
                content?.SuspendLayout();
                try
                {
                    LoadPanelForTab(idx);
                    _loadedTabIndices.Add(idx);
                }
                finally
                {
                    content?.ResumeLayout(false);
                }
            }
            finally
            {
                if (content != null) content.Visible = true;
            }
        }

        // Sync data to in-memory JSON and refresh tree when switching to Raw JSON tab
        if (_tabControl.SelectedTab?.Controls.Count > 0
            && _tabControl.SelectedTab.Controls[0] == _rawJsonPanel)
        {
            SyncAllPanelData();
            _rawJsonPanel.RefreshTree();
        }
    }

    private void OnExosuitCrossInventoryTransferCompleted(object? sender, EventArgs e)
    {
        if (_currentSaveData == null)
            return;

        // Refresh loaded destination panels so transferred items appear immediately.
        if (_loadedTabIndices.Contains(3)) // Starships
            _shipPanel.LoadData(_currentSaveData);

        if (_loadedTabIndices.Contains(4)) // Fleet (Freighter is inside)
            _fleetPanel.LoadData(_currentSaveData);

        if (_loadedTabIndices.Contains(7)) // Bases & Storage (includes Chests)
            _basePanel.LoadData(_currentSaveData);
    }

    private void OnStarshipCrossInventoryTransferCompleted(object? sender, EventArgs e)
    {
        if (_currentSaveData == null)
            return;

        // Refresh loaded destination panels so transferred items appear immediately.
        if (_loadedTabIndices.Contains(4)) // Fleet (Freighter is inside)
            _fleetPanel.LoadData(_currentSaveData);

        if (_loadedTabIndices.Contains(7)) // Bases & Storage (includes Chests)
            _basePanel.LoadData(_currentSaveData);
    }

    private void OnCompanionExosuitCargoModified(object? sender, EventArgs e)
    {
        if (_currentSaveData == null)
            return;

        _hasUnsavedChanges = true;

        // Reload the exosuit panel cargo grid so the newly placed egg is visible
        if (_loadedTabIndices.Contains(1)) // Exosuit is tab 1
            _exosuitPanel.LoadData(_currentSaveData);
    }

    /// <summary>
    /// Returns the content panel inside a tab page, or null if the page is null/empty.
    /// </summary>
    private static Control? GetTabContent(TabPage? page)
        => page?.Controls.Count > 0 ? page.Controls[0] : null;

    /// <summary>
    /// Loads data for the panel at the given tab index.
    /// Called either eagerly (for the active tab during load) or
    /// deferred (on first tab selection).
    /// </summary>
    private void LoadPanelForTab(int tabIndex)
    {
        if (_currentSaveData == null) return;

        switch (tabIndex)
        {
            case 0: // Player
                if (_currentFilePath != null)
                    _mainStatsPanel.SetSaveFilePath(_currentFilePath);
                _mainStatsPanel.LoadData(_currentSaveData);
                if (_accountPanel.AccountData != null)
                    _mainStatsPanel.LoadAccountData(_accountPanel.AccountData);
                break;
            case 1: // Exosuit
                _exosuitPanel.SetSaveScopeKey(AppConfig.BuildSaveScopeKey(_currentFilePath));
                _exosuitPanel.LoadData(_currentSaveData);
                break;
            case 2: // Multi-tool
                _multitoolPanel.LoadData(_currentSaveData);
                break;
            case 3: // Starships
                _shipPanel.SetSaveScopeKey(AppConfig.BuildSaveScopeKey(_currentFilePath));
                _shipPanel.LoadData(_currentSaveData);
                break;
            case 4: // Fleet (loads all three sub-panels)
                _fleetPanel.LoadData(_currentSaveData);
                break;
            case 5: // Exocraft
                _vehiclePanel.LoadData(_currentSaveData);
                break;
            case 6: // Companions
                _companionPanel.LoadData(_currentSaveData);
                break;
            case 7: // Bases & Storage
                _basePanel.LoadData(_currentSaveData);
                break;
            case 8: // Discoveries (includes Recipes sub-tab)
                _cataloguePanel.LoadData(_currentSaveData);
                break;
            case 9: // Milestones
                _milestonePanel.LoadData(_currentSaveData);
                break;
            case 10: // Settlements
                _settlementPanel.LoadData(_currentSaveData);
                break;
            case 11: // ByteBeats
                _byteBeatPanel.LoadData(_currentSaveData);
                break;
            case 12: // Account Rewards
                _accountPanel.LoadData(_currentSaveData);
                break;
            case 13: // Export Settings
                _exportConfigPanel.LoadConfig();
                break;
            case 14: // Raw JSON Editor
                _rawJsonPanel.LoadData(_currentSaveData);
                break;
        }
    }

    /// <summary>
    /// Flushes all loaded panel UI state to the in-memory JsonObject.
    /// Only syncs panels that have been loaded (deferred panels are skipped).
    /// Does NOT write to disk.
    /// </summary>
    private void SyncAllPanelData()
    {
        if (_currentSaveData == null) return;

        if (_loadedTabIndices.Contains(0)) _mainStatsPanel.SaveData(_currentSaveData);
        if (_loadedTabIndices.Contains(1)) _exosuitPanel.SaveData(_currentSaveData);
        if (_loadedTabIndices.Contains(2)) _multitoolPanel.SaveData(_currentSaveData);
        if (_loadedTabIndices.Contains(3)) _shipPanel.SaveData(_currentSaveData);
        if (_loadedTabIndices.Contains(4)) _fleetPanel.SaveData(_currentSaveData);
        if (_loadedTabIndices.Contains(5)) _vehiclePanel.SaveData(_currentSaveData);
        if (_loadedTabIndices.Contains(6)) _companionPanel.SaveData(_currentSaveData);
        if (_loadedTabIndices.Contains(7)) _basePanel.SaveData(_currentSaveData);
        if (_loadedTabIndices.Contains(8)) _cataloguePanel.SaveData(_currentSaveData);
        if (_loadedTabIndices.Contains(9)) _milestonePanel.SaveData(_currentSaveData);
        if (_loadedTabIndices.Contains(10)) _settlementPanel.SaveData(_currentSaveData);
        if (_loadedTabIndices.Contains(11)) _byteBeatPanel.SaveData(_currentSaveData);
        if (_loadedTabIndices.Contains(12)) _accountPanel.SaveData(_currentSaveData);
        // Index 13 (Export Settings) has no save data to sync
        if (_loadedTabIndices.Contains(14)) _rawJsonPanel.SaveData(_currentSaveData);

        if (_accountPanel.AccountData != null && _loadedTabIndices.Contains(0))
            _mainStatsPanel.SaveAccountData(_accountPanel.AccountData);
    }

    private static TabPage CreateTab(string text, Control content)
    {
        var page = new TabPage(text);
        content.Dock = DockStyle.Fill;
        page.Controls.Add(content);
        return page;
    }

    /// <summary>The OS-detected default NMS save directory (cached on first load).</summary>
    private string? _defaultSaveDirectory;

    private void LoadConfig()
    {
        var config = AppConfig.Instance;
        config.Initialize();

        if (config.MainFrameWidth > 0 && config.MainFrameHeight > 0)
        {
            Location = new Point(config.MainFrameX, config.MainFrameY);
            Size = new Size(config.MainFrameWidth, config.MainFrameHeight);
        }

        // Detect the OS default save directory
        _defaultSaveDirectory = SaveFileManager.FindDefaultSaveDirectory();

        // Load recent directories from config
        var recent = config.RecentDirectories;

        // If no recent directories exist, seed with either LastDirectory or the default
        if (recent.Count == 0)
        {
            string? initial = config.LastDirectory ?? _defaultSaveDirectory;
            if (initial != null)
            {
                recent.Add(initial);

                // If LastDirectory differs from default, ensure default is also present
                if (_defaultSaveDirectory != null && !string.Equals(initial, _defaultSaveDirectory,
                        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    recent.Add(_defaultSaveDirectory);

                config.RecentDirectories = recent;
                config.Save();
            }
        }
        else
        {
            // Ensure the default directory is always in the list
            if (_defaultSaveDirectory != null &&
                !recent.Any(d => string.Equals(d, _defaultSaveDirectory,
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
            {
                // Use AddRecentDirectory with the first entry to trigger default-pinning logic
                config.AddRecentDirectory(recent[0], _defaultSaveDirectory);
                recent = config.RecentDirectories;
                config.Save();
            }
        }

        // Populate the directory dropdown
        string? lastDir = config.LastDirectory;
        RebuildDirectoryDropdown(recent, lastDir);
    }

    /// <summary>
    /// Rebuilds the directory dropdown from the given list, selecting <paramref name="selectedDir"/>
    /// (or the first item if null/missing). Suppresses the SelectedIndexChanged event during rebuild.
    /// </summary>
    private void RebuildDirectoryDropdown(IEnumerable<string> directories, string? selectedDir)
    {
        _directoryCombo.SelectedIndexChanged -= OnDirectoryComboChanged;
        _directoryCombo.Items.Clear();
        foreach (var dir in directories)
            _directoryCombo.Items.Add(dir);

        if (selectedDir != null && _directoryCombo.Items.Contains(selectedDir))
            _directoryCombo.SelectedItem = selectedDir;
        else if (_directoryCombo.Items.Count > 0)
            _directoryCombo.SelectedIndex = 0;
        _directoryCombo.SelectedIndexChanged += OnDirectoryComboChanged;
    }

    // (Partial file - full file retained; only LoadDatabase shown here for clarity)
    private void LoadDatabase()
    {
        try
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string dbPath = Path.Combine(basePath, "Resources", "map");

            // Load items from JSON
            string jsonPath = Path.Combine(basePath, "Resources", "json");
            _splashForm?.SetProgress(5, "Loading item database...");
            _database.LoadItemsFromJsonDirectory(jsonPath);

            // Populate corvette part category lookup for the optimizer
            _splashForm?.SetProgress(15, "Loading starship data...");
            StarshipDatabase.LoadFromDatabase(_database);

            // Register extractor-generated techpacks
            TechPacks.RegisterGeneratedPacks();

            // Load recipe database for Recipe panel
            _splashForm?.SetProgress(20, "Loading recipes...");
            string recipesPath = Path.Combine(jsonPath, "Recipes.json");
            _recipeDatabase.LoadFromFile(recipesPath);

            // Load title database for Main Stats titles tab
            string titlesPath = Path.Combine(jsonPath, "Titles.json");
            TitleDatabase.LoadFromFile(titlesPath);

            // Load optional JSON databases (fall back to hardcoded if files don't exist)
            _splashForm?.SetProgress(30, "Loading supplementary databases...");
            FrigateTraitDatabase.LoadFromFile(Path.Combine(jsonPath, "Frigate Traits.json"));
            SettlementDatabase.LoadFromFile(Path.Combine(jsonPath, "Settlement Perks.json"));
            WikiGuideDatabase.LoadFromFile(Path.Combine(jsonPath, "Wiki Guide.json"));
            CompanionAccessoryDatabase.LoadFromFile(Path.Combine(jsonPath, "Companion Accessories.json"));
            PetBattleMoveDatabase.LoadFromFile(Path.Combine(jsonPath, "Pet Battle Moves.json"));
            PetBattleMovesetDatabase.LoadFromFile(Path.Combine(jsonPath, "Pet Battle Movesets.json"));
            PetBiomeAffinityMap.LoadFromFile(Path.Combine(jsonPath, "Game Table Globals.json"));

            // Load word database for Known Words feature (from Words.json)
            _splashForm?.SetProgress(45, "Loading word database...");
            _wordDatabase = new WordDatabase();
            string wordsPath = Path.Combine(jsonPath, "Words.json");
            _wordDatabase.LoadFromFile(wordsPath);
            _cataloguePanel.SetWordDatabase(_wordDatabase);

            // Initialize localisation service with lang/ directory
            _splashForm?.SetProgress(55, "Loading localisation...");
            string langDir = Path.Combine(jsonPath, "lang");
            _localisationService.SetLangDirectory(langDir);

            // Initialize UI string table service with ui/lang/ directory
            string uiLangDir = Path.Combine(basePath, "Resources", "ui", "lang");
            UiStrings.SetDirectory(uiLangDir);

            // Load icon images from Resources/images
            _splashForm?.SetProgress(65, "Loading icons...");
            string iconsPath = Path.Combine(basePath, "Resources", "images");
            if (Directory.Exists(iconsPath))
            {
                _iconManager = new IconManager(iconsPath);
                CoordinateHelper.SetGlyphBasePath(iconsPath);
                _mainStatsPanel.RefreshGlyphButtonImages();

                // Start icon pre-loading immediately on a background thread.
                // This allows icon images to load while the form continues
                // initializing. The main window itself remains visible during
                // startup, matching the previous behaviour.
                var db = _database;
                var iconMgr = _iconManager;
                _iconPreloadTask = Task.Run(() => iconMgr.PreloadIcons(db));
            }

            // Pass item database and icons to inventory panels
            _splashForm?.SetProgress(75, "Initialising panels...");
            _exosuitPanel.SetDatabase(_database);
            _shipPanel.SetDatabase(_database);
            _multitoolPanel.SetDatabase(_database);
            _vehiclePanel.SetDatabase(_database);
            _cataloguePanel.SetDatabase(_database);
            _settlementPanel.SetDatabase(_database);
            _fleetPanel.SetDatabase(_database);
            _basePanel.SetDatabase(_database);

            _exosuitPanel.SetIconManager(_iconManager);
            _shipPanel.SetIconManager(_iconManager);
            _multitoolPanel.SetIconManager(_iconManager);
            _vehiclePanel.SetIconManager(_iconManager);
            _cataloguePanel.SetIconManager(_iconManager);
            _milestonePanel.SetIconManager(_iconManager);
            _settlementPanel.SetIconManager(_iconManager);
            _basePanel.SetIconManager(_iconManager);
            _fleetPanel.SetIconManager(_iconManager);

            _accountPanel.SetDatabase(_database);
            _accountPanel.SetIconManager(_iconManager);
            _mainStatsPanel.SetIconManager(_iconManager);

            // Load rewards database for Account panel (from Rewards.json, falls back to inline static data)
            _splashForm?.SetProgress(85, "Loading rewards data...");
            _accountPanel.LoadRewardsDatabase(jsonPath);

            // Wire up Recipe panel with databases
            _recipePanel.SetDatabases(_recipeDatabase, _database);
            _recipePanel.SetIconManager(_iconManager);

            // Repopulate combo boxes that were created before JSON databases loaded.
            // Panels are constructed in the MainForm constructor before LoadDatabase(),
            // so their initial combo population sees empty static lists.
            _frigatePanel.RefreshTraitCombos();
            _settlementPanel.RefreshPerkCombos();

            // Load export configuration (custom extensions and naming templates)
            _splashForm?.SetProgress(90, "Loading configuration...");
            string exportConfigPath = Path.Combine(basePath, "export_config.json");
            ExportConfig.LoadFromFile(exportConfigPath);
            _exportConfigPanel.ConfigFilePath = exportConfigPath;

            // Load JSON name mapper for obfuscated NMS save file keys (JSON only)
            var mapperJsonPath = Path.Combine(dbPath, "mapping.json");

            // Calculate total items loaded across all databases (including UI string keys)
            _totalDatabaseItems = _database.Items.Count
                + (_wordDatabase?.Count ?? 0)
                + FrigateTraitDatabase.Traits.Count
                + SettlementDatabase.Perks.Count
                + RewardDatabase.Count
                + InventoryStackDatabase.Count
                + UiStrings.TotalKeyCount;

            if (File.Exists(mapperJsonPath))
            {
                var mapper = new JsonNameMapper();
                mapper.Load(mapperJsonPath);
                JsonParser.SetDefaultMapper(mapper);
                _statusLabel.Text = UiStrings.Format("status.loaded_items_mappings", _database.Items.Count, mapper.Count);
            }
            else
            {
                _statusLabel.Text = UiStrings.Format("status.loaded_items_no_mapping", _database.Items.Count);
            }

            _itemCountLabel.Text = UiStrings.Format("status.total_db_items", _totalDatabaseItems);
            _splashForm?.SetProgress(92, "Finalising...");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = UiStrings.Format("status.db_load_warning", ex.Message);
        }
    }

    private void PopulateSaveSlots()
    {
        _saveSlotCombo.Items.Clear();
        _saveFileCombo.Items.Clear();
        _xboxContainersIndexPath = null;
        _ps4MemoryDatPath = null;
        _platformSlotIdentifiers = null;
        _xboxFileIdentifiers = null;

        if (_directoryCombo.SelectedItem is not string dir || !Directory.Exists(dir))
            return;

        _detectedPlatform = SaveFileManager.DetectPlatform(dir);

        // Inform the account panel which platform is active so it can
        // show/hide MXML controls (MXML is only relevant for PC platforms).
        _accountPanel.SetPlatform(_detectedPlatform);

        // -- Xbox Game Pass: containers.index --
        if (_detectedPlatform == SaveFileManager.Platform.XboxGamePass)
        {
            string containersPath = Path.Combine(dir, "containers.index");
            if (File.Exists(containersPath))
            {
                _xboxContainersIndexPath = containersPath;
                try
                {
                    var xboxSlots = ContainersIndexManager.ParseContainersIndex(containersPath);
                    _platformSlotIdentifiers = new List<string>();
                    _saveSlotFiles = new List<List<string>>();
                    _xboxFileIdentifiers = new List<List<string>>();

                    // Group Xbox slots by slot number (e.g., Slot1Auto + Slot1Manual -> Slot 1)
                    // to match the Steam save format: slot combo shows "Xbox: Slot N - SaveName - DIFFICULTY"
                    // and file combo shows "Auto - {GUID}" and "Manual - {GUID}"
                    var slotGroups = new SortedDictionary<int, List<(string Identifier, XboxSlotInfo Info)>>();
                    foreach (var kvp in xboxSlots)
                    {
                        if (!ContainersIndexManager.IsSaveSlot(kvp.Key))
                            continue;

                        int slotNum = ContainersIndexManager.ExtractSlotNumber(kvp.Key);
                        if (!slotGroups.TryGetValue(slotNum, out var group))
                        {
                            group = new List<(string, XboxSlotInfo)>();
                            slotGroups[slotNum] = group;
                        }
                        group.Add((kvp.Key, kvp.Value));
                    }

                    foreach (var kvp in slotGroups)
                    {
                        int slotNum = kvp.Key;
                        var entries = kvp.Value;

                        // Sort so Manual comes before Auto (matching Steam save convention:
                        // manual is listed first in the file combo)
                        entries.Sort((a, b) =>
                        {
                            bool aIsAuto = a.Identifier.Contains("Auto", StringComparison.OrdinalIgnoreCase);
                            bool bIsAuto = b.Identifier.Contains("Auto", StringComparison.OrdinalIgnoreCase);
                            return aIsAuto.CompareTo(bIsAuto);
                        });

                        var slotFiles = new List<string>();
                        var slotIdentifiers = new List<string>();
                        foreach (var (id, info) in entries)
                        {
                            slotFiles.Add(info.DataFilePath ?? "");
                            slotIdentifiers.Add(id);
                        }

                        _saveSlotFiles.Add(slotFiles);
                        _xboxFileIdentifiers.Add(slotIdentifiers);
                        _platformSlotIdentifiers.Add(slotNum.ToString());

                        // Detect save name and difficulty from the first available data file
                        string saveName = "";
                        string difficulty = "";
                        foreach (var (_, info) in entries)
                        {
                            if (info.DataFilePath != null && File.Exists(info.DataFilePath))
                            {
                                saveName = DetectSaveName(info.DataFilePath);
                                difficulty = DetectDifficulty(info.DataFilePath);
                                if (!string.IsNullOrEmpty(saveName)) break;
                            }
                        }

                        string label = BuildSlotLabel($"Xbox: Slot {slotNum}", saveName, difficulty);
                        _saveSlotCombo.Items.Add(label);
                    }

                    // Load Xbox AccountData as the platform equivalent of accountdata.hg
                    if (xboxSlots.TryGetValue(ContainersIndexManager.AccountDataIdentifier, out var accountSlot))
                    {
                        _accountPanel.LoadXboxAccountData(accountSlot);
                    }
                }
                catch (Exception ex)
                {
                    _statusLabel.Text = UiStrings.Format("status.failed_xbox_containers", ex.Message);
                }
            }
        }
        // -- PS4: memory.dat monolithic format --
        else if (_detectedPlatform == SaveFileManager.Platform.PS4 && File.Exists(Path.Combine(dir, "memory.dat")))
        {
            string memoryDatPath = Path.Combine(dir, "memory.dat");
            _ps4MemoryDatPath = memoryDatPath;
            _platformSlotIdentifiers = new List<string>();
            _saveSlotFiles = new List<List<string>>();

            // PS4 memory.dat supports up to 15 save slots (each with auto+manual = 30 sub-slots)
            // Try each slot to see if it has data
            // Minimum valid JSON save data is at least a few bytes (e.g. `{"Version":1}`)
            const int MinimumSlotDataLength = 10;
            for (int i = 0; i < 30; i++)
            {
                try
                {
                    string? testData = MemoryDatManager.ExtractSlotData(memoryDatPath, i);
                    if (testData != null && testData.Length > MinimumSlotDataLength)
                    {
                        _platformSlotIdentifiers.Add(i.ToString());
                        _saveSlotFiles.Add(new List<string> { memoryDatPath });
                        bool isAuto = i % 2 == 1;
                        _saveSlotCombo.Items.Add($"PS4 Slot {i / 2 + 1}{(isAuto ? " (Auto)" : " (Manual)")}");
                    }
                }
                catch { }
            }
        }
        else
        {
            // -- Steam/GOG/PS4 streaming/Switch: file-based saves --
            var saveFiles = new List<List<string>>();
            for (int i = 0; i < 15; i++)
            {
                string manualSave = i == 0 ? "save.hg" : $"save{i * 2 + 1}.hg";
                string autoSave = $"save{i * 2 + 2}.hg";

                string manualPath = Path.Combine(dir, manualSave);
                string autoPath = Path.Combine(dir, autoSave);

                bool hasManual = File.Exists(manualPath);
                bool hasAuto = File.Exists(autoPath);

                if (hasManual || hasAuto)
                {
                    var slotFiles = new List<string>();
                    if (hasManual) slotFiles.Add(manualPath);
                    if (hasAuto) slotFiles.Add(autoPath);

                    saveFiles.Add(slotFiles);
                    string difficulty = DetectDifficulty(slotFiles[0]);
                    string saveName = DetectSaveName(slotFiles[0]);
                    string label = BuildSlotLabel($"Slot {i + 1}", saveName, difficulty);
                    _saveSlotCombo.Items.Add(label);
                }
            }

            // Also check for PS4-style saves (savedata00.hg, savedata02.hg, etc.)
            if (_saveSlotCombo.Items.Count == 0)
            {
                var ps4Files = Directory.GetFiles(dir, "savedata*.hg")
                    .OrderBy(f => f)
                    .ToArray();
                for (int i = 0; i < ps4Files.Length; i++)
                {
                    saveFiles.Add(new List<string> { ps4Files[i] });
                    string difficulty = DetectDifficulty(ps4Files[i]);
                    string saveName = DetectSaveName(ps4Files[i]);
                    string label = BuildSlotLabel($"Save {i + 1}", saveName, difficulty);
                    _saveSlotCombo.Items.Add(label);
                }
            }

            _saveSlotFiles = saveFiles;
        }

        // Load account data (accountdata.hg for Steam/GOG/PS4; Xbox handled above)
        if (_detectedPlatform != SaveFileManager.Platform.XboxGamePass)
            _accountPanel.LoadAccountFile(dir);

        if (_saveSlotCombo.Items.Count > 0)
        {
            _saveSlotCombo.SelectedIndex = 0;
            _statusLabel.Text = UiStrings.Format("status.found_save_slots", _saveSlotCombo.Items.Count, Path.GetFileName(dir), _detectedPlatform);
        }
        else
        {
            _statusLabel.Text = UiStrings.Get("status.no_saves_found");
        }
    }

    private void PopulateSaveFileCombo()
    {
        _saveFileCombo.Items.Clear();

        int slotIndex = _saveSlotCombo.SelectedIndex;
        if (slotIndex < 0 || slotIndex >= _saveSlotFiles.Count)
            return;

        var files = _saveSlotFiles[slotIndex];
        int newestIndex = 0;
        DateTime newestTime = DateTime.MinValue;

        // Xbox: show "Auto - {DirectoryGUID}" / "Manual - {DirectoryGUID}" labels
        bool isXbox = _xboxFileIdentifiers != null
            && slotIndex < _xboxFileIdentifiers.Count;

        for (int i = 0; i < files.Count; i++)
        {
            var filePath = files[i];
            string label;

            if (isXbox)
            {
                string xboxId = _xboxFileIdentifiers![slotIndex][i];
                bool isAuto = xboxId.Contains("Auto", StringComparison.OrdinalIgnoreCase);
                string type = isAuto ? "Auto" : "Manual";

                // Show the blob directory GUID (parent folder of the data blob)
                string dirName = "";
                try
                {
                    string? parentDir = Path.GetDirectoryName(filePath);
                    if (parentDir != null)
                        dirName = Path.GetFileName(parentDir);
                }
                catch { }

                // Append file timestamp
                string timestamp = "";
                try
                {
                    var lastWrite = File.GetLastWriteTime(filePath);
                    timestamp = $" - {lastWrite:dd/MM/yy h:mmtt}";
                    if (lastWrite > newestTime)
                    {
                        newestTime = lastWrite;
                        newestIndex = i;
                    }
                }
                catch { }

                label = $"{type} - {dirName}{timestamp}";
            }
            else
            {
                string fileName = Path.GetFileName(filePath);

                // Determine if this is a manual or auto save based on file naming
                string suffix;
                if (fileName.StartsWith("savedata", StringComparison.OrdinalIgnoreCase))
                    suffix = "";
                else if (fileName.Equals("save.hg", StringComparison.OrdinalIgnoreCase))
                {
                    suffix = " (Manual)";
                }
                else
                {
                    // Odd-numbered saves are manual; even-numbered are auto
                    string numPart = fileName.Replace("save", "").Replace(".hg", "");
                    bool isAuto = int.TryParse(numPart, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out int num) && num % 2 == 0;
                    suffix = isAuto ? " (Auto)" : " (Manual)";
                }

                // Append file timestamp
                string timestamp = "";
                try
                {
                    var lastWrite = File.GetLastWriteTime(filePath);
                    timestamp = $" - {lastWrite:dd/MM/yy h:mmtt}";
                    if (lastWrite > newestTime)
                    {
                        newestTime = lastWrite;
                        newestIndex = i;
                    }
                }
                catch { }

                label = $"{fileName}{suffix}{timestamp}";
            }

            _saveFileCombo.Items.Add(label);
        }

        if (_saveFileCombo.Items.Count > 0)
            _saveFileCombo.SelectedIndex = newestIndex;
    }

    /// <summary>
    /// Detect the difficulty/game mode from a save file using fast header scanning.
    /// Only decompresses the first LZ4 block instead of fully parsing the save.
    /// </summary>
    private static string DetectDifficulty(string filePath)
    {
        try
        {
            int gameMode = SaveFileManager.DetectGameModeFast(filePath);
            if (gameMode > 0)
                return GameModeToString(gameMode);
        }
        catch { }
        return "";
    }

    private static string DetectSaveName(string filePath)
    {
        try
        {
            return SaveFileManager.DetectSaveNameFast(filePath);
        }
        catch { }
        return "";
    }

    /// <summary>
    /// Build a slot label combining prefix, save name, and difficulty.
    /// Format: "Slot N - SaveName - DIFFICULTY" or "Slot N - DIFFICULTY" or "Slot N".
    /// </summary>
    private static string BuildSlotLabel(string prefix, string saveName, string difficulty)
    {
        var parts = new List<string> { prefix };
        if (!string.IsNullOrEmpty(saveName)) parts.Add(saveName);
        if (!string.IsNullOrEmpty(difficulty)) parts.Add(difficulty);
        return string.Join(" - ", parts);
    }

    /// <summary>
    /// Map a 1-based game mode integer to a display string.
    /// </summary>
    private static string GameModeToString(int mode)
    {
        return mode switch
        {
            1 => "NORMAL",
            2 => "SURVIVAL",
            3 => "PERMADEATH",
            4 => "CREATIVE",
            5 => "CUSTOM",
            6 => "SEASONAL",
            7 => "RELAXED",
            8 => "HARDCORE",
            _ => $"MODE {mode}"
        };
    }

    private async void LoadSaveData(string filePath)
    {
        try
        {
            var loadTimer = Stopwatch.StartNew();
            _progressBar.Visible = true;
            _progressBar.Value = 0;
            _statusLabel.Text = UiStrings.Get("status.loading_save");


            // Load and decompress file in background
            var progress = new Progress<int>(v => _progressBar.Value = v);

            _currentSaveData = await Task.Run(() =>
            {
                ((IProgress<int>)progress).Report(10);
                var data = SaveFileManager.LoadSaveFile(filePath);
                ((IProgress<int>)progress).Report(60);
                return data;
            });

            _currentFilePath = filePath;
            string? saveDir = Path.GetDirectoryName(filePath);

            // If the file was loaded directly (Open File), update the toolbar to reflect it
            UpdateToolbarForLoadedFile(filePath);

            // Update panels - only load the currently active tab eagerly.
            // Other tabs are loaded on first selection (deferred loading).
            // Hide the active panel content while loading to suppress painting
            // of intermediate states (same technique as OnTabChanged).
            _progressBar.Value = 80;
            _loadedTabIndices.Clear();

            int activeTab = _tabControl.SelectedIndex;
            var activeContent = GetTabContent(activeTab >= 0 ? _tabControl.TabPages[activeTab] : null);

            if (activeContent != null) activeContent.Visible = false;
            SuspendLayout();
            try
            {
                // Always load account data early (needed by MainStats and Raw JSON)
                if (saveDir != null) _accountPanel.LoadAccountFile(saveDir);
                _rawJsonPanel.SetSaveFilePath(filePath);
                _rawJsonPanel.SetAccountData(_accountPanel.AccountData, _accountPanel.AccountFilePath);

                // Load only the currently selected tab (other tabs loaded on first selection)
                activeContent?.SuspendLayout();
                try
                {
                    LoadPanelForTab(activeTab);
                    _loadedTabIndices.Add(activeTab);
                }
                finally
                {
                    activeContent?.ResumeLayout(false);
                }
            }
            finally
            {
                ResumeLayout(true);
                if (activeContent != null) activeContent.Visible = true;
            }

            // Done
            _progressBar.Value = 100;
            await Task.Delay(200);
            _progressBar.Visible = false;

            // Enable save controls
            _saveButton.Enabled = true;
            EnableMenuItems();

            _statusLabel.Text = UiStrings.Format("status.loaded_save", Path.GetFileName(filePath), loadTimer.ElapsedMilliseconds.ToString("N0"));
            _hasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            _progressBar.Visible = false;
            MessageBox.Show(this, UiStrings.Format("dialog.failed_load_save", ex.Message), UiStrings.Get("dialog.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel.Text = UiStrings.Get("status.failed_load_save");
        }
    }

    /// <summary>
    /// Updates the toolbar combos to reflect a directly-loaded file.
    /// Sets the directory combo to the file's parent directory and
    /// selects the correct save slot and file if possible.
    /// </summary>
    private void UpdateToolbarForLoadedFile(string filePath)
    {
        string? dir = Path.GetDirectoryName(filePath);
        if (dir == null) return;

        string fileName = Path.GetFileName(filePath);

        // If the directory is not already in the combo, add it
        bool dirFound = false;
        for (int i = 0; i < _directoryCombo.Items.Count; i++)
        {
            if (string.Equals(_directoryCombo.Items[i]?.ToString(), dir, StringComparison.OrdinalIgnoreCase))
            {
                // Temporarily unhook to avoid re-populating save slots
                _directoryCombo.SelectedIndexChanged -= OnDirectoryComboChanged;
                _directoryCombo.SelectedIndex = i;
                _directoryCombo.SelectedIndexChanged += OnDirectoryComboChanged;
                dirFound = true;
                break;
            }
        }

        if (!dirFound)
        {
            _directoryCombo.SelectedIndexChanged -= OnDirectoryComboChanged;
            _directoryCombo.Items.Insert(0, dir);
            _directoryCombo.SelectedIndex = 0;
            _directoryCombo.SelectedIndexChanged += OnDirectoryComboChanged;
            // Re-detect platform and populate slots for this directory
            PopulateSaveSlots();
        }

        // Try to match the loaded file to a slot+file in the combos
        for (int slot = 0; slot < _saveSlotFiles.Count; slot++)
        {
            var files = _saveSlotFiles[slot];
            for (int fi = 0; fi < files.Count; fi++)
            {
                if (string.Equals(Path.GetFileName(files[fi]), fileName, StringComparison.OrdinalIgnoreCase))
                {
                    if (_saveSlotCombo.SelectedIndex != slot)
                    {
                        _saveSlotCombo.SelectedIndex = slot;
                    }
                    // After setting slot, try to select the file
                    if (fi < _saveFileCombo.Items.Count)
                        _saveFileCombo.SelectedIndex = fi;
                    return;
                }
            }
        }
    }

    // Event handlers
    private void OnOpenDirectory(object? sender, EventArgs e) => OnBrowseDirectory(sender, e);

    private void OnBrowseDirectory(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = UiStrings.Get("dialog.select_save_dir"),
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            RecordRecentDirectory(dialog.SelectedPath);
        }
    }

    /// <summary>
    /// Records a directory as the most recently used, updates the dropdown and persists to config.
    /// </summary>
    private void RecordRecentDirectory(string directory)
    {
        var config = AppConfig.Instance;
        config.AddRecentDirectory(directory, _defaultSaveDirectory);
        config.Save();

        RebuildDirectoryDropdown(config.RecentDirectories, directory);
        PopulateSaveSlots();
    }

    private void OnDirectoryComboChanged(object? sender, EventArgs e)
    {
        if (_directoryCombo.SelectedItem is string dir)
        {
            var config = AppConfig.Instance;
            config.AddRecentDirectory(dir, _defaultSaveDirectory);
            config.Save();

            RebuildDirectoryDropdown(config.RecentDirectories, dir);
        }

        PopulateSaveSlots();
    }

    private void OnLoadSlot(object? sender, EventArgs e)
    {
        int slotIndex = _saveSlotCombo.SelectedIndex;

        // Xbox containers.index loading - use file combo to pick Auto vs Manual
        if (_xboxContainersIndexPath != null && _xboxFileIdentifiers != null
            && slotIndex >= 0 && slotIndex < _xboxFileIdentifiers.Count)
        {
            int fileIndex = _saveFileCombo.SelectedIndex;
            var identifiers = _xboxFileIdentifiers[slotIndex];
            if (fileIndex < 0 || fileIndex >= identifiers.Count)
                fileIndex = 0;
            string slotId = identifiers[fileIndex];
            LoadXboxSaveData(_xboxContainersIndexPath, slotId);
            return;
        }

        // PS4 memory.dat loading
        if (_ps4MemoryDatPath != null && _platformSlotIdentifiers != null
            && slotIndex >= 0 && slotIndex < _platformSlotIdentifiers.Count)
        {
            int memSlot = int.Parse(_platformSlotIdentifiers[slotIndex], System.Globalization.CultureInfo.InvariantCulture);
            LoadPS4MemoryDatSaveData(_ps4MemoryDatPath, memSlot);
            return;
        }

        // Standard file-based loading
        if (slotIndex >= 0 && slotIndex < _saveSlotFiles.Count)
        {
            var files = _saveSlotFiles[slotIndex];
            int fileIndex = _saveFileCombo.SelectedIndex;
            if (fileIndex < 0 || fileIndex >= files.Count)
                fileIndex = 0;
            string filePath = files[fileIndex];
            LoadSaveData(filePath);
        }
        else
        {
            MessageBox.Show(this, UiStrings.Get("dialog.no_save_slot"), UiStrings.Get("dialog.info"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void LoadXboxSaveData(string containersIndexPath, string slotId)
    {
        try
        {
            var loadTimer = Stopwatch.StartNew();
            _progressBar.Visible = true;
            _progressBar.Value = 0;
            _statusLabel.Text = UiStrings.Format("status.loading_xbox", slotId);

            _currentSaveData = await Task.Run(() =>
            {
                return SaveFileManager.LoadXboxSave(containersIndexPath, slotId);
            });

            if (_currentSaveData == null)
            {
                _progressBar.Visible = false;
                MessageBox.Show(this, UiStrings.Format("dialog.xbox_slot_failed", slotId), UiStrings.Get("dialog.error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _currentFilePath = containersIndexPath; // Track the containers.index path

            _progressBar.Value = 80;
            _loadedTabIndices.Clear();
            int activeTab = _tabControl.SelectedIndex;
            var activeContent = GetTabContent(activeTab >= 0 ? _tabControl.TabPages[activeTab] : null);
            if (activeContent != null) activeContent.Visible = false;
            SuspendLayout();
            try
            {
                // Account data for Xbox is already loaded in PopulateSaveSlots via LoadXboxAccountData
                _rawJsonPanel.SetSaveFilePath(containersIndexPath);
                _rawJsonPanel.SetAccountData(_accountPanel.AccountData, _accountPanel.AccountFilePath);
                activeContent?.SuspendLayout();
                try
                {
                    LoadPanelForTab(activeTab);
                    _loadedTabIndices.Add(activeTab);
                }
                finally { activeContent?.ResumeLayout(false); }
            }
            finally
            {
                ResumeLayout(true);
                if (activeContent != null) activeContent.Visible = true;
            }

            _progressBar.Value = 100;
            await Task.Delay(200);
            _progressBar.Visible = false;
            _saveButton.Enabled = true;
            EnableMenuItems();
            _statusLabel.Text = UiStrings.Format("status.loaded_xbox", slotId, loadTimer.ElapsedMilliseconds.ToString("N0"));
            _hasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            _progressBar.Visible = false;
            MessageBox.Show(this, UiStrings.Format("dialog.failed_load_xbox", ex.Message), UiStrings.Get("dialog.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel.Text = UiStrings.Get("status.failed_load_xbox");
        }
    }

    private async void LoadPS4MemoryDatSaveData(string memoryDatPath, int slotIndex)
    {
        try
        {
            var loadTimer = Stopwatch.StartNew();
            _progressBar.Visible = true;
            _progressBar.Value = 0;
            _statusLabel.Text = UiStrings.Format("status.loading_ps4", slotIndex);

            _currentSaveData = await Task.Run(() =>
            {
                return SaveFileManager.LoadPS4MemoryDatSave(memoryDatPath, slotIndex);
            });

            if (_currentSaveData == null)
            {
                _progressBar.Visible = false;
                MessageBox.Show(this, UiStrings.Format("dialog.ps4_slot_failed", slotIndex), UiStrings.Get("dialog.error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _currentFilePath = memoryDatPath; // Track memory.dat path

            _progressBar.Value = 80;
            _loadedTabIndices.Clear();
            int activeTab = _tabControl.SelectedIndex;
            var activeContent = GetTabContent(activeTab >= 0 ? _tabControl.TabPages[activeTab] : null);
            if (activeContent != null) activeContent.Visible = false;
            SuspendLayout();
            try
            {
                string? saveDir = Path.GetDirectoryName(memoryDatPath);
                if (saveDir != null) _accountPanel.LoadAccountFile(saveDir);
                _rawJsonPanel.SetSaveFilePath(memoryDatPath);
                _rawJsonPanel.SetAccountData(_accountPanel.AccountData, _accountPanel.AccountFilePath);
                activeContent?.SuspendLayout();
                try
                {
                    LoadPanelForTab(activeTab);
                    _loadedTabIndices.Add(activeTab);
                }
                finally { activeContent?.ResumeLayout(false); }
            }
            finally
            {
                ResumeLayout(true);
                if (activeContent != null) activeContent.Visible = true;
            }

            _progressBar.Value = 100;
            await Task.Delay(200);
            _progressBar.Visible = false;
            _saveButton.Enabled = true;
            EnableMenuItems();
            _statusLabel.Text = UiStrings.Format("status.loaded_ps4", slotIndex, loadTimer.ElapsedMilliseconds.ToString("N0"));
            _hasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            _progressBar.Visible = false;
            MessageBox.Show(this, UiStrings.Format("dialog.failed_load_ps4", ex.Message), UiStrings.Get("dialog.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel.Text = UiStrings.Get("status.failed_load_ps4");
        }
    }

    /// <summary>Enable File/Edit/Tools menu items after loading a save.</summary>
    private void EnableMenuItems()
    {
        if (_menuStrip.Items.Count > 0 && _menuStrip.Items[0] is ToolStripMenuItem fileMenu)
            foreach (ToolStripItem item in fileMenu.DropDownItems)
                if (item is ToolStripMenuItem mi && (mi.Text?.StartsWith("&Save") == true || mi.Text?.StartsWith("Save") == true))
                    mi.Enabled = true;
        if (_menuStrip.Items.Count > 1 && _menuStrip.Items[1] is ToolStripMenuItem editMenu)
            foreach (ToolStripItem item in editMenu.DropDownItems)
                if (item is ToolStripMenuItem mi)
                    mi.Enabled = true;
        if (_menuStrip.Items.Count > 2 && _menuStrip.Items[2] is ToolStripMenuItem toolsMenu)
            foreach (ToolStripItem item in toolsMenu.DropDownItems)
                if (item is ToolStripMenuItem mi)
                    mi.Enabled = true;
    }

    private void OnLoadFile(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = UiStrings.Get("dialog.open_save_filter"),
            Title = UiStrings.Get("dialog.open_save_title")
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            LoadSaveData(dialog.FileName);
        }
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (_currentSaveData == null || _currentFilePath == null) return;

        try
        {
            // Sync all panel data to in-memory JsonObjects
            SyncAllPanelData();

            // Backup the save directory before writing, but only if there are unsaved changes
            if (_hasUnsavedChanges)
            {
                string? saveDir = Path.GetDirectoryName(_currentFilePath);
                if (saveDir != null)
                {
                    try { SaveFileManager.BackupSaveDirectory(saveDir); }
                    catch (Exception ex) { Debug.WriteLine($"Backup failed: {ex.Message}"); }
                }
            }

            // Xbox Game Pass saves use a completely different save pipeline:
            // data goes to blob directories, not directly to containers.index.
            if (_detectedPlatform == SaveFileManager.Platform.XboxGamePass
                && _xboxContainersIndexPath != null
                && _xboxFileIdentifiers != null)
            {
                int slotIdx = _saveSlotCombo.SelectedIndex;
                if (slotIdx >= 0 && slotIdx < _xboxFileIdentifiers.Count)
                {
                    int fileIdx = _saveFileCombo.SelectedIndex;
                    var identifiers = _xboxFileIdentifiers[slotIdx];
                    if (fileIdx < 0 || fileIdx >= identifiers.Count)
                        fileIdx = 0;
                    string slotId = identifiers[fileIdx];
                    SaveFileManager.SaveXboxSave(_xboxContainersIndexPath, slotId, _currentSaveData);
                }

                // Save account data (season rewards, etc.) to the AccountData blob.
                // Account data uses raw LZ4 block compression, not NMS streaming.
                if (_accountPanel.AccountData != null)
                {
                    SaveFileManager.SaveXboxAccountData(_xboxContainersIndexPath, _accountPanel.AccountData);
                }

                _statusLabel.Text = UiStrings.Format("status.save_written", Path.GetFileName(_xboxContainersIndexPath));
                _hasUnsavedChanges = false;
                MessageBox.Show(this, UiStrings.Get("dialog.save_success"), UiStrings.Get("dialog.success"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Determine slot index for meta writing
            int metaSlotIdx = _saveSlotCombo.SelectedIndex >= 0 ? _saveSlotCombo.SelectedIndex : 0;

            // Write save file to disk with platform-appropriate meta
            SaveFileManager.SaveToFile(_currentFilePath, _currentSaveData,
                compress: true, writeMeta: true, platform: _detectedPlatform, slotIndex: metaSlotIdx);

            // Write account data file to disk (if loaded).
            // accountdata.hg is always plain JSON with a null terminator — no LZ4
            // compression and no meta file. (Xbox account data is handled separately
            // via SaveXboxAccountData which uses raw LZ4 block compression.)
            if (_accountPanel.AccountData != null && _accountPanel.AccountFilePath != null)
                SaveFileManager.SaveToFile(_accountPanel.AccountFilePath, _accountPanel.AccountData,
                    compress: false, writeMeta: false);

            _statusLabel.Text = UiStrings.Format("status.save_written", Path.GetFileName(_currentFilePath));
            _hasUnsavedChanges = false;
            MessageBox.Show(this, UiStrings.Get("dialog.save_success"), UiStrings.Get("dialog.success"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("dialog.save_failed", ex.Message), UiStrings.Get("dialog.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnSaveAs(object? sender, EventArgs e)
    {
        if (_currentSaveData == null) return;

        using var dialog = new SaveFileDialog
        {
            Filter = UiStrings.Get("dialog.save_as_filter"),
            Title = UiStrings.Get("dialog.save_as_title")
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _currentFilePath = dialog.FileName;
            OnSave(sender, e);
        }
    }

    private void OnReload(object? sender, EventArgs e)
    {
        if (_currentFilePath != null) LoadSaveData(_currentFilePath);
    }

    private void OnRestoreBackup(object? sender, EventArgs e)
    {
        if (_currentFilePath == null) return;

        string? saveDir = Path.GetDirectoryName(_currentFilePath);
        if (saveDir == null) return;

        string dirName = new DirectoryInfo(saveDir).Name;
        string fileName = Path.GetFileName(_currentFilePath);
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        string backupRoot = Path.Combine(exeDir, "Save Backups");

        if (!Directory.Exists(backupRoot))
        {
            MessageBox.Show(this, UiStrings.Get("dialog.no_backup_dir"), UiStrings.Get("dialog.restore_backup"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Find the most recent backup ZIP for this save directory
        string backupPattern = $"{dirName}_*.zip";
        var backups = Directory.GetFiles(backupRoot, backupPattern)
            .OrderByDescending(f => File.GetCreationTimeUtc(f))
            .ToList();

        if (backups.Count == 0)
        {
            MessageBox.Show(this, UiStrings.Get("dialog.no_backup_zips"), UiStrings.Get("dialog.restore_backup"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string latestBackup = backups[0];
        string backupName = Path.GetFileName(latestBackup);

        var result = MessageBox.Show(this,
            UiStrings.Format("dialog.restore_confirm", fileName, backupName, File.GetCreationTime(latestBackup).ToString("g")),
            UiStrings.Get("dialog.restore_backup"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        try
        {
            using var zip = System.IO.Compression.ZipFile.OpenRead(latestBackup);
            var entry = zip.GetEntry(fileName);
            if (entry == null)
            {
                MessageBox.Show(this, UiStrings.Format("dialog.restore_file_not_found", fileName, backupName), UiStrings.Get("dialog.restore_backup"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            entry.ExtractToFile(_currentFilePath, overwrite: true);
            LoadSaveData(_currentFilePath);
            _statusLabel.Text = UiStrings.Format("status.restored_file", fileName, backupName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("dialog.restore_failed", ex.Message), UiStrings.Get("dialog.restore_backup"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnRestoreBackupSingle(object? sender, EventArgs e)
    {
        if (_currentFilePath == null) return;

        string? saveDir = Path.GetDirectoryName(_currentFilePath);
        if (saveDir == null) return;

        string dirName = new DirectoryInfo(saveDir).Name;
        string fileName = Path.GetFileName(_currentFilePath);
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        string backupRoot = Path.Combine(exeDir, "Save Backups");

        if (!Directory.Exists(backupRoot))
        {
            MessageBox.Show(this, UiStrings.Get("dialog.no_backup_dir"), UiStrings.Get("dialog.restore_backup_single"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string backupPattern = $"{dirName}_*.zip";
        var backups = Directory.GetFiles(backupRoot, backupPattern)
            .OrderByDescending(f => File.GetCreationTimeUtc(f))
            .ToList();

        if (backups.Count == 0)
        {
            MessageBox.Show(this, UiStrings.Get("dialog.no_backup_zips"), UiStrings.Get("dialog.restore_backup_single"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string latestBackup = backups[0];
        string backupName = Path.GetFileName(latestBackup);

        // Determine account file name (e.g. "accountdata.hg")
        string? accountFileName = null;
        if (_accountPanel.AccountFilePath != null)
            accountFileName = Path.GetFileName(_accountPanel.AccountFilePath);

        string fileList = $"  • {fileName}";
        if (accountFileName != null)
            fileList += $"\n  • {accountFileName}";

        var result = MessageBox.Show(this,
            UiStrings.Format("dialog.restore_single_confirm", fileList, backupName, File.GetCreationTime(latestBackup).ToString("g")),
            UiStrings.Get("dialog.restore_backup_single"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        try
        {
            using var zip = ZipFile.OpenRead(latestBackup);

            // Restore save file
            var saveEntry = zip.GetEntry(fileName);
            if (saveEntry == null)
            {
                MessageBox.Show(this, UiStrings.Format("dialog.restore_file_not_found", fileName, backupName), UiStrings.Get("dialog.restore_backup_single"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            saveEntry.ExtractToFile(_currentFilePath, overwrite: true);

            // Restore account file
            if (accountFileName != null && _accountPanel.AccountFilePath != null)
            {
                var accountEntry = zip.GetEntry(accountFileName);
                if (accountEntry != null)
                    accountEntry.ExtractToFile(_accountPanel.AccountFilePath, overwrite: true);
            }

            LoadSaveData(_currentFilePath);
            _statusLabel.Text = accountFileName != null
                ? UiStrings.Format("status.restored_files", fileName, accountFileName, backupName)
                : UiStrings.Format("status.restored_file", fileName, backupName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("dialog.restore_failed", ex.Message), UiStrings.Get("dialog.restore_backup_single"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnExportJson(object? sender, EventArgs e)
    {
        if (_currentSaveData == null) return;

        using var dialog = new SaveFileDialog
        {
            Filter = UiStrings.Get("dialog.export_json_filter"),
            Title = UiStrings.Get("dialog.export_json_title")
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            // Flush all panel UI state to in-memory JSON before export
            SyncAllPanelData();
            _currentSaveData.ExportToFile(dialog.FileName);
            _statusLabel.Text = UiStrings.Format("status.exported_json", Path.GetFileName(dialog.FileName));
        }
    }

    private void OnImportJson(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = UiStrings.Get("dialog.import_json_filter"),
            Title = UiStrings.Get("dialog.import_json_title")
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                _currentSaveData = JsonObject.ImportFromFile(dialog.FileName);

                // Exported JSON uses human-readable keys, so auto-detection won't set
                // the NameMapper. Ensure it is set so that save-to-disk correctly
                // reverse-maps keys back to the obfuscated form the game expects.
                _currentSaveData.NameMapper ??= JsonParser.GetDefaultMapper();

                // Modern NMS saves use ActiveContext/BaseContext/ExpeditionContext
                // instead of direct PlayerStateData/SpawnStateData keys.  Register
                // the same context transforms that LoadSaveFile applies so that all
                // panels and meta-file extraction can resolve these virtual keys.
                SaveFileManager.RegisterContextTransforms(_currentSaveData);

                _mainStatsPanel.LoadData(_currentSaveData);
                _exosuitPanel.LoadData(_currentSaveData);
                _multitoolPanel.LoadData(_currentSaveData);
                _shipPanel.LoadData(_currentSaveData);
                _freighterPanel.LoadData(_currentSaveData);
                _frigatePanel.LoadData(_currentSaveData);
                _vehiclePanel.LoadData(_currentSaveData);
                _companionPanel.LoadData(_currentSaveData);
                _squadronPanel.LoadData(_currentSaveData);
                _basePanel.LoadData(_currentSaveData);
                _cataloguePanel.LoadData(_currentSaveData);
                _milestonePanel.LoadData(_currentSaveData);
                _settlementPanel.LoadData(_currentSaveData);
                _byteBeatPanel.LoadData(_currentSaveData);
                _accountPanel.LoadData(_currentSaveData);
                _rawJsonPanel.LoadData(_currentSaveData);
                if (_accountPanel.AccountData != null)
                    _mainStatsPanel.LoadAccountData(_accountPanel.AccountData);

                // Mark all panels as loaded so SyncAllPanelData includes them on save
                for (int i = 0; i <= 14; i++)
                    _loadedTabIndices.Add(i);

                _statusLabel.Text = UiStrings.Format("status.imported_json", Path.GetFileName(dialog.FileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, UiStrings.Format("dialog.import_failed", ex.Message), UiStrings.Get("dialog.error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnGitHub(object? sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = GitHubUrl,
                UseShellExecute = true
            });
        }
        catch { }
    }

    /// <summary>
    /// Applies the saved language preference (or default en-GB) on startup.
    /// Must be called after LoadDatabase() so that the localisation service
    /// has its lang directory set and all databases are loaded.
    /// </summary>
    private void ApplyStartupLanguage()
    {
        string tag = AppConfig.Instance.Language;

        // Update language menu check marks using stored field reference
        foreach (ToolStripItem sub in _languageMenu.DropDownItems)
        {
            if (sub is ToolStripMenuItem langItem)
                langItem.Checked = langItem.Text == tag;
        }

        // Load UI string tables for the selected language (always loads English fallback)
        UiStrings.Load(tag);

        bool loaded = _localisationService.LoadLanguage(tag);
        if (loaded)
        {
            _database.ApplyLocalisation(_localisationService);
            RewardDatabase.ApplyLocalisation(_localisationService);
            _wordDatabase?.ApplyLocalisation(_localisationService);
            _recipeDatabase.ApplyLocalisation(_localisationService);
            TitleDatabase.ApplyLocalisation(_localisationService);
            FrigateTraitDatabase.ApplyLocalisation(_localisationService);
            SettlementDatabase.ApplyLocalisation(_localisationService);
            WikiGuideDatabase.ApplyLocalisation(_localisationService);
            CompanionAccessoryDatabase.ApplyLocalisation(_localisationService);
            _accountPanel.RefreshRewardNames();
            _frigatePanel.RefreshTraitCombos();
            _settlementPanel.RefreshPerkCombos();
            _recipePanel.RefreshLanguage();
            _statusLabel.Text = UiStrings.Format("status.language_set", tag);
        }

        // Apply UI localisation to menus, tabs, and all panels
        ApplyUiLocalisation();
    }

    private void OnLanguageSelected(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem menuItem) return;
        string tag = menuItem.Text ?? "";
        if (string.IsNullOrEmpty(tag)) return;

        // Update language menu check marks using stored field reference
        foreach (ToolStripItem sub in _languageMenu.DropDownItems)
        {
            if (sub is ToolStripMenuItem langItem)
                langItem.Checked = langItem.Text == tag;
        }

        // Load UI string tables for the selected language
        UiStrings.Load(tag);
        string loadingMsg = UiStrings.Get("status.switching_language");

        // `using var` is correct here: ShowDialog() blocks until the form is closed
        // (by loadingForm.Close() in the Shown handler below), then the using-var
        // disposes the form when the enclosing method returns.  This is equivalent
        // to wrapping ShowDialog in a using-block but slightly more concise.
        using var loadingForm = new Form
        {
            Text = loadingMsg,
            Size = new System.Drawing.Size(350, 120),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ControlBox = false,
            ShowInTaskbar = false,
        };
        var loadingLabel = new Label
        {
            Text = loadingMsg,
            Dock = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Font = new System.Drawing.Font(Font.FontFamily, 11f),
        };
        loadingForm.Controls.Add(loadingLabel);

        // Perform the localisation work once the dialog is shown, then close it.
        loadingForm.Shown += async (s, args) =>
        {
            // Give the dialog a chance to paint before doing the work.
            await Task.Yield();

            bool loaded = false;
            int uiStringCount = UiStrings.TranslatedCount;
            int itemCount = 0, rewardCount = 0, wordCount = 0;

            try
            {
                await Task.Run(() =>
                {
                    loaded = _localisationService.LoadLanguage(tag);
                    if (!loaded)
                    {
                        _localisationService.LoadLanguage(null);
                        // Revert all databases to English
                        _database.RevertLocalisation();
                        RewardDatabase.RevertLocalisation();
                        _wordDatabase?.RevertLocalisation();
                        _recipeDatabase.RevertLocalisation();
                        TitleDatabase.RevertLocalisation();
                        FrigateTraitDatabase.RevertLocalisation();
                        SettlementDatabase.RevertLocalisation();
                        WikiGuideDatabase.RevertLocalisation();
                    }
                    else
                    {
                        // Apply localisation to all databases
                        itemCount = _database.ApplyLocalisation(_localisationService);
                        rewardCount = RewardDatabase.ApplyLocalisation(_localisationService);
                        wordCount = _wordDatabase?.ApplyLocalisation(_localisationService) ?? 0;
                        _recipeDatabase.ApplyLocalisation(_localisationService);
                        TitleDatabase.ApplyLocalisation(_localisationService);
                        FrigateTraitDatabase.ApplyLocalisation(_localisationService);
                        SettlementDatabase.ApplyLocalisation(_localisationService);
                        WikiGuideDatabase.ApplyLocalisation(_localisationService);
                        CompanionAccessoryDatabase.ApplyLocalisation(_localisationService);
                    }
                });
            }
            catch
            {
                // In case of unexpected errors, fall back to showing "not found".
                loaded = false;
            }

            try
            {
                if (!loaded)
                {
                    _statusLabel.Text = UiStrings.Format("status.language_not_found", tag);
                }
                else
                {
                    _statusLabel.Text = UiStrings.Format("status.language_localised", tag, itemCount, rewardCount, wordCount, uiStringCount);
                }

                // Refresh all currently-loaded panels so they display the new language
                RefreshLoadedPanels();

                // Re-resolve cached reward display names from localised GameItemDatabase/RewardEntry data
                _accountPanel.RefreshRewardNames();

                // Refresh recipe grid with localised item names (it embeds string values, not live objects)
                _recipePanel.RefreshLanguage();

                // Repopulate combo boxes whose display text embeds trait/perk names
                _frigatePanel.RefreshTraitCombos();
                _settlementPanel.RefreshPerkCombos();

                // Apply UI localisation to menus, tabs, and all panels
                ApplyUiLocalisation();

                // Persist the language preference for next startup
                AppConfig.Instance.Language = tag;
                AppConfig.Instance.Save();
            }
            finally
            {
                loadingForm.Close();
            }
        };

        // Show dialog so it stays centered to the main window
        loadingForm.ShowDialog(this);
    }

    /// <summary>
    /// Re-loads data for every panel that has already been loaded (i.e. whose tab
    /// the user has visited at least once). This is called after a language switch
    /// so that cached display names are replaced with the new translations.
    /// </summary>
    private void RefreshLoadedPanels()
    {
        if (_currentSaveData == null) return;

        foreach (int idx in _loadedTabIndices.ToArray())
        {
            LoadPanelForTab(idx);
        }
    }

    /// <summary>
    /// Applies UI string localisation to all menus, toolbar labels, tab titles,
    /// and panel controls. Called on startup and after every language switch.
    /// Menu accelerator keys (e.g. <c>&amp;File</c>) are included in the
    /// translated strings so they remain functional across languages.
    /// </summary>
    private void ApplyUiLocalisation()
    {
        // ---- Main Menus ----
        // Menu items are identified by their position, which is stable.
        // Language menu (index 3) is NOT localised - the BCP 47 tags stay as-is.
        if (_menuStrip.Items.Count >= 5)
        {
            // File (index 0)
            if (_menuStrip.Items[0] is ToolStripMenuItem fileMenu)
            {
                fileMenu.Text = UiStrings.Get("menu.file");
                if (fileMenu.DropDownItems.Count >= 6)
                {
                    fileMenu.DropDownItems[0].Text = UiStrings.Get("menu.file.open_directory");
                    fileMenu.DropDownItems[1].Text = UiStrings.Get("menu.file.load_file");
                    // index 2 is separator
                    fileMenu.DropDownItems[3].Text = UiStrings.Get("menu.file.save");
                    fileMenu.DropDownItems[4].Text = UiStrings.Get("menu.file.save_as");
                    // index 5 is separator
                    if (fileMenu.DropDownItems.Count >= 7)
                        fileMenu.DropDownItems[6].Text = UiStrings.Get("menu.file.exit");
                }
            }
            // Edit (index 1)
            if (_menuStrip.Items[1] is ToolStripMenuItem editMenu)
            {
                editMenu.Text = UiStrings.Get("menu.edit");
                if (editMenu.DropDownItems.Count >= 3)
                {
                    editMenu.DropDownItems[0].Text = UiStrings.Get("menu.edit.reload");
                    editMenu.DropDownItems[1].Text = UiStrings.Get("menu.edit.restore_backup_all");
                    editMenu.DropDownItems[2].Text = UiStrings.Get("menu.edit.restore_backup_single");
                }
            }
            // Tools (index 2)
            if (_menuStrip.Items[2] is ToolStripMenuItem toolsMenu)
            {
                toolsMenu.Text = UiStrings.Get("menu.tools");
                if (toolsMenu.DropDownItems.Count >= 2)
                {
                    toolsMenu.DropDownItems[0].Text = UiStrings.Get("menu.tools.export_json");
                    toolsMenu.DropDownItems[1].Text = UiStrings.Get("menu.tools.import_json");
                }
            }
            // Language (use stored field reference, BCP 47 tags stay as-is)
            _languageMenu.Text = UiStrings.Get("menu.language");
            // Help (use stored field references to avoid fragile hardcoded indices)
            _helpMenu.Text = UiStrings.Get("menu.help");
            _helpGitHubItem.Text = UiStrings.Get("menu.help.github");
            _helpSponsorItem.Text = UiStrings.Get("menu.help.sponsor");
            _helpCheckUpdatesItem.Text = UiStrings.Get("menu.help.check_updates");
            _helpAboutItem.Text = UiStrings.Get("menu.help.about");
        }

        // ---- Toolbar labels ----
        // Row 1: Directory: [0], combo [1], Browse [2]
        if (_toolStrip.Items.Count >= 3)
        {
            _toolStrip.Items[0].Text = UiStrings.Get("toolbar.directory");
            _toolStrip.Items[2].Text = UiStrings.Get("toolbar.browse");
        }
        // Row 2: Save Slot: [0], combo [1], File: [2], combo [3], sep [4], Load [5], Save [6]
        if (_toolStrip2.Items.Count >= 7)
        {
            _toolStrip2.Items[0].Text = UiStrings.Get("toolbar.save_slot");
            _toolStrip2.Items[2].Text = UiStrings.Get("toolbar.file");
            _loadButton.Text = UiStrings.Get("toolbar.load");
            _saveButton.Text = UiStrings.Get("toolbar.save");
        }

        // ---- Tab pages ----
        string[] tabKeys =
        {
            "tab.player", "tab.exosuit", "tab.multitools", "tab.starships",
            "tab.fleet", "tab.exocraft", "tab.companions", "tab.bases_storage",
            "tab.discoveries", "tab.milestones", "tab.settlements", "tab.bytebeats",
            "tab.account_rewards", "tab.export_settings", "tab.raw_json_editor"
        };
        for (int i = 0; i < _tabControl.TabCount && i < tabKeys.Length; i++)
        {
            _tabControl.TabPages[i].Text = UiStrings.Get(tabKeys[i]);
        }

        // ---- Status bar ----
        if (_totalDatabaseItems > 0)
            _itemCountLabel.Text = UiStrings.Format("status.total_db_items", _totalDatabaseItems);

        // ---- Panel-level localisation ----
        _mainStatsPanel.ApplyUiLocalisation();
        _milestonePanel.ApplyUiLocalisation();
        _cataloguePanel.ApplyUiLocalisation();
        _settlementPanel.ApplyUiLocalisation();
        _byteBeatPanel.ApplyUiLocalisation();
        _accountPanel.ApplyUiLocalisation();
        _recipePanel.ApplyUiLocalisation();
        _rawJsonPanel.ApplyUiLocalisation();
        _exportConfigPanel.ApplyUiLocalisation();
        _exosuitPanel.ApplyUiLocalisation();
        _companionPanel.ApplyUiLocalisation();
        _basePanel.ApplyUiLocalisation();
        _fleetPanel.ApplyUiLocalisation();
        _vehiclePanel.ApplyUiLocalisation();
        _multitoolPanel.ApplyUiLocalisation();
        _shipPanel.ApplyUiLocalisation();
    }

    private void OnSponsor(object? sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = SponsorUrl,
                UseShellExecute = true
            });
        }
        catch { }
    }

    // ---- Update functionality ----

    private static Version CurrentAppVersion =>
        new(int.Parse(VerMajor, System.Globalization.CultureInfo.InvariantCulture), int.Parse(VerMinor, System.Globalization.CultureInfo.InvariantCulture), int.Parse(VerPatch, System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>
    /// Silent background update check that runs after the form is shown.
    /// Shows a prompt only when a newer version is available.
    /// </summary>
    private async Task CheckForUpdateOnStartupAsync()
    {
        try
        {
            // Small delay so the UI is fully interactive before the network call
            await Task.Delay(2000).ConfigureAwait(true);

            var update = await UpdateService.CheckForUpdateAsync(CurrentAppVersion)
                                            .ConfigureAwait(true);
            if (update != null)
                PromptUserForUpdate(update);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Startup update check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Manual update check triggered from Help -> Check for Updates.
    /// Shows feedback even when no update is available.
    /// </summary>
    private async void OnCheckForUpdates(object? sender, EventArgs e)
    {
        _statusLabel.Text = UiStrings.Get("update.checking");
        try
        {
            var update = await UpdateService.CheckForUpdateAsync(CurrentAppVersion)
                                            .ConfigureAwait(true);
            if (update != null)
            {
                PromptUserForUpdate(update);
            }
            else
            {
                _statusLabel.Text = UiStrings.Get("update.up_to_date");
                MessageBox.Show(this,
                    UiStrings.Format("update.up_to_date_msg", $"{VerMajor}.{VerMinor}.{VerPatch}"),
                    UiStrings.Get("update.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = UiStrings.Get("update.check_failed");
            MessageBox.Show(this,
                UiStrings.Format("update.check_failed_msg", ex.Message),
                UiStrings.Get("update.title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Shows a dialog offering the user to download and install an update.
    /// On acceptance, downloads the zip, applies the update, and exits.
    /// </summary>
    private async void PromptUserForUpdate(UpdateInfo update)
    {
        var result = MessageBox.Show(this,
            UiStrings.Format("update.available_msg",
                $"{VerMajor}.{VerMinor}.{VerPatch}",
                update.RemoteVersion.ToString(3)),
            UiStrings.Get("update.available"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (result != DialogResult.Yes)
            return;

        string zipPath = Path.Combine(Path.GetTempPath(),
            $"NMSE-update-{update.RemoteVersion.ToString(3)}.zip");
        try
        {
            _statusLabel.Text = UiStrings.Get("update.downloading");

            var progress = new Progress<(long received, long? total)>(p =>
            {
                if (p.total > 0)
                {
                    int pct = (int)(p.received * 100 / p.total.Value);
                    _statusLabel.Text = UiStrings.Format("update.downloading_progress", pct);
                }
            });

            await UpdateService.DownloadFileAsync(update.DownloadUrl, zipPath, progress)
                               .ConfigureAwait(true);

            _statusLabel.Text = UiStrings.Get("update.applying");

            bool launched = UpdateService.ApplyUpdateAndRelaunch(zipPath);
            if (launched)
            {
                Application.Exit();
            }
            else
            {
                _statusLabel.Text = UiStrings.Get("update.apply_failed");
                MessageBox.Show(this,
                    UiStrings.Get("update.apply_failed_msg"),
                    UiStrings.Get("update.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = UiStrings.Get("update.download_failed");
            MessageBox.Show(this,
                UiStrings.Format("update.download_failed_msg", ex.Message),
                UiStrings.Get("update.title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            // Clean up partial download
            try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
        }
    }

    private void OnAbout(object? sender, EventArgs e)
    {
        using var aboutForm = new Form
        {
            Text = $"About",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(340, 160),
            MaximizeBox = false,
            MinimizeBox = false
        };

        var label = new Label
        {
            Text = $"{AppName}\n{VerMajor}.{VerMinor}.{VerPatch} ({SuppGameRel})\n\nby vector_cmdr",
            AutoSize = true,
            Location = new Point(16, 16)
        };

        var link = new LinkLabel
        {
            Text = GitHubCreatorUrl,
            AutoSize = true,
            Location = new Point(16, 80)
        };
        link.Links[0].LinkData = GitHubCreatorUrl;
        link.LinkClicked += (s, args) =>
        {
            try
            {
                var linkData = link.Links[0].LinkData?.ToString();
                if (!string.IsNullOrEmpty(linkData))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = linkData,
                        UseShellExecute = true
                    });
                }
            }
            catch { }
        };

        var okButton = new System.Windows.Forms.Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(aboutForm.ClientSize.Width - 90, aboutForm.ClientSize.Height - 40),
            Size = new Size(75, 25)
        };

        aboutForm.Controls.Add(label);
        aboutForm.Controls.Add(link);
        aboutForm.Controls.Add(okButton);
        aboutForm.AcceptButton = okButton;
        aboutForm.ShowDialog(this);
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        // Prompt if there are unsaved changes
        if (_hasUnsavedChanges && _currentSaveData != null)
        {
            var result = MessageBox.Show(this,
                UiStrings.Get("dialog.unsaved_changes_msg"),
                UiStrings.Get("dialog.unsaved_changes"),
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                OnSave(this, EventArgs.Empty);
            }
            else if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
        }

        var config = AppConfig.Instance;
        if (WindowState == FormWindowState.Normal)
        {
            config.MainFrameX = Location.X;
            config.MainFrameY = Location.Y;
            config.MainFrameWidth = Size.Width;
            config.MainFrameHeight = Size.Height;
        }
        config.Save();
    }
}
