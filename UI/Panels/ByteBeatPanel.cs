using NMSE.Core;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

public partial class ByteBeatPanel : UserControl
{
    private JsonObject? _library;
    private JsonArray? _mySongs;
    private bool _loading;
    private int _previousSongIndex = -1;

    public ByteBeatPanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    public void LoadData(JsonObject saveData)
    {
        SuspendLayout();
        _songList.BeginUpdate();
        try
        {
        _songList.Items.Clear();
        _detailPanel.Visible = false;
        _library = null;
        _mySongs = null;
        _previousSongIndex = -1;

        try
        {
            var commonState = saveData.GetObject("CommonStateData");
            if (commonState == null) return;

            _library = commonState.GetObject("ByteBeatLibrary");
            if (_library == null)
            {
                _infoLabel.Text = UiStrings.Get("bytebeat.no_library");
                return;
            }

            _mySongs = _library.GetArray("MySongs");
            if (_mySongs == null || _mySongs.Length == 0)
            {
                _infoLabel.Text = UiStrings.Get("bytebeat.no_songs_found");
                return;
            }

            RefreshList();

            // Load library settings
            _loading = true;
            try
            {
                try { _shuffleField.Checked = _library.GetBool("Shuffle"); } catch { _shuffleField.Checked = false; }
                try { _autoplayOnFootField.Checked = _library.GetBool("AutoplayOnFoot"); } catch { _autoplayOnFootField.Checked = false; }
                try { _autoplayInShipField.Checked = _library.GetBool("AutoplayInShip"); } catch { _autoplayInShipField.Checked = false; }
                try { _autoplayInVehicleField.Checked = _library.GetBool("AutoplayInVehicle"); } catch { _autoplayInVehicleField.Checked = false; }
            }
            finally { _loading = false; }
        }
        catch { _infoLabel.Text = UiStrings.Get("bytebeat.failed_load"); }
        }
        finally
        {
            _songList.EndUpdate();
            ResumeLayout(true);
        }
    }

    public void SaveData(JsonObject saveData)
    {
        if (_library == null) return;

        // Save current song details
        SaveCurrentSong();

        // Save library settings
        try { _library.Set("Shuffle", _shuffleField.Checked); } catch { }
        try { _library.Set("AutoplayOnFoot", _autoplayOnFootField.Checked); } catch { }
        try { _library.Set("AutoplayInShip", _autoplayInShipField.Checked); } catch { }
        try { _library.Set("AutoplayInVehicle", _autoplayInVehicleField.Checked); } catch { }
    }

    private void SaveCurrentSong()
    {
        SaveSongAtIndex(_songList.SelectedIndex);
    }

    private void SaveSongAtIndex(int idx)
    {
        if (_loading) return;
        if (idx < 0 || _mySongs == null || idx >= _mySongs.Length) return;
        JsonObject? song;
        try { song = _mySongs.GetObject(idx); } catch { return; }
        if (song == null) return;

        try { song.Set("Name", _nameField.Text); } catch { }
        try { song.Set("AuthorUsername", _authorUsernameField.Text); } catch { }
        try { song.Set("AuthorOnlineID", _authorOnlineIdField.Text); } catch { }
        try { song.Set("AuthorPlatform", _authorPlatformField.Text); } catch { }

        var dataArr = song.GetArray("Data");
        if (dataArr != null)
        {
            for (int i = 0; i < 8 && i < dataArr.Length; i++)
            {
                try { dataArr.Set(i, _dataFields[i].Text); } catch { }
            }
        }

        // Update the song list display name
        string name = _nameField.Text;
        string displayName = string.IsNullOrWhiteSpace(name) ? UiStrings.Format("bytebeat.song_format", idx + 1) : name;
        if (idx < _songList.Items.Count)
        {
            _songList.SelectedIndexChanged -= OnSongSelected;
            _songList.Items[idx] = displayName;
            _songList.SelectedIndexChanged += OnSongSelected;
        }
    }

    private JsonObject? SelectedSong()
    {
        int idx = _songList.SelectedIndex;
        if (idx < 0 || _mySongs == null || idx >= _mySongs.Length) return null;
        try { return _mySongs.GetObject(idx); } catch { return null; }
    }

    private void RefreshList()
    {
        int sel = _songList.SelectedIndex;
        _songList.BeginUpdate();
        try
        {
        _songList.Items.Clear();
        if (_mySongs == null) return;

        for (int i = 0; i < _mySongs.Length; i++)
        {
            try
            {
                var song = _mySongs.GetObject(i);
                string name = song.GetString("Name") ?? "";
                _songList.Items.Add(string.IsNullOrWhiteSpace(name) ? UiStrings.Format("bytebeat.song_format", i + 1) : name);
            }
            catch { _songList.Items.Add(UiStrings.Format("bytebeat.song_format", i + 1)); }
        }

        _infoLabel.Text = UiStrings.Format("bytebeat.total_songs", _mySongs.Length);
        if (sel >= 0 && sel < _songList.Items.Count)
            _songList.SelectedIndex = sel;
        }
        finally
        {
            _songList.EndUpdate();
        }
    }

