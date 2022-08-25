using CalculatorNotepad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using thisType = Numbers.Double;

namespace Numbers
{

    #region Description
    /*
        Double is class that mimic System.Double, but add math functions to match other Numbers classes like Quad and MPFR.
    
        Double number format:
            double : 64-bit internal field with System.Double 
                 1 bit  : mantissa sign
                11 bits : exponent biased +1023 ( so 2^0== 1023 )
                52 bits : mantissa with implied set virtual highest 53rd bit , so "1."xxxxx
            actual float = sign*(2^53+mantissa)*2^(exponent-1023-53)
                example  1.0 has mantissa=0, exponent=1023
    */
    #endregion


    [System.Diagnostics.DebuggerDisplay("{ToString(),nq}")] //this attributes makes the debugger display the value without braces or quotes
    public struct Double : IFloatNumber<Double>
    {

        #region Public constants
        public static thisType NaN => double.NaN;
        public static thisType PositiveInfinity => double.PositiveInfinity;
        public static thisType NegativeInfinity => double.NegativeInfinity;
        public static thisType MaxValue => double.MaxValue; 
        public static thisType MinValue => double.MinValue;
        public static thisType Zero => 0;
        public static thisType NegativeZero => -0;
        public static thisType One => 1;
        public static thisType PI => Math.PI;
        public static thisType PIx2 => Math.PI*2;
        public static thisType E => Math.E;
        public static thisType Euler => 0.57721566490153286060651209008240243104215933593992;
        public static thisType Catalan => 0.915965594177219015054603514932384110774;


        /// <summary>
        /// number of bits in mantissa , with implied "1." bit
        /// </summary>
        public int Precision => 53;

        #endregion

        #region Constructors, Data field and properties

        public readonly double Value;



        public Double(double d) {
            Value = d;
        }

        public Double(Double other)
        {
            Value = other.Value;
        }



        #endregion

        #region helper functions

        // shift number by changing exponent. Unsafe and can overflow
        private unsafe static thisType BitShift(thisType a, int shift)
        {
            // if a is zero or not regular number (ie NaN or Inf), do nothing
            if (IsSpecial(a) != SpecialFloatValue.None || shift==0 )
                return a;
            double d = a.Value;
            ulong bits= *(ulong*)&d;
            long exp= (int)((bits >> 52) & 0x7FF);
            exp += shift;
            if (exp < 0) // silent underflow
                return new thisType(0.0);
            if (exp > 0x7ff) // overflow
                return a.Value < 0 ? thisType.NegativeInfinity : thisType.PositiveInfinity;
            bits &= 0x800FFFFFFFFFFFFFul;
            bits |= (ulong)(exp << 52);
            double d2 = *(double*)&bits;
            return new thisType(d2);
        }
        #endregion

        #region Casts

        //public static implicit operator double(thisType a) => a.Value;  // should cover casts to Quad and MPFR too, since they accept double
        public static explicit operator double(thisType a) => a.Value;  
        public static explicit operator ulong(thisType a) => (ulong)a.Value;
        public static explicit operator long(thisType a) => (long)a.Value;
        public static explicit operator int(thisType a) => (int)a.Value;
        public static explicit operator uint(thisType a) => (uint)a.Value;
        public static explicit operator Quad(thisType a) => (Quad)a.Value;
        public static explicit operator MPFR(thisType a) => (MPFR)a.Value;
        public static implicit operator thisType(double a) => new thisType(a);
        public static implicit operator thisType(ulong a) =>  new thisType(a);
        public static implicit operator thisType(long a) => new thisType(a);
        public static implicit operator thisType(int a) => new thisType(a);
        public static implicit operator thisType(uint a) => new thisType(a);
        public static explicit operator thisType(Quad q) => new thisType((double)q);
        public static explicit operator thisType(MPFR q) => new thisType((double)q);

        // CastToTypeOf only has meaning for base Number type, derived types return directly InputVariable in their own type
        static public thisType CastToTypeOf(thisType InputVariable, thisType TargetTypeVariable) => InputVariable;

