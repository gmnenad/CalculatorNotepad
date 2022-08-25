



using System;
using System.Collections.Generic;
using System.Text;
using thisType = Numbers.Quad;

namespace Numbers
{

    #region Description

    /*
      
        Quad is class that use 2x64bit values to extend double (primarily larger exponent), with custom math functions.
        QUAD 128-bit float format:
              SignificandBits: (1 leftmost bit: sign) ( 63 rightmost bits: mantissa as INTEGER PART without implied 64th bit as "1" )
              Exponent : 64 bit exponent value, it is 2^exponent value minus 63 due to mantissa being integer
              actual float = sign*(2^64+mantissa)*2^exponent    
              1 has mantissa=0, exponent=-63
      
    */
    #region references
    /*
        Copyright (c) 2011 Jeff Pasternack.  All rights reserved.

        This program is free software: you can redistribute it and/or modify
        it under the terms of the GNU Lesser General Public License as published by
        the Free Software Foundation, either version 3 of the License, or
        (at your option) any later version.

        This program is distributed in the hope that it will be useful,
        but WITHOUT ANY WARRANTY; without even the implied warranty of
        MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
        GNU General Public License for more details.

        You should have received a copy of the GNU General Public License
        along with this program.  If not, see <http://www.gnu.org/licenses/>.

        Modified version :
            - addition of many functions corresponding to Math.counterparts, like : Sqrt, Log(n,base), 
            - correct rounding of x.9999999... numbers when casting to double
            - class inherits Number and IFloatNumber interface
    */




    /// <summary>
    /// Quad is a signed 128-bit floating point number, stored internally as a 64-bit significand (with the most significant bit as the sign bit) and
    /// a 64-bit signed exponent, with a value == significand * 2^exponent.  Quads have both a higher precision (64 vs. 53 effective significand bits)
    /// and a much higher range (64 vs. 11 exponent bits) than doubles, but also support NaN and PositiveInfinity/NegativeInfinity values and can be generally
    /// used as a drop-in replacement for doubles, much like double is a drop-in replacement for float.  Operations are checked and become +/- infinity in the
    /// event of overflow (values larger than ~8E+2776511644261678592) and 0 in the event of underflow (values less than ~4E-2776511644261678592).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Exponents >= long.MaxValue - 64 and exponents &lt;= long.MinValue + 64 are reserved
    /// and constitute overflow and underflow, respectively.  Zero, PositiveInfinity, NegativeInfinity and NaN are
    /// defined by significand bits == 0 and an exponent of long.MinValue + 0, + 1, + 2, and + 3, respectively.
    /// </para>
    /// <para>
    /// Quad multiplication and division operators are slightly imprecise for the sake of efficiency; specifically,
    /// they may assign the wrong least significant bit, such that the precision is effectively only 63 bits rather than 64.
    /// </para>    
    /// <para>
    /// For speed, consider using instance methods (like Multiply and Add) rather
    /// than the operators (like * and +) when possible, as the former are significantly faster (by as much as 50%).
    /// </para>    
    /// </remarks>
    #endregion
    #endregion

    [System.Diagnostics.DebuggerDisplay("{ToString(),nq}")] //this attributes makes the debugger display the value without braces or quotes
    public struct Quad : IFloatNumber<Quad>
    {
        #region Public constants
        // all constants are 'new', to prevent Quad x= Zero; 'change x'

        /// <summary>
        /// 0.  Equivalent to (Quad)0.
        /// </summary>
        public static Quad Zero => new Quad(0UL, long.MinValue); //there is only one zero; all other significands with exponent long.MinValue are invalid.

        /// <summary>
        /// NegativeZero is not supported, so same as Zero
        /// </summary>
        public static Quad NegativeZero => new Quad(0UL, long.MinValue); // Quad does not support NegativeZero, so returning zero

        /// <summary>
        /// 1.  Equivalent to (Quad)1.
        /// </summary>
        //public static readonly Quad One = (Quad)1UL; //used for increment/decrement operators
        public static Quad One => (Quad)1UL; //used for increment/decrement operators

        /// <summary>
        /// Positive infinity.  Equivalent to (Quad)double.PositiveInfinity.
        /// </summary>
        public static Quad PositiveInfinity => new Quad(0UL, infinityExponent);

        /// <summary>
        /// Negative infinity.  Equivalent to (Quad)double.NegativeInfinity.
        /// </summary>
        public static Quad NegativeInfinity => new Quad(0UL, negativeInfinityExponent);

        /// <summary>
        /// The Not-A-Number value.  Equivalent to (Quad)double.NaN.
        /// </summary>
        public static Quad NaN => new Quad(0UL, notANumberExponent);

        /// <summary>
        /// The maximum value representable by a Quad, (2 - 1/(2^63)) * 2^(long.MaxValue-65)
        /// </summary>
        public static Quad MaxValue => new Quad(~_highestBit, exponentUpperBound);

        /// <summary>
        /// The minimum value representable by a Quad, -(2 - 1/(2^63)) * 2^(long.MaxValue-65)
        /// </summary>
        public static Quad MinValue => new Quad(ulong.MaxValue, exponentUpperBound);

        /// <summary>
        /// The smallest positive value greater than zero representable by a Quad, 2^(long.MinValue+65)
        /// </summary>
        public static Quad Epsilon => new Quad(0UL, exponentLowerBound);

        /// <summary>
        /// PI=> 3.1415926535897932384626 at 63 bit precision
        /// </summary>
        public static Quad PI => new Quad(0x490f_daa2_2168_c000ul, -62);

        /// <summary>
        /// 2*PI=> 6.28...
        /// </summary>
        public static Quad PIx2 => 2*PI;

        /// <summary>
        /// E=> 2.7182...  at 63 bit precision
        /// </summary>
        public static Quad E => new Quad(0x2DF85458A2BB4A9Bul, -62);

        /// <summary>
        /// Euler - Mascheroni constant => 0.57721566490153286060651209008240243104215933593992...  at 63 bit precision
        /// </summary>
        public static Quad Euler => new Quad(0x13c467e37db0c7a5ul,-64);

        /// <summary>
        /// Catalan constant => 0.915965594177219015054603514932384110774...  at 63 bit precision
        /// </summary>
        public static Quad Catalan => new Quad(0x6a7cb89f409ae845ul, -64);



        /// <summary>
        /// Maximal number of bits in mantissa for Quad, with implied "1." bit. Actual precision can be less, stored in validBits. Used to determine number of correct decimal digits
        /// </summary>
        public const int mantissaBits = 64; 

        #endregion

        #region Constructors and Public fields
        /// <summary>
        /// The first (most significant) bit of the significand is the sign bit; 0 for positive values, 1 for negative.
        /// The remainder of the bits represent the fractional part (after the binary point) of the significant; there is always an implicit "1"
        /// preceding the binary point, just as in IEEE's double specification.  For "special" values 0, PositiveInfinity, NegativeInfinity, and NaN,
        /// SignificantBits == 0.
        /// </summary>
        public ulong SignificandBits;

        /// <summary>
        /// The value of the Quad == (-1)^[first bit of significant] * 1.[last 63 bits of significand] * 2^exponent.
        //  Exponents >= long.MaxValue - 64 and exponents  <= long.MinValue + 64 are reserved.
        /// Exponents of long.MinValue + 0, + 1, + 2 and + 3 are used to represent 0, PositiveInfinity, NegativeInfinity, and NaN, respectively.
        /// </summary>
        public long Exponent;

        /// <summary>
        /// track how many bits in mantissa are valid for precision, including implied highest "1." bit. Starts with 64, but reduces to 53 if double was used.
        /// Negative values represent special values
        /// </summary>
        public int Precision;

        public Quad() 
        {
        }


        /// <summary>
        /// Creates a new Quad with the given significand bits and exponent.  The significand has a first (most significant) bit
        /// corresponding to the quad's sign (1 for positive, 0 for negative), and the rest of the bits correspond to the fractional
        /// part of the significand value (immediately after the binary point).  A "1" before the binary point is always implied.
        /// </summary>
        /// <param name="significand"></param>
        /// <param name="exponent"></param>
        /// <param name="precision">precision of mantissa, including implied "1." bit.</param>
        public Quad(ulong significandBits, long exponent, int precision = 64)
        {
            this.SignificandBits = significandBits;
            this.Exponent = exponent;
            this.Precision = precision;
        }

        /// <summary>
        /// Creates a new Quad with the given significand value and exponent.
        /// </summary>
        /// <param name="significand"></param>
        /// <param name="exponent"></param>
        public Quad(long significand, long exponent, int precision = 64)
        {
            Precision = precision;
            if (significand == 0) //handle 0
            {
                SignificandBits = 0;
                Exponent = long.MinValue;
                return;
            }

            if (significand < 0)
            {
                if (significand == long.MinValue) //corner case
                {
                    SignificandBits = _highestBit;
                    Exponent = 0;
                    return;
                }

                significand = -significand;
                SignificandBits = _highestBit;
            }
            else
                SignificandBits = 0;

            int shift = nlz((ulong)significand); //we must normalize the value such that the most significant bit is 1
            this.SignificandBits |= ~_highestBit & (((ulong)significand) << shift); //mask out the highest bit--it's implicit
            this.Exponent = exponent - shift;
        }

        /// <summary>
        /// create Quad from other Quad, but also from double,long,int due to casts ( casts are correctly setting validBits )
        /// </summary>
        /// <param name="other"></param>
        public Quad(Quad other)
        {
            SignificandBits = other.SignificandBits;
            Exponent = other.Exponent;
            Precision = other.Precision;
        }



        #endregion

        #region Helper functions and constants

        #region "Special" arithmetic tables for zeros, infinities, and NaN's
        //first index = first argument to the operation; second index = second argument
        //One's are used as placeholders when dividing a finite by a finite; these will not be used as the actual result of division, of course.
        //arguments are in the order: 0, positive infinity, negative infinity, NaN, positive finite, negative finite
        private static readonly Quad[,] specialDivisionTable = new Quad[6, 6]{
            { NaN, Zero, Zero, NaN, Zero, Zero }, // 0 divided by something
            { PositiveInfinity, NaN, NaN, NaN, PositiveInfinity, NegativeInfinity }, // +inf divided by something
            { NegativeInfinity, NaN, NaN, NaN, NegativeInfinity, PositiveInfinity }, // -inf divided by something
            { NaN, NaN, NaN, NaN, NaN, NaN }, // NaN divided by something
            { PositiveInfinity, Zero, Zero, NaN, One, One }, //positive finite divided by something
            { NegativeInfinity, Zero, Zero, NaN, One, One } //negative finite divided by something
        };

        private static readonly Quad[,] specialMultiplicationTable = new Quad[6, 6]{
            { Zero, NaN, NaN, NaN, Zero, Zero }, // 0 * something
            { NaN, PositiveInfinity, NegativeInfinity, NaN, PositiveInfinity, NegativeInfinity }, // +inf * something
            { NaN, NegativeInfinity, PositiveInfinity, NaN, NegativeInfinity, PositiveInfinity }, // -inf * something
            { NaN, NaN, NaN, NaN, NaN, NaN }, // NaN * something
            { Zero, PositiveInfinity, NegativeInfinity, NaN, One, One }, //positive finite * something
            { Zero, NegativeInfinity, PositiveInfinity, NaN, One, One } //negative finite * something
        };

        private static readonly bool[,] specialGreaterThanTable = new bool[6, 6]{
            { false, false, true, false, false, true }, // 0 > something
            { true, false, true, false, true, true }, // +inf > something
            { false, false, false, false, false, false }, // -inf > something
            { false, false, false, false, false, false }, // NaN > something
            { true, false, true, false, false, true }, //positive finite > something
            { false, false, true, false, false, false } //negative finite > something
        };

        private static readonly bool[,] specialGreaterEqualThanTable = new bool[6, 6]{
            { true, false, true, false, false, true }, // 0 >= something
            { true, true, true, false, true, true }, // +inf >= something
            { false, false, true, false, false, false }, // -inf >= something
            { false, false, false, false, false, false }, // NaN >= something
            { true, false, true, false, false, true }, //positive finite >= something
            { false, false, true, false, false, false } //negative finite >= something
        };

        private static readonly bool[,] specialLessThanTable = new bool[6, 6]{
            { false, true, false, false, true, false }, // 0 < something
            { false, false, false, false, false, false }, // +inf < something
            { true, true, false, false, true, true }, // -inf < something
            { false, false, false, false, false, false }, // NaN < something
            { false, true, false, false, false, false }, //positive finite < something
            { true, true, false, false, true, false } //negative finite < something
        };

        private static readonly bool[,] specialLessEqualThanTable = new bool[6, 6]{
            { true, true, false, false, true, false }, // 0 < something
            { false, true, false, false, false, false }, // +inf < something
            { true, true, true, false, true, true }, // -inf < something
            { false, false, false, false, false, false }, // NaN < something
            { false, true, false, false, false, false }, //positive finite < something
            { true, true, false, false, true, false } //negative finite < something
        };

        private static readonly Quad[,] specialSubtractionTable = new Quad[6, 6]{
            {Zero, NegativeInfinity, PositiveInfinity, NaN, One, One}, //0 - something
            {PositiveInfinity, NaN, PositiveInfinity, NaN, PositiveInfinity, PositiveInfinity}, //+Infinity - something
            {NegativeInfinity, NegativeInfinity, NaN, NaN,NegativeInfinity,NegativeInfinity}, //-Infinity - something
            { NaN, NaN, NaN, NaN, NaN, NaN }, //NaN - something
            { One, NegativeInfinity, PositiveInfinity, NaN, One, One }, //+finite - something
            { One, NegativeInfinity, PositiveInfinity, NaN, One, One } //-finite - something
        };

        private static readonly Quad[,] specialAdditionTable = new Quad[6, 6]{
            {Zero, PositiveInfinity, NegativeInfinity, NaN, One, One}, //0 + something
            {PositiveInfinity, PositiveInfinity, NaN, NaN, PositiveInfinity, PositiveInfinity}, //+Infinity + something
            {NegativeInfinity, NaN, NegativeInfinity, NaN,NegativeInfinity,NegativeInfinity}, //-Infinity + something
            { NaN, NaN, NaN, NaN, NaN, NaN }, //NaN + something
            { One, PositiveInfinity, NegativeInfinity, NaN, One, One }, //+finite + something
            { One, PositiveInfinity, NegativeInfinity, NaN, One, One } //-finite + something
        };

        private static readonly double[] specialDoubleLogTable = new double[] { double.NegativeInfinity, double.PositiveInfinity, double.NaN, double.NaN };

        private static readonly string[] specialStringTable = new string[] { "0", "Infinity", "-Infinity", "NaN" };
        #endregion

        private const long zeroExponent = long.MinValue;
        private const long infinityExponent = long.MinValue + 1;
        private const long negativeInfinityExponent = long.MinValue + 2;
        private const long notANumberExponent = long.MinValue + 3;

        private const long exponentUpperBound = long.MaxValue - 65; //no exponent should be higher than this
        private const long exponentLowerBound = long.MinValue + 65; //no exponent should be lower than this

        private const double base2to10Multiplier = 0.30102999566398119521373889472449; //Math.Log(2) / Math.Log(10);
        public const ulong _highestBit = 1UL << 63;
        public const ulong _notHighestBit = ~_highestBit;
        private const ulong mantisaOver = 1UL << 52;
        private const ulong secondHighestBit = 1UL << 62;
        private const ulong lowWordMask = 0xffffffff; //lower 32 bits
        private const ulong highWordMask = 0xffffffff00000000; //upper 32 bits

        private const ulong b = 4294967296; // Number base (32 bits).

