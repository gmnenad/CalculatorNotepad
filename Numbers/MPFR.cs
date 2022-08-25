using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mpfr.Gmp;
using Mpfr.Native;
using thisType = Numbers.MPFR;

namespace Numbers
{

    #region Description
    /*
        MPFR is class that expose high precision Gmp Mpfr numbers as standard C# number class.
        It is wrapper around Mprf_t class, which is itself wrapper around DLL calls to C library.
    
        MPFR number format:
            Pointer : IntPtr (mpfr_ptr) pointing to memory allocated by external DLL, storing value
                Int32 _mpfr_prec : precision in bits
                Int32 _mpfr_sign : sign of mantissa
                Int32 _mpfr_exp  : binary signed exponent
                    - mantissa is always assumed to be '1.xxx' and it has implied division by 2^(64*mpfr_d.Length)
                    - since first whole digit is included in mantissa, exponent is offset by 1
                    - meaning number 1 is written as (1<<63) in mantissa and mpfr_exp=1 
                    - Zero is special exponent: -2^31+1 , Sign=1
                    - NaN is special exponent: -2^31+2 , Sign=1
                    - Infinity is special exponent: -2^31+3 , Sign=+/-1
                IntPtr _mpfr_d    : pointer to the limbs
                    - limbs are UInt64[] array for mantissa bits 
                    - highest bit of mantissa is binary one, ie "1.xxx" ( it is not implied, but actually must be there )
                    - that highest mantissa bit is in the in highest/leftmost/63rd bit of latest element in mpfr_d[] array
                    - when precision is not divisible by 64, lowest bits in mpfr_d[0] are unused and set to zero
                    - actual number, if  'mpfr_d[]' represent integer binary value, is :
                            value=  Sign* mpfr_d[] / 2^(mpfr_d.Length*64) * 2^mpfr_exp
                    - actual number, if  'mantissa' represent bits from mpfr_d[] interpreted as "1.xxxx":
                            value=  Sign* mantissa * 2^ (mpfr_exp-1)
                    - so if precision=100, number -1 is written as mpfr_sign=-1, mpfr_d=[ 0, 1<<63], mpfr_exp=1
             

    */
    #endregion


    [System.Diagnostics.DebuggerDisplay("{ToString(),nq}")] //this attributes makes the debugger display the value without braces or quotes
    public class MPFR : IFloatNumber<MPFR>
    {
        #region Public static constants
        public static MPFR NaN => doFunc0(mpfr_lib.mpfr_set_nan);
        public static MPFR PositiveInfinity => doFunc1(mpfr_lib.mpfr_set_inf, +1);
        public static MPFR NegativeInfinity => doFunc1(mpfr_lib.mpfr_set_inf, -1);
        public static MPFR Zero => doFunc1(mpfr_lib.mpfr_set_zero, 0);
        public static MPFR NegativeZero => doFunc1(mpfr_lib.mpfr_set_zero, -1);
        public static MPFR One => 1L;
        public static MPFR MaxValue => Exp2(1ul << 31 - 2); // can go slightly higher, but that would depend on precision, ie this is 2^2^31, it can go 2^2^31+0.111111...
        public static MPFR MinValue => doFunc1(mpfr_lib.mpfr_neg, MaxValue);
        private static MPFR __PI, __E, __Euler, __Catalan;
        public static MPFR PI { get => __PI; }
        public static MPFR PIx2 { get => __PI * 2; }
        public static MPFR E { get => __E; }
        public static MPFR Euler { get => __Euler; }
        public static MPFR Catalan { get => __Catalan; }

        /// <summary>
        /// Maximal number of bits in mantissa for MPFR, with implied "1." bit. Actual precision can be less, stored in validBits. Used to determine number of correct decimal digits
        /// Change using 'SetDefaultPrecision'
        /// </summary>
        public static int defaultPrecision = 127;


        // initialize in static constructor
        static MPFR()
        {
            SetDefaultPrecision(defaultPrecision);
        }

