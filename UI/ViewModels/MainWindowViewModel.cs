using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Compression;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Config;
using NMSE.Core;
using NMSE.Data;
using NMSE.IO;
using NMSE.Models;
using NMSE.UI.ViewModels.Panels;

namespace NMSE.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public const string AppName = "NMSE (NO MAN'S SAVE EDITOR)";
    public const string SuppGameRel = "6.20 Remnant";
    public const string GitHubUrl = "https://github.com/vectorcmdr/NMSE";
    public const string SponsorUrl = "https://github.com/sponsors/vectorcmdr";
    public const string GitHubCreatorUrl = "https://github.com/vectorcmdr";

    // Data services
    private readonly GameItemDatabase _database = new();
    private readonly RecipeDatabase _recipeDatabase = new();
    private readonly LocalisationService _localisationService = new();
    private WordDatabase? _wordDatabase;
    private IconManager? _iconManager;

    // Save state
    private List<List<string>> _saveSlotFiles = new();
    private JsonObject? _currentSaveData;
    private string? _currentFilePath;
    private string? _defaultSaveDirectory;
    private string? _xboxContainersIndexPath;
    private string? _ps4MemoryDatPath;
    private List<string>? _platformSlotIdentifiers;
    private SaveFileManager.Platform _detectedPlatform = SaveFileManager.Platform.Unknown;
    private readonly HashSet<int> _loadedTabIndices = new();
    private int _totalDatabaseItems;

    [ObservableProperty] private string _title = $"{AppName} - Build {BuildInfo.VerMajor}.{BuildInfo.VerMinor}.{BuildInfo.VerPatch} ({SuppGameRel})";
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private string _itemCountText = "";
    [ObservableProperty] private bool _isProgressVisible;
    [ObservableProperty] private int _progressValue;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _loadingMessage = "";
    [ObservableProperty] private bool _hasUnsavedChanges;
    [ObservableProperty] private bool _isSaveLoaded;
    [ObservableProperty] private int _selectedNavIndex;
    [ObservableProperty] private PanelViewModelBase? _selectedPanel;
    [ObservableProperty] private bool _isNavExpanded = true;
    [ObservableProperty] private double _navWidth = 200;

    [ObservableProperty] private ObservableCollection<string> _directories = new();
    [ObservableProperty] private int _selectedDirectoryIndex = -1;
    [ObservableProperty] private ObservableCollection<string> _saveSlots = new();
    [ObservableProperty] private int _selectedSlotIndex = -1;
    [ObservableProperty] private ObservableCollection<string> _saveFiles = new();
    [ObservableProperty] private int _selectedFileIndex = -1;

    // Panel ViewModels
    public MainStatsViewModel MainStats { get; } = new();
    public ExosuitViewModel Exosuit { get; } = new();
    public MultitoolViewModel Multitool { get; } = new();
    public StarshipViewModel Starship { get; } = new();
    public FreighterViewModel Freighter { get; } = new();
    public FleetViewModel Fleet { get; } = new();
    public ExocraftViewModel Exocraft { get; } = new();
    public CompanionViewModel Companion { get; } = new();
    public BaseViewModel Base { get; } = new();
    public DiscoveryViewModel Discovery { get; } = new();
    public MilestoneViewModel Milestone { get; } = new();
    public SettlementViewModel Settlement { get; } = new();
    public ByteBeatViewModel ByteBeat { get; } = new();
    public AccountViewModel Account { get; } = new();
    public ExportConfigViewModel ExportConfig { get; } = new();
    public RawJsonViewModel RawJson { get; } = new();

    public ObservableCollection<PanelViewModelBase> Panels { get; }

    // Navigation items for the sidebar
    public ObservableCollection<NavItem> NavItems { get; } = new()
    {
        new("tab.player", "M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14Z"),
        new("tab.exosuit", "M21,16V14L13,9V3.5A1.5,1.5 0 0,0 11.5,2A1.5,1.5 0 0,0 10,3.5V9L2,14V16L10,13.5V19.5L8,21V22L11.5,21L15,22V21L13,19.5V13.5L21,16Z"),
        new("tab.multitools", "M22.7,19L13.6,9.9C14.5,7.6 14,4.9 12.1,3C10.1,1 7.1,0.6 4.7,1.7L8.5,5.5L5.5,8.5L1.7,4.7C0.6,7.1 1,10.1 3,12.1C4.9,14 7.6,14.5 9.9,13.6L19,22.7C19.4,23.1 20,23.1 20.4,22.7L22.6,20.5C23.1,20.1 23.1,19.4 22.7,19Z"),
        new("tab.starships", "M21,16V14L13,9V3.5A1.5,1.5 0 0,0 11.5,2A1.5,1.5 0 0,0 10,3.5V9L2,14V16L10,13.5V19.5L8,21V22L11.5,21L15,22V21L13,19.5V13.5L21,16Z"),
        new("tab.freighter", "M20,8H17.19C16.74,7.22 15.94,6.63 15,6.28V4H9V6.28C8.06,6.63 7.26,7.22 6.81,8H4A2,2 0 0,0 2,10V14A2,2 0 0,0 4,16H5V20H19V16H20A2,2 0 0,0 22,14V10A2,2 0 0,0 20,8Z"),
        new("tab.fleet", "M3,6H21V18H3V6M12,9A3,3 0 0,1 15,12A3,3 0 0,1 12,15A3,3 0 0,1 9,12A3,3 0 0,1 12,9Z"),
        new("tab.exocraft", "M16,6L19,10H21C22.11,10 23,10.89 23,12V15H21A3,3 0 0,1 18,18A3,3 0 0,1 15,15H9A3,3 0 0,1 6,18A3,3 0 0,1 3,15H1V12C1,10.89 1.89,10 3,10L6,6H16Z"),
        new("tab.companions", "M4.5,9.5C5.33,9.5 6,10.17 6,11C6,11.83 5.33,12.5 4.5,12.5C3.67,12.5 3,11.83 3,11C3,10.17 3.67,9.5 4.5,9.5M9,5C10.1,5 11,5.9 11,7C11,8.1 10.1,9 9,9C7.9,9 7,8.1 7,7C7,5.9 7.9,5 9,5Z"),
        new("tab.bases_storage", "M10,20V14H14V20H19V12H22L12,3L2,12H5V20H10Z"),
        new("tab.discoveries", "M12,2C6.48,2 2,6.48 2,12C2,17.52 6.48,22 12,22C17.52,22 22,17.52 22,12C22,6.48 17.52,2 12,2Z"),
        new("tab.milestones", "M19,5H5V19H19V5M17,17H7V7H17V17Z"),
        new("tab.settlements", "M5,3V21H9V13H15V21H19V3H5M7,7H9V9H7V7M11,7H13V9H11V7M15,7H17V9H15V7Z"),
        new("tab.bytebeats", "M14,3.23V5.29C16.89,6.15 19,8.83 19,12C19,15.17 16.89,17.84 14,18.7V20.77C18,19.86 21,16.28 21,12C21,7.72 18,4.14 14,3.23M16.5,12C16.5,10.23 15.5,8.71 14,7.97V16C15.5,15.29 16.5,13.76 16.5,12M3,9V15H7L12,20V4L7,9H3Z"),
        new("tab.account_rewards", "M20,12A8,8 0 0,0 12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12M22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2A10,10 0 0,1 22,12Z"),
        new("tab.export_settings", "M12,15.5A3.5,3.5 0 0,1 8.5,12A3.5,3.5 0 0,1 12,8.5A3.5,3.5 0 0,1 15.5,12A3.5,3.5 0 0,1 12,15.5M19.43,12.97C19.47,12.65 19.5,12.33 19.5,12C19.5,11.67 19.47,11.34 19.43,11L21.54,9.37C21.73,9.22 21.78,8.95 21.66,8.73L19.66,5.27C19.54,5.05 19.27,4.96 19.05,5.05L16.56,6.05C16.04,5.66 15.5,5.32 14.87,5.07L14.5,2.42C14.46,2.18 14.25,2 14,2H10C9.75,2 9.54,2.18 9.5,2.42L9.13,5.07C8.5,5.32 7.96,5.66 7.44,6.05L4.95,5.05C4.73,4.96 4.46,5.05 4.34,5.27L2.34,8.73C2.21,8.95 2.27,9.22 2.46,9.37L4.57,11C4.53,11.34 4.5,11.67 4.5,12C4.5,12.33 4.53,12.65 4.57,12.97L2.46,14.63C2.27,14.78 2.21,15.05 2.34,15.27L4.34,18.73C4.46,18.95 4.73,19.04 4.95,18.95L7.44,17.94C7.96,18.34 8.5,18.68 9.13,18.93L9.5,21.58C9.54,21.82 9.75,22 10,22H14C14.25,22 14.46,21.82 14.5,21.58L14.87,18.93C15.5,18.67 16.04,18.34 16.56,17.94L19.05,18.95C19.27,19.04 19.54,18.95 19.66,18.73L21.66,15.27C21.78,15.05 21.73,14.78 21.54,14.63L19.43,12.97Z"),
        new("tab.raw_json_editor", "M5,3H7V5H5V10A2,2 0 0,1 3,12A2,2 0 0,1 5,14V19H7V21H5C3.93,21 3,20.07 3,19V15A2,2 0 0,0 1,13H0V11H1A2,2 0 0,0 3,9V5A2,2 0 0,1 5,3Z"),
    };

    // Language items
    public ObservableCollection<LanguageItem> Languages { get; } = new();

    public MainWindowViewModel()
    {
        Panels = new ObservableCollection<PanelViewModelBase>
        {
            MainStats, Exosuit, Multitool, Starship, Freighter, Fleet,
            Exocraft, Companion, Base, Discovery, Milestone,
            Settlement, ByteBeat, Account, ExportConfig, RawJson
        };

        foreach (var (_, tag) in LocalisationService.SupportedLanguages)
            Languages.Add(new LanguageItem(tag));

        MainStats.ReloadRequested += (_, _) =>
        {
            PopulateSaveSlots();
            if (_currentFilePath != null && File.Exists(_currentFilePath))
                _ = LoadSaveDataAsync(_currentFilePath);
        };

        SelectedPanel = MainStats;
    }

    public async Task InitializeAsync()
    {
        LoadConfig();
        LoadDatabase();
        ApplyStartupLanguage();
        PopulateSaveSlots();
        await CheckForUpdateOnStartupAsync();
    }

    private void LoadConfig()
    {
        var config = AppConfig.Instance;
        config.Initialize();

        _defaultSaveDirectory = SaveFileManager.FindDefaultSaveDirectory();

        var recent = config.RecentDirectories;

        if (recent.Count == 0)
        {
            string? initial = config.LastDirectory ?? _defaultSaveDirectory;
            if (initial != null)
            {
                recent.Add(initial);
                if (_defaultSaveDirectory != null && !string.Equals(initial, _defaultSaveDirectory,
                        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    recent.Add(_defaultSaveDirectory);
                config.RecentDirectories = recent;
                config.Save();
            }
        }
        else
        {
            if (_defaultSaveDirectory != null &&
                !recent.Any(d => string.Equals(d, _defaultSaveDirectory,
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
            {
                config.AddRecentDirectory(recent[0], _defaultSaveDirectory);
                recent = config.RecentDirectories;
                config.Save();
            }
        }

        RebuildDirectoryDropdown(recent, config.LastDirectory);
    }

    private void RebuildDirectoryDropdown(IEnumerable<string> directories, string? selectedDir)
    {
        Directories.Clear();
        foreach (var dir in directories)
            Directories.Add(dir);

        if (selectedDir != null && Directories.Contains(selectedDir))
            SelectedDirectoryIndex = Directories.IndexOf(selectedDir);
        else if (Directories.Count > 0)
            SelectedDirectoryIndex = 0;
    }

    private void LoadDatabase()
    {
        try
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string dbPath = Path.Combine(basePath, "Resources", "map");
            string jsonPath = Path.Combine(basePath, "Resources", "json");

            _database.LoadItemsFromJsonDirectory(jsonPath);
            TechPacks.RegisterGeneratedPacks();

            string recipesPath = Path.Combine(jsonPath, "Recipes.json");
            _recipeDatabase.LoadFromFile(recipesPath);

            string titlesPath = Path.Combine(jsonPath, "Titles.json");
            TitleDatabase.LoadFromFile(titlesPath);

            FrigateTraitDatabase.LoadFromFile(Path.Combine(jsonPath, "FrigateTraits.json"));
            SettlementPerkDatabase.LoadFromFile(Path.Combine(jsonPath, "SettlementPerks.json"));
            WikiGuideDatabase.LoadFromFile(Path.Combine(jsonPath, "WikiGuide.json"));

            _wordDatabase = new WordDatabase();
            string wordsPath = Path.Combine(jsonPath, "Words.json");
            _wordDatabase.LoadFromFile(wordsPath);

            string langDir = Path.Combine(jsonPath, "lang");
            _localisationService.SetLangDirectory(langDir);

            string uiLangDir = Path.Combine(basePath, "Resources", "ui", "lang");
            UiStrings.SetDirectory(uiLangDir);

            string iconsPath = Path.Combine(basePath, "Resources", "images");
            if (Directory.Exists(iconsPath))
            {
                _iconManager = new IconManager(iconsPath);
                CoordinateHelper.SetGlyphBasePath(iconsPath);

                var db = _database;
                var iconMgr = _iconManager;
                _ = Task.Run(() => iconMgr.PreloadIcons(db));
            }

            string exportConfigPath = Path.Combine(basePath, "export_config.json");
            Core.ExportConfig.LoadFromFile(exportConfigPath);

            var mapperJsonPath = Path.Combine(dbPath, "mapping.json");

            _totalDatabaseItems = _database.Items.Count
                + (_wordDatabase?.Count ?? 0)
                + FrigateTraitDatabase.Traits.Count
                + SettlementPerkDatabase.Perks.Count
                + RewardDatabase.Count
                + InventoryStackDatabase.Count
                + UiStrings.TotalKeyCount;

            if (File.Exists(mapperJsonPath))
            {
                var mapper = new JsonNameMapper();
                mapper.Load(mapperJsonPath);
                JsonParser.SetDefaultMapper(mapper);
                StatusText = UiStrings.Format("status.loaded_items_mappings", _database.Items.Count, mapper.Count);
            }
            else
            {
                StatusText = UiStrings.Format("status.loaded_items_no_mapping", _database.Items.Count);
            }

            ItemCountText = UiStrings.Format("status.total_db_items", _totalDatabaseItems);
        }
        catch (Exception ex)
        {
            StatusText = UiStrings.Format("status.db_load_warning", ex.Message);
        }
    }

    private void ApplyStartupLanguage()
    {
        string tag = AppConfig.Instance.Language;

        foreach (var lang in Languages)
            lang.IsSelected = lang.Tag == tag;

        UiStrings.Load(tag);
        NMSE.UI.Localization.LocaleManager.Instance.NotifyLanguageChanged();
        bool loaded = _localisationService.LoadLanguage(tag);
        if (loaded)
        {
            _database.ApplyLocalisation(_localisationService);
            RewardDatabase.ApplyLocalisation(_localisationService);
            _wordDatabase?.ApplyLocalisation(_localisationService);
            _recipeDatabase.ApplyLocalisation(_localisationService);
            TitleDatabase.ApplyLocalisation(_localisationService);
            FrigateTraitDatabase.ApplyLocalisation(_localisationService);
            SettlementPerkDatabase.ApplyLocalisation(_localisationService);
            WikiGuideDatabase.ApplyLocalisation(_localisationService);
            StatusText = UiStrings.Format("status.language_set", tag);
        }
    }

    partial void OnSelectedDirectoryIndexChanged(int value)
    {
        if (value >= 0 && value < Directories.Count)
        {
            var dir = Directories[value];
            var config = AppConfig.Instance;
            config.AddRecentDirectory(dir, _defaultSaveDirectory);
            config.Save();
            RebuildDirectoryDropdown(config.RecentDirectories, dir);
        }
        PopulateSaveSlots();
    }

    partial void OnSelectedSlotIndexChanged(int value)
    {
        PopulateSaveFileCombo();
    }

    private void PopulateSaveSlots()
    {
        SaveSlots.Clear();
        SaveFiles.Clear();
        _xboxContainersIndexPath = null;
        _ps4MemoryDatPath = null;
        _platformSlotIdentifiers = null;

        if (SelectedDirectoryIndex < 0 || SelectedDirectoryIndex >= Directories.Count)
            return;

        string dir = Directories[SelectedDirectoryIndex];
        if (!Directory.Exists(dir)) return;

        _detectedPlatform = SaveFileManager.DetectPlatform(dir);

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
                    foreach (var kvp in xboxSlots.OrderBy(s => s.Key))
                    {
                        _platformSlotIdentifiers.Add(kvp.Key);
                        _saveSlotFiles.Add(new List<string> { kvp.Value.DataFilePath ?? "" });
                        SaveSlots.Add($"Xbox: {kvp.Key}");
                    }
                }
                catch (Exception ex)
                {
                    StatusText = UiStrings.Format("status.failed_xbox_containers", ex.Message);
                }
            }
        }
        else if (_detectedPlatform == SaveFileManager.Platform.PS4 && File.Exists(Path.Combine(dir, "memory.dat")))
        {
            string memoryDatPath = Path.Combine(dir, "memory.dat");
            _ps4MemoryDatPath = memoryDatPath;
            _platformSlotIdentifiers = new List<string>();
            _saveSlotFiles = new List<List<string>>();

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
                        SaveSlots.Add($"PS4 Slot {i / 2 + 1}{(isAuto ? " (Auto)" : " (Manual)")}");
                    }
                }
                catch { }
            }
        }
        else
        {
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
                    SaveSlots.Add(label);
                }
            }

            if (SaveSlots.Count == 0)
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
                    SaveSlots.Add(label);
                }
            }

            _saveSlotFiles = saveFiles;
        }

        if (SaveSlots.Count > 0)
        {
            SelectedSlotIndex = 0;
            StatusText = UiStrings.Format("status.found_save_slots", SaveSlots.Count, Path.GetFileName(dir), _detectedPlatform);
        }
        else
        {
            StatusText = UiStrings.Get("status.no_saves_found");
        }
    }

    private void PopulateSaveFileCombo()
    {
        SaveFiles.Clear();

        int slotIndex = SelectedSlotIndex;
        if (slotIndex < 0 || slotIndex >= _saveSlotFiles.Count)
            return;

        var files = _saveSlotFiles[slotIndex];
        foreach (var filePath in files)
        {
            string fileName = Path.GetFileName(filePath);
            string suffix;
            if (fileName.StartsWith("savedata", StringComparison.OrdinalIgnoreCase))
                suffix = "";
            else if (fileName.Equals("save.hg", StringComparison.OrdinalIgnoreCase))
                suffix = " (Manual)";
            else
            {
                string numPart = fileName.Replace("save", "").Replace(".hg", "");
                bool isAuto = int.TryParse(numPart, out int num) && num % 2 == 0;
                suffix = isAuto ? " (Auto)" : " (Manual)";
            }
            SaveFiles.Add($"{fileName}{suffix}");
        }

        if (SaveFiles.Count > 0)
            SelectedFileIndex = 0;
    }

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

    private static string GameModeToString(int mode) => mode switch
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

    [RelayCommand]
    private async Task LoadSlotAsync()
    {
        int slotIndex = SelectedSlotIndex;

        if (_xboxContainersIndexPath != null && _platformSlotIdentifiers != null
            && slotIndex >= 0 && slotIndex < _platformSlotIdentifiers.Count)
        {
            string slotId = _platformSlotIdentifiers[slotIndex];
            await LoadSaveDataInternalAsync(() => SaveFileManager.LoadXboxSave(_xboxContainersIndexPath, slotId),
                _xboxContainersIndexPath, UiStrings.Format("status.loading_xbox", slotId));
            return;
        }

        if (_ps4MemoryDatPath != null && _platformSlotIdentifiers != null
            && slotIndex >= 0 && slotIndex < _platformSlotIdentifiers.Count)
        {
            int memSlot = int.Parse(_platformSlotIdentifiers[slotIndex]);
            await LoadSaveDataInternalAsync(() => SaveFileManager.LoadPS4MemoryDatSave(_ps4MemoryDatPath, memSlot),
                _ps4MemoryDatPath, UiStrings.Format("status.loading_ps4", memSlot));
            return;
        }

        if (slotIndex >= 0 && slotIndex < _saveSlotFiles.Count)
        {
            var files = _saveSlotFiles[slotIndex];
            int fileIndex = SelectedFileIndex;
            if (fileIndex < 0 || fileIndex >= files.Count)
                fileIndex = 0;
            string filePath = files[fileIndex];
            await LoadSaveDataAsync(filePath);
        }
    }

    public async Task LoadSaveDataAsync(string filePath)
    {
        await LoadSaveDataInternalAsync(() => SaveFileManager.LoadSaveFile(filePath),
            filePath, UiStrings.Get("status.loading_save"));
    }

    private async Task LoadSaveDataInternalAsync(Func<JsonObject?> loadFunc, string filePath, string loadingMsg)
    {
        try
        {
            var loadTimer = Stopwatch.StartNew();
            IsProgressVisible = true;
            ProgressValue = 0;
            StatusText = loadingMsg;

            string? saveDir = Path.GetDirectoryName(filePath);

            ProgressValue = 10;
            _currentSaveData = await Task.Run(loadFunc);
            ProgressValue = 60;

            if (_currentSaveData == null)
            {
                IsProgressVisible = false;
                StatusText = UiStrings.Get("status.failed_load_save");
                return;
            }

            _currentFilePath = filePath;

            SaveFileManager.RegisterContextTransforms(_currentSaveData);

            MainStats.SetSaveFilePath(filePath);

            ProgressValue = 70;

            Account.SetSaveDirectory(saveDir);
            Account.LoadData(_currentSaveData, _database, _iconManager);
            if (Account.AccountData != null)
                MainStats.LoadAccountData(Account.AccountData);

            ProgressValue = 80;
            int accountIdx = Panels.IndexOf(Account);
            _loadedTabIndices.Clear();
            _loadedTabIndices.Add(accountIdx);

            if (SelectedNavIndex != accountIdx)
            {
                LoadPanelForTab(SelectedNavIndex);
                _loadedTabIndices.Add(SelectedNavIndex);
            }

            ProgressValue = 100;
            await Task.Delay(200);
            IsProgressVisible = false;

            IsSaveLoaded = true;
            StatusText = UiStrings.Format("status.loaded_save", Path.GetFileName(filePath), loadTimer.ElapsedMilliseconds.ToString("N0"));
            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            IsProgressVisible = false;
            StatusText = UiStrings.Format("status.failed_load_save", ex.Message);
        }
    }

    private void LoadPanelForTab(int tabIndex)
    {
        if (_currentSaveData == null) return;

        var panel = tabIndex >= 0 && tabIndex < Panels.Count ? Panels[tabIndex] : null;
        panel?.LoadData(_currentSaveData, _database, _iconManager);

        if (panel == Account && Account.AccountData != null)
            MainStats.LoadAccountData(Account.AccountData);

        if (panel == Discovery)
            Discovery.Recipe.SetDatabases(_recipeDatabase, _database);
    }

    private void SyncAllPanelData()
    {
        if (_currentSaveData == null) return;

        foreach (int idx in _loadedTabIndices)
        {
            if (idx >= 0 && idx < Panels.Count)
                Panels[idx].SaveData(_currentSaveData);
        }
    }

    partial void OnSelectedNavIndexChanged(int value)
    {
        if (value >= 0 && value < Panels.Count)
            SelectedPanel = Panels[value];

        if (_currentSaveData != null && !_loadedTabIndices.Contains(value))
        {
            LoadPanelForTab(value);
            _loadedTabIndices.Add(value);
        }

        if (value == 15 && _currentSaveData != null)
        {
            SyncAllPanelData();
            RawJson.RefreshTree();
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (_currentSaveData == null || _currentFilePath == null) return;

        try
        {
            SyncAllPanelData();

            if (HasUnsavedChanges)
            {
                string? saveDir = Path.GetDirectoryName(_currentFilePath);
                if (saveDir != null)
                {
                    try { SaveFileManager.BackupSaveDirectory(saveDir); }
                    catch (Exception ex) { Debug.WriteLine($"Pre-save backup failed: {ex.Message}"); }
                }
            }

            int slotIdx = SelectedSlotIndex >= 0 ? SelectedSlotIndex : 0;
            SaveFileManager.SaveToFile(_currentFilePath, _currentSaveData,
                compress: true, writeMeta: true, platform: _detectedPlatform, slotIndex: slotIdx);

            if (Account.AccountData != null && Account.AccountFilePath != null)
                SaveFileManager.SaveToFile(Account.AccountFilePath, Account.AccountData,
                    compress: true, writeMeta: true, platform: _detectedPlatform);

            StatusText = UiStrings.Format("status.save_written", Path.GetFileName(_currentFilePath));
            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            StatusText = UiStrings.Format("dialog.save_failed", ex.Message);
        }
    }

    [RelayCommand]
    private void Reload()
    {
        if (_currentFilePath != null)
            _ = LoadSaveDataAsync(_currentFilePath);
    }

    private static void ExtractZipEntry(ZipArchiveEntry entry, string destPath)
    {
        using var entryStream = entry.Open();
        using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
        entryStream.CopyTo(fileStream);
    }

    [RelayCommand]
    private void RestoreBackupAll()
    {
        if (_currentFilePath == null) return;
        try
        {
            string? saveDir = Path.GetDirectoryName(_currentFilePath);
            if (saveDir == null) return;
            string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Save Backups");
            if (!Directory.Exists(backupDir)) { StatusText = UiStrings.Get("status.no_backups_found"); return; }

            string dirName = Path.GetFileName(saveDir);
            var zips = Directory.GetFiles(backupDir, $"{dirName}_*.zip").OrderByDescending(f => f).ToArray();
            if (zips.Length == 0) { StatusText = UiStrings.Get("status.no_backups_found"); return; }

            string latestZip = zips[0];
            string currentFileName = Path.GetFileName(_currentFilePath);

            using var archive = ZipFile.OpenRead(latestZip);
            var entry = archive.GetEntry(currentFileName);
            if (entry == null) { StatusText = UiStrings.Format("status.backup_entry_not_found", currentFileName); return; }

            ExtractZipEntry(entry, _currentFilePath);
            StatusText = UiStrings.Format("status.backup_restored", currentFileName, Path.GetFileName(latestZip));
            _ = LoadSaveDataAsync(_currentFilePath);
        }
        catch (Exception ex) { StatusText = UiStrings.Format("status.backup_restore_failed", ex.Message); }
    }

    [RelayCommand]
    private void RestoreBackupSingle()
    {
        if (_currentFilePath == null) return;
        try
        {
            string? saveDir = Path.GetDirectoryName(_currentFilePath);
            if (saveDir == null) return;
            string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Save Backups");
            if (!Directory.Exists(backupDir)) { StatusText = UiStrings.Get("status.no_backups_found"); return; }

            string dirName = Path.GetFileName(saveDir);
            var zips = Directory.GetFiles(backupDir, $"{dirName}_*.zip").OrderByDescending(f => f).ToArray();
            if (zips.Length == 0) { StatusText = UiStrings.Get("status.no_backups_found"); return; }

            string latestZip = zips[0];
            string currentFileName = Path.GetFileName(_currentFilePath);

            using var archive = ZipFile.OpenRead(latestZip);
            var entry = archive.GetEntry(currentFileName);
            if (entry == null) { StatusText = UiStrings.Format("status.backup_entry_not_found", currentFileName); return; }

            ExtractZipEntry(entry, _currentFilePath);

            if (Account.AccountFilePath != null)
            {
                string accountFileName = Path.GetFileName(Account.AccountFilePath);
                var accountEntry = archive.GetEntry(accountFileName);
                if (accountEntry != null)
                    ExtractZipEntry(accountEntry, Account.AccountFilePath);
            }

            StatusText = UiStrings.Format("status.backup_restored", currentFileName, Path.GetFileName(latestZip));
            _ = LoadSaveDataAsync(_currentFilePath);
        }
        catch (Exception ex) { StatusText = UiStrings.Format("status.backup_restore_failed", ex.Message); }
    }

    public Func<Task<string?>>? SaveFilePickerFunc { get; set; }
    public Func<Task<string?>>? OpenFilePickerFunc { get; set; }

    [RelayCommand]
    private async Task ExportJson()
    {
        if (_currentSaveData == null || SaveFilePickerFunc == null) return;
        SyncAllPanelData();
        string? path = await SaveFilePickerFunc();
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            File.WriteAllText(path, _currentSaveData.ToString());
            StatusText = UiStrings.Format("status.json_exported", Path.GetFileName(path));
        }
        catch (Exception ex) { StatusText = UiStrings.Format("status.json_export_failed", ex.Message); }
    }

    [RelayCommand]
    private async Task ImportJson()
    {
        if (OpenFilePickerFunc == null) return;
        string? path = await OpenFilePickerFunc();
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        try
        {
            string json = File.ReadAllText(path);
            var imported = JsonParser.ParseObject(json);
            if (imported == null) { StatusText = "Failed to parse JSON file."; return; }

            _currentSaveData = imported;
            SaveFileManager.RegisterContextTransforms(_currentSaveData);
            _loadedTabIndices.Clear();
            LoadPanelForTab(SelectedNavIndex);
            _loadedTabIndices.Add(SelectedNavIndex);
            IsSaveLoaded = true;
            StatusText = UiStrings.Format("status.json_imported", Path.GetFileName(path));
        }
        catch (Exception ex) { StatusText = UiStrings.Format("status.json_import_failed", ex.Message); }
    }

    public Action? ShutdownApp { get; set; }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        try
        {
            StatusText = UiStrings.Get("update.checking");
            var currentVersion = new Version(int.Parse(BuildInfo.VerMajor), int.Parse(BuildInfo.VerMinor), int.Parse(BuildInfo.VerPatch));
            var update = await UpdateService.CheckForUpdateAsync(currentVersion);
            if (update == null)
            {
                StatusText = UiStrings.Format("update.up_to_date_msg",
                    $"{BuildInfo.VerMajor}.{BuildInfo.VerMinor}.{BuildInfo.VerPatch}");
                return;
            }

            StatusText = UiStrings.Get("update.downloading");
            string downloadDir = Path.Combine(Path.GetTempPath(), "nmse_update");
            Directory.CreateDirectory(downloadDir);
            string zipPath = Path.Combine(downloadDir, "update.zip");

            var progress = new Progress<(long received, long? total)>(p =>
            {
                if (p.total > 0)
                {
                    int pct = (int)(p.received * 100 / p.total.Value);
                    StatusText = UiStrings.Format("update.downloading_progress", pct);
                }
            });

            await UpdateService.DownloadFileAsync(update.DownloadUrl, zipPath, progress);
            StatusText = UiStrings.Get("update.applying");

            bool launched = UpdateService.ApplyUpdateAndRelaunch(zipPath);
            if (launched)
            {
                ShutdownApp?.Invoke();
            }
            else
            {
                StatusText = UiStrings.Get("update.apply_failed");
            }
        }
        catch (Exception ex) { StatusText = UiStrings.Format("update.check_failed", ex.Message); }
    }

    [RelayCommand]
    private void OpenGitHub()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = GitHubUrl, UseShellExecute = true });
        }
        catch { }
    }

    [RelayCommand]
    private void OpenSponsor()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = SponsorUrl, UseShellExecute = true });
        }
        catch { }
    }

    [RelayCommand]
    private async Task SelectLanguage(string tag)
    {
        foreach (var lang in Languages)
            lang.IsSelected = lang.Tag == tag;

        LoadingMessage = UiStrings.Get("status.switching_language");
        IsLoading = true;

        try
        {
            await Task.Yield();

            UiStrings.Load(tag);
            NMSE.UI.Localization.LocaleManager.Instance.NotifyLanguageChanged();
            bool loaded = _localisationService.LoadLanguage(tag);
            if (loaded)
            {
                _database.ApplyLocalisation(_localisationService);
                RewardDatabase.ApplyLocalisation(_localisationService);
                _wordDatabase?.ApplyLocalisation(_localisationService);
                _recipeDatabase.ApplyLocalisation(_localisationService);
                TitleDatabase.ApplyLocalisation(_localisationService);
                FrigateTraitDatabase.ApplyLocalisation(_localisationService);
                SettlementPerkDatabase.ApplyLocalisation(_localisationService);
                WikiGuideDatabase.ApplyLocalisation(_localisationService);

                foreach (int idx in _loadedTabIndices)
                {
                    if (idx >= 0 && idx < Panels.Count && _currentSaveData != null)
                        Panels[idx].LoadData(_currentSaveData, _database, _iconManager);
                }

                StatusText = UiStrings.Format("status.language_set", tag);
            }
            else
            {
                StatusText = UiStrings.Format("status.language_not_found", tag);
            }

            AppConfig.Instance.Language = tag;
            AppConfig.Instance.Save();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SaveWindowState(int x, int y, int width, int height)
    {
        var config = AppConfig.Instance;
        config.MainFrameX = x;
        config.MainFrameY = y;
        config.MainFrameWidth = width;
        config.MainFrameHeight = height;
        config.Save();
    }

    public (int x, int y, int width, int height) GetWindowState()
    {
        var config = AppConfig.Instance;
        return (config.MainFrameX, config.MainFrameY, config.MainFrameWidth, config.MainFrameHeight);
    }

    private async Task CheckForUpdateOnStartupAsync()
    {
        try
        {
            await Task.Delay(2000);
            var currentVersion = new Version(int.Parse(BuildInfo.VerMajor), int.Parse(BuildInfo.VerMinor), int.Parse(BuildInfo.VerPatch));
            var update = await UpdateService.CheckForUpdateAsync(currentVersion);
            if (update != null)
            {
                StatusText = UiStrings.Format("update.available_msg",
                    $"{BuildInfo.VerMajor}.{BuildInfo.VerMinor}.{BuildInfo.VerPatch}",
                    update.RemoteVersion.ToString(3));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Startup update check failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ToggleNav()
    {
        IsNavExpanded = !IsNavExpanded;
        NavWidth = IsNavExpanded ? 200 : 48;
    }

    [RelayCommand]
    private void SetTheme(string theme)
    {
        if (Avalonia.Application.Current is App app)
            app.SetTheme(theme);
    }

    [RelayCommand]
    private void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Close();
        }
    }

    public void SetSaveFilePath(string path)
    {
        _currentFilePath = path;
    }

    public void RecordRecentDirectory(string directory)
    {
        var config = AppConfig.Instance;
        config.AddRecentDirectory(directory, _defaultSaveDirectory);
        config.Save();
        RebuildDirectoryDropdown(config.RecentDirectories, directory);
        PopulateSaveSlots();
    }
}

public class NavItem : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private readonly string _localeKey;

    public NavItem(string localeKey, string iconPath)
    {
        _localeKey = localeKey;
        IconPath = iconPath;
        NMSE.UI.Localization.LocaleManager.Instance.PropertyChanged += (_, _) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Name)));
    }

    public string Name => NMSE.UI.Localization.LocaleManager.Instance[_localeKey];
    public string IconPath { get; }
}

public partial class LanguageItem(string tag) : ObservableObject
{
    public string Tag { get; } = tag;
    [ObservableProperty] private bool _isSelected;
}
