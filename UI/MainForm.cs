using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using NMSE.Config;
using NMSE.Core;
using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.IO;
using NMSE.Models;
using NMSE.UI.Controls;
using NMSE.UI.Panels;
using NMSE.UI.Util;

namespace NMSE.UI;

public partial class MainFormResources : Form
{
    public const string AppName = "NMSE (NO MAN'S SAVE EDITOR)";
    // VerMajor, VerMinor, VerPatch and SuppGameRel are generated from version.json into BuildInfo.g.cs
    public const string IconPath = "Resources/app/NMSE.ico";
    public const string GitHubUrl = "https://github.com/vectorcmdr/NMSE";
    public const string ReleaseNotesUrl = "https://github.com/vectorcmdr/NMSE/releases/latest";
    public const string SponsorUrl = "https://github.com/sponsors/vectorcmdr";
    public const string GitHubCreatorUrl = "https://github.com/vectorcmdr";
    public const string UserGuideUrl = "https://github.com/vectorcmdr/NMSE/blob/main/docs/user/README.md";

    // Strips + buttons
    private readonly MenuStrip _menuStrip;
    private readonly ToolStrip _toolStrip;
    private readonly ToolStrip _toolStrip2;
    private readonly StatusStrip _statusStrip;
    private readonly TabControl _tabControl;
    private ToolStripMenuItem _languageMenu = null!;
    private ToolStripMenuItem _themeMenu = null!;
    private ToolStripMenuItem _watchMenu = null!;
    private ToolStripMenuItem _watchPromptItem = null!;
    private ToolStripMenuItem _watchAutoItem = null!;
    private ToolStripMenuItem _watchIgnoreItem = null!;
    private ToolStripMenuItem _themeSystemItem = null!;
    private ToolStripMenuItem _themeLightItem = null!;
    private ToolStripMenuItem _themeDarkItem = null!;
    // Help menu item references for robust localisation
    // (avoids fragile hardcoded indices that break when items are reordered/added).
    private ToolStripMenuItem _helpMenu = null!;
    private ToolStripMenuItem _helpGitHubItem = null!;
    private ToolStripMenuItem _helpSponsorItem = null!;
    private ToolStripMenuItem _helpCheckUpdatesItem = null!;
    private ToolStripMenuItem _helpReleaseNotesItem = null!;
    private ToolStripMenuItem _helpUserGuideItem = null!;
    private ToolStripMenuItem _helpAboutItem = null!;
    private readonly ToolStripStatusLabel _statusLabel;
    private readonly ToolStripStatusLabel _itemCountLabel;
    private int _totalDatabaseItems;
    private ToolStripComboBox _directoryCombo;
    private ToolStripComboBox _saveSlotCombo;
    private bool _isGoToJsonNavigation;
    private ToolStripComboBox _saveFileCombo;
    private readonly ToolStripButton _loadButton;
    private readonly ToolStripButton _saveButton;
    private readonly ToolStripComboBox _backupPathCombo;
    private readonly ToolStripButton _backupBrowseButton;

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
    private readonly DatabaseSearchPanel _databaseSearchPanel;

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

    // External file change detection (another program, cloud sync or the game writes
    // to the loaded save/meta/account files while the editor is open).
    private readonly List<FileSystemWatcher> _externalWatchers = new();
    private System.Windows.Forms.Timer? _externalDebounceTimer;
    private readonly HashSet<string> _externalWatchedFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _externalFileTimes = new(StringComparer.OrdinalIgnoreCase);
    private DateTime _externalIgnoreUntilUtc = DateTime.MinValue;
    /// <summary>True while the external-change prompt is open, preventing stacked prompts.</summary>
    private bool _externalPromptOpen;
    private bool _externalDiverged;

    /// <summary>The detected platform of the currently loaded save directory.</summary>
    private SaveFileManager.Platform _detectedPlatform = SaveFileManager.Platform.Unknown;
    /// <summary>For Xbox saves: path to the save container directory (xgs container or containers.index folder).</summary>
    private string? _xboxSavePath;
    /// <summary>For PS4 memory.dat saves: path to memory.dat and which slot indices map to which slot.</summary>
    private string? _ps4MemoryDatPath;
    /// <summary>For Xbox/PS4 memory.dat: maps combo index to slot identifier or slot index.</summary>
    private List<string>? _platformSlotIdentifiers;
    /// <summary>For Xbox: maps [slotComboIdx][fileComboIdx] to the Xbox slot identifier (e.g. "Slot1Auto").</summary>
    private List<List<string>>? _xboxFileIdentifiers;
    /// <summary>For PS4 memory.dat: maps [slotComboIdx][fileComboIdx] to the memory.dat sub-slot index.</summary>
    private List<List<int>>? _ps4SubSlotIndices;
    /// <summary>For PS4 SaveWizard streaming (.hg with NOMANSKY header): original file path for save-back.</summary>
    private string? _ps4NomanSkyPath;

    // Deferred panel loading: track which tabs have had LoadData called
    private readonly HashSet<int> _loadedTabIndices = new();

    /// <summary>Tracks the previously selected tab index for purge on leave logic.</summary>
    private int _prevTabIndex = -1;

    /// <summary>
    /// Editor tabs gated until a save file is loaded: the content panel, the
    /// overlay shown over it, and the original enabled state of every control
    /// (restored when the lock is lifted).
    /// </summary>
    private readonly List<(Control Panel, NoSaveOverlay Overlay, Dictionary<Control, bool> EnabledStates)> _lockedTabs = new();

    /// <summary>Current editor lock state, so repeated lock/unlock requests are no-ops.</summary>
    private bool _editorLocked;

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
        _directoryCombo = new ToolStripComboBox { AutoSize = false, Width = 440 };
        _saveSlotCombo = new ToolStripComboBox { AutoSize = false, Width = 300 };
        _saveFileCombo = new ToolStripComboBox { AutoSize = false, Width = 220 };
        _loadButton = new ToolStripButton("Load");
        _saveButton = new ToolStripButton("Save") { Enabled = false };
        _backupPathCombo = new ToolStripComboBox { AutoSize = false, Width = 250 };
        _backupBrowseButton = new ToolStripButton("Browse...");

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
        _databaseSearchPanel = new DatabaseSearchPanel();

        // Embed Recipes as a sub-tab inside Discoveries
        _cataloguePanel.AddRecipeTab(_recipePanel);

        // Track unsaved changes from inventory grids
        _exosuitPanel.DataModified += OnPanelDataModified;
        _exosuitPanel.CrossInventoryTransferCompleted += OnExosuitCrossInventoryTransferCompleted;
        _multitoolPanel.DataModified += OnPanelDataModified;
        _shipPanel.DataModified += OnPanelDataModified;
        _shipPanel.CrossInventoryTransferCompleted += OnStarshipCrossInventoryTransferCompleted;
        _fleetPanel.DataModified += OnPanelDataModified;
        _vehiclePanel.DataModified += OnPanelDataModified;
        _cataloguePanel.DataModified += OnPanelDataModified;
        _accountPanel.DataModified += OnPanelDataModified;
        _basePanel.DataModified += OnPanelDataModified;
        _mainStatsPanel.DataModified += OnPanelDataModified;
        _milestonePanel.DataModified += OnPanelDataModified;
        _settlementPanel.DataModified += OnPanelDataModified;
        _companionPanel.DataModified += OnPanelDataModified;
        _companionPanel.ExosuitCargoModified += OnCompanionExosuitCargoModified;
        _byteBeatPanel.DataModified += OnPanelDataModified;
        _rawJsonPanel.DataModified += OnPanelDataModified;

        // Wire up GOTO JSON navigation from sub-panels
        _fleetPanel.GoToJsonRequested += OnGoToJsonRequested;
        _freighterPanel.GoToJsonRequested += OnGoToJsonRequested;
        _frigatePanel.GoToJsonRequested += OnGoToJsonRequested;
        _squadronPanel.GoToJsonRequested += OnGoToJsonRequested;
        _vehiclePanel.GoToJsonRequested += OnGoToJsonRequested;
        _companionPanel.GoToJsonRequested += OnGoToJsonRequested;
        _basePanel.GoToJsonRequested += OnGoToJsonRequested;
        _cataloguePanel.GoToJsonRequested += OnGoToJsonRequested;
        _milestonePanel.GoToJsonRequested += OnGoToJsonRequested;
        _settlementPanel.GoToJsonRequested += OnGoToJsonRequested;
        _byteBeatPanel.GoToJsonRequested += OnGoToJsonRequested;
        _exosuitPanel.GoToJsonRequested += OnGoToJsonRequested;
        _multitoolPanel.GoToJsonRequested += OnGoToJsonRequested;
        _shipPanel.GoToJsonRequested += OnGoToJsonRequested;

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
        InstallEditorLock();

        // Subscribe to theme changes so the form re-themes when the user picks a new theme.
        ThemeManager.ThemeChanged += ReapplyTheme;
        // Apply the initial theme to this form so the user sees it from the first paint.
        ThemeApplicator.ApplyToForm(this);

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
        ApplySavedTheme();
        LoadDatabase();

        _splashForm?.SetProgress(93, "Applying language...");
        ApplyStartupLanguage();

        _splashForm?.SetProgress(96, "Detecting save slots...");
        PopulateSaveSlots();

        // Reveal the fully-rendered form once icon preloading finishes so the
        // user never sees icons appear one by one. The preload runs against
        // 128 px assets and takes about 1.5 s on a typical machine.
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