        // (re)set constants that depend on precision
        private static void setConstants()
        {
            __PI = new MPFR(defaultPrecision + 10);
            mpfr_lib.mpfr_const_pi(__PI.X, DefR);
            __E = new MPFR(defaultPrecision + 10);
            mpfr_lib.mpfr_exp(__E.X, One.X, DefR);
            __Euler = new MPFR(defaultPrecision + 10);
            mpfr_lib.mpfr_const_euler(__Euler.X, DefR);
            __Catalan = new MPFR(defaultPrecision + 10);
            mpfr_lib.mpfr_const_catalan(__Catalan.X, DefR);
        }



        #endregion

        #region Constructors and instance Data field

        public mpfr_t X = new mpfr_t();

        /// <summary>
        /// Exponent, assuming mantissa is in "0."1xxxx form, with "0." implied and all bits as fraction. So 1 has exponent 1
        /// </summary>
        public long Exponent => X._mpfr_exp;
        /// <summary>
        /// Number of bits in mantissa, including fixed highest bit 1
        /// </summary>
        public int Precision => (int)mpfr_lib.mpfr_get_prec(X);
        /// <summary>
        /// Mantissa as array of 64bit ulong, with highest bit fixed at 1 as highest bit in last element of array.
        /// CURRENTLY NULL - NOT AVAILABLE !
        /// </summary>
        public UInt64[] Mantissa
        {
            get
            {
                var limbs = X._mp_d_intptr;
                int sz = (int)X._mp_size;
                var res = new UInt64[sz];
                for (int i = 0; i < sz; i++)
                    res[i] = (ulong)Marshal.ReadInt64(limbs + i*8);
                return res;
            }
        }


        private MPFR(mpfr_t value) => mpfr_lib.mpfr_init_set(X, value, DefR);


        private MPFR() => mpfr_lib.mpfr_init(X);

        private MPFR(int precision) => mpfr_lib.mpfr_init2(X, (mpfr_prec_t)precision);


        public MPFR(MPFR other) => mpfr_lib.mpfr_init_set(X, other.X, DefR);


        ~MPFR()
        {
            mpfr_lib.mpfr_clears(X, null); // frees unmanaged allocated space
        }


        #endregion

        #region Helper functions
        const mpfr_rnd_t DefR = mpfr_rnd_t.MPFR_RNDN;

        public static void SetDefaultPrecision(int bits)
        {
            defaultPrecision = bits;
            mpfr_lib.mpfr_set_default_prec((mpfr_prec_t)bits);
            setConstants();
        }

        /// <summary>
        /// check if this thread has correct defaut/static MPFR precision , and SET it if not
        /// </summary>
        public static bool CheckDefaultPrecision()
        {
            int actualPrecision= (int)mpfr_lib.mpfr_get_default_prec();
            if (actualPrecision == defaultPrecision)
                return true;
            // otherwise set default precision, but no need to again set constants or 'defaultPrecision' - they are already set
            mpfr_lib.mpfr_set_default_prec((mpfr_prec_t)defaultPrecision);
            return false;
        }