        private static readonly Quad e19 = (Quad)10000000000000000000UL;
        private static readonly Quad e10 = (Quad)10000000000UL;
        private static readonly Quad e5 = (Quad)100000UL;
        private static readonly Quad e3 = (Quad)1000UL;
        private static readonly Quad e1 = (Quad)10UL;

        private static readonly Quad en19 = One / e19;
        private static readonly Quad en10 = One / e10;
        private static readonly Quad en5 = One / e5;
        private static readonly Quad en3 = One / e3;
        private static readonly Quad en1 = One / e1;

        private static readonly Quad en18 = One / (Quad)1000000000000000000UL;
        private static readonly Quad en9 = One / (Quad)1000000000UL;
        private static readonly Quad en4 = One / (Quad)10000UL;
        private static readonly Quad en2 = One / (Quad)100UL;

        private static readonly Quad half = One / 2;

        private static readonly Quad[] _powers10neg = {
            new Quad(5534023222112865485ul, -67,53), new Quad(2582544170319337226ul, -70,53), new Quad(5888200708328088876ul, -77,53), new Quad(3156028355999026941ul, -90,53), new Quad(7391977910456672603ul, -117,53), new Quad(5742404729413670074ul, -170,53), new Quad(2918308539556030885ul, -276,53), new Quad(6759980540763104417ul, -489,53), new Quad(4625552120148007226ul, -914,53), new Quad(1173731080099058204ul, -1764,53),
            new Quad(2496826692267917502ul, -3465,53), new Quad(5669560580549520868ul, -6867,53), new Quad(2800399803871043550ul, -13670,53), new Quad(6451056870507260009ul, -27277,53), new Quad(4095386037211137303ul, -54490,53), new Quad(392922086741411301ul, -108916,53), new Quad(802582928640082017ul, -217769,53), new Quad(1675003586341795413ul, -435475,53), new Quad(3654194947290004213ul, -870887,53), new Quad(8756140311612226103ul, -1741711,53),
            new Quad(8300742950428765838ul, -3483358,53), new Quad(7424259917079542151ul, -6966652,53), new Quad(5800615316482137521ul, -13933240,53), new Quad(3012944306630780162ul, -27866416,53), new Quad(7010109282078301158ul, -55732769,53), new Quad(5062396480296630437ul, -111465474,53), new Quad(1839999434563602303ul, -222930884,53), new Quad(4047066131042499201ul, -445861705,53), new Quad(323273618568523515ul, -891723346,53), new Quad(657877782196359670ul, -1783446629,53),
            new Quad(1362680178793852104ul, -3566893195,53), new Quad(2926685562974394610ul, -7133786327,53), new Quad(6782043237438681057ul, -14267572591,53), new Quad(4663811364127437171ul, -28535145118,53), new Quad(1231256814822783038ul, -57070290172,53), new Quad(2626877957354557385ul, -114140580281,53), new Quad(6001908301413485290ul, -228281160499,53), new Quad(3343027781595266055ul, -456562320934,53), new Quad(7897741994558727396ul, -913124641805,53), new Quad(6667375105587516082ul, -1826249283546,53),
            new Quad(4465539336124344297ul, -3652498567028,53), new Quad(934859346879850669ul, -7304997133992,53), new Quad(1964473843498478128ul, -14609994267921,53), new Quad(4347358379568962552ul, -29219988535779,53), new Quad(760217657855710295ul, -58439977071494,53), new Quad(1583094708068827831ul, -116879954142925,53), new Quad(3437910956231808947ul, -233759908285787,53), new Quad(8157265585748333253ul, -467519816571511,53), new Quad(7152773420639962347ul, -935039633142958,53), new Quad(5314594065642151781ul, -1870079266285852,53),
            new Quad(2234067690873161560ul, -3740158532571640,53), new Quad(5009267022914006137ul, -7480317065143217,53), new Quad(1757862038721605448ul, -14960634130286370,53), new Quad(3850751108211473628ul, -29921268260572677,53), new Quad(42907981415313065ul, -59842536521145290,53), new Quad(86015574736313140ul, -119685073042290517,53), new Quad(172833315867698994ul, -239370146084580971,53), new Quad(348905290542010555ul, -478740292169161879,53), new Quad(711009105580460822ul, -957480584338323695,53), new Quad(1476828311653270094ul, -1914961168676647327,53),
            new Quad(3190123487419330500ul, -3829922337353294591,53), new Quad(7483627368074414072ul, -7659844674706589119,53),
        };

        private static readonly Quad[] _powers10pos = {
            new Quad(2305843009213693952ul, -60,53), new Quad(5188146770730811392ul, -57,53), new Quad(2035627031571464192ul, -50,53), new Quad(4520523310345224192ul, -37,53), new Quad(1016627963145224192ul, -10,53), new Quad(2145311735306827166ul, 43,53), new Quad(4789612606393394901ul, 149,53), new Quad(1421527563165600992ul, 362,53), new Quad(3062144262578232974ul, 787,53), new Quad(7140915356143358407ul, 1637,53),
            new Quad(5293547632578595863ul, 3338,53), new Quad(2200918116827749861ul, 6740,53), new Quad(4927028163204126619ul, 13543,53), new Quad(1631325411200672026ul, 27150,53), new Quad(3551181154539668405ul, 54363,53), new Quad(8469637481539807050ul, 108789,53), new Quad(7746700544363073151ul, 217642,53), new Quad(6388237375683127741ul, 435348,53), new Quad(3988842946763776261ul, 870760,53), new Quad(239686813847372566ul, 1741584,53),
            new Quad(485602344101580625ul, 3483231,53), new Quad(996771220232304290ul, 6966525,53), new Quad(2101263657988238283ul, 13933113,53), new Quad(4681236072680576555ul, 27866289,53), new Quad(1257508811668695738ul, 55732642,53), new Quad(2686465593245331738ul, 111465347,53), new Quad(6155410452734385865ul, 222930757,53), new Quad(3597695459775473799ul, 445861578,53), new Quad(8598718527888737012ul, 891723219,53), new Quad(7995217369966571043ul, 1783446502,53),
            new Quad(6848831273172478961ul, 3566893068,53), new Quad(4779951162441120006ul, 7133786200,53), new Quad(1406854080035524310ul, 14267572464,53), new Quad(3028297617658138797ul, 28535144991,53), new Quad(7050872211726868421ul, 57070290045,53), new Quad(5134230844401680905ul, 114140580154,53), new Quad(1951540981516698286ul, 228281160372,53), new Quad(4316001683426447443ul, 456562320807,53), new Quad(714134549941932439ul, 913124641678,53), new Quad(1483562126488771880ul, 1826249283419,53),
            new Quad(3205752443877498603ul, 3652498566901,53), new Quad(7525723059846754308ul, 7304997133865,53), new Quad(5984308269078886089ul, 14609994267794,53), new Quad(3313991704050345255ul, 29219988535652,53), new Quad(7818712890835469395ul, 58439977071367,53), new Quad(6521013938366401401ul, 116879954142798,53), new Quad(4214538130177355513ul, 233759908285660,53), new Quad(565750039955837734ul, 467519816571384,53), new Quad(1166202475785015824ul, 935039633142831,53), new Quad(2479859506053218042ul, 1870079266285725,53),
            new Quad(5626471165786186769ul, 3740158532571513,53), new Quad(2730924193519331694ul, 7480317065143090,53), new Quad(6270440615667057312ul, 14960634130286243,53), new Quad(3790210715321992771ul, 29921268260572550,53), new Quad(9137953449209338731ul, 59842536521145163,53), new Quad(9052930396683063414ul, 119685073042290390,53), new Quad(8884063579287031325ul, 239370146084580844,53), new Quad(8550996343959751835ul, 478740292169161752,53), new Quad(7903128448787166294ul, 957480584338323568,53), new Quad(6677375418233055053ul, 1914961168676647200,53),
            new Quad(4482774078698638124ul, 3829922337353294464,53), new Quad(960454474222444746ul, 7659844674706588992,53),
        };



        /// <summary>
        /// calculate 10^long = Pow(10,long), with 63-bit mantissa precision
        /// </summary>
        private static Quad power10(long X)
        {
            Quad onePos(long x, Quad[] powers10)
            {
                if (x > (1l << 61))
                    throw new ArithmeticException("Quad.power10 exponent too large : " + x);
                // process each bit of exponent, this will use multiply up to 61 times, so not very fast, but keep 63 bit precision, unlike Math.Pow(10,long) !
                int i = 0;
                Quad res = One;
                while (x > 0)
                {
                    if ((x & 1) != 0)
                        res.Multiply(powers10[i]);
                    i++;
                    x >>= 1;
                }
                return res;
            }
            return X==0? One : X>0 ? onePos(X, _powers10pos)  : onePos(-X, _powers10neg);
        }

        /// <summary>
        /// find first integer 'exp' such that 10^exp > x , and return quotient so that result*10^exp == x 
        /// </summary>
        private static Quad closestPower10(Quad x, out long exp) 
        {
            (Quad res, long exp) onePos(Quad[] powers10)
            {
                // find closest smaller integer exp, ie if 3.45E7 -> M=1E7 , exp=7
                long exp = 0;
                Quad M = One;
                int i = powers10.Length - 1;
                while (i >= 0)
                {
                    var M2= M* powers10[i];
                    if (M2 <= x)
                    {
                        M = M2;
                        exp |= 1L << i;
                    }
                    i--;
                }
                // increase exp+1, and divide mantissa by 10, so we have 0.345e8 instead of 3.45e7
                return ( x/(M*_powers10pos[0]), exp+1);
            }
            if (x <= Zero)
                throw new ArithmeticException("closestPower10 error - input must be positive : " + x);
            var r=  x<One? onePos(_powers10neg) : onePos(_powers10pos);
            exp = r.exp;
            return r.res;
        }


        /// <summary>
        /// Returns the position of the highest set bit, counting from the most significant bit position (position 0).
        /// Returns 64 if no bit is set.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static int nlz(ulong x)
        {
            //Future work: might be faster with a huge, explicit nested if tree, or use of an 256-element per-byte array.            

            int n;

            if (x == 0) return (64);
            n = 0;
            if (x <= 0x00000000FFFFFFFF) { n = n + 32; x = x << 32; }
            if (x <= 0x0000FFFFFFFFFFFF) { n = n + 16; x = x << 16; }
            if (x <= 0x00FFFFFFFFFFFFFF) { n = n + 8; x = x << 8; }
            if (x <= 0x0FFFFFFFFFFFFFFF) { n = n + 4; x = x << 4; }
            if (x <= 0x3FFFFFFFFFFFFFFF) { n = n + 2; x = x << 2; }
            if (x <= 0x7FFFFFFFFFFFFFFF) { n = n + 1; }
            return n;
        }

        /// <summary>
        /// return exponent, sign and integer mantissa of DOUBLE value = sign * mantissa * 2^exponent
        /// </summary>
        private static unsafe (int sign, int exponent, ulong mantissa) getParts(double d)
        {
            ulong bits = *(ulong*)&d;
            int sign = (bits & _highestBit) != 0 ? -1 : +1;
            int exponent = (int)((bits & _notHighestBit) >> 52) - 1023; // remove exponent bias of 1023
            ulong mantissa = (bits & 0x000F_FFFF_FFFF_FFFFul) | 0x0010_0000_0000_0000ul; // append missing "1" as 53rd bit
            return (sign, exponent, mantissa);
        }

        /// <summary>
        /// return exponent, sign and integer mantissa of QUAD value = sign * mantissa * 2^exponent
        /// </summary>
        private static (int sign, int exponent, ulong mantissa) getParts(Quad q)
        {
            int sign = (q.SignificandBits & _highestBit) != 0 ? -1 : +1;
            int exponent = (int)q.Exponent;
            ulong mantissa = q.SignificandBits & _notHighestBit | 0x8000_0000_0000_0000ul; // append missing "1" as 64th bit
            return (sign, exponent, mantissa);
        }


        /// <summary>
        /// return resulting precision from combinig two Quads, possibly limited  with maxPrecision
        /// </summary>
        private static int valid_bits(Quad a, Quad b, int maxPrecision = 64) => Math.Min(Math.Min(a.Precision, b.Precision), maxPrecision);

        /// <summary>
        /// return precision of a double, check if double is 'max precision', eg integer or 1.5 etc
        /// </summary>
        private static unsafe int valid_bits(double a)
        {
            // if more than half lower bits are zero (28 out of 52), assume this is integer or exactly correct fraction 
            if ((*(ulong*)&a & 0xFFF_FFFFul) == 0)
                return 64;
            else
                return 53; // otherwise assume it is fractional double with only 52+1 bits of precision
        }

        /// <summary>
        /// return resulting precision from combinig double and Quad, check if double is 'max precision', eg integer or 1.5 etc
        /// </summary>
        private static unsafe int valid_bits(double a, Quad b)
        {
            return Math.Min(valid_bits(a), b.Precision);
        }



        /// <summary>
        /// return true if quad can 'fit' into double without resulting in +/-Inf. Oonly exponent is important, mantissa reduction is acceptable. Assume that NaN/Inf itself can fit into double.
        /// </summary>
        public static bool fitDouble(Quad qd) => (qd.Exponent <= exponentUpperBound) && (qd.Exponent >= exponentLowerBound);


        #endregion

        #region Casts

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
            if (bf.Exponent < exponentLowerBound + 63)
                return bf.Sign < 0 ? thisType.NegativeZero : thisType.Zero;
            // check exponent for overflow, will be rare since Quad exp is also 64bit
            if (bf.Exponent - 63 >= exponentUpperBound)
                return bf.Sign < 0 ? thisType.NegativeInfinity : thisType.PositiveInfinity;
            // Quad exponent is biased -63
            long exp = bf.Exponent - 63;
            // mantissa should not have implied "1" bit at start, so clear it
            ulong b = bf.Mantissa[bf.Mantissa.Length - 1] & 0x7FFF_FFFF_FFFF_FFFFul;
            // round based on highest bit in next UInt64 [optional]
            if (bf.Mantissa.Length > 1 && (bf.Mantissa[bf.Mantissa.Length - 2] & 0x8000_0000_0000_0000ul) != 0)
            {
                ulong b2 = b + 1;
                // if overflow (was "0.111111..111", now it is 1.0000000....==2 ), change exponent and shift
                if ((b2 & 0x8000_0000_0000_0000ul) != 0)
                {
                    b2 >>= 1;
                    if (exp + 1 < exponentUpperBound) // only change if we do not cause overflow
                        exp++;
                    else // otherwise keep old (un-rounded) number
                        b2 = b;
                }
                b = b2;
            }
            // set sign bit (mantissa is already at correct position)
            if (bf.Sign < 0)
                b |= 0x8000_0000_0000_0000ul;
            // mantissa precision is smaller of Quad's 64 bits and input BinaryFloatNumber
            int precision = Math.Min(64, bf.Precision);
            // return from ulong mantissa and long exponent
            return new thisType(b, exp, precision);
        }


        /// <summary>
        /// get common BinaryFloatNumber structure from Quad
        /// </summary>
        static public unsafe BinaryFloatNumber ToBinary(thisType a)
        {
            var res = new BinaryFloatNumber(IsSpecial(a));
            res.Precision = a.Precision;
            ulong b = a.SignificandBits;
            res.Sign = (b & 0x8000_0000_0000_0000ul) != 0 ? -1 : +1;
            if (res.specialValue == SpecialFloatValue.None)
            {
                res.Exponent = a.Exponent+63;
                res.Mantissa = new UInt64[1];
                res.Mantissa[0] = b | 0x8000_0000_0000_0000ul;
            }
            return res;
        }



