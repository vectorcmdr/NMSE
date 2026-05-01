using System.Globalization;
using NMSE.Core;
using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.IO;
using NMSE.Models;
using NMSE.UI.Controls;
using NMSE.UI.Util;
using System.Xml.Linq;

namespace NMSE.UI.Panels;

public partial class MainStatsPanel : UserControl
{
    /// <summary>Raised when a save utility operation requires the current save to be reloaded.</summary>
    public event EventHandler? ReloadRequested;

    public string PlayerName = "";

    private static readonly string[] DifficultyPresets =
        { "Invalid", "Custom", "Normal", "Creative", "Relaxed", "Survival", "Permadeath" };

    private static readonly string[] DifficultyPresetLocKeys =
        { "player.preset_invalid", "player.preset_custom", "player.preset_normal",
          "player.preset_creative", "player.preset_relaxed", "player.preset_survival",
          "player.preset_permadeath" };

    private string? _saveFilePath;
    private IconManager? _iconManager;

    /// <summary>Raw (unclamped) stat values read from JSON at load time, keyed by JSON field name.</summary>
    private Dictionary<string, decimal>? _rawStatValues;

    /// <summary>Raw (unclamped) coordinate values read from JSON at load time, keyed by JSON field name.</summary>
    private Dictionary<string, int>? _rawCoordValues;

    private static readonly string[] GuideCategories =
        { "Survival Basics", "Getting Around", "Making Discoveries", "Upgrades & Crafting", "Construction", "Making Money", "Alien Lifeforms", "Combat" };

    private JsonObject? _saveData;
    private JsonObject? _accountData;

    public MainStatsPanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    /// <summary>Sets the icon manager for loading guide topic icons.</summary>
    public void SetIconManager(IconManager? iconManager) => _iconManager = iconManager;


