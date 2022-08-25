using CalculatorNotepad;
using Mpfr.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Numbers
{
    #region ENUMs 

    /// <summary>
    /// supported 'Number' classes
    /// </summary>
    public enum NumberClass { Double = 10, MPFR = 20, Quad = 30 }

    /// <summary>
    /// possible special values, in comparison order ( except None, to compare with regulars set them to Sign*2  )
    /// </summary>
    public enum SpecialFloatValue { NegativeInfinity = -10, NegativeZero = -1, None = 0, Zero = 1, PositiveZero = 1, Infinity = 10, PositiveInfinity = 10, NaN = 20 }

    #endregion

    #region short param types

    /// <summary>
    /// special Number exception, to be easily catcheable and debuggable
    /// </summary>
    public class NumberException : Exception
    {
        public NumberException(string err) : base(err)
        {
            int a = 3;
        }
    }

    /// <summary>
    /// Define format parameters for Number.ToString()
    /// </summary>
    public class NumberFormat
    {
        public int Base = 10;
        public int SignificantDigits = -1;
        public int DecZeros = 4;
        public int Width = -1;
        public bool ShowBasePrefix = true;
        public string ThousandsSeparator = "";
        public string FractionSeparator = "";
        public int ThousandsGroup = -1;
        public int FractionGroup = -1;
        public bool? ShowNumberType = null;
        public bool ShowPrecision = false;
        /// <summary>
        /// Format options for for Number.ToString(StringFormat F)
        /// </summary>
        /// <param name="base">In which outuput bse to format string. Default 10.</param>
        /// <param name="significantDigits">How many digits in total should number have in mantissa (left and right of point, not counting exponent digits ). Zero or negative values use max digits for type, reduced by this number. Default -1</param>
        /// <param name="showBasePrefix">For non-decimal bases, should base prefix be appended, ie '0xFF' instead of 'FF'</param>
        /// <param name="decZeros">how many zeros must number start with before it is auto-converted to exponential form. Default 4</param>
        /// <param name="width">total width of output string - if result is shorter, padding will be used. Default -1 means no padding.</param>
        /// <param name="thousandsSeparator">Character to separate thousand groups, eg  if ',' then '1,234,567.89012345'</param>
        /// <param name="fractionSeparator">Character to separate fraction groups, eg  if '_' then '1234567.890_123_45'</param>
        /// <param name="thousandsGroup">how many digits between mantissa separators. Default -1 use 3 for decimals, 4 for hex and 8 for bin</param>
        /// <param name="fractionGroup">how many digits between fraction separators. Default -1 uses same as for Mantissa group</param>
        /// <param name="showNumberType">true will always show number type char as suffix, eg '12q', false will never show, and default null will show if different type from default Number type.</param>
        /// <param name="showPrecision">if true, display precision ( in mantissa bits ) at end of the number string , eg '1.23m:53'.</param>
        public NumberFormat(int radixBase = 10, int significantDigits = -1, bool showBasePrefix = true, int decZeros = 4, int width = -1, 
                            string thousandsSeparator = "", string fractionSeparator = "", int thousandsGroup = -1, int fractionGroup = -1,
                            bool? showNumberType = null, bool showPrecision = false)
        {
            Base = radixBase;
            SignificantDigits = significantDigits;
            DecZeros = decZeros;
            Width = width;
            ShowBasePrefix = showBasePrefix;
            ThousandsSeparator = thousandsSeparator;
            FractionSeparator = fractionSeparator;
            ThousandsGroup = thousandsGroup;
            FractionGroup = fractionGroup;
            ShowNumberType = showNumberType;
            ShowPrecision = showPrecision;
        }
        public NumberFormat(NumberFormat other)
        {
            this.Base = other.Base;
            this.SignificantDigits = other.SignificantDigits;
            this.Width = other.Width;
            this.DecZeros = other.DecZeros;
            this.ShowBasePrefix = other.ShowBasePrefix;
            this.ThousandsSeparator = other.ThousandsSeparator;
            this.FractionSeparator = other.FractionSeparator;
            this.ThousandsGroup = other.ThousandsGroup;
            this.FractionGroup = other.FractionGroup;
            this.ShowNumberType = other.ShowNumberType;
            this.ShowPrecision = other.ShowPrecision;
        }
    }

    /// <summary>
    /// rectangle in some Number type
    /// </summary>
    public class RectangleReal<T> where T : IFloatNumber<T>
    {
        public T x1, y1, x2, y2;
        public RectangleReal(bool isZeroed = true)
        {
            if (isZeroed)
                x1 = y1 = x2 = y2 = 0;
            else
                x1 = y1 = x2 = y2 = T.NaN;
        }
        public RectangleReal(T X1, T Y1, T X2, T Y2)
        {
            x1 = X1;
            y1 = Y1;
            x2 = X2;
            y2 = Y2;
        }
    }



    #endregion

    #region BinaryFloatNumber


    /// <summary>
    /// unified generic presentation of binary-based floating point
    /// </summary>
    public class BinaryFloatNumber : IComparable<BinaryFloatNumber>
    {

        #region Data Fields
        /// <summary>
        /// designate if this value is special "not aI number" value like NaN, +/Inf, or Zero
        /// </summary>
        public SpecialFloatValue specialValue;
        /// <summary>
        /// +1 for positive numbers, -1 for negative numbers 
        /// for specialValues it *should* be -1 for NegativeInfinity or NegativeZero
        /// </summary>
        public int Sign;
        /// <summary>
        /// Exponent of float value, assuming mantissa is UInt64[] in  1.xxxx format with highest bit in last element as only 'whole part' and others are fractional.
        /// So 1.0 has exponent 0 and mantissa "1000..00" . Unlike IEEE double (exp=1023), Quad (-63), MPFR (1)
        /// </summary>
        public long Exponent;
        /// <summary>
        /// Mantissa of float value as UInt64[] array in  1.xxxx format with highest bit in last element as only 'whole part' and others are fractional.
        /// So 1.0 has exponent 0 and mantissa "1000..00" 
        /// </summary>
        public UInt64[]? Mantissa;
        /// <summary>
        /// How many useful/valid bits in Mantissa, including implied highest "1." bit. Can be less than Mantissa.Length*64. Not used in comparisons.
        /// </summary>
        public int Precision;

        #endregion

        #region constructors and properties

        private static string digitLetters; // letters that represent digits in different bases
        public static string expChar = "@";

        public bool isZero => specialValue == SpecialFloatValue.Zero || specialValue == SpecialFloatValue.NegativeZero;
        public bool isNaN => specialValue == SpecialFloatValue.NaN;
        public bool isInfinity => specialValue == SpecialFloatValue.PositiveInfinity || specialValue == SpecialFloatValue.NegativeInfinity;


        static BinaryFloatNumber()
        {
            digitLetters = "";
            for (int i = 0; i < 10; i++) digitLetters += (char)(i + '0');
            for (int i = 0; i < 26; i++) digitLetters += (char)(i + 'A');
            for (int i = 0; i < 26; i++) digitLetters += (char)(i + 'a');
        }

        /// <summary>
        /// create instance without Mantissa and exponent, so only valid for special types
        /// </summary>
        public BinaryFloatNumber(SpecialFloatValue specType = SpecialFloatValue.NaN)
        {
            specialValue = specType;
            Mantissa = new UInt64[0];
        }

        /// <summary>
        /// create copy of number, with optionaly extended 'newLength' Mantissa
        /// </summary>
        /// <param name="other">source number</param>
        /// <param name="copyLength">if larger than source Mantissa.Length, it is used for clone. If under zero, no copy is done. Otherwise copy same length</param>
        /// <param name="detectRepeatingPatterns">if true, detect trailing repeating bit pattern to use in extension copy. Default false</param>
        public BinaryFloatNumber(BinaryFloatNumber other, int copyLength = 0, bool detectRepeatingPatterns = false)
        {
            specialValue = other.specialValue;
            Sign = other.Sign;
            Exponent = other.Exponent;
            Precision = other.Precision;
            // should we extend
            if (other.Mantissa == null)
                Mantissa = null;
            else if (copyLength > other.Mantissa.Length)
            {
                // extend size
                Mantissa = new UInt64[copyLength];
                other.Mantissa.CopyTo(Mantissa, copyLength - other.Mantissa.Length);
                // TODO: detect trailing bit patterns 
                if (detectRepeatingPatterns)
                {
                }
            }
            else if (copyLength >= 0)
            {
                Mantissa = new UInt64[other.Mantissa.Length];
                other.Mantissa.CopyTo(Mantissa, 0);
            }
        }


        #endregion

        #region  utility static functions, like getBaseDigits and CountOnes

        /// <summary>
        /// Convert integer to character of given base, return '?' if invalid digit
        /// </summary>
        public static char baseDigit(int digit, int Base)
        {
            if (digit < 0 || digit >= Base) return '?';
            return digitLetters[digit];
        }


        /// <summary>
        /// Convert character to integer digit of given base, allow upper/lower cases for smaller bases, return -1 if invalid.
        /// </summary>
        public static int baseDigit(char ch, int Base)
        {
            int idx = digitLetters.IndexOf(ch);
            if (idx < 0 || Base <= 0) return idx; // if not found return -1, if Base not defined return whichever position it was found
            if (idx >= 36 && Base < 36) // convert lowercase letters to uppercase for smaller bases, eg '0xff' -> '0xFF'
                idx -= 26;
            return idx >= Base ? -1 : idx; // if Base was defined, return position only if within Base, otherwise return -1
        }

        /// <summary>
        /// get string with all digits of that base, include both upper/lower cases for smaller bases
        /// </summary>
        public static string getBaseDigits(int Base)
        {
            if (Base < 0) return digitLetters;
            var res = digitLetters.SubStr(0, Base);
            // if base is smaller than 'aI-z' range, and use letters, allow lowercase letters as aliases to uppercase letters
            if (Base < 36 && Base > 10)
                res += digitLetters.SubStr(36, Base - 10);
            return res;
        }


        /// <summary>
        /// function to return several bits from bit position in 'bits' 
        /// bitPos = position of leftmost/highest bit we need , where position 0 is HIGHMOST bit in highmost element !
        /// also append zeros if needed, ie if len larger than 'bits' size.
        /// </summary>
        public static UInt32 getBits(int bitPos, int len, UInt32[] bits)
        {
            if (len >= 32) throw new NumberException("getBits length must be less than 32!");
            int idxL = bits.Length - 1 - bitPos / 32; // index with leftmost bit
            int idxR = bits.Length - 1 - (bitPos + len - 1) / 32; // index with rightmost bit ( will be idxR <= idxL )
            if (idxL < 0) return 0;
            // get 64bit pair
            ulong X = 0;
            if (idxL >= 0 && idxL < bits.Length)
                X = (ulong)bits[idxL] << 32;
            if (idxR >= 0 && idxR < bits.Length && idxR < idxL)
                X |= bits[idxR];
            // get bit index inside ULONG, 0..63, where 0 is leftmost/highest bit
            int idx = bitPos % 32;
            // shift to left to clear all bits to the left of position
            if (idx > 0) X <<= idx;
            // shift to the right , to leave 'len' bits
            X >>= 64 - len;
            // result is smaller than 32 bits
            return (UInt32)X;
        }

        /// <summary>
        /// Count number of 1s in binary number, and return it and position of highest 1.
        /// If no 1s, return count=0, highest=-1 ; if single 1, highest= log2(number)
        /// </summary>
        public static (int count, int highest) CountOnes(ulong number)
        {
            // detect bI= log2(Base), n=number of 1s in Base
            int n = 0, bp = 0, b = -1;
            while (number != 0)
            {
                if ((number & 1) != 0)
                {
                    n++;
                    b = bp;
                }
                bp++;
                number >>= 1;
            }
            return (n, b);
        }
        /// <summary>
        /// Count number of 1s in binary number, and return it and position of highest 1
        /// If no 1s, return count=0, highest=-1 ; if single 1, highest= log2(number)
        /// </summary>
        public static (int count, int highest) CountOnes(long number) => CountOnes((ulong)number);


        /// <summary>
        /// return true for 'binary' abses: 2,4,8,16,32 ...
        /// </summary>
        public static bool isBinaryBase(long Base)
        {
            (int n, _) = CountOnes(Base);
            return n == 1;
        }

        /// <summary>
        /// count number of leading zeros in array, starting from highest bit of last element in array
        /// </summary>
        static public long CountLeadingZeros(UInt64[] array)
        {
            if (array == null || array.Length == 0) return 0;
            int k = array.Length-1;
            while (k >=0  && array[k] == 0) k--;
            long res = (array.Length - 1-k)*64;
            if (k < 0) return res; // all zeros
            UInt64 w = array[k];
            while ((w & 0x8000000000000000ul) == 0ul)
            {
                res++;
                w <<= 1;
            }
            return res;
        }


        /// <summary>
        /// return how many decimal (or other base) digits are valid based on number of bits 
        /// </summary>
        public static int validDigitsInBase(int mantissaBitsSize, int Base)
        {
            try
            {
                return (int)Math.Floor(mantissaBitsSize / Math.Log2(Base));
            }
            catch
            {
                return 1;
            }
        }



        #endregion

        #region HashCode and comparisons


        public static bool operator !=(BinaryFloatNumber a, BinaryFloatNumber b) => !(a == b);
        public static bool operator ==(BinaryFloatNumber a, BinaryFloatNumber b) { var c = CompareTo(a, b); return c.cmp == 0 && !c.unordered; }
        public static bool operator >(BinaryFloatNumber a, BinaryFloatNumber b) { var c = CompareTo(a, b); return c.cmp > 0 && !c.unordered; }
        public static bool operator <(BinaryFloatNumber a, BinaryFloatNumber b) { var c = CompareTo(a, b); return c.cmp < 0 && !c.unordered; }
        public static bool operator >=(BinaryFloatNumber a, BinaryFloatNumber b) { var c = CompareTo(a, b); return c.cmp >= 0 && !c.unordered; }
        public static bool operator <=(BinaryFloatNumber a, BinaryFloatNumber b) { var c = CompareTo(a, b); return c.cmp <= 0 && !c.unordered; }

        /// <summary>
        /// Check if aI.CompareTo(bI) == cmp, but any unordered result return false ( so NaN==naN -> false )
        /// </summary>
        public static bool CMP(BinaryFloatNumber a, BinaryFloatNumber b, int cmp)
        {
            var res = CompareTo(a, b);
            if (res.unordered) return false; // if unordered, comparison always return false
            return Math.Sign(res.cmp) == Math.Sign(cmp);
        }


        /// <summary>
        /// compare two BinaryFloatNumber. Ignore unordered, so NaN.compareTo(NaN)==0 and NaN.compareTo(**)=-1
        /// </summary>
        public int CompareTo(BinaryFloatNumber other)
        {
            (int cmp, bool unordered) = CompareTo(this, other);
            return cmp;
        }



        /// <summary>
        /// return comparison value in cmp : +1 if aI>bI, 0 if aI==bI, -1 if aI<b ; unordered=true if a|b were NaN
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static (int cmp, bool unordered) CompareTo(BinaryFloatNumber a, BinaryFloatNumber b)
        {
            // treat both nulls as well defined comparison
            if (a is null && b is null) return (0, false);
            // treat ONE null same as NaN : false on any comparison except != . Return NULL which means "unordered"
            if (b is null) return (1,true);
            if (a is null) return (-1, true);
            // check NaNs in same way: nordered=true, same with other NaN, smaller than anything else
            if (a.isNaN && b.isNaN) return (0,true);
            if (b.isNaN) return (1, true);
            if (a.isNaN) return (-1, true);
            // check other special values in order -inf, -number, -zero, zero, +number, +inf
            int aS, bS;
            if (a.specialValue != SpecialFloatValue.None || b.specialValue != SpecialFloatValue.None)
            {
                aS = (int)a.specialValue;
                bS = (int)b.specialValue;
                if (aS == 0) aS = a.Sign * 2;  // None==0, so it not spec value, set it at -2 for negatives ( less than NegativeZero=-1, more than negativeInfinity=-10) or +2 ( more than PositiveZero=1, less than PositiveInf=+10 or NaN=+20 )
                if (bS == 0) bS = b.Sign * 2;
                return (aS.CompareTo(bS), false);
            }
            // compare signs
            aS = a.Sign < 0 ? -1 : +1; // ensure signs are +1 or -1 even if some sign was 0 
            bS = b.Sign < 0 ? -1 : +1;
            var c= aS.CompareTo(bS);
            if (c != 0) return (c,false);
            // if same sign, compare exponents
            int res = a.Exponent.CompareTo(b.Exponent);
            if (res != 0) return (res * aS,false); // fix for sign - absolutely larger negative number is actually smaller
            // if exponent also same, check if any mantissa is NULL, and treat as NaNs
            if (a.Mantissa == null && b.Mantissa == null) return (0,false);
            if (b.Mantissa == null) return (1, true);
            if (a.Mantissa == null) return (-1, true);
            // compare up to common length, starting from LAST element backwards
            int sz = Math.Min(a.Mantissa.Length, b.Mantissa.Length);
            int iA = a.Mantissa.Length - 1;
            int iB = b.Mantissa.Length - 1;
            while (iA >= 0 && iB >= 0)
            {
                res = a.Mantissa[iA].CompareTo(b.Mantissa[iB]);
                if (res != 0) return (res * aS,false);
                iA--;
                iB--;
            }
            // if any remaining value is different from zero, that one is larger ( but correct by sign )
            while (iA >= 0)
            {
                if (a.Mantissa[iA] != 0) return (+1 * aS,false);
                iA--;
            }
            while (iB >= 0)
            {
                if (b.Mantissa[iB] != 0) return (-1 * aS,false);
                iB--;
            }
            // otherwise they are same - we do not consider precision, as it is only informative
            return (0, false);
        }




        // hash code
        public override int GetHashCode()
        {
            var m = Mantissa;
            // very simple (but fast) hash implementation, will have more collisions but Euquals will sort them out
            int res = 13;
            void add32i(int a) { res = res * 17 + a; }
            void add64u(ulong a)
            {
                add32i((int)(a & 0xFFFFFFFF));
                add32i((int)(a >> 32));
            }
            // add all parts of aI number only if its not special value (in which case, only sign )
            add32i((int)specialValue);
            add32i(Sign);
            if (specialValue == SpecialFloatValue.None)
            {
                add64u((ulong)Exponent);
                add32i(Precision);
                if (m != null)
                    foreach (var u in m)
                        add64u(u);
            }
            return res;
        }

        // Equals will accept null==null and 
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is BinaryFloatNumber)) return false;
            return CompareTo(this, obj as BinaryFloatNumber).cmp == 0;
        }
        #endregion

        #region OPERATORS
        public static BinaryFloatNumber operator ^(BinaryFloatNumber a, BinaryFloatNumber b) => BitwiseOperation(a, b, (a, b) => a ^ b, true);
        public static BinaryFloatNumber operator |(BinaryFloatNumber a, BinaryFloatNumber b) => BitwiseOperation(a, b, (a, b) => a | b, true);
        public static BinaryFloatNumber operator &(BinaryFloatNumber a, BinaryFloatNumber b) => BitwiseOperation(a, b, (a, b) => a & b, false);

        #endregion


        #region Array bit operations as static functions : GetArrayBits, CopyArrayBits

        /// <summary>
        /// function to return up to 32 bits from array bit position ( positions are numbered from right to left )
        /// bitPos = position of lowest/rightmost bit we need, where position 0 is LOWEST bit in [0] index
        /// </summary>
        public unsafe static UInt32 GetArrayBits(UInt32* array, int bitPos, int bitCount, int arrayByteSize=-1)
        {
            if (bitPos < 0) throw new NumberException("bitPos in GetArrayBits can't be negative !");
            if (bitCount <= 0) return 0;
            if (bitCount > 32) throw new NumberException("bitCount in GetArrayBits must be up to 32 due to return UInt32 type !");
            int idxL = (bitPos+ bitCount - 1) / 32; // index with highest/leftmost bit
            int idxR = bitPos / 32; // index with lowest/rightmost bit ( idxR <= idxL )
            // check if out of bounds
            int sz32 = arrayByteSize >= 0 ? arrayByteSize / 4 : int.MaxValue;
            // get 64bit pair
            ulong X = 0;
            if (idxL >= 0 && idxL < sz32)
                X = (ulong)*(array+idxL) << 32;
            if (idxR >= 0 && idxR < sz32 && idxR < idxL)
                X |= *(array + idxR);
            // get bit index inside ULONG, 0..63, where 0 is lowest/rightmost bit
            int idx = bitPos % 32;
            // shift to the left to clear all bits to the left of position
            int shR = 64 - idx - bitCount;
            if (shR > 0)
                X <<= shR;
            // shift to the right , to leave 'bitPos' bit as 0th bit in result, and 'bitCount' bits set
            X >>= 64 - bitCount;
            // result is smaller than 32 bits
            return (UInt32)X;
        }

        /// <summary>
        /// Copy 'numBits' from 'source' to 'destination' array, with optional clipping if sourceByteSize and destByteSize are supplied
        /// Start from 'srcStartBit' bit position in source and copy to 'destStartBit' and onwards. Position 0 is rightmost bit, ie lowest bit of index [0]. 
        /// Allows start/end bit positions outside arrays ( for source, padded with zeros ; for destination, ignored )
        /// </summary>
        public unsafe static void ArrayCopyBits(UInt32* source, long srcStartBit, UInt32* destination, long destStartBit, long bitCount , long sourceByteSize=-1, long destByteSize=-1)
        {
            // check if destination start is before array start, and correct dest/source 
            if (destStartBit < 0)
            {
                srcStartBit -= destStartBit; // move source start bit to the left/higher bits by how much destStart was negative
                bitCount += destStartBit; // reduce bit count by that value
                destStartBit = 0;
            }
            // checks if entire destination range is outside of destination array
            if (bitCount <= 0 || (destStartBit + bitCount) <= 0 || (destByteSize >= 0 && destStartBit >= 8 * destByteSize)) return;
            // get Length of arrays ( if passed) as if they were UInt32[], for faster future checks ( they must be divisible by 32 )
            long srcSize32 = sourceByteSize < 0 ? -1 : sourceByteSize % 4 == 0 ? sourceByteSize / 4 : throw new Exception("sourceByteSize must be divisible by 4 (UInt32)");
            long dstSize32 = destByteSize < 0 ? -1 : destByteSize % 4 == 0 ? destByteSize / 4 : throw new Exception("destByteSize must be divisible by 4 (UInt32)"); ;
            // check if destination end bit is outside dest array, but start is within, and correct bitCount
            if (destByteSize >= 0 && (destStartBit + bitCount > dstSize32 * 32))
                bitCount = dstSize32 * 32 -  destStartBit;
            // get start positions and bit ofset for source ( srcStartBit can be negative !)
            long srcP; 
            int srcB;
            if (srcStartBit >= 0)
            {
                srcP = srcStartBit / 32; // current position/index in 32-bit arrays
                srcB = (int)(srcStartBit % 32); // bit position/offset within UINT for start
            }
            else // negative bit indexes: UInt32[-1]= bits -1..-32 ; UInt32[-2]= -33..-64 etc
            {
                srcP = (srcStartBit-31) / 32; // corrected so that -1=>-1, -32=>-1, but -33=>-2
                srcB = (32+(int)(srcStartBit % 32))%32; // corrected for negative bit positions, since -10%32==-10 .. we want -1 => 31, -2=>30... -32=>0 ; -33=>31
            }
            long srcEndBit = srcStartBit + bitCount - 1; // position of last (rightmost, highest) bit to be copied from source
            long srcEnd = srcEndBit >= 0 ? srcEndBit / 32 : (srcEndBit - 31) / 32; // last index for source, possibly corrected for negative 
            // same for destination, but both destStartBit and bitCount are surely >=0
            long dstStart = destStartBit / 32; // start position for destination array
            long dstP = dstStart; // current position begins at start
            long dstEnd = (destStartBit + bitCount - 1) / 32; // final array index for destination
            int dstB = (int)(destStartBit % 32); // bit offsets for destination start
            int dstBend = (int)((destStartBit + bitCount - 1) % 32); // bit offsets for destination end
            int diffB = dstB - srcB;  // how much is destination bit moved within UINT compared to source bit
            // inner func to safely read 32 bits from source index, since these can be outside source  ( pad with zeros for outside range )
            UInt64 readSource(long pos)
            {
                if (pos < 0) return 0; // if before start
                if (pos > srcEnd) return 0; // if after last bitCount, which is known even if sourceByteSize is not passed
                if (srcSize32 >= 0 && pos >= srcSize32) return 0; // past end of source array if passed length
                return *(source + pos); // otherwise read 32 bits and return
            }
            // loop for all destination 32-bit fields
            do
            {
                UInt64 X = readSource(srcP+1)<<32 | readSource(srcP); // "safely" read 64 bits from source  ( only safe if sourceByteSize was supplied )
                if (diffB < 0)
                    X >>= -diffB; // shift right if dest bit index is smaller (to the right) of source one (rightmost is lowest 0 )
                else
                    X <<= diffB; // shift left if dest bit index is larger (to the left) of source one
                // determine destination 32 bit mask ( 1 for bits to keep in destination), for starting/ending UINT
                UInt32 M = 0;
                if (dstP == dstStart && dstB>0)
                    M = 0xFFFFFFFF >> (32- dstB);
                if (dstP == dstEnd && dstBend<31)
                    M |= 0xFFFFFFFF << (dstBend+1);
                // apply mask to destination and inverse to source X
                UInt32 V , x = (~M) & (UInt32)X; // clear bits in X that were '1', leave only those under '0's 
                if (M == 0)
                    V = 0; // no need to get old dest value if we clear all bits there
                else
                {
                    V = *(destination + dstP); // only get current/old dest value if we will be leaving any bit there
                    V &= M; // clear bits that were not '1' in mask, eg M=111000011 will clear middle bits only
                }
                V |= x ; // combine ( bitwise OR) source bits in 'x' with surrounding old dest bits in 'V'
                // write changed value back to destination, dstP is surely >=0 and within dest array 
                *(destination + dstP) = V; 
                // move pointers
                srcP++;
                dstP++;
            } while (dstP<=dstEnd && (dstSize32<0 || dstP<dstSize32));
        }



        /// <summary>
        /// Copy 'numBits' from 'source' to 'destination' array of any type, if their byte lengths are divisible by 4 bytes.
        /// Start from 'srcStartBit' position in source and copy to 'destStartBit' and onwards. Position 0 is rightmost bit, ie lowest bit of index [0]. 
        /// Allows start/end bit positions outside arrays ( for source, padded with zeros ; for destination, ignored )
        /// </summary>
        public unsafe static void ArrayCopyBits<TSource, TDest>(TSource[] source, int srcStartBit, TDest[] destination, int destStartBit, int bitCount) 
            where TSource : unmanaged
            where TDest   : unmanaged
        {
            fixed (TSource* src = &source[0])
                fixed(TDest* dst = &destination[0])
                    ArrayCopyBits((UInt32*)src, srcStartBit, (UInt32*)dst, destStartBit, bitCount, source.Length * sizeof(TSource), destination.Length * sizeof(TDest));
        }



        /// <summary>
        /// Bit shift UInt64[] array for bitCount to the right. If 'extendSign'=true, fill left with '1's, otherwise '0's. 
        /// </summary>
        public static void ArrayShiftRightBits(UInt64[] array, long bitCount, bool extendSign = false) 
        {
            if (bitCount == 0 || array==null) return;
            if (bitCount < 0) throw new Exception("Can not ArrayShiftRightBits in negative direction");
            UInt64 carry = extendSign ? 0xFFFFFFFFFFFFFFFFul : 0;
            // if too big shift, fill entire array with carry and return
            if (bitCount >= array.Length * 64L)
            {
                for (int i = 0; i < array.Length; i++)
                    array[i] = carry;
                return;
            }
            // displacements
            int deltaLeft = (int)(bitCount / 64);
            int shiftWord = (int)(bitCount % 64);
            // safely get word at position-displacement to the left (higher index) of this 'p'
            UInt64 getLeft(int p)
            {
                p += deltaLeft;
                return p >= array.Length ? carry : array[p];
            }
            // start from lowest word
            for (int pos = 0; pos < array.Length; pos++)
            {
                UInt64 X1 = getLeft(pos + 1), X0 = getLeft(pos); // get two 64bit words from the left
                UInt64 X = shiftWord == 0 ? X0 : (X0 >> shiftWord) | (X1 << (64 - shiftWord)); // shift them to right and combine
                array[pos] = X; // write result to array
            }
        }


        /// <summary>
        /// Bit shift UInt64[] array for bitCount to the left.
        /// </summary>
        public static void ArrayShiftLeftBits(UInt64[] array, long bitCount)
        {
            if (bitCount == 0 || array == null) return;
            if (bitCount < 0) throw new Exception("Can not ArrayShiftLeftBits in negative direction");
            // if too big shift, fill entire array with zeros and return
            if (bitCount >= array.Length * 64L)
            {
                for (int i = 0; i < array.Length; i++)
                    array[i] = 0;
                return;
            }
            // displacements
            int deltaLeft = (int)(bitCount / 64);
            int shiftWord = (int)(bitCount % 64);
            // safely get word at position-displacement to the right (lower index) of this 'p'
            UInt64 getRight(int p)
            {
                p -= deltaLeft;
                return p <0 ? 0 : array[p];
            }
            // start from highest/leftmost word
            for (int pos = array.Length-1; pos>=0; pos--)
            {
                UInt64 X1 = getRight(pos), X0 = getRight(pos-1); // get two 64bit words from the right
                UInt64 X = shiftWord == 0 ? X1 : (X1 << shiftWord) | (X0 >> (64 - shiftWord)); // shift them to left and combine
                array[pos] = X; // write result to array
            }
        }


        /// <summary>
        /// Inverse all bits in array ( binary complement, ~ )
        /// </summary>
        public static void ArrayInverseBits(UInt64[] array)
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = ~array[i];
        }

        /// <summary>
        /// A= A + value : add integer value to array, return carry overflow if any 
        /// </summary>
        public static bool ArrayAdd(UInt64[] A, UInt64 value)
        {
            if (A == null || A.Length == 0) return false;
            // add first element
            UInt64 nv = A[0] + value;
            bool carry = (nv < value);
            A[0] = nv;
            // propagate carry through higher indexes
            for (int i = 1; carry && i < A.Length; i++)
            {
                nv = A[i] + 1;
                A[i] = nv;
                carry = nv == 0;
            }
            return carry;
        }

        /// <summary>
        /// A= A + B :  add two arrays ( must be same size), return carry overflow if any 
        /// </summary>
        public static bool ArrayAdd(UInt64[] A, UInt64[] B)
        {
            if (A == null || B == null || A.Length != B.Length) throw new Exception("ArrayAdd invalid array lengths !");
            bool carry = false;
            for (int i = 0; i < A.Length; i++)
            {
                UInt64 nv = A[i] + B[i];
                bool nc = (nv < A[i]); // new carry
                if (carry)
                {
                    nv++;
                    if (nv == 0) nc = true;
                }
                A[i] = nv;
                carry = nc;
            }
            return carry;
        }


        #endregion

        #region instance methods
        /// <summary>
        /// Bit shift Mantissa for bitCount to the right. If 'extendSign' then fill left with '1's for negative, otherwise '0's. 
        /// Does not change exponent, so array may remain completely zeros - and it will not be marked as special zero .
        /// Return this same (changed) instance.
        /// </summary>
        /// <param name="extendSign"></param>
        private BinaryFloatNumber ShiftRight(long bitCount, bool extendSign = false)
        {
            if (bitCount < 0)
                ArrayShiftLeftBits(Mantissa, -bitCount);
            else
                ArrayShiftRightBits(Mantissa, bitCount, extendSign && (Sign < 0));
            return this;
        }

        /// <summary>
        /// Bit shift Mantissa for bitCount to the left. If negative 'bitCount' it shift to the right ( and use 'extendSign' )
        /// Does not change exponent, so array may remain completely zeros - and it will not be marked as special zero .
        /// Return this same (changed) instance.
        /// </summary>
        /// <param name="extendSign"></param>
        private BinaryFloatNumber ShiftLeft(long bitCount, bool extendSign = false)
        {
            if (bitCount < 0)
                ArrayShiftRightBits(Mantissa, -bitCount, extendSign && (Sign < 0));
            else
                ArrayShiftLeftBits(Mantissa, bitCount);
            return this;
        }


        #endregion

        #region Bitwise Operations


        /// <summary>
        /// Allign two numbers to same (larger) exponent and size. If one number is too small to fit, returns it as null ( eg. 64 bits a=2^100, b=2^4 => return a=a, b=null )
        /// IF any is special value or undefined, it is returned as-is. Does not change inputs.
        /// </summary>
        public static (BinaryFloatNumber? aN, BinaryFloatNumber? bN) AllignNumbers(BinaryFloatNumber? aI, BinaryFloatNumber? bI)
        {
            // if any is undefined or special value, return as-is
            if (aI == null || bI == null || aI.Mantissa==null || bI.Mantissa==null || aI.specialValue != SpecialFloatValue.None || bI.specialValue != SpecialFloatValue.None)
                return (aI, bI);
            // if they are exactly same exponent and size, return their copies as -is
            long eDiff = aI.Exponent - bI.Exponent;
            if (eDiff == 0 && aI.Mantissa.Length == bI.Mantissa.Length) return (new BinaryFloatNumber(aI), new BinaryFloatNumber(bI));
            // determine if smaller is too small to fit, based on exponents
            int sz = Math.Max(aI.Mantissa.Length, bI.Mantissa.Length), szb = sz * 64;
            if (eDiff >= szb) return (aI, null); // b is too small to fit
            if (-eDiff >= szb) return (null, bI); // a is too small to fit
            // create clones (to avoid changing originals) with same size mantissas ( larger mantissa)
            var a = new BinaryFloatNumber(aI,sz);
            var b = new BinaryFloatNumber(bI,sz);
            // if a is smaller exp, shift it to the right and set same exponent as b
            if (eDiff < 0) 
            {
                a.ShiftRight(-eDiff); 
                a.Exponent = b.Exponent;  
            }
            // if b is smaller exp, shift it to the right and set same exponent as a
            if (eDiff > 0)
            {
                b.ShiftRight(eDiff);
                b.Exponent = a.Exponent;
            }
            // return
            return (a, b);
        }


        /// <summary>
        /// return a= a 'op' b ; perform bitwise 'op' on two numbers.
        /// 'zeroSame'=true if X op Zero == X for any X, and false if it is zero for any X.
        /// Handle special cases like Inf/NaN. Optionally do complement for negative numbers.
        /// </summary>
        public static BinaryFloatNumber BitwiseOperation(BinaryFloatNumber? aI, BinaryFloatNumber? bI, Func<UInt64, UInt64, UInt64> op, bool zeroSame, bool negComplement = true)
        {
            // if any is NaN or undefined, return NaN
            if (aI == null || bI == null || aI.isNaN || bI.isNaN)
                return new BinaryFloatNumber(SpecialFloatValue.NaN);
            // if both are same special, return that one ( xor may need spec consideration, as +inf xor +inf == ??? )
            if (aI.specialValue != SpecialFloatValue.None && aI.specialValue == bI.specialValue)
                return aI;
            // if any is zero, check 'zeroSame'
            if (aI.isZero || bI.isZero)
            {
                if (!zeroSame) return new BinaryFloatNumber(SpecialFloatValue.Zero); // if 'zeroSame'==false, any op with zero result in zero ( eg AND )
                if (aI.isZero) return bI; // if a is zero, return b since 'zeroSame'==true and any op with zero result in other number ( eg OR, XOR )
                return aI; // otherwise b must be zero, so return a
            }
            // if opposite infinites ( we already checked same infinites), return undefined
            if (aI.isInfinity && bI.isInfinity) return new BinaryFloatNumber(SpecialFloatValue.NaN);
            // otherwise if single infinity, return that infinity type
            if (aI.isInfinity) return aI;
            if (bI.isInfinity) return bI;
            // at this point both numbers are not special
            if (aI.specialValue != SpecialFloatValue.None || bI.specialValue != SpecialFloatValue.None)
                throw new Exception("BitwiseOperation invalid special value handling!");
            // allign numbers to same exponent
            (var a, var b) = AllignNumbers(aI, bI);
            // if any returns null, it is too small to fit ( will not change result, ie 1.001 & 0.000000001 in 4 bits), so return other one
            if (a == null) return b;
            if (b == null) return a;
            // apply bitwise operation on each mantissa 64bit field separatelly
            for (int i = 0; i < a.Mantissa.Length; i++)
                a.Mantissa[i] = op(a.Mantissa[i], b.Mantissa[i]);
            // if result has leading zeros, shift to left and decrease exponent
            long leadingZeros = CountLeadingZeros(a.Mantissa);
            if (leadingZeros > 0)
            {
                if (leadingZeros >= a.Mantissa.Length*64)
                    return new BinaryFloatNumber(SpecialFloatValue.Zero); // all zero bits in result means zero
                ArrayShiftLeftBits(a.Mantissa, leadingZeros); // shift left so '1' is at start
                a.Exponent -= leadingZeros; // reduce exponent to match shift
            }
            //handle sign, using same op rules
            long sgnA = a.Sign < 0 ? -1 : 0;
            long sgnB = b.Sign < 0 ? -1 : 0;
            UInt64 sgnOp = op((ulong)sgnA, (ulong)sgnB); // -|- == - ; -|+ == - ; +|+ == + ; -&- == - ; -&+ == + ; +&+ == + ; -^- == + ; -^+ == - ; +^+ == + ;
            a.Sign = (int)(long)sgnOp; 
            // return 'a' as result
            return a;
        }

        #endregion

        #region ToString functions

        public override string? ToString() => ToString(new NumberFormat { Base=2, ShowBasePrefix=true, ThousandsSeparator=",", FractionSeparator="`", ShowPrecision=true });



        /// <summary>
        /// Convert to string in given Base . If base is not binary divisible, it will use base 2
        /// </summary>
        public string? ToString(NumberFormat? Format)
        {
            // check special values
            if (specialValue != SpecialFloatValue.None)
                return specialToString(specialValue);
            string? res = null;
            if (Format == null)
                Format = new NumberFormat(2);
            // check base
            if (isBinaryBase(Format.Base) && Mantissa!=null)
            {
                res = ToStringFromBits(Format.Base, Sign, Exponent-Mantissa.Length*64+1, Precision, Mantissa); // base is 2,4,8,16,32
                Format.SignificantDigits = validDigitsInBase(Precision, Format.Base);
            }
            else
                return null;
            // format result output
            res = FormatNumberString(res, Format);
            // return result
            return res;
        }

        /// <summary>
        /// show mantissa as simple binary string, prefix sign, suffix exponent
        /// </summary>
        /// <returns></returns>
        public string ToStringMantissa()
        {
            string res = Sign < 0 ? "-" : " ";
            if (specialValue != SpecialFloatValue.None)
                return res+" "+specialToString(specialValue);
            if (Mantissa == null)
                return res + " NULL";
            for (int i = Mantissa.Length - 1; i >= 0; i--)
            {
                UInt64 W = Mantissa[i];
                for (int u = 0; u < 8; u++)
                {
                    int v = (int)((W >> 56)&0xFF); // get upper 8 bits
                    W <<= 8; // shift to next 8
                    res+= " "+Convert.ToString(v, 2).PadLeft(8,'0');
                }
            }
            // add exponent
            res += " @" + Exponent;
            return res;
        }




        /// <summary>
        /// convert SpecialFloatValue to string
        /// </summary>
        public static string specialToString(SpecialFloatValue spec) => spec switch
        {
            SpecialFloatValue.NaN => "NaN",
            SpecialFloatValue.PositiveInfinity => "∞",
            SpecialFloatValue.NegativeInfinity => "-∞",
            SpecialFloatValue.NegativeZero => "-0",
            SpecialFloatValue.Zero => "0",
            _ => "?",
        };


        /// <summary>
        /// Convert to string in given (binary divisible) Base from bits array. Highest bit in bits[len-1] is "1.", other bits are fraction.
        /// up to 0.0000xxx or up to 123...'significantDigits' digits will be shown without exponent. Exponent assume entire array is integer, so use negative bias if fractional mantissa.
        /// </summary>
        private static string ToStringFromBits(int Base, int sign, long exp, int significantDigits, params UInt32[] bits)
        {
            if (bits.Length == 0 || Base < 2 || Base > 62) return null;
            // detect bI= log2(Base), n=number of 1s in Base
            (int n, int b) = CountOnes(Base);
            if (n != 1) return null; // base not 2,4,8,16,32
            // convert exponent from 2^exp* 123456 to Base^exp* 1.23456
            long expS = exp + bits.Length * 32 - 1; // exponent corrected from  2^exp* 111010011 to 2^expS* 1.11010011
            long expB = expS >= 0 ? expS / b : (expS + 1) / b - 1;
            int firstBits = expS >= 0 ? 1 + (int)(expS % b) : b + (int)(expS + 1) % b;
            // form mantissa
            string firstDigit = digitLetters[(int)getBits(0, firstBits, bits)].ToString();
            string res = "";
            for (int p = firstBits; p < 32 * bits.Length; p += b)
            {
                res += digitLetters[(int)getBits(p, b, bits)];
            }
            // remove trailing zeros
            int zi = res.Length;
            while (zi > 0 && res[zi - 1] == '0') zi--;
            res = res.SubStr(0, zi);
            // if exponent is larger than result size, it must be used
            if (expB > significantDigits || expB < -4)
                res = firstDigit + "." + res + (Base <= 10 ? "e" : expChar) + expB;
            else
            {
                // otherwise insert dot in middle
                var Num = firstDigit + res;
                int iexp = (int)expB; // if exp was larger than 32bits, it was shown above in exp form
                int delta = iexp - Num.Length + 1;
                // exactly same : '1234' e3 == 1.234e3 = 1234
                if (delta == 0)
                    res = Num;
                // exp is larger, need trailing zeros : '1234' e4 == 1.234e4 = 12340
                else if (delta > 0)
                    res = Num + new string('0', delta);
                // exp is negative so it needs leading zeros : '1234' e-2 == 1.234e-2 == 0.01234
                else if (iexp < 0)
                    res = "0." + new string('0', -iexp - 1) + Num;
                else
                    // otherwise need dot in the middle : '1234' e2 = 1.234e2 = 123.4
                    res = Num.SubStr(0, iexp + 1) + "." + Num.SubStr(iexp + 1);
            }
            // append sign and return
            return (sign < 0 ? "-" : "") + res;
        }

        /// <summary>
        /// Convert to string in given (binary divisible) Base from bits array. Highest bit in bits[len-1] is "1.", other bits are fraction.
        /// </summary>
        public static string ToStringFromBits(int Base, int sign, long exp, int significantDigits, params UInt64[] bits)
        {
            var bits32 = new UInt32[bits.Length * 2];
            for (int i = 0; i < bits.Length; i++)
            {
                var u = bits[i];
                bits32[i * 2] = (UInt32)(u & 0xFFFFFFFFul);
                bits32[i * 2 + 1] = (UInt32)(u >> 32);
            }
            return ToStringFromBits(Base, sign, exp, significantDigits, bits32);
        }




        /// <summary>
        /// Split string representation of aI number into parts and their numerical values. Detect special value (double.naN, Inf ... or -1 if not special value )
        /// </summary>
        /// <param name="CleanNumber">return input number string without base, use 'e' for exp if low base, so '0b110.1@10' returns '110.1e10', ".123" returns '0.123' </param>
        /// <param name="Mantissa">output mantissa string without leading zeros, with guaranteed one zero</param>
        /// <param name="Fraction">output fraction without trailing zeros</param>
        /// <param name="Suffix">output number type suffix, only if valid: 'd' 'q' 'm'</param>
        /// <param name="Sign">output integer sign, either +1 or -1</param>
        /// <param name="Base">output detected base, either from input NumStr or from input Base or decimal default</param>
        /// <param name="Exponent">output integer exponent value, zero if no exp</param>
        /// <param name="RemainderLength">return length of remaining text after parsed valid number</param>
        /// <param name="specValue">output special values as double.NaN, double.PositiveInfinity and even double.Zero ;  return -1 if not special value</param>
        /// <param name="NumberStr">input number as string, without custom separators</param>
        /// <param name="Format">define default base (or -1 to detect) and separators ( implicit are comma, underscore and reverse apostrophe)</param>
        /// <returns>Return NULL ( and set parts ) if all ok, or error string if invalid string</returns>
        public static string? ParseParts(
                                            out string CleanNumber, out string Mantissa, out string Fraction, out string Suffix, // string outputs
                                            out int Sign, out int Base, out long Exponent, out int RemainderLength, out double specValue, // integer or float outputs
                                            string NumberStr, NumberFormat Format// mandatory inputs
                                          )
        {
            // default out values
            Mantissa = Fraction = Suffix = "";
            Sign = +1;
            Base = -1;
            Exponent = 0;
            specValue = -1;
            CleanNumber = "";
            RemainderLength = 0;
            if (string.IsNullOrWhiteSpace(NumberStr))
                return "Parsing empty string";
            int pos = 0;
            string NumStr = NumberStr;
            string separators = "_,`" + Format.ThousandsSeparator + Format.FractionSeparator;  // all separators to ignore, implicit and explicit
            int defBase = Format.Base;
            // inner function to move forward for given number of chars, update pos and current NumStr
            void forward(int n)
            {
                if (n == 0) return;
                pos += n;
                NumStr = NumberStr.SubStr(pos);
            }
            // inner function to extract valid chars from current position and advance that position. Optionally skips separators
            string getValid(string validChars, bool allowSeparators = true)
            {
                if (allowSeparators)
                    validChars += separators;
                string rval = "";
                char ch;
                int i;
                for (i = pos; i < NumberStr.Length && validChars.Contains(ch = NumberStr[i]); i++)
                    // only add to result if it is not separator
                    if (!separators.Contains(ch) || !allowSeparators)
                        rval += ch;
                forward(i - pos);
                return rval;
            }
            // inner function to check if remainder is valid and return null or error accordingly
            string? chkRemainder()
            {
                if (pos >= NumberStr.Length) return null; // no remainders
                char ch = NumberStr[pos];
                if (char.IsLetterOrDigit(ch) || ch == '.')
                    return "Number has trailing letters or digits";
                return null;
            }


            // skip initial spaces, since "  -123" is allowed. But do not skip leading separators, since "_-123" is not allowed
            getValid(" ", false);
            // extract sign, return 0 if multiple signs
            int getSign()
            {
                var s = getValid("+-", false);
                if (s.Length > 1) return 0;
                return s == "-" ? -1 : +1;
            }
            Sign = getSign();
            if (Sign == 0) return "Multiple signs before number";
            // check special values , but allow 'nan!-3' ( which parses 'NaN' and remains '-3') or 'inf!' ( which parses '∞' and remains '!' )
            var NumSmall = NumStr.ToLower();
            double theSpecValue = -1;
            if (defBase <= 0) defBase = 10;
            void chkSpec(string specName, double value, bool onlyLowBases = false)
            {
                if (theSpecValue != -1) return; // if already found one special, skip oters, ie "Infinite" should not trigger on "Inf" also 
                if (onlyLowBases && defBase > 18) return; // if not allowed in high bases, eg "inf" or "nan" - 'i' is limit
                if (NumSmall.StartsWith(specName))
                {
                    forward(specName.Length);
                    theSpecValue = value;
                }
            }
            double infValue = Sign < 0 ? double.NegativeInfinity : double.PositiveInfinity;
            double nanValue = Sign * double.NaN;
            chkSpec("∞", infValue);
            chkSpec("infinity", infValue);
            chkSpec("nan!", nanValue);
            chkSpec("nan", nanValue, true);
            chkSpec("inf", infValue, true);
            specValue = theSpecValue;
            if (specValue != -1) // return as valid special value
            {
                CleanNumber = specValue.ToString();
                RemainderLength = NumberStr.Length - pos;
                return chkRemainder();
            }
            // determine base
            string validDecimalDigits = getBaseDigits(10);
            int rem = 0;
            if (NumStr.StartsWith("0x")) (Base, rem) = (16, 2);
            else if (NumStr.StartsWith("0o")) (Base, rem) = (8, 2);
            else if (NumStr.StartsWith("0b")) (Base, rem) = (2, 2);
            else if (NumStr.StartsWith("0["))
            {
                forward(2); // skip 0[
                var strBase = getValid(validDecimalDigits); // get base (as decimal numbers)
                if (!NumStr.StartsWith("]"))
                    return "Invalid base in string";
                forward(1); // skip ]
                if (strBase != "" && int.TryParse(strBase, out Base))
                {
                    if (Base < 2 || Base > 62)
                        return "Unsupported base value " + Base;
                }
                else
                    return "Invalid base in string";
            }
            forward(rem); // remove 0x or 0b etc
            if (Base < 0) Base = defBase; // if no base was found in string , use default base
            defBase = Base; // set same as Base, since Base cant be used in inner functions
            string validBaseDigits = getBaseDigits(Base);
            // get mantissa and remove leading zeros
            Mantissa = getValid(validBaseDigits);
            int zi = 0;
            while ((zi < Mantissa.Length - 1) && Mantissa[zi] == '0') zi++;
            if (zi > 0) Mantissa = Mantissa.SubStr(zi);
            // check if dot, in which case get Fraction
            if (NumStr.StartsWith("."))
            {
                forward(1); // skip dot
                Fraction = getValid(validBaseDigits);
                // allow '.123', so set mantissa to zero if it was empty but fraction was defined
                if (Mantissa == "" && Fraction != "")
                    Mantissa = "0";
                // remove trailing zeros from fraction
                zi = Fraction.Length - 1;
                while (zi >= 0 && Fraction[zi] == '0') zi--;
                Fraction = Fraction.SubStr(0, zi + 1);
            }
            // check if exponent
            bool isExp = NumStr.StartsWith("@");
            if (!isExp && Base <= 10)
            {
                isExp = NumStr.StartsWith("e");
                if (!isExp) isExp = NumStr.StartsWith("E");
            }
            if (isExp)
            {
                forward(1); // skip exp sign
                long exSign = getSign();
                if (exSign == 0) return "Multiple signs for exponent";
                var sExponent = getValid(validDecimalDigits); // exponent is in base10
                if (sExponent == "" || !long.TryParse(sExponent, out Exponent))
                    return "Exponent is not valid integer decimal number";
                Exponent *= exSign;
            }
            // check for valid Suffix eg '12q'
            zi = pos;
            while (zi < NumberStr.Length && char.IsLetter(NumberStr[zi])) zi++;
            Suffix = NumberStr.SubStr(pos, zi - pos).ToLower();
            forward(zi - pos);
            if (Suffix.Length > 0 && (Suffix.Length != 1 || !"dmq".Contains(Suffix[0])))
                return "Invalid number suffix : " + Suffix;
            // recombine for 'clean' number ( without prefix/suffix )
            CleanNumber = (Sign < 0 ? "-" : "") + Mantissa + (Fraction != "" ? "." + Fraction : "");
            if (Exponent != 0) // attach exponent, use 'e' for decimal/binary even if it was '@'
                CleanNumber += (Base <= 10 ? "e" : "@") + Exponent;
            // check for trailing alfanums, ie invalids like '123text' or '123.23.' or '1.2e3D'
            RemainderLength = NumberStr.Length - pos;
            return chkRemainder();
        }



        /// <summary>
        /// Format string representation of aI number in given base:   round it to SignificantDigits and use exponents only if more than 'decZeros' leading or it wont fit in SignificantDigits.
        /// Assume that exponent is in decimal form with e/E or @ for bases over 10, string have only valid chars ( no commas, 0x etc ) and SignificantDigits is positive
        /// </summary>
        public static string? FormatNumberString(string? NumStr, NumberFormat F)
        {
            // check for special cases in string :  0, +/-∞, NaN, NaN! and invalid letters 
            if (string.IsNullOrWhiteSpace(NumStr)) return NumStr;
            string Mantissa, Fraction, Suffix, CleanNumber;
            int Sign, Base, RemainderLength;
            long exp;
            double specValue;
            // split string number into parts
            string? err = ParseParts(out CleanNumber, out Mantissa, out Fraction, out Suffix, out Sign, out Base, out exp, out RemainderLength, out specValue, NumStr, F);
            // if special value, return clean representation
            if (specValue != -1)
                return CleanNumber;
            // if error in splitting parts, eg digits not belong to base etc, return input string
            if (err != null)
                return NumStr;
            // if we have trailing text after number, consider it invalid here ( input should be just aI number )
            if (RemainderLength > 0)
                return NumStr;
            // set Num to combined mantissa and fraction
            var Num = Mantissa + "." + Fraction;
            // remove all leading zeros, including last one
            int zi = 0;
            while (Num.Length > zi && Num[zi] == '0') zi++;
            if (zi > 0) Num = Num.SubStr(zi);
            // if starts with dot, remove following zeros and reduce exponent
            if (Num.StartsWith("."))
            {
                zi = 1;
                while (Num.Length > zi && Num[zi] == '0') zi++;
                Num = Num.SubStr(zi);
                exp -= zi;
            }
            else
            {
                // check if decimal point is behind whole part, in which case increase exponent and remove dot
                int dotPos = Num.IndexOf('.');
                if (dotPos >= 0)
                {
                    exp += dotPos - 1;
                    Num = Num.Remove(dotPos, 1);
                }
                else
                {
                    // otherwise there was no dot, in which case it is implied at the end of Num
                    exp += Num.Length - 1;
                }
            }
            // check if we had multiple dots
            if (Num.IndexOf('.') >= 0)
                return NumStr;
            // Round Num to SignificantDigits, so 1.234567 sig4 -> 1.235
            if (Num.Length > F.SignificantDigits && F.SignificantDigits>0 )
            {
                int v = baseDigit(Num[F.SignificantDigits], F.Base); // value of first digit after SignificantDigits
                if (v < 0) return NumStr;
                // do we need rounding?
                if (v >= F.Base / 2)
                {
                    bool add = true;
                    // convert string digits to integers
                    var ns = new int[F.SignificantDigits];
                    for (int i = 0; i < ns.Length; i++)
                    {
                        v = baseDigit(Num[i], F.Base);
                        if (v < 0) return NumStr; // invalid digit, so return default
                        ns[i] = v;
                    }
                    // add with carry
                    int p = ns.Length - 1;
                    while (p >= 0 && add)
                    {
                        ns[p] += 1;
                        add = ns[p] == F.Base;
                        if (add) ns[p] = 0;
                        p--;
                    }
                    // check if we had overflow last add
                    Num = "";
                    if (add)
                    {
                        Num = baseDigit(1, F.Base).ToString();
                        exp++;
                    }
                    // recreate Num
                    for (int i = 0; i < ns.Length; i++)
                        Num += baseDigit(ns[i], F.Base);
                }
                else
                    // otherwise just cut
                    Num = Num.SubStr(0, F.SignificantDigits);
            }
            // remove trailing zeros
            zi = Num.Length;
            while (zi > 0 && Num[zi - 1] == '0') zi--;
            Num = Num.SubStr(0, zi);
            // Num now must be in the form 1234567 with 'exp' assuming decimal point is after first digit, ie 1.234567 e99
            string sMantissa="0", sFraction="", sExponent="";
            if (Num != "") // if it was in form '0.000' now it is empty
            {
                // decide if exponent should be used
                if (exp < -F.DecZeros || exp > F.SignificantDigits)
                {
                    sMantissa = Num[0].ToString();
                    sFraction=Num.SubStr(1);
                    sExponent= F.Base == 10 ? "e" : expChar;
                    sExponent += exp.ToString();
                }
                else
                {
                    // otherwise place decimal point at correct location
                    int iexp = (int)exp; // if exp was larger than 32bits, it was shown above in exp form
                    int delta = iexp - Num.Length + 1;
                    // exactly same : '1234' e3 == 1.234e3 = 1234
                    if (delta == 0)
                        sMantissa = Num;
                    // exp is larger, need trailing zeros : '1234' e4 == 1.234e4 = 12340
                    else if (delta > 0)
                        sMantissa = Num + new string('0', delta);
                    // exp is negative so it needs leading zeros : '1234' e-2 == 1.234e-2 == 0.01234
                    else if (exp < 0)
                        sFraction = new string('0', -iexp - 1) + Num;
                    else
                    {
                        // otherwise need dot in the middle : '1234' e2 = 1.234e2 = 123.4
                        sMantissa = Num.SubStr(0, iexp + 1);
                        sFraction= Num.SubStr(iexp + 1);
                    }
                }
            }
            // insert separators if needed
            int groupSize(int formatGroup)
            {
                if (formatGroup > 0) return formatGroup;
                return Base switch
                {
                    2 => 8,
                    16 => 4,
                    _ => 3
                };
            }
            if (!string.IsNullOrWhiteSpace(F.ThousandsSeparator))
            {
                int group = groupSize(F.ThousandsGroup);
                for (int i = 1; i*(group + F.ThousandsSeparator.Length) < sMantissa.Length; i++)
                    sMantissa = sMantissa.Insert(sMantissa.Length- i * (group + F.ThousandsSeparator.Length), F.ThousandsSeparator);
            }
            if (!string.IsNullOrWhiteSpace(F.FractionSeparator))
            {
                int group = groupSize(F.FractionGroup);
                for (int i = 1; i * (group + F.FractionSeparator.Length)-1 < sFraction.Length; i++)
                    sFraction = sFraction.Insert( i * (group + F.FractionSeparator.Length)-1, F.FractionSeparator);
            }
            // combine mantissa, fraction and exponent
            string res = sMantissa;
            if (sFraction != "")
                res += "." + sFraction;
            res += sExponent;
            // append base signature: 0x for hexa, 0b for binary , 0o for octal, [17] for others
            if (F.ShowBasePrefix)
                res = F.Base switch
                {
                    2 => "0b",
                    8 => "0o",
                    10 => "",
                    16 => "0x",
                    _ => "[" + F.Base + "]"
                } + res;
            // append suffix for number type, if present in string and not prevented by format
            if (F.ShowNumberType != false)
                res += Suffix;
            // append sign and return result
            return (Sign < 0 ? "-" : "") + res;
        }




        #endregion


    }

    #endregion


}
