using System.Globalization;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

/// <summary>
/// Unified list for one catalogue item tab. Every valid item is shown, with separate
/// Known and Seen checkboxes and a read-only Catalogue Y/N marker.
/// </summary>
internal sealed class CatalogueItemsPanel : UserControl
{
	private readonly CatalogueItemKind _kind;
	private readonly DataGridView _grid;
	private readonly TextBox _filterBox;
	private readonly CheckBox _catalogueOnly;
	private readonly CheckBox _showSeen;
	private readonly Button _addAllButton;
	private readonly Button _removeAllButton;
	private readonly Label _allCounterLabel;
	private readonly Label _counterLabel;
	private readonly Button _exportButton;
	private readonly Button _importButton;
	private readonly Dictionary<string, Image> _iconCache = new(StringComparer.OrdinalIgnoreCase);
	private static readonly Bitmap PlaceholderIcon = new(24, 24);
	private static readonly List<WeakReference<CatalogueItemsPanel>> OpenPanels = [];
	private static bool _showSeenForSession;

	private CatalogueItemCatalog? _catalog;
	private IconManager? _iconManager;
	private GameItemDatabase? _database;
	private bool _refreshing;
	private bool _syncingShowSeen;

	/// <summary>Raised when the catalogue state changes.</summary>
	internal event EventHandler? DataModified;

	/// <summary>Raised when the user requests the Raw JSON Editor at this tab's array.</summary>
	internal event EventHandler<GoToJsonEventArgs>? GoToJsonRequested;

	/// <summary>Creates a unified item list for one kind.</summary>
	/// <param name="kind">The item kind shown by the panel.</param>
	internal CatalogueItemsPanel(CatalogueItemKind kind)
	{
		_kind = kind;
		Dock = DockStyle.Fill;

		_grid = new DataGridView
		{
			Dock = DockStyle.Fill,
			AllowUserToAddRows = false,
			AllowUserToDeleteRows = false,
			AllowUserToResizeRows = false,
			AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
			SelectionMode = DataGridViewSelectionMode.FullRowSelect,
			MultiSelect = true,
			RowHeadersVisible = false,
			ReadOnly = true,
			RowTemplate = { Height = 27 },
		};

		_grid.Columns.Add(new DataGridViewImageColumn
		{
			Name = "Icon",
			HeaderText = string.Empty,
			Width = 34,
			ImageLayout = DataGridViewImageCellLayout.Zoom,
			AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
		});
		_grid.Columns.Add(CreateTextColumn("Name", "Name", 1, DataGridViewAutoSizeColumnMode.None));
		_grid.Columns.Add(CreateTextColumn("ID", "ID", 1, DataGridViewAutoSizeColumnMode.None));
		_grid.Columns.Add(CreateTextColumn("Category", "Category", 1, DataGridViewAutoSizeColumnMode.None));
		_grid.Columns.Add(CreateTextColumn("Expedition", "Expedition", 1, DataGridViewAutoSizeColumnMode.None));
		_grid.Columns.Add(CreateTextColumn("Description", "Description", 100, DataGridViewAutoSizeColumnMode.Fill));
		_grid.Columns.Add(CreateTextColumn("Catalogue", "Catalogue", 1, DataGridViewAutoSizeColumnMode.None));
		_grid.Columns.Add(new DataGridViewCheckBoxColumn
		{
			Name = "Known",
			HeaderText = "Known",
			ThreeState = true,
			AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
			SortMode = DataGridViewColumnSortMode.NotSortable,
		});
		_grid.Columns.Add(new DataGridViewCheckBoxColumn
		{
			Name = "Seen",
			HeaderText = "Seen",
			ThreeState = false,
			Visible = _showSeenForSession,
			AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
			SortMode = DataGridViewColumnSortMode.NotSortable,
		});

		_grid.CellClick += Grid_CellClick;
		_grid.CellToolTipTextNeeded += Grid_CellToolTipTextNeeded;

		_filterBox = new TextBox
		{
			Width = 220,
			MinimumSize = new Size(0, 25),
			PlaceholderText = "Filter by name, category or ID...",
		};
		_filterBox.TextChanged += (_, _) => ApplyFilter();
		var clearButton = new Button
		{
			Text = "X",
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			MinimumSize = new Size(28, 25),
		};
		clearButton.Click += (_, _) => _filterBox.Text = string.Empty;

		_catalogueOnly = new CheckBox
		{
			Text = "Catalogue only",
			AutoSize = false,
			Width = 130,
			Height = 25,
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = new Padding(8, 3, 4, 0),
		};
		_catalogueOnly.CheckedChanged += (_, _) => ApplyFilter();

		_showSeen = new CheckBox
		{
			Text = "Show seen",
			AutoSize = false,
			Width = 110,
			Height = 25,
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = new Padding(8, 3, 4, 0),
			Checked = _showSeenForSession,
		};
		_showSeen.CheckedChanged += (_, _) => SetSeenColumnVisible(_showSeen.Checked);

		var gotoButton = new Button
		{
			FlatStyle = FlatStyle.Flat,
			FlatAppearance =
			{
				BorderColor = ThemeManager.Effective == AppTheme.Dark
					? Color.FromArgb(100, 100, 100)
					: SystemColors.ControlDark,
				BorderSize = 1,
			},
			Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular, GraphicsUnit.Point),
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			MinimumSize = new Size(28, 25),
			Text = "\U0001F4D1",
			Margin = new Padding(1, 3, 6, 1),
			Cursor = Cursors.Hand,
		};
		gotoButton.Click += (_, _) => GoToJsonRequested?.Invoke(this, new GoToJsonEventArgs(_catalog?.JsonPath ?? ["PlayerStateData"]));