        // execute parameterless function 'int f1(out result)', like mpfr_set_nan or PI
        static MPFR doFunc0(Func<mpfr_t, int> f0)
        {
            var res = new MPFR();
            f0(res.X);
            return res;
        }
        static MPFR doFunc0(Action<mpfr_t> f0)
        {
            var res = new MPFR();
            f0(res.X);
            return res;
        }
        // execute parameterless function that has DefRounding as last parameter
        static MPFR doFunc0(Func<mpfr_t, mpfr_rnd_t, int> f0)
        {
            var res = new MPFR();
            f0(res.X, DefR);
            return res;
        }
        // execute one-parameter function 'int f1(out result, in a)' , like mpfr_log2
        static MPFR doFunc1(Func<mpfr_t, mpfr_t, int> f1, MPFR a)
        {
            var res = new MPFR();
            f1(res.X, a.X);
            return res;
        }
        static MPFR doFunc1(Func<mpfr_t, mpfr_t, mpfr_rnd_t, int> f1, MPFR a)
        {
            var res = new MPFR();
            f1(res.X, a.X, DefR);
            return res;
        }
        static MPFR doFunc1(Action<mpfr_t, int> f1, int b) // like mpfr_set_inf(+/-1)
        {
            var res = new MPFR();
            f1(res.X, b);
            return res;
        }
        // execute two-parameter2 function 'int f2(out result, in a, in b)', like mpfr_max
        static MPFR doFunc2(Func<mpfr_t, mpfr_t, mpfr_t, int> f2, MPFR a, MPFR b)
        {
            var res = new MPFR();
            f2(res.X, a.X, b.X);
            return res;
        }
        // execute two-parameter2 function that has DefRounding as last parameter
        static MPFR doFunc2(Func<mpfr_t, mpfr_t, mpfr_t, mpfr_rnd_t, int> f2, MPFR a, MPFR b)
        {
            var res = new MPFR();
            f2(res.X, a.X, b.X, DefR);
            return res;
        }

        /// <summary>
        /// Overwrite exponent of MPFR value.  unsafe, used for bit shifts and casts
        /// </summary>
        private static void _WriteExponent(mpfr_t X, Int32 exp) 
        {
            Marshal.WriteInt32(X.Pointer, /*sizeof(int) + sizeof(int)*/ 8, exp);
        }

        // shift number in place for given number of bits, by changing exponent. Same footprint as mpfr_set_inf
        private static void BitShift(mpfr_t a, int shift)
        {
            // if a is zero or not regular number (ie NaN or Inf), do nothing
            if ((mpfr_lib.mpfr_zero_p(a) != 0) || (mpfr_lib.mpfr_number_p(a) == 0))
                return;
            long exp = shift+(long)a._mpfr_exp;
            if (exp < int.MinValue) // underflow to silent zero
                mpfr_lib.mpfr_set(a, Zero.X, DefR);
            else if (exp > int.MaxValue) // overflow to silent infinity
            {
                if (a._mpfr_sign>=0)
                    mpfr_lib.mpfr_set(a, PositiveInfinity.X, DefR);
                else
                    mpfr_lib.mpfr_set(a, NegativeInfinity.X, DefR);
            }
            else
                _WriteExponent(a, (int)exp);
        }

        #endregion

        #region Casts

        public static explicit operator double(MPFR a) => mpfr_lib.mpfr_get_d(a.X, DefR);

        public static explicit operator ulong(MPFR a) => mpfr_lib.mpfr_get_uj(a.X, DefR);

        public static explicit operator long(MPFR a) => mpfr_lib.mpfr_get_sj(a.X, DefR);

        public static explicit operator Quad(MPFR a) // convert MFPR to Quad, conserving first 63 bits of mantissa and entire exponent 
        {
            // check special values
            if (MPFR.IsNaN(a)) return Quad.NaN;
            if (MPFR.IsPositiveInfinity(a)) return Quad.PositiveInfinity;
            if (MPFR.IsNegativeInfinity(a)) return Quad.NegativeInfinity;
            if (MPFR.IsZero(a)) return Quad.Zero;
            // get MPFR number data. Since limb data is unaccessible, make short 64bit only MPFR that is castable to 64 bit ulong
            var a2 = new mpfr_t();
            mpfr_lib.mpfr_init2(a2, 64); // new 'short' mpfr_t with 64 bit mantissa
            mpfr_lib.mpfr_set(a2, a.X, DefR); // copy old , potentially longer one, into new one 
            int sign = mpfr_lib.mpfr_sgn(a2);
            if (sign < 0) mpfr_lib.mpfr_neg(a2, a2, DefR); // if sign was negative, make it positive to allow cast to ulong
            int exp = a2._mpfr_exp;
            _WriteExponent(a2, 64); // overwrite exponent to 64, so all 64 mantissa bits will be considered as ulong, and a2 will be ulong integer. Assume lower bits were zero
            UInt64 mantissa = mpfr_lib.mpfr_get_uj(a2, DefR) & Quad._notHighestBit; // convert to ulong with highest bit cleared
            // convert to Quad format
            if (sign < 0) mantissa |= Quad._highestBit; // quad use mantissas highest bit for sign, so set if negative
            exp -= 64; // reduce MPFR exponent by 64, since Quad require explicit correction for integer mantissa
            // create new QUAD 
            var res = new Quad(mantissa, exp);
            return res;
        }

