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
}
