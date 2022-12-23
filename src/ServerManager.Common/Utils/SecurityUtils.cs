using NLog;
using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Net;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace ServerManagerTool.Common.Utils
{
    public static class SecurityUtils
    {
        private const string PasswordChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string GROUP_USERSBUILTIN = @"BUILTIN\Users";

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static int callCount = 0;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "SCS0005:Weak random number generator.", Justification = "<Pending>")]
        public static string GeneratePassword(int count)
        {
            StringBuilder newPassword = new StringBuilder(count);
            Random random;
            unchecked
            {
                random = new Random((int)DateTime.Now.Ticks + callCount);
                callCount++;
            }

            for(int i = 0; i < count; i++)
            {
                newPassword.Append(PasswordChars[random.Next(PasswordChars.Length)]);
            }

            return newPassword.ToString();
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return  principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static bool SetDirectoryOwnershipForAllUsers(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return true;

            try
            {
                var directoryInfo = new DirectoryInfo(directory);
                var security = directoryInfo.GetAccessControl(AccessControlSections.Access);
                bool result;

                var iFlags = InheritanceFlags.None;

                // *** Add Access Rule to the actual directory itself
                var accessRule = new FileSystemAccessRule(GROUP_USERSBUILTIN, FileSystemRights.FullControl, iFlags, PropagationFlags.NoPropagateInherit, AccessControlType.Allow);
                security.ModifyAccessRule(AccessControlModification.Set, accessRule, out result);

                if (!result)
                    return false;

                // *** Always allow objects to inherit on a directory
                iFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;

                // *** Add Access rule for the inheritance
                accessRule = new FileSystemAccessRule(GROUP_USERSBUILTIN, FileSystemRights.FullControl, iFlags, PropagationFlags.InheritOnly, AccessControlType.Allow);
                security.ModifyAccessRule(AccessControlModification.Add, accessRule, out result);

                if (!result)
                    return false;

                directoryInfo.SetAccessControl(security);
                return true;
            }
            catch (Exception ex)
            {
                // We give it a best-effort here.  If we aren't running an enterprise OS, this group may not exist (or be needed.)
                _logger.Error($"{nameof(SetDirectoryOwnershipForAllUsers)}. {ex.Message}\r\n{ex.StackTrace}");
                return false;
            }
        }

        public static bool SetFileOwnershipForAllUsers(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                return true;

            try
            {
                var fileInfo = new FileInfo(file);
                var security = fileInfo.GetAccessControl(AccessControlSections.Access);
                bool result;

                var iFlags = InheritanceFlags.None;

                // *** Add Access Rule to the actual file itself
                var accessRule = new FileSystemAccessRule(GROUP_USERSBUILTIN, FileSystemRights.FullControl, iFlags, PropagationFlags.NoPropagateInherit, AccessControlType.Allow);
                security.ModifyAccessRule(AccessControlModification.Set, accessRule, out result);

                if (!result)
                    return false;

                fileInfo.SetAccessControl(security);
                return true;
            }
            catch (Exception ex)
            {
                // We give it a best-effort here.  If we aren't running an enterprise OS, this group may not exist (or be needed.)
                _logger.Error($"{nameof(SetFileOwnershipForAllUsers)}. {ex.Message}\r\n{ex.StackTrace}");
                return false;
            }
        }

        public static bool DoesLocalWindowsAccountExist(string username)
        {
            try
            {
                if (MachineUtils.IsThisMachineADomainController())
                    throw new Exception($"This computer reports to be a domain controller.");

                var context = new PrincipalContext(ContextType.Machine, Environment.MachineName);
                if (context == null)
                    context = new PrincipalContext(ContextType.Machine);
                if (context == null)
                    throw new Exception($"Could not create instance of the PrincipalContext ({Environment.MachineName}).");

                var user = UserPrincipal.FindByIdentity(context, IdentityType.Name, username);
                if (user == null || !(user.Enabled.HasValue && user.Enabled.Value) || user.IsAccountLockedOut())
                    return false;

                var usersGroupSID = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                GroupPrincipal group = GroupPrincipal.FindByIdentity(context, IdentityType.Sid, usersGroupSID.Value);
                if (group == null || !group.Members.Contains(user))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(DoesLocalWindowsAccountExist)}. {ex.Message}\r\n{ex.StackTrace}");
                return false;
            }
        }

        public static bool CreateLocalWindowsAccount(string username, string password, string displayName, string description, bool userCannotChangePassword, bool passwordNeverExpires)
        {
            try
            {
                if (MachineUtils.IsThisMachineADomainController())
                    throw new Exception($"This computer reports to be a domain controller. User cannot be created.");

                var context = new PrincipalContext(ContextType.Machine, Environment.MachineName);
                if (context == null)
                    context = new PrincipalContext(ContextType.Machine);
                if (context == null)
                    throw new Exception($"Could not create instance of the PrincipalContext ({Environment.MachineName}).");

                // create the user if it does not exist
                var user = UserPrincipal.FindByIdentity(context, IdentityType.Name, username);
                if (user == null)
                {
                    user = new UserPrincipal(context)
                    {
                        Name = username,
                        DisplayName = displayName,
                        Description = description,
                    };

                    if (user == null)
                        throw new Exception($"Could not create new instance of the UserPrincipal ({username}).");
                }

                user.Enabled = true;
                user.PasswordNeverExpires = passwordNeverExpires;
                user.UserCannotChangePassword = userCannotChangePassword;
                user.SetPassword(password);
                if (user.IsAccountLockedOut())
                    user.UnlockAccount();
                user.Save();

                // now add user to "Users" group
                var usersGroupSID = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                if (usersGroupSID == null)
                    throw new Exception($"Could not find instance of the SecurityIdentifier (WellKnownSidType.BuiltinUsersSid).");

                GroupPrincipal group = GroupPrincipal.FindByIdentity(context, IdentityType.Sid, usersGroupSID.Value);
                if (group == null)
                    throw new Exception($"Could not find instance of the GroupPrincipal (USERS).");

                if (!group.Members.Contains(user))
                {
                    group.Members.Add(user);
                    group.Save();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(CreateLocalWindowsAccount)}. {ex.Message}\r\n{ex.StackTrace}");
                return false;
            }
        }

        public static bool ChangeWindowsAccountPassword(string username, string password)
        {
            try
            {
                if (MachineUtils.IsThisMachineADomainController())
                    throw new Exception($"This computer reports to be a domain controller. User password cannot be changed.");

                var context = new PrincipalContext(ContextType.Machine, Environment.MachineName);
                if (context == null)
                    context = new PrincipalContext(ContextType.Machine);
                if (context == null)
                    throw new Exception($"Could not create instance of the PrincipalContext ({Environment.MachineName}).");

                // create the user if it does not exist
                var user = UserPrincipal.FindByIdentity(context, IdentityType.Name, username);
                if (user == null)
                    throw new Exception($"Could not find instance of the UserPrincipal ({username}).");

                user.SetPassword(password);
                user.Save();

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(ChangeWindowsAccountPassword)}. {ex.Message}\r\n{ex.StackTrace}");
                return false;
            }
        }

        public static bool DeleteLocalWindowsAccount(string username)
        {
            try
            {
                if (MachineUtils.IsThisMachineADomainController())
                    throw new Exception($"This computer reports to be a domain controller. User cannot be deleted.");

                var context = new PrincipalContext(ContextType.Machine, Environment.MachineName);
                if (context == null)
                    context = new PrincipalContext(ContextType.Machine);
                if (context == null)
                    throw new Exception($"Could not create instance of the PrincipalContext ({Environment.MachineName}).");

                // create the user if it does not exist
                var user = UserPrincipal.FindByIdentity(context, IdentityType.Name, username);
                if (user == null)
                    throw new Exception($"Could not find instance of the UserPrincipal ({username}).");

                // now remove user from "Users" group
                var usersGroupSID = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                if (usersGroupSID != null)
                {
                    GroupPrincipal group = GroupPrincipal.FindByIdentity(context, IdentityType.Sid, usersGroupSID.Value);
                    if (group != null && group.Members.Contains(user))
                        group.Members.Remove(user);
                }

                user.Delete();

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(DeleteLocalWindowsAccount)}. {ex.Message}\r\n{ex.StackTrace}");
                return false;
            }
        }

        public static SecureString GetSecureString(string sourceString)
        {
            var secureString = new SecureString();
            foreach (var sourceChar in sourceString)
            {
                secureString.AppendChar(sourceChar);
            }
            return secureString;
        }

        public static SecurityProtocolType GetSecurityProtocol(int securityProtocolValue)
        {
            if (Enum.TryParse(securityProtocolValue.ToString(), out SecurityProtocolType securityProtocol))
                return securityProtocol;
            return SecurityProtocolType.Tls12;
        }
    }
}