        public static explicit operator int(MPFR a) => (int)(long)a;
        public static explicit operator uint(MPFR a) => (uint)(ulong)a;

        public static explicit operator MPFR(string a)
        {
            var res = new MPFR();
            res.X = a; // use implicit base = operator ?
            return res;
        }


        public static implicit operator MPFR(double a) 
        {
            var x = new MPFR();
            mpfr_lib.mpfr_set_d(x.X, a, DefR);
            return x;
        }

        public static implicit operator MPFR(ulong a) 
        {
            var x = new MPFR();
            mpfr_lib.mpfr_set_uj(x.X, a, DefR);
            return x;
        }

        public static implicit operator MPFR(long a)
        {
            var x = new MPFR();
            mpfr_lib.mpfr_set_sj(x.X, a, DefR);
            return x;
        }

        public static implicit operator MPFR(int a) => (long)a;
        public static implicit operator MPFR(uint a) => (ulong)a;

        public static explicit operator MPFR(Quad q) // convert Quad to MFPR, conserving entire 63 bits of mantissa and just half of exponent (32bits)
        {
            // check special values
            if (Quad.IsNaN(q)) return NaN;
            if (Quad.IsPositiveInfinity(q)) return PositiveInfinity;
            if (Quad.IsNegativeInfinity(q)) return NegativeInfinity;
            if (q == Quad.Zero) return Zero;
            int sign = Quad.Sign(q);
            if (q.Exponent <= Int32.MinValue + 64) return Zero; // underflow due to exponent more negative than fit in 32bits (Quad exponent is 64 bit)
            if (q.Exponent >= Int32.MaxValue)  // overflow due to exponent more positive than fit in 32bits (Quad exponent is 64 bit)
                return sign >= 0 ? PositiveInfinity : NegativeInfinity;
            // create new MPFR with exactly 64 bit precision (Quad mantissa is 63 bits + 1 assumed )
            var res = new mpfr_t();
            mpfr_lib.mpfr_init2(res, 64);
            // get mantissa
            UInt64 mantissa = q.SignificandBits | Quad._highestBit; // fix assumed "1", also MPFR must have 1 in highest position, here leftmost bit
            mpfr_lib.mpfr_set_uj(res, mantissa, DefR); // set num as mantissa (so it is larger by 2^64) but  without exponent ( so it is smaller by 2^exp)
            Int32 exp = (Int32)q.Exponent; // Quad exponent had -63 bias, but since we created MPFR from full 64 bit integer, we can leave this bias .. we need to correct MPFR for that 
            BitShift(res, exp); // bitshift actually only change MPFR exponent value by +/-exp
            // negate value if sign was negative
            if (sign < 0)
                mpfr_lib.mpfr_neg(res, res, DefR);
            // create MPFR from already initialized mpfr_t
            return new MPFR(res);
        }


        // CastToTypeOf only has meaning for base Number type, derived types return directly InputVariable in their own type
        static public thisType CastToTypeOf(thisType InputVariable, thisType TargetTypeVariable) => InputVariable;