    private static Label AddRow(TableLayoutPanel layout, string label, Control field, int row)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Padding = new Padding(0, 5, 10, 0) };
        layout.Controls.Add(lbl, 0, row);
        layout.Controls.Add(field, 1, row);
        return lbl;
    }

    private static Label AddSectionHeader(TableLayoutPanel layout, string text, int row)
    {
        var lbl = new Label
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(0, 10, 0, 2)
        };
        FontManager.ApplyHeadingFont(lbl, 11);
        layout.Controls.Add(lbl, 0, row);
        layout.SetColumnSpan(lbl, 2);
        return lbl;
    }

    /// <summary>
    /// Refreshes portal glyph button images after glyph icons become available
    /// (e.g. after CoordinateHelper.SetGlyphBasePath has been called).
    /// </summary>
    public void RefreshGlyphButtonImages()
    {
        if (_glyphButtonPanel == null) return;
        foreach (Control ctrl in _glyphButtonPanel.Controls)
        {
            if (ctrl is Button btn && btn.Tag is char hexChar)
            {
                var img = CoordinateHelper.GetGlyphImage(hexChar);
                if (img != null)
                {
                    var oldImage = btn.Image;
                    btn.Image = CreateGlyphButtonImage(img, 20);
                    btn.ImageAlign = ContentAlignment.MiddleCenter;
                    btn.Text = "";
                    oldImage?.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Creates a composite glyph button image with a dark gray background and the
    /// glyph icon centered, matching the style of CoordinateHelper.UpdateGlyphPanel.
    /// The caller takes ownership of the returned Bitmap and is responsible for disposal.
    /// </summary>
    internal static Bitmap CreateGlyphButtonImage(Image glyphImg, int size)
    {
        var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            using var brush = new SolidBrush(Color.FromArgb(60, 60, 60));
            g.FillRectangle(brush, 0, 0, size, size);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(glyphImg, 0, 0, size, size);
        }
        return bmp;
    }

    public void LoadData(JsonObject saveData)
    {
        _saveData = saveData;
        SuspendLayout();
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            // Player stats - store raw values (only write back if user changed them)
            _rawStatValues = new Dictionary<string, decimal>
            {
                ["Health"] = MainStatsLogic.ReadRawStatValue(playerState, "Health"),
                ["Shield"] = MainStatsLogic.ReadRawStatValue(playerState, "Shield"),
                ["Energy"] = MainStatsLogic.ReadRawStatValue(playerState, "Energy"),
                ["Units"] = MainStatsLogic.ReadRawStatValue(playerState, "Units"),
                ["Nanites"] = MainStatsLogic.ReadRawStatValue(playerState, "Nanites"),
                ["Specials"] = MainStatsLogic.ReadRawStatValue(playerState, "Specials"),
            };
            _healthField.NumericValue = (double)MainStatsLogic.ReadStatValue(playerState, "Health", (decimal)(_healthField.Minimum ?? 0), (decimal)(_healthField.Maximum ?? 999999));
            _shieldField.NumericValue = (double)MainStatsLogic.ReadStatValue(playerState, "Shield", (decimal)(_shieldField.Minimum ?? 0), (decimal)(_shieldField.Maximum ?? 999999));
            _energyField.NumericValue = (double)MainStatsLogic.ReadStatValue(playerState, "Energy", (decimal)(_energyField.Minimum ?? 0), (decimal)(_energyField.Maximum ?? 999999));
            _unitsField.NumericValue = (double)MainStatsLogic.ReadStatValue(playerState, "Units", (decimal)(_unitsField.Minimum ?? 0), (decimal)(_unitsField.Maximum ?? uint.MaxValue));
            _nanitesField.NumericValue = (double)MainStatsLogic.ReadStatValue(playerState, "Nanites", (decimal)(_nanitesField.Minimum ?? 0), (decimal)(_nanitesField.Maximum ?? uint.MaxValue));
            _quicksilverField.NumericValue = (double)MainStatsLogic.ReadStatValue(playerState, "Specials", (decimal)(_quicksilverField.Minimum ?? 0), (decimal)(_quicksilverField.Maximum ?? uint.MaxValue));

            // Save info
            try { _saveNameField.Text = saveData.GetObject("CommonStateData")?.GetString("SaveName") ?? ""; } catch { }
            try { _saveSummaryField.Text = playerState.GetString("SaveSummary") ?? ""; } catch { }
            try
            {
                int totalSeconds = saveData.GetObject("CommonStateData")?.GetInt("TotalPlayTime") ?? 0;
                var ts = TimeSpan.FromSeconds(totalSeconds);
                _playTimeField.Text = UiStrings.Format("player.time_format", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
            }
            catch { _playTimeField.Text = ""; }

            // Third person camera
            try { _thirdPersonCharCam.Checked = saveData.GetObject("CommonStateData")?.GetBool("UsesThirdPersonCharacterCam") ?? false; } catch { _thirdPersonCharCam.Checked = false; }

            // Account name (player USN from discovery owners)
            try
            {
                string usn = "";
                var commonState = saveData.GetObject("CommonStateData");
                // UsedDiscoveryOwnersV2 is a direct child of CommonStateData, NOT under SeasonData
                var owners = commonState?.GetArray("UsedDiscoveryOwnersV2");
                if (owners != null && owners.Length > 0)
                    usn = owners.GetObject(0)?.GetString("USN") ?? "";
                if (string.IsNullOrEmpty(usn))
                {
                    // Fallback: try persistent base owners
                    var bases = playerState.GetArray("PersistentPlayerBases");
                    if (bases != null)
                    {
                        for (int i = 0; i < bases.Length && string.IsNullOrEmpty(usn); i++)
                        {
                            try { usn = bases.GetObject(i)?.GetObject("Owner")?.GetString("USN") ?? ""; } catch { }
                        }
                    }
                }
                string displayName = string.IsNullOrEmpty(usn) ? UiStrings.Get("player.explorer_default") : usn;
                _accountNameField.Text = displayName;
                PlayerName = displayName;
            }
            catch { _accountNameField.Text = UiStrings.Get("player.explorer_default"); }

            // Difficulty presets
            try
            {
                var diffState = playerState.GetObject("DifficultyState");
                if (diffState != null)
                {
                    try { SelectPreset(_currentPresetCombo, diffState.GetObject("Preset")?.GetString("DifficultyPresetType")); } catch { _currentPresetCombo.SelectedIndex = -1; }
                    try { SelectPreset(_easiestPresetCombo, diffState.GetObject("EasiestUsedPreset")?.GetString("DifficultyPresetType")); } catch { _easiestPresetCombo.SelectedIndex = -1; }
                    try { SelectPreset(_hardestPresetCombo, diffState.GetObject("HardestUsedPreset")?.GetString("DifficultyPresetType")); } catch { _hardestPresetCombo.SelectedIndex = -1; }
                }
            }
            catch { }

            // Coordinates
            LoadCoordinates(playerState, saveData);

            // Space battle
            LoadSpaceBattle(playerState, saveData);
        }
        catch { }
        finally
        {
            ResumeLayout(true);
        }
    }

    private void LoadCoordinates(JsonObject playerState, JsonObject saveData)
    {
        try
        {
            var addr = playerState.GetObject("UniverseAddress");
            if (addr == null) return;

            int realityIndex = addr.GetInt("RealityIndex");
            string galaxyType = GalaxyDatabase.GetGalaxyType(realityIndex);
            _galaxyField.Text = $"{GalaxyDatabase.GetGalaxyDisplayName(realityIndex)} ({galaxyType})";
            _galaxyCoreCaptionLabel.Text = UiStrings.Get("player.core_label");
            GalaxyDisplayHelper.ConfigureGalaxyDotLabel(_galaxyDotLabel, realityIndex);

            var galactic = addr.GetObject("GalacticAddress");
            if (galactic == null) return;

            int voxelX = galactic.GetInt("VoxelX");
            int voxelY = galactic.GetInt("VoxelY");
            int voxelZ = galactic.GetInt("VoxelZ");
            int solarIdx = galactic.GetInt("SolarSystemIndex");
            int planetIdx = 0;
            try { planetIdx = galactic.GetInt("PlanetIndex"); } catch { }

            // Store raw coordinate values for preservation
            _rawCoordValues = new Dictionary<string, int>
            {
                ["RealityIndex"] = realityIndex,
                ["VoxelX"] = voxelX,
                ["VoxelY"] = voxelY,
                ["VoxelZ"] = voxelZ,
                ["SolarSystemIndex"] = solarIdx,
                ["PlanetIndex"] = planetIdx,
            };

            _portalCodeField.Text = CoordinateHelper.VoxelToPortalCode(voxelX, voxelY, voxelZ, solarIdx, planetIdx);
            _portalCodeDecField.Text = CoordinateHelper.PortalHexToDec(_portalCodeField.Text);
            CoordinateHelper.UpdateGlyphPanel(_portalGlyphPanel, _portalCodeField.Text);
            _signalBoosterField.Text = CoordinateHelper.VoxelToSignalBooster(voxelX, voxelY, voxelZ, solarIdx);

            // Populate editable coordinate NUDs.
            // The NUD is presented to the user as a 1-based galaxy selector (1..257),
            // while the underlying RealityIndex remains 0-based (0..256).
            _galaxyNud.NumericValue = Math.Clamp(realityIndex + 1, (int)(_galaxyNud.Minimum ?? 1), (int)(_galaxyNud.Maximum ?? 257));
            _voxelXNud.NumericValue = Math.Clamp(voxelX, (int)(_voxelXNud.Minimum ?? -2048), (int)(_voxelXNud.Maximum ?? 2047));
            _voxelYNud.NumericValue = Math.Clamp(voxelY, (int)(_voxelYNud.Minimum ?? -128), (int)(_voxelYNud.Maximum ?? 127));
            _voxelZNud.NumericValue = Math.Clamp(voxelZ, (int)(_voxelZNud.Minimum ?? -2048), (int)(_voxelZNud.Maximum ?? 2047));
            _solarSystemNud.NumericValue = Math.Clamp(solarIdx, (int)(_solarSystemNud.Minimum ?? 0), (int)(_solarSystemNud.Maximum ?? 600));
            _planetNud.NumericValue = Math.Clamp(planetIdx, (int)(_planetNud.Minimum ?? 0), (int)(_planetNud.Maximum ?? 15));

            // Player state
            try
            {
                var spawnState = saveData.GetObject("SpawnStateData");
                string lastState = spawnState?.GetString("LastKnownPlayerState") ?? "";
                int stateIdx = Array.IndexOf(CoordinateHelper.PlayerStates, lastState);
                _playerStateField.SelectedIndex = stateIdx >= 0 ? stateIdx : -1;
            }
            catch { _playerStateField.SelectedIndex = -1; }

            double dist = CoordinateHelper.GetDistanceToCenter(voxelX, voxelY, voxelZ);
            _distanceToCenterField.Text = $"{dist.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)} ly";
            _jumpsToCenterField.Text = CoordinateHelper.GetJumpsToCenter(dist, CoordinateHelper.DefaultHyperdriveRange).ToString(CultureInfo.CurrentCulture);

            // Freighter/Nexus in system
            try
            {
                var freighterAddr = playerState.GetObject("FreighterUniverseAddress");
                bool freighterHere = false;
                if (freighterAddr != null)
                {
                    int fRealIdx = freighterAddr.GetInt("RealityIndex");
                    var fGal = freighterAddr.GetObject("GalacticAddress");
                    if (fGal != null && fRealIdx == realityIndex)
                        freighterHere = fGal.GetInt("VoxelX") == voxelX && fGal.GetInt("VoxelY") == voxelY
                            && fGal.GetInt("VoxelZ") == voxelZ && fGal.GetInt("SolarSystemIndex") == solarIdx;
                }
                _freighterInSystemField.Text = freighterHere ? UiStrings.Get("common.yes") : UiStrings.Get("common.no");
            }
            catch { _freighterInSystemField.Text = UiStrings.Get("common.unknown"); }

            try
            {
                var nexusAddr = playerState.GetObject("NexusUniverseAddress");
                bool nexusHere = false;
                if (nexusAddr != null)
                {
                    int nRealIdx = nexusAddr.GetInt("RealityIndex");
                    var nGal = nexusAddr.GetObject("GalacticAddress");
                    if (nGal != null && nRealIdx == realityIndex)
                        nexusHere = nGal.GetInt("VoxelX") == voxelX && nGal.GetInt("VoxelY") == voxelY
                            && nGal.GetInt("VoxelZ") == voxelZ && nGal.GetInt("SolarSystemIndex") == solarIdx;
                }
                _nexusInSystemField.Text = nexusHere ? UiStrings.Get("common.yes") : UiStrings.Get("common.no");
            }
            catch { _nexusInSystemField.Text = UiStrings.Get("common.unknown"); }

            // Planets in system
            try
            {
                var planetSeeds = playerState.GetArray("PlanetSeeds");
                int count = 0;
                if (planetSeeds != null)
                {
                    for (int i = 0; i < planetSeeds.Length; i++)
                    {
                        try
                        {
                            var seed = planetSeeds.GetArray(i);
                            // "0x0" indicates an uninitialised planet seed (no planet at this index)
                            if (seed != null && seed.Length >= 2 && seed.Get(1)?.ToString() != "0x0")
                                count++;
                        }
                        catch { }
                    }
                }
                _planetsInSystemField.Text = count.ToString(CultureInfo.CurrentCulture);
            }
            catch { _planetsInSystemField.Text = "0"; }

            // Portal interference
            try { _portalInterference.Checked = playerState.GetBool("OnOtherSideOfPortal"); } catch { _portalInterference.Checked = false; }
        }
        catch { }
    }

    private void LoadSpaceBattle(JsonObject playerState, JsonObject saveData)
    {
        try
        {
            int totalPlayTime = 0;
            try { totalPlayTime = saveData.GetObject("CommonStateData")?.GetInt("TotalPlayTime") ?? 0; } catch { }
            int timeLastBattle = 0;
            try { timeLastBattle = playerState.GetInt("TimeLastSpaceBattle"); } catch { }

            int timeRemaining = Math.Max(0, Math.Min(
                CoordinateHelper.SpaceBattleIntervalSeconds - (totalPlayTime - timeLastBattle),
                CoordinateHelper.SpaceBattleIntervalSeconds));
            var ts = TimeSpan.FromSeconds(timeRemaining);
            _timeToNextBattleField.Text = UiStrings.Format("player.time_format", (int)ts.TotalHours, ts.Minutes, ts.Seconds);

            // Warps - would need Stats array with ^DIST_WARP
            int warpsLastBattle = 0;
            try { warpsLastBattle = playerState.GetInt("WarpsLastSpaceBattle"); } catch { }
            int totalWarps = ReadTotalWarps(playerState);

            int warpsRemaining = Math.Max(0, CoordinateHelper.SpaceBattleIntervalWarps - (totalWarps - warpsLastBattle));
            _warpsToNextBattleField.NumericValue = Math.Min((int)(_warpsToNextBattleField.Maximum ?? 999), warpsRemaining);
        }
        catch { }
    }

    /// <summary>Reads the total warp count from the player's global stats (^DIST_WARP).</summary>
    private static int ReadTotalWarps(JsonObject playerState)
    {
        try
        {
            var statsGroups = playerState.GetArray("Stats");
            if (statsGroups != null)
            {
                for (int i = 0; i < statsGroups.Length; i++)
                {
                    var group = statsGroups.GetObject(i);
                    if (group.GetString("GroupId") == "^GLOBAL_STATS")
                    {
                        var stats = group.GetArray("Stats");
                        if (stats != null)
                        {
                            for (int j = 0; j < stats.Length; j++)
                            {
                                var stat = stats.GetObject(j);
                                if (stat.GetString("Id") == "^DIST_WARP")
                                    return stat.GetObject("Value")?.GetInt("IntValue") ?? 0;
                            }
                        }
                        break;
                    }
                }
            }
        }
        catch { }
        return 0;
    }

    public void LoadAccountData(JsonObject accountData)
    {
        _accountData = accountData;
        LoadGuides(accountData);
        LoadTitles(accountData);
    }

    public void SetSaveFilePath(string path)
    {
        _saveFilePath = path;
        try
        {
            var lastWrite = File.GetLastWriteTime(path);
            _lastSaveDateLabel.Text = lastWrite.ToString("g", CultureInfo.CurrentCulture);
        }
        catch { _lastSaveDateLabel.Text = ""; }
    }

    private const int GridBorderPadding = 2;
    private const int MinimumEmptyGridHeight = 24;

    private static void AutoSizeGridHeight(DataGridView grid)
    {
        int height = grid.ColumnHeadersHeight + GridBorderPadding;
        foreach (DataGridViewRow row in grid.Rows)
            if (row.Visible) height += row.Height;
        grid.Height = Math.Max(height, grid.ColumnHeadersHeight + MinimumEmptyGridHeight);
    }

    private static void SelectPreset(ComboBox combo, string? presetValue)
    {
        if (string.IsNullOrEmpty(presetValue)) { combo.SelectedIndex = -1; return; }
        int idx = Array.IndexOf(DifficultyPresets, presetValue);
        combo.SelectedIndex = idx >= 0 ? idx : -1;
    }

    private void LoadGuides(JsonObject accountData)
    {
        foreach (var grid in _guidesGrids)
            grid.Rows.Clear();

        try
        {
            var userData = accountData.GetObject("UserSettingsData");
            if (userData == null) return;

            var seenSet = new HashSet<string>(StringComparer.Ordinal);
            var unlockedSet = new HashSet<string>(StringComparer.Ordinal);

            var seenTopics = userData.GetArray("SeenWikiTopics");
            var unlockedTopics = userData.GetArray("UnlockedWikiTopics");

            if (seenTopics != null)
                for (int i = 0; i < seenTopics.Length; i++)
                    try { seenSet.Add(seenTopics.GetString(i)); } catch { }
            if (unlockedTopics != null)
                for (int i = 0; i < unlockedTopics.Length; i++)
                    try { unlockedSet.Add(unlockedTopics.GetString(i)); } catch { }

            // Build a lookup: category -> grid (grid tags use hardcoded English category names)
            var categoryGridMap = new Dictionary<string, DataGridView>(StringComparer.Ordinal);
            foreach (var grid in _guidesGrids)
                categoryGridMap[grid.Tag?.ToString() ?? ""] = grid;

            var shown = new HashSet<string>(StringComparer.Ordinal);
            foreach (var topic in WikiGuideDatabase.Topics)
            {
                shown.Add(topic.Id);
                // Use English category for grid placement (grid tags are always English)
                // but display the (potentially localised) topic.Name.
                string englishCat = WikiGuideDatabase.GetEnglishCategory(topic.Id);
                if (categoryGridMap.TryGetValue(englishCat, out var grid))
                {
                    string iconKey = WikiGuideDatabase.GetTopicIconKey(topic.Id);
                    Image? icon = LoadGuideIcon(iconKey);
                    grid.Rows.Add((object?)icon ?? DBNull.Value, topic.Id, topic.Name,
                        seenSet.Contains(topic.Id), unlockedSet.Contains(topic.Id));
                }
            }

            // Add any topics in the save that aren't in our database (to the last grid)
            var fallbackGrid = _guidesGrids.Count > 0 ? _guidesGrids[^1] : null;
            foreach (string topicId in seenSet.Union(unlockedSet))
            {
                if (!shown.Contains(topicId) && !string.IsNullOrEmpty(topicId))
                {
                    shown.Add(topicId);
                    string cat = WikiGuideDatabase.GetEnglishCategory(topicId);
                    var targetGrid = (!string.IsNullOrEmpty(cat) && categoryGridMap.TryGetValue(cat, out var g)) ? g : fallbackGrid;
                    string iconKey = WikiGuideDatabase.GetTopicIconKey(topicId);
                    Image? icon = LoadGuideIcon(iconKey);
                    targetGrid?.Rows.Add((object?)icon ?? DBNull.Value, topicId,
                        WikiGuideDatabase.GetTopicName(topicId),
                        seenSet.Contains(topicId), unlockedSet.Contains(topicId));
                }
            }

            foreach (var grid in _guidesGrids)
                AutoSizeGridHeight(grid);
        }
        catch { }
    }

    /// <summary>
    /// Attempts to load a guide topic icon from the icon manager.
    /// Returns the icon image or null if not found (graceful fallback).
    /// </summary>
    private Image? LoadGuideIcon(string iconKey)
    {
        if (string.IsNullOrEmpty(iconKey) || _iconManager == null) return null;
        try
        {
            return _iconManager.GetIcon(iconKey + ".png")
                ?? _iconManager.GetIcon(iconKey);
        }
        catch { return null; }
    }

    public void SaveData(JsonObject saveData)
    {
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return;

        MainStatsLogic.WriteStatValues(playerState,
            (decimal)(_healthField.NumericValue ?? 0), (decimal)(_shieldField.NumericValue ?? 0), (decimal)(_energyField.NumericValue ?? 0),
            (decimal)(_unitsField.NumericValue ?? 0), (decimal)(_nanitesField.NumericValue ?? 0), (decimal)(_quicksilverField.NumericValue ?? 0),
            _rawStatValues);

        // Save name / summary
        try { saveData.GetObject("CommonStateData")?.Set("SaveName", _saveNameField.Text); } catch { }
        try { playerState.Set("SaveSummary", _saveSummaryField.Text); } catch { }

        // Third person camera
        try { saveData.GetObject("CommonStateData")?.Set("UsesThirdPersonCharacterCam", _thirdPersonCharCam.Checked); } catch { }

        // Difficulty presets
        try
        {
            var diffState = playerState.GetObject("DifficultyState");
            if (diffState != null)
            {
                if (_currentPresetCombo.SelectedIndex >= 0)
                    try { diffState.GetObject("Preset")?.Set("DifficultyPresetType", DifficultyPresets[_currentPresetCombo.SelectedIndex]); } catch { }
                if (_easiestPresetCombo.SelectedIndex >= 0)
                    try { diffState.GetObject("EasiestUsedPreset")?.Set("DifficultyPresetType", DifficultyPresets[_easiestPresetCombo.SelectedIndex]); } catch { }
                if (_hardestPresetCombo.SelectedIndex >= 0)
                    try { diffState.GetObject("HardestUsedPreset")?.Set("DifficultyPresetType", DifficultyPresets[_hardestPresetCombo.SelectedIndex]); } catch { }
            }
        }
        catch { }

        // Player state
        if (_playerStateField.SelectedIndex >= 0)
        {
            try
            {
                var spawnState = saveData.GetObject("SpawnStateData");
                spawnState?.Set("LastKnownPlayerState", CoordinateHelper.PlayerStates[_playerStateField.SelectedIndex]);
            }
            catch { }
        }

        // Portal interference
        try { playerState.Set("OnOtherSideOfPortal", _portalInterference.Checked); } catch { }

        // Space battle - warps to next
        try
        {
            int warpsToNext = (int)(_warpsToNextBattleField.NumericValue ?? 0);
            int totalWarps = ReadTotalWarps(playerState);
            int warpsLastBattle = totalWarps - CoordinateHelper.SpaceBattleIntervalWarps + warpsToNext;
            playerState.Set("WarpsLastSpaceBattle", Math.Max(0, warpsLastBattle));
        }
        catch { }

        // Coordinates
        SaveCoordinatesToJson(playerState);
    }

    public void SaveAccountData(JsonObject accountData)
    {
        // Guide changes are saved immediately via OnGuideCellChanged
    }

    private void SaveCoordinatesToJson(JsonObject playerState)
    {
        try
        {
            var addr = playerState.GetObject("UniverseAddress");
            if (addr == null) return;

            int desiredRealityIndex = (int)(_galaxyNud.NumericValue ?? 1) - 1;
            if (_rawCoordValues == null || !_rawCoordValues.TryGetValue("RealityIndex", out int rawRealityIndex)
                || desiredRealityIndex != rawRealityIndex)
            {
                addr.Set("RealityIndex", desiredRealityIndex);
            }

            var galactic = addr.GetObject("GalacticAddress");
            if (galactic == null) return;

            WriteCoordIfChanged(galactic, "VoxelX", (int)(_voxelXNud.NumericValue ?? 0), _voxelXNud);
            WriteCoordIfChanged(galactic, "VoxelY", (int)(_voxelYNud.NumericValue ?? 0), _voxelYNud);
            WriteCoordIfChanged(galactic, "VoxelZ", (int)(_voxelZNud.NumericValue ?? 0), _voxelZNud);
            WriteCoordIfChanged(galactic, "SolarSystemIndex", (int)(_solarSystemNud.NumericValue ?? 0), _solarSystemNud);
            WriteCoordIfChanged(galactic, "PlanetIndex", (int)(_planetNud.NumericValue ?? 0), _planetNud);
        }
        catch { }
    }

    /// <summary>
    /// Writes a coordinate value only if the user changed it from the clamped display value.
    /// </summary>
    private void WriteCoordIfChanged(JsonObject target, string key, int uiValue, InvariantNumericTextBox nud)
    {
        if (_rawCoordValues != null && _rawCoordValues.TryGetValue(key, out int raw))
        {
            // Coordinates are clamped on display (Math.Clamp in LoadData), so the
            // comparison is against the clamped raw value. If the user didn't change
            // it, we preserve the original JSON value (which may exceed the UI range).
            int clamped = Math.Clamp(raw, (int)(nud.Minimum ?? int.MinValue), (int)(nud.Maximum ?? int.MaxValue));
            if (uiValue == clamped)
                return; // User didn't change it - preserve original JSON value
        }
        RawNumberGuard.SetInt(target, key, uiValue);
    }

    private void OnApplyCoordinates(object? sender, EventArgs e)
    {
        if (_saveData != null)
        {
            try
            {
                var playerState = _saveData.GetObject("PlayerStateData");
                if (playerState != null)
                    SaveCoordinatesToJson(playerState);
            }
            catch { }
        }
        RefreshCoordinateDisplay();
    }

    private void RefreshCoordinateDisplay()
    {
        int realityIndex = (int)(_galaxyNud.NumericValue ?? 1) - 1;
        int voxelX = (int)(_voxelXNud.NumericValue ?? 0);
        int voxelY = (int)(_voxelYNud.NumericValue ?? 0);
        int voxelZ = (int)(_voxelZNud.NumericValue ?? 0);
        int solarIdx = (int)(_solarSystemNud.NumericValue ?? 0);
        int planetIdx = (int)(_planetNud.NumericValue ?? 0);

        string galaxyType = GalaxyDatabase.GetGalaxyType(realityIndex);
        _galaxyField.Text = $"{GalaxyDatabase.GetGalaxyDisplayName(realityIndex)} ({galaxyType})";
        _galaxyCoreCaptionLabel.Text = UiStrings.Get("player.core_label");
        GalaxyDisplayHelper.ConfigureGalaxyDotLabel(_galaxyDotLabel, realityIndex);
        _portalCodeField.Text = CoordinateHelper.VoxelToPortalCode(voxelX, voxelY, voxelZ, solarIdx, planetIdx);
        _portalCodeDecField.Text = CoordinateHelper.PortalHexToDec(_portalCodeField.Text);
        CoordinateHelper.UpdateGlyphPanel(_portalGlyphPanel, _portalCodeField.Text);
        _signalBoosterField.Text = CoordinateHelper.VoxelToSignalBooster(voxelX, voxelY, voxelZ, solarIdx);

        double dist = CoordinateHelper.GetDistanceToCenter(voxelX, voxelY, voxelZ);
        _distanceToCenterField.Text = $"{dist.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)} ly";
        _jumpsToCenterField.Text = CoordinateHelper.GetJumpsToCenter(dist, CoordinateHelper.DefaultHyperdriveRange).ToString(CultureInfo.CurrentCulture);
    }

    private void OnConvertPortalCode(object? sender, EventArgs e)
    {
        string portalCode = _portalHexInput.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(portalCode))
        {
            MessageBox.Show(this, UiStrings.Get("player.portal_code_msg_12"), UiStrings.Get("player.invalid_portal_title"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!CoordinateHelper.PortalCodeToVoxel(portalCode, out int voxelX, out int voxelY, out int voxelZ, out int systemIndex, out int planetIndex))
        {
            MessageBox.Show(this, UiStrings.Get("player.invalid_portal_format"), UiStrings.Get("player.invalid_portal_title"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _voxelXNud.NumericValue = Math.Clamp(voxelX, (int)(_voxelXNud.Minimum ?? -2048), (int)(_voxelXNud.Maximum ?? 2047));
        _voxelYNud.NumericValue = Math.Clamp(voxelY, (int)(_voxelYNud.Minimum ?? -128), (int)(_voxelYNud.Maximum ?? 127));
        _voxelZNud.NumericValue = Math.Clamp(voxelZ, (int)(_voxelZNud.Minimum ?? -2048), (int)(_voxelZNud.Maximum ?? 2047));
        _solarSystemNud.NumericValue = Math.Clamp(systemIndex, (int)(_solarSystemNud.Minimum ?? 0), (int)(_solarSystemNud.Maximum ?? 600));
        _planetNud.NumericValue = Math.Clamp(planetIndex, (int)(_planetNud.Minimum ?? 0), (int)(_planetNud.Maximum ?? 15));
    }

    private void OnCoordinateRoulette(object? sender, EventArgs e)
    {
        // Generate a random 12-character hex portal code
        const string hexChars = "0123456789ABCDEF";
        var portalChars = new char[12];
        for (int i = 0; i < 12; i++)
            portalChars[i] = hexChars[Random.Shared.Next(16)];
        string portalCode = new string(portalChars);

        // Random galaxy 0-255
        int galaxy = Random.Shared.Next(256);

        // Parse the portal code into voxel coordinates (should always succeed for valid hex)
        if (!CoordinateHelper.PortalCodeToVoxel(portalCode, out int voxelX, out int voxelY, out int voxelZ, out int systemIndex, out int planetIndex))
        {
            MessageBox.Show(this, UiStrings.Get("player.roulette_failed"), UiStrings.Get("player.roulette_title"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string galaxyName = GalaxyDatabase.GetGalaxyDisplayName(galaxy);

        var result = MessageBox.Show(this, 
            UiStrings.Format("player.roulette_confirm", portalCode, galaxyName, galaxy),
            UiStrings.Get("player.roulette_title"),
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question);

        if (result != DialogResult.OK) return;

        // Apply the random coordinates to the NUDs
        _galaxyNud.NumericValue = galaxy + 1;
        _voxelXNud.NumericValue = Math.Clamp(voxelX, (int)(_voxelXNud.Minimum ?? -2048), (int)(_voxelXNud.Maximum ?? 2047));
        _voxelYNud.NumericValue = Math.Clamp(voxelY, (int)(_voxelYNud.Minimum ?? -128), (int)(_voxelYNud.Maximum ?? 127));
        _voxelZNud.NumericValue = Math.Clamp(voxelZ, (int)(_voxelZNud.Minimum ?? -2048), (int)(_voxelZNud.Maximum ?? 2047));
        _solarSystemNud.NumericValue = Math.Clamp(systemIndex, (int)(_solarSystemNud.Minimum ?? 0), (int)(_solarSystemNud.Maximum ?? 600));
        _planetNud.NumericValue = Math.Clamp(planetIndex, (int)(_planetNud.Minimum ?? 0), (int)(_planetNud.Maximum ?? 15));

        // Apply to save data and refresh display (same as "Apply Coordinates")
        OnApplyCoordinates(sender, e);
    }

    private void OnTriggerSpaceBattle(object? sender, EventArgs e)
    {
        if (_saveData == null) return;
        try
        {
            var playerState = _saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            // Set TimeLastSpaceBattle to 0 and WarpsLastSpaceBattle to 0
            playerState.Set("TimeLastSpaceBattle", 0);
            playerState.Set("WarpsLastSpaceBattle", 0);

            _warpsToNextBattleField.NumericValue = 0;
            _timeToNextBattleField.Text = UiStrings.Format("player.time_format", 0, 0, 0);

            MessageBox.Show(this, UiStrings.Get("player.space_battle_triggered"), UiStrings.Get("player.space_battle_title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch { }
    }

    // -- Save Utility event handlers --

    private string? GetSaveDirectory() =>
        _saveFilePath != null ? Path.GetDirectoryName(_saveFilePath) : null;

    private SaveFileManager.Platform GetDetectedPlatform()
    {
        string? dir = GetSaveDirectory();
        return dir != null ? SaveFileManager.DetectPlatform(dir) : SaveFileManager.Platform.Unknown;
    }

    private static SaveFileManager.Platform TransferPlatformFromIndex(int index) => index switch
    {
        0 => SaveFileManager.Platform.Steam,
        1 => SaveFileManager.Platform.GOG,
        2 => SaveFileManager.Platform.XboxGamePass,
        3 => SaveFileManager.Platform.PS4,
        4 => SaveFileManager.Platform.Switch,
        _ => SaveFileManager.Platform.Unknown,
    };

    private void OnCopySlot(object? sender, EventArgs e)
    {
        string? dir = GetSaveDirectory();
        if (dir == null) { ShowNoDirWarning(); return; }
        int src = _slotSourceCombo.SelectedIndex;
        int dst = _slotDestCombo.SelectedIndex;
        if (src < 0 || dst < 0) return;
        if (src == dst) { MessageBox.Show(this, UiStrings.Get("player.slots_must_differ"), UiStrings.Get("player.copy_slot"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        var platform = GetDetectedPlatform();
        var result = MessageBox.Show(this, UiStrings.Format("player.copy_slot_confirm", src + 1, dst + 1),
            UiStrings.Get("player.copy_slot"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        try
        {
            SaveSlotManager.CopySlot(dir, src, dst, platform);
            MessageBox.Show(this, UiStrings.Format("player.copy_slot_success", src + 1, dst + 1), UiStrings.Get("player.copy_slot"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("player.copy_slot_failed", ex.Message), UiStrings.Get("player.copy_slot"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnMoveSlot(object? sender, EventArgs e)
    {
        string? dir = GetSaveDirectory();
        if (dir == null) { ShowNoDirWarning(); return; }
        int src = _slotSourceCombo.SelectedIndex;
        int dst = _slotDestCombo.SelectedIndex;
        if (src < 0 || dst < 0) return;
        if (src == dst) { MessageBox.Show(this, UiStrings.Get("player.slots_must_differ"), UiStrings.Get("player.move_slot"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        var platform = GetDetectedPlatform();
        var result = MessageBox.Show(this, UiStrings.Format("player.move_slot_confirm", src + 1, dst + 1),
            UiStrings.Get("player.move_slot"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        try
        {
            SaveSlotManager.MoveSlot(dir, src, dst, platform);
            MessageBox.Show(this, UiStrings.Format("player.move_slot_success", src + 1, dst + 1), UiStrings.Get("player.move_slot"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            ReloadRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("player.move_slot_failed", ex.Message), UiStrings.Get("player.move_slot"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnSwapSlots(object? sender, EventArgs e)
    {
        string? dir = GetSaveDirectory();
        if (dir == null) { ShowNoDirWarning(); return; }
        int src = _slotSourceCombo.SelectedIndex;
        int dst = _slotDestCombo.SelectedIndex;
        if (src < 0 || dst < 0) return;
        if (src == dst) { MessageBox.Show(this, UiStrings.Get("player.slots_must_differ"), UiStrings.Get("player.swap_slots"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        var platform = GetDetectedPlatform();
        var result = MessageBox.Show(this, UiStrings.Format("player.swap_slot_confirm", src + 1, dst + 1),
            UiStrings.Get("player.swap_slots"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        try
        {
            SaveSlotManager.SwapSlots(dir, src, dst, platform);
            MessageBox.Show(this, UiStrings.Format("player.swap_slot_success", src + 1, dst + 1), UiStrings.Get("player.swap_slots"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            ReloadRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("player.swap_slot_failed", ex.Message), UiStrings.Get("player.swap_slots"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnDeleteSlot(object? sender, EventArgs e)
    {
        string? dir = GetSaveDirectory();
        if (dir == null) { ShowNoDirWarning(); return; }
        int src = _slotSourceCombo.SelectedIndex;
        if (src < 0) return;

        var platform = GetDetectedPlatform();
        var result = MessageBox.Show(this, UiStrings.Format("player.delete_slot_confirm", src + 1),
            UiStrings.Get("player.delete_slot"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        try
        {
            SaveSlotManager.DeleteSlot(dir, src, platform);
            MessageBox.Show(this, UiStrings.Format("player.delete_slot_success", src + 1), UiStrings.Get("player.delete_slot"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            ReloadRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("player.delete_slot_failed", ex.Message), UiStrings.Get("player.delete_slot"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnTransferPlatform(object? sender, EventArgs e)
    {
        if (_saveFilePath == null || _saveData == null) { ShowNoDirWarning(); return; }
        int destPlatformIdx = _transferPlatformCombo.SelectedIndex;
        if (destPlatformIdx < 0) return;
        int destSlot = _slotDestCombo.SelectedIndex;
        if (destSlot < 0) { MessageBox.Show(this, UiStrings.Get("player.transfer_select_dest"), UiStrings.Get("player.transfer_label"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        var destPlatform = TransferPlatformFromIndex(destPlatformIdx);

        using var dialog = new FolderBrowserDialog
        {
            Description = UiStrings.Get("player.transfer_dest_folder"),
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        var result = MessageBox.Show(this, 
            UiStrings.Format("player.transfer_cross_confirm", _transferPlatformCombo.Text, destSlot + 1, dialog.SelectedPath),
            UiStrings.Get("player.transfer_cross_title"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        try
        {
            SaveSlotManager.TransferCrossPlatform(_saveFilePath, dialog.SelectedPath, destSlot, destPlatform);
            MessageBox.Show(this, UiStrings.Get("player.transfer_cross_complete"), UiStrings.Get("player.transfer_cross_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("player.transfer_cross_failed", ex.Message), UiStrings.Get("player.transfer_cross_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void ShowNoDirWarning() =>
        MessageBox.Show(UiStrings.Get("player.no_save_loaded"), UiStrings.Get("player.save_utils_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private void OnGuideCellChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_accountData == null || e.RowIndex < 0) return;
        if (sender is not DataGridView grid) return;
        if (e.ColumnIndex != 3 && e.ColumnIndex != 4) return; // Only Seen/Unlocked columns

        try
        {
            var userData = _accountData.GetObject("UserSettingsData");
            if (userData == null) return;

            string topicId = grid.Rows[e.RowIndex].Cells[1].Value?.ToString() ?? "";
            bool value = (bool)(grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value ?? false);
            string arrayName = e.ColumnIndex == 3 ? "SeenWikiTopics" : "UnlockedWikiTopics";

            var arr = userData.GetArray(arrayName);
            if (arr == null) return;

            if (value)
            {
                bool found = false;
                for (int i = 0; i < arr.Length; i++)
                {
                    try { if (arr.GetString(i) == topicId) { found = true; break; } } catch { }
                }
                if (!found) arr.Add(topicId);
            }
            else
            {
                for (int i = arr.Length - 1; i >= 0; i--)
                {
                    try { if (arr.GetString(i) == topicId) { arr.RemoveAt(i); break; } } catch { }
                }
            }
        }
        catch { }
    }

    private void OnUnlockAllGuides(object? sender, EventArgs e)
    {
        foreach (var grid in _guidesGrids)
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                grid.Rows[i].Cells["Seen"].Value = true;
                grid.Rows[i].Cells["Unlocked"].Value = true;
            }
    }

    private void OnLockAllGuides(object? sender, EventArgs e)
    {
        foreach (var grid in _guidesGrids)
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                grid.Rows[i].Cells["Seen"].Value = false;
                grid.Rows[i].Cells["Unlocked"].Value = false;
            }
    }

    private void OnGuidesFilterChanged(object? sender, EventArgs e)
    {
        string filter = _guidesFilter.Text.Trim();
        foreach (var grid in _guidesGrids)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                string topicId = row.Cells["TopicId"].Value?.ToString() ?? "";
                string name = row.Cells["TopicName"].Value?.ToString() ?? "";
                string category = grid.Tag?.ToString() ?? "";
                row.Visible = string.IsNullOrEmpty(filter)
                    || topicId.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || category.Contains(filter, StringComparison.OrdinalIgnoreCase);
            }
            AutoSizeGridHeight(grid);
        }
    }

    // -- Titles --

    private void LoadTitles(JsonObject accountData)
    {
        _titlesGrid.Rows.Clear();

        if (!TitleDatabase.IsLoaded) return;

        try
        {
            // Use the same fallback as AccountLogic.LoadAccountData:
            // UserSettingsData may be a nested object or at the root level.
            var userData = accountData.GetObject("UserSettingsData") ?? accountData;
            var unlockedTitles = userData.GetArray("UnlockedTitles");
            var unlockedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (unlockedTitles != null)
            {
                for (int i = 0; i < unlockedTitles.Length; i++)
                {
                    // Values may be stored as string or BinaryData (hex-escaped in save file).
                    // Title IDs in the save are prefixed with '^' (e.g. "^T_TRA1") while our
                    // TitleDatabase uses unprefixed IDs (e.g. "T_TRA1"). Strip the prefix.
                    string? titleId = ExtractStringValue(unlockedTitles.Get(i));
                    if (!string.IsNullOrEmpty(titleId))
                    {
                        if (titleId.StartsWith('^'))
                            titleId = titleId.Substring(1);
                        unlockedSet.Add(titleId);
                    }
                }
            }

            foreach (var title in TitleDatabase.Titles)
            { 
                var fixedName = string.Format(CultureInfo.CurrentCulture, title.Name, PlayerName);
                int rowIdx = _titlesGrid.Rows.Add(title.Id, fixedName, title.UnlockDescription, unlockedSet.Contains(title.Id));
                _titlesGrid.Rows[rowIdx].Tag = title.Id;
            }
        }
        catch { }
    }

    /// <summary>
    /// Extract a string value from an object that may be a string or BinaryData.
    /// NMS save files may store string values as hex-escaped sequences (\xNN)
    /// which the JSON parser returns as BinaryData instead of string.
    /// </summary>
    private static string? ExtractStringValue(object? value)
    {
        if (value is string s) return s;
        if (value is BinaryData bin) return System.Text.Encoding.Latin1.GetString(bin.ToByteArray());
        return value?.ToString();
    }

    private void OnTitleCellChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_accountData == null || e.RowIndex < 0) return;
        if (e.ColumnIndex != 3) return; // Only handle "Unlocked" column changes

        try
        {
            var userData = _accountData.GetObject("UserSettingsData") ?? _accountData;

            string titleId = _titlesGrid.Rows[e.RowIndex].Tag?.ToString() ?? "";
            bool isUnlocked = (bool)(_titlesGrid.Rows[e.RowIndex].Cells["Unlocked"].Value ?? false);

            var unlockedTitles = userData.GetArray("UnlockedTitles");
            if (unlockedTitles == null) return;

            // Save file title IDs use '^' prefix (e.g. "^T_TRA1").
            // Our TitleDatabase IDs are unprefixed. Compare with both forms.
            string saveTitleId = "^" + titleId;

            // Check if title is already in the array
            bool found = false;
            for (int i = 0; i < unlockedTitles.Length; i++)
            {
                string? existing = ExtractStringValue(unlockedTitles.Get(i));
                // Match with or without ^ prefix
                if (string.Equals(existing, titleId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(existing, saveTitleId, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    if (!isUnlocked)
                        unlockedTitles.RemoveAt(i);
                    break;
                }
            }

            // Add with ^ prefix to match save file format
            if (isUnlocked && !found)
                unlockedTitles.Add(saveTitleId);
        }
        catch { }
    }

    private void OnUnlockAllTitles(object? sender, EventArgs e)
    {
        for (int i = 0; i < _titlesGrid.Rows.Count; i++)
            _titlesGrid.Rows[i].Cells["Unlocked"].Value = true;
    }

    private void OnLockAllTitles(object? sender, EventArgs e)
    {
        for (int i = 0; i < _titlesGrid.Rows.Count; i++)
            _titlesGrid.Rows[i].Cells["Unlocked"].Value = false;
    }

    public void ApplyUiLocalisation()
    {
        // Tab pages
        if (_tabs.TabCount >= 3)
        {
            _tabs.TabPages[0].Text = UiStrings.Get("player.tab_general");
            _tabs.TabPages[1].Text = UiStrings.Get("player.tab_guide");
            _tabs.TabPages[2].Text = UiStrings.Get("player.tab_titles");
        }

        // Buttons
        _copySlotBtn.Text = UiStrings.Get("player.copy_slot");
        _moveSlotBtn.Text = UiStrings.Get("player.move_slot");
        _swapSlotBtn.Text = UiStrings.Get("player.swap_slots");
        _deleteSlotBtn.Text = UiStrings.Get("player.delete_slot");
        _transferBtn.Text = UiStrings.Get("player.transfer_to_platform");
        _applyCoordinatesBtn.Text = UiStrings.Get("player.apply_coordinates");
        _convertPortalBtn.Text = UiStrings.Get("player.convert_coords");
        _coordinateRouletteBtn.Text = UiStrings.Get("player.coordinate_roulette");
        _triggerBattleBtn.Text = UiStrings.Get("player.trigger_space_battle");

        // Checkboxes
        _thirdPersonCharCam.Text = UiStrings.Get("player.third_person_camera");
        _portalInterference.Text = UiStrings.Get("player.portal_interference");

        // Section headers
        _playerStatsHeader.Text = UiStrings.Get("player.section_stats");
        _saveInfoHeader.Text = UiStrings.Get("player.save_info");
        _currentCoordsHeader.Text = UiStrings.Get("player.section_coordinates");
        _spaceBattleHeader.Text = UiStrings.Get("player.space_battle_section");
        _editCoordsHeader.Text = UiStrings.Get("player.section_edit_coords");
        _portalToCoordsHeader.Text = UiStrings.Get("player.section_portal_to_coords");
        _utilitiesHeader.Text = UiStrings.Get("player.section_save_utils");

        // Inline labels
        _sourceLabel.Text = UiStrings.Get("player.source_label");
        _destLabel.Text = UiStrings.Get("player.dest_label");
        _destPlatformLabel.Text = UiStrings.Get("player.dest_platform");
        _guidesTitle.Text = UiStrings.Get("player.guide_title");
        _guidesFilterLabel.Text = UiStrings.Get("player.guide_filter");
        _titlesTitle.Text = UiStrings.Get("player.titles_header");

        // Unlock/Lock buttons
        _guideUnlockAllBtn.Text = UiStrings.Get("player.guide_unlock_all");
        _guideLockAllBtn.Text = UiStrings.Get("player.guide_lock_all");
        _titlesUnlockAllBtn.Text = UiStrings.Get("player.titles_unlock_all");
        _titlesLockAllBtn.Text = UiStrings.Get("player.titles_lock_all");

        // Save utilities warning
        _saveUtilsWarning.Text = UiStrings.Get("player.save_utils_warning");

        // Guide category labels
        for (int i = 0; i < _guideCategoryLabels.Count && i < GuideCategories.Length; i++)
            _guideCategoryLabels[i].Text = UiStrings.GetOrNull($"player.guide_cat_{i}") ?? GuideCategories[i];

        // Stat labels
        _healthLabel.Text = UiStrings.Get("player.health");
        _shieldLabel.Text = UiStrings.Get("player.shield");
        _energyLabel.Text = UiStrings.Get("player.energy");
        _unitsLabel.Text = UiStrings.Get("player.units");
        _nanitesLabel.Text = UiStrings.Get("player.nanites");
        _quicksilverLabel.Text = UiStrings.Get("player.quicksilver");

        // Save info labels
        _saveNameLabel.Text = UiStrings.Get("player.save_name");
        _saveSummaryLabel.Text = UiStrings.Get("player.save_summary");
        _playTimeLabel.Text = UiStrings.Get("player.total_play_time");
        _lastSaveLabel.Text = UiStrings.Get("player.last_save_date");
        _currentPresetLabel.Text = UiStrings.Get("player.current_preset");
        _easiestPresetLabel.Text = UiStrings.Get("player.easiest_used");
        _hardestPresetLabel.Text = UiStrings.Get("player.hardest_used");
        _accountNameLabel.Text = UiStrings.Get("player.account_name");

        // Coordinate labels
        _galaxyLabel.Text = UiStrings.Get("player.galaxy");
        _galaxyCoreCaptionLabel.Text = string.Empty;
        _portalHexLabel.Text = UiStrings.Get("player.portal_code_hex");
        _portalDecLabel.Text = UiStrings.Get("player.portal_code_dec");
        _portalGlyphsLabel.Text = UiStrings.Get("player.portal_glyphs");
        _signalBoosterLabel.Text = UiStrings.Get("player.signal_booster");
        _playerStateLabel.Text = UiStrings.Get("player.player_state");
        _distanceToCenterLabel.Text = UiStrings.Get("player.distance_to_center");
        _jumpsToCenterLabel.Text = UiStrings.Get("player.jumps_to_center");
        _freighterInSystemLabel.Text = UiStrings.Get("player.freighter_in_system");
        _nexusInSystemLabel.Text = UiStrings.Get("player.nexus_in_system");
        _planetsInSystemLabel.Text = UiStrings.Get("player.planets_in_system");

        // Space battle labels
        _warpsToNextLabel.Text = UiStrings.Get("player.warps_to_next");
        _timeToNextLabel.Text = UiStrings.Get("player.time_to_next");

        // Edit coordinate labels
        _galaxyRangeLabel.Text = UiStrings.Get("player.galaxy_range");
        _voxelXLabel.Text = UiStrings.Get("player.voxel_x");
        _voxelYLabel.Text = UiStrings.Get("player.voxel_y");
        _voxelZLabel.Text = UiStrings.Get("player.voxel_z");
        _solarSystemLabel.Text = UiStrings.Get("player.solar_system");
        _planetLabel.Text = UiStrings.Get("player.planet_range");
        _glyphButtonLabel.Text = UiStrings.Get("player.glyphs_label");
        _editPortalHexLabel.Text = UiStrings.Get("player.portal_code_hex");

        // Guide grid column headers
        foreach (var grid in _guidesGrids)
        {
            if (grid.Columns["TopicId"] is DataGridViewColumn topicId) topicId.HeaderText = UiStrings.Get("player.guide_col_topic_id");
            if (grid.Columns["TopicName"] is DataGridViewColumn topicName) topicName.HeaderText = UiStrings.Get("player.guide_col_name");
            if (grid.Columns["Seen"] is DataGridViewColumn seen) seen.HeaderText = UiStrings.Get("player.guide_col_seen");
            if (grid.Columns["Unlocked"] is DataGridViewColumn unlocked) unlocked.HeaderText = UiStrings.Get("player.guide_col_unlocked");
        }

        // Titles grid column headers
        if (_titlesGrid.Columns["TitleId"] is DataGridViewColumn titleId) titleId.HeaderText = UiStrings.Get("player.titles_col_id");
        if (_titlesGrid.Columns["TitleName"] is DataGridViewColumn titleName) titleName.HeaderText = UiStrings.Get("player.titles_col_title");
        if (_titlesGrid.Columns["Description"] is DataGridViewColumn desc) desc.HeaderText = UiStrings.Get("player.titles_col_description");
        if (_titlesGrid.Columns["Unlocked"] is DataGridViewColumn titleUnlocked) titleUnlocked.HeaderText = UiStrings.Get("player.titles_col_unlocked");

        // Platform combo display names
        int platformIdx = _transferPlatformCombo.SelectedIndex;
        _transferPlatformCombo.Items.Clear();
        _transferPlatformCombo.Items.AddRange(new object[] {
            UiStrings.Get("player.platform_steam"),
            UiStrings.Get("player.platform_gog"),
            UiStrings.Get("player.platform_xbox"),
            UiStrings.Get("player.platform_ps4"),
            UiStrings.Get("player.platform_switch")
        });
        if (platformIdx >= 0 && platformIdx < _transferPlatformCombo.Items.Count)
            _transferPlatformCombo.SelectedIndex = platformIdx;

        // Difficulty preset combo display names
        RefreshPresetCombo(_currentPresetCombo);
        RefreshPresetCombo(_easiestPresetCombo);
        RefreshPresetCombo(_hardestPresetCombo);

        // Player state combo display names
        RefreshPlayerStateCombo();
    }

    private static void RefreshPresetCombo(ComboBox combo)
    {
        int idx = combo.SelectedIndex;
        combo.Items.Clear();
        for (int i = 0; i < DifficultyPresetLocKeys.Length; i++)
            combo.Items.Add(UiStrings.Get(DifficultyPresetLocKeys[i]));
        if (idx >= 0 && idx < combo.Items.Count)
            combo.SelectedIndex = idx;
    }

    private void RefreshPlayerStateCombo()
    {
        int idx = _playerStateField.SelectedIndex;
        _playerStateField.Items.Clear();
        for (int i = 0; i < CoordinateHelper.PlayerStateLocKeys.Length; i++)
            _playerStateField.Items.Add(UiStrings.Get(CoordinateHelper.PlayerStateLocKeys[i]));
        if (idx >= 0 && idx < _playerStateField.Items.Count)
            _playerStateField.SelectedIndex = idx;
    }
}
