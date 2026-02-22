using System;
using System.IO;

namespace RdpReaper.Core.Infrastructure;

public static class AppPaths
{
    public const string ProductName = "RdpReaper";

    public static string ProgramDataRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ProductName);

    public static string ConfigPath => Path.Combine(ProgramDataRoot, "config.json");

    public static string DatabasePath => Path.Combine(ProgramDataRoot, "rdp-reaper.db");
}
