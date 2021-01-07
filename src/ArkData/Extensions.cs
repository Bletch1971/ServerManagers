using System.Collections.Generic;

namespace ArkData
{
    internal static class Extensions
    {
        private static readonly int[] Empty = new int[0];

        public static int LocateFirst(this byte[] self, byte[] candidate, int offset = 0)
        {
            if (IsEmptyLocate(self, candidate, offset))
                return -1;

            for (int position = offset; position < self.Length; position++)
                if (IsMatch(self, position, candidate))
                    return position;
     
            return -1;
        }

        public static int[] Locate(this byte[] self, byte[] candidate)
        {
            if (IsEmptyLocate(self, candidate, 0))
                return Empty;

            List<int> list = new List<int>();
            for (int position = 0; position < self.Length; ++position)
                if (IsMatch(self, position, candidate))
                    list.Add(position);

            if (list.Count != 0)
                return list.ToArray();

            return Empty;
        }

        private static bool IsMatch(byte[] array, int position, byte[] candidate)
        {
            if (candidate.Length > array.Length - position)
                return false;

            for (int index = 0; index < candidate.Length; ++index)
                if ((array[position + index] != candidate[index]))
                    return false;

            return true;
        }

        private static bool IsEmptyLocate(byte[] array, byte[] candidate, int offset)
        {
            if (array != null && candidate != null &&
               (array.Length != 0 && candidate.Length != 0) &&
               (candidate.Length <= array.Length && offset != -1))
                return offset > array.Length;
            return true;
        }
    }
}