        public static explicit operator Quad(System.Numerics.BigInteger value)
        {
            bool positive = value.Sign >= 0;
            if (!positive)
                value = -value; //don't want 2's complement!

            if (value == System.Numerics.BigInteger.Zero)
                return Zero;

            if (value <= ulong.MaxValue) //easy
            {
                ulong bits = (ulong)value;
                int shift = nlz(bits);
                return new Quad((bits << shift) & ~_highestBit | (positive ? 0 : _highestBit), -shift);
            }
            else //can only keep some of the bits
            {
                byte[] bytes = value.ToByteArray(); //least significant byte is first

                if (bytes[bytes.Length - 1] == 0) //appended when the MSB is set to differentiate from negative values
                    return new Quad((positive ? 0 : _highestBit) | (~_highestBit & ((ulong)bytes[bytes.Length - 2] << 56 | (ulong)bytes[bytes.Length - 3] << 48 | (ulong)bytes[bytes.Length - 4] << 40 | (ulong)bytes[bytes.Length - 5] << 32 | (ulong)bytes[bytes.Length - 6] << 24 | (ulong)bytes[bytes.Length - 7] << 16 | (ulong)bytes[bytes.Length - 8] << 8 | (ulong)bytes[bytes.Length - 9])), (bytes.Length - 9) * 8);
                else //shift bits up
                {
                    ulong bits = (ulong)bytes[bytes.Length - 1] << 56 | (ulong)bytes[bytes.Length - 2] << 48 | (ulong)bytes[bytes.Length - 3] << 40 | (ulong)bytes[bytes.Length - 4] << 32 | (ulong)bytes[bytes.Length - 5] << 24 | (ulong)bytes[bytes.Length - 6] << 16 | (ulong)bytes[bytes.Length - 7] << 8 | (ulong)bytes[bytes.Length - 8];
                    int shift = nlz(bits);
                    bits = (bits << shift) | (((ulong)bytes[bytes.Length - 9]) >> (8 - shift));
                    return new Quad((positive ? 0 : _highestBit) | (~_highestBit & bits), (bytes.Length - 8) * 8 - shift);
                }
            }
        }

        public static explicit operator System.Numerics.BigInteger(Quad value)
        {
            if (value.Exponent == negativeInfinityExponent
                || value.Exponent == infinityExponent)
                throw new InvalidCastException("Cannot cast infinity to BigInteger");
            else if (value.Exponent == notANumberExponent)
                throw new InvalidCastException("Cannot cast NaN to BigInteger");

            if (value.Exponent <= -64) //fractional or zero
                return System.Numerics.BigInteger.Zero;

            if (value.Exponent < 0)
            {
                if ((value.SignificandBits & _highestBit) == _highestBit)
                    return -new System.Numerics.BigInteger((value.SignificandBits) >> ((int)-value.Exponent));
                else
                    return new System.Numerics.BigInteger((value.SignificandBits | _highestBit) >> ((int)-value.Exponent));
            }

            if (value.Exponent > int.MaxValue) //you can presumably get a BigInteger bigger than 2^int.MaxValue bits, but you probably don't want to (it'd be several hundred MB).
                throw new InvalidCastException("BigIntegers do not permit left-shifts by more than int.MaxValue bits.  Since the exponent of the quad is more than this, the conversion cannot be performed.");

            if ((value.SignificandBits & _highestBit) == _highestBit) //negative number?
                return -(new System.Numerics.BigInteger(value.SignificandBits) << (int)value.Exponent);
            else
                return (new System.Numerics.BigInteger(value.SignificandBits | _highestBit) << (int)value.Exponent);
        }

        public static explicit operator ulong(Quad value)
        {
            if (value.Exponent == negativeInfinityExponent
                || value.Exponent == infinityExponent)
                throw new InvalidCastException("Cannot cast infinity to 64-bit unsigned integer");
            else if (value.Exponent == notANumberExponent)
                throw new InvalidCastException("Cannot cast NaN to 64-bit unsigned integer");

            if (value.SignificandBits >= _highestBit) throw new ArgumentOutOfRangeException("Cannot convert negative value to ulong");

            if (value.Exponent > 0)
                throw new InvalidCastException("Value too large to fit in 64-bit unsigned integer");

            if (value.Exponent <= -64) return 0;

            return (_highestBit | value.SignificandBits) >> (int)(-value.Exponent);
        }

        public static explicit operator long(Quad value)
        {
            if (value.Exponent == negativeInfinityExponent
                || value.Exponent == infinityExponent)
                throw new InvalidCastException("Cannot cast infinity to 64-bit signed integer");
            else if (value.Exponent == notANumberExponent)
                throw new InvalidCastException("Cannot cast NaN to 64-bit signed integer");

            if (value.SignificandBits == _highestBit && value.Exponent == 0) //corner case
                return long.MinValue;

            if (value.Exponent >= 0)
                throw new InvalidCastException("Value too large to fit in 64-bit signed integer");

            if (value.Exponent <= -64) return 0;

            if (value.SignificandBits >= _highestBit) //negative
                return -(long)(value.SignificandBits >> (int)(-value.Exponent));
            else
                return (long)((value.SignificandBits | _highestBit) >> (int)(-value.Exponent));
        }

        public static explicit operator int(thisType a) => (int)(long)a;
        public static explicit operator uint(thisType a) => (uint)(ulong)a;


        /// <summary>
        /// Convert Quad to double
        /// </summary>
        public static unsafe explicit operator double(Quad value)
        {
            // QUAD 128-bit float format:
            //      SignificandBits: (1 leftmost bit: sign) ( 63 rightmost bits: mantissa as INTEGER PART without implied 64th bit as "1" )
            //      Exponent : 64 bit exponent value, it is 2^exponent value minus 63 due to mantissa being integer
            //      actual float = sign*(2^64+mantissa)*2^exponent    
            //      1 has mantissa=0, exponent=-63
            // IEEE 64-bit float format:
            //      (1 bit: sign)(11 bits: exponent+1023)(52 bits: mantissa as FRACTION without implied 53rd bit as "1.")
            //      actual float = sign*(2^53+mantissa)*2^(exponent-1023-53)
            //      1 has mantissa=0, exponent=1023

            // handle special Quad values
            switch (value.Exponent)
            {
                case zeroExponent: return 0;
                case infinityExponent: return double.PositiveInfinity;
                case negativeInfinityExponent: return double.NegativeInfinity;
                case notANumberExponent: return double.NaN;
            }
            ulong bits;

            ulong sign1 = value.SignificandBits & _highestBit; // sign bit remains at 64th leftmost location
            long exponent11 = value.Exponent + 1086; // convert Quad exponent to 11-bit IEEE one: +1023 due to IEEE bias, +63 to convert Quad mantisa from 0.xxx fraction to 63-bit integer
            ulong mantissa52 = ((value.SignificandBits & _notHighestBit) + 1) >> 11; // round up last 63rd bit in mantissa, so if it was 0.0011111... it will be 0.01
            if (mantissa52 >= mantisaOver) // if it was 0.1111...111 it is now 1.000..., so increase exponent by 1 and reduce mantissa to 0
            {
                exponent11++;
                mantissa52 = 0;
            }
            // check overflows or underflows
            if (exponent11 >= 0x7ffL)   //too large, exponent can not fit in 11 bits
                return value.SignificandBits >= _highestBit ? double.NegativeInfinity : double.PositiveInfinity;
            if (exponent11 <= 0)        // too small, exponent was too negative
            {
                if (exponent11 > -52)  //can create subnormal double value
                {
                    bits = sign1 | (sign1 >> (int)(-exponent11 + 12));
                    return *((double*)&bits);
                }
                else
                    return 0;
            }
            // construct result IEEE 64-bit float
            bits = sign1 | ((ulong)exponent11 << 52) | mantissa52;
            return *((double*)&bits);
        }

        /// <summary>
        /// Converts a 64-bit unsigned integer into a Quad.  No data can be lost, nor will any exception be thrown, by this cast;
        /// however, it is marked explicit in order to avoid ambiguity with the implicit long-to-Quad cast operator.
        /// NOTE: 'Q2= Q-U' will first cast ulong U to double (!), to prevent use 'Q2= Q- (Quad)U' 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator Quad(ulong value)
        {
            if (value == 0) return Zero;
            int firstSetPosition = nlz(value);
            return new Quad((value << firstSetPosition) & ~_highestBit, -firstSetPosition);
        }

        public static implicit operator Quad(long value)
        {
            return new Quad(value, 0);
        }
        public static implicit operator Quad(int a) => (long)a;
        public static implicit operator Quad(uint a) => (ulong)a;

        public static unsafe implicit operator Quad(double value)
        {
            // Translate the double into sign, exponent and mantissa.
            //long bits = BitConverter.DoubleToInt64Bits(value); // doing an unsafe pointer-conversion to get the bits is faster
            ulong bits = *((ulong*)&value);

            // Note that the shift is sign-extended, hence the test against -1 not 1                
            long exponent = (((long)bits >> 52) & 0x7ffL);
            ulong mantissa = (bits) & 0xfffffffffffffUL;

            if (exponent == 0x7ffL)
            {
                if (mantissa == 0)
                {
                    if (bits >= _highestBit) //sign bit set?
                        return NegativeInfinity;
                    else
                        return PositiveInfinity;
                }
                else
                    return NaN;
            }

            // Subnormal numbers; exponent is effectively one higher,
            // but there's no extra normalisation bit in the mantissa
            if (exponent == 0)
            {
                if (mantissa == 0) return Zero;
                exponent++;

                int firstSetPosition = nlz(mantissa);
                mantissa <<= firstSetPosition;
                exponent -= firstSetPosition;
            }
            else
            {
                //mantissa = ((mantissa << 1)|1)  << 10; // add 1 to expand last double bit to 0.5
                mantissa <<= 11;
                exponent -= 11;
            }

            exponent -= 1075;

            return new Quad((_highestBit & bits) | mantissa, exponent, valid_bits(value));
        }

        #endregion

        #region Struct-modifying instance arithmetic functions