    private void OnSongSelected(object? sender, EventArgs e)
    {
        // Save the previous song's data before loading the new selection
        if (_previousSongIndex >= 0)
            SaveSongAtIndex(_previousSongIndex);

        var song = SelectedSong();
        if (song == null)
        {
            _detailPanel.Visible = false;
            _previousSongIndex = -1;
            return;
        }

        _loading = true;
        try
        {
            _detailPanel.Visible = true;
            _nameField.Text = song.GetString("Name") ?? "";
            _authorUsernameField.Text = song.GetString("AuthorUsername") ?? "";
            _authorOnlineIdField.Text = song.GetString("AuthorOnlineID") ?? "";
            _authorPlatformField.Text = song.GetString("AuthorPlatform") ?? "";

            var dataArr = song.GetArray("Data");
            for (int i = 0; i < 8; i++)
            {
                try { _dataFields[i].Text = dataArr != null && i < dataArr.Length ? (dataArr.GetString(i) ?? "") : ""; }
                catch { _dataFields[i].Text = ""; }
            }
        }
        catch { }
        finally { _loading = false; }

        _previousSongIndex = _songList.SelectedIndex;
    }

    private void OnExport(object? sender, EventArgs e)
    {
        var song = SelectedSong();
        if (song == null)
        {
            MessageBox.Show(this, UiStrings.Get("bytebeat.no_song_selected"), UiStrings.Get("common.export"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SaveCurrentSong();

        var config = ExportConfig.Instance;
        var vars = new Dictionary<string, string>
        {
            ["name"] = song.GetString("Name") ?? "song",
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        using var dialog = new SaveFileDialog
        {
            Filter = ExportConfig.BuildDialogFilter(config.ByteBeatExt, "ByteBeat songs"),
            DefaultExt = config.ByteBeatExt.TrimStart('.'),
            FileName = ExportConfig.BuildFileName(config.ByteBeatTemplate, config.ByteBeatExt, vars)
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                song.ExportToFile(dialog.FileName);
                MessageBox.Show(this, UiStrings.Get("bytebeat.export_success"), UiStrings.Get("common.export"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, UiStrings.Format("common.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnImport(object? sender, EventArgs e)
    {
        int idx = _songList.SelectedIndex;
        if (idx < 0 || _mySongs == null || idx >= _mySongs.Length)
        {
            MessageBox.Show(this, UiStrings.Get("bytebeat.no_song_selected"), UiStrings.Get("common.import"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var config = ExportConfig.Instance;
        using var dialog = new OpenFileDialog
        {
            Filter = ExportConfig.BuildOpenFilter(config.ByteBeatExt, "ByteBeat songs")
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var imported = JsonObject.ImportFromFile(dialog.FileName);
            var song = _mySongs.GetObject(idx);

            foreach (var propName in imported.Names())
                song.Set(propName, imported.Get(propName));

            OnSongSelected(null, EventArgs.Empty);
            RefreshList();
            MessageBox.Show(this, UiStrings.Get("bytebeat.import_success"), UiStrings.Get("common.import"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("common.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnDeleteSong(object? sender, EventArgs e)
    {
        int idx = _songList.SelectedIndex;
        if (idx < 0 || _mySongs == null || idx >= _mySongs.Length)
        {
            MessageBox.Show(this, UiStrings.Get("bytebeat.no_song_selected"), UiStrings.Get("common.delete"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show(this, UiStrings.Get("bytebeat.delete_confirm"),
            UiStrings.Get("bytebeat.delete_title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        try
        {
            var song = _mySongs.GetObject(idx);
            // Clear the song by resetting all fields to empty/defaults
            song.Set("Name", "");
            song.Set("AuthorUsername", "");
            song.Set("AuthorOnlineID", "");
            song.Set("AuthorPlatform", "");
            var dataArr = song.GetArray("Data");
            if (dataArr != null)
            {
                for (int i = 0; i < dataArr.Length; i++)
                    dataArr.Set(i, "");
            }

            OnSongSelected(null, EventArgs.Empty);
            RefreshList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("bytebeat.delete_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "unnamed";
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c));
    }

    private static Label AddRow(TableLayoutPanel layout, string label, Control field, int row)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 10, 0) };
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
            Padding = new Padding(0, 8, 0, 2)
        };
        FontManager.ApplyHeadingFont(lbl, 10);
        layout.Controls.Add(lbl, 0, row);
        layout.SetColumnSpan(lbl, 2);
        return lbl;
    }

    public void ApplyUiLocalisation()
    {
        _titleLabel.Text = UiStrings.Get("bytebeat.title");
        _exportBtn.Text = UiStrings.Get("common.export");
        _importBtn.Text = UiStrings.Get("common.import");
        _deleteBtn.Text = UiStrings.Get("common.delete");
        _infoLabel.Text = UiStrings.Get("bytebeat.no_songs");
        _shuffleField.Text = UiStrings.Get("bytebeat.shuffle");
        _autoplayOnFootField.Text = UiStrings.Get("bytebeat.autoplay_foot");
        _autoplayInShipField.Text = UiStrings.Get("bytebeat.autoplay_ship");
        _autoplayInVehicleField.Text = UiStrings.Get("bytebeat.autoplay_vehicle");

        _sectionDetailsLabel.Text = UiStrings.Get("bytebeat.section_details");
        _nameLabel.Text = UiStrings.Get("bytebeat.name");
        _authorUsernameLabel.Text = UiStrings.Get("bytebeat.author_username");
        _authorOnlineIdLabel.Text = UiStrings.Get("bytebeat.author_online_id");
        _authorPlatformLabel.Text = UiStrings.Get("bytebeat.author_platform");
        _sectionDataLabel.Text = UiStrings.Get("bytebeat.section_data");
        for (int i = 0; i < _dataLabels.Length; i++)
            _dataLabels[i].Text = string.Format(UiStrings.Get("bytebeat.data_channel"), i);
        _sectionLibraryLabel.Text = UiStrings.Get("bytebeat.section_library");
    }
}