        /// <summary>
        /// create Quad from common BinaryFloatNumber structure, with rounding on precision reduction
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
            // check exponent for underflow, will be rare since Quad exp is also 64bit
            if (bf.Exponent < int.MinValue+4) // 3 special exponents at bottom
                return bf.Sign < 0 ? thisType.NegativeZero : thisType.Zero;
            // check exponent for overflow, will be rare since Quad exp is also 64bit
            if (bf.Exponent  > int.MaxValue -1)
                return bf.Sign < 0 ? thisType.NegativeInfinity : thisType.PositiveInfinity;
            // MPFR exponent is int32 biased +1
            int exp = (int)(bf.Exponent + 1);
            // check precision with Mantissa boundaries
            int precision =  Math.Min(bf.Mantissa.Length * 64,  Math.Max(1, bf.Precision));
            // mantissa is already in same format, so create new destination MPFR
            var res = new MPFR(precision);
            // directly set values, without calling mpfr_lib
            Marshal.WriteInt32(res.X.Pointer, 4, bf.Sign);  // sign
            Marshal.WriteInt32(res.X.Pointer, 8, exp);  // exponent
            var limbs = res.X._mp_d_intptr; // mantissa goes into limbs
            int sz = (int)res.X._mp_size; // check if we really have same number of limbs as input
            for (int i = 0; i < Math.Min(sz,bf.Mantissa.Length) ; i++)
                Marshal.WriteInt64(limbs + i*8, (long)bf.Mantissa[i]);
            // return packed MPFR
            return res;
        }


        /// <summary>
        /// get common BinaryFloatNumber structure from MPFR
        /// </summary>
        static public unsafe BinaryFloatNumber ToBinary(thisType a)
        {
            var res = new BinaryFloatNumber(IsSpecial(a));
            res.Precision = a.Precision;
            res.Sign = Sign(a);
            if (res.specialValue == SpecialFloatValue.None)
            {
                res.Exponent = a.Exponent -1;
                res.Mantissa = a.Mantissa;
            }
            return res;
        }



        #endregion

        #region Operators

        // unary operations
        public static MPFR operator -(MPFR a) => doFunc1(mpfr_lib.mpfr_neg, a);
        public static MPFR operator ++(MPFR a) => doFunc2(mpfr_lib.mpfr_add, a, One);
        public static MPFR operator --(MPFR a) => doFunc2(mpfr_lib.mpfr_sub, a, One);
        public static MPFR operator <<(MPFR a, int shift) { var r = new MPFR(a.X); BitShift(r.X,  shift); return r; }
        public static MPFR operator >>(MPFR a, int shift) { var r = new MPFR(a.X); BitShift(r.X, -shift); return r; }

        // binary operations

        public static MPFR operator +(MPFR a, MPFR b) => doFunc2(mpfr_lib.mpfr_add, a, b);
        public static MPFR operator -(MPFR a, MPFR b) => doFunc2(mpfr_lib.mpfr_sub, a, b);
        public static MPFR operator *(MPFR a, MPFR b) => doFunc2(mpfr_lib.mpfr_mul, a, b);
        public static MPFR operator /(MPFR a, MPFR b) => doFunc2(mpfr_lib.mpfr_div, a, b);
        public static MPFR operator %(MPFR a, MPFR b) => doFunc2(mpfr_lib.mpfr_fmod, a, b);

        #endregion


        #region Comparisons and HashCode 

        // some equals with different types
        public int CompareTo(MPFR other) => mpfr_lib.mpfr_cmp(X, other.X);
        public int CompareTo(int other) => mpfr_lib.mpfr_cmp_si(X, other);
        public int CompareTo(double other) => mpfr_lib.mpfr_cmp_d(X, other);
        public int CompareTo(uint other) => mpfr_lib.mpfr_cmp_ui(X, other);


        // pure MPFR vs MPFR comparisons
        public static bool operator ==(MPFR a, MPFR b) => mpfr_lib.mpfr_equal_p(a.X, b.X) != 0;

        public static bool operator !=(MPFR a, MPFR b) => mpfr_lib.mpfr_equal_p(a.X, b.X) == 0;

        public static bool operator >(MPFR a, MPFR b) => mpfr_lib.mpfr_greater_p(a.X, b.X) != 0;

