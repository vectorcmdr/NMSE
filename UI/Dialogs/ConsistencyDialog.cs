using System.Drawing;
using System.Windows.Forms;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.Dialogs;

/// <summary>
/// Modal dialog that displays reward consistency issues in a scrollable grid.
/// Each row shows the item icon, ID, name, and issue description, with per-row
/// action buttons to either add the item to the missing array or remove it from
/// the current array. Bulk "Fix All" buttons at the top apply the same action
/// to every listed issue.
/// </summary>
internal sealed class ConsistencyDialog : Form
{
    private readonly DataGridView _grid;
    private readonly List<AccountLogic.ConsistencyIssue> _issues;
    private readonly JsonObject _saveData;
    private readonly GameItemDatabase? _database;
    private readonly IconManager? _iconManager;

    // Column indices for the action button columns.
    private const int ColAddBtnIndex = 5;
    private const int ColRemoveBtnIndex = 6;

    internal ConsistencyDialog(
        List<AccountLogic.ConsistencyIssue> issues,
        JsonObject saveData,
        GameItemDatabase? database,
        IconManager? iconManager)
    {
        _issues = issues;
        _saveData = saveData;
        _database = database;
        _iconManager = iconManager;

        Text = UiStrings.Get("account.consistency_check");
        Size = new Size(920, 520);
        MinimumSize = new Size(720, 340);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;

        // Bulk action panel at the top.
        var bulkPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(6, 4, 6, 4),
        };

        var addAllBtn = new Button
        {
            Text = UiStrings.Get("account.consistency_add_all"),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        addAllBtn.Click += OnAddAll;

        var removeAllBtn = new Button
        {
            Text = UiStrings.Get("account.consistency_remove_all"),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        removeAllBtn.Click += OnRemoveAll;

        var countLabel = new Label
        {
            Text = UiStrings.Format("account.consistency_issues_found", issues.Count),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 6, 0, 0),
        };

        bulkPanel.Controls.Add(addAllBtn);
        bulkPanel.Controls.Add(removeAllBtn);
        bulkPanel.Controls.Add(countLabel);

        // Warning label.
        var warningLabel = new Label
        {
            Text = UiStrings.Get("account.consistency_warning"),
            AutoSize = false,
            Dock = DockStyle.Top,
            ForeColor = Color.DarkOrange,
            Padding = new Padding(8, 4, 8, 4),
            Height = 52,
        };

        // Grid.
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
        };
        _grid.RowTemplate.Height = 30;

        // Columns: Icon, Name, ID, Issue, "Add to..." button, "Remove from..." button.
        _grid.Columns.Add(new DataGridViewImageColumn
        {
            Name = "Icon",
            HeaderText = "",
            Width = 34,
            FillWeight = 5,
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Name",
            HeaderText = UiStrings.Get("account.col_name"),
            FillWeight = 18,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ID",
            HeaderText = UiStrings.Get("account.col_reward_id"),
            FillWeight = 12,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Issue",
            HeaderText = UiStrings.Get("account.consistency_col_issue"),
            FillWeight = 24,
        });

        // Invisible column to track the issue index.
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "IssueIndex",
            Visible = false,
        });

        _grid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "AddBtn",
            HeaderText = "",
            FillWeight = 22,
            UseColumnTextForButtonValue = false,
        });
        _grid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "RemoveBtn",
            HeaderText = "",
            FillWeight = 22,
            UseColumnTextForButtonValue = false,
        });

        PopulateGrid();

        _grid.CellContentClick += OnCellContentClick;

        // Close button at the bottom.
        var closeBtn = new Button
        {
            Text = UiStrings.Get("common.close"),
            DialogResult = DialogResult.OK,
            Width = 90,
            Anchor = AnchorStyles.Right,
        };
        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(6, 4, 6, 4),
        };
        bottomPanel.Controls.Add(closeBtn);

        Controls.Add(_grid);
        Controls.Add(warningLabel);
        Controls.Add(bulkPanel);
        Controls.Add(bottomPanel);
        AcceptButton = closeBtn;
    }

    private void PopulateGrid()
    {
        _grid.Rows.Clear();
        for (int i = 0; i < _issues.Count; i++)
        {
            var issue = _issues[i];
            var icon = ResolveIcon(issue.Id);
            string addLabel = UiStrings.Format("account.consistency_add_to", HumanizeArrayName(issue.MissingArray));
            // Arg passsed but not currently used. 
            string removeLabel = UiStrings.Format("account.consistency_remove_from", HumanizeArrayName(issue.CurrentArray));

            int rowIndex = _grid.Rows.Add(
                icon ?? (object)new Bitmap(1, 1),
                issue.Name,
                issue.Id,
                issue.Description,
                i,
                addLabel,
                removeLabel);

            // Tag the row with the issue index so we can find it after removes.
            _grid.Rows[rowIndex].Tag = i;
        }
    }

    /// <summary>
    /// Converts a PascalCase JSON array name (e.g. "RedeemedSeasonRewards") into a
    /// human-readable form with spaces (e.g. "Redeemed Season Rewards").
    /// </summary>
    private static string HumanizeArrayName(string arrayName)
    {
        if (string.IsNullOrEmpty(arrayName)) return arrayName;

        var sb = new System.Text.StringBuilder(arrayName.Length + 4);
        sb.Append(arrayName[0]);
        for (int i = 1; i < arrayName.Length; i++)
        {
            if (char.IsUpper(arrayName[i]) && !char.IsUpper(arrayName[i - 1]))
                sb.Append(' ');
            sb.Append(arrayName[i]);
        }
        return sb.ToString();
    }

    private Image? ResolveIcon(string id)
    {
        if (_iconManager == null || _database == null) return null;

        string lookupId = CatalogueLogic.StripCaretPrefix(id);
        var icon = _iconManager.GetIconForItem(lookupId, _database);
        if (icon == null) return null;

        // Scale to 24x24 for the grid.
        var scaled = new Bitmap(24, 24);
        using (var g = Graphics.FromImage(scaled))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(icon, 0, 0, 24, 24);
        }
        return scaled;
    }

    private void OnCellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;

        var row = _grid.Rows[e.RowIndex];
        if (row.Cells["IssueIndex"].Value is not int issueIndex) return;
        if (issueIndex < 0 || issueIndex >= _issues.Count) return;
        var issue = _issues[issueIndex];

        if (e.ColumnIndex == ColAddBtnIndex)
        {
            AccountLogic.ResolveConsistencyIssue(_saveData, issue, addToMissing: true);
            _grid.Rows.RemoveAt(e.RowIndex);
        }
        else if (e.ColumnIndex == ColRemoveBtnIndex)
        {
            AccountLogic.ResolveConsistencyIssue(_saveData, issue, addToMissing: false);
            _grid.Rows.RemoveAt(e.RowIndex);
        }
    }

    private void OnAddAll(object? sender, EventArgs e)
    {
        foreach (var issue in _issues)
            AccountLogic.ResolveConsistencyIssue(_saveData, issue, addToMissing: true);

        _grid.Rows.Clear();

        MessageBox.Show(this, 
            UiStrings.Get("account.consistency_all_added"),
            UiStrings.Get("account.consistency_check"),
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnRemoveAll(object? sender, EventArgs e)
    {
        foreach (var issue in _issues)
            AccountLogic.ResolveConsistencyIssue(_saveData, issue, addToMissing: false);

        _grid.Rows.Clear();

        MessageBox.Show(this, 
            UiStrings.Get("account.consistency_all_removed"),
            UiStrings.Get("account.consistency_check"),
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
