using NLog;
using System;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;

namespace ServerManagerTool.Common.Utils
{
    public static class MachineUtils
    {
        private const int OS_ANYSERVER = 29;

        [DllImport("shlwapi.dll", SetLastError = true, EntryPoint = "#437")]
        private static extern bool IsOS(int os);

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static bool IsWindowsServer()
        {
            return IsOS(OS_ANYSERVER);
        }

        public static bool IsThisMachineADomainController()
        {
            try
            {
                Domain domain = Domain.GetCurrentDomain();
                if (domain == null)
                    return false;

                string thisMachine = $"{Environment.MachineName}.{domain}".ToLower();

                var domainControllers = domain.DomainControllers.OfType<DomainController>();
                return domainControllers.Any(dc => dc.Name.Equals(thisMachine, StringComparison.OrdinalIgnoreCase));
            }
            catch (ActiveDirectoryObjectNotFoundException ex)
            {
                _logger.Debug($"{nameof(IsThisMachineADomainController)} checked. {ex.Message}");
                return false;
            }
            catch (ActiveDirectoryOperationException ex)
            {
                _logger.Debug($"{nameof(IsThisMachineADomainController)} checked. {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(IsThisMachineADomainController)}. {ex.Message}\r\n{ex.StackTrace}");
                return false;
            }
        }

        public static bool IsThisMachinePartOfADomain()
        {
            ManagementObject manObject = new ManagementObject(string.Format("Win32_ComputerSystem.Name='{0}'", Environment.MachineName));
            return (bool)manObject["PartOfDomain"];
        }
    }
}