        public static bool operator <(MPFR a, MPFR b) => mpfr_lib.mpfr_less_p(a.X, b.X) != 0;

        public static bool operator >=(MPFR a, MPFR b) => mpfr_lib.mpfr_greaterequal_p(a.X, b.X) != 0;

        public static bool operator <=(MPFR a, MPFR b) => mpfr_lib.mpfr_lessequal_p(a.X, b.X) != 0;

        // hash code
        public override int GetHashCode() {
            var m = Mantissa;
            // very simple (but fast) hash implementation, will have more collisions but Euquals will sort them out
            int res = 13;
            void add32i(int a) { res = res * 17 + a; }
            void add64u(ulong a) {
                add32i((int)(a & 0xFFFFFFFF));
                add32i((int)(a >> 32));
            }
            // add all parts of a number: sign, exponent and all limbs
            add32i(X._mpfr_sign);
            add32i(X._mpfr_exp);
            foreach (var u in m)
                add64u(u);
            return res;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            try
            {
                return this == (MPFR)obj;
            }
            catch
            {
                return false;
            }
        }


        #endregion

        #region IsInfinity/IsNaN and other info about number

        // testing abnormal values
        public static bool IsNaN(MPFR a) => mpfr_lib.mpfr_nan_p(a.X) != 0;
        public static bool IsInfinity(MPFR a) => mpfr_lib.mpfr_inf_p(a.X) != 0;
        public static bool IsPositiveInfinity(MPFR a) => IsInfinity(a) && (Sign(a) >= 0);
        public static bool IsNegativeInfinity(MPFR a) => IsInfinity(a) && (Sign(a) < 0);

        // testing normal values
        public static bool IsNegative(MPFR a) => a.X._mpfr_sign<0;
        public static bool IsZero(MPFR a) => mpfr_lib.mpfr_zero_p(a.X) != 0;
        public static bool IsNumber(MPFR a) => mpfr_lib.mpfr_number_p(a.X) != 0; // not NaN or Inf
        public static bool IsRegular(MPFR a) => mpfr_lib.mpfr_regular_p(a.X) != 0; // not NaN or Inf or Zero

        public static SpecialFloatValue IsSpecial(MPFR a)
        {
            if (IsNaN(a)) return SpecialFloatValue.NaN;
            if (IsNegativeInfinity(a)) return SpecialFloatValue.NegativeInfinity;
            if (IsPositiveInfinity(a)) return SpecialFloatValue.PositiveInfinity;
            if (IsZero(a)) return SpecialFloatValue.Zero;
            return SpecialFloatValue.None;
        }

        public static int MantissaBitSize(MPFR a) => a.Precision;


        #endregion

        #region String conversions

        public override string ToString() => ToString(this,10); 


