using System.Text;

namespace NeXt.Vdf
{
    static class StringRepeatUtils
    {
        /// <summary>
        /// Repeats a string i times
        /// </summary>
        /// <param name="self"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        public static string Repeat(this string self, int i)
        {
            if(i < 1)
            {
                return string.Empty;
            }
            else if(i < 2)
            {
                return self;
            }
            else if(i < 3)
            {
                return self + self;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                for(int x = 0; x < i ; x++)
                {
                    sb.Append(self);
                }
                return sb.ToString();
            }
        }
    }
}
