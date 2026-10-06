using System.Drawing.Imaging;

namespace BHelper.App.Utils;

internal static class ResourceImageHelper
{
    public static Image? Load(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", fileName);
        if (!File.Exists(path))
            return null;

        using var stream = File.OpenRead(path);
        return Image.FromStream(stream);
    }

    // Loads an icon and repaints every visible pixel with one color, keeping the alpha channel.
    // Used for single-color icons that were drawn in black and would vanish on the dark theme.
    public static Image? LoadTinted(string fileName, Color color)
    {
        using var source = Load(fileName);
        if (source is null)
            return null;

        var tinted = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(tinted);
        using var attributes = new ImageAttributes();

        var matrix = new ColorMatrix
        {
            Matrix00 = 0f,
            Matrix11 = 0f,
            Matrix22 = 0f,
            Matrix33 = 1f,
            Matrix40 = color.R / 255f,
            Matrix41 = color.G / 255f,
            Matrix42 = color.B / 255f
        };
        attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

        graphics.DrawImage(
            source,
            new Rectangle(0, 0, tinted.Width, tinted.Height),
            0, 0, source.Width, source.Height,
            GraphicsUnit.Pixel,
            attributes);

        return tinted;
    }
}
