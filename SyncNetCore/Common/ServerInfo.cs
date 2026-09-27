using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace SyncNet.Common
{
    class ServerInfo
    {
        #region Windows
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

        public static string GetDiskInfoWindows(string disk = @"C:\")
        {
            string result = "";

            try
            {
                uint serialNumber, maxComponentLen, fileSystemFlags;

                StringBuilder volumeName = new StringBuilder(256);
                StringBuilder fileSystemName = new StringBuilder(256);

                bool success = GetVolumeInformation(disk, volumeName, volumeName.Capacity,
                    out serialNumber, out maxComponentLen, out fileSystemFlags, fileSystemName,
                    fileSystemName.Capacity);

                //validate result
                if (success) result = serialNumber.ToString();
            }
            catch { }

            return result;
        }
        #endregion

        #region Linux
        public static string GetDiskInfoLinux(string disk = "/dev/sda")
        {
            string retval = "";

            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "udevadm",
                    Arguments = $"info --query=all --name={disk}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = processStartInfo;
                    process.Start();

                    string result = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Mem-parsing hasil output untuk mendapatkan serial number
                    retval = ParseSerialNumber(result);
                }
            }
            catch { }

            return retval;
        }

        private static string ParseSerialNumber(string udevadmOutput)
        {
            const string serialKey = "E: ID_SERIAL=";

            foreach (var line in udevadmOutput.Split('\n'))
            {
                if (line.StartsWith(serialKey))
                {
                    return line.Substring(serialKey.Length).Trim();
                }
            }

            return "Serial number not found";
        }
        #endregion

        #region General
        public static string GetMacAddress()
        {
            string retval = "";

            try
            {
                var macAddress = NetworkInterface
                    .GetAllNetworkInterfaces()
                    .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .Select(nic => nic.GetPhysicalAddress().ToString())
                    .FirstOrDefault();

                retval = macAddress ?? "";
            }
            catch { }

            return retval;
        }
        #endregion
    }
}
