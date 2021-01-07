using System;
using System.Collections.Generic;

namespace ServerManagerTool.Lib
{
    public class BranchSnapshot
    {
        public string BranchName = string.Empty;
        public string BranchPassword = string.Empty;
    }

    public class BranchSnapshotComparer : IEqualityComparer<BranchSnapshot>
    {
        public bool Equals(BranchSnapshot x, BranchSnapshot y)
        {
            //Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (x is null || y is null)
                return false;

            //Check whether the snapshot' properties are equal.
            return x.BranchName == y.BranchName;
        }

        public int GetHashCode(BranchSnapshot snapshot)
        {
            //Check whether the object is null
            if (snapshot is null) return 0;

            //Get hash code for the Name field if it is not null.
            return snapshot.BranchName == null ? 0 : snapshot.BranchName.GetHashCode();
        }
    }
}
