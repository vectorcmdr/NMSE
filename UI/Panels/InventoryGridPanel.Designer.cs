#nullable enable
using NMSE.Config;
using NMSE.UI.Controls;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

partial class InventoryGridPanel
{
    private System.ComponentModel.IContainer? components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sharedToolTip?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Component Designer generated code
    private void InitializeComponent()
    {
        this.SuspendLayout();
        // 
        // InventoryGridPanel
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.DoubleBuffered = true;
        this.ResumeLayout(false);
    }
    #endregion

    private void SetupLayout()
    {
        SuspendLayout();

        var desiredRightPanelWidth = 300;
        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            FixedPanel = FixedPanel.Panel2,
            SplitterDistance = 280
        };

        splitContainer.SizeChanged += (s, e) =>
        {
            if (splitContainer.Width <= desiredRightPanelWidth)
                return;

            splitContainer.Panel2MinSize = desiredRightPanelWidth;
            splitContainer.SplitterDistance = splitContainer.Width - desiredRightPanelWidth;
        };

        // Left: info row above toolbar/grid for long inventory guidance text
        _infoPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 26,
            Padding = new Padding(4, 4, 4, 0),
            Visible = false
        };

        // Left: resize controls above the grid
        _toolbarPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            MinimumSize = new Size(0, 36),
            MaximumSize = new Size(int.MaxValue, 36),
            Padding = new Padding(4, 4, 4, 0),
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = false,
            WrapContents = false
        };
        _resizeWidthLabel = new Label { Text = "Width:", AutoSize = true, Dock = DockStyle.Left, Padding = new Padding(0, 4, 2, 0) };
        _resizeWidth = new InvariantNumericTextBox { Minimum = 1, Maximum = 20, NumericValue = 10, Width = 50, Dock = DockStyle.Left };
        _resizeHeightLabel = new Label { Text = "Height:", AutoSize = true, Dock = DockStyle.Left, Padding = new Padding(8, 4, 2, 0) };
        _resizeHeight = new InvariantNumericTextBox { Minimum = 1, Maximum = 20, NumericValue = 6, Width = 50, Dock = DockStyle.Left };
        _resizeButton = new Button { Text = "Resize", AutoSize = true, Size = new Size(75, 28), MinimumSize = new Size(75, 28), Margin = new Padding(8, 0, 0, 0) };
        _resizeButton.Click += OnResizeInventory;
        _sortModeLabel = new Label { Text = "Sort:", AutoSize = true, Dock = DockStyle.Left, Padding = new Padding(12, 4, 2, 0), Margin = new Padding(0, 2, 0, 0) };
        _sortModeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80, Margin = new Padding(0, 2, 0, 0) };
        _sortModeCombo.SelectedIndexChanged += OnSortModeChanged;
        _autoStackToolStrip = new ToolStrip
        {
            AutoSize = true,
            MinimumSize = new Size(60, 28),
            GripStyle = ToolStripGripStyle.Hidden,
            CanOverflow = false,
            Padding = Padding.Empty,
            Margin = new Padding(8, 0, 0, 0),
            RenderMode = ToolStripRenderMode.System
        };
        _autoStackDropDownButton = new ToolStripDropDownButton("Auto-Stack");
        _autoStackToChestsButtonMenuItem = new ToolStripMenuItem("To Chests", null, OnAutoStackToStorage);
        _autoStackToStarshipButtonMenuItem = new ToolStripMenuItem("To Starship", null, OnAutoStackToStarship);
        _autoStackToFreighterButtonMenuItem = new ToolStripMenuItem("To Freighter", null, OnAutoStackToFreighter);
        _autoStackDropDownButton.DropDownItems.Add(_autoStackToChestsButtonMenuItem);
        _autoStackDropDownButton.DropDownItems.Add(_autoStackToStarshipButtonMenuItem);
        _autoStackDropDownButton.DropDownItems.Add(_autoStackToFreighterButtonMenuItem);
        _autoStackToolStrip.Items.Add(_autoStackDropDownButton);
        _exportButton = new Button { Text = "Export", AutoSize = true, Size = new Size(75, 28), MinimumSize = new Size(75, 28), Margin = new Padding(8, 0, 0, 0) };
        _exportButton.Click += OnExportInventory;
        _importButton = new Button { Text = "Import", AutoSize = true, Size = new Size(75, 28), MinimumSize = new Size(75, 28), Margin = new Padding(4, 0, 0, 0) };
        _importButton.Click += OnImportInventory;
        _toolbarPanel.Controls.Add(_resizeWidthLabel);
        _toolbarPanel.Controls.Add(_resizeWidth);
        _toolbarPanel.Controls.Add(_resizeHeightLabel);
        _toolbarPanel.Controls.Add(_resizeHeight);
        _toolbarPanel.Controls.Add(_resizeButton);
        _toolbarPanel.Controls.Add(_sortModeLabel);
        _toolbarPanel.Controls.Add(_sortModeCombo);
        _toolbarPanel.Controls.Add(_autoStackToolStrip);
        _toolbarPanel.Controls.Add(_exportButton);
        _toolbarPanel.Controls.Add(_importButton);
        // Note: Dock=Left controls are added in reverse visual order

        // Left: grid of slot cells
        _gridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        splitContainer.Panel1.Controls.Add(_gridContainer);
        splitContainer.Panel1.Controls.Add(_toolbarPanel);
        splitContainer.Panel1.Controls.Add(_infoPanel);

        // Right: detail/editor panel
        _detailPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            AutoScroll = true,
            BackColor = SystemColors.Control
        };

        var detailLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            Padding = new Padding(4)
        };
        detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;

        // === SLOT DETAILS SECTION ===
        _slotDetailHeader = new Label
        {
            Text = "Slot Details",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 4)
        };
        FontManager.ApplyHeadingFont(_slotDetailHeader, 11);
        detailLayout.Controls.Add(_slotDetailHeader, 0, row);
        detailLayout.SetColumnSpan(_slotDetailHeader, 2);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Icon with slot position, info tooltip hint, and class icon beside it
        _detailIcon = new PictureBox
        {
            Size = new Size(72, 72),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(40, 40, 40),
            BorderStyle = BorderStyle.FixedSingle
        };
        _detailSlotPosition = new Label { Text = "", AutoSize = true, Margin = new Padding(0, 1, 0, 0) };
        _detailPositionLabel = CreateLabel("Slot:");
        _detailPositionLabel.Padding = new Padding(0, 0, 2, 0);
        _detailPositionLabel.Margin = new Padding(0, 1, 0, 0);
        _detailInfoButton = new PictureBox
        {
            Size = new Size(16, 16),
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = CreateInfoIconBitmap(16, Color.SteelBlue),
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 4, 4, 0)
        };
        _detailInfoHintLabel = new Label
        {
            Text = "Hover for info.",
            AutoSize = true,
            ForeColor = Color.Gray,
            Margin = new Padding(0, 4, 0, 0)
        };
        _detailDescription = new Label { Text = "", Visible = false };
        _detailClassIcon = new PictureBox
        {
            Size = new Size(24, 24),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 0, 0)
        };
        var detailSlotRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        detailSlotRow.Controls.Add(_detailPositionLabel);
        detailSlotRow.Controls.Add(_detailSlotPosition);
        var detailInfoRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 3, 0, 0),
            Padding = Padding.Empty
        };
        detailInfoRow.Controls.Add(_detailInfoButton);
        detailInfoRow.Controls.Add(_detailInfoHintLabel);
        var iconSidePanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(4, 0, 0, 0),
            Padding = Padding.Empty
        };
        iconSidePanel.Controls.Add(detailSlotRow);
        iconSidePanel.Controls.Add(detailInfoRow);
        iconSidePanel.Controls.Add(_detailClassIcon);
        var iconRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        iconRow.Controls.Add(_detailIcon);
        iconRow.Controls.Add(iconSidePanel);
        detailLayout.Controls.Add(iconRow, 0, row);
        detailLayout.SetColumnSpan(iconRow, 2);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Item Name
        _detailItemName = new Label
        {
            Text = "(no slot selected)",
            AutoSize = true,
            ForeColor = Color.DarkBlue,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 0, 0)
        };
        FontManager.ApplyHeadingFont(_detailItemName, 11);
        _detailNameLabel = CreateLabel("Name:");
        detailLayout.Controls.Add(_detailNameLabel, 0, row);
        detailLayout.Controls.Add(_detailItemName, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Item Type
        _detailItemType = new Label { Text = "", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) };
        _detailTypeLabel = CreateLabel("Type:");
        detailLayout.Controls.Add(_detailTypeLabel, 0, row);
        detailLayout.Controls.Add(_detailItemType, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Item Category
        _detailItemCategory = new Label { Text = "", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) };
        _detailCategoryLabel = CreateLabel("Category:");
        detailLayout.Controls.Add(_detailCategoryLabel, 0, row);
        detailLayout.Controls.Add(_detailItemCategory, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Item ID (editable) with inline seed field for procedural items
        _detailItemId = new TextBox { Width = 120, Anchor = AnchorStyles.Left, Margin = new Padding(0, 4, 0, 0) };
        _detailSeedLabel = CreateLabel("#");
        _detailSeedLabel.AutoSize = true;
        _detailSeedLabel.Font = new Font(_detailSeedLabel.Font.FontFamily, _detailSeedLabel.Font.Size + 2, _detailSeedLabel.Font.Style);
        _detailSeedLabel.Margin = new Padding(0, 4, 0, 0);
        _detailSeedLabel.Visible = false;
        _detailSeedField = new TextBox { Width = 56, MaxLength = 5, Visible = false, Margin = new Padding(0, 4, 0, 0) };
        _detailItemIdLabel = CreateLabel("Item ID:");
        _detailItemIdLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _detailItemIdLabel.Margin = new Padding(0, 4, 0, 0);

        _detailIdSeedPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _detailIdSeedPanel.Controls.Add(_detailItemId);

        var _detailSeedFieldPanel = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _detailSeedFieldPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _detailSeedFieldPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _detailSeedFieldPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _detailSeedFieldPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _detailSeedFieldPanel.Controls.Add(_detailSeedLabel, 0, 0);
        _detailSeedFieldPanel.Controls.Add(_detailSeedField, 1, 0);

        _detailGenSeedButton = new Button
        {
            Text = "Gen",
            AutoSize = false,
            Size = new Size(44, 24),
            MinimumSize = new Size(44, 24),
            Visible = false,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 4, 0, 0),
            Padding = new Padding(0, 0, 0, 0)
        };
        _detailGenSeedButton.Click += OnGenSeedClick;
        _detailSeedFieldPanel.Controls.Add(_detailGenSeedButton, 1, 1);

        var _detailSeedEntryPanel = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _detailSeedEntryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _detailSeedEntryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _detailSeedEntryPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _detailSeedEntryPanel.Controls.Add(_detailIdSeedPanel, 0, 0);
        _detailSeedEntryPanel.Controls.Add(_detailSeedFieldPanel, 1, 0);

        detailLayout.Controls.Add(_detailItemIdLabel, 0, row);
        detailLayout.Controls.Add(_detailSeedEntryPanel, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Amount
        _detailAmount = new InvariantNumericTextBox { Dock = DockStyle.Fill, Minimum = int.MinValue, Maximum = int.MaxValue };
        _detailAmountLabel = CreateLabel("Amount:");
        detailLayout.Controls.Add(_detailAmountLabel, 0, row);
        detailLayout.Controls.Add(_detailAmount, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Max Amount
        _detailMaxAmount = new InvariantNumericTextBox { Dock = DockStyle.Fill, Minimum = int.MinValue, Maximum = int.MaxValue };
        _detailMaxLabel = CreateLabel("Max:");
        detailLayout.Controls.Add(_detailMaxLabel, 0, row);
        detailLayout.Controls.Add(_detailMaxAmount, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Damage Factor
        _detailDamageFactor = new InvariantNumericTextBox
        {
            Dock = DockStyle.Fill
        };
        _detailDamageLabel = CreateLabel("Damage:");
        detailLayout.Controls.Add(_detailDamageLabel, 0, row);
        detailLayout.Controls.Add(_detailDamageFactor, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Apply button
        _applyButton = new Button
        {
            Text = "Apply Changes",
            Dock = DockStyle.Fill,
            Height = 30,
            Enabled = false
        };
        _applyButton.Click += OnApplyChanges;
        detailLayout.Controls.Add(_applyButton, 0, row);
        detailLayout.SetColumnSpan(_applyButton, 2);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // === ITEM PICKER SECTION ===
        var separator = new Label
        {
            Text = "",
            BorderStyle = BorderStyle.Fixed3D,
            AutoSize = false,
            Height = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 0, 8)
        };
        detailLayout.Controls.Add(separator, 0, row);
        detailLayout.SetColumnSpan(separator, 2);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        _itemPickerHeader = new Label
        {
            Text = "Item Picker",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 4)
        };
        FontManager.ApplyHeadingFont(_itemPickerHeader, 10);
        detailLayout.Controls.Add(_itemPickerHeader, 0, row);
        detailLayout.SetColumnSpan(_itemPickerHeader, 2);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // === PICKER ITEM DETAILS SECTION (above the search/filter controls) ===
        // Icon with info tooltip hint and class icon beside it
        _pickerIcon = new PictureBox
        {
            Size = new Size(72, 72),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(40, 40, 40),
            BorderStyle = BorderStyle.FixedSingle
        };
        _pickerInfoButton = new PictureBox
        {
            Size = new Size(16, 16),
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = CreateInfoIconBitmap(16, Color.SteelBlue),
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 4, 4, 0)
        };
        _pickerInfoHintLabel = new Label
        {
            Text = "Hover for info.",
            AutoSize = true,
            ForeColor = Color.Gray,
            Margin = new Padding(0, 5, 0, 0)
        };
        _pickerDescription = new Label { Text = "", Visible = false };
        _pickerClassIcon = new PictureBox
        {
            Size = new Size(24, 24),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 0, 0)
        };
        var pickerInfoRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        pickerInfoRow.Controls.Add(_pickerInfoButton);
        pickerInfoRow.Controls.Add(_pickerInfoHintLabel);
        var pickerIconSidePanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(4, 0, 0, 0),
            Padding = Padding.Empty
        };
        pickerIconSidePanel.Controls.Add(pickerInfoRow);
        pickerIconSidePanel.Controls.Add(_pickerClassIcon);
        var pickerIconRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        pickerIconRow.Controls.Add(_pickerIcon);
        pickerIconRow.Controls.Add(pickerIconSidePanel);
        detailLayout.Controls.Add(pickerIconRow, 0, row);
        detailLayout.SetColumnSpan(pickerIconRow, 2);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Picker Item Name
        _pickerItemName = new Label
        {
            Text = "(no item selected)",
            AutoSize = true,
            ForeColor = Color.DarkBlue,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 0, 0)
        };
        FontManager.ApplyHeadingFont(_pickerItemName, 11);
        _pickerNameLabel = CreateLabel("Name:");
        detailLayout.Controls.Add(_pickerNameLabel, 0, row);
        detailLayout.Controls.Add(_pickerItemName, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Picker Item ID with seed fields
        _pickerItemId = new TextBox { Width = 120, Anchor = AnchorStyles.Left, Margin = new Padding(0, 4, 0, 0) };
        _pickerSeedLabel = CreateLabel("#");
        _pickerSeedLabel.AutoSize = true;
        _pickerSeedLabel.Font = new Font(_pickerSeedLabel.Font.FontFamily, _pickerSeedLabel.Font.Size + 2, _pickerSeedLabel.Font.Style);
        _pickerSeedLabel.Margin = new Padding(0, 4, 0, 0);
        _pickerSeedLabel.Visible = false;
        _pickerSeedField = new TextBox { Width = 56, MaxLength = 5, Visible = false, Margin = new Padding(0, 4, 0, 0) };
        _pickerItemIdLabel = CreateLabel("Item ID:");
        _pickerItemIdLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _pickerItemIdLabel.Margin = new Padding(0, 4, 0, 0);
        _pickerIdSeedPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _pickerIdSeedPanel.Controls.Add(_pickerItemId);

        var _pickerSeedFieldPanel = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _pickerSeedFieldPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _pickerSeedFieldPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _pickerSeedFieldPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _pickerSeedFieldPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _pickerSeedFieldPanel.Controls.Add(_pickerSeedLabel, 0, 0);
        _pickerSeedFieldPanel.Controls.Add(_pickerSeedField, 1, 0);

        _pickerGenSeedButton = new Button
        {
            Text = "Gen",
            AutoSize = false,
            Size = new Size(44, 24),
            MinimumSize = new Size(44, 24),
            Visible = false,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 4, 0, 0),
            Padding = new Padding(0, 0, 0, 0)
        };
        _pickerGenSeedButton.Click += OnPickerGenSeedClick;
        _pickerSeedFieldPanel.Controls.Add(_pickerGenSeedButton, 1, 1);

        var _pickerSeedEntryPanel = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _pickerSeedEntryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _pickerSeedEntryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _pickerSeedEntryPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _pickerSeedEntryPanel.Controls.Add(_pickerIdSeedPanel, 0, 0);
        _pickerSeedEntryPanel.Controls.Add(_pickerSeedFieldPanel, 1, 0);

        detailLayout.Controls.Add(_pickerItemIdLabel, 0, row);
        detailLayout.Controls.Add(_pickerSeedEntryPanel, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Picker Amount
        _pickerAmount = new InvariantNumericTextBox { Dock = DockStyle.Fill, Minimum = int.MinValue, Maximum = int.MaxValue };
        _pickerAmountLabel = CreateLabel("Amount:");
        detailLayout.Controls.Add(_pickerAmountLabel, 0, row);
        detailLayout.Controls.Add(_pickerAmount, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Picker Max Amount
        _pickerMaxAmount = new InvariantNumericTextBox { Dock = DockStyle.Fill, Minimum = int.MinValue, Maximum = int.MaxValue };
        _pickerMaxLabel = CreateLabel("Max:");
        detailLayout.Controls.Add(_pickerMaxLabel, 0, row);
        detailLayout.Controls.Add(_pickerMaxAmount, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Picker Damage Factor
        _pickerDamageFactor = new InvariantNumericTextBox
        {
            Dock = DockStyle.Fill
        };
        _pickerDamageLabel = CreateLabel("Damage:");
        detailLayout.Controls.Add(_pickerDamageLabel, 0, row);
        detailLayout.Controls.Add(_pickerDamageFactor, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Picker Add/Replace Item button
        _pickerApplyButton = new Button
        {
            Text = "Add Item",
            Dock = DockStyle.Fill,
            Height = 30,
            Enabled = false
        };
        _pickerApplyButton.Click += OnPickerApplyItem;
        detailLayout.Controls.Add(_pickerApplyButton, 0, row);
        detailLayout.SetColumnSpan(_pickerApplyButton, 2);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Picker separator before search/filter controls
        var pickerSeparator2 = new Label
        {
            Text = "",
            BorderStyle = BorderStyle.Fixed3D,
            AutoSize = false,
            Height = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 6, 0, 6)
        };
        detailLayout.Controls.Add(pickerSeparator2, 0, row);
        detailLayout.SetColumnSpan(pickerSeparator2, 2);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Search box
        var searchPanel = new Panel { Dock = DockStyle.Fill, Height = 26 };
        _searchBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search items..." };
        _searchButton = new Button { Text = "Search", Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(60, 0) };
        _searchButton.Click += OnSearch;
        _searchBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) OnSearch(s, e); };
        searchPanel.Controls.Add(_searchBox);
        searchPanel.Controls.Add(_searchButton);
        _searchLabel = CreateLabel("Search:");
        detailLayout.Controls.Add(_searchLabel, 0, row);
        detailLayout.Controls.Add(searchPanel, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Type filter
        _typeFilter = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _typeFilter.SelectedIndexChanged += OnTypeFilterChanged;
        _typeFilterLabel = CreateLabel("Type:");
        detailLayout.Controls.Add(_typeFilterLabel, 0, row);
        detailLayout.Controls.Add(_typeFilter, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Category filter
        _categoryFilter = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _categoryFilter.SelectedIndexChanged += OnCategoryFilterChanged;
        _categoryFilterLabel = CreateLabel("Category:");
        detailLayout.Controls.Add(_categoryFilterLabel, 0, row);
        detailLayout.Controls.Add(_categoryFilter, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Item picker combobox
        _itemPicker = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            MaxDropDownItems = 20
        };
        _itemPicker.SelectedIndexChanged += OnItemPickerChanged;
        _itemFilterLabel = CreateLabel("Item:");
        detailLayout.Controls.Add(_itemFilterLabel, 0, row);
        detailLayout.Controls.Add(_itemPicker, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        _detailPanel.Controls.Add(detailLayout);
        splitContainer.Panel2.Controls.Add(_detailPanel);

        // Context menu
        _cellContextMenu = new ContextMenuStrip();
        _addItemMenuItem = new ToolStripMenuItem("Add Item", null, OnAddItem);
        _removeItemMenuItem = new ToolStripMenuItem("Remove Item", null, OnRemoveItem);
        _enableSlotMenuItem = new ToolStripMenuItem("Enable/Disable Slot", null, OnEnableSlot);
        _enableAllSlotsMenuItem = new ToolStripMenuItem("Enable All Slots", null, OnEnableAllSlots);
        _pinSlotMenuItem = new ToolStripMenuItem("Pin Slot", null, OnTogglePinnedSlot);
        _repairSlotMenuItem = new ToolStripMenuItem("Repair Slot", null, OnRepairSlot);
        _repairAllSlotsMenuItem = new ToolStripMenuItem("Repair All Slots", null, OnRepairAllSlots);
        _superchargeSlotMenuItem = new ToolStripMenuItem("Supercharge Slot", null, OnSuperchargeSlot);
        _superchargeAllSlotsMenuItem = new ToolStripMenuItem("Supercharge All Slots", null, OnSuperchargeAllSlots);
        _fillStackMenuItem = new ToolStripMenuItem("Fill Stack", null, OnFillStack);
        _rechargeAllTechMenuItem = new ToolStripMenuItem("Recharge All Technology", null, OnRechargeAllTech);
        _refillAllStacksMenuItem = new ToolStripMenuItem("Refill All Stacks", null, OnRefillAllStacks);
        _copyItemMenuItem = new ToolStripMenuItem("Copy Item", null, OnCopyItem);
        _pasteItemMenuItem = new ToolStripMenuItem("Paste Item", null, OnPasteItem);
        _sortByNameMenuItem = new ToolStripMenuItem("Sort by Name", null, OnSortByName);
        _sortByCategoryMenuItem = new ToolStripMenuItem("Sort by Category", null, OnSortByCategory);
        _autoStackToStorageMenuItem = new ToolStripMenuItem("Auto-Stack to Chests", null, OnAutoStackToStorage);
        _autoStackToStarshipMenuItem = new ToolStripMenuItem("Auto-Stack to Starship", null, OnAutoStackToStarship);
        _autoStackToFreighterMenuItem = new ToolStripMenuItem("Auto-Stack to Freighter", null, OnAutoStackToFreighter);
        _cellContextMenu.Items.Add(_addItemMenuItem);
        _cellContextMenu.Items.Add(_removeItemMenuItem);
        _cellContextMenu.Items.Add(new ToolStripSeparator());
        _cellContextMenu.Items.Add(_enableSlotMenuItem);
        _cellContextMenu.Items.Add(_enableAllSlotsMenuItem);
        _cellContextMenu.Items.Add(_pinSlotMenuItem);
        _cellContextMenu.Items.Add(new ToolStripSeparator());
        _cellContextMenu.Items.Add(_repairSlotMenuItem);
        _cellContextMenu.Items.Add(_repairAllSlotsMenuItem);
        _cellContextMenu.Items.Add(new ToolStripSeparator());
        _cellContextMenu.Items.Add(_superchargeSlotMenuItem);
        _cellContextMenu.Items.Add(_superchargeAllSlotsMenuItem);
        _cellContextMenu.Items.Add(new ToolStripSeparator());
        _cellContextMenu.Items.Add(_fillStackMenuItem);
        _cellContextMenu.Items.Add(_rechargeAllTechMenuItem);
        _cellContextMenu.Items.Add(_refillAllStacksMenuItem);
        _cellContextMenu.Items.Add(new ToolStripSeparator());
        _cellContextMenu.Items.Add(_copyItemMenuItem);
        _cellContextMenu.Items.Add(_pasteItemMenuItem);
        _cellContextMenu.Items.Add(new ToolStripSeparator());
        _cellContextMenu.Items.Add(_sortByNameMenuItem);
        _cellContextMenu.Items.Add(_sortByCategoryMenuItem);
        _cellContextMenu.Items.Add(_autoStackToStorageMenuItem);
        _cellContextMenu.Items.Add(_autoStackToStarshipMenuItem);
        _cellContextMenu.Items.Add(_autoStackToFreighterMenuItem);
        _cellContextMenu.Opening += OnContextMenuOpening;


        Controls.Add(splitContainer);
        ResumeLayout(false);
        DisableControlsOnInit();
        PerformLayout();
    }

    private const float InfoIconFontSizeRatio = 0.55f;

    /// <summary>
    /// Creates a crisp GDI+ drawn info icon (circle with 'i') at the specified size.
    /// Avoids unicode/font rendering issues in WinForms.
    /// </summary>
    private static Bitmap CreateInfoIconBitmap(int size, Color color)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Transparent);

        // Draw filled circle
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 0, 0, size - 1, size - 1);

        // Draw 'i' letter centered in white
        using var textBrush = new SolidBrush(Color.White);
        using var font = new Font("Segoe UI", size * InfoIconFontSizeRatio, FontStyle.Bold, GraphicsUnit.Pixel);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("i", font, textBrush, new RectangleF(0, 0, size, size), sf);

        return bmp;
    }

    // Grid area
    private Panel _infoPanel = null!;
    private FlowLayoutPanel _toolbarPanel = null!;
    private Panel _gridContainer = null!;

    // Detail/editor panel controls (Slot Details section)
    private Panel _detailPanel = null!;
    private PictureBox _detailIcon = null!;
    private Label _detailItemName = null!;
    private Label _detailSlotPosition = null!;
    private Label _detailItemType = null!;
    private Label _detailItemCategory = null!;
    private TextBox _detailItemId = null!;
    private TextBox _detailSeedField = null!;
    private Label _detailSeedLabel = null!;
    private FlowLayoutPanel _detailIdSeedPanel = null!;
    private Button _detailGenSeedButton = null!;
    private PictureBox _detailInfoButton = null!;
    private Label _detailInfoHintLabel = null!;
    private Label _detailDescription = null!;
    private PictureBox _detailClassIcon = null!;
    private InvariantNumericTextBox _detailAmount = null!;
    private InvariantNumericTextBox _detailMaxAmount = null!;
    private InvariantNumericTextBox _detailDamageFactor = null!;
    private Button _applyButton = null!;
    private Label _detailAmountLabel = null!;
    private Label _detailMaxLabel = null!;
    private Label _detailNameLabel = null!;
    private Label _detailPositionLabel = null!;
    private Label _detailTypeLabel = null!;
    private Label _detailCategoryLabel = null!;
    private Label _detailItemIdLabel = null!;
    private Label _detailDamageLabel = null!;
    private Label _slotDetailHeader = null!;
    private Label _itemPickerHeader = null!;

    // Picker detail controls (Item Picker detail section)
    private PictureBox _pickerIcon = null!;
    private Label _pickerItemName = null!;
    private TextBox _pickerItemId = null!;
    private TextBox _pickerSeedField = null!;
    private Label _pickerSeedLabel = null!;
    private FlowLayoutPanel _pickerIdSeedPanel = null!;
    private Button _pickerGenSeedButton = null!;
    private PictureBox _pickerInfoButton = null!;
    private Label _pickerInfoHintLabel = null!;
    private Label _pickerDescription = null!;
    private PictureBox _pickerClassIcon = null!;
    private InvariantNumericTextBox _pickerAmount = null!;
    private InvariantNumericTextBox _pickerMaxAmount = null!;
    private InvariantNumericTextBox _pickerDamageFactor = null!;
    private Button _pickerApplyButton = null!;
    private Label _pickerAmountLabel = null!;
    private Label _pickerMaxLabel = null!;
    private Label _pickerNameLabel = null!;
    private Label _pickerItemIdLabel = null!;
    private Label _pickerDamageLabel = null!;

    // Item picker controls
    private ComboBox _typeFilter = null!;
    private ComboBox _categoryFilter = null!;
    private ComboBox _itemPicker = null!;
    private TextBox _searchBox = null!;
    private Button _searchButton = null!;
    private Label _searchLabel = null!;
    private Label _typeFilterLabel = null!;
    private Label _categoryFilterLabel = null!;
    private Label _itemFilterLabel = null!;

    // Resize controls
    private InvariantNumericTextBox _resizeWidth = null!;
    private InvariantNumericTextBox _resizeHeight = null!;
    private Label _resizeWidthLabel = null!;
    private Label _resizeHeightLabel = null!;
    private Button _resizeButton = null!;
    private Label _sortModeLabel = null!;
    private ComboBox _sortModeCombo = null!;
    private ToolStrip _autoStackToolStrip = null!;
    private ToolStripDropDownButton _autoStackDropDownButton = null!;
    private ToolStripMenuItem _autoStackToChestsButtonMenuItem = null!;
    private ToolStripMenuItem _autoStackToStarshipButtonMenuItem = null!;
    private ToolStripMenuItem _autoStackToFreighterButtonMenuItem = null!;
    private Button _importButton = null!;
    private Button _exportButton = null!;

    // Context menu
    private ContextMenuStrip _cellContextMenu = null!;
    private ToolStripMenuItem _addItemMenuItem = null!;
    private ToolStripMenuItem _removeItemMenuItem = null!;
    private ToolStripMenuItem _enableSlotMenuItem = null!;
    private ToolStripMenuItem _enableAllSlotsMenuItem = null!;
    private ToolStripMenuItem _pinSlotMenuItem = null!;
    private ToolStripMenuItem _repairSlotMenuItem = null!;
    private ToolStripMenuItem _repairAllSlotsMenuItem = null!;
    private ToolStripMenuItem _superchargeSlotMenuItem = null!;
    private ToolStripMenuItem _superchargeAllSlotsMenuItem = null!;
    private ToolStripMenuItem _fillStackMenuItem = null!;
    private ToolStripMenuItem _rechargeAllTechMenuItem = null!;
    private ToolStripMenuItem _refillAllStacksMenuItem = null!;
    private ToolStripMenuItem _copyItemMenuItem = null!;
    private ToolStripMenuItem _pasteItemMenuItem = null!;
    private ToolStripMenuItem _sortByNameMenuItem = null!;
    private ToolStripMenuItem _sortByCategoryMenuItem = null!;
    private ToolStripMenuItem _autoStackToStorageMenuItem = null!;
    private ToolStripMenuItem _autoStackToStarshipMenuItem = null!;
    private ToolStripMenuItem _autoStackToFreighterMenuItem = null!;
}