		var filterFlow = new FlowLayoutPanel
		{
			AutoSize = true,
			WrapContents = false,
			FlowDirection = FlowDirection.LeftToRight,
			Margin = new Padding(0),
		};
		filterFlow.Controls.Add(_filterBox);
		filterFlow.Controls.Add(clearButton);
		filterFlow.Controls.Add(_catalogueOnly);
		filterFlow.Controls.Add(_showSeen);

		var filterPanel = new TableLayoutPanel
		{
			AutoSize = true,
			Dock = DockStyle.Top,
			ColumnCount = 3,
			RowCount = 1,
			Margin = new Padding(0),
		};
		filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		filterPanel.Controls.Add(filterFlow, 0, 0);
		filterPanel.Controls.Add(gotoButton, 2, 0);

		_allCounterLabel = new Label
		{
			AutoSize = true,
			Padding = new Padding(0, 8, 8, 0),
			Font = FontManager.CreateFont(9, FontStyle.Bold),
		};

		_counterLabel = new Label
		{
			AutoSize = true,
			Padding = new Padding(0, 8, 8, 0),
			Font = FontManager.CreateFont(9, FontStyle.Bold),
		};

		_addAllButton = new Button
		{
			Text = "Add All",
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
		};
		_addAllButton.Click += (_, _) => AddAllVisible();

		_removeAllButton = new Button
		{
			Text = "Remove All",
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
		};
		_removeAllButton.Click += (_, _) => RemoveAllVisible();

		_exportButton = new Button
		{
			Text = "Export",
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
		};
		_exportButton.Click += (_, _) => ExportKnown();

		_importButton = new Button
		{
			Text = "Import",
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
		};
		_importButton.Click += (_, _) => ImportKnown();

