using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    // extension methods, added to any suitable type
    public static partial class nmExtensions
    {

        /// <summary>
        /// Safe substring allowing out of range values of start/length ( string is clipped as needed )
        /// </summary>
        public static string SubStr(this string value, int start, int length = int.MaxValue / 2)
        {
            if (value == null) return "";
            // if actual start is before beggining of string, clip to beginning (but reduce length accordingly)
            if (start < 0)
            {
                length += start;
                start = 0;
            }
            // basic check for empty string result
            int Len = value.Length;
            if ((Len == 0) || (length <= 0) || (start >= Len))
                return "";
            // if actual end is beyond end of string, clip length
            if (start + length > Len)
                length = Len - start;
            // return substring from safe start/length values
            return value.Substring(start, length);
            //return new string((value ?? string.Empty).Skip(startIndex).Take(length).ToArray());
        }

        /// <summary>
        /// Safe substring with special meaning for negative start/length, allowing out of range values:
        /// </summary>
        /// <param name="startIndex">if startIndex less than 0, it count from end of string.  So "12345".SubStrNeg(-2,3)=="45"</param>
        /// <param name="length">length less than 0,  start is actually end of substring. So "12345".SubStrNeg(3,-2)=="34"</param>
        public static string SubStrNeg(this string value, int startIndex, int length)
        {
            if (value == null) return "";
            int Len = value.Length;
            if ((Len == 0) || (length == 0)) return "";
            // negative startIndex counts from end of string
            int start = (startIndex >= 0) ? startIndex : Len + startIndex;
            // negative length means start== end of substring
            if (length < 0)
            {
                start += length + 1;
                length = -length;
            }
            return value.SubStr(start, length);
        }




    }


}
