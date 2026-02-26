namespace Oscilloscope.Client.Infrastructure.Utils;

public static class ProjectRootLocator
{
    /// Пошук по папкам "вгору", поки не знайде: "Oscilloscope.Server" and "Oscilloscope.Emulator".
    public static string LocateRootOrThrow()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        for (int i = 0; i < 10 && dir is not null; i++)
        {
            var hasServer = Directory.Exists(Path.Combine(dir.FullName, "Oscilloscope.Server"));
            var hasEmu = Directory.Exists(Path.Combine(dir.FullName, "Oscilloscope.Emulator"));

            if (hasServer && hasEmu)
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Cannot locate solution root. Please set ExternalApps:ServerApiProjectPath and EmulatorProjectPath in appsettings.json.");
    }
}