		var buttonFlow = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			AutoSize = true,
			FlowDirection = FlowDirection.LeftToRight,
		};
		buttonFlow.Controls.Add(_allCounterLabel);
		buttonFlow.Controls.Add(_counterLabel);
		buttonFlow.Controls.Add(_addAllButton);
		buttonFlow.Controls.Add(_removeAllButton);
		buttonFlow.Controls.Add(_exportButton);
		buttonFlow.Controls.Add(_importButton);

		var layout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 3,
		};
		layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		layout.Controls.Add(filterPanel, 0, 0);
		layout.Controls.Add(_grid, 0, 1);
		layout.Controls.Add(buttonFlow, 0, 2);
		OpenPanels.Add(new WeakReference<CatalogueItemsPanel>(this));
		Controls.Add(layout);
	}

	/// <summary>The item kind shown by this panel.</summary>
	internal CatalogueItemKind Kind => _kind;

	/// <summary>Sets the item database used for icons.</summary>
	/// <param name="database">The item database, or null.</param>
	internal void SetDatabase(GameItemDatabase? database) => _database = database;

	/// <summary>Sets the icon manager and refreshes existing rows.</summary>
	/// <param name="iconManager">The icon manager, or null.</param>
	internal void SetIconManager(IconManager? iconManager)
	{
		_iconManager = iconManager;
		if (_catalog == null) return;
		foreach (DataGridViewRow row in _grid.Rows)
		{
			if (row.Tag is not CatalogueItemEntry entry) continue;
			row.Cells["Icon"].Value = GetIcon(entry.Id) ?? PlaceholderIcon;
		}
	}

	/// <summary>Loads a built catalog into the list.</summary>
	/// <param name="catalog">The catalog, or null when data is unavailable.</param>
	internal void LoadCatalog(CatalogueItemCatalog? catalog)
	{
		_catalog = catalog;
		_grid.SuspendLayout();
		try
		{
			_grid.Rows.Clear();
			if (catalog == null)
			{
				_allCounterLabel.Text = string.Empty;
				_counterLabel.Text = string.Empty;
				_addAllButton.Enabled = false;
				_removeAllButton.Enabled = false;
				_exportButton.Enabled = false;
				_importButton.Enabled = false;
				return;
			}

			int descriptionColumn = _grid.Columns["Description"] is DataGridViewColumn description
				? description.Index
				: -1;
			var rows = new List<DataGridViewRow>(catalog.Entries.Count);
			foreach (var entry in catalog.Entries)
			{
				var row = new DataGridViewRow();
				row.CreateCells(
					_grid,
					GetIcon(entry.Id) ?? PlaceholderIcon,
					entry.Name,
					entry.Id,
					entry.Category,
					entry.ExpeditionNumber?.ToString(CultureInfo.InvariantCulture) ?? "-",
					entry.Description.Replace('\r', ' ').Replace('\n', ' '),
					entry.IsCatalogue ? "Y" : "N",
					CheckState.Unchecked,
					CheckState.Unchecked);
				if (descriptionColumn >= 0)
					row.Cells[descriptionColumn].ToolTipText = entry.Description;
				row.Tag = entry;
				rows.Add(row);
			}
			_grid.Rows.AddRange(rows.ToArray());
		}
		finally
		{
			_grid.ResumeLayout(true);
		}

		ApplyFilter();
		AdjustColumnWidths();
		RefreshStates();
		_addAllButton.Enabled = catalog.Entries.Count > 0;
		_removeAllButton.Enabled = catalog.Entries.Count > 0;
		_exportButton.Enabled = catalog.Entries.Count > 0;
		_importButton.Enabled = catalog.Entries.Count > 0;
	}

	/// <summary>Recomputes checkbox states and counters from the catalog.</summary>
	internal void RefreshStates()
	{
		if (_catalog == null)
		{
			_allCounterLabel.Text = string.Empty;
			_counterLabel.Text = string.Empty;
			return;
		}

		_refreshing = true;
		try
		{
			foreach (DataGridViewRow row in _grid.Rows)
			{
				if (row.Tag is not CatalogueItemEntry entry) continue;
				row.Cells["Known"].Value = GetCheckState(entry);
				row.Cells["Seen"].Value = _catalog.IsSeen(entry)
					? CheckState.Checked
					: CheckState.Unchecked;
			}
		}
		finally
		{
			_refreshing = false;
		}

		RefreshCounter();
	}

	/// <summary>Reloads known state from the save and account objects without rebuilding rows.</summary>
	internal void RefreshFromData()
	{
		_catalog?.RefreshKnownSets();
		RefreshStates();
	}

	/// <summary>Adds every catalogue-counted row, ignoring the current filter.</summary>
	/// <returns>The number of rows whose state changed.</returns>
	internal int AddAllCatalogue()
	{
		if (_catalog == null) return 0;
		int changed = _catalog.SetKnown(
			_catalog.Entries.Where(entry => entry.IsCatalogue && entry.CanAddKnown),
			true);
		RefreshStates();
		return changed;
	}

	/// <summary>Counts catalogue rows that the game does not yet treat as visible.</summary>
	internal int CountMissingCatalogue()
	{
		if (_catalog == null) return 0;
		return _catalog.Entries.Count(entry => entry.IsCatalogue && !_catalog.IsCatalogueVisible(entry));
	}

	/// <summary>Applies the current UI localisation.</summary>
	internal void ApplyUiLocalisation()
	{
		if (_grid.Columns["Known"] is DataGridViewColumn known)
		{
			known.HeaderText = UiStrings.Get("discovery.col_known");
			known.ToolTipText = UiStrings.Get("discovery.known_help");
		}
		if (_grid.Columns["Seen"] is DataGridViewColumn seen)
		{
			seen.HeaderText = UiStrings.Get("discovery.col_seen");
			seen.ToolTipText = UiStrings.Get("discovery.seen_help");
		}
		if (_grid.Columns["Name"] is DataGridViewColumn name) name.HeaderText = UiStrings.Get("discovery.col_name");
		if (_grid.Columns["Category"] is DataGridViewColumn category) category.HeaderText = UiStrings.Get("discovery.col_category");
		if (_grid.Columns["ID"] is DataGridViewColumn id) id.HeaderText = UiStrings.Get("discovery.col_id");
		if (_grid.Columns["Expedition"] is DataGridViewColumn expedition) expedition.HeaderText = UiStrings.Get("discovery.col_expedition");
		if (_grid.Columns["Description"] is DataGridViewColumn description) description.HeaderText = UiStrings.Get("discovery.col_description");
		if (_grid.Columns["Catalogue"] is DataGridViewColumn catalogue)
		{
			catalogue.HeaderText = UiStrings.Get("discovery.col_catalogue");
			catalogue.ToolTipText = UiStrings.Get("discovery.catalogue_marker_help");
		}

		_filterBox.PlaceholderText = UiStrings.Get("discovery.filter_items");
		_catalogueOnly.Text = UiStrings.Get("discovery.catalogue_only");
		_showSeen.Text = UiStrings.Get("discovery.show_seen");
		_addAllButton.Text = UiStrings.Get("discovery.add_all_visible");
		_removeAllButton.Text = UiStrings.Get("discovery.remove_all_visible");
		_exportButton.Text = UiStrings.Get("common.export");
		_importButton.Text = UiStrings.Get("common.import");
		AdjustColumnWidths();
		RefreshCounter();
	}

	private void SetSeenColumnVisible(bool visible)
	{
		if (_syncingShowSeen) return;
		SetShowSeenForSession(visible);
	}

	private static void SetShowSeenForSession(bool visible)
	{
		_showSeenForSession = visible;
		for (int i = OpenPanels.Count - 1; i >= 0; i--)
		{
			if (OpenPanels[i].TryGetTarget(out var panel))
				panel.ApplySeenColumnVisible(visible);
			else
				OpenPanels.RemoveAt(i);
		}
	}

	private void ApplySeenColumnVisible(bool visible)
	{
		_syncingShowSeen = true;
		try
		{
			if (_showSeen.Checked != visible)
				_showSeen.Checked = visible;
			if (_grid.Columns["Seen"] is DataGridViewColumn seen)
				seen.Visible = visible;
			AdjustColumnWidths();
		}
		finally
		{
			_syncingShowSeen = false;
		}
	}

	/// <summary>Releases the rows, icons and catalog references held by the panel.</summary>
	internal void PurgeData()
	{
		_catalog = null;
		_refreshing = true;
		try { _grid.Rows.Clear(); }
		finally { _refreshing = false; }
		foreach (var image in _iconCache.Values) image.Dispose();
		_iconCache.Clear();
		_allCounterLabel.Text = string.Empty;
		_counterLabel.Text = string.Empty;
	}

	private void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
	{
		if (_refreshing || _catalog == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;
		if (_grid.Rows[e.RowIndex].Tag is not CatalogueItemEntry entry) return;

		string column = _grid.Columns[e.ColumnIndex].Name;
		if (column == "Known")
		{
			bool target = !_catalog.IsKnown(entry);
			if (target && !entry.CanAddKnown) return;
			if (!_catalog.SetKnown(entry, target)) return;
		}
		else if (column == "Seen")
		{
			if (!_catalog.SetSeen(entry, !_catalog.IsSeen(entry))) return;
		}
		else
		{
			return;
		}

		RefreshStates();
		DataModified?.Invoke(this, EventArgs.Empty);
	}

	private void Grid_CellToolTipTextNeeded(object? sender, DataGridViewCellToolTipTextNeededEventArgs e)
	{
		if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
		if (_grid.Columns[e.ColumnIndex].Name == "Catalogue")
			e.ToolTipText = UiStrings.Get("discovery.catalogue_marker_help");
		else if (_grid.Columns[e.ColumnIndex].Name == "Known")
			e.ToolTipText = UiStrings.Get("discovery.known_help");
		else if (_grid.Columns[e.ColumnIndex].Name == "Seen")
			e.ToolTipText = UiStrings.Get("discovery.seen_help");
	}

	private CheckState GetCheckState(CatalogueItemEntry entry)
	{
		if (_catalog == null) return CheckState.Unchecked;
		if (_catalog.IsKnown(entry)) return CheckState.Checked;
		return _catalog.IsPartiallyKnown(entry) ? CheckState.Indeterminate : CheckState.Unchecked;
	}

	private void AddAllVisible()
	{
		if (_catalog == null) return;
		var entries = VisibleEntries()
			.Where(entry => entry.CanAddKnown && !_catalog.IsKnown(entry))
			.ToList();
		if (entries.Count == 0)
		{
			ShowInfo("discovery.add_all_none");
			return;
		}

		string messageKey = _catalogueOnly.Checked
			? "discovery.add_all_catalogue_confirm"
			: "discovery.add_all_visible_confirm";
		if (!Confirm(messageKey, entries.Count)) return;

		if (_catalog.SetKnown(entries, true) == 0) return;
		RefreshStates();
		DataModified?.Invoke(this, EventArgs.Empty);
	}

	private void RemoveAllVisible()
	{
		if (_catalog == null) return;
		var entries = VisibleEntries()
			.Where(entry => _catalog.IsKnown(entry) || _catalog.IsPartiallyKnown(entry))
			.ToList();
		if (entries.Count == 0)
		{
			ShowInfo("discovery.remove_all_none");
			return;
		}

		if (!Confirm("discovery.remove_all_visible_confirm", entries.Count)) return;
		if (_catalog.SetKnown(entries, false) == 0) return;
		RefreshStates();
		DataModified?.Invoke(this, EventArgs.Empty);
	}

	private IEnumerable<CatalogueItemEntry> VisibleEntries()
	{
		foreach (DataGridViewRow row in _grid.Rows)
		{
			if (row.Visible && row.Tag is CatalogueItemEntry entry)
				yield return entry;
		}
	}

	private bool Confirm(string messageKey, int count) =>
		MessageBox.Show(this,
			UiStrings.Format(messageKey, count),
			UiStrings.Get("discovery.add_all_title"),
			MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

	private void ShowInfo(string key) =>
		MessageBox.Show(this, UiStrings.Get(key), UiStrings.Get("discovery.add_all_title"),
			MessageBoxButtons.OK, MessageBoxIcon.Information);

	private void ApplyFilter()
	{
		if (_catalog == null) return;

		string filter = _filterBox.Text.Trim();
		bool catalogueOnly = _catalogueOnly.Checked;
		bool catalogueAlias = IsAlias(filter, "catalogue", "counted", "yes");
		bool nonCatalogueAlias = IsAlias(filter, "not catalogue", "not counted", "no");
		bool yAlias = filter.Equals("Y", StringComparison.OrdinalIgnoreCase);
		bool nAlias = filter.Equals("N", StringComparison.OrdinalIgnoreCase);

		foreach (DataGridViewRow row in _grid.Rows)
		{
			if (row.Tag is not CatalogueItemEntry entry) continue;
			if (catalogueOnly && !entry.IsCatalogue)
			{
				row.Visible = false;
				continue;
			}

			if (filter.Length == 0)
			{
				row.Visible = true;
				continue;
			}

			bool matches = catalogueAlias || yAlias
				? entry.IsCatalogue
				: nonCatalogueAlias || nAlias
					? !entry.IsCatalogue
					: entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
						|| entry.Category.Contains(filter, StringComparison.OrdinalIgnoreCase)
						|| entry.Id.Contains(filter, StringComparison.OrdinalIgnoreCase)
						|| entry.Description.Contains(filter, StringComparison.OrdinalIgnoreCase)
						|| (entry.ExpeditionNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)
							.Contains(filter, StringComparison.OrdinalIgnoreCase);
			row.Visible = matches;
		}
	}

	private static bool IsAlias(string text, params string[] aliases) =>
		aliases.Any(alias => text.Equals(alias, StringComparison.OrdinalIgnoreCase));

	private void RefreshCounter()
	{
		if (_catalog == null)
		{
			_allCounterLabel.Text = string.Empty;
			_counterLabel.Text = string.Empty;
			return;
		}

		int allKnown = _catalog.GetKnownCount();
		int allTotal = _catalog.Entries.Count;
		int allPercent = allTotal <= 0 ? 100 : (int)Math.Round(allKnown * 100.0 / allTotal, MidpointRounding.AwayFromZero);
		_allCounterLabel.Text = UiStrings.Format("discovery.all_counter", allKnown, allTotal, allPercent);

		var (have, total) = _catalog.GetCatalogueCompletion();
		int percent = total <= 0 ? 100 : (int)Math.Round(have * 100.0 / total, MidpointRounding.AwayFromZero);
		_counterLabel.Text = UiStrings.Format("discovery.catalogue_counter", have, total, percent);
	}

	private void AdjustColumnWidths()
	{
		if (_catalog == null) return;

		SetContentWidth("Name", entry => entry.Name);
		SetContentWidth("ID", entry => entry.Id);
		SetContentWidth("Category", entry => entry.Category);
		SetContentWidth("Expedition", entry => entry.ExpeditionNumber?.ToString(CultureInfo.InvariantCulture) ?? "-");
		SetContentWidth("Catalogue", entry => entry.IsCatalogue ? "Y" : "N");

		if (_grid.Columns["Known"] is DataGridViewColumn known)
		{
			known.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
			known.Width = Math.Max(MeasureWidth(known.HeaderText, _grid.ColumnHeadersDefaultCellStyle.Font), 28) + 24;
		}

		if (_grid.Columns["Seen"] is DataGridViewColumn seen)
		{
			seen.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
			seen.Width = Math.Max(MeasureWidth(seen.HeaderText, _grid.ColumnHeadersDefaultCellStyle.Font), 28) + 24;
		}

		if (_grid.Columns["Description"] is DataGridViewColumn description)
		{
			description.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
			description.FillWeight = 100;
		}
	}

	private void SetContentWidth(string columnName, Func<CatalogueItemEntry, string> value)
	{
		if (_grid.Columns[columnName] is not DataGridViewColumn column) return;

		int width = MeasureWidth(column.HeaderText, _grid.ColumnHeadersDefaultCellStyle.Font);
		foreach (var entry in _catalog!.Entries)
			width = Math.Max(width, MeasureWidth(value(entry)));

		column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
		column.Width = width + 12;
	}

	private int MeasureWidth(string text, Font? font = null) =>
		TextRenderer.MeasureText(
			text ?? string.Empty,
			font ?? _grid.Font,
			new Size(int.MaxValue, int.MaxValue),
			TextFormatFlags.NoPadding).Width;

	private Image? GetIcon(string itemId)
	{
		if (_iconManager == null) return null;
		if (_iconCache.TryGetValue(itemId, out var cached)) return cached;

		var icon = _iconManager.GetIconForItem(itemId, _database);
		if (icon == null) return null;

		try
		{
			var scaled = new Bitmap(24, 24);
			using (var graphics = Graphics.FromImage(scaled))
			{
				graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				graphics.DrawImage(icon, 0, 0, 24, 24);
			}
			_iconCache[itemId] = scaled;
			return scaled;
		}
		catch
		{
			return null;
		}
	}

	private static DataGridViewTextBoxColumn CreateTextColumn(
		string name,
		string header,
		float weight,
		DataGridViewAutoSizeColumnMode autoSize) => new()
	{
		Name = name,
		HeaderText = header,
		ReadOnly = true,
		FillWeight = weight,
		AutoSizeMode = autoSize,
	};

	private void ExportKnown()
	{
		if (_catalog == null) return;

		var config = ExportConfig.Instance;
		var vars = new Dictionary<string, string> { ["name"] = _catalog.JsonPath[^1] };
		using var dialog = new SaveFileDialog
		{
			Filter = ExportConfig.BuildDialogFilter(config.DiscoveryExt, "Discovery files"),
			DefaultExt = config.DiscoveryExt.TrimStart('.'),
			FileName = ExportConfig.BuildFileName(config.DiscoveryTemplate, config.DiscoveryExt, vars),
		};
		if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

		try
		{
			var root = new JsonObject();
			var array = new JsonArray();
			foreach (var entry in _catalog.Entries)
				if (_catalog.IsKnown(entry)) array.Add(entry.Id);
			root.Set(_catalog.JsonPath[^1], array);
			root.ExportToFile(dialog.FileName);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, UiStrings.Format("discovery.export_failed", ex.Message),
				UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void ImportKnown()
	{
		if (_catalog == null) return;

		var config = ExportConfig.Instance;
		using var dialog = new OpenFileDialog
		{
			Filter = ExportConfig.BuildOpenFilter(config.DiscoveryExt, "Discovery files"),
		};
		if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

		try
		{
			var imported = JsonObject.ImportFromFile(dialog.FileName);
			JsonArray? array = null;
			foreach (string name in imported.Names())
			{
				try { array = imported.GetArray(name); break; }
				catch { }
			}
			if (array == null || array.Length == 0)
			{
				MessageBox.Show(this, UiStrings.Get("discovery.import_no_items"),
					UiStrings.Get("discovery.import_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			int added = 0;
			for (int i = 0; i < array.Length; i++)
			{
				string id = CatalogueCompletionLogic.NormalizeId(array.GetString(i));
				if (id.Length == 0) continue;
				var entry = _catalog.Entries.FirstOrDefault(e =>
					e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
				if (entry == null) continue;
				if (_catalog.SetKnown(entry, true)) added++;
			}

			RefreshStates();
			RaiseDataModified();
			MessageBox.Show(this, UiStrings.Format("discovery.import_success_items", added),
				UiStrings.Get("discovery.import_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, UiStrings.Format("discovery.import_failed", ex.Message),
				UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void RaiseDataModified() => DataModified?.Invoke(this, EventArgs.Empty);
}