        public unsafe void Multiply(double multiplierDouble)
        {
            Quad multiplier= new Quad();
            Precision = Math.Min(Precision, 53); // we are multiplying with double value, so precision drops to double 52+1 bits
            #region Parse the double
            // Implementation note: the use of goto is generally discouraged,
            // but here the idea is to copy-paste the casting call for double -> Quad
            // to avoid the expense of an additional function call
            // and the use of a single "return" goto target keeps things simple

            // Translate the double into sign, exponent and mantissa.
            //long bits = BitConverter.DoubleToInt64Bits(value); // doing an unsafe pointer-conversion to get the bits is faster
            ulong bits = *((ulong*)&multiplierDouble);

            // Note that the shift is sign-extended, hence the test against -1 not 1                
            long exponent = (((long)bits >> 52) & 0x7ffL);
            ulong mantissa = (bits) & 0xfffffffffffffUL;

            if (exponent == 0x7ffL)
            {
                if (mantissa == 0)
                {
                    if (bits >= _highestBit) //sign bit set?
                        multiplier = NegativeInfinity;
                    else
                        multiplier = PositiveInfinity;

                    goto Parsed;
                }
                else
                {
                    multiplier = NaN;
                    goto Parsed;
                }
            }

            // Subnormal numbers; exponent is effectively one higher,
            // but there's no extra normalisation bit in the mantissa
            if (exponent == 0)
            {
                if (mantissa == 0)
                {
                    multiplier = Zero;
                    goto Parsed;
                }
                exponent++;

                int firstSetPosition = nlz(mantissa);
                mantissa <<= firstSetPosition;
                exponent -= firstSetPosition;
            }
            else
            {
                mantissa = mantissa << 11;
                exponent -= 11;
            }

            exponent -= 1075;

            multiplier.SignificandBits = (_highestBit & bits) | mantissa;
            multiplier.Exponent = exponent;

        Parsed:
            #endregion

            #region Multiply
            if (this.Exponent <= notANumberExponent) //zero/infinity/NaN * something            
            {
                Quad result = specialMultiplicationTable[(int)(this.Exponent - zeroExponent), multiplier.Exponent > notANumberExponent ? (int)(4 + (multiplier.SignificandBits >> 63)) : (int)(multiplier.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }
            else if (multiplier.Exponent <= notANumberExponent) //finite * zero/infinity/NaN            
            {
                Quad result = specialMultiplicationTable[(int)(4 + (this.SignificandBits >> 63)), (int)(multiplier.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }

            ulong high1 = (this.SignificandBits | _highestBit) >> 32; //de-implicitize the 1
            ulong high2 = (multiplier.SignificandBits | _highestBit) >> 32;

            //because the MSB of both significands is 1, the MSB of the result will also be 1, and the product of low bits on both significands is dropped (and thus we can skip its calculation)
            ulong significandBits = high1 * high2 + (((this.SignificandBits & lowWordMask) * high2) >> 32) + ((high1 * (multiplier.SignificandBits & lowWordMask)) >> 32);

            long qd2Exponent;
            long qd1Exponent = this.Exponent;
            if (significandBits < (1UL << 63))
            {
                this.SignificandBits = ((this.SignificandBits ^ multiplier.SignificandBits) & _highestBit) | ((significandBits << 1) & ~_highestBit);
                qd2Exponent = multiplier.Exponent - 1 + 64;
                this.Exponent = this.Exponent + qd2Exponent;
            }
            else
            {
                this.SignificandBits = ((this.SignificandBits ^ multiplier.SignificandBits) & _highestBit) | (significandBits & ~_highestBit);
                qd2Exponent = multiplier.Exponent + 64;
                this.Exponent = this.Exponent + qd2Exponent;
            }

            if (qd2Exponent < 0 && this.Exponent > qd1Exponent) //did the exponent get larger after adding something negative?
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent;
            }
            else if (qd2Exponent > 0 && this.Exponent < qd1Exponent) //did the exponent get smaller when it should have gotten larger?
            {
                this.SignificandBits = 0;
                this.Exponent = this.SignificandBits >= _highestBit ? negativeInfinityExponent : infinityExponent; //overflow
            }
            else if (this.Exponent < exponentLowerBound) //check for underflow
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent;
            }
            else if (this.Exponent > exponentUpperBound) //overflow
            {
                this.SignificandBits = 0;
                this.Exponent = this.SignificandBits >= _highestBit ? negativeInfinityExponent : infinityExponent; //overflow
            }
            #endregion
        }

        public void Multiply(Quad multiplier)
        {
            Precision = valid_bits(this, multiplier, 62);  // multiplication and division have one bit less precision, so 62
            if (this.Exponent <= notANumberExponent) //zero/infinity/NaN * something            
            {
                Quad result = specialMultiplicationTable[(int)(this.Exponent - zeroExponent), multiplier.Exponent > notANumberExponent ? (int)(4 + (multiplier.SignificandBits >> 63)) : (int)(multiplier.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }
            else if (multiplier.Exponent <= notANumberExponent) //finite * zero/infinity/NaN            
            {
                Quad result = specialMultiplicationTable[(int)(4 + (this.SignificandBits >> 63)), (int)(multiplier.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }

            ulong high1 = (this.SignificandBits | _highestBit) >> 32; //de-implicitize the 1
            ulong high2 = (multiplier.SignificandBits | _highestBit) >> 32;

            //because the MSB of both significands is 1, the MSB of the result will also be 1, and the product of low bits on both significands is dropped (and thus we can skip its calculation)
            ulong significandBits = high1 * high2 + (((this.SignificandBits & lowWordMask) * high2) >> 32) + ((high1 * (multiplier.SignificandBits & lowWordMask)) >> 32);

            long qd2Exponent;
            long qd1Exponent = this.Exponent;
            if (significandBits < (1UL << 63))
            {
                this.SignificandBits = ((this.SignificandBits ^ multiplier.SignificandBits) & _highestBit) | ((significandBits << 1) & ~_highestBit);
                qd2Exponent = multiplier.Exponent - 1 + 64;
            }
            else
            {
                this.SignificandBits = ((this.SignificandBits ^ multiplier.SignificandBits) & _highestBit) | (significandBits & ~_highestBit);
                qd2Exponent = multiplier.Exponent + 64;
            }

            this.Exponent = this.Exponent + qd2Exponent;

            if (qd2Exponent < 0 && this.Exponent > qd1Exponent) //did the exponent get larger after adding something negative?
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent;
            }
            else if (qd2Exponent > 0 && this.Exponent < qd1Exponent) //did the exponent get smaller when it should have gotten larger?
            {
                this.SignificandBits = 0;
                this.Exponent = this.SignificandBits >= _highestBit ? negativeInfinityExponent : infinityExponent; //overflow
            }
            else if (this.Exponent < exponentLowerBound) //check for underflow
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent;
            }
            else if (this.Exponent > exponentUpperBound) //overflow
            {
                this.SignificandBits = 0;
                this.Exponent = this.SignificandBits >= _highestBit ? negativeInfinityExponent : infinityExponent; //overflow
            }

        }

        /// <summary>
        /// Multiplies this Quad by a given multiplier, but does not check for underflow or overflow in the result.
        /// This is substantially (~20%) faster than the standard Multiply() method.
        /// </summary>
        /// <param name="multiplier"></param>
        public void MultiplyUnchecked(Quad multiplier)
        {
            Precision = valid_bits(this, multiplier, 62);  // multiplication and division have one bit less precision, so 62
            if (this.Exponent <= notANumberExponent) //zero/infinity/NaN * something            
            {
                Quad result = specialMultiplicationTable[(int)(this.Exponent - zeroExponent), multiplier.Exponent > notANumberExponent ? (int)(4 + (multiplier.SignificandBits >> 63)) : (int)(multiplier.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }
            else if (multiplier.Exponent <= notANumberExponent) //finite * zero/infinity/NaN            
            {
                Quad result = specialMultiplicationTable[(int)(4 + (this.SignificandBits >> 63)), (int)(multiplier.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }

            ulong high1 = (this.SignificandBits | _highestBit) >> 32; //de-implicitize the 1
            ulong high2 = (multiplier.SignificandBits | _highestBit) >> 32;

            //because the MSB of both significands is 1, the MSB of the result will also be 1, and the product of low bits on both significands is dropped (and thus we can skip its calculation)
            ulong significandBits = high1 * high2 + (((this.SignificandBits & lowWordMask) * high2) >> 32) + ((high1 * (multiplier.SignificandBits & lowWordMask)) >> 32);

            long qd2Exponent;
            long qd1Exponent = this.Exponent;
            if (significandBits < (1UL << 63))
            {
                this.SignificandBits = ((this.SignificandBits ^ multiplier.SignificandBits) & _highestBit) | ((significandBits << 1) & ~_highestBit);
                qd2Exponent = multiplier.Exponent - 1 + 64;
                this.Exponent = this.Exponent + qd2Exponent;
            }
            else
            {
                this.SignificandBits = ((this.SignificandBits ^ multiplier.SignificandBits) & _highestBit) | (significandBits & ~_highestBit);
                qd2Exponent = multiplier.Exponent + 64;
                this.Exponent = this.Exponent + qd2Exponent;
            }
        }

        public unsafe void Add(double valueDouble)
        {
            Precision = valid_bits(valueDouble, this);

            #region Parse the double
            // Implementation note: the use of goto is generally discouraged,
            // but here the idea is to copy-paste the casting call for double -> Quad
            // to avoid the expense of an additional function call
            // and the use of a single "return" goto target keeps things simple

            Quad value= new Quad();
            {
                // Translate the double into sign, exponent and mantissa.
                //long bits = BitConverter.DoubleToInt64Bits(value); // doing an unsafe pointer-conversion to get the bits is faster
                ulong bits = *((ulong*)&valueDouble);

                // Note that the shift is sign-extended, hence the test against -1 not 1                
                long exponent = (((long)bits >> 52) & 0x7ffL);
                ulong mantissa = (bits) & 0xfffffffffffffUL;

                if (exponent == 0x7ffL)
                {
                    if (mantissa == 0)
                    {
                        if (bits >= _highestBit) //sign bit set?
                            value = NegativeInfinity;
                        else
                            value = PositiveInfinity;

                        goto Parsed;
                    }
                    else
                    {
                        value = NaN;
                        goto Parsed;
                    }
                }

                // Subnormal numbers; exponent is effectively one higher,
                // but there's no extra normalisation bit in the mantissa
                if (exponent == 0)
                {
                    if (mantissa == 0)
                    {
                        value = Zero;
                        goto Parsed;
                    }
                    exponent++;

                    int firstSetPosition = nlz(mantissa);
                    mantissa <<= firstSetPosition;
                    exponent -= firstSetPosition;
                }
                else
                {
                    mantissa = mantissa << 11;
                    exponent -= 11;
                }

                exponent -= 1075;

                value.SignificandBits = (_highestBit & bits) | mantissa;
                value.Exponent = exponent;
            }
        Parsed:
            #endregion
            #region Addition
            {
                if (this.Exponent <= notANumberExponent) //zero or infinity or NaN + something
                {
                    if (this.Exponent == zeroExponent)
                    {
                        this.SignificandBits = value.SignificandBits;
                        this.Exponent = value.Exponent;
                    }
                    else
                    {
                        Quad result = specialAdditionTable[(int)(this.Exponent - zeroExponent), value.Exponent > notANumberExponent ? (int)(4 + (value.SignificandBits >> 63)) : (int)(value.Exponent - zeroExponent)];
                        this.SignificandBits = result.SignificandBits;
                        this.Exponent = result.Exponent;
                    }

                    return;
                }
                else if (value.Exponent <= notANumberExponent) //finite + (infinity or NaN)
                {
                    if (value.Exponent != zeroExponent)
                    {
                        Quad result = specialAdditionTable[(int)(4 + (this.SignificandBits >> 63)), (int)(value.Exponent - zeroExponent)];
                        this.SignificandBits = result.SignificandBits;
                        this.Exponent = result.Exponent;
                    }
                    return; //if value == 0, no need to change
                }

                if ((this.SignificandBits ^ value.SignificandBits) >= _highestBit) //this and value have different signs--use subtraction instead
                {
                    Subtract(new Quad(value.SignificandBits ^ _highestBit, value.Exponent, Precision));
                    return;
                }

                if (this.Exponent > value.Exponent)
                {
                    if (this.Exponent >= value.Exponent + 64)
                        return; //value too small to make a difference
                    else
                    {
                        ulong bits = (this.SignificandBits | _highestBit) + ((value.SignificandBits | _highestBit) >> (int)(this.Exponent - value.Exponent));

                        if (bits < _highestBit) //this can only happen in an overflow  
                        {
                            this.SignificandBits = (this.SignificandBits & _highestBit) | (bits >> 1);
                            this.Exponent = this.Exponent + 1;
                        }
                        else
                        {
                            this.SignificandBits = (this.SignificandBits & _highestBit) | (bits & ~_highestBit);
                            //this.Exponent = this.Exponent; //exponent stays the same
                        }
                    }
                }
                else if (this.Exponent < value.Exponent)
                {
                    if (value.Exponent >= this.Exponent + 64)
                    {
                        this.SignificandBits = value.SignificandBits; //too small to matter
                        this.Exponent = value.Exponent;
                    }
                    else
                    {
                        ulong bits = (value.SignificandBits | _highestBit) + ((this.SignificandBits | _highestBit) >> (int)(value.Exponent - this.Exponent));

                        if (bits < _highestBit) //this can only happen in an overflow                    
                        {
                            this.SignificandBits = (value.SignificandBits & _highestBit) | (bits >> 1);
                            this.Exponent = value.Exponent + 1;
                        }
                        else
                        {
                            this.SignificandBits = (value.SignificandBits & _highestBit) | (bits & ~_highestBit);
                            this.Exponent = value.Exponent;
                        }
                    }
                }
                else //expDiff == 0
                {
                    //the MSB must have the same sign, so the MSB will become 0, and logical overflow is guaranteed in this situation (so we can shift right and increment the exponent).
                    this.SignificandBits = ((this.SignificandBits + value.SignificandBits) >> 1) | (this.SignificandBits & _highestBit);
                    this.Exponent = this.Exponent + 1;
                }
            }
            #endregion
        }


        public void Add(Quad value)
        {
            Precision = valid_bits(this, value);
            #region Addition

            if (this.Exponent <= notANumberExponent) //zero or infinity or NaN + something
            {
                if (this.Exponent == zeroExponent)
                {
                    this.SignificandBits = value.SignificandBits;
                    this.Exponent = value.Exponent;
                }
                else
                {
                    Quad result = specialAdditionTable[(int)(this.Exponent - zeroExponent), value.Exponent > notANumberExponent ? (int)(4 + (value.SignificandBits >> 63)) : (int)(value.Exponent - zeroExponent)];
                    this.SignificandBits = result.SignificandBits;
                    this.Exponent = result.Exponent;
                }

                return;
            }
            else if (value.Exponent <= notANumberExponent) //finite + (infinity or NaN)
            {
                if (value.Exponent != zeroExponent)
                {
                    Quad result = specialAdditionTable[(int)(4 + (this.SignificandBits >> 63)), (int)(value.Exponent - zeroExponent)];
                    this.SignificandBits = result.SignificandBits;
                    this.Exponent = result.Exponent;
                }
                return; //if value == 0, no need to change
            }

            if ((this.SignificandBits ^ value.SignificandBits) >= _highestBit) //this and value have different signs--use subtraction instead
            {
                Subtract(new Quad(value.SignificandBits ^ _highestBit, value.Exponent));
                return;
            }

            if (this.Exponent > value.Exponent)
            {
                if (this.Exponent >= value.Exponent + 64)
                    return; //value too small to make a difference
                else
                {
                    ulong bits = (this.SignificandBits | _highestBit) + ((value.SignificandBits | _highestBit) >> (int)(this.Exponent - value.Exponent));

                    if (bits < _highestBit) //this can only happen in an overflow  
                    {
                        this.SignificandBits = (this.SignificandBits & _highestBit) | (bits >> 1);
                        this.Exponent = this.Exponent + 1;
                    }
                    else
                    {
                        this.SignificandBits = (this.SignificandBits & _highestBit) | (bits & ~_highestBit);
                        //this.Exponent = this.Exponent; //exponent stays the same
                    }
                }
            }
            else if (this.Exponent < value.Exponent)
            {
                if (value.Exponent >= this.Exponent + 64)
                {
                    this.SignificandBits = value.SignificandBits; //too small to matter
                    this.Exponent = value.Exponent;
                }
                else
                {
                    ulong bits = (value.SignificandBits | _highestBit) + ((this.SignificandBits | _highestBit) >> (int)(value.Exponent - this.Exponent));

                    if (bits < _highestBit) //this can only happen in an overflow                    
                    {
                        this.SignificandBits = (value.SignificandBits & _highestBit) | (bits >> 1);
                        this.Exponent = value.Exponent + 1;
                    }
                    else
                    {
                        this.SignificandBits = (value.SignificandBits & _highestBit) | (bits & ~_highestBit);
                        this.Exponent = value.Exponent;
                    }
                }
            }
            else //expDiff == 0
            {
                //the MSB must have the same sign, so the MSB will become 0, and logical overflow is guaranteed in this situation (so we can shift right and increment the exponent).
                this.SignificandBits = ((this.SignificandBits + value.SignificandBits) >> 1) | (this.SignificandBits & _highestBit);
                this.Exponent = this.Exponent + 1;
            }

            #endregion
        }

        public unsafe void Subtract(double valueDouble)
        {
            Precision = valid_bits(valueDouble, this);
            #region Parse the double
            // Implementation note: the use of goto is generally discouraged,
            // but here the idea is to copy-paste the casting call for double -> Quad
            // to avoid the expense of an additional function call
            // and the use of a single "return" goto target keeps things simple

            Quad value = new Quad();
            {
                // Translate the double into sign, exponent and mantissa.
                //long bits = BitConverter.DoubleToInt64Bits(value); // doing an unsafe pointer-conversion to get the bits is faster
                ulong bits = *((ulong*)&valueDouble);

                // Note that the shift is sign-extended, hence the test against -1 not 1                
                long exponent = (((long)bits >> 52) & 0x7ffL);
                ulong mantissa = (bits) & 0xfffffffffffffUL;

                if (exponent == 0x7ffL)
                {
                    if (mantissa == 0)
                    {
                        if (bits >= _highestBit) //sign bit set?
                            value = NegativeInfinity;
                        else
                            value = PositiveInfinity;

                        goto Parsed;
                    }
                    else
                    {
                        value = NaN;
                        goto Parsed;
                    }
                }

                // Subnormal numbers; exponent is effectively one higher,
                // but there's no extra normalisation bit in the mantissa
                if (exponent == 0)
                {
                    if (mantissa == 0)
                    {
                        value = Zero;
                        goto Parsed;
                    }
                    exponent++;

                    int firstSetPosition = nlz(mantissa);
                    mantissa <<= firstSetPosition;
                    exponent -= firstSetPosition;
                }
                else
                {
                    mantissa = mantissa << 11;
                    exponent -= 11;
                }

                exponent -= 1075;

                value.SignificandBits = (_highestBit & bits) | mantissa;
                value.Exponent = exponent;
            }
        Parsed:
            #endregion

            #region Subtraction
            if (this.Exponent <= notANumberExponent) //infinity or NaN - something
            {
                if (this.Exponent == zeroExponent)
                {
                    this.SignificandBits = value.SignificandBits ^ _highestBit; //negate value
                    this.Exponent = value.Exponent;
                }
                else
                {
                    Quad result = specialSubtractionTable[(int)(this.Exponent - zeroExponent), value.Exponent > notANumberExponent ? (int)(4 + (value.SignificandBits >> 63)) : (int)(value.Exponent - zeroExponent)];
                    this.SignificandBits = result.SignificandBits;
                    this.Exponent = result.Exponent;
                }

                return;
            }
            else if (value.Exponent <= notANumberExponent) //finite - (infinity or NaN)
            {
                if (value.Exponent != zeroExponent)
                {
                    Quad result = specialSubtractionTable[(int)(4 + (this.SignificandBits >> 63)), (int)(value.Exponent - zeroExponent)];
                    this.SignificandBits = result.SignificandBits;
                    this.Exponent = result.Exponent;
                }

                return;
            }

            if ((this.SignificandBits ^ value.SignificandBits) >= _highestBit) //this and value have different signs--use addition instead            
            {
                this.Add(new Quad(value.SignificandBits ^ _highestBit, value.Exponent));
                return;
            }

            if (this.Exponent > value.Exponent)
            {
                if (this.Exponent >= value.Exponent + 64)
                    return; //value too small to make a difference
                else
                {
                    ulong bits = (this.SignificandBits | _highestBit) - ((value.SignificandBits | _highestBit) >> (int)(this.Exponent - value.Exponent));

                    //make sure MSB is 1                       
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~_highestBit) | (this.SignificandBits & _highestBit);
                    this.Exponent = this.Exponent - highestBitPos;
                }
            }
            else if (this.Exponent < value.Exponent) //must subtract our significand from value, and switch the sign
            {
                if (value.Exponent >= this.Exponent + 64)
                {
                    this.SignificandBits = value.SignificandBits ^ _highestBit;
                    this.Exponent = value.Exponent;
                    return;
                }
                else
                {
                    ulong bits = (value.SignificandBits | _highestBit) - ((this.SignificandBits | _highestBit) >> (int)(value.Exponent - this.Exponent));

                    //make sure MSB is 1                       
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~_highestBit) | (~value.SignificandBits & _highestBit);
                    this.Exponent = value.Exponent - highestBitPos;
                }
            }
            else // (this.Exponent == value.Exponent)
            {
                if (value.SignificandBits > this.SignificandBits) //must switch sign
                {
                    ulong bits = value.SignificandBits - this.SignificandBits; //notice that we don't worry about de-implicitizing the MSB--it'd be eliminated by subtraction anyway
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~_highestBit) | (~value.SignificandBits & _highestBit);
                    this.Exponent = value.Exponent - highestBitPos;
                }
                else if (value.SignificandBits < this.SignificandBits) //sign remains the same
                {
                    ulong bits = this.SignificandBits - value.SignificandBits; //notice that we don't worry about de-implicitizing the MSB--it'd be eliminated by subtraction anyway
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~_highestBit) | (this.SignificandBits & _highestBit);
                    this.Exponent = this.Exponent - highestBitPos;
                }
                else //this == value
                {
                    //result is 0
                    this.SignificandBits = 0;
                    this.Exponent = zeroExponent;
                    return;
                }
            }

            if (this.Exponent < exponentLowerBound) //catch underflow
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent;
            }

            #endregion
        }

        public void Subtract(Quad value)
        {
            Precision = valid_bits(this, value);
            #region Subtraction
            if (this.Exponent <= notANumberExponent) //infinity or NaN - something
            {
                if (this.Exponent == zeroExponent)
                {
                    this.SignificandBits = value.SignificandBits ^ _highestBit; //negate value
                    this.Exponent = value.Exponent;
                }
                else
                {
                    Quad result = specialSubtractionTable[(int)(this.Exponent - zeroExponent), value.Exponent > notANumberExponent ? (int)(4 + (value.SignificandBits >> 63)) : (int)(value.Exponent - zeroExponent)];
                    this.SignificandBits = result.SignificandBits;
                    this.Exponent = result.Exponent;
                }

                return;
            }
            else if (value.Exponent <= notANumberExponent) //finite - (infinity or NaN)
            {
                if (value.Exponent != zeroExponent)
                {
                    Quad result = specialSubtractionTable[(int)(4 + (this.SignificandBits >> 63)), (int)(value.Exponent - zeroExponent)];
                    this.SignificandBits = result.SignificandBits;
                    this.Exponent = result.Exponent;
                }

                return;
            }

            if ((this.SignificandBits ^ value.SignificandBits) >= _highestBit) //this and value have different signs--use addition instead            
            {
                this.Add(new Quad(value.SignificandBits ^ _highestBit, value.Exponent));
                return;
            }

            if (this.Exponent > value.Exponent)
            {
                if (this.Exponent >= value.Exponent + 64)
                    return; //value too small to make a difference
                else
                {
                    ulong bits = (this.SignificandBits | _highestBit) - ((value.SignificandBits | _highestBit) >> (int)(this.Exponent - value.Exponent));

                    //make sure MSB is 1                       
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~_highestBit) | (this.SignificandBits & _highestBit);
                    this.Exponent = this.Exponent - highestBitPos;
                }
            }
            else if (this.Exponent < value.Exponent) //must subtract our significand from value, and switch the sign
            {
                if (value.Exponent >= this.Exponent + 64)
                {
                    this.SignificandBits = value.SignificandBits ^ _highestBit;
                    this.Exponent = value.Exponent;
                    return;
                }
                else
                {
                    ulong bits = (value.SignificandBits | _highestBit) - ((this.SignificandBits | _highestBit) >> (int)(value.Exponent - this.Exponent));

                    //make sure MSB is 1                       
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~_highestBit) | (~value.SignificandBits & _highestBit);
                    this.Exponent = value.Exponent - highestBitPos;
                }
            }
            else // (this.Exponent == value.Exponent)
            {
                if (value.SignificandBits > this.SignificandBits) //must switch sign
                {
                    ulong bits = value.SignificandBits - this.SignificandBits; //notice that we don't worry about de-implicitizing the MSB--it'd be eliminated by subtraction anyway
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~_highestBit) | (~value.SignificandBits & _highestBit);
                    this.Exponent = value.Exponent - highestBitPos;
                }
                else if (value.SignificandBits < this.SignificandBits) //sign remains the same
                {
                    ulong bits = this.SignificandBits - value.SignificandBits; //notice that we don't worry about de-implicitizing the MSB--it'd be eliminated by subtraction anyway
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~_highestBit) | (this.SignificandBits & _highestBit);
                    this.Exponent = this.Exponent - highestBitPos;
                }
                else //this == value
                {
                    //result is 0
                    this.SignificandBits = 0;
                    this.Exponent = zeroExponent;
                    return;
                }
            }

