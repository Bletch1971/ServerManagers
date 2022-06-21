using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFirewallHelper;
using WindowsFirewallHelper.FirewallRules;

/*
WindowsFirewallHelper
A class library to manage the Windows Firewall as well as adding your program to the Windows Firewall Exception list

=========================================================================
USAGE
    Get an instance of the active firewall  using FirewallManager class and
    use the properties to get the list of firewall rules and profiles. 
    You can also  use the methods  on this class  to add a new rule  to the 
    firewall.

CODE SAMPLE FOR ADDING AN APPLICATION RULE TO THE ACTIVE PROFILE:
    var rule = FirewallManager.Instance.CreateApplicationRule(FirewallManager.Instance.GetProfile().Type, @"MyApp Rule", FirewallAction.Allow, @"C:\MyApp.exe"); 
    rule.Direction = FirewallDirection.Outbound;
    FirewallManager.Instance.Rules.Add(rule);

CODE SAMPLE FOR ADDING A PORT RULE TO THE ACTIVE PROFILE:
    var rule = FirewallManager.Instance.CreatePortRule(FirewallManager.Instance.GetProfile().Type, @"Port 80 - Any Protocol", FirewallAction.Allow, 80, FirewallProtocol.Any);
    FirewallManager.Instance.Rules.Add(rule);

CODE SAMPLE TO GET A LIST OF ALL REGISTERED RULES:
    var allRules = FirewallManager.Instance.Rules.ToArray();

MORE SAMPLES:
    Check the Project's Github page at: https://github.com/falahati/WindowsFirewallHelper
 */


namespace ServerManagerTool.Common.Utils
{
    /// <summary>
    /// Code for dealing with firewalls
    /// </summary>
    public static class FirewallUtils
    {
        public static bool CreateFirewallRules(string exeName, List<int> ports, string ruleName, string description = "", string group = "")
        {
            if (FirewallWAS.IsSupported(new COMTypeResolver()))
            {
                var firewallManager = FirewallManager.Instance;

                DeleteFirewallRules(exeName);

                // create the TCP rule
                var rule = firewallManager.CreateApplicationRule(FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public, $"{ruleName} TCP", FirewallAction.Allow, exeName, FirewallProtocol.TCP);
                if (rule != null)
                {
                    rule.Direction = FirewallDirection.Inbound;
                    rule.IsEnable = true;
                    rule.LocalPorts = ports.Select(p => (ushort)p).ToArray();

                    if (rule is FirewallWASRule wasRule)
                    {
                        wasRule.Description = description;
                        wasRule.Grouping = group;
                    }
                    firewallManager.Rules.Add(rule);
                }

                // create the UDP rule
                rule = firewallManager.CreateApplicationRule(FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public, $"{ruleName} UDP", FirewallAction.Allow, exeName, FirewallProtocol.UDP);
                if (rule != null)
                {
                    rule.Direction = FirewallDirection.Inbound;
                    rule.IsEnable = true;
                    rule.LocalPorts = ports.Select(p => (ushort)p).ToArray();

                    if (rule is FirewallWASRule wasRule)
                    {
                        wasRule.Description = description;
                        wasRule.Grouping = group;
                    }
                    firewallManager.Rules.Add(rule);
                }

                return true;
            }

            return false;
        }

        public static bool DeleteFirewallRules(string exeName)
        {
            if (FirewallWAS.IsSupported(new COMTypeResolver()))
            {
                var firewallManager = FirewallManager.Instance;

                // check for existing rules
                var rulesToDelete = firewallManager.Rules.Cast<IFirewallRule>().Where(r => !string.IsNullOrWhiteSpace(r.ApplicationName) && r.ApplicationName.Equals(exeName, StringComparison.OrdinalIgnoreCase)).ToList();

                if (rulesToDelete != null && rulesToDelete.Count > 0)
                {
                    // delete the existing rules
                    rulesToDelete.ForEach(r => firewallManager.Rules.Remove(r));
                }

                return true;
            }

            return false;
        }
    }
}