        public static string ToString(MPFR a, int Base, int SignificantDigits = -1, int decZeros = 4)
        {
            // detect special values
            if (IsNaN(a)) return "NaN";
            if (IsPositiveInfinity(a)) return "+Inf";
            if (IsNegativeInfinity(a)) return "-Inf";
            if (IsZero(a)) return "0";
            // get string limited to few less digits from precision, to round a bit
            int validDigits = Number.validDigitsInBase(a, Base) - 2;
            mpfr_exp_t exp1 = 0;
            char_ptr s_ptr = mpfr_lib.mpfr_get_str(char_ptr.Zero, ref exp1, Base, (size_t)validDigits, a.X, mpfr_lib.mpfr_get_default_rounding_mode());
            string s1 = s_ptr.ToString().Trim();
            gmp_lib.free(s_ptr);
            // get full string representation
            mpfr_exp_t exp2 = 0;
            s_ptr = mpfr_lib.mpfr_get_str(char_ptr.Zero, ref exp2, Base, 0, a.X, mpfr_lib.mpfr_get_default_rounding_mode());
            string s2 = s_ptr.ToString().Trim();
            gmp_lib.free(s_ptr);
            // keep shorter one, and remove trailing zeros
            string s = s1.Length < s2.Length ? s1 : s2;
            int exp = s1.Length < s2.Length ? exp1 : exp2;
            int z = s.Length;
            while ((z > 0) && (s[z - 1] == '0')) z--;
            if (z < s.Length)
                s = s.Remove(z);
            // convert return format, where -1.23456 is returned as -12345656e1 and would require adding "0." at start to get "-0.123456e1" 
            string sign = "";
            if (s.StartsWith("-"))
            {
                sign = "-";
                s = s.Substring(1);
            }
            int digits = s.Length;
            // use as many as 'validDigits' for large numbers, or 5 zeros for small.
            //   -eg lose exponents in : 1234e1->1.2345 , .123456e3 -> 123.4567 , .1234e8->12340000 , .1234e-3 -> 0.0001234
            //   -eg keep exponents in : .1234e19->1.234e18 , .1234e-7 -> 1.234e-8
            int wholeDigits = exp > validDigits || exp < -5 ? 1 : exp;
            // correct exponent due to whole digits
            exp -= wholeDigits;
            // place decimal point at correct spot
            if (wholeDigits <= 0)
                s = "0." + new string('0', -wholeDigits) + s;
            else if (wholeDigits >= digits)
                s += new string('0', wholeDigits - digits);
            else
                s = s.Substring(0, wholeDigits) + "." + s.Substring(wholeDigits);
            // append exponent if needed, use ^ if Base >=15, since then letter 'e' can be used as digit
            if (exp != 0)
                s += (Base < 15 ? "e" : "^") + (exp > 0 ? "+" : "") + exp;
            return sign + s;
        }




        #endregion

        #region Rounding and precision functions
        public static MPFR Truncate(MPFR a) => doFunc1(mpfr_lib.mpfr_trunc, a);

        public static MPFR Fraction(MPFR a) => doFunc1(mpfr_lib.mpfr_frac, a);

        public static MPFR Floor(MPFR a) => doFunc1(mpfr_lib.mpfr_floor, a);

        public static MPFR Ceiling(MPFR a) => doFunc1(mpfr_lib.mpfr_ceil, a);



        /// <summary>
        /// round value to closest integer, and midpoint to nearest even number
        /// </summary>
        public static MPFR Round(MPFR a) => doFunc1(mpfr_lib.mpfr_roundeven, a);

        /// <summary>
        /// round value to given number of decimal places
        /// </summary>
        public static MPFR Round(MPFR value, int decimals)
        {
            if (decimals != 0)
            {
                var move10 = Pow(10, decimals);
                return Round(value * move10) / move10;
            }
            else
                return Round(value);
        }

        #endregion

        #region Math functions


        public static MPFR Max(MPFR a, MPFR b) => doFunc2(mpfr_lib.mpfr_max, a, b);

        public static MPFR Min(MPFR a, MPFR b) => doFunc2(mpfr_lib.mpfr_min, a, b);

        public static MPFR Abs(MPFR a) => doFunc1(mpfr_lib.mpfr_abs, a);

        public static int Sign(MPFR a) => mpfr_lib.mpfr_sgn(a.X);

        public static MPFR Gamma(MPFR a) => doFunc1(mpfr_lib.mpfr_gamma, a);

        public static MPFR Factorial(MPFR x) => Gamma(x + One);

        #endregion

        #region Powers and logarithms

        /// <summary>
        /// Calculates the natural log (base e) of a MPFR.
        /// </summary>
        public static MPFR Log(MPFR a) => doFunc1(mpfr_lib.mpfr_log, a);

        /// <summary>
        /// Calculates the log (base 2) of a MPFR
        /// </summary>
        public static MPFR Log2(MPFR a) => doFunc1(mpfr_lib.mpfr_log2, a);

        /// <summary>
        /// Calculates the log (base 10) of a MPFR.            
        /// </summary>
        public static MPFR Log10(MPFR a) => doFunc1(mpfr_lib.mpfr_log10, a);

