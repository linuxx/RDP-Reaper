using System;
using System.Security.Cryptography;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace RdpReaper.Core.Security;

[SupportedOSPlatform("windows")]
public static class ApiSecretStore
{
    private const string RegistryKeyPath = @"SOFTWARE\RdpReaper";
    private const string RegistryValueName = "ApiSecret";

    public static string GetOrCreateSecret()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("ApiSecretStore requires Windows.");
        }

        var existing = ReadSecret();
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var secretBytes = RandomNumberGenerator.GetBytes(32);
        var protectedBytes = ProtectedData.Protect(secretBytes, null, DataProtectionScope.LocalMachine);
        var encoded = Convert.ToBase64String(protectedBytes);

        using var key = Registry.LocalMachine.CreateSubKey(RegistryKeyPath, writable: true);
        key?.SetValue(RegistryValueName, encoded, RegistryValueKind.String);

        return Convert.ToBase64String(secretBytes);
    }

    public static string? ReadSecret()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        using var key = Registry.LocalMachine.OpenSubKey(RegistryKeyPath, writable: false);
        var encoded = key?.GetValue(RegistryValueName) as string;
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return null;
        }

        var protectedBytes = Convert.FromBase64String(encoded);
        var secretBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.LocalMachine);
        return Convert.ToBase64String(secretBytes);
    }
}
