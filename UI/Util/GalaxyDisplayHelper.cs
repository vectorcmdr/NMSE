using System.Drawing;
using System.Windows.Forms;
using NMSE.Data;

namespace NMSE.UI.Util;

public static class GalaxyDisplayHelper
{
    /// <summary>
    /// Creates a galaxy core dot image for the given reality index.
    /// </summary>
    public static Bitmap CreateGalaxyCoreDotImage(int realityIndex, int size = 12)
    {
        var color = GalaxyDatabase.GetGalaxyCoreColorValue(realityIndex);
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 0, 0, size - 1, size - 1);
        using var pen = new Pen(Color.Black, 1);
        g.DrawEllipse(pen, 0, 0, size - 1, size - 1);
        return bmp;
    }

    /// <summary>
    /// Configures a label to show the galaxy core dot image.
    /// </summary>
    public static void ConfigureGalaxyDotLabel(Label label, int realityIndex, int size = 12)
    {
        var newImage = CreateGalaxyCoreDotImage(realityIndex, size);
        label.Image?.Dispose();
        label.Image = newImage;
        label.ImageAlign = ContentAlignment.MiddleCenter;
        label.Text = string.Empty;
        label.AutoSize = false;
        label.Size = label.Image?.Size ?? new Size(size, size);
    }

    /// <summary>
    /// Paints galaxy name text and the core dot inside a DataGridView cell.
    /// </summary>
    public static void PaintGalaxyCell(DataGridViewCellPaintingEventArgs e, string text, int realityIndex)
    {
        if (e == null) throw new ArgumentNullException(nameof(e));

        e.PaintBackground(e.ClipBounds, e.State.HasFlag(DataGridViewElementStates.Selected));
        if (e.Graphics == null || string.IsNullOrEmpty(text))
        {
            e.Handled = true;
            return;
        }

        var font = e.CellStyle?.Font ?? SystemFonts.DefaultFont;
        var textColor = e.State.HasFlag(DataGridViewElementStates.Selected)
            ? (e.CellStyle?.SelectionForeColor ?? SystemColors.HighlightText)
            : (e.CellStyle?.ForeColor ?? SystemColors.ControlText);

        using var textBrush = new SolidBrush(textColor);
        var textSize = e.Graphics.MeasureString(text + " ", font);
        int textX = e.CellBounds.X + 2;
        int textY = e.CellBounds.Y + (int)((e.CellBounds.Height - textSize.Height) / 2);
        e.Graphics.DrawString(text, font, textBrush, textX, textY);

        int dotSize = Math.Min(10, e.CellBounds.Height - 4);
        int dotX = textX + (int)Math.Ceiling(textSize.Width) + 4;
        int dotY = e.CellBounds.Y + (e.CellBounds.Height - dotSize) / 2;
        using var dotImage = CreateGalaxyCoreDotImage(realityIndex, dotSize);
        e.Graphics.DrawImage(dotImage, dotX, dotY, dotSize, dotSize);

        e.Handled = true;
    }
}
