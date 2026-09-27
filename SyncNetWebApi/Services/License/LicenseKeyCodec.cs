using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SyncNetApi.Services.License
{
    /// <summary>
    /// Codec for the legacy license key format, ported from the Blazor app's
    /// Common/LicenseManager.cs. A license key is:
    ///
    ///   hex( AES-256-CBC( base64( hex-bytes(serialMd5Hex + expiryYYYYMMDD) ) ) ), halves swapped
    ///
    /// LicenseService only ever needs <see cref="TryDecrypt"/>. <see cref="Generate"/> exists
    /// because the legacy app's key-issuing tool isn't part of this codebase (only the
    /// verifier shipped) — without it nobody could ever produce a LicenseKey that passes
    /// validation. Both directions live in one file so they can't drift out of sync.
    /// </summary>
    public static class LicenseKeyCodec
    {
        // Fixed AES-256-CBC key/IV the legacy app wraps every issued LicenseKey with.
        // Ported byte-for-byte from LicenseManager.Decrypt.
        private static readonly byte[] WrapKey = Convert.FromBase64String("4I9D6dfx9tww+J7NyP3KZ+jD4Vq6SsPmmZvNAcPgUoU=");
        private static readonly byte[] WrapIv = Convert.FromBase64String("tDr5qdXokODro+U75VsWTw==");

        public static string Generate(string serialNumberHex, DateOnly expiresOn)
        {
            var licenseDataHex = serialNumberHex + expiresOn.ToString("yyyyMMdd");
            var rawBytes = Convert.FromHexString(licenseDataHex);
            var plainBase64 = Convert.ToBase64String(rawBytes);

            using var aes = Aes.Create();
            aes.Key = WrapKey;
            aes.IV = WrapIv;
            using var encryptor = aes.CreateEncryptor();
            var cipherBytes = encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(plainBase64), 0, plainBase64.Length);

            var cipherHex = Convert.ToHexString(cipherBytes);
            return SwapHalves(cipherHex);
        }

        /// <returns>The embedded serial number (uppercase hex) and expiry date, or null if the
        /// key is malformed/undecryptable. Caller is responsible for comparing the serial
        /// against the current machine and the expiry against today.</returns>
        public static (string SerialNumber, DateOnly ExpiresOn)? TryDecrypt(string licenseKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(licenseKey) || licenseKey.Length < 2 || licenseKey.Length % 2 != 0)
                    return null;

                var cipherHex = SwapHalves(licenseKey);
                var cipherBytes = Convert.FromHexString(cipherHex);

                using var aes = Aes.Create();
                aes.Key = WrapKey;
                aes.IV = WrapIv;
                using var decryptor = aes.CreateDecryptor();
                var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                var plainBase64 = Encoding.UTF8.GetString(plainBytes);

                var licenseDataHex = Convert.ToHexString(Convert.FromBase64String(plainBase64));
                if (licenseDataHex.Length < 10) return null;

                var serialNumber = licenseDataHex[..^8];
                var expiryRaw = licenseDataHex[^8..];

                if (!DateTime.TryParseExact(expiryRaw, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var expiryDate))
                    return null;

                return (serialNumber, DateOnly.FromDateTime(expiryDate));
            }
            catch
            {
                return null;
            }
        }

        private static string SwapHalves(string s)
        {
            var half = s.Length / 2;
            return s[half..] + s[..half];
        }

        /// <summary>MD5 hash (uppercase hex) of this machine's disk serial — ported from
        /// Common/ServerInfo.cs + LicenseManager.GetSerialNumber. Both LicenseService (to
        /// validate) and a license-issuing tool (to generate) need this, so it lives here.</summary>
        public static string ComputeMachineSerialNumber()
        {
            try
            {
                var diskInfo = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? GetDiskInfoWindows()
                    : GetDiskInfoLinux();

                var hash = MD5.HashData(Encoding.ASCII.GetBytes(diskInfo));
                return Convert.ToHexString(hash);
            }
            catch
            {
                return string.Empty;
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetVolumeInformation(
            string lpRootPathName,
            StringBuilder lpVolumeNameBuffer,
            int nVolumeNameSize,
            out uint lpVolumeSerialNumber,
            out uint lpMaximumComponentLength,
            out uint lpFileSystemFlags,
            StringBuilder lpFileSystemNameBuffer,
            int nFileSystemNameSize);

        private static string GetDiskInfoWindows(string disk = @"C:\")
        {
            try
            {
                var volumeName = new StringBuilder(256);
                var fileSystemName = new StringBuilder(256);

                var success = GetVolumeInformation(disk, volumeName, volumeName.Capacity,
                    out var serialNumber, out _, out _, fileSystemName, fileSystemName.Capacity);

                return success ? serialNumber.ToString(CultureInfo.InvariantCulture) : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetDiskInfoLinux(string disk = "/dev/sda")
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "udevadm",
                    Arguments = $"info --query=all --name={disk}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo)!;
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                const string serialKey = "E: ID_SERIAL=";
                foreach (var line in output.Split('\n'))
                {
                    if (line.StartsWith(serialKey, StringComparison.Ordinal))
                        return line[serialKey.Length..].Trim();
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