            // Move focus to the tab control so the toolbar combos don't
            // appear with their text highlighted on startup.
            _tabControl.Focus();

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
                _splashForm.CloseFromOwner();
                _splashForm = null;
            }

            // Non-blocking background update check after startup
            _ = CheckForUpdateOnStartupAsync();
        };
    }

    private void InitializeForm()
    {
        BackColor = SystemColors.Control;
        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.Font;
        Text = $"{AppName} - Build {VerMajor}.{VerMinor}.{VerPatch} ({SuppGameRel})";
        ClientSize = new Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(800, 600);
        FormClosing += OnFormClosing;
        ResizeBegin += (_, _) => SuspendLayout();
        ResizeEnd += (_, _) => { ResumeLayout(true); Refresh(); };
        Resize += OnFormResize;

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
        editMenu.DropDownItems.Add(new ToolStripSeparator());
        _watchMenu = new ToolStripMenuItem("Save File &Watching");
        _watchPromptItem = new ToolStripMenuItem("&Ask before reloading", null, (_, _) => SetExternalChangeMode("prompt"));
        _watchAutoItem = new ToolStripMenuItem("&Reload automatically", null, (_, _) => SetExternalChangeMode("auto"));
        _watchIgnoreItem = new ToolStripMenuItem("&Ignore changes", null, (_, _) => SetExternalChangeMode("ignore"));
        _watchMenu.DropDownItems.Add(_watchPromptItem);
        _watchMenu.DropDownItems.Add(_watchAutoItem);
        _watchMenu.DropDownItems.Add(_watchIgnoreItem);
        editMenu.DropDownItems.Add(_watchMenu);
        _menuStrip.Items.Add(editMenu);

        // Tools menu
        var toolsMenu = new ToolStripMenuItem("&Tools");
        toolsMenu.DropDownItems.Add(new ToolStripMenuItem("&Export JSON...", null, OnExportJson) { Enabled = false });
        toolsMenu.DropDownItems.Add(new ToolStripMenuItem("&Import JSON...", null, OnImportJson) { Enabled = false });
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add(new ToolStripMenuItem("Recharge All Technology", null, OnToolsRechargeAllTech) { Enabled = false });
        toolsMenu.DropDownItems.Add(new ToolStripMenuItem("Refill All Stacks", null, OnToolsRefillAllStacks) { Enabled = false });
        toolsMenu.DropDownItems.Add(new ToolStripMenuItem("Repair All Slots", null, OnToolsRepairAllSlots) { Enabled = false });
        toolsMenu.DropDownItems.Add(new ToolStripMenuItem("Repair All Technology", null, OnToolsRepairAllTech) { Enabled = false });
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

        // Theme menu (second last, before Help)
        _themeMenu = new ToolStripMenuItem("&Theme");
        _themeSystemItem = new ToolStripMenuItem("&System", null, (_, _) => SetTheme(AppTheme.System));
        _themeLightItem = new ToolStripMenuItem("&Light", null, (_, _) => SetTheme(AppTheme.Light));
        _themeDarkItem = new ToolStripMenuItem("&Dark", null, (_, _) => SetTheme(AppTheme.Dark));
        _themeMenu.DropDownItems.Add(_themeSystemItem);
        _themeMenu.DropDownItems.Add(_themeLightItem);
        _themeMenu.DropDownItems.Add(_themeDarkItem);
        _menuStrip.Items.Add(_themeMenu);

        // Help menu (store item references for robust localisation)
        _helpMenu = new ToolStripMenuItem("&Help");
        _helpGitHubItem = new ToolStripMenuItem("&GitHub Page", null, OnGitHub);
        _helpUserGuideItem = new ToolStripMenuItem("&User Guide", null, OnUserGuide);
        _helpSponsorItem = new ToolStripMenuItem("&Sponsor Development", null, OnSponsor);
        _helpCheckUpdatesItem = new ToolStripMenuItem("Check for &Updates...", null, OnCheckForUpdates);
        _helpReleaseNotesItem = new ToolStripMenuItem("&Release Notes", null, OnReleaseNotes);
        _helpAboutItem = new ToolStripMenuItem("&About", null, OnAbout);
        _helpMenu.DropDownItems.Add(_helpGitHubItem);
        _helpMenu.DropDownItems.Add(_helpUserGuideItem);
        _helpMenu.DropDownItems.Add(new ToolStripSeparator());
        _helpMenu.DropDownItems.Add(_helpSponsorItem);
        _helpMenu.DropDownItems.Add(new ToolStripSeparator());
        _helpMenu.DropDownItems.Add(_helpCheckUpdatesItem);
        _helpMenu.DropDownItems.Add(_helpReleaseNotesItem);
        _helpMenu.DropDownItems.Add(new ToolStripSeparator());
        _helpMenu.DropDownItems.Add(_helpAboutItem);
        _menuStrip.Items.Add(_helpMenu);
    }

    private void InitializeToolbar()
    {
        // Row 1: Directory + Backup
        _toolStrip.Items.Add(new ToolStripLabel("Directory:"));
        _toolStrip.Items.Add(_directoryCombo);
        _toolStrip.Items.Add(new ToolStripButton("Browse...", null, OnBrowseDirectory));
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(new ToolStripLabel("Backup:"));
        _toolStrip.Items.Add(_backupPathCombo);
        _toolStrip.Items.Add(_backupBrowseButton);

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
        _backupBrowseButton.Click += OnBrowseBackup;
        _backupPathCombo.SelectedIndexChanged += OnBackupComboChanged;
        _backupPathCombo.Leave += OnBackupPathChanged;
        _directoryCombo.SelectedIndexChanged += OnDirectoryComboChanged;
        _saveSlotCombo.SelectedIndexChanged += (_, _) => PopulateSaveFileCombo();

        // Widen the drop-down lists to fit their longest entry when they open. The selector
        // itself keeps its configured width.
        _directoryCombo.ComboBox.DropDown += (_, _) => FitToolbarDropDownWidth(_directoryCombo);
        _backupPathCombo.ComboBox.DropDown += (_, _) => FitToolbarDropDownWidth(_backupPathCombo);
        _saveSlotCombo.ComboBox.DropDown += (_, _) => FitToolbarDropDownWidth(_saveSlotCombo);
        _saveFileCombo.ComboBox.DropDown += (_, _) => FitToolbarDropDownWidth(_saveFileCombo);
    }

    /// <summary>
    /// Sizes a toolbar combo's drop-down list to fit its longest entry, capped to the working
    /// area of the screen the window is on. The selector width is not changed.
    /// </summary>
    /// <param name="combo">The toolbar combo about to open its drop-down list.</param>
    private void FitToolbarDropDownWidth(ToolStripComboBox combo)
    {
        if (combo.Items.Count == 0) return;

        var inner = combo.ComboBox;
        int width = combo.Width;
        foreach (var item in combo.Items)
        {
            string text = item?.ToString() ?? "";
            if (text.Length == 0) continue;

            int measured = TextRenderer.MeasureText(text, inner.Font).Width;
            if (measured > width) width = measured;
        }

        width += SystemInformation.VerticalScrollBarWidth + 8;
        int cap = Math.Max(combo.Width, Screen.FromControl(this).WorkingArea.Width - 40);
        combo.DropDownWidth = Math.Min(width, cap);
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
        _tabControl.TabPages.Add(CreateTab("Database Search", _databaseSearchPanel)); // 15

        // When the user switches to the Raw JSON tab, sync all panel data to
        // the in-memory JsonObject first so the editor reflects current edits.
        _tabControl.SelectedIndexChanged += OnTabChanged;
    }

    /// <summary>
    /// Installs the no-save overlay and lock on every editor tab. The Export
    /// Settings tab (index 13) and the Database Search tab (index 15) are exempt
    /// because they never require a loaded save.
    /// </summary>
    private void InstallEditorLock()
    {
        const int exportSettingsTabIdx = 13;
        const int databaseSearchTabIdx = 15;

        for (int i = 0; i < _tabControl.TabPages.Count; i++)
        {
            if (i == exportSettingsTabIdx || i == databaseSearchTabIdx) continue;

            var page = _tabControl.TabPages[i];
            var content = GetTabContent(page);
            if (content == null) continue;

            var overlay = new NoSaveOverlay();
            page.Controls.Add(overlay);
            overlay.BringToFront();

            _lockedTabs.Add((content, overlay, CaptureEnabledStates(content)));
        }

        UpdateEditorLockState();
    }

    /// <summary>
    /// Locks or unlocks the editor tabs depending on whether a save file is
    /// currently loaded. While locked, every tab (except Export Settings)
    /// shows the no-save overlay and its content controls are disabled.
    /// </summary>
    private void UpdateEditorLockState()
    {
        bool locked = _currentSaveData == null;
        if (locked == _editorLocked) return;
        _editorLocked = locked;
        foreach (var (panel, overlay, states) in _lockedTabs)
        {
            overlay.Visible = locked;
            ApplyEnabledStates(panel, states, locked);
        }
    }

    /// <summary>
    /// Captures the current enabled state of every control in the given tree,
    /// so the original states can be restored when the editor lock is lifted.
    /// </summary>
    private static Dictionary<Control, bool> CaptureEnabledStates(Control root)
    {
        var map = new Dictionary<Control, bool>();
        void Walk(Control control)
        {
            map[control] = control.Enabled;
            foreach (Control child in control.Controls)
                Walk(child);
        }
        Walk(root);
        return map;
    }

    /// <summary>
    /// Applies the lock state to a control tree. When locking, every control is
    /// disabled. When unlocking, each control is restored to its captured state;
    /// controls created after capture (e.g. dynamically built rows) are enabled.
    /// </summary>
    private static void ApplyEnabledStates(Control root, Dictionary<Control, bool> states, bool locked)
    {
        void Walk(Control control)
        {
            control.Enabled = locked ? false : (states.TryGetValue(control, out bool original) ? original : true);
            foreach (Control child in control.Controls)
                Walk(child);
        }
        Walk(root);
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

        // Purge the Catalogue panel when navigating away from it. CataloguePanel is one of
        // the heaviest panels (DataGridViews with icon images + scaled icon bitmap cache)
		// and it is safe to reload on demand via the deferred loading path below.
        const int catalogueTabIdx = 8;
        if (_prevTabIndex == catalogueTabIdx && idx != catalogueTabIdx
            && _loadedTabIndices.Contains(catalogueTabIdx))
        {
            // Sync any pending changes before clearing the rows.
            _cataloguePanel.SaveData(_currentSaveData);
            _cataloguePanel.PurgeData();
            // Remove from loaded set so the deferred loader rehydrates it next visit.
            _loadedTabIndices.Remove(catalogueTabIdx);
        }
        _prevTabIndex = idx;

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
        // (but not during GoToJson navigation - the handler does this itself)
        if (!_isGoToJsonNavigation
            && _tabControl.SelectedTab?.Controls.Count > 0
            && _tabControl.SelectedTab.Controls[0] == _rawJsonPanel)
        {
            SyncAllPanelData();
            _rawJsonPanel.RefreshTree(_currentSaveData);
        }
    }

    private void OnExosuitCrossInventoryTransferCompleted(object? sender, EventArgs e)
    {
        if (_currentSaveData == null)
            return;

        // Flush pending panel edits into the JSON before the panels are reloaded.
        SyncAllPanelData();

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

        // Flush pending panel edits into the JSON before the panels are reloaded.
        SyncAllPanelData();

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
    /// Handles GOTO JSON requests from panels. Shows confirmation, syncs all panel data,
    /// switches to the Raw JSON Editor tab, and navigates to the requested path.
    /// </summary>
    private void OnGoToJsonRequested(object? sender, GoToJsonEventArgs e)
    {
        if (_currentSaveData == null) return;

        var result = MessageBox.Show(this,
            UiStrings.Get("common.goto_json_confirm"),
            UiStrings.Get("common.goto_json_title"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        SyncAllPanelData();

        _isGoToJsonNavigation = true;
        _tabControl.SelectedIndex = 14;
        _isGoToJsonNavigation = false;

        _rawJsonPanel.RefreshTree(_currentSaveData);
        _rawJsonPanel.NavigateToPath(e.PathSegments);
    }

    /// <summary>
    /// Returns the content panel inside a tab page, or null if the page is null/empty.
    /// Skips the no-save overlay so callers always operate on the actual panel
    /// (the overlay sits at the front of the tab page's z-order after being
    /// brought to the front by <see cref="InstallEditorLock"/>).
    /// </summary>
    private static Control? GetTabContent(TabPage? page)
    {
        if (page == null || page.Controls.Count == 0)
            return null;
        foreach (Control control in page.Controls)
        {
            if (control is not NoSaveOverlay)
                return control;
        }
        return null;
    }

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
                _cataloguePanel.SetAccountData(_accountPanel.AccountData);
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

    /// <summary>The OS-detected default NMS save directories, in preference order (cached on first load).</summary>
    private List<string> _defaultSaveDirectories = new();

    private void LoadConfig()
    {
        var config = AppConfig.Instance;
        config.Initialize();

        if (config.MainFrameWidth > 0 && config.MainFrameHeight > 0)
        {
            // Restore the saved bounds, but only when they are still on a connected screen.
            // StartPosition must be Manual or WinForms recomputes (and discards) the location
            // when the form is first shown.
            var bounds = new Rectangle(config.MainFrameX, config.MainFrameY,
                config.MainFrameWidth, config.MainFrameHeight);
            bool onScreen = Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(bounds));
            if (onScreen)
            {
                StartPosition = FormStartPosition.Manual;
                Location = bounds.Location;
                Size = bounds.Size;
            }
        }

        // Detect every OS default save directory (Steam/GOG profiles and Game Pass containers
        // can coexist; all of them are offered rather than just the first).
        _defaultSaveDirectories = SaveFileManager.FindDefaultSaveDirectories();

        // Load recent directories from config
        var recent = config.RecentDirectories;
        bool changed = false;

        // If no recent directories exist, seed with either LastDirectory or the first default
        if (recent.Count == 0)
        {
            string? initial = config.LastDirectory ?? _defaultSaveDirectories.FirstOrDefault();
            if (initial != null)
            {
                recent.Add(initial);
                changed = true;
            }
        }

        // Ensure every detected default directory is present in the list
        foreach (var def in _defaultSaveDirectories)
        {
            if (!recent.Any(d => string.Equals(d, def, PathComparison)))
            {
                recent.Add(def);
                changed = true;
            }
        }

        if (changed)
        {
            config.RecentDirectories = recent;
            config.Save();
        }

        // Populate the directory dropdown
        string? lastDir = config.LastDirectory;
        RebuildDirectoryDropdown(recent, lastDir);

        // Populate backup path combo with saved recent paths.
        // Guarded so a stale or unreadable backup path in the config cannot
        // prevent the application from starting.
        try
        {
            RebuildBackupDropdown(config);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Backup dropdown rebuild failed: {ex.Message}");
        }
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

        // Prefer the requested directory, but skip stale entries that no longer exist so a
        // moved or deleted path cannot leave the slot list empty while valid defaults exist.
        int index = -1;
        if (!string.IsNullOrEmpty(selectedDir))
        {
            for (int i = 0; i < _directoryCombo.Items.Count; i++)
            {
                if (string.Equals(_directoryCombo.Items[i]?.ToString(), selectedDir, PathComparison))
                {
                    index = i;
                    break;
                }
            }
        }
        if (index < 0 || !Directory.Exists(_directoryCombo.Items[index]?.ToString()))
        {
            index = -1;
            for (int i = 0; i < _directoryCombo.Items.Count; i++)
            {
                if (Directory.Exists(_directoryCombo.Items[i]?.ToString()))
                {
                    index = i;
                    break;
                }
            }
        }
        if (index >= 0)
            _directoryCombo.SelectedIndex = index;
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
            ShipCustomisationDatabase.LoadFromFile(Path.Combine(jsonPath, "Ship Customisation.json"));
            NmsColourPalette.LoadShipPalettes(Path.Combine(jsonPath, "Colour Palettes.json"));
            PetBattleMoveDatabase.LoadFromFile(Path.Combine(jsonPath, "Pet Battle Moves.json"));
            PetBattleMovesetDatabase.LoadFromFile(Path.Combine(jsonPath, "Pet Battle Movesets.json"));
            PetBiomeAffinityMap.LoadFromFile(Path.Combine(jsonPath, "Game Table Globals.json"));
            CompanionDatabase.LoadFromFile(Path.Combine(jsonPath, "Creature Species.json"));
            CreaturePartDatabase.LoadFromFile(Path.Combine(jsonPath, "Creature Descriptors.json"));
            SpacePoiTableDatabase.LoadFromFile(Path.Combine(jsonPath, "Space POI.json"));

            // Refresh companion panel species list now that the database has been loaded
            // (the panel constructor runs before data loading, so the combo is initially empty)
            _companionPanel.RefreshSpeciesList();

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
            ProcTechLogic.SetLanguageDirectory(langDir);

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
            _cataloguePanel.SetCatalogueDatabase(new CatalogueDatabase(
                Path.Combine(basePath, "Resources", "json")));
            _settlementPanel.SetDatabase(_database);
            _fleetPanel.SetDatabase(_database);
            _basePanel.SetDatabase(_database);
            _databaseSearchPanel.SetDatabase(_database);

            _exosuitPanel.SetIconManager(_iconManager);
            _shipPanel.SetIconManager(_iconManager);
            _multitoolPanel.SetIconManager(_iconManager);
            _vehiclePanel.SetIconManager(_iconManager);
            _cataloguePanel.SetIconManager(_iconManager);
            _milestonePanel.SetIconManager(_iconManager);
            _settlementPanel.SetIconManager(_iconManager);
            _basePanel.SetIconManager(_iconManager);
            _fleetPanel.SetIconManager(_iconManager);
            _databaseSearchPanel.SetIconManager(_iconManager);

            _accountPanel.SetDatabase(_database);
            _accountPanel.SetIconManager(_iconManager);
            _mainStatsPanel.SetIconManager(_iconManager);

            // Load rewards database for Account panel (from Rewards.json, falls back to inline static data)
            _splashForm?.SetProgress(85, "Loading rewards data...");
            _accountPanel.LoadRewardsDatabase(jsonPath);

            // Wire up Recipe panel with databases
            _recipePanel.SetDatabases(_recipeDatabase, _database);
            _recipePanel.SetIconManager(_iconManager);
            _cataloguePanel.SetRecipeDatabase(_recipeDatabase);

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
            // Populate the Export Settings fields now so they are never blank,
            // including when no save is open (the deferred tab loader needs a save).
            _exportConfigPanel.LoadConfig();

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
        _xboxSavePath = null;
        _ps4MemoryDatPath = null;
        _ps4NomanSkyPath = null;
        _platformSlotIdentifiers = null;
        _xboxFileIdentifiers = null;
        _ps4SubSlotIndices = null;

        if (_directoryCombo.SelectedItem is not string dir || !Directory.Exists(dir))
            return;

        // Tolerate parent folders (the Packages folder, a package folder, SystemAppData,
        // xgs/wgs or the NMS profile root) by resolving to the actual container or profile
        // before detecting the platform and enumerating slots.
        string resolvedDir = SaveFileManager.ResolveSaveDirectory(dir);
        if (!string.Equals(resolvedDir, dir, PathComparison))
            dir = resolvedDir;

        _detectedPlatform = SaveFileManager.DetectPlatform(dir);

        // Inform the account panel which platform is active so it can
        // show/hide MXML controls (MXML is only relevant for PC platforms).
        _accountPanel.SetPlatform(_detectedPlatform);

        // -- Xbox Game Pass: xgs (XGameSaveFiles) or legacy wgs (containers.index) --
        if (_detectedPlatform == SaveFileManager.Platform.XboxGamePass)
        {
            if (SaveFileManager.DetectXboxSaveFormat(dir) != SaveFileManager.XboxSaveFormat.None)
            {
                _xboxSavePath = dir;
                try
                {
                    var xboxSlots = SaveFileManager.EnumerateXboxSlots(dir);
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

                        // Sort so Auto comes before Manual (save.hg is auto, save2.hg is manual)
                        entries.Sort((a, b) =>
                        {
                            bool aIsAuto = a.Identifier.Contains("Auto", StringComparison.OrdinalIgnoreCase);
                            bool bIsAuto = b.Identifier.Contains("Auto", StringComparison.OrdinalIgnoreCase);
                            return bIsAuto.CompareTo(aIsAuto);
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
                        _platformSlotIdentifiers.Add(slotNum.ToString(CultureInfo.InvariantCulture));

                        // Detect save name and difficulty from the manual save first (last in
                        // sorted order), falling back to the auto save.  User-assigned names
                        // are written to the manual save, so reading from the auto save would
                        // show a stale name when the player has renamed their slot.
                        string saveName = "";
                        string difficulty = "";
                        string? xboxLabelFilePath = null;
                        foreach (var (_, info) in Enumerable.Reverse(entries))
                        {
                            if (info.DataFilePath != null && File.Exists(info.DataFilePath))
                            {
                                xboxLabelFilePath = info.DataFilePath;
                                saveName = DetectSaveName(info.DataFilePath);
                                difficulty = DetectDifficulty(info.DataFilePath);
                                if (!string.IsNullOrEmpty(saveName)) break;
                            }
                        }

                        string? xboxExpeditionTag = (xboxLabelFilePath != null
                            && SaveFileManager.DetectActiveContextFast(xboxLabelFilePath, out bool xboxIsExp)
                            && xboxIsExp) ? UiStrings.Get("slot.expedition") : null;
                        string label = BuildSlotLabel($"Xbox: Slot {slotNum}", saveName, difficulty, xboxExpeditionTag);
                        _saveSlotCombo.Items.Add(label);
                    }

                    // Load Xbox AccountData as the platform equivalent of accountdata.hg
                    if (xboxSlots.TryGetValue(ContainersIndexManager.AccountDataIdentifier, out var accountSlot))
                    {
                        _accountPanel.LoadXboxAccountData(accountSlot);
                        _rawJsonPanel.CaptureAccountBaseline(_accountPanel.AccountData);
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
            _ps4SubSlotIndices = new List<List<int>>();

            // PS4 supports 5 game slots; each has an auto (sub-slot 2N-1) and a manual (sub-slot 2N).
            // Sub-slot 0 is account data and is not listed here.
            const int PS4MaxGameSlots = 5;
            const int MinimumSlotDataLength = 10;
            for (int n = 1; n <= PS4MaxGameSlots; n++)
            {
                int autoIdx   = 2 * n - 1;
                int manualIdx = 2 * n;

                string? autoJson   = null;
                string? manualJson = null;
                try { autoJson   = MemoryDatManager.ExtractSlotData(memoryDatPath, autoIdx);   } catch { }
                try { manualJson = MemoryDatManager.ExtractSlotData(memoryDatPath, manualIdx); } catch { }

                bool hasAuto   = autoJson   != null && autoJson.Length   > MinimumSlotDataLength;
                bool hasManual = manualJson != null && manualJson.Length > MinimumSlotDataLength;
                if (!hasAuto && !hasManual) continue;

                var subSlots  = new List<int>();
                var slotFiles = new List<string>();
                if (hasAuto)   { subSlots.Add(autoIdx);   slotFiles.Add(memoryDatPath); }
                if (hasManual) { subSlots.Add(manualIdx); slotFiles.Add(memoryDatPath); }

                _ps4SubSlotIndices.Add(subSlots);
                _saveSlotFiles.Add(slotFiles);
                _platformSlotIdentifiers.Add(n.ToString(CultureInfo.InvariantCulture));

                // Prefer the manual save for slot label (user-assigned names live there),
                // falling back to auto if manual is absent.
                string labelJson = hasManual ? manualJson! : autoJson!;
                string saveName  = "";
                string difficulty = "";
                try
                {
                    saveName  = SaveFileManager.DetectSaveNameFromJson(labelJson);
                    int mode  = SaveFileManager.DetectGameModeFromJson(labelJson);
                    if (mode > 0) difficulty = GameModeToString(mode);
                }
                catch { }

                SaveFileManager.DetectActiveContextFromJson(labelJson, out bool ps4IsExpedition);
                string? ps4ExpeditionTag = ps4IsExpedition ? UiStrings.Get("slot.expedition") : null;
                string label = BuildSlotLabel($"PS4: Slot {n}", saveName, difficulty, ps4ExpeditionTag);
                _saveSlotCombo.Items.Add(label);
            }
        }
        else
        {
            // -- Steam/GOG/PS4 streaming/Switch: file-based saves --
            var saveFiles = new List<List<string>>();
            for (int i = 0; i < 15; i++)
            {
                // save.hg (slot 0 auto), save2.hg (slot 0 manual),
                // save3.hg (slot 1 auto), save4.hg (slot 1 manual), etc.
                string autoSave = i == 0 ? "save.hg" : $"save{i * 2 + 1}.hg";
                string manualSave = $"save{i * 2 + 2}.hg";

                string autoPath = Path.Combine(dir, autoSave);
                string manualPath = Path.Combine(dir, manualSave);

                bool hasAuto = File.Exists(autoPath);
                bool hasManual = File.Exists(manualPath);

                if (hasAuto || hasManual)
                {
                    var slotFiles = new List<string>();
                    if (hasAuto) slotFiles.Add(autoPath);
                    if (hasManual) slotFiles.Add(manualPath);

                    saveFiles.Add(slotFiles);
                    // Prefer the manual save (last entry) for the slot label because
                    // user-assigned names are written there.  The auto save retains
                    // the name from when the game last auto-saved, which may be stale.
                    // slotFiles is guaranteed non-empty by the hasAuto || hasManual guard above.
                    string labelFile = slotFiles[^1];
                    string difficulty = DetectDifficulty(labelFile);
                    string saveName = DetectSaveName(labelFile);
                    SaveFileManager.DetectActiveContextFast(labelFile, out bool pcIsExpedition);
                    string? pcExpeditionTag = pcIsExpedition ? UiStrings.Get("slot.expedition") : null;
                    string label = BuildSlotLabel($"Slot {i + 1}", saveName, difficulty, pcExpeditionTag);
                    _saveSlotCombo.Items.Add(label);
                }
            }

            // PS4 HTOS / Switch format: savedata00.hg is not a game slot (settings on
            // Switch, account data on PS4); game slots start at savedata02.hg.
            // Pair savedata{N*2+2}.hg (auto) + savedata{N*2+3}.hg (manual) for slot N (0-based).
            if (_saveSlotCombo.Items.Count == 0 &&
                (_detectedPlatform == SaveFileManager.Platform.PS4
                 || _detectedPlatform == SaveFileManager.Platform.Switch))
            {
                string platformPrefix = _detectedPlatform == SaveFileManager.Platform.Switch
                    ? "Switch: "
                    : "PS4: ";
                for (int i = 0; i < 15; i++)
                {
                    string autoFile   = Path.Combine(dir, $"savedata{i * 2 + 2:D2}.hg");
                    string manualFile = Path.Combine(dir, $"savedata{i * 2 + 3:D2}.hg");

                    bool hasAuto   = File.Exists(autoFile);
                    bool hasManual = File.Exists(manualFile);

                    if (hasAuto || hasManual)
                    {
                        var slotFiles = new List<string>();
                        if (hasAuto)   slotFiles.Add(autoFile);
                        if (hasManual) slotFiles.Add(manualFile);

                        saveFiles.Add(slotFiles);
                        string labelFile   = slotFiles[^1];
                        string difficulty  = DetectDifficulty(labelFile);
                        string saveName    = DetectSaveName(labelFile);
                        SaveFileManager.DetectActiveContextFast(labelFile, out bool ps4MdIsExpedition);
                        string? ps4MdExpeditionTag = ps4MdIsExpedition ? UiStrings.Get("slot.expedition") : null;
                        string label       = BuildSlotLabel($"{platformPrefix}Slot {i + 1}", saveName, difficulty, ps4MdExpeditionTag);
                        _saveSlotCombo.Items.Add(label);
                    }
                }
            }

            _saveSlotFiles = saveFiles;
        }

        // Load account data (accountdata.hg for Steam/GOG/PS4; Xbox handled above)
        if (_detectedPlatform != SaveFileManager.Platform.XboxGamePass)
            _accountPanel.LoadAccountFile(dir);
        _rawJsonPanel.CaptureAccountBaseline(_accountPanel.AccountData);

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

        // PS4 memory.dat: show "Auto" / "Manual" with timestamp from slot metadata.
        bool isPs4MemoryDat = _ps4SubSlotIndices != null
            && slotIndex < _ps4SubSlotIndices.Count;

        // Read PS4 slot metadata once (for timestamps) before the per-file loop.
        MemoryDatSlot[]? ps4Slots = null;
        if (isPs4MemoryDat && _ps4MemoryDatPath != null)
        {
            try { ps4Slots = MemoryDatManager.ReadSlots(_ps4MemoryDatPath); } catch { }
        }

        // Xbox: show "Auto - {DirectoryGUID}" / "Manual - {DirectoryGUID}" labels
        bool isXbox = _xboxFileIdentifiers != null
            && slotIndex < _xboxFileIdentifiers.Count;

        for (int i = 0; i < files.Count; i++)
        {
            var filePath = files[i];
            string label;

            if (isPs4MemoryDat)
            {
                int subSlotIdx = _ps4SubSlotIndices![slotIndex][i];
                // Odd sub-slot indices are auto-saves; even are manual saves.
                bool isAuto = subSlotIdx % 2 == 1;
                string type = isAuto ? "Auto" : "Manual";

                // Append timestamp from the slot's metadata (no file-system time for memory.dat).
                string timestamp = "";
                if (ps4Slots != null && subSlotIdx < ps4Slots.Length
                    && ps4Slots[subSlotIdx].Timestamp.HasValue)
                {
                    var ts = ps4Slots[subSlotIdx].Timestamp!.Value.LocalDateTime;
                    timestamp = $" - {ts:dd/MM/yy h:mmtt}";
                    if (ts > newestTime)
                    {
                        newestTime = ts;
                        newestIndex = i;
                    }
                }

                label = $"{type}{timestamp}";
            }
            else if (isXbox)
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

                // Append file timestamp (missing blobs report the 1601 epoch, so
                // only existing files contribute a timestamp or the newest pick).
                string timestamp = "";
                try
                {
                    if (File.Exists(filePath))
                    {
                        var lastWrite = File.GetLastWriteTime(filePath);
                        timestamp = $" - {lastWrite:dd/MM/yy h:mmtt}";
                        if (lastWrite > newestTime)
                        {
                            newestTime = lastWrite;
                            newestIndex = i;
                        }
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
                {
                    // PS4 HTOS format: savedataNN.hg where even N = auto, odd N = manual.
                    string numPart = fileName
                        .Replace("savedata", "", StringComparison.OrdinalIgnoreCase)
                        .Replace(".hg", "", StringComparison.OrdinalIgnoreCase);
                    if (int.TryParse(numPart, System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture, out int num))
                        suffix = num % 2 == 0 ? " (Auto)" : " (Manual)";
                    else
                        suffix = "";
                }
                else if (fileName.Equals("save.hg", StringComparison.OrdinalIgnoreCase))
                {
                    // save.hg is the first slot's auto-save (metaIndex 2, collectionIndex 0)
                    suffix = " (Auto)";
                }
                else
                {
                    // Odd-numbered files (save3, save5, ...) are auto-saves;
                    // even-numbered files (save2, save4, save6, ...) are manual saves.
                    string numPart = fileName.Replace("save", "").Replace(".hg", "");
                    bool isAuto = int.TryParse(numPart, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out int num) && num % 2 == 1;
                    suffix = isAuto ? " (Auto)" : " (Manual)";
                }

                // Append file timestamp (missing blobs report the 1601 epoch, so
                // only existing files contribute a timestamp or the newest pick).
                string timestamp = "";
                try
                {
                    if (File.Exists(filePath))
                    {
                        var lastWrite = File.GetLastWriteTime(filePath);
                        timestamp = $" - {lastWrite:dd/MM/yy h:mmtt}";
                        if (lastWrite > newestTime)
                        {
                            newestTime = lastWrite;
                            newestIndex = i;
                        }
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
    /// Build a slot label combining prefix, save name, difficulty, and optional expedition tag.
    /// Format: "Slot N - SaveName - DIFFICULTY" or "Slot N - DIFFICULTY - EXPEDITION" or "Slot N".
    /// </summary>
    private static string BuildSlotLabel(string prefix, string saveName, string difficulty, string? expeditionTag = null)
    {
        var parts = new List<string> { prefix };
        if (!string.IsNullOrEmpty(saveName)) parts.Add(saveName);
        if (!string.IsNullOrEmpty(difficulty)) parts.Add(difficulty);
        if (!string.IsNullOrEmpty(expeditionTag)) parts.Add(expeditionTag);
        return string.Join(" - ", parts);
    }

    /// <summary>
    /// Updates the currently selected slot's label in the Save Slot dropdown to reflect
    /// the latest save name.  Called after a successful save so the label stays in sync
    /// without requiring the user to reload the directory.
    /// </summary>
    private void UpdateCurrentSlotLabel()
    {
        int slotIdx = _saveSlotCombo.SelectedIndex;
        if (slotIdx < 0 || _currentSaveData == null) return;

        // Extract the save name directly from the in-memory JSON (already synced
        // by SyncAllPanelData() before the write).
        string saveName = "";
        try
        {
            saveName = _currentSaveData.GetObject("CommonStateData")?.GetString("SaveName") ?? "";
        }
        catch { }

        // Derive the slot prefix from the existing label (everything before the first " - ")
        // so we don't have to reconstruct platform/slot-number logic here.
        string existingLabel = _saveSlotCombo.Items[slotIdx]?.ToString() ?? "";
        string prefix = existingLabel.Contains(" - ")
            ? existingLabel[..existingLabel.IndexOf(" - ", StringComparison.Ordinal)]
            : existingLabel;

        // Re-detect difficulty and expedition from the current file (cheap fast scan).
        // For Xbox the tracked path is containers.index, so scan the slot's data blob instead.
        string? scanPath = _currentFilePath;
        if (_xboxSavePath != null && _xboxFileIdentifiers != null)
        {
            int fileIdx = _saveFileCombo.SelectedIndex;
            if (slotIdx < _saveSlotFiles.Count && fileIdx >= 0 && fileIdx < _saveSlotFiles[slotIdx].Count)
            {
                string xboxPath = _saveSlotFiles[slotIdx][fileIdx];
                if (!string.IsNullOrEmpty(xboxPath)) scanPath = xboxPath;
            }
        }

        string difficulty = scanPath != null ? DetectDifficulty(scanPath) : "";

        string? expeditionTag = null;
        if (scanPath != null)
        {
            SaveFileManager.DetectActiveContextFast(scanPath, out bool isExpedition);
            if (isExpedition) expeditionTag = UiStrings.Get("slot.expedition");
        }

        string newLabel = BuildSlotLabel(prefix, saveName, difficulty, expeditionTag);

        // Swap the item without triggering a SelectedIndexChanged repopulation.
        _saveSlotCombo.Items[slotIdx] = newLabel;
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
            SaveFileManager.TryDetectActiveContext(_currentSaveData);
            string? saveDir = Path.GetDirectoryName(filePath);

            // If the file was loaded directly (Open File), update the toolbar to reflect it.
            // This must come before the NOMANSKY check below because it may call
            // PopulateSaveSlots() which resets _ps4NomanSkyPath.
            UpdateToolbarForLoadedFile(filePath);

            // Track PS4 SaveWizard streaming files (NOMANSKY header .hg) for correct save-back.
            // Must come after UpdateToolbarForLoadedFile() which may reset this field.
            if (_detectedPlatform == SaveFileManager.Platform.PS4 && _ps4MemoryDatPath == null)
            {
                _ps4NomanSkyPath = SaveFileManager.IsNomanSkyFile(filePath) ? filePath : null;
            }
            else
            {
                _ps4NomanSkyPath = null;
            }

            // Update panels - only load the currently active tab eagerly.
            // Other tabs are loaded on first selection (deferred loading).
            // Hide the active panel content while loading to suppress painting
            // of intermediate states (same technique as OnTabChanged).
            _progressBar.Value = 80;
            _loadedTabIndices.Clear();

            // Lift the editor lock before loading the active tab so its panels set
            // their enabled states last (restoring the startup snapshot here would
            // re-disable controls the panels have just enabled).
            UpdateEditorLockState();

            int activeTab = _tabControl.SelectedIndex;
            var activeContent = GetTabContent(activeTab >= 0 ? _tabControl.TabPages[activeTab] : null);

            if (activeContent != null) activeContent.Visible = false;
            SuspendLayout();
            try
            {
                // Always load account data early (needed by MainStats and Raw JSON)
                if (saveDir != null) _accountPanel.LoadAccountFile(saveDir);
                _rawJsonPanel.CaptureAccountBaseline(_accountPanel.AccountData);
                _rawJsonPanel.SetSaveFilePath(filePath);
                _rawJsonPanel.SetAccountData(_accountPanel.AccountData, _accountPanel.AccountFilePath);
                // Capture the diff baseline now, before any panel LoadData is called.
                // Some panels (e.g. companion panel) write changes directly to the JSON
                // object the moment the user interacts with a control, so if baseline
                // capture is deferred to the first Raw JSON Editor tab visit it may
                // already include panel side changes, causing "Show Changes" to miss
				// them in the diff.
                _rawJsonPanel.CaptureBaseline(_currentSaveData);

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
            UpdateEditorLockState();
            EnableMenuItems();

            _statusLabel.Text = UiStrings.Format("status.loaded_save", Path.GetFileName(filePath), loadTimer.ElapsedMilliseconds.ToString("N0", CultureInfo.CurrentCulture));
            _rawJsonPanel.CaptureSavedBaseline(_currentSaveData, _accountPanel.AccountData);
            _hasUnsavedChanges = false;
            SetupExternalWatcher(filePath);
        }
        catch (Exception ex)
        {
            _progressBar.Visible = false;
            MessageBox.Show(this, UiStrings.Format("dialog.failed_load_save", ex.Message), UiStrings.Get("dialog.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel.Text = UiStrings.Get("status.failed_load_save");
            UpdateEditorLockState();
        }
    }

    /// <summary>
    /// Starts watching the loaded save file, its meta file and the account data file for
    /// changes made outside the editor. Xbox saves are watched through the loaded slot's
    /// data/meta blob files, the AccountData blob and the legacy wgs index instead of a
    /// single save file. Creates one watcher per distinct directory (the macOS account MXML
    /// lives in a sibling SETTINGS folder).
    /// </summary>
    /// <param name="saveFilePath">The loaded save file path (unused for Xbox containers).</param>
    private void SetupExternalWatcher(string? saveFilePath)
    {
        DisposeExternalWatcher();
        _externalDiverged = false;
        _externalWatchedFiles.Clear();

        if (_detectedPlatform == SaveFileManager.Platform.XboxGamePass)
        {
            AddXboxWatchedFiles();
        }
        else
        {
            if (string.IsNullOrEmpty(saveFilePath)) return;

            string? directory = Path.GetDirectoryName(saveFilePath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return;

            _externalWatchedFiles.Add(saveFilePath);
            _externalWatchedFiles.Add(Path.Combine(directory, "mf_" + Path.GetFileName(saveFilePath)));
            if (!string.IsNullOrEmpty(_accountPanel.AccountFilePath))
                _externalWatchedFiles.Add(_accountPanel.AccountFilePath);
        }

        if (_externalWatchedFiles.Count == 0) return;
        SnapshotExternalFileTimes();

        foreach (string watchedDirectory in _externalWatchedFiles
                     .Select(Path.GetDirectoryName)
                     .OfType<string>()
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var watcher = new FileSystemWatcher(watchedDirectory)
                {
                    // Raise events on the UI thread: the debounce timer, the reload prompt and
                    // the watched-file bookkeeping all assume the UI thread. Set before the
                    // watcher starts raising events.
                    SynchronizingObject = this,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                    IncludeSubdirectories = false,
                    InternalBufferSize = 64 * 1024,
                    EnableRaisingEvents = true
                };
                watcher.Changed += OnExternalFileEvent;
                watcher.Created += OnExternalFileEvent;
                watcher.Deleted += OnExternalFileEvent;
                watcher.Renamed += OnExternalFileEvent;
                _externalWatchers.Add(watcher);
            }
            catch
            {
                // Ignore directories that cannot be watched.
            }
        }
    }

    /// <summary>
    /// Adds the loaded Xbox slot's data and meta files, the AccountData blob (part of the
    /// same save) and the legacy wgs containers.index to the watched set.
    /// </summary>
    private void AddXboxWatchedFiles()
    {
        if (_xboxSavePath == null) return;

        string? slotId = GetCurrentXboxSlotIdentifier();
        if (slotId != null)
        {
            var (dataFile, metaFile) = SaveFileManager.GetXboxSlotFiles(_xboxSavePath, slotId);
            if (!string.IsNullOrEmpty(dataFile)) _externalWatchedFiles.Add(dataFile);
            if (!string.IsNullOrEmpty(metaFile)) _externalWatchedFiles.Add(metaFile);
        }

        // Account data is part of the same save: watch its data blob too.
        var (accountDataFile, _) = SaveFileManager.GetXboxSlotFiles(
            _xboxSavePath, ContainersIndexManager.AccountDataIdentifier);
        if (!string.IsNullOrEmpty(accountDataFile)) _externalWatchedFiles.Add(accountDataFile);

        // The wgs index is rewritten whenever a blob is added or replaced.
        string indexPath = Path.Combine(_xboxSavePath, "containers.index");
        if (File.Exists(indexPath)) _externalWatchedFiles.Add(indexPath);
    }

    /// <summary>
    /// Reloads the Xbox AccountData blob from the loaded container. The save slot and the
    /// account data are treated as one save, so an external change to either reloads both.
    /// </summary>
    private void ReloadXboxAccountData()
    {
        if (_xboxSavePath == null) return;

        try
        {
            var slots = SaveFileManager.EnumerateXboxSlots(_xboxSavePath);
            if (slots.TryGetValue(ContainersIndexManager.AccountDataIdentifier, out var accountSlot))
            {
                _accountPanel.LoadXboxAccountData(accountSlot);
                _rawJsonPanel.CaptureAccountBaseline(_accountPanel.AccountData);
            }
        }
        catch
        {
            // Ignore account data reload failures; the save slot reload still proceeds.
        }
    }

    /// <summary>Returns the Xbox slot identifier currently selected in the slot/file combos.</summary>
    private string? GetCurrentXboxSlotIdentifier()
    {
        if (_xboxFileIdentifiers == null) return null;

        int slotIdx = _saveSlotCombo.SelectedIndex;
        if (slotIdx < 0 || slotIdx >= _xboxFileIdentifiers.Count) return null;

        var identifiers = _xboxFileIdentifiers[slotIdx];
        if (identifiers.Count == 0) return null;

        int fileIdx = _saveFileCombo.SelectedIndex;
        if (fileIdx < 0 || fileIdx >= identifiers.Count) fileIdx = 0;
        return identifiers[fileIdx];
    }

    /// <summary>Stops and disposes all external change watchers.</summary>
    private void DisposeExternalWatcher()
    {
        foreach (var watcher in _externalWatchers)
        {
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.Changed -= OnExternalFileEvent;
                watcher.Created -= OnExternalFileEvent;
                watcher.Deleted -= OnExternalFileEvent;
                watcher.Renamed -= OnExternalFileEvent;
                watcher.Dispose();
            }
            catch
            {
                // Ignore disposal failures.
            }
        }
        _externalWatchers.Clear();
        _externalDebounceTimer?.Stop();
    }

    /// <summary>Records the current timestamps of the watched files for change comparison.</summary>
    private void SnapshotExternalFileTimes()
    {
        _externalFileTimes.Clear();
        foreach (string file in _externalWatchedFiles)
        {
            try
            {
                _externalFileTimes[file] = File.Exists(file) ? File.GetLastWriteTimeUtc(file) : DateTime.MinValue;
            }
            catch
            {
                _externalFileTimes[file] = DateTime.MinValue;
            }
        }
    }

    /// <summary>Queues a debounced external-change check (files are often written in chunks).</summary>
    private void OnExternalFileEvent(object sender, FileSystemEventArgs e)
    {
        if (!_externalWatchedFiles.Contains(e.FullPath))
            return;

        if (_externalDebounceTimer == null)
        {
            _externalDebounceTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _externalDebounceTimer.Tick += (_, _) =>
            {
                _externalDebounceTimer!.Stop();
                HandleExternalChange();
            };
        }
        _externalDebounceTimer.Stop();
        _externalDebounceTimer.Start();
    }

    /// <summary>
    /// Compares the watched files against their snapshots and reloads, prompts or marks
    /// the in-memory copy as diverged, following <see cref="ExternalChangeDecision"/>.
    /// </summary>
    private void HandleExternalChange()
    {
        if (_currentSaveData == null || _currentFilePath == null) return;
        if (DateTime.UtcNow < _externalIgnoreUntilUtc) return;

        // A prompt may already be open. The message box pumps messages, so the debounce timer
        // keeps ticking while it is shown; without this guard every save would stack another
        // box. The open prompt reloads the latest file from disk when the user answers Yes.
        if (_externalPromptOpen) return;

        bool changed = false;
        bool deleted = false;
        foreach (var (file, previousTime) in _externalFileTimes)
        {
            try
            {
                if (!File.Exists(file))
                {
                    if (previousTime != DateTime.MinValue)
                        deleted = true;
                    continue;
                }
                if (File.GetLastWriteTimeUtc(file) != previousTime)
                    changed = true;
            }
            catch
            {
                // Ignore files that cannot be inspected.
            }
        }

        // Flush pending panel edits so the unsaved-edit test sees them before deciding.
        SyncAllPanelData();

        var mode = GetExternalChangeMode();
        var decision = ExternalChangeDecision.Decide(changed, deleted, mode);
        if (decision == ExternalChangeDecision.Action.None)
            return;

        bool dirty = HasUnsavedEdits();

        if (decision == ExternalChangeDecision.Action.Reload)
        {
            // Auto reload mode: apply the change without asking.
            ReloadFromExternalChange(dirty);
            return;
        }

        // The user declined an earlier change and has not saved or reloaded since: keep the
        // diverged status but do not prompt again for every game autosave.
        if (_externalDiverged && !deleted)
        {
            _statusLabel.Text = UiStrings.Get("status.external_diverged");
            return;
        }

        string title = deleted ? UiStrings.Get("external.deleted_title") : UiStrings.Get("external.changed_title");
        string message = deleted
            ? UiStrings.Get("external.deleted_message")
            : dirty
                ? UiStrings.Get("external.changed_message")
                : UiStrings.Get("external.changed_clean_message");
        DialogResult result;
        _externalPromptOpen = true;
        try
        {
            result = MessageBox.Show(this, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        }
        finally
        {
            _externalPromptOpen = false;
        }
        if (result == DialogResult.Yes && !deleted)
        {
            ReloadFromExternalChange(dirty);
        }
        else
        {
            _externalDiverged = true;
            _statusLabel.Text = UiStrings.Get("status.external_diverged");
        }
    }

    /// <summary>Parses the configured save-file watching mode.</summary>
    private static ExternalChangeDecision.Mode GetExternalChangeMode() =>
        AppConfig.Instance.ExternalChangeMode switch
        {
            "auto" => ExternalChangeDecision.Mode.AutoReload,
            "ignore" => ExternalChangeDecision.Mode.Ignore,
            _ => ExternalChangeDecision.Mode.Prompt
        };

    /// <summary>True when the in-memory save or account data differs from the saved baseline.</summary>
    private bool HasUnsavedEdits() =>
        _currentSaveData != null
        && (!_rawJsonPanel.SaveDataMatchesSavedBaseline(_currentSaveData)
            || !_rawJsonPanel.AccountDataMatchesSavedBaseline(_accountPanel.AccountData));

    /// <summary>Reloads the current save from disk after an external change.</summary>
    /// <param name="discardedEdits">True when unsaved edits were replaced by the reload.</param>
    private void ReloadFromExternalChange(bool discardedEdits = false)
    {
        if (_currentFilePath == null) return;

        _externalIgnoreUntilUtc = DateTime.UtcNow.AddSeconds(2);
        _statusLabel.Text = discardedEdits
            ? UiStrings.Get("status.external_reloaded_discarded")
            : UiStrings.Get("status.external_reloaded");

        if (_ps4MemoryDatPath != null)
        {
            int slotIndex = _saveSlotCombo.SelectedIndex >= 0 ? _saveSlotCombo.SelectedIndex : 0;
            LoadPS4MemoryDatSaveData(_ps4MemoryDatPath, slotIndex);
        }
        else if (_detectedPlatform == SaveFileManager.Platform.XboxGamePass && _xboxSavePath != null)
        {
            // Xbox saves reload through the container/slot pipeline, not a single file.
            // Reload the account data first so the save load captures the fresh baseline
            // (the slot and account blobs are treated as one save).
            string? slotId = GetCurrentXboxSlotIdentifier();
            if (slotId != null)
            {
                ReloadXboxAccountData();
                LoadXboxSaveData(_xboxSavePath, slotId);
            }
        }
        else
        {
            LoadSaveData(_currentFilePath);
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

        // Open at the currently selected directory when it is valid, otherwise at the
        // detected default, so Xbox users are not left hunting through the Packages tree.
        string? current = _directoryCombo.SelectedItem as string;
        if (!string.IsNullOrEmpty(current) && Directory.Exists(current))
            dialog.SelectedPath = current;
        else if (_defaultSaveDirectories.Count > 0 && Directory.Exists(_defaultSaveDirectories[0]))
            dialog.SelectedPath = _defaultSaveDirectories[0];

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            RecordRecentDirectory(dialog.SelectedPath);
        }
    }

    private void RebuildBackupDropdown(AppConfig config)
    {
        _backupPathCombo.SelectedIndexChanged -= OnBackupComboChanged;
        _backupPathCombo.Items.Clear();

        var recent = config.RecentBackupDirectories;
        string resolved = SaveFileManager.ResolveBackupRoot();

        // Ensure the resolved default is always in the list
        if (!recent.Any(d => string.Equals(d, resolved, StringComparison.OrdinalIgnoreCase)))
        {
            recent.Add(resolved);
            config.RecentBackupDirectories = recent;
            config.Save();
        }

        foreach (var dir in recent)
            _backupPathCombo.Items.Add(dir);

        string? current = config.BackupDirectory ?? resolved;
        if (_backupPathCombo.Items.Contains(current))
            _backupPathCombo.SelectedItem = current;
        else if (_backupPathCombo.Items.Count > 0)
            _backupPathCombo.SelectedIndex = 0;

        _backupPathCombo.SelectedIndexChanged += OnBackupComboChanged;
    }

    private void OnBrowseBackup(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = UiStrings.Get("dialog.select_backup_dir"),
            UseDescriptionForTitle = true,
            SelectedPath = _backupPathCombo.Text
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            var config = AppConfig.Instance;
            config.AddRecentBackupDirectory(dialog.SelectedPath);
            config.Save();
            RebuildBackupDropdown(config);
        }
    }

    private void OnBackupComboChanged(object? sender, EventArgs e)
    {
        if (_backupPathCombo.SelectedItem is string path && Directory.Exists(path))
        {
            var config = AppConfig.Instance;
            config.AddRecentBackupDirectory(path);
            config.Save();
        }
    }

    private void OnBackupPathChanged(object? sender, EventArgs e)
    {
        string path = _backupPathCombo.Text.Trim();
        var config = AppConfig.Instance;

        if (string.IsNullOrEmpty(path))
        {
            config.BackupDirectory = null;
            config.Save();
            RebuildBackupDropdown(config);
            return;
        }

        if (Directory.Exists(path))
        {
            config.AddRecentBackupDirectory(path);
            config.Save();
            RebuildBackupDropdown(config);
        }
        else
        {
            // Revert to previous saved value
            string resolved = config.BackupDirectory ?? SaveFileManager.ResolveBackupRoot();
            _backupPathCombo.Text = resolved;
        }
    }

    /// <summary>Path comparison mode for the current OS.</summary>
    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    /// Records a directory as the most recently used, updates the dropdown and persists to config.
    /// Parent folders are resolved to an actual save directory first.
    /// </summary>
    private void RecordRecentDirectory(string directory)
    {
        var config = AppConfig.Instance;
        string resolved = SaveFileManager.ResolveSaveDirectory(directory);
        config.AddRecentDirectory(resolved, _defaultSaveDirectories.FirstOrDefault());
        config.Save();

        RebuildDirectoryDropdown(config.RecentDirectories, resolved);
        PopulateSaveSlots();
    }

    private void OnDirectoryComboChanged(object? sender, EventArgs e)
    {
        if (_directoryCombo.SelectedItem is string dir)
        {
            var config = AppConfig.Instance;
            config.AddRecentDirectory(dir, _defaultSaveDirectories.FirstOrDefault());
            config.Save();

            RebuildDirectoryDropdown(config.RecentDirectories, dir);
        }

        PopulateSaveSlots();
    }

    private void OnLoadSlot(object? sender, EventArgs e)
    {
        int slotIndex = _saveSlotCombo.SelectedIndex;

        // Xbox containers.index loading - use file combo to pick Auto vs Manual
        if (_xboxSavePath != null && _xboxFileIdentifiers != null
            && slotIndex >= 0 && slotIndex < _xboxFileIdentifiers.Count)
        {
            int fileIndex = _saveFileCombo.SelectedIndex;
            var identifiers = _xboxFileIdentifiers[slotIndex];
            if (fileIndex < 0 || fileIndex >= identifiers.Count)
                fileIndex = 0;
            string slotId = identifiers[fileIndex];
            LoadXboxSaveData(_xboxSavePath, slotId);
            return;
        }

        // PS4 memory.dat loading: use file combo to pick Auto vs Manual sub-slot
        if (_ps4MemoryDatPath != null && _ps4SubSlotIndices != null
            && slotIndex >= 0 && slotIndex < _ps4SubSlotIndices.Count)
        {
            var subSlots = _ps4SubSlotIndices[slotIndex];
            int fileIndex = _saveFileCombo.SelectedIndex;
            if (fileIndex < 0 || fileIndex >= subSlots.Count) fileIndex = 0;
            int memSlot = subSlots[fileIndex];
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
                UpdateEditorLockState();
                return;
            }

            _currentFilePath = containersIndexPath; // Track the containers.index path
            SaveFileManager.TryDetectActiveContext(_currentSaveData);

            _progressBar.Value = 80;
            _loadedTabIndices.Clear();

            // Lift the editor lock before loading the active tab (see LoadSaveData).
            UpdateEditorLockState();

            int activeTab = _tabControl.SelectedIndex;
            var activeContent = GetTabContent(activeTab >= 0 ? _tabControl.TabPages[activeTab] : null);
            if (activeContent != null) activeContent.Visible = false;
            SuspendLayout();
            try
            {
                // Account data for Xbox is already loaded in PopulateSaveSlots via LoadXboxAccountData
                _rawJsonPanel.SetSaveFilePath(containersIndexPath);
                _rawJsonPanel.SetAccountData(_accountPanel.AccountData, _accountPanel.AccountFilePath);
                // Capture the diff baseline before any panel LoadData is called (see LoadSaveData).
                _rawJsonPanel.CaptureBaseline(_currentSaveData);
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
            UpdateEditorLockState();
            EnableMenuItems();
            _statusLabel.Text = UiStrings.Format("status.loaded_xbox", slotId, loadTimer.ElapsedMilliseconds.ToString("N0", CultureInfo.CurrentCulture));
            _rawJsonPanel.CaptureSavedBaseline(_currentSaveData, _accountPanel.AccountData);
            _hasUnsavedChanges = false;
            SetupExternalWatcher(null);
        }
        catch (Exception ex)
        {
            _progressBar.Visible = false;
            MessageBox.Show(this, UiStrings.Format("dialog.failed_load_xbox", ex.Message), UiStrings.Get("dialog.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel.Text = UiStrings.Get("status.failed_load_xbox");
            UpdateEditorLockState();
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
                UpdateEditorLockState();
                return;
            }

            _currentFilePath = memoryDatPath; // Track memory.dat path
            SaveFileManager.TryDetectActiveContext(_currentSaveData);

            _progressBar.Value = 80;
            _loadedTabIndices.Clear();

            // Lift the editor lock before loading the active tab (see LoadSaveData).
            UpdateEditorLockState();

            int activeTab = _tabControl.SelectedIndex;
            var activeContent = GetTabContent(activeTab >= 0 ? _tabControl.TabPages[activeTab] : null);
            if (activeContent != null) activeContent.Visible = false;
            SuspendLayout();
            try
            {
                string? saveDir = Path.GetDirectoryName(memoryDatPath);
                if (saveDir != null) _accountPanel.LoadAccountFile(saveDir);
                _rawJsonPanel.CaptureAccountBaseline(_accountPanel.AccountData);
                _rawJsonPanel.SetSaveFilePath(memoryDatPath);
                _rawJsonPanel.SetAccountData(_accountPanel.AccountData, _accountPanel.AccountFilePath);
                // Capture the diff baseline before any panel LoadData is called (see LoadSaveData).
                _rawJsonPanel.CaptureBaseline(_currentSaveData);
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
            UpdateEditorLockState();
            EnableMenuItems();
            _statusLabel.Text = UiStrings.Format("status.loaded_ps4", slotIndex, loadTimer.ElapsedMilliseconds.ToString("N0", CultureInfo.CurrentCulture));
            _rawJsonPanel.CaptureSavedBaseline(_currentSaveData, _accountPanel.AccountData);
            _hasUnsavedChanges = false;
            SetupExternalWatcher(memoryDatPath);
        }
        catch (Exception ex)
        {
            _progressBar.Visible = false;
            MessageBox.Show(this, UiStrings.Format("dialog.failed_load_ps4", ex.Message), UiStrings.Get("dialog.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel.Text = UiStrings.Get("status.failed_load_ps4");
            UpdateEditorLockState();
        }
    }

    /// <summary>Enable File/Edit/Tools menu items after loading a save.</summary>
    private void EnableMenuItems()
    {
        if (_menuStrip.Items.Count > 0 && _menuStrip.Items[0] is ToolStripMenuItem fileMenu)
            foreach (ToolStripItem item in fileMenu.DropDownItems)
                if (item is ToolStripMenuItem mi && (mi.Text?.StartsWith("&Save", StringComparison.Ordinal) == true || mi.Text?.StartsWith("Save", StringComparison.Ordinal) == true))
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

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            LoadSaveData(dialog.FileName);
        }
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (_currentSaveData == null || _currentFilePath == null) return;

        try
        {
            // Force any focused text field to lose focus, triggering its Leave handler
            // so that pending edits are committed before we sync panel data.
            this.ActiveControl = null;

            // Ignore external-change events caused by our own writes.
            _externalIgnoreUntilUtc = DateTime.UtcNow.AddSeconds(5);

            // Sync all panel data to in-memory JsonObjects
            SyncAllPanelData();

            // Backup the save directory before writing (always, not just when changes detected)
            string backupRoot = "";
            string? saveDir = Path.GetDirectoryName(_currentFilePath);
            if (saveDir != null)
            {
                try
                {
                    SaveFileManager.BackupSaveDirectory(saveDir);
                    backupRoot = SaveFileManager.ResolveBackupRoot();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Backup failed: {ex.Message}");
                    MessageBox.Show(this, $"Backup failed: {ex.Message}", "Debug",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            // Xbox Game Pass saves use a completely different save pipeline:
            // data goes to blob directories, not directly to containers.index.
            if (_detectedPlatform == SaveFileManager.Platform.XboxGamePass
                && _xboxSavePath != null
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
                    var savedSlot = SaveFileManager.SaveXboxSave(_xboxSavePath, slotId, _currentSaveData);

                    // The write replaces the blob files with new GUID-named ones, so the
                    // cached path must be refreshed or the file combo reads a deleted file
                    // and shows the 1601 epoch timestamp until the directory is re-selected.
                    if (savedSlot.DataFilePath != null
                        && slotIdx < _saveSlotFiles.Count
                        && fileIdx < _saveSlotFiles[slotIdx].Count)
                    {
                        _saveSlotFiles[slotIdx][fileIdx] = savedSlot.DataFilePath;
                    }

                    // Refresh the file combo labels (timestamps) but keep the saved file selected.
                    int keepFileIdx = _saveFileCombo.SelectedIndex;
                    PopulateSaveFileCombo();
                    if (keepFileIdx >= 0 && keepFileIdx < _saveFileCombo.Items.Count)
                        _saveFileCombo.SelectedIndex = keepFileIdx;
                }

                // Save account data (season rewards, etc.) to the AccountData blob.
                // Account data uses raw LZ4 block compression, not NMS streaming.
                if (_accountPanel.AccountData != null)
                {
                    SaveFileManager.SaveXboxAccountData(_xboxSavePath, _accountPanel.AccountData);
                }

                UpdateCurrentSlotLabel();
                _statusLabel.Text = string.IsNullOrEmpty(backupRoot)
                    ? UiStrings.Format("status.save_written", Path.GetFileName(_xboxSavePath))
                    : UiStrings.Format("status.save_written_with_backup", Path.GetFileName(_xboxSavePath), backupRoot);
                _rawJsonPanel.CaptureSavedBaseline(_currentSaveData, _accountPanel.AccountData);
                _hasUnsavedChanges = false;
                SnapshotExternalFileTimes();
                _externalDiverged = false;
                MessageBox.Show(this, UiStrings.Get("dialog.save_success"), UiStrings.Get("dialog.success"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Determine slot index for meta writing
            int metaSlotIdx = _saveSlotCombo.SelectedIndex >= 0 ? _saveSlotCombo.SelectedIndex : 0;

			// PS4 SaveWizard streaming (.hg with NOMANSKY header): write back in same format.
            // Use _ps4NomanSkyPath (original file) for the header template since _currentFilePath
            // may have been changed by Save As and the new file may not exist yet.
            if (_ps4NomanSkyPath != null && _currentFilePath != null)
            {
                SaveFileManager.SaveNomanSkyFile(_ps4NomanSkyPath, _currentSaveData);

                // Rename/copy to _currentFilePath if it differs from the original.
                if (!string.Equals(_ps4NomanSkyPath, _currentFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(_ps4NomanSkyPath, _currentFilePath, overwrite: true);
                    // Update _ps4NomanSkyPath so subsequent saves target the new file.
                    _ps4NomanSkyPath = _currentFilePath;
                }

                // Write account data file to disk (if loaded). macOS account data is
                // MXML-sourced and is written by the account panel's MXML sync instead.
                if (_accountPanel.AccountData != null && _accountPanel.AccountFilePath != null
                    && !_accountPanel.IsMxmlSource)
                {
                    _accountPanel.AccountData.NameMapper ??= JsonParser.GetDefaultMapper();
                    SaveFileManager.SaveToFile(_accountPanel.AccountFilePath, _accountPanel.AccountData,
                        compress: false, writeMeta: true, platform: _detectedPlatform, slotIndex: 0);
                }

                UpdateCurrentSlotLabel();
                _statusLabel.Text = string.IsNullOrEmpty(backupRoot)
                    ? UiStrings.Format("status.save_written", Path.GetFileName(_currentFilePath))
                    : UiStrings.Format("status.save_written_with_backup", Path.GetFileName(_currentFilePath), backupRoot);
                _rawJsonPanel.CaptureSavedBaseline(_currentSaveData, _accountPanel.AccountData);
                _hasUnsavedChanges = false;
                SnapshotExternalFileTimes();
                _externalDiverged = false;
                MessageBox.Show(this, UiStrings.Get("dialog.save_success"), UiStrings.Get("dialog.success"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // PS4 HTOS saves are plain uncompressed JSON (savedata*.hg without memory.dat).
            // Writing LZ4-compressed data would produce a file the PS4 game cannot read.
            bool isPs4Htos = _detectedPlatform == SaveFileManager.Platform.PS4 && _ps4MemoryDatPath == null;
            bool compress = !isPs4Htos;

            // Write save file to disk with platform-appropriate meta
            SaveFileManager.SaveToFile(_currentFilePath!, _currentSaveData,
                compress: compress, writeMeta: true, platform: _detectedPlatform, slotIndex: metaSlotIdx);

            // Write account data file to disk (if loaded).
            // accountdata.hg is always plain JSON with a null terminator - no LZ4 compression.
            // For PS4 HTOS saves the manifest (manifest00.hg) must also be rewritten whenever
            // the account file size changes (e.g. after unlocking rewards), otherwise the PS4
            // system reads the stale size from the manifest and may reject the save.
            // (Xbox account data is handled separately via SaveXboxAccountData.)
            if (_accountPanel.AccountData != null && _accountPanel.AccountFilePath != null
                && !_accountPanel.IsMxmlSource)
            {
                if (_detectedPlatform == SaveFileManager.Platform.PS4)
                    _accountPanel.AccountData.NameMapper ??= JsonParser.GetDefaultMapper();

                bool writeAccountMeta = isPs4Htos;
                SaveFileManager.SaveToFile(_accountPanel.AccountFilePath, _accountPanel.AccountData,
                    compress: false, writeMeta: writeAccountMeta, platform: _detectedPlatform, slotIndex: 0);
            }

            _statusLabel.Text = string.IsNullOrEmpty(backupRoot)
                ? UiStrings.Format("status.save_written", Path.GetFileName(_currentFilePath!))
                : UiStrings.Format("status.save_written_with_backup", Path.GetFileName(_currentFilePath!), backupRoot);
                _rawJsonPanel.CaptureSavedBaseline(_currentSaveData, _accountPanel.AccountData);
                _hasUnsavedChanges = false;
                // Re-arm the watcher: wgs writes rotate the blob GUIDs, so the watched
                // data path must be refreshed from the updated containers.index.
                SetupExternalWatcher(null);
                MessageBox.Show(this, UiStrings.Get("dialog.save_success"), UiStrings.Get("dialog.success"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
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

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _currentFilePath = dialog.FileName;
            OnSave(sender, e);
        }
    }

    private void OnReload(object? sender, EventArgs e)
    {
        if (_currentFilePath != null)
        {
            PopulateSaveSlots();
            LoadSaveData(_currentFilePath);
        }
    }

    private void OnRestoreBackup(object? sender, EventArgs e)
    {
        if (_currentFilePath == null) return;

        string? saveDir = Path.GetDirectoryName(_currentFilePath);
        if (saveDir == null) return;

        string fileName = Path.GetFileName(_currentFilePath);

        // Search across all existing backup locations (configured, EXE-relative, TEMP)
        if (SaveFileManager.FindExistingBackupRoots().Count == 0)
        {
            MessageBox.Show(this, UiStrings.Get("dialog.no_backup_dir"), UiStrings.Get("dialog.restore_backup"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var backups = SaveFileManager.FindBackupZips(saveDir);
        if (backups.Count == 0)
        {
            MessageBox.Show(this, UiStrings.Get("dialog.no_backup_zips"), UiStrings.Get("dialog.restore_backup"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var picker = new BackupPickerDialog(backups);
        if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedZipPath == null)
            return;

        string backupPath = picker.SelectedZipPath;
        string backupName = Path.GetFileName(backupPath);

        if (!SaveFileManager.BackupContainsFile(backupPath, fileName))
        {
            MessageBox.Show(this, UiStrings.Format("dialog.restore_file_not_found", fileName, backupName), UiStrings.Get("dialog.restore_backup"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // "Restore (All)": restore every file the backup contains (save files,
        // meta files, account data) back into the save directory.
        string fileList = BuildBackupFileList(SaveFileManager.GetBackupEntryNames(backupPath), 15);
        var result = MessageBox.Show(this,
            UiStrings.Format("dialog.restore_all_confirm", fileList, backupName, File.GetCreationTime(backupPath).ToString("g", CultureInfo.CurrentCulture)),
            UiStrings.Get("dialog.restore_backup"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        try
        {
            var restored = SaveFileManager.RestoreBackupToDirectory(backupPath, saveDir);
            if (restored.Count == 0)
            {
                MessageBox.Show(this, UiStrings.Format("dialog.restore_file_not_found", fileName, backupName), UiStrings.Get("dialog.restore_backup"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ReloadCurrentSave();
            _statusLabel.Text = UiStrings.Format("status.restored_folder", restored.Count, backupName);
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

        string fileName = Path.GetFileName(_currentFilePath);

        // Search across all existing backup locations (configured, EXE-relative, TEMP)
        if (SaveFileManager.FindExistingBackupRoots().Count == 0)
        {
            MessageBox.Show(this, UiStrings.Get("dialog.no_backup_dir"), UiStrings.Get("dialog.restore_backup_single"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var backups = SaveFileManager.FindBackupZips(saveDir);
        if (backups.Count == 0)
        {
            MessageBox.Show(this, UiStrings.Get("dialog.no_backup_zips"), UiStrings.Get("dialog.restore_backup_single"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var picker = new BackupPickerDialog(backups);
        if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedZipPath == null)
            return;

        string backupPath = picker.SelectedZipPath;
        string backupName = Path.GetFileName(backupPath);

        if (!SaveFileManager.BackupContainsFile(backupPath, fileName))
        {
            MessageBox.Show(this, UiStrings.Format("dialog.restore_file_not_found", fileName, backupName), UiStrings.Get("dialog.restore_backup_single"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // "Restore (Single)": restore only the currently loaded save file.
        string fileList = $"  • {fileName}";
        var result = MessageBox.Show(this,
            UiStrings.Format("dialog.restore_single_confirm", fileList, backupName, File.GetCreationTime(backupPath).ToString("g", CultureInfo.CurrentCulture)),
            UiStrings.Get("dialog.restore_backup_single"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        try
        {
            if (!SaveFileManager.RestoreFileFromBackup(backupPath, fileName, _currentFilePath))
            {
                MessageBox.Show(this, UiStrings.Format("dialog.restore_file_not_found", fileName, backupName), UiStrings.Get("dialog.restore_backup_single"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ReloadCurrentSave();
            _statusLabel.Text = UiStrings.Format("status.restored_file", fileName, backupName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("dialog.restore_failed", ex.Message), UiStrings.Get("dialog.restore_backup_single"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Builds a bullet-pointed list of backup entry names for confirmation dialogs,
    /// capping the list at <paramref name="maxItems"/> entries.
    /// </summary>
    private static string BuildBackupFileList(List<string> entryNames, int maxItems)
    {
        var sb = new System.Text.StringBuilder();
        int count = Math.Min(maxItems, entryNames.Count);
        for (int i = 0; i < count; i++)
            sb.Append("  • ").Append(entryNames[i]).Append('\n');
        if (entryNames.Count > maxItems)
            sb.Append(UiStrings.Format("dialog.restore_more_files", entryNames.Count - maxItems));
        return sb.ToString().TrimEnd('\n');
    }

    /// <summary>
    /// Reloads the currently loaded save using the platform-appropriate loader,
    /// so a restored save is reflected in the editor. Restoring a save file and
    /// then loading it with the wrong pipeline (e.g. a container index or a
    /// memory.dat) would otherwise report a bogus load failure.
    /// </summary>
    private void ReloadCurrentSave()
    {
        if (_detectedPlatform == SaveFileManager.Platform.XboxGamePass
            && _xboxSavePath != null
            && _xboxFileIdentifiers != null)
        {
            int slotIdx = _saveSlotCombo.SelectedIndex;
            if (slotIdx >= 0 && slotIdx < _xboxFileIdentifiers.Count)
            {
                var identifiers = _xboxFileIdentifiers[slotIdx];
                int fileIdx = _saveFileCombo.SelectedIndex;
                if (fileIdx < 0 || fileIdx >= identifiers.Count)
                    fileIdx = 0;
                LoadXboxSaveData(_xboxSavePath, identifiers[fileIdx]);
                return;
            }
        }

        if (_detectedPlatform == SaveFileManager.Platform.PS4
            && _ps4MemoryDatPath != null
            && _ps4SubSlotIndices != null)
        {
            int slotIdx = _saveSlotCombo.SelectedIndex;
            if (slotIdx >= 0 && slotIdx < _ps4SubSlotIndices.Count)
            {
                var subSlots = _ps4SubSlotIndices[slotIdx];
                int fileIdx = _saveFileCombo.SelectedIndex;
                if (fileIdx < 0 || fileIdx >= subSlots.Count)
                    fileIdx = 0;
                LoadPS4MemoryDatSaveData(_ps4MemoryDatPath, subSlots[fileIdx]);
                return;
            }
        }

        if (_currentFilePath != null)
            LoadSaveData(_currentFilePath);
    }

    private void OnExportJson(object? sender, EventArgs e)
    {
        if (_currentSaveData == null) return;

        using var dialog = new SaveFileDialog
        {
            Filter = UiStrings.Get("dialog.export_json_filter"),
            Title = UiStrings.Get("dialog.export_json_title")
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
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

        if (dialog.ShowDialog(this) == DialogResult.OK)
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
                SaveFileManager.TryDetectActiveContext(_currentSaveData);

                // Capture the diff baseline before any panel LoadData is called so that
                // the baseline reflects the imported state (see LoadSaveData for details).
                _rawJsonPanel.CaptureBaseline(_currentSaveData);
                _rawJsonPanel.CaptureSavedBaseline(_currentSaveData, _accountPanel.AccountData);

                // Imported JSON is not tied to a save file on disk; stop watching the
                // previously loaded save.
                SetupExternalWatcher(null);

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
                _cataloguePanel.SetAccountData(_accountPanel.AccountData);
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

    // Tools menu bulk inventory actions

    private void OnToolsRechargeAllTech(object? sender, EventArgs e)
    {
        if (_currentSaveData == null) return;
        var playerState = _currentSaveData.GetObject("PlayerStateData");
        if (playerState == null) return;

        int count = InventoryBulkActions.RechargeAllTechnology(playerState, _database);
        if (count > 0) _hasUnsavedChanges = true;
        ReloadAllLoadedPanels();
        _statusLabel.Text = UiStrings.Format("status.recharged_all_tech", count);
    }

    private void OnToolsRefillAllStacks(object? sender, EventArgs e)
    {
        if (_currentSaveData == null) return;
        var playerState = _currentSaveData.GetObject("PlayerStateData");
        if (playerState == null) return;

        int count = InventoryBulkActions.RefillAllStacks(playerState, _database);
        if (count > 0) _hasUnsavedChanges = true;
        ReloadAllLoadedPanels();
        _statusLabel.Text = UiStrings.Format("status.refilled_all_stacks", count);
    }

    private void OnToolsRepairAllSlots(object? sender, EventArgs e)
    {
        if (_currentSaveData == null) return;
        var playerState = _currentSaveData.GetObject("PlayerStateData");
        if (playerState == null) return;

        int count = InventoryBulkActions.RepairAllSlots(playerState, _database);
        if (count > 0) _hasUnsavedChanges = true;
        ReloadAllLoadedPanels();
        _statusLabel.Text = UiStrings.Format("status.repaired_all_slots", count);
    }

    private void OnToolsRepairAllTech(object? sender, EventArgs e)
    {
        if (_currentSaveData == null) return;
        var playerState = _currentSaveData.GetObject("PlayerStateData");
        if (playerState == null) return;

        int count = InventoryBulkActions.RepairAllTechnology(playerState, _database);
        if (count > 0) _hasUnsavedChanges = true;
        ReloadAllLoadedPanels();
        _statusLabel.Text = UiStrings.Format("status.repaired_all_tech", count);
    }

    /// <summary>
    /// Reloads all currently loaded inventory panels so they reflect bulk changes
    /// made directly to the underlying JSON data by tools menu actions.
    /// Only reloads panels that have already been loaded (deferred tabs are skipped).
    /// </summary>
    private void ReloadAllLoadedPanels()
    {
        if (_currentSaveData == null) return;

        // Flush pending panel edits into the JSON before the panels are reloaded.
        SyncAllPanelData();

        // Reload inventory panels that may display modified data.
        // Panels that haven't been opened yet (not in _loadedTabIndices) will
        // pick up the changes when first loaded from the (already modified) JSON.
        if (_loadedTabIndices.Contains(1)) _exosuitPanel.LoadData(_currentSaveData);
        if (_loadedTabIndices.Contains(2)) _multitoolPanel.LoadData(_currentSaveData);
        if (_loadedTabIndices.Contains(3)) _shipPanel.LoadData(_currentSaveData);
        if (_loadedTabIndices.Contains(4)) _fleetPanel.LoadData(_currentSaveData);
        if (_loadedTabIndices.Contains(5)) _vehiclePanel.LoadData(_currentSaveData);
        if (_loadedTabIndices.Contains(7)) _basePanel.LoadData(_currentSaveData);

        // Tell the Raw JSON Editor that the underlying data has changed so its
        // cached diff is discarded and "Show Changes" recomputes on next click.
        if (_loadedTabIndices.Contains(14)) _rawJsonPanel.NotifyDataChanged();
    }

    /// <summary>
    /// Opens the project GitHub page in the default browser.
    /// </summary>
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
    /// Opens the user guide page in the default browser.
    /// </summary>
    private void OnUserGuide(object? sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = UserGuideUrl,
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
            SpacePoiTableDatabase.ApplyLocalisation(_localisationService);
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

        // Flush pending panel edits into the JSON before the panels are reloaded
        // with the new language, so unsaved edits are not discarded.
        SyncAllPanelData();

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
                        SpacePoiTableDatabase.RevertLocalisation();
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
                        SpacePoiTableDatabase.ApplyLocalisation(_localisationService);
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
    /// Sets the active application theme, re-applies it to this form, and updates
    /// the check marks on the Theme menu.
    /// </summary>
    private void SetTheme(AppTheme theme)
    {
        ThemeManager.SetTheme(theme);
        // Persist the chosen theme so it is restored on the next launch.
        AppConfig.Instance.Theme = theme.ToString();
        AppConfig.Instance.Save();
        // SetTheme fires ThemeChanged which triggers ReapplyTheme, so we don't
        // need to call ThemeApplicator.ApplyToForm here directly. But re-applying
        // is idempotent and protects against the event not being subscribed yet.
        ThemeApplicator.ApplyToForm(this);
        UpdateThemeMenuChecks();
    }

    /// <summary>
    /// Restores the theme saved in the configuration (if any) so the user's last
    /// chosen theme is applied on startup. No-op when no theme has been saved.
    /// </summary>
    private void ApplySavedTheme()
    {
        ThemeManager.ApplySavedTheme();
    }

    /// <summary>
    /// Called by the theme manager when the active theme changes. Re-applies
    /// the theme to the form and refreshes the menu check marks.
    /// </summary>
	private void ReapplyTheme()
	{
		ThemeApplicator.ApplyToForm(this);
		_mainStatsPanel.RefreshGlyphButtonImages();
		UpdateThemeMenuChecks();
	}

    /// <summary>
    /// Updates the checked state of the Theme menu items to reflect the current theme.
    /// </summary>
    private void UpdateThemeMenuChecks()
    {
        var current = ThemeManager.Current;
        _themeSystemItem.Checked = current == AppTheme.System;
        _themeLightItem.Checked = current == AppTheme.Light;
        _themeDarkItem.Checked = current == AppTheme.Dark;
    }

    /// <summary>Stores and applies the save-file watching mode.</summary>
    private void SetExternalChangeMode(string mode)
    {
        var config = AppConfig.Instance;
        config.ExternalChangeMode = mode;
        config.Save();
        UpdateWatchMenuChecks();
    }

    /// <summary>Updates the watching-mode menu check marks from the configuration.</summary>
    private void UpdateWatchMenuChecks()
    {
        string mode = AppConfig.Instance.ExternalChangeMode;
        _watchPromptItem.Checked = !string.Equals(mode, "auto", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mode, "ignore", StringComparison.OrdinalIgnoreCase);
        _watchAutoItem.Checked = string.Equals(mode, "auto", StringComparison.OrdinalIgnoreCase);
        _watchIgnoreItem.Checked = string.Equals(mode, "ignore", StringComparison.OrdinalIgnoreCase);
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
                if (toolsMenu.DropDownItems.Count >= 7)
                {
                    toolsMenu.DropDownItems[0].Text = UiStrings.Get("menu.tools.export_json");
                    toolsMenu.DropDownItems[1].Text = UiStrings.Get("menu.tools.import_json");
                    // Index 2 is separator
                    toolsMenu.DropDownItems[3].Text = UiStrings.Get("menu.tools.recharge_all_tech");
                    toolsMenu.DropDownItems[4].Text = UiStrings.Get("menu.tools.refill_all_stacks");
                    toolsMenu.DropDownItems[5].Text = UiStrings.Get("menu.tools.repair_all_slots");
                    toolsMenu.DropDownItems[6].Text = UiStrings.Get("menu.tools.repair_all_tech");
                }
            }
            // Language (use stored field reference, BCP 47 tags stay as-is)
            _languageMenu.Text = UiStrings.Get("menu.language");
            // Theme (localised labels + checked state for current selection)
            _themeMenu.Text = UiStrings.Get("theme_menu");
            _themeSystemItem.Text = UiStrings.Get("theme_system");
            _themeLightItem.Text = UiStrings.Get("theme_light");
            _themeDarkItem.Text = UiStrings.Get("theme_dark");
            UpdateThemeMenuChecks();
            // Save-file watching mode (Edit menu)
            _watchMenu.Text = UiStrings.Get("menu.edit.save_watching");
            _watchPromptItem.Text = UiStrings.Get("settings.watch.prompt");
            _watchAutoItem.Text = UiStrings.Get("settings.watch.auto");
            _watchIgnoreItem.Text = UiStrings.Get("settings.watch.ignore");
            UpdateWatchMenuChecks();
            // Help (use stored field references to avoid fragile hardcoded indices)
            _helpMenu.Text = UiStrings.Get("menu.help");
            _helpGitHubItem.Text = UiStrings.Get("menu.help.github");
            _helpUserGuideItem.Text = UiStrings.Get("menu.help.user_guide");
            _helpSponsorItem.Text = UiStrings.Get("menu.help.sponsor");
            _helpCheckUpdatesItem.Text = UiStrings.Get("menu.help.check_updates");
            _helpReleaseNotesItem.Text = UiStrings.Get("menu.help.release_notes");
            _helpAboutItem.Text = UiStrings.Get("menu.help.about");
        }

        // ---- Toolbar labels ----
        // Row 1: Directory: [0], combo [1], Browse [2], separator [3], Backup: [4], combo [5], Browse [6]
        if (_toolStrip.Items.Count >= 5)
        {
            _toolStrip.Items[0].Text = UiStrings.Get("toolbar.directory");
            _toolStrip.Items[2].Text = UiStrings.Get("toolbar.browse");
            _toolStrip.Items[4].Text = UiStrings.Get("toolbar.backup");
            _backupBrowseButton.Text = UiStrings.Get("toolbar.browse");
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
            "tab.account_rewards", "tab.export_settings", "tab.raw_json_editor",
            "tab.database_search"
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
        _databaseSearchPanel.ApplyUiLocalisation();
        _exportConfigPanel.ApplyUiLocalisation();
        _exosuitPanel.ApplyUiLocalisation();
        _companionPanel.ApplyUiLocalisation();
        _basePanel.ApplyUiLocalisation();
        _fleetPanel.ApplyUiLocalisation();
        _vehiclePanel.ApplyUiLocalisation();
        _multitoolPanel.ApplyUiLocalisation();
        _shipPanel.ApplyUiLocalisation();

        // ---- No-save overlay messages ----
        foreach (var (_, overlay, _) in _lockedTabs)
            overlay.RefreshLocalisation();
    }

    /// <summary>
    /// Opens the project sponsor page in the default browser.
    /// </summary>
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

    /// <summary>
    /// Opens the latest GitHub release page in the default browser so the
    /// user can read the release notes for the currently installed version
    /// and any earlier releases.
    /// </summary>
    private void OnReleaseNotes(object? sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = ReleaseNotesUrl,
                UseShellExecute = true
            });
        }
        catch { }
    }

    // ---- Update functionality ----

    /// <summary>
    /// The version of the currently running application, assembled from the
    /// compile-time constants <see cref="VerMajor"/>, <see cref="VerMinor"/>,
    /// and <see cref="VerPatch"/>.
    /// </summary>
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
    /// Displays the release notes for the incoming version in a scrollable panel.
    /// On acceptance, downloads the zip, applies the update, and exits.
    /// </summary>
    private async void PromptUserForUpdate(UpdateInfo update)
    {
        // If running from a cloud-synced folder, show an advisory before the
        // update prompt so the user knows they may need to relaunch manually
        // should the sync agent briefly lock the new executable during upload.
        if (UpdateService.IsInKnownSyncFolder(AppContext.BaseDirectory))
        {
            MessageBox.Show(this,
                UiStrings.Get("update.sync_folder_advisory"),
                UiStrings.Get("update.title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        DialogResult result;
        using (var dialog = new Form
        {
            Text            = UiStrings.Get("update.available"),
            FormBorderStyle = FormBorderStyle.Sizable,
            StartPosition   = FormStartPosition.CenterParent,
            Size            = new Size(540, 440),
            MinimumSize     = new Size(400, 300),
            MaximizeBox     = true,
            MinimizeBox     = false,
        })
        {
            // Header label with version info and install prompt.
            // AutoSize = true lets the label grow its height to wrap all text at any
            // DPI/font-scale setting; Dock = Top constrains the width to the dialog width.
            var headerLabel = new Label
            {
                Text      = UiStrings.Format("update.available_msg",
                                $"{VerMajor}.{VerMinor}.{VerPatch}",
                                update.RemoteVersion.ToString(3)),
                Dock      = DockStyle.Top,
                AutoSize  = true,
                Padding   = new Padding(10, 10, 10, 4),
            };

            // Scrollable release notes panel.
            // RichTextBox is used instead of TextBox because Rich Edit supports RTF
            // hyperlink fields, which lets us display #N issue references as short
            // clickable labels (e.g. "#64") while storing the full GitHub URL in the
            // hidden \fldinst instruction.
            var notesFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            // Dispose the font when the dialog is disposed, after the RichTextBox is done with it.
            dialog.Disposed += (_, _) => notesFont.Dispose();
            string notesText = UpdateService.MarkdownToPlainText(update.ReleaseNotes);
            var notesBox = new RichTextBox
            {
                Multiline   = true,
                ReadOnly    = true,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                WordWrap    = true,
                Dock        = DockStyle.Fill,
                BackColor   = SystemColors.Window,
                Font        = notesFont,
                DetectUrls  = true,
                BorderStyle = BorderStyle.None,
            };
            if (notesText.Length > 0)
            {
                // Use RTF so #N tokens display as short labels but navigate to the full URL.
                // The RTF font spec in BuildRtfWithIssueLinks takes precedence over notesBox.Font
                // for RTF content; notesFont above still applies for the fallback plain-text path.
                notesBox.Rtf = UpdateService.BuildRtfWithIssueLinks(notesText);
            }
            else
            {
                notesBox.Text = UiStrings.Get("update.no_release_notes");
            }
            // Open clicked hyperlinks in the default browser.
            // RTF \field hyperlinks fire LinkClicked with e.LinkText = the displayed label
            // (e.g. "#64"), not the hidden destination URL, so #N patterns are resolved here.
            notesBox.LinkClicked += (s, e) =>
            {
                string linkText = e.LinkText ?? string.Empty;
                string url      = linkText;

                // Resolve #N shorthand that RTF fields surface as their display label.
                var issueMatch = System.Text.RegularExpressions.Regex.Match(
                    linkText, @"^#(\d+)$");
                if (issueMatch.Success)
                    url = $"https://github.com/{UpdateService.GitHubOwner}/{UpdateService.GitHubRepo}/issues/{issueMatch.Groups[1].Value}";

                if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                    catch { /* best-effort */ }
                }
            };

            // Yes / No button row, right-aligned
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection   = FlowDirection.RightToLeft,
                AutoSize        = true,
                AutoSizeMode    = AutoSizeMode.GrowAndShrink,
                Dock            = DockStyle.Bottom,
                WrapContents    = false,
                Padding         = new Padding(8, 6, 8, 6),
            };
            var noButton = new Button
            {
                Text          = UiStrings.Get("update.prompt_no"),
                DialogResult  = DialogResult.No,
                MinimumSize   = new Size(80, 26),
                AutoSize      = true,
            };
            var yesButton = new Button
            {
                Text          = UiStrings.Get("update.prompt_yes"),
                DialogResult  = DialogResult.Yes,
                MinimumSize   = new Size(80, 26),
                AutoSize      = true,
            };
            // RightToLeft flow: first added is rightmost; No on right, Yes left of it
            buttonPanel.Controls.Add(noButton);
            buttonPanel.Controls.Add(yesButton);

            dialog.Controls.Add(notesBox);
            dialog.Controls.Add(headerLabel);
            dialog.Controls.Add(buttonPanel);
            dialog.AcceptButton = yesButton;
            dialog.CancelButton = noButton;
            // Move focus to the Yes button so the RichTextBox is not focused on open
            // (a focused read-only RichTextBox auto-selects all its text).
            dialog.Shown += (s, e) => yesButton.Focus();

            result = dialog.ShowDialog(this);
        }

        if (result != DialogResult.Yes)
            return;

        string zipPath = Path.Combine(Path.GetTempPath(),
            $"NMSE-update-{update.RemoteVersion.ToString(3)}.zip");

        // Show a modal, non-closable progress dialog so the user cannot interact
        // with the editor while the download and apply steps are running.
        using var progressDialog = new Form
        {
            Text            = UiStrings.Get("update.title"),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition   = FormStartPosition.CenterParent,
            ClientSize      = new Size(380, 90),
            MaximizeBox     = false,
            MinimizeBox     = false,
            ControlBox      = false,
        };
        var dlgStatusLabel = new Label
        {
            Text      = UiStrings.Get("update.downloading"),
            Bounds    = new System.Drawing.Rectangle(12, 12, 356, 22),
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
        };
        var dlgProgressBar = new ProgressBar
        {
            Bounds  = new System.Drawing.Rectangle(12, 46, 356, 22),
            Minimum = 0,
            Maximum = 100,
            Value   = 0,
            Style   = ProgressBarStyle.Continuous,
        };
        progressDialog.Controls.Add(dlgStatusLabel);
        progressDialog.Controls.Add(dlgProgressBar);

        progressDialog.Shown += async (_, _) =>
        {
            try
            {
                var progress = new Progress<(long received, long? total)>(p =>
                {
                    if (p.total > 0)
                    {
                        int pct = (int)(p.received * 100 / p.total.Value);
                        dlgStatusLabel.Text  = UiStrings.Format("update.downloading_progress", pct);
                        dlgProgressBar.Style = ProgressBarStyle.Continuous;
                        dlgProgressBar.Value = pct;
                    }
                    else
                    {
                        // Unknown content-length: show indeterminate progress.
                        dlgProgressBar.Style = ProgressBarStyle.Marquee;
                    }
                });

                await UpdateService.DownloadFileAsync(update.DownloadUrl, zipPath, progress)
                                   .ConfigureAwait(true);

                dlgStatusLabel.Text  = UiStrings.Get("update.applying");
                dlgProgressBar.Style = ProgressBarStyle.Continuous;
                dlgProgressBar.Value = 100;

                // Yield briefly so the UI repaints the final label before the
                // synchronous apply step blocks the message pump.
                await Task.Delay(50).ConfigureAwait(true);

                bool launched = UpdateService.ApplyUpdateAndRelaunch(zipPath);
                progressDialog.Close();
                if (launched)
                {
                    Application.Exit();
                }
                else
                {
                    MessageBox.Show(this,
                        UiStrings.Get("update.apply_failed_msg"),
                        UiStrings.Get("update.title"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (UpdateSyncLockException)
            {
                progressDialog.Close();
                MessageBox.Show(this,
                    UiStrings.Get("update.sync_lock_msg"),
                    UiStrings.Get("update.sync_lock_title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                progressDialog.Close();
                MessageBox.Show(this,
                    UiStrings.Format("update.download_failed_msg", ex.Message),
                    UiStrings.Get("update.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                // Clean up partial download.
                try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
            }
        };

        progressDialog.ShowDialog(this);
    }

    private void OnAbout(object? sender, EventArgs e)
    {
        // The form uses AutoSize so it grows to fit all content regardless of
        // the system DPI or font-scale setting.  A TableLayoutPanel provides
        // the vertical stacking; the two-column layout places the OK button
        // in the right column of the last row for natural right-alignment.
        using var aboutForm = new Form
        {
            Text            = "About",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition   = FormStartPosition.CenterParent,
            AutoSize        = true,
            AutoSizeMode    = AutoSizeMode.GrowAndShrink,
            MaximizeBox     = false,
            MinimizeBox     = false,
        };

        // Two-column table: col 0 = percent-fill spacer, col 1 = auto-size (button).
        // Rows 0 and 1 span both columns (label and link); row 2 places the OK
        // button in col 1 so it appears right-aligned without hardcoded positions.
        var tableLayout = new TableLayoutPanel
        {
            ColumnCount  = 2,
            RowCount     = 3,
            AutoSize     = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock         = DockStyle.Fill,
            Padding      = new Padding(16),
        };
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var infoLabel = new Label
        {
            Text     = $"{AppName}\n{VerMajor}.{VerMinor}.{VerPatch} ({SuppGameRel})\n\nby vector_cmdr",
            AutoSize = true,
            Margin   = new Padding(0, 0, 20, 8),
        };
        tableLayout.Controls.Add(infoLabel, 0, 0);
        tableLayout.SetColumnSpan(infoLabel, 2);

        var link = new LinkLabel
        {
            Text     = GitHubCreatorUrl,
            AutoSize = true,
            Margin   = new Padding(0, 0, 0, 12),
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
                        FileName        = linkData,
                        UseShellExecute = true
                    });
                }
            }
            catch { }
        };
        tableLayout.Controls.Add(link, 0, 1);
        tableLayout.SetColumnSpan(link, 2);

        var okButton = new Button
        {
            Text         = "OK",
            DialogResult = DialogResult.OK,
            MinimumSize  = new Size(80, 26),
            AutoSize     = true,
            Margin       = new Padding(0),
        };
        tableLayout.Controls.Add(okButton, 1, 2);

        aboutForm.Controls.Add(tableLayout);
        aboutForm.AcceptButton = okButton;
        aboutForm.ShowDialog(this);
    }

    private void OnFormResize(object? sender, EventArgs e)
    {
        int halfWidth = ClientSize.Width / 2;
        int thirdWidth = ClientSize.Width / 3;
        _directoryCombo.Width = Math.Min(halfWidth, 1200);
        _saveFileCombo.Width = Math.Min(thirdWidth, 800);
        DeselectComboText(_directoryCombo);
        DeselectComboText(_saveFileCombo);
        DeselectComboText(_saveSlotCombo);
    }

    private static void DeselectComboText(ToolStripComboBox combo)
    {
        if (combo.ComboBox != null)
            combo.ComboBox.SelectionLength = 0;
    }

    /// <summary>
    /// Marks the save as modified and invalidates the Raw JSON Editor's change cache so
    /// the Show Changes view always reflects edits made from other panels.
    /// </summary>
    private void OnPanelDataModified(object? sender, EventArgs e)
    {
        _hasUnsavedChanges = true;
        _rawJsonPanel.NotifyDataChanged();
    }

    /// <summary>Gets the save data currently loaded in the editor.</summary>
    internal JsonObject? CurrentSaveData => _currentSaveData;

    /// <summary>Gets the active game localisation service.</summary>
    internal LocalisationService CurrentLocalisation => _localisationService;

    /// <summary>
    /// Reloads every loaded panel tab after a direct inventory mutation (such as packing
    /// or unpacking a technology) so destination inventories repaint immediately, and
    /// marks the save as modified.
    /// </summary>
    internal void RefreshInventoryPanelsAfterPackOperation()
    {
        if (_currentSaveData == null) return;

        // Flush pending panel edits into the JSON before the panels are reloaded.
        SyncAllPanelData();

        if (_loadedTabIndices.Contains(1)) _exosuitPanel.LoadData(_currentSaveData);
        if (_loadedTabIndices.Contains(2)) _multitoolPanel.LoadData(_currentSaveData);
        if (_loadedTabIndices.Contains(3)) _shipPanel.LoadData(_currentSaveData);
        if (_loadedTabIndices.Contains(4)) _fleetPanel.LoadData(_currentSaveData);
        if (_loadedTabIndices.Contains(5)) _vehiclePanel.LoadData(_currentSaveData);
        if (_loadedTabIndices.Contains(7)) _basePanel.LoadData(_currentSaveData);

        OnPanelDataModified(this, EventArgs.Empty);
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        DisposeExternalWatcher();
        SaveContext.Reset();

        // Flush any pending panel UI edits into the in-memory save before the
        // baseline comparison, so unsaved edits (including ship-detail fields that
        // are only written on sync) trigger the prompt instead of being lost.
        SyncAllPanelData();

        // Prompt if there are unsaved changes. A change that has been reverted (for
        // example ticking and unticking a catalogue row) must not prompt, so compare
        // the live data with the last saved/loaded state. A successful save refreshes
        // that state, so saving and then closing does not prompt.
        bool changesExist = _hasUnsavedChanges
            && _currentSaveData != null
            && (!_rawJsonPanel.SaveDataMatchesSavedBaseline(_currentSaveData)
                || !_rawJsonPanel.AccountDataMatchesSavedBaseline(_accountPanel.AccountData));
        if (changesExist)
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