        /// <summary>
        /// create double from common BinaryFloatNumber structure, with rounding on precision reduction
        /// </summary>
        static public unsafe thisType FromBinary(BinaryFloatNumber bf)
        {
            // check if input number is special value
            if (bf == null) return thisType.NaN;
            if (bf.Mantissa == null || bf.Mantissa.Length == 0 || bf.specialValue != SpecialFloatValue.None)
            {
                return bf.specialValue switch
                {
                    SpecialFloatValue.NegativeInfinity => thisType.NegativeInfinity,
                    SpecialFloatValue.PositiveInfinity => thisType.PositiveInfinity,
                    SpecialFloatValue.Zero => thisType.Zero,
                    SpecialFloatValue.NegativeZero => thisType.NegativeZero,
                    _ => thisType.NaN  // will return NaN for uninitialized Mantissa also
                };
            }
            // check exponent for underflow
            if (bf.Exponent + 1023 < 0)
                return bf.Sign < 0 ? thisType.NegativeZero : thisType.Zero;
            // check exponent for overflow
            if (bf.Exponent + 1023 >= (1 << 11))
                return bf.Sign < 0 ? thisType.NegativeInfinity : thisType.PositiveInfinity;
            // exponent is biased +1023
            ulong exp = (ulong)(bf.Exponent + 1023);
            // mantissa should not have implied "1" bit at start, so clear it
            ulong b0 = bf.Mantissa[bf.Mantissa.Length-1] & 0x7FFF_FFFF_FFFF_FFFFul;
            // round based on (53+1)th bit  [ optional ]
            ulong b = b0 + 0X400;
            if ((b & 0x8000_0000_0000_0000ul) != 0)
            { // if overflow (was "0.111111..111", now it is 1.0000000....==2 ), change exponent and shift
                b >>= 1;
                if (exp + 1 < (1 << 11)) // only change if we do not cause overflow
                    exp++;
                else // otherwise keep old (un-rounded) number
                    b = b0;
            }
            // shift mantissa to lowest 52 bits
            b >>= 11;
            // insert exponent
            b |= exp<<52;
            // set sign
            if (bf.Sign < 0)
                b |= 0x8000_0000_0000_0000ul;
            // convert to double with unsafe pointers
            double d = *(double*)&b;
            // return from double value
            return new thisType(d);
        }

        /// <summary>
        /// get common BinaryFloatNumber structure from double
        /// </summary>
        static public unsafe BinaryFloatNumber ToBinary(thisType a)
        {
            var res = new BinaryFloatNumber(IsSpecial(a));
            res.Precision = 53;
            ulong b = *(ulong*)&a.Value;
            if ((b & 0x8000_0000_0000_0000ul) != 0)
            {
                res.Sign = -1;
                b &= 0x7FFF_FFFF_FFFF_FFFFul;
            }
            else
                res.Sign = +1;
            if (res.specialValue == SpecialFloatValue.None)
            {
                res.Exponent = (long)((b >> 52) & 0x7ffL) - 1023;
                res.Mantissa = new UInt64[1];
                res.Mantissa[0] = (b << 11) | 0x8000_0000_0000_0000ul;
            }
            return res;
        }



        #endregion

        #region Operators

        // unary operations
        public static thisType operator -(thisType a) => new thisType(-a.Value);
        public static thisType operator ++(thisType a) => new thisType(a.Value+1);
        public static thisType operator --(thisType a) => new thisType(a.Value - 1);
        public static thisType operator <<(thisType a, int shift) => BitShift(a, shift);
        public static thisType operator >>(thisType a, int shift) => BitShift(a, -shift);

        // binary operations

        public static thisType operator +(thisType a, thisType b) => new thisType(a.Value + b.Value);
        public static thisType operator -(thisType a, thisType b) => new thisType(a.Value - b.Value);
        public static thisType operator *(thisType a, thisType b) => new thisType(a.Value * b.Value);
        public static thisType operator /(thisType a, thisType b) => new thisType(a.Value / b.Value);
        public static thisType operator %(thisType a, thisType b) => new thisType(a.Value % b.Value);

        #endregion

        #region Comparisons and HashCode 

        // some instance equals with different types
        public int CompareTo(thisType other) => Value.CompareTo(other.Value);

