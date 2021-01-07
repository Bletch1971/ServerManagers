using System;
using System.Collections.Generic;

namespace ServerManagerTool.Lib
{
    public class ServerBranchSnapshot
    {
        public string BranchName = string.Empty;
        public string BranchPassword = string.Empty;
    }

    public class ServerBranchSnapshotComparer : IEqualityComparer<ServerBranchSnapshot>
    {
        public bool Equals(ServerBranchSnapshot x, ServerBranchSnapshot y)
        {
            //Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (x is null || y is null)
                return false;

            //Check whether the snapshot' properties are equal.
            return x.BranchName == y.BranchName;
        }

        public int GetHashCode(ServerBranchSnapshot snapshot)
        {
            //Check whether the object is null
            if (snapshot is null) return 0;

            //Get hash code for the Name field if it is not null.
            return snapshot.BranchName == null ? 0 : snapshot.BranchName.GetHashCode();
        }
    }
}
