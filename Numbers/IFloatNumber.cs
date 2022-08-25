using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Numbers
{

    /*
        PREVIEW features used !
        See more at:
            C# generic math NET6 : https://devblogs.microsoft.com/dotnet/preview-features-in-net-6-generic-math/
            C# generic math NET7 : https://devblogs.microsoft.com/dotnet/dotnet-7-generic-math/
            INumber and other design interfaces : https://github.com/dotnet/designs/tree/main/accepted/2021/statics-in-interfaces
    */


    /// <summary>
    /// Interface that define all operations mandatory for Number classes
    /// </summary>
    public interface IFloatNumber<TSelf> : IComparable<TSelf>
        where TSelf : IFloatNumber<TSelf>
    {

        #region  Constants and properties
        static abstract TSelf NaN { get; }
        static abstract TSelf PositiveInfinity { get; }
        static abstract TSelf NegativeInfinity { get; }
        static abstract TSelf MaxValue { get; }
        static abstract TSelf MinValue { get; }
        static abstract TSelf Zero { get; }
        static abstract TSelf NegativeZero { get; }
        static abstract TSelf One { get; }
        static abstract TSelf PI { get; }
        static abstract TSelf PIx2 { get; }
        static abstract TSelf E { get; }
        static abstract TSelf Euler { get; }
        static abstract TSelf Catalan { get; }
        #endregion


        #region OPERATORS ( static, use instance overrides )
        // unary operations
        static abstract TSelf operator -(TSelf a);
        static abstract TSelf operator ++(TSelf a);
        static abstract TSelf operator --(TSelf a);
        static abstract TSelf operator <<(TSelf a, int shift); // must support negative
        static abstract TSelf operator >>(TSelf a, int shift); // must support negative

        // binary operations

        static abstract TSelf operator +(TSelf a, TSelf b);
        static abstract TSelf operator -(TSelf a, TSelf b);
        static abstract TSelf operator *(TSelf a, TSelf b);
        static abstract TSelf operator /(TSelf a, TSelf b);
        static abstract TSelf operator %(TSelf a, TSelf b);

        // Comparisons
        static abstract bool operator ==(TSelf a, TSelf b);
        static abstract bool operator !=(TSelf a, TSelf b);
        static abstract bool operator <(TSelf a, TSelf b);
        static abstract bool operator >(TSelf a, TSelf b);
        static abstract bool operator >=(TSelf a, TSelf b);
        static abstract bool operator <=(TSelf a, TSelf b);

        #endregion

        #region Casts and class functions
        // casts
        static abstract implicit operator TSelf(double a);
        static abstract implicit operator TSelf(long a);
        static abstract implicit operator TSelf(ulong a);
        static abstract implicit operator TSelf(int a);
        static abstract implicit operator TSelf(uint a);

        static abstract explicit operator double(TSelf a);
        static abstract explicit operator long(TSelf a);
        static abstract explicit operator ulong(TSelf a);
        static abstract explicit operator int(TSelf a);
        static abstract explicit operator uint(TSelf a);

        static abstract TSelf CastToTypeOf(TSelf InputVariable, TSelf TargetTypeVariable);
        static abstract TSelf FromBinary(BinaryFloatNumber bf);
        static abstract BinaryFloatNumber ToBinary(TSelf a);


        #endregion

        #region Math

        // isSpecial and info on number
        static abstract bool IsNaN(TSelf a);
        static abstract bool IsInfinity(TSelf a);
        static abstract bool IsPositiveInfinity(TSelf a);
        static abstract bool IsNegativeInfinity(TSelf a);
        static abstract bool IsNegative(TSelf a);
        static abstract bool IsZero(TSelf a);
        static abstract bool IsNumber(TSelf a);
        static abstract bool IsRegular(TSelf a);
        static abstract SpecialFloatValue IsSpecial(TSelf a);
        static abstract int  MantissaBitSize(TSelf a);

        // Rounding and precision functions
        static abstract TSelf Truncate(TSelf a);
        static abstract TSelf Fraction(TSelf a);
        static abstract TSelf Floor(TSelf a);
        static abstract TSelf Ceiling(TSelf a);
        static abstract TSelf Round(TSelf a);
        static abstract TSelf Round(TSelf a, int decimals);

        //math functions

        static abstract TSelf Max(TSelf a, TSelf b);
        static abstract TSelf Min(TSelf a, TSelf b);
        static abstract TSelf Abs(TSelf a);
        static abstract int Sign(TSelf a);
        static abstract TSelf Gamma(TSelf a);
        static abstract TSelf Factorial(TSelf a);

        // powers and logarithms
        static abstract TSelf Log(TSelf a);
        static abstract TSelf Log2(TSelf a);
        static abstract TSelf Log10(TSelf a);
        static abstract TSelf Log(TSelf num, TSelf Base);
        static abstract TSelf Exp(TSelf a);
        static abstract TSelf Exp2(TSelf a);
        static abstract TSelf Exp10(TSelf a);
        static abstract TSelf Pow(TSelf num, TSelf exp);
        static abstract TSelf Sqrt(TSelf a);

        // Trigonometric functions
        static abstract TSelf Sin(TSelf a);
        static abstract TSelf Sinh(TSelf a);
        static abstract TSelf Asin(TSelf a);
        static abstract TSelf Asinh(TSelf a);
        static abstract TSelf Cos(TSelf a);
        static abstract TSelf Cosh(TSelf a);
        static abstract TSelf Acos(TSelf a);
        static abstract TSelf Acosh(TSelf a);
        static abstract TSelf Tan(TSelf a);
        static abstract TSelf Tanh(TSelf a);
        static abstract TSelf Atan(TSelf a);
        static abstract TSelf Atanh(TSelf a);
        static abstract TSelf Atan2(TSelf a, TSelf b);

        // combinatorics
        static abstract TSelf nCr(TSelf n, TSelf r);
        static abstract TSelf nPr(TSelf n, TSelf r);
        static abstract TSelf DoubleFactorial(TSelf a);


        // calculator functions
        static abstract bool isInt(TSelf a);


        #endregion




    }

}