        // pure MPFR vs MPFR comparisons
        public static bool operator ==(thisType a, thisType b) => a.Value == b.Value;
        public static bool operator !=(thisType a, thisType b) => a.Value != b.Value;
        public static bool operator >(thisType a, thisType b) => a.Value > b.Value;
        public static bool operator <(thisType a, thisType b) => a.Value < b.Value;
        public static bool operator >=(thisType a, thisType b) => a.Value >= b.Value;
        public static bool operator <=(thisType a, thisType b) => a.Value <= b.Value;

        // hash code
        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object? obj) 
        {
            if (obj == null) return false;

            try
            {
                return this == (thisType)obj;
            }
            catch
            {
                return false;
            }
        }


        #endregion

        #region IsInfinity/IsNaN and other number info

        // testing abnormal values
        public static bool IsNaN(thisType a) => double.IsNaN(a.Value);
        public static bool IsInfinity(thisType a) => double.IsInfinity(a.Value);
        public static bool IsPositiveInfinity(thisType a) => double.IsPositiveInfinity(a.Value);
        public static bool IsNegativeInfinity(thisType a) => double.IsNegativeInfinity(a.Value);

        // testing normal values
        public static bool IsNegative(thisType a) => a.Value < 0;
        public static bool IsZero(thisType a) => a.Value == 0;
        public static bool IsNumber(thisType a) => !(double.IsNaN(a.Value) || double.IsInfinity(a.Value)); // not NaN or Inf
        public static bool IsRegular(thisType a) => !(double.IsNaN(a.Value) || double.IsInfinity(a.Value) || (a.Value == 0)); // not NaN or Inf or Zero

        public static SpecialFloatValue IsSpecial(thisType a)
        {
            if (IsNaN(a)) return SpecialFloatValue.NaN;
            if (IsNegativeInfinity(a)) return SpecialFloatValue.NegativeInfinity;
            if (IsPositiveInfinity(a)) return SpecialFloatValue.PositiveInfinity;
            //if (a.Value== double.NegativeZero) return SpecialFloatValue.NegativeZero; // double really does not differentiate between those two !!
            if (IsZero(a)) return SpecialFloatValue.Zero;
            return SpecialFloatValue.None;
        }

        public static int MantissaBitSize(thisType a) => a.Precision;

        #endregion

        #region String conversions

        public override string ToString() => Value.ToString(); //  ToString(this,10);
        


        

        #endregion

        #region Rounding and precision functions
        public static thisType Truncate(thisType a) => new thisType(Math.Truncate(a.Value));
        public static thisType Fraction(thisType a) => new thisType(a.Value- Math.Truncate(a.Value));
        public static thisType Floor(thisType a) => new thisType(Math.Floor(a.Value));
        public static thisType Ceiling(thisType a) => new thisType(Math.Ceiling(a.Value));
        public static thisType Round(thisType a) => new thisType(Math.Round(a.Value));
        public static thisType Round(thisType a, int decimals) => new thisType(Math.Round (a.Value, decimals));


        #endregion

        #region Math functions

        public static thisType Max(thisType a, thisType b) => new thisType(Math.Max(a.Value, b.Value));
        public static thisType Min(thisType a, thisType b) => new thisType(Math.Min(a.Value, b.Value));
        public static thisType Abs(thisType a) => new thisType(Math.Abs(a.Value));
        public static int Sign(thisType a) => Math.Sign(a.Value);

        #endregion

        #region Powers and logarithms

        public static thisType Log(thisType a) => new thisType(Math.Log(a.Value));
        public static thisType Log2(thisType a) => new thisType(Math.Log2(a.Value));
        public static thisType Log10(thisType a) => new thisType(Math.Log10(a.Value));
        public static thisType Log(thisType num, thisType Base) => new thisType(Math.Log(num.Value, Base.Value));
        public static thisType Exp(thisType exp) => new thisType(Math.Exp(exp.Value));
        public static thisType Exp2(thisType exp) => new thisType(Math.Pow(2,exp.Value));
        public static thisType Exp10(thisType exp) => new thisType(Math.Pow(10, exp.Value));
        public static thisType Pow(thisType num, thisType exp) => new thisType(Math.Pow(num.Value, exp.Value));
        public static thisType Sqrt(thisType a) => new thisType(Math.Sqrt(a.Value));

