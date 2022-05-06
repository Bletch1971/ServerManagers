using System;
using System.Net;

namespace ServerManagerTool.Updater
{
    public static class SecurityUtils
    {
        public static SecurityProtocolType GetSecurityProtocol(int securityProtocolValue)
        {
            if (Enum.TryParse(securityProtocolValue.ToString(), out SecurityProtocolType securityProtocol))
                return securityProtocol;
            return SecurityProtocolType.Tls12;
        }
    }
}
