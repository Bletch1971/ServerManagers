using System;
using System.Collections.Generic;

namespace ServerManagerTool.Lib
{
    public class BranchSnapshot
    {
        private BranchSnapshot()
        {
        }

        public string AppIdServer = string.Empty;
        public string BranchName = string.Empty;
        public string BranchPassword = string.Empty;

        public static BranchSnapshot Create(ServerProfile profile)
        {
            return new BranchSnapshot
            {
                AppIdServer = profile.SOTF_Enabled ? Config.Default.AppIdServer_SotF : string.Empty,
                BranchName = profile.BranchName,
                BranchPassword = profile.BranchPassword
            };
        }

        public static BranchSnapshot Create(ServerProfileSnapshot profile)
        {
            return new BranchSnapshot
            {
                AppIdServer = profile.AppIdServer,
                BranchName = profile.BranchName,
                BranchPassword = profile.BranchPassword
            };
        }
    }

    public class BranchSnapshotComparer : IEqualityComparer<BranchSnapshot>
    {
        public bool Equals(BranchSnapshot x, BranchSnapshot y)
        {
            //Check whether the compared objects reference the same data.
            if (ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (x is null || y is null)
                return false;

            //Check whether the snapshot properties are equal.
            var result = string.Equals(x.AppIdServer ?? string.Empty, y.AppIdServer ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            return result && string.Equals(x.BranchName ?? string.Empty, y.BranchName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(BranchSnapshot snapshot)
        {
            //Check whether the object is null
            if (snapshot is null) 
                return 0;

            //Get hash code for the Name field if it is not null.
            var result = $"{snapshot.AppIdServer ?? ""}-{snapshot.BranchName ?? ""}";
            return result.GetHashCode();
        }
    }
}