        #endregion

        #region Trigonometric functions
        public static Double Sin(Double a) => new Double(Math.Sin(a.Value));
        public static Double Sinh(Double a) => new Double( Math.Sinh(a.Value));
        public static Double Asin(Double a) => new Double( Math.Asin(a.Value));
        public static Double Asinh(Double a) => new Double( Math.Asinh(a.Value));
        public static Double Cos(Double a) => new Double( Math.Cos(a.Value));
        public static Double Cosh(Double a) => new Double( Math.Cosh(a.Value));
        public static Double Acos(Double a) => new Double( Math.Acos(a.Value));
        public static Double Acosh(Double a) => new Double( Math.Acosh(a.Value));
        public static Double Tan(Double a) => new Double( Math.Tan(a.Value));
        public static Double Tanh(Double a) => new Double( Math.Tanh(a.Value));
        public static Double Atan(Double a) => new Double( Math.Atan(a.Value));
        public static Double Atanh(Double a) => new Double( Math.Atanh(a.Value));
        public static Double Atan2(Double a, Double b) => new Double( Math.Atan2(a.Value, b.Value));

        #endregion



        // below this are same for any IFloatNumber implementation, ie Double / Quad / MPFR , and should be copy-pasted among those classes

        #region IFloatNumber statics that use generics

        public static bool isInt(thisType x) => Number.isInt(x);

        public static thisType Gamma(thisType a) => Number.Gamma(a);
        public static thisType Factorial(thisType a) => Number.Factorial(a);
        public static thisType DoubleFactorial(thisType a) => Number.DoubleFactorial(a);
        public static thisType nCr(thisType n, thisType r) => Number.nCr(n,r);
        public static thisType nPr(thisType n, thisType r) => Number.nPr(n,r);


        #endregion

        #region Number Override for instance methods using own static methods [ OPTIONAL , commented out ]
        /*
        // operator math
        public thisType Inc() => this + One;
        public thisType Dec() => this - One;
        public thisType Neg() => -this;


        // isSpecial
        public  bool IsNaN() => IsNaN(this);
        public  bool IsInfinity() => IsInfinity(this);
        public  bool IsPositiveInfinity() => IsPositiveInfinity(this);
        public  bool IsNegativeInfinity() => IsNegativeInfinity(this);
        public  bool IsNegative() => IsNegative(this);
        public  bool IsZero() => IsZero(this);
        public  bool IsNumber() => IsNumber(this);
        public  bool IsRegular() => IsRegular(this);
        public  SpecialFloatValues IsSpecial() => IsSpecial(this);



        // Rounding and precision functions
        public  thisType Truncate() => Truncate(this);
        public  thisType Fraction() => Fraction(this);
        public  thisType Floor() => Floor(this);
        public  thisType Ceiling() => Ceiling(this);
        public  thisType Round() => Round(this);
        public  thisType Round(int decimals) => Round(this, decimals);

        //math functions

        public  thisType Abs() => Abs(this);
        public  int Sign() => Sign(this);
        public  thisType Gamma() => Gamma(this); 
        public  thisType Factorial() => Factorial(this); 

        // powers and logarithms
        public  thisType Log() => Log(this);
        public  thisType Log2() => Log2(this);
        public  thisType Log10() => Log10(this);
        public  thisType Exp() => Exp(this);
        public  thisType Exp2() => Exp2(this);
        public  thisType Exp10() => Exp10(this);
        public  thisType Sqrt() => Sqrt(this);

        // Trigonometric functions
        public  thisType Sin() => Sin(this);
        public  thisType Sinh() => Sinh(this);
        public  thisType Asin() => Asin(this);
        public  thisType Asinh() => Asinh(this);
        public  thisType Cos() => Cos(this);
        public  thisType Cosh() => Cosh(this);
        public  thisType Acos() => Acos(this);
        public  thisType Acosh() => Acosh(this);
        public  thisType Tan() => Tan(this);
        public  thisType Tanh() => Tanh(this);
        public  thisType Atan() => Atan(this);
        public  thisType Atanh() => Atanh(this);

        // combinatorics
        public  thisType DoubleFactorial() => DoubleFactorial(this);
        */
        #endregion


    }
}
