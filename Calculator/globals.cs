
// NUMBER RANGES :
// float : 32 bit, m=23+1b e=8b, ~ 6d 10^38
// double : 64 bit, m=52+1b e=11b, ~ 15d 10^308
// extended : 80 bit, m=63+1b e=16b, ~ 18d 10^4932
// Decimal : 128 bit, m=96b e=31b!, ~ 28d 10^28
// _float128 : 128 bit, m=122+1b e=15b, ~ 33d 10^4932
// Quad : 128 bit, m=63+1b e=64b, ~ 18d 10^2Exa(19d)
// MPFR : X bit, m=2..inf b e=32b, ~ ?d 10^600M(9d)
// MPFR - '96 bit'+128b, m=64 b e=32b s=32b, rest=96b, ~ 18d 10^600M(9d)
// MPFR - '160 bit'+128n, m=128 b e=32b s=32b, rest=96b, ~ 38d 10^600M(9d)

// Relative SPEEDS:   
//   Quad    :  30x slower than double
//  dynamic  :  30x slower than double
//   MPFR    : 600x slower than double , 20x slower than Quad
//   mcValue : 500x slower than double , 20x slower than Quad, 3x slower than MPFR

// When dynamic wrap other base types:
//   - it is respectivelly 25x/2x/10% slower that direct implementations of double/Quad/MPFR
//   dyn double : 25x double, about as fast as native Quad
//   dyn Quad   : 60x double, twice slower than native Quad
//   dyn MPFR   : 650x double, 10x slower than dynQuad and 20x slower than dynDouble

// When mcValue wrap other base types:
//   - it is respectivelly 500/20/4 times slower that direct implementations of double/Quad/MPFR
//   Quad : 1x ~ about same speed as wrapped double
//   MPFR : 5x ~ around five times slower than wrapped double

// Conclusion:
//   double : +fastest  , +most functions supported,        +precise,       -small exponent (10^308)
//   Quad   : +fast     , -many functions not supported,    -imprecise,     +super large exponent (10^19digits)
//   MPFR   : -slow(x5) , +all functions supported,         +precise,       +large exponent (10^9digits)

// To use desired float number as a base for Calculator Notepad:
//   - uncomment 3 'global using' lines for that number class
//   - comment other 'global using' lines

// MPFR floats: 64 bits mantissa, 32bits exponent, all native math functions but 5x slower
//global using real = Numbers.MPFR;
//global using MATH = Numbers.MPFR;
//global using NM = Numbers.MPFR;

// Quad floats: 64 bits mantissa, 64bits exponent, some math functions are less precise double variants
//global using real = Numbers.Quad;
//global using MATH = Numbers.Quad;
//global using NM = Numbers.Quad;

// double floats: 53 bits mantissa, 11bits exponent, low exponent up to 10^308
//global using real = System.Double;
//global using MATH = System.Math;
//global using NM = CalculatorNotepad.nm;




// other global usings
global using Extensions;

namespace CalculatorNotepad
{
    public static class globals
    {
    }
}