        /// <summary>
        /// Calculates the log of a MPFR in a given base.            
        /// </summary>
        public static MPFR Log(MPFR num, MPFR Base) => Log2(num) / Log2(Base);

        /// <summary>
        /// Raise e=2.71.. to a given exponent 
        /// </summary>
        public static MPFR Exp(MPFR exp) => doFunc1(mpfr_lib.mpfr_exp, exp);
        /// <summary>
        /// Raise 2 to a given exponent 
        /// </summary>
        public static MPFR Exp2(MPFR exp) => doFunc1(mpfr_lib.mpfr_exp2, exp);
        /// <summary>
        /// Raise 10 to a given exponent 
        /// </summary>
        public static MPFR Exp10(MPFR exp) => doFunc1(mpfr_lib.mpfr_exp10, exp);


        /// <summary>
        /// Raise number to a given exponent, detect 2 and 10 for faster versions..  
        /// </summary>
        public static MPFR Pow(MPFR num, MPFR exp)
        {
            if (num == 2)
                return doFunc1(mpfr_lib.mpfr_exp2, exp);
            else if (num == 10)
                return doFunc1(mpfr_lib.mpfr_exp10, exp);
            else
                return doFunc2(mpfr_lib.mpfr_pow, num, exp);
        }

        /// <summary>
        /// Return square root of a number
        /// </summary>
        public static MPFR Sqrt(MPFR a) => doFunc1(mpfr_lib.mpfr_sqrt, a);

        #endregion

        #region Trigonometric functions
        public static MPFR Sin(MPFR a) => doFunc1(mpfr_lib.mpfr_sin, a);
        public static MPFR Sinh(MPFR a) => doFunc1(mpfr_lib.mpfr_sinh, a);
        public static MPFR Asin(MPFR a) => doFunc1(mpfr_lib.mpfr_asin, a);
        public static MPFR Asinh(MPFR a) => doFunc1(mpfr_lib.mpfr_asinh, a);
        public static MPFR Cos(MPFR a) => doFunc1(mpfr_lib.mpfr_cos, a);
        public static MPFR Cosh(MPFR a) => doFunc1(mpfr_lib.mpfr_cosh, a);
        public static MPFR Acos(MPFR a) => doFunc1(mpfr_lib.mpfr_acos, a);
        public static MPFR Acosh(MPFR a) => doFunc1(mpfr_lib.mpfr_acosh, a);
        public static MPFR Tan(MPFR a) => doFunc1(mpfr_lib.mpfr_tan, a);
        public static MPFR Tanh(MPFR a) => doFunc1(mpfr_lib.mpfr_tanh, a);
        public static MPFR Atan(MPFR a) => doFunc1(mpfr_lib.mpfr_atan, a);
        public static MPFR Atanh(MPFR a) => doFunc1(mpfr_lib.mpfr_atanh, a);
        public static MPFR Atan2(MPFR a, MPFR b) => doFunc2(mpfr_lib.mpfr_atan2, a,b);




        #endregion



        // below this are same for any IFloatNumber implementation, ie Double / Quad / MPFR , and should be copy-pasted among those classes

        #region IFloatNumber statics that use generics

        public static bool isInt(thisType x) => Number.isInt(x);


        //public static thisType Gamma(thisType a) => Number.Gamma(a);
        //public static thisType Factorial(thisType a) => Number.Factorial(a);
        public static thisType DoubleFactorial(thisType a) => Number.DoubleFactorial(a);
        public static thisType nCr(thisType n, thisType r) => Number.nCr(n, r);
        public static thisType nPr(thisType n, thisType r) => Number.nPr(n, r);

        #endregion

        #region Number Override for instance methods using own static methods [ OPTIONAL , commented out ]
        /*
        // operator math
        public  thisType Inc() => this + One;
        public  thisType Dec() => this - One;
        public  thisType Neg() => -this;


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