            if (this.Exponent < exponentLowerBound) //catch underflow
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent;
            }

            #endregion
        }

        public unsafe void Divide(double divisorDouble)
        {
            Precision = valid_bits(divisorDouble, this);
            #region Parse the double
            // Implementation note: the use of goto is generally discouraged,
            // but here the idea is to copy-paste the casting call for double -> Quad
            // to avoid the expense of an additional function call
            // and the use of a single "return" goto target keeps things simple

            Quad divisor= new Quad();
            {
                // Translate the double into sign, exponent and mantissa.
                //long bits = BitConverter.DoubleToInt64Bits(divisor); // doing an unsafe pointer-conversion to get the bits is faster
                ulong bits = *((ulong*)&divisorDouble);

                // Note that the shift is sign-extended, hence the test against -1 not 1                
                long exponent = (((long)bits >> 52) & 0x7ffL);
                ulong mantissa = (bits) & 0xfffffffffffffUL;

                if (exponent == 0x7ffL)
                {
                    if (mantissa == 0)
                    {
                        if (bits >= _highestBit) //sign bit set?
                            divisor = NegativeInfinity;
                        else
                            divisor = PositiveInfinity;

                        goto Parsed;
                    }
                    else
                    {
                        divisor = NaN;
                        goto Parsed;
                    }
                }

                // Subnormal numbers; exponent is effectively one higher,
                // but there's no extra normalisation bit in the mantissa
                if (exponent == 0)
                {
                    if (mantissa == 0)
                    {
                        divisor = Zero;
                        goto Parsed;
                    }
                    exponent++;

                    int firstSetPosition = nlz(mantissa);
                    mantissa <<= firstSetPosition;
                    exponent -= firstSetPosition;
                }
                else
                {
                    mantissa = mantissa << 11;
                    exponent -= 11;
                }

                exponent -= 1075;

                divisor.SignificandBits = (_highestBit & bits) | mantissa;
                divisor.Exponent = exponent;
            }
        Parsed:
            #endregion

            #region Division
            if (this.Exponent <= notANumberExponent) //zero/infinity/NaN divided by something
            {
                Quad result = specialDivisionTable[(int)(this.Exponent - zeroExponent), divisor.Exponent > notANumberExponent ? (int)(4 + (divisor.SignificandBits >> 63)) : (int)(divisor.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }
            else if (divisor.Exponent <= notANumberExponent) //finite divided by zero/infinity/NaN
            {
                Quad result = specialDivisionTable[(int)(4 + (this.SignificandBits >> 63)), (int)(divisor.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }

            ulong un1 = 0,     // Norm. dividend LSD's.
                     vn1, vn0,        // Norm. divisor digits.
                     q1, q0,          // Quotient digits.
                     un21,// Dividend digit pairs.
                     rhat;            // A remainder.            

            //result.Significand = highestBit & (this.Significand ^ divisor.Significand); //determine the sign bit

            //this.Significand |= highestBit; //de-implicitize the 1 before the binary point
            //divisor.Significand |= highestBit;

            long adjExponent = 0;
            ulong thisAdjSignificand = this.SignificandBits | _highestBit;
            ulong divisorAdjSignificand = divisor.SignificandBits | _highestBit;

            if (thisAdjSignificand >= divisorAdjSignificand)
            {
                //need to make this's significand smaller than divisor's
                adjExponent = 1;
                un1 = (this.SignificandBits & 1) << 31;
                thisAdjSignificand = thisAdjSignificand >> 1;
            }

            vn1 = divisorAdjSignificand >> 32;            // Break divisor up into
            vn0 = divisor.SignificandBits & 0xFFFFFFFF;         // two 32-bit digits.            

            q1 = thisAdjSignificand / vn1;            // Compute the first
            rhat = thisAdjSignificand - q1 * vn1;     // quotient digit, q1.
        again1:
            if (q1 >= b || q1 * vn0 > b * rhat + un1)
            {
                q1 = q1 - 1;
                rhat = rhat + vn1;
                if (rhat < b) goto again1;
            }

            un21 = thisAdjSignificand * b + un1 - q1 * divisorAdjSignificand;  // Multiply and subtract.

            q0 = un21 / vn1;            // Compute the second
            rhat = un21 - q0 * vn1;     // quotient digit, q0.
        again2:
            if (q0 >= b || q0 * vn0 > b * rhat)
            {
                q0 = q0 - 1;
                rhat = rhat + vn1;
                if (rhat < b) goto again2;
            }

            thisAdjSignificand = q1 * b + q0; //convenient place to store intermediate result

            //if (this.Significand == 0) //the final significand should never be 0
            //    result.Exponent = 0;
            //else

            long originalExponent;
            long divisorExponent;

            if (thisAdjSignificand < (1UL << 63))
            {
                this.SignificandBits = (~_highestBit & (thisAdjSignificand << 1)) | ((this.SignificandBits ^ divisor.SignificandBits) & _highestBit);

                originalExponent = this.Exponent - 1 + adjExponent;
                divisorExponent = divisor.Exponent + 64;
            }
            else
            {
                this.SignificandBits = (~_highestBit & thisAdjSignificand) | ((this.SignificandBits ^ divisor.SignificandBits) & _highestBit);

                originalExponent = this.Exponent + adjExponent;
                divisorExponent = divisor.Exponent + 64;
            }

            this.Exponent = originalExponent - divisorExponent;

            //now check for underflow or overflow
            if (divisorExponent > 0 && this.Exponent > originalExponent) //underflow
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent; //new value is 0
            }
            else if (divisorExponent < 0 && this.Exponent < originalExponent) //overflow
            {
                this.SignificandBits = 0;// (this.SignificandBits & highestBit);
                this.Exponent = this.SignificandBits >= _highestBit ? negativeInfinityExponent : infinityExponent;
            }
            else if (this.Exponent < exponentLowerBound)
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent; //new value is 0
            }
            else if (this.Exponent > exponentUpperBound)
            {
                this.SignificandBits = 0;// (this.SignificandBits & highestBit);
                this.Exponent = this.SignificandBits >= _highestBit ? negativeInfinityExponent : infinityExponent;
            }

            #endregion
        }

        public void Divide(Quad divisor)
        {
            Precision = valid_bits(this, divisor);
            #region Division
            if (this.Exponent <= notANumberExponent) //zero/infinity/NaN divided by something
            {
                Quad result = specialDivisionTable[(int)(this.Exponent - zeroExponent), divisor.Exponent > notANumberExponent ? (int)(4 + (divisor.SignificandBits >> 63)) : (int)(divisor.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }
            else if (divisor.Exponent <= notANumberExponent) //finite divided by zero/infinity/NaN
            {
                Quad result = specialDivisionTable[(int)(4 + (this.SignificandBits >> 63)), (int)(divisor.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }

            ulong un1 = 0,     // Norm. dividend LSD's.
                     vn1, vn0,        // Norm. divisor digits.
                     q1, q0,          // Quotient digits.
                     un21,// Dividend digit pairs.
                     rhat;            // A remainder.            

            //result.Significand = highestBit & (this.Significand ^ divisor.Significand); //determine the sign bit

            //this.Significand |= highestBit; //de-implicitize the 1 before the binary point
            //divisor.Significand |= highestBit;

            long adjExponent = 0;
            ulong thisAdjSignificand = this.SignificandBits | _highestBit;
            ulong divisorAdjSignificand = divisor.SignificandBits | _highestBit;

            if (thisAdjSignificand >= divisorAdjSignificand)
            {
                //need to make this's significand smaller than divisor's
                adjExponent = 1;
                un1 = (this.SignificandBits & 1) << 31;
                thisAdjSignificand = thisAdjSignificand >> 1;
            }

            vn1 = divisorAdjSignificand >> 32;            // Break divisor up into
            vn0 = divisor.SignificandBits & 0xFFFFFFFF;         // two 32-bit digits.            

            q1 = thisAdjSignificand / vn1;            // Compute the first
            rhat = thisAdjSignificand - q1 * vn1;     // quotient digit, q1.
        again1:
            if (q1 >= b || q1 * vn0 > b * rhat + un1)
            {
                q1 = q1 - 1;
                rhat = rhat + vn1;
                if (rhat < b) goto again1;
            }

            un21 = thisAdjSignificand * b + un1 - q1 * divisorAdjSignificand;  // Multiply and subtract.

            q0 = un21 / vn1;            // Compute the second
            rhat = un21 - q0 * vn1;     // quotient digit, q0.
        again2:
            if (q0 >= b || q0 * vn0 > b * rhat)
            {
                q0 = q0 - 1;
                rhat = rhat + vn1;
                if (rhat < b) goto again2;
            }

            thisAdjSignificand = q1 * b + q0; //convenient place to store intermediate result

            //if (this.Significand == 0) //the final significand should never be 0
            //    result.Exponent = 0;
            //else

            long originalExponent;
            long divisorExponent;

            if (thisAdjSignificand < (1UL << 63))
            {
                this.SignificandBits = (~_highestBit & (thisAdjSignificand << 1)) | ((this.SignificandBits ^ divisor.SignificandBits) & _highestBit);

                originalExponent = this.Exponent - 1 + adjExponent;
                divisorExponent = divisor.Exponent + 64;
            }
            else
            {
                this.SignificandBits = (~_highestBit & thisAdjSignificand) | ((this.SignificandBits ^ divisor.SignificandBits) & _highestBit);

                originalExponent = this.Exponent + adjExponent;
                divisorExponent = divisor.Exponent + 64;
            }

            this.Exponent = originalExponent - divisorExponent;

            //now check for underflow or overflow
            if (divisorExponent > 0 && this.Exponent > originalExponent) //underflow
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent; //new value is 0
            }
            else if (divisorExponent < 0 && this.Exponent < originalExponent) //overflow
            {
                this.SignificandBits = 0;// (this.SignificandBits & highestBit);
                this.Exponent = this.SignificandBits >= _highestBit ? negativeInfinityExponent : infinityExponent;
            }
            else if (this.Exponent < exponentLowerBound)
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent; //new value is 0
            }
            else if (this.Exponent > exponentUpperBound)
            {
                this.SignificandBits = 0;// (this.SignificandBits & highestBit);
                this.Exponent = this.SignificandBits >= _highestBit ? negativeInfinityExponent : infinityExponent;
            }

            #endregion
        }

        #endregion

        #region Operators
        /// <summary>
        /// Efficiently multiplies the Quad by 2^shift.
        /// </summary>
        /// <param name="qd"></param>
        /// <param name="shift"></param>
        /// <returns></returns>        
        public static Quad operator <<(Quad qd, int shift)
        {
            if (qd.Exponent <= notANumberExponent)
                return qd; //finite * infinity == infinity, finite * NaN == NaN, finite * 0 == 0
            else
                return new Quad(qd.SignificandBits, qd.Exponent + shift, qd.Precision);
        }

        /// <summary>
        /// Efficiently divides the Quad by 2^shift.
        /// </summary>
        /// <param name="qd"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        public static Quad operator >>(Quad qd, int shift)
        {
            if (qd.Exponent <= notANumberExponent)
                return qd; //infinity / finite == infinity, NaN / finite == NaN, 0 / finite == 0
            else
                return new Quad(qd.SignificandBits, qd.Exponent - shift, qd.Precision);
        }

        /// <summary>
        /// Efficiently multiplies the Quad by 2^shift.
        /// </summary>
        /// <param name="qd"></param>
        /// <param name="shift"></param>
        /// <returns></returns>        
        public static Quad LeftShift(Quad qd, int shift)
        {
            if (qd.Exponent <= notANumberExponent)
                return qd; //finite * infinity == infinity, finite * NaN == NaN, finite * 0 == 0
            else
                return new Quad(qd.SignificandBits, qd.Exponent + shift, qd.Precision);
        }

        /// <summary>
        /// Efficiently divides the Quad by 2^shift.
        /// </summary>
        /// <param name="qd"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        public static Quad RightShift(Quad qd, int shift)
        {
            if (qd.Exponent <= notANumberExponent)
                return qd; //infinity / finite == infinity, NaN / finite == NaN, 0 / finite == 0
            else
                return new Quad(qd.SignificandBits, qd.Exponent - shift, qd.Precision);
        }

        /// <summary>
        /// Divides one Quad by another and returns the result
        /// </summary>
        /// <param name="qd1"></param>
        /// <param name="qd2"></param>
        /// <returns></returns>
        /// <remarks>
        /// This code is a heavily modified derivation of a division routine given by http://www.hackersdelight.org/HDcode/divlu.c.txt ,
        /// which has a very liberal (public domain-like) license attached: http://www.hackersdelight.org/permissions.htm
        /// </remarks>
        public static Quad operator /(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //zero/infinity/NaN divided by something            
                return specialDivisionTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            else if (qd2.Exponent <= notANumberExponent) //finite divided by zero/infinity/NaN            
                return specialDivisionTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];

            if (qd2.Exponent == long.MinValue)
                throw new DivideByZeroException();
            else if (qd1.Exponent == long.MinValue)
                return Zero;

            ulong un1 = 0,     // Norm. dividend LSD's.
                     vn1, vn0,        // Norm. divisor digits.
                     q1, q0,          // Quotient digits.
                     un21,// Dividend digit pairs.
                     rhat;            // A remainder.                        

            long adjExponent = 0;
            ulong qd1AdjSignificand = qd1.SignificandBits | _highestBit;  //de-implicitize the 1 before the binary point
            ulong qd2AdjSignificand = qd2.SignificandBits | _highestBit;  //de-implicitize the 1 before the binary point

            if (qd1AdjSignificand >= qd2AdjSignificand)
            {
                // need to make qd1's significand smaller than qd2's
                // If we were faithful to the original code this method derives from,
                // we would branch on qd1AdjSignificand > qd2AdjSignificand instead.
                // However, this results in undesirable results like (in binary) 11/11 = 0.11111...,
                // where the result should be 1.0.  Thus, we branch on >=, which prevents this problem.
                adjExponent = 1;
                un1 = (qd1.SignificandBits & 1) << 31;
                qd1AdjSignificand = qd1AdjSignificand >> 1;
            }

            vn1 = qd2AdjSignificand >> 32;            // Break divisor up into
            vn0 = qd2.SignificandBits & 0xFFFFFFFF;         // two 32-bit digits.            

            q1 = qd1AdjSignificand / vn1;            // Compute the first
            rhat = qd1AdjSignificand - q1 * vn1;     // quotient digit, q1.
        again1:
            if (q1 >= b || q1 * vn0 > b * rhat + un1)
            {
                q1 = q1 - 1;
                rhat = rhat + vn1;
                if (rhat < b) goto again1;
            }

            un21 = qd1AdjSignificand * b + un1 - q1 * qd2AdjSignificand;  // Multiply and subtract.

            q0 = un21 / vn1;            // Compute the second
            rhat = un21 - q0 * vn1;     // quotient digit, q0.
        again2:
            if (q0 >= b || q0 * vn0 > b * rhat)
            {
                q0 = q0 - 1;
                rhat = rhat + vn1;
                if (rhat < b) goto again2;
            }

            qd1AdjSignificand = q1 * b + q0; //convenient place to store intermediate result

            //if (qd1.Significand == 0) //the final significand should never be 0
            //    result.Exponent = 0;
            //else

            long originalExponent;
            long divisorExponent;
            Quad result= new Quad();
            result.Precision = valid_bits(qd1, qd2, 62); // division reduce max precision to 62

            if (qd1AdjSignificand < (1UL << 63))
            {
                result.SignificandBits = (~_highestBit & (qd1AdjSignificand << 1)) | ((qd1.SignificandBits ^ qd2.SignificandBits) & _highestBit);

                originalExponent = qd1.Exponent - 1 + adjExponent;
                divisorExponent = qd2.Exponent + 64;
            }
            else
            {
                result.SignificandBits = (~_highestBit & qd1AdjSignificand) | ((qd1.SignificandBits ^ qd2.SignificandBits) & _highestBit);

                originalExponent = qd1.Exponent + adjExponent;
                divisorExponent = qd2.Exponent + 64;
            }

            result.Exponent = originalExponent - divisorExponent;

            //now check for underflow or overflow
            if (divisorExponent > 0 && result.Exponent > originalExponent) //underflow
                return Zero;
            else if (divisorExponent < 0 && result.Exponent < originalExponent) //overflow            
                return result.SignificandBits >= _highestBit ? NegativeInfinity : PositiveInfinity;
            else if (result.Exponent < exponentLowerBound)
                return Zero;
            else if (result.Exponent > exponentUpperBound)
                return result.SignificandBits >= _highestBit ? NegativeInfinity : PositiveInfinity;
            else
                return result;
        }

        /// <summary>
        /// Divides two numbers and gets the remainder.
        /// This is equivalent to qd1 - (qd2 * Truncate(qd1 / qd2)).
        /// </summary>
        /// <param name="qd1"></param>
        /// <param name="qd2"></param>
        /// <returns></returns>
        public static Quad operator %(Quad qd1, Quad qd2)
        {
            if (qd2.Exponent == infinityExponent || qd2.Exponent == negativeInfinityExponent)
            {
                if (qd1.Exponent == infinityExponent || qd1.Exponent == negativeInfinityExponent)
                    return NaN;
                else
                    return qd1;
            }

            return qd1 - (qd2 * Truncate(qd1 / qd2));
        }

        public static Quad operator -(Quad qd) //u nary
        {
            if (qd.Exponent <= notANumberExponent)
            {
                if (qd.Exponent == infinityExponent) return NegativeInfinity;
                else if (qd.Exponent == negativeInfinityExponent) return PositiveInfinity;
                else return qd;
            }
            else
                return new Quad(qd.SignificandBits ^ _highestBit, qd.Exponent, qd.Precision); //just swap the sign bit                        
        }

        public static Quad operator +(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //zero or infinity or NaN + something
            {
                if (qd1.Exponent == zeroExponent) return qd2;
                else return specialAdditionTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            }
            else if (qd2.Exponent <= notANumberExponent) //finite + (infinity or NaN)
            {
                if (qd2.Exponent == zeroExponent) return qd1;
                else return specialAdditionTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];
            }

            if ((qd1.SignificandBits ^ qd2.SignificandBits) >= _highestBit) //qd1 and qd2 have different signs--use subtraction instead
            {
                return qd1 - new Quad(qd2.SignificandBits ^ _highestBit, qd2.Exponent, qd2.Precision);
            }

            Quad result= new Quad();
            result.Precision = valid_bits(qd1, qd2);
            if (qd1.Exponent > qd2.Exponent)
            {
                if (qd1.Exponent >= qd2.Exponent + 64)
                    return qd1; //qd2 too small to make a difference
                else
                {
                    ulong bits = (qd1.SignificandBits | _highestBit) + ((qd2.SignificandBits | _highestBit) >> (int)(qd1.Exponent - qd2.Exponent));

                    if (bits < _highestBit) //this can only happen in an overflow                    
                        result = new Quad((qd1.SignificandBits & _highestBit) | (bits >> 1), qd1.Exponent + 1, result.Precision);
                    else
                        return new Quad((qd1.SignificandBits & _highestBit) | (bits & ~_highestBit), qd1.Exponent, result.Precision);
                }
            }
            else if (qd1.Exponent < qd2.Exponent)
            {
                if (qd2.Exponent >= qd1.Exponent + 64)
                    return qd2; //qd1 too small to matter
                else
                {
                    ulong bits = (qd2.SignificandBits | _highestBit) + ((qd1.SignificandBits | _highestBit) >> (int)(qd2.Exponent - qd1.Exponent));

                    if (bits < _highestBit) //this can only happen in an overflow                    
                        result = new Quad((qd2.SignificandBits & _highestBit) | (bits >> 1), qd2.Exponent + 1, result.Precision);
                    else
                        return new Quad((qd2.SignificandBits & _highestBit) | (bits & ~_highestBit), qd2.Exponent, result.Precision);
                }
            }
            else //expDiff == 0
            {
                //the MSB must have the same sign, so the MSB will become 0, and logical overflow is guaranteed in this situation (so we can shift right and increment the exponent).
                result = new Quad(((qd1.SignificandBits + qd2.SignificandBits) >> 1) | (qd1.SignificandBits & _highestBit), qd1.Exponent + 1, result.Precision);
            }

            if (result.Exponent > exponentUpperBound) //overflow check
                return result.SignificandBits >= _highestBit ? NegativeInfinity : PositiveInfinity;
            else
                return result;
        }

        public static Quad operator -(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //infinity or NaN - something
            {
                if (qd1.Exponent == zeroExponent) return -qd2;
                else return specialSubtractionTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            }
            else if (qd2.Exponent <= notANumberExponent) //finite - (infinity or NaN)            
            {
                if (qd2.Exponent == zeroExponent) return qd1;
                else return specialSubtractionTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];
            }

            if ((qd1.SignificandBits ^ qd2.SignificandBits) >= _highestBit) //qd1 and qd2 have different signs--use addition instead
            {
                return qd1 + new Quad(qd2.SignificandBits ^ _highestBit, qd2.Exponent, qd2.Precision);
            }

            Quad result= new Quad();
            result.Precision = valid_bits(qd1, qd2);
            if (qd1.Exponent > qd2.Exponent)
            {
                if (qd1.Exponent >= qd2.Exponent + 64)
                    return qd1; //qd2 too small to make a difference
                else
                {
                    ulong bits = (qd1.SignificandBits | _highestBit) - ((qd2.SignificandBits | _highestBit) >> (int)(qd1.Exponent - qd2.Exponent));

                    //make sure MSB is 1                       
                    int highestBitPos = nlz(bits);
                    result = new Quad(((bits << highestBitPos) & ~_highestBit) | (qd1.SignificandBits & _highestBit), qd1.Exponent - highestBitPos, result.Precision);
                }
            }
            else if (qd1.Exponent < qd2.Exponent) //must subtract qd1's significand from qd2, and switch the sign
            {
                if (qd2.Exponent >= qd1.Exponent + 64)
                    return new Quad(qd2.SignificandBits ^ _highestBit, qd2.Exponent, result.Precision); //qd1 too small to matter, switch sign of qd2 and return

                ulong bits = (qd2.SignificandBits | _highestBit) - ((qd1.SignificandBits | _highestBit) >> (int)(qd2.Exponent - qd1.Exponent));

                //make sure MSB is 1                       
                int highestBitPos = nlz(bits);
                result = new Quad(((bits << highestBitPos) & ~_highestBit) | (~qd2.SignificandBits & _highestBit), qd2.Exponent - highestBitPos, result.Precision);
            }
            else // (qd1.Exponent == qd2.Exponent)
            {
                if (qd2.SignificandBits > qd1.SignificandBits) //must switch sign
                {
                    ulong bits = qd2.SignificandBits - qd1.SignificandBits; //notice that we don't worry about de-implicitizing the MSB--it'd be eliminated by subtraction anyway
                    int highestBitPos = nlz(bits);
                    result = new Quad(((bits << highestBitPos) & ~_highestBit) | (~qd2.SignificandBits & _highestBit), qd2.Exponent - highestBitPos, result.Precision);
                }
                else if (qd2.SignificandBits < qd1.SignificandBits) //sign remains the same
                {
                    ulong bits = qd1.SignificandBits - qd2.SignificandBits; //notice that we don't worry about de-implicitizing the MSB--it'd be eliminated by subtraction anyway
                    int highestBitPos = nlz(bits);
                    result = new Quad(((bits << highestBitPos) & ~_highestBit) | (qd1.SignificandBits & _highestBit), qd1.Exponent - highestBitPos, result.Precision);
                }
                else //qd1 == qd2
                    return Zero;
            }

            if (result.Exponent < exponentLowerBound) //handle underflow
                return Zero;
            // check if result is smaller than imprecise bits of larger exponent value.
            // Eg 1.10000000000000_10E50 - 1.100000000000000_01E50 if _xy are imprecise bits should result in zero, not 1E33 !
            // same for negative exponents, eg 1.10000000000000_10E-50 - 1.100000000000000_01E-50 should be zero, not 1E-67 !
            var largerExp = Math.Max(qd1.Exponent, qd2.Exponent);
            var largerBits = qd1.Exponent > qd2.Exponent ? qd1.Precision : qd2.Precision;
            if (largerExp - result.Exponent >= largerBits)
                return Zero;
            // otherwise return valid result
            return result;
        }

        public static Quad operator *(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //zero/infinity/NaN * something            
                return specialMultiplicationTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            else if (qd2.Exponent <= notANumberExponent) //finite * zero/infinity/NaN            
                return specialMultiplicationTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];

            ulong high1 = (qd1.SignificandBits | _highestBit) >> 32; //de-implicitize the 1
            ulong high2 = (qd2.SignificandBits | _highestBit) >> 32;

            //because the MSB of both significands is 1, the MSB of the result will also be 1, and the product of low bits on both significands is dropped (and thus we can skip its calculation)
            ulong significandBits = high1 * high2 + (((qd1.SignificandBits & lowWordMask) * high2) >> 32) + ((high1 * (qd2.SignificandBits & lowWordMask)) >> 32);

            long qd2Exponent;
            Quad result= new Quad();
            result.Precision = valid_bits(qd1, qd2);
            if (significandBits < (1UL << 63))
            {
                qd2Exponent = qd2.Exponent - 1 + 64;
                result = new Quad(((qd1.SignificandBits ^ qd2.SignificandBits) & _highestBit) | ((significandBits << 1) & ~_highestBit), qd1.Exponent + qd2Exponent, result.Precision);
            }
            else
            {
                qd2Exponent = qd2.Exponent + 64;
                result = new Quad(((qd1.SignificandBits ^ qd2.SignificandBits) & _highestBit) | (significandBits & ~_highestBit), qd1.Exponent + qd2Exponent, result.Precision);
            }

            if (qd2Exponent < 0 && result.Exponent > qd1.Exponent) //did the exponent get larger after adding something negative?
                return Zero; //underflow
            else if (qd2Exponent > 0 && result.Exponent < qd1.Exponent) //did the exponent get smaller when it should have gotten larger?
                return result.SignificandBits >= _highestBit ? NegativeInfinity : PositiveInfinity; //overflow
            else if (result.Exponent < exponentLowerBound) //check for underflow
                return Zero;
            else if (result.Exponent > exponentUpperBound) //overflow
                return result.SignificandBits >= _highestBit ? NegativeInfinity : PositiveInfinity; //overflow
            else
                return result;
        }

        public static Quad operator ++(Quad qd)
        {
            return qd + One;
        }

        public static Quad operator --(Quad qd)
        {
            return qd - One;
        }

        #endregion

        #region Comparison and Hash

        /// <summary>
        /// Determines if qd1 is the same value as qd2. The same rules for doubles are used, e.g. PositiveInfinity == PositiveInfinity, but NaN != NaN.
        /// </summary>        
        public static bool ExactEqual(Quad qd1, Quad qd2) => (qd1.SignificandBits == qd2.SignificandBits && qd1.Exponent == qd2.Exponent && qd1.Exponent != notANumberExponent);



        /// <summary>
        /// Determines if qd1 is the same value as qd2. The same rules for doubles are used, e.g. PositiveInfinity == PositiveInfinity, but NaN != NaN.
        /// Handle UNDERFLOW, ie differences under current validBits. If q1=5.0000000001 with precision 6 digits, and q2=5 - they should be same !
        /// </summary>        
        public static bool operator ==(Quad qd1, Quad qd2)
        {
            if (!IsNumber(qd1) || !IsNumber(qd2)) // if any is not a number (NaN or +/- Inf), use old comparison
                return ExactEqual(qd1, qd2);// || (qd1.Exponent == long.MinValue && qd2.Exponent == long.MinValue);
            // use subtraction, since it handles underflows
            return ExactEqual(qd1 - qd2, Zero);

        }

        /// <summary>
        /// Determines if qd1 is different from qd2. Always true if qd1 or qd2 is NaN.  False if both qd1 and qd2 are infinity with the same polarity (e.g. PositiveInfinities).
        /// Handle UNDERFLOWS, by using == ( slower, but safer )
        /// </summary>        
        public static bool operator !=(Quad qd1, Quad qd2)
        {
            // return (qd1.SignificandBits != qd2.SignificandBits || qd1.Exponent != qd2.Exponent || qd1.Exponent == notANumberExponent);// && (qd1.Exponent != long.MinValue || qd2.Exponent != long.MinValue);
            return !(qd1==qd2);
        }

        public static bool operator >(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //zero/infinity/NaN * something            
                return specialGreaterThanTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            else if (qd2.Exponent <= notANumberExponent) //finite * zero/infinity/NaN            
                return specialGreaterThanTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];

            //There is probably a faster way to accomplish this by cleverly exploiting signed longs
            switch ((qd1.SignificandBits & _highestBit) | ((qd2.SignificandBits & _highestBit) >> 1))
            {
                case _highestBit: //qd1 is negative, qd2 positive
                    return false;
                case secondHighestBit: //qd1 positive, qd2 negative
                    return true;
                case _highestBit | secondHighestBit: //both negative
                    return qd1.Exponent < qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits < qd2.SignificandBits);
                default: //both positive
                    return qd1.Exponent > qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits > qd2.SignificandBits);
            }
        }

        public static bool operator <(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //zero/infinity/NaN * something            
                return specialLessThanTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            else if (qd2.Exponent <= notANumberExponent) //finite * zero/infinity/NaN            
                return specialLessThanTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];

            switch ((qd1.SignificandBits & _highestBit) | ((qd2.SignificandBits & _highestBit) >> 1))
            {
                case _highestBit: //qd1 is negative, qd2 positive
                    return true;
                case secondHighestBit: //qd1 positive, qd2 negative
                    return false;
                case _highestBit | secondHighestBit: //both negative
                    return qd1.Exponent > qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits > qd2.SignificandBits);
                default: //both positive
                    return qd1.Exponent < qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits < qd2.SignificandBits);
            }

        }

        public static bool operator >=(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //zero/infinity/NaN * something            
                return specialGreaterEqualThanTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            else if (qd2.Exponent <= notANumberExponent) //finite * zero/infinity/NaN            
                return specialGreaterEqualThanTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];

            switch ((qd1.SignificandBits & _highestBit) | ((qd2.SignificandBits & _highestBit) >> 1))
            {
                case _highestBit: //qd1 is negative, qd2 positive
                    return false;
                case secondHighestBit: //qd1 positive, qd2 negative
                    return true;
                case _highestBit | secondHighestBit: //both negative
                    return qd1.Exponent < qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits <= qd2.SignificandBits);
                default: //both positive
                    return qd1.Exponent > qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits >= qd2.SignificandBits);
            }
        }

        public static bool operator <=(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //zero/infinity/NaN * something            
                return specialLessEqualThanTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            else if (qd2.Exponent <= notANumberExponent) //finite * zero/infinity/NaN            
                return specialLessEqualThanTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];

            switch ((qd1.SignificandBits & _highestBit) | ((qd2.SignificandBits & _highestBit) >> 1))
            {
                case _highestBit: //qd1 is negative, qd2 positive
                    return true;
                case secondHighestBit: //qd1 positive, qd2 negative
                    return false;
                case _highestBit | secondHighestBit: //both negative
                    return qd1.Exponent > qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits >= qd2.SignificandBits);
                default: //both positive
                    return qd1.Exponent < qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits <= qd2.SignificandBits);
            }
        }


        public  int GetHashCode()
        {
            int expHash = Exponent.GetHashCode();
            return SignificandBits.GetHashCode() ^ (expHash << 16 | expHash >> 16); //rotate expHash's bits 16 places
        }

        public  bool Equals(object obj)
        {
            if (obj == null) return false;
            try
            {
                return this == (Quad)obj;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns 1 if this Quad is greater than the argument, or the argument is NaN; 0 if they are both equal or both NaN/PositiveInfinity/NegativeInfinity;
        /// and -1 if this Quad is less than the argument, or this Quad is NaN.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Quad other)
        {
            if (this.Exponent == notANumberExponent) //special value
            {
                return other.Exponent == notANumberExponent ? 0 : -1; //If both NaN, return 0; otherwise, this NaN is "less than" everything else
            }
            else if (other.Exponent == notANumberExponent)
                return 1; //this non-NaN "greater" than other (NaN)

            if (this == other) return 0;
            else if (this > other) return 1;
            else return -1; //this < other
        }
        #endregion

        #region IsInfinity/IsNaN and other number info methods

        // testing abnormal values
        public static bool IsNaN(Quad a) =>a.Exponent == notANumberExponent;
        public static bool IsInfinity(Quad a) => a.Exponent == infinityExponent || a.Exponent == negativeInfinityExponent;
        public static bool IsPositiveInfinity(Quad a) =>  a.Exponent == infinityExponent;
        public static bool IsNegativeInfinity(Quad a) => a.Exponent == negativeInfinityExponent;

        // testing normal values
        public static bool IsNegative(Quad a) => (a.SignificandBits & _highestBit) != 0;
        public static bool IsZero(Quad a) => a.SignificandBits==0UL && a.Exponent==long.MinValue;
        public static bool IsNumber(Quad a) => !(IsNaN(a) || IsInfinity(a)); // not NaN or Inf
        public static bool IsRegular(Quad a) => !(IsNaN(a) || IsInfinity(a) || IsZero(a)); // not NaN or Inf or Zero

        public static SpecialFloatValue IsSpecial(Quad a)
        {
            if (IsNaN(a)) return SpecialFloatValue.NaN;
            if (IsNegativeInfinity(a)) return SpecialFloatValue.NegativeInfinity;
            if (IsPositiveInfinity(a)) return SpecialFloatValue.PositiveInfinity;
            if (IsZero(a)) return SpecialFloatValue.Zero;
            return SpecialFloatValue.None;
        }

        public static int MantissaBitSize(Quad a) => a.Precision;


        #endregion

        #region String conversions



        /// <summary>
        /// Parses decimal number strings in the form of "1234.5678".  Does not presently handle exponential/scientific notation.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private static Quad _Parse(string number)
        {
            if (number.Equals(specialStringTable[1], StringComparison.OrdinalIgnoreCase))
                return PositiveInfinity;
            else if (number.Equals(specialStringTable[2], StringComparison.OrdinalIgnoreCase))
                return NegativeInfinity;
            else if (number.Equals(specialStringTable[3], StringComparison.OrdinalIgnoreCase))
                return NaN;

            //Can piggyback on BigInteger's parser for this, but this is inefficient.
            //Smarter way is to break the numeric string into chunks and parse each of them using long's parse method, then combine.

            bool negative = number.StartsWith("-");
            if (negative) number = number.Substring(1);

            string left = number, right = null;
            int decimalPoint = number.IndexOf('.');
            if (decimalPoint >= 0)
            {
                left = number.Substring(0, decimalPoint);
                right = number.Substring(decimalPoint + 1);
            }

            System.Numerics.BigInteger leftInt = System.Numerics.BigInteger.Parse(left);

            Quad result = (Quad)leftInt;
            if (right != null)
            {
                System.Numerics.BigInteger rightInt = System.Numerics.BigInteger.Parse(right);
                Quad fractional = (Quad)rightInt;

                // we implicitly multiplied the stuff right of the decimal point by 10^(right.length) to get an integer;
                // now we must reverse that and add this quantity to our results.
                result += fractional * (Quad.Pow(new Quad(10L, 0), -right.Length));
            }

            return negative ? -result : result;
        }



        /// <summary>
        /// Returns this number as a decimal, or in scientific notation where a decimal would be excessively long.
        /// Equivalent to ToString(QuadrupleFormat.ScientificApproximate).
        /// </summary>
        /// <returns></returns>
        public override string ToString() =>ToString(QuadrupleStringFormat.ScientificApproximate);


        /// <summary>
        /// Obtains a string representation for this Quad according to the specified format.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        /// <remarks>
        /// ScientificExact returns the value in scientific notation as accurately as possible, but is still subject to imprecision due to the conversion from 
        /// binary to decimal and the divisions or multiplications used in the conversion.  It does not use rounding, which can lead to odd-looking outputs
        /// that would otherwise be rounded by double.ToString() or the ScientificApproximate format (which uses double.ToString()).  For example, 0.1 will be rendered
        /// as the string "9.9999999999999999981e-2".
        /// </remarks>
        public string ToString(QuadrupleStringFormat format)
        {
            if (Exponent <= notANumberExponent) return specialStringTable[(int)(Exponent - zeroExponent)];

            switch (format)
            {
                case QuadrupleStringFormat.HexExponential:
                    if (SignificandBits >= _highestBit)
                        return "-" + SignificandBits.ToString("x") + "*2^" + (Exponent >= 0 ? Exponent.ToString("x") : "-" + (-Exponent).ToString("x"));
                    else
                        return (SignificandBits | _highestBit).ToString("x") + "*2^" + (Exponent >= 0 ? Exponent.ToString("x") : "-" + (-Exponent).ToString("x"));

                case QuadrupleStringFormat.DecimalExponential:
                    if (SignificandBits >= _highestBit)
                        return "-" + SignificandBits.ToString() + "*2^" + Exponent.ToString();
                    else
                        return (SignificandBits | _highestBit).ToString() + "*2^" + Exponent.ToString();

                case QuadrupleStringFormat.ScientificApproximate:
                    if (Exponent >= -1022 && Exponent <= 1023) //can be represented as double (albeit with a precision loss)
                    {
                        var d = (double)this;
                        return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }

                    double dVal = (double)new Quad(SignificandBits, -61);
                    double dExp = base2to10Multiplier * (Exponent + 61);

                    string sign = "";
                    if (dVal < 0)
                    {
                        sign = "-";
                        dVal = -dVal;
                    }

                    if (dExp >= 0)
                        dVal *= Math.Pow(10, (dExp % 1));
                    else
                        dVal *= Math.Pow(10, -((-dExp) % 1));

                    long iExp = (long)Math.Truncate(dExp);

                    while (dVal >= 10) { iExp++; dVal /= 10; }
                    while (dVal < 1) { iExp--; dVal *= 10; }

                    if (iExp >= -10 && iExp < 0)
                    {
                        string dValString = dVal.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        if (dValString[1] != '.')
                            goto returnScientific; //unexpected formatting; use default behavior.
                        else
                            return sign + "0." + new string('0', (int)((-iExp) - 1)) + dVal.ToString(System.Globalization.CultureInfo.InvariantCulture).Remove(1, 1);
                    }
                    else if (iExp >= 0 && iExp <= 10)
                    {
                        string dValString = dVal.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        if (dValString[1] != '.')
                            goto returnScientific; //unexpected formating; use default behavior.
                        else
                        {
                            dValString = dValString.Remove(1, 1);
                            if (iExp < dValString.Length - 1)
                                return sign + dValString.Substring(0, 1 + (int)iExp) + "." + dValString.Substring(1 + (int)iExp);
                            else
                                return sign + dValString + new string('0', (int)iExp - (dValString.Length - 1)) + ".0";
                        }
                    }

                returnScientific:
                    return sign + dVal.ToString(System.Globalization.CultureInfo.InvariantCulture) + "E" + (iExp >= 0 ? "+" + iExp : iExp.ToString());

                case QuadrupleStringFormat.ScientificExact:
                    if (this == Zero) return "0";
                    if (Fraction(this) == Zero && this.Exponent <= 0) //integer value that we can output directly
                        return (this.SignificandBits >= _highestBit ? "-" : "") + ((this.SignificandBits | _highestBit) >> (int)(-this.Exponent)).ToString();

                    Quad absValue = Abs(this);

                    long e = 0;
                    if (absValue < One)
                    {
                        while (true)
                        {
                            if (absValue < en18)
                            {
                                absValue.Multiply(e19);
                                e -= 19;
                            }
                            else if (absValue < en9)
                            {
                                absValue.Multiply(e10);
                                e -= 10;
                            }
                            else if (absValue < en4)
                            {
                                absValue.Multiply(e5);
                                e -= 5;
                            }
                            else if (absValue < en2)
                            {
                                absValue.Multiply(e3);
                                e -= 3;
                            }
                            else if (absValue < One)
                            {
                                absValue.Multiply(e1);
                                e -= 1;
                            }
                            else
                                break;
                        }
                    }
                    else
                    {
                        while (true)
                        {
                            if (absValue >= e19)
                            {
                                absValue.Divide(e19);
                                e += 19;
                            }
                            else if (absValue >= e10)
                            {
                                absValue.Divide(e10);
                                e += 10;
                            }
                            else if (absValue >= e5)
                            {
                                absValue.Divide(e5);
                                e += 5;
                            }
                            else if (absValue >= e3)
                            {
                                absValue.Divide(e3);
                                e += 3;
                            }
                            else if (absValue >= e1)
                            {
                                absValue.Divide(e1);
                                e += 1;
                            }
                            else
                                break;
                        }
                    }

                    //absValue is now in the interval [1,10)
                    StringBuilder result = new StringBuilder();

                    result.Append(IntegerString(absValue, 1) + ".");

                    while ((absValue = Fraction(absValue)) > Zero)
                    {
                        absValue.Multiply(e19);
                        result.Append(IntegerString(absValue, 19));
                    }

                    string resultString = result.ToString().TrimEnd('0'); //trim excess 0's at the end
                    if (resultString[resultString.Length - 1] == '.') resultString += "0"; //e.g. 1.0 instead of 1.

                    return (this.SignificandBits >= _highestBit ? "-" : "") + resultString + "e" + (e >= 0 ? "+" : "") + e;

                default:
                    throw new ArgumentException("Unknown format requested");
            }
        }

        /// <summary>
        /// Retrieves the integer portion of the quad as a string,
        /// assuming that the quad's value is less than long.MaxValue.
        /// No sign ("-") is prepended to the result in the case of negative values.
        /// </summary>
        /// <returns></returns>
        private static string IntegerString(Quad quad, int digits)
        {
            if (quad.Exponent > 0) throw new ArgumentOutOfRangeException("The given quad is larger than long.MaxValue");
            if (quad.Exponent <= -64) return "0";

            ulong significand = quad.SignificandBits | _highestBit; //make explicit the implicit bit
            return (significand >> (int)(-quad.Exponent)).ToString(new string('0', digits));
        }




        #endregion

        #region Rounding and precision functions
        /// <summary>
        /// Removes any fractional part of the provided value (rounding down for positive numbers, and rounding up for negative numbers)            
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Quad Truncate(Quad value)
        {
            value.Precision = 64; // since we truncate, we assume number will be fully precise

            if (value.Exponent <= notANumberExponent) return value;

            if (value.Exponent <= -64) return Zero;
            else if (value.Exponent >= 0) return value;
            else
            {
                //clear least significant "-value.exponent" bits that come after the binary point by shifting
                return new Quad((value.SignificandBits >> (int)(-value.Exponent)) << (int)(-value.Exponent), value.Exponent, value.Precision);
            }
        }

        /// <summary>
        /// Returns only the fractional part of the provided value.  Equivalent to value % 1.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Quad Fraction(Quad value)
        {
            if (value.Exponent >= 0) return Zero; //no fraction
            else if (value.Exponent <= -64)
            {
                if (value.Exponent == infinityExponent || value.Exponent == negativeInfinityExponent)
                    return NaN;
                else
                    return value; //all fraction (or zero or NaN)
            }
            else
            {
                //clear most significant 64+value.exponent bits before the binary point
                ulong bits = (value.SignificandBits << (int)(64 + value.Exponent)) >> (int)(64 + value.Exponent);
                if (bits == 0) return Zero; //value is an integer

                int shift = nlz(bits); //renormalize                

                return new Quad((~_highestBit & (bits << shift)) | (_highestBit & value.SignificandBits), value.Exponent - shift, value.Precision);
            }
        }


        /// <summary>
        /// Return first integer smaller or equal to input, same as Truncate for positive numbers but use lower number for negatives.
        /// </summary>
        public static Quad Floor(Quad value)
        {
            var i = Quad.Truncate(value);
            if (i == value)
                return i;
            else
                return Quad.IsNegative(value) ? i - Quad.One : i;
        }

        /// <summary>
        /// Return first integer larger  or equal to input
        /// </summary>
        public static Quad Ceiling(Quad value)
        {
            var i = Quad.Truncate(value);
            if (i == value)
                return i;
            else
                return Quad.IsNegative(value) ? i : i + Quad.One;
        }



        /// <summary>
        /// round value to closest integer, and midpoint to nearest even number
        /// </summary>
        public static Quad Round(Quad value)
        {
            var i = Quad.Truncate(value);
            var f = Quad.Fraction(value);
            if (f > 0.5)
                return i + Quad.One;
            else
                return i;
        }

        /// <summary>
        /// round value to given number of decimal places
        /// </summary>
        public static Quad Round(Quad value, int decimals)
        {
            if (decimals != 0)
            {
                var move10 = Pow(e1, decimals);
                return Round(value * move10) / move10;
            }
            else
                return Round(value);
        }


        #endregion

        #region Math functions


        public static Quad Max(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent == notANumberExponent) return NaN;
            else return qd1 > qd2 ? qd1 : qd2;
        }

        public static Quad Min(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent == notANumberExponent) return NaN;
            else return qd1 < qd2 ? qd1 : qd2;
        }

        public static Quad Abs(Quad qd)
        {
            if (qd.Exponent == negativeInfinityExponent) return PositiveInfinity;
            else return new Quad(qd.SignificandBits & ~_highestBit, qd.Exponent, qd.Precision); //clear the sign bit
        }

        public static int Sign(Quad qd)
        {
            if (qd.Exponent >= exponentLowerBound) //regular number
            {
                if ((qd.SignificandBits & _highestBit) == 0) //positive?
                    return 1;
                else
                    return -1;
            }
            else
            {
                if (qd.Exponent == zeroExponent) return 0;
                else if (qd.Exponent == infinityExponent) return 1;
                else if (qd.Exponent == negativeInfinityExponent) return -1;
                else throw new ArithmeticException("Cannot find the Sign of a Quad that is NaN");
            }
        }


        #endregion

        #region Powers and logarithms
        /// <summary>
        /// Calculates the log of a Quad in a given base. Cast from double will set lower validBits when needed
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Quad Log(Quad value, double Base)
        {
            if (value.SignificandBits >= _highestBit) return double.NaN;
            if (value.Exponent <= notANumberExponent) return specialDoubleLogTable[(int)(value.Exponent - zeroExponent)];
            if (Base == 2)
                return Log2(value);
            if (Base == 10)
                return Log10(value);
            return Math.Log(value.SignificandBits | _highestBit, Base) + value.Exponent / Math.Log(Base, 2);
        }
        public static Quad Log(Quad value, Quad Base) => Log(value, (double)Base); // reduce precision


        /// <summary>
        /// Calculates the natural log (base e) of a Quad.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Quad Log(Quad value)
        {
            if (value.SignificandBits >= _highestBit) return double.NaN;
            if (value.Exponent <= notANumberExponent) return specialDoubleLogTable[(int)(value.Exponent - zeroExponent)];
            return Math.Log(value.SignificandBits | _highestBit) + value.Exponent * 0.69314718055994530941723212145818;
        }

        /// <summary>
        /// Calculates the log (base 2) of a Quad, in full precision if it i spower of 2, otherwise use double 52-bit precision            
        /// </summary>
        public static Quad Log2(Quad value)
        {
            if (value.SignificandBits >= _highestBit) return NaN;
            if (value.Exponent <= notANumberExponent) return specialDoubleLogTable[(int)(value.Exponent - zeroExponent)];
            if (value.SignificandBits == 0) return new Quad(value.Exponent + 63, 0);
            return Math.Log2(value.SignificandBits | _highestBit) + value.Exponent;
        }


        /// <summary>
        /// Calculates the log (base 10) of a Quad.            
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Quad Log10(Quad value)
        {
            if (value.SignificandBits >= _highestBit) return double.NaN;
            if (value.Exponent <= notANumberExponent) return specialDoubleLogTable[(int)(value.Exponent - zeroExponent)];
            //var tmp = Math.Log10((double)value);
            //return Math.Log10(value.SignificandBits | _highestBit) + value.Exponent * new Quad(1882674540191938456,-65); // 0.30102999566398119521373889472449;
            var a = Math.Log10(value.SignificandBits | _highestBit);
            var l2= new Quad(1882674540191938456ul, -65); // 0.30102999566398119521373889472449
            var b = value.Exponent * l2;
            var r = a + b;
            return r;
        }

        /// <summary>
        /// Raise a Quad to a given exponent.  Pow returns 1 for x^0 for all x >= 0.  An exception is thrown
        /// if 0 is raised to a negative exponent (implying division by 0), or if a negative value is raised
        /// by a non-integer exponent (yielding an imaginary number).
        /// </summary>
        /// <param name="value"></param>
        /// <param name="exponent"></param>
        /// <returns></returns>
        /// <remarks>Internally, Pow uses Math.Pow.  This effectively limits the precision of the output to a double's 53 bits.</remarks>
        public static Quad Pow(Quad value, double exponent)
        {
            if (value.Exponent <= notANumberExponent)
            {
                //check NaN
                if (value.Exponent == notANumberExponent || double.IsNaN(exponent)) return NaN;

                //anything ^ 0 == 1
                if (exponent == 0) return One;

                //0 ^ y
                if (value.Exponent == zeroExponent)
                    return exponent < 0 ? PositiveInfinity : Zero;

                //PositiveInfinity ^ y
                if (value.Exponent == infinityExponent)
                    return exponent < 0 ? Zero : PositiveInfinity;

                if (value.Exponent == negativeInfinityExponent)
                    return Math.Pow(double.NegativeInfinity, exponent); //lots of weird special cases
            }

            if (double.IsNaN(exponent)) return NaN;
            if (double.IsInfinity(exponent))
            {
                if (value < -2)
                    return Math.Pow(-2, exponent);
                else if (value > 2)
                    return Math.Pow(2, exponent);
                else
                    return Math.Pow((double)value, exponent);
            }

            if (exponent == 0) return One;

            if (value.SignificandBits >= _highestBit && exponent % 1 != 0)
                return NaN; //result is an imaginary number--negative value raised to non-integer exponent
            // is power integer value?
            if (Math.Abs(exponent) < long.MaxValue)
            {
                long expInt = (long)exponent;
                if (expInt == exponent)
                {
                    // is this 2^power ? That can be done in full precision
                    if ((value.SignificandBits & _notHighestBit) == 0)
                    {
                        long qExp = value.Exponent + 63;
                        var dExp = expInt * (double)qExp - 63;
                        ulong sign = value.SignificandBits >= _highestBit ? (expInt % 2 == 0 ? 0 : _highestBit) : 0; // result sign is negative only if value negative and expInt is odd
                        if (dExp > exponentUpperBound)
                            return sign >= _highestBit ? NegativeInfinity : PositiveInfinity; //overflow
                        else if (dExp < exponentLowerBound)
                            return Zero; // underflow
                        else
                            return new Quad(sign, qExp * expInt - 63, value.Precision);
                    }
                    // is this 10^power? That can use tables to be done in full precision
                    if (value == e1)
                        return power10(expInt);

                }
            }
            // otherwise generic Pow using Math.Pow, 53-bit precision
            var qSignificand = new Quad(value.SignificandBits, -63, 53);
            double resultSignificand = Math.Pow((double)qSignificand, exponent);
            double resultExponent = (value.Exponent + 63) * exponent; //exponents multiply
            resultSignificand *= Math.Pow(2, resultExponent % 1); //push the fractional exponent into the significand
            if (double.IsInfinity(resultSignificand)||(resultSignificand==0)) // if above is out of range, try big pow
                return PowBig(value, exponent);
            Quad result = (Quad)resultSignificand;
            result.Exponent += (long)Math.Truncate(resultExponent);
            result.Precision = 53;
            return result;
        }


        public static Quad Pow(Quad value, Quad exponent) => Pow(value, (double)exponent);


        /// <summary>
        /// Raise a Quad to a given exponent, but previously logarithmically normalize exponent to achieve larger range.
        /// Sacrifice bit of precision and performance, to achieve higher exponent range.
        public static Quad PowBig(Quad value, double exponent)
        {
            if (value.Exponent <= notANumberExponent)
            {
                //check NaN
                if (value.Exponent == notANumberExponent || double.IsNaN(exponent)) return NaN;

                //anything ^ 0 == 1
                if (exponent == 0) return One;

                //0 ^ y
                if (value.Exponent == zeroExponent)
                    return exponent < 0 ? PositiveInfinity : Zero;

                //PositiveInfinity ^ y
                if (value.Exponent == infinityExponent)
                    return exponent < 0 ? Zero : PositiveInfinity;

                if (value.Exponent == negativeInfinityExponent)
                    return Math.Pow(double.NegativeInfinity, exponent); //lots of weird special cases
            }

            if (double.IsNaN(exponent)) return NaN;
            if (double.IsInfinity(exponent))
            {
                if (value < -2)
                    return Math.Pow(-2, exponent);
                else if (value > 2)
                    return Math.Pow(2, exponent);
                else
                    return Math.Pow((double)value, exponent);
            }

            if (exponent == 0) return One;

            if (value.SignificandBits >= _highestBit && exponent % 1 != 0)
                return NaN; //result is an imaginary number--negative value raised to non-integer exponent
            // value= a= am*2^ae
            ulong am = value.SignificandBits | _highestBit;
            long ae = value.Exponent;
            // a^x=2^y, y=?  y=x*log2(a)
            double logA = Math.Log(am, 2) + ae;
            double y = exponent * logA;
            if (Math.Abs(y) < long.MaxValue)
            {
                long resE = (long)y;
                double me = y - resE;
                double resM = Math.Pow(2, me);
                // form result resM * 2^ resE
                var res = new Quad(resM);
                res.Exponent += resE;
                res.Precision = 53;
                return res;
            }
            else
            {
                // exponent is too large, return as if it was infinite
                if (IsZero(value) || (value == One)) return value; // 0^x=0 and 1^x=1 regardless of x
                // assume exponent always even (since it is too large, bit 0 would be 0 anyway ), so that -value^exp is positive
                // 0.1^-inf=+inf ; 0.1^+inf=0; 10^-inf=0 ; 10^+inf=+inf
                int expSign = Math.Sign(exponent);
                int valSmall = Abs(value) < One ? -1 : +1;
                return expSign * valSmall > 0 ? PositiveInfinity : Zero; 
            }
        }


        public static Quad Sqrt(Quad value) => Quad.Pow(value, 0.5);
        public static Quad Exp(Quad value) => Quad.Pow(Quad.E, value);
        public static Quad Exp2(Quad value) => Quad.Pow(2, value);
        public static Quad Exp10(Quad value) => Quad.Pow(10, value);


        #endregion

        #region Trigonometric functions

        // use moduo 2*pi for large numbers
        private static Quad doTrig2pi(Func<double, double> MathTrig, Quad a) 
        {
            // if number is larger that double range, use moduo 2*PI
            double da = fitDouble(a) ? (double)a : (double)(a % PIx2);
            // cast from double will clip precision to 52+1
            return new Quad(MathTrig(da));
        }


        public static Quad Sin(Quad a) => doTrig2pi(Math.Sin, a);
        public static Quad Sinh(Quad a) => new Quad(Math.Sinh((double)a));
        public static Quad Asin(Quad a) => new Quad(Math.Asin((double)a));
        public static Quad Asinh(Quad a) => new Quad(Math.Asinh((double)a));
        public static Quad Cos(Quad a) => doTrig2pi(Math.Cos, a);
        public static Quad Cosh(Quad a) => new Quad(Math.Cosh((double)a));
        public static Quad Acos(Quad a) => new Quad(Math.Acos((double)a));
        public static Quad Acosh(Quad a) => new Quad(Math.Acosh((double)a));
        public static Quad Tan(Quad a) => doTrig2pi(Math.Tan, a);
        public static Quad Tanh(Quad a) => new Quad(Math.Tanh((double)a));
        public static Quad Atan(Quad a) => new Quad(Math.Atan((double)a));
        public static Quad Atanh(Quad a) => new Quad(Math.Atanh((double)a));
        public static Quad Atan2(Quad a, Quad b) => new Quad(Math.Atan2((double)a, (double)b));

        #endregion



        // below this are same for any IFloatNumber implementation, ie Double / Quad / MPFR , and should be copy-pasted among those classes

        #region IFloatNumber statics that use generics

        public static bool isInt(thisType x) => Number.isInt(x);

        public static thisType Gamma(thisType a) => Number.Gamma(a);
        public static thisType Factorial(thisType a) => Number.Factorial(a);
        public static thisType DoubleFactorial(thisType a) => Number.DoubleFactorial(a);
        public static thisType nCr(thisType n, thisType r) => Number.nCr(n, r);
        public static thisType nPr(thisType n, thisType r) => Number.nPr(n, r);

        #endregion

        #region Number overrides for instance methods using own static methods [ OPTIONAL , commented out ]
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




    /// <summary>
    /// Determines the format of the string produced by Quad.ToString(QuadrupleStringFormat).
    /// ScientificApproximate is the default.
    /// </summary>
    public enum QuadrupleStringFormat
    {
        /// <summary>
        /// Obtains the quadruple in scientific notation.  Only ~52 bits of significand precision are used to create this string.
        /// </summary>
        ScientificApproximate,

        /// <summary>
        /// Obtains the quadruple in scientific notation with full precision.  This can be very expensive to compute and takes time linear in the value of the exponent.
        /// </summary>
        ScientificExact,

        /// <summary>
        /// Obtains the quadruple in hexadecimal exponential format, consisting of a 64-bit hex integer followed by the binary exponent,
        /// also expressed as a (signed) 64-bit hexadecimal integer.
        /// E.g. ffffffffffffffff*2^-AB3
        /// </summary>
        HexExponential,

        /// <summary>
        /// Obtains the quadruple in decimal exponential format, consisting of a 64-bit decimal integer followed by the 64-bit signed decimal integer exponent.
        /// E.g. 34592233*2^34221
        /// </summary>
        DecimalExponential
    }


}
