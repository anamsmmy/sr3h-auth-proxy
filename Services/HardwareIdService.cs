using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using SR3H_MACRO.Services;

namespace MacroApp.Services
{
    public class HardwareIdService
    {
        public static string GenerateHardwareId()
        {
            return SafeHardwareIdService.GenerateHardwareId();
        }

        public async Task<string> GenerateHardwareIdAsync()
        {
            return await Task.Run(() => SafeHardwareIdService.GenerateHardwareId());
        }

        public async Task<bool> ValidateHardwareIdAsync(string storedHwid)
        {
            try
            {
                var currentHwid = await GenerateHardwareIdAsync();
                return currentHwid.Equals(storedHwid, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetHardwareInfoAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var info = new StringBuilder();
                    info.AppendLine($"Machine Name: {Environment.MachineName}");
                    info.AppendLine($"User Name: {Environment.UserName}");
                    info.AppendLine($"OS Version: {Environment.OSVersion}");
                    
                    var macAddress = GetMacAddress();
                    if (!string.IsNullOrEmpty(macAddress))
                        info.AppendLine($"MAC Address: {macAddress}");
                    
                    return info.ToString();
                }
                catch (Exception ex)
                {
                    return $"Error getting hardware info: {ex.Message}";
                }
            });
        }

        private static string GetMacAddress()
        {
            try
            {
                var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(nic => nic.OperationalStatus == OperationalStatus.Up && 
                                 nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                 nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .FirstOrDefault();
                
                return networkInterface?.GetPhysicalAddress().ToString();
            }
            catch { }
            return null;
        }
    }
}