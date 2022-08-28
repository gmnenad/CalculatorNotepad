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


}
