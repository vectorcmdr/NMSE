using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.ViewModels.Panels;

public partial class ByteBeatSongViewModel : ObservableObject
{
    [ObservableProperty] private string _displayName = "";
    public int Index { get; set; }
}

public partial class ByteBeatViewModel : PanelViewModelBase
{
    private JsonObject? _library;
    private JsonArray? _mySongs;
    private bool _loading;
    private int _previousSongIndex = -1;

    [ObservableProperty] private ObservableCollection<ByteBeatSongViewModel> _songList = new();
    [ObservableProperty] private int _selectedSongIndex = -1;
    [ObservableProperty] private string _infoText = "";
    [ObservableProperty] private bool _isDetailVisible;

    [ObservableProperty] private string _songName = "";
    [ObservableProperty] private string _authorUsername = "";
    [ObservableProperty] private string _authorOnlineId = "";
    [ObservableProperty] private string _authorPlatform = "";

    [ObservableProperty] private string _data0 = "";
    [ObservableProperty] private string _data1 = "";
    [ObservableProperty] private string _data2 = "";
    [ObservableProperty] private string _data3 = "";
    [ObservableProperty] private string _data4 = "";
    [ObservableProperty] private string _data5 = "";
    [ObservableProperty] private string _data6 = "";
    [ObservableProperty] private string _data7 = "";

