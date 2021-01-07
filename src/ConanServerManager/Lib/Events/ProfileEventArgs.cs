using System;

namespace ServerManagerTool.Lib
{
    public class ProfileEventArgs : EventArgs
    {
        public ProfileEventArgs(ServerProfile profile)
        {
            Profile = profile;
        }

        public ServerProfile Profile;
    }
}