    [ObservableProperty] private bool _shuffle;
    [ObservableProperty] private bool _autoplayOnFoot;
    [ObservableProperty] private bool _autoplayInShip;
    [ObservableProperty] private bool _autoplayInVehicle;

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        try
        {
            SongList.Clear();
            IsDetailVisible = false;
            _library = null;
            _mySongs = null;
            _previousSongIndex = -1;

            var commonState = saveData.GetObject("CommonStateData");
            if (commonState == null) return;

            _library = commonState.GetObject("ByteBeatLibrary");
            if (_library == null)
            {
                InfoText = "No ByteBeat library found";
                return;
            }

            _mySongs = _library.GetArray("MySongs");
            if (_mySongs == null || _mySongs.Length == 0)
            {
                InfoText = "No songs found";
                return;
            }

            RefreshSongList();

            _loading = true;
            try
            {
                try { Shuffle = _library.GetBool("Shuffle"); } catch { Shuffle = false; }
                try { AutoplayOnFoot = _library.GetBool("AutoplayOnFoot"); } catch { AutoplayOnFoot = false; }
                try { AutoplayInShip = _library.GetBool("AutoplayInShip"); } catch { AutoplayInShip = false; }
                try { AutoplayInVehicle = _library.GetBool("AutoplayInVehicle"); } catch { AutoplayInVehicle = false; }
            }
            finally { _loading = false; }
        }
        catch { InfoText = "Failed to load ByteBeat data"; }
    }

    public override void SaveData(JsonObject saveData)
    {
        if (_library == null) return;

        SaveCurrentSong();

        try { _library.Set("Shuffle", Shuffle); } catch { }
        try { _library.Set("AutoplayOnFoot", AutoplayOnFoot); } catch { }
        try { _library.Set("AutoplayInShip", AutoplayInShip); } catch { }
        try { _library.Set("AutoplayInVehicle", AutoplayInVehicle); } catch { }
    }

    partial void OnSelectedSongIndexChanged(int value)
    {
        if (_previousSongIndex >= 0)
            SaveSongAtIndex(_previousSongIndex);

        if (value < 0 || _mySongs == null || value >= _mySongs.Length)
        {
            IsDetailVisible = false;
            _previousSongIndex = -1;
            return;
        }

        _loading = true;
        try
        {
            var song = _mySongs.GetObject(value);
            IsDetailVisible = true;
            SongName = song.GetString("Name") ?? "";
            AuthorUsername = song.GetString("AuthorUsername") ?? "";
            AuthorOnlineId = song.GetString("AuthorOnlineID") ?? "";
            AuthorPlatform = song.GetString("AuthorPlatform") ?? "";

            var dataArr = song.GetArray("Data");
            SetDataField(0, dataArr, v => Data0 = v);
            SetDataField(1, dataArr, v => Data1 = v);
            SetDataField(2, dataArr, v => Data2 = v);
            SetDataField(3, dataArr, v => Data3 = v);
            SetDataField(4, dataArr, v => Data4 = v);
            SetDataField(5, dataArr, v => Data5 = v);
            SetDataField(6, dataArr, v => Data6 = v);
            SetDataField(7, dataArr, v => Data7 = v);
        }
        catch { }
        finally { _loading = false; }

        _previousSongIndex = value;
    }

    private static void SetDataField(int index, JsonArray? dataArr, Action<string> setter)
    {
        try { setter(dataArr != null && index < dataArr.Length ? (dataArr.GetString(index) ?? "") : ""); }
        catch { setter(""); }
    }

    private void SaveCurrentSong()
    {
        SaveSongAtIndex(SelectedSongIndex);
    }

    private void SaveSongAtIndex(int idx)
    {
        if (_loading) return;
        if (idx < 0 || _mySongs == null || idx >= _mySongs.Length) return;

        JsonObject? song;
        try { song = _mySongs.GetObject(idx); } catch { return; }
        if (song == null) return;

        try { song.Set("Name", SongName); } catch { }
        try { song.Set("AuthorUsername", AuthorUsername); } catch { }
        try { song.Set("AuthorOnlineID", AuthorOnlineId); } catch { }
        try { song.Set("AuthorPlatform", AuthorPlatform); } catch { }

        var dataArr = song.GetArray("Data");
        if (dataArr != null)
        {
            string[] dataValues = [Data0, Data1, Data2, Data3, Data4, Data5, Data6, Data7];
            for (int i = 0; i < 8 && i < dataArr.Length; i++)
            {
                try { dataArr.Set(i, dataValues[i]); } catch { }
            }
        }

        if (idx < SongList.Count)
        {
            string displayName = string.IsNullOrWhiteSpace(SongName) ? $"Song {idx + 1}" : SongName;
            SongList[idx].DisplayName = displayName;
        }
    }

    private void RefreshSongList()
    {
        SongList.Clear();
        if (_mySongs == null) return;

        for (int i = 0; i < _mySongs.Length; i++)
        {
            try
            {
                var song = _mySongs.GetObject(i);
                string name = song.GetString("Name") ?? "";
                SongList.Add(new ByteBeatSongViewModel
                {
                    DisplayName = string.IsNullOrWhiteSpace(name) ? $"Song {i + 1}" : name,
                    Index = i
                });
            }
            catch
            {
                SongList.Add(new ByteBeatSongViewModel { DisplayName = $"Song {i + 1}", Index = i });
            }
        }

        InfoText = $"Total songs: {_mySongs.Length}";
    }

    [RelayCommand]
    private async Task ExportSong()
    {
        if (_mySongs == null || SelectedSongIndex < 0 || SelectedSongIndex >= _mySongs.Length || SaveFilePickerFunc == null) return;
        SaveCurrentSong();
        var song = _mySongs.GetObject(SelectedSongIndex);
        var cfg = ExportConfig.Instance;
        var vars = new Dictionary<string, string>
        {
            ["name"] = song.GetString("Name") ?? "song",
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
        };
        string? path = await SaveFilePickerFunc("Export Song", cfg.ByteBeatExt.TrimStart('.'),
            ExportConfig.BuildDialogFilter(cfg.ByteBeatExt, "ByteBeat songs"));
        if (string.IsNullOrEmpty(path)) return;
        try { song.ExportToFile(path); } catch { }
    }

    [RelayCommand]
    private async Task ImportSong()
    {
        int idx = SelectedSongIndex;
        if (idx < 0 || _mySongs == null || idx >= _mySongs.Length || OpenFilePickerFunc == null) return;
        string? path = await OpenFilePickerFunc("Import Song",
            ExportConfig.BuildOpenFilter(ExportConfig.Instance.ByteBeatExt, "ByteBeat songs"));
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            var imported = JsonObject.ImportFromFile(path);
            var song = _mySongs.GetObject(idx);
            foreach (var propName in imported.Names())
                song.Set(propName, imported.Get(propName));

            _previousSongIndex = -1;
            OnSelectedSongIndexChanged(idx);
            RefreshSongList();
        }
        catch { }
    }

    [RelayCommand]
    private void DeleteSong()
    {
        int idx = SelectedSongIndex;
        if (idx < 0 || _mySongs == null || idx >= _mySongs.Length) return;

        try
        {
            var song = _mySongs.GetObject(idx);
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

            _previousSongIndex = -1;
            RefreshSongList();
            if (idx < SongList.Count)
                SelectedSongIndex = idx;
        }
        catch { }
    }
}
