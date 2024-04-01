// This class is INDEPENDENT of other nmUnits, and can be used separately

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Numbers;

namespace CalculatorNotepad
{
    public partial class nm
    {


        #region UTILITY functions

        public static readonly string TypeName = "Double";  // name of default floating point type used


        //*** UTILITY functions
        public const double epsDbl = 1e-15; // rounding value for double type, numbers under this are considered false(zero)
        public static Random rnd = new Random();

        public static Func<bool> isTimeout = noTimeout; // rebind to outside timeout check if needed

        static bool noTimeout() { return false; }


        #endregion



        #region Double extensions to match Quad/MPFR methods

        /*
        
         
         
        */



        // testing normal double values
        public static bool IsNegative(double a) => a<0;
        public static bool IsZero(double a) => a==0;
        public static bool IsNumber(double a) => !(double.IsNaN(a) || double.IsInfinity(a)); // not NaN or Inf
        public static bool IsRegular(double a) => !(double.IsNaN(a) || double.IsInfinity(a) || IsZero(a)); // not NaN or Inf or Zero


        // is double = true value (!= 0), but very small numbers are also false
        public static bool isT(double x)
        {
            return (Math.Abs(x) > epsDbl);
        }

        // is double integer, allowing for epsilon
        public static bool isInt(double x)
        {
            return (Math.Abs(x) % 1) <= epsDbl;
        }

        /// <summary>
        /// compare, but ignore epsilon (very small differences) 
        /// </summary>
        public static int CMP(double a, double b)
        {
            double diff = Math.Abs(a - b);
            // test if they are very close to each other, within double error margin
            if (diff <= epsDbl) return 0;
            // otherwise return depending on which one is larger
            return a > b ? 1 : -1;
        }

        // at what digit two numbers differ. Larger = better precision
        public static double diffDigit(double a, double b)
        {
            if (a == b) return double.PositiveInfinity;
            double ratio;
            if ((a != 0) && (b != 0))
            {
                double diff = Math.Abs(a - b);
                double big = Math.Abs(a) > Math.Abs(b) ? Math.Abs(a) : Math.Abs(b);
                ratio = diff / big;
            }
            else
            {
                ratio = Math.Abs(a + b);
            }
            double digit = -Math.Log10(ratio);
            return digit;
        }

        private const int mantissaBits = 52; // number of bits in mantissa for underlying float type (double here). Used to determine number of correct decimal digits
        /// <summary>
        /// return max number of mantissa valid digits for given double in given base ( based on 52+1 bit mantissa )
        /// </summary>
        public static int validDigitsInBase(double d, int Base)
        {
            if (Base == 2)
                return mantissaBits;
            else
                return (int)Math.Floor((mantissaBits / Math.Log2(Base)));
        }


        // n!,  int and float factorial
        public static double Factorial(double x)
        {
            // detect if this is integer
            if (isInt(x))
            {
                if (x < 0)
                    throw new ArgumentException("Integer argument of Factorial can not be negative !");
                if (x > 300) return double.PositiveInfinity; // 300!==3e614, and max double is 2e308
                int xi = (int)Math.Round(x);
                double res = 1;
                for (int i = 2; i <= xi; i++)
                    res *= i;
                return res;
            }
            else
            {
                // it is float, so use Gamma(x+1) . It should work for negative float values
                return Gamma(x + 1);
            }
        }

        // for float factorial
        public static double Gamma(double z)
        {
            var p = new double[]
            {
                 676.5203681218851
                ,-1259.1392167224028
                ,771.32342877765313
                ,-176.61502916214059
                ,12.507343278686905
                ,-0.13857109526572012
                ,9.9843695780195716e-6
                ,1.5056327351493116e-7
            };
            if (z < 0.5)
            {
                var y = Math.PI / (Math.Sin(Math.PI * z) * Gamma(1 - z)); // reflection, since z<0.5 not allowed 
                return y;
            }
            else
            {
                z -= 1;
                double x = 0.99999999999980993;
                for (int i = 0; i < p.Length; i++)
                    x += p[i] / (z + i + 1);
                var t = z + p.Length - 0.5;
                var y = Math.Sqrt(2 * Math.PI) * Math.Pow(t, z + 0.5) * Math.Pow(Math.E, -t) * x;
                return y;
            }
        }



        #endregion



        #region DOUBLE custom MATH functions
        //**** custom MATH functions

        /// <summary>
        /// How many COMBINATIONS or ways to choose 'r' out of 'n', when order does NOT matter
        /// </summary>
        public static double nCr(double n, double r)
        {
            if (r > n) return 0;
            // standard formula, nCr = n!/r!/(n-r)!   , support fractions
            double res = Factorial(n) / Factorial(r) / Factorial(n - r);
            // if above overflowed, try to optimize
            if (double.IsNaN(res) || double.IsInfinity(res))
            {
                if (n - r > r) r = n - r; // so that A is smaller
                var k = n - r+1;
                double i = 1;
                res = 1;
                while (k <= n && !double.IsNaN(res) && !double.IsInfinity(res)) 
                {
                    res *= k / i;
                    k++;
                    i++;
                }
            }
            return res;
        }

        /// <summary>
        /// How many PERMUTATIONS or ways to choose 'r' out of 'n', when order DOES matter, 
        /// </summary>
        public static double nPr(double n, double r)
        {
            if (r > n) return 0;
            // standard formula, nPr= n!/(n-r)!   , support fractions
            double res = Factorial(n) / Factorial(n-r);
            // if above overflowed, try to optimize with integer calc
            if (double.IsNaN(res) || double.IsInfinity(res))
            {
                res = 1;
                for (int i = (int)Math.Round(r+1); i <= n; i++)
                    res *= i;
            }
            return res;
        }


        // n!! , int double factorial
        public static double DoubleFactorial(double x)
        {
            // detect if this is integer
            if (isInt(x))
            {
                if (x > 300) return double.PositiveInfinity; // 300!==3e614, and max double is 2e308
                int xi = (int)Math.Round(x);
                double res = 1;
                if (xi >= 0)
                    while (xi > 0)
                    {
                        res *= xi;
                        xi -= 2;
                    }
                else
                if (xi % 2 != 0)
                {
                    while (xi < -1)
                    {
                        xi += 2;
                        res *= xi;
                    }
                    res = 1 / res;
                }
                else
                    throw new ArgumentException("DoubleFactorial negative arguments must be odd !");
                return res;
            }
            else
            {
                // not defined for float
                throw new ArgumentException("DoubleFactorial require integer arguments !");
            }
        }


        // harmonic( ( n [,coverage=100%] )  - return harmonic number Hn= sum(1/i) for i=1..n = 1/N+1/(N-1)+...1/1
        //     N*harmonic(N) = expected tries to complete all set (all different coupons or get all six dice numbers (N=6) ...)
        //                     assume probability to get each of N different set items is equal, 1/N
        // when coverage != 100% , return partial harmonic number Hnp= sum(1/i) for i= (1-coverage)*n+1..n = Hn-H(1+(1-coverage)*n)
        //     N*harmonic(N, 30%) = expected tries to complete 30% of a set
        public static double Harmonic(double n, double coverage=1)
        {
            double res = 0;
            double m= Math.Round(1 + (1 - coverage) * n);
            if (m < 1) m = 1;
            if (n < 1000000) {
                // actual sum of all elements
                for (int i = (int)m; i <= n; i++)
                    res += 1 / (double)i;
            }
            else
            {
                // approximation, Hn ~ ln(n)+y+1/2n+..., where y= 0.5772156649
                // and H(n,m)= H(n)-H(m-1)
                double y = 0.5772156649;
                res = Math.Log(n) + 0.5 / n + y;
                m--;
                if (m>0)
                    res-= Math.Log(m) + 0.5 / m + y;
            }
            return res;
        }



        // inverse hyperbolic functions, fromj https://en.wikipedia.org/wiki/Inverse_hyperbolic_functions
        public static double asinh(double x)
        {
            return Math.Log(x + Math.Sqrt(x * x + 1));
        }
        public static double acosh(double x)
        {
            return Math.Log(x + Math.Sqrt(x * x - 1));
        }
        public static double atanh(double x)
        {
            return Math.Log((1+x)/(1-x))/2;
        }


        #endregion


        #region INTEGER custom MATH functions
        // functions that take only integers as arguments, and/or return integer results only (with their mcValue wrappers)
        // use long as Int64 , and mcValue.Long when 64bits needed


        // GCD = greater common denominator of two integers
        public static int gcd(int a, int b)
        {
            while (b != 0)
            {
                int t = b;
                b = a % b;
                a = t;
            }
            return a;
        }
        // GCD with multiple integers in list
        public static int gcd(List<int> list)
        {
            if ((list == null) || (list.Count < 1)) return 0; // questionable, or exception
            int a = list[0];
            if (list.Count == 1) return a;
            int b = list[1];
            int res = gcd(a, b);
            // if more than two arguments, gcd(a,b,c)= gcd( gcd(a,b) , c )...
            for (int i = 2; i < list.Count; i++)
                res = gcd(res, list[i]);
            return res;
        }

        // LCM =  Least Common Multiplier 
        public static int lcm(int a, int b) // two integers
        {
            if (a * b == 0) return 0;
            return Math.Abs(a * b) / gcd(a, b);
        }
        public static int lcm(List<int> v) // vector of integers
        {
            if (v.Count < 1) return 0; // questionable
            if (v.Count == 1) return v[0];
            int res = lcm(v[0], v[1]);
            // if more than two arguments, lcm(a,b,c)= lcm( lcm(a,b) , c )...
            for (int i = 2; i < v.Count; i++)
                res = lcm(res, v[i]);
            return res;
        }


        #endregion


        #region LAMBDA functions


        // find lowest INDEX where boolFunc(i) == true:  binarySearch ( (i)=> boolFunc(i) [,i_start=-maxInt [,i_end=+maxInt    [,flags=0]]] );

        //    Return integer 'i' such that boolFunc(i)==true and boolFunc(i-1)==false
        //    Precondition: boolFunc must be monotonous(false,...false,true,...true)
        //    It searches within start..end range, and if not found it raises an exception. Default range is INT32 range, but boundaries can be larger if needed.
        //    Using 'flag' bitwise parameters can change behaviour when not found:
        //        - 1: extend 'end' of range (useful when we do not know actual range)
        //        - 2: do not raise exception, return 'start'-1 when not found
        //    Examples:
        //        binarySearch((i)=> i^3 >= 4*i^2 , 3,10 ) : 4 
        //        v = vec(2, 5, 5, 7, 9, 11, 13); binarySearch((i)=> v[i] >= 7 , 0, vLen(v)-1 ) : 3 
        public static int binarySearch(Func<int,bool>lambda, int range_start= int.MinValue, int range_end = int.MaxValue, int flags=0)
        {
            bool extendEnd = (flags & 1) != 0;
            bool dontThrow = (flags & 2) != 0;
            int initStart = range_start;
            if (range_start > range_end)
                throw new ArgumentException("Invalid range for binarySearch, start > end !");
            bool? endValue = null;
            bool? startValue = null;
            // if end can be extended, do it at start
            if (extendEnd)
            {
                bool eChk = false;
                while ((range_end < 0) && !(eChk = lambda(range_end))) { range_start = range_end; startValue = false; range_end /= 2; }
                if ((range_end == 0) && !(eChk = lambda(range_end))) { range_start = range_end; startValue = false; range_end = 1; }
                while ((range_end < int.MaxValue / 2) && !(eChk = lambda(range_end))) { range_start = range_end; startValue = false; range_end *= 2; }
                if (eChk) endValue = true;
            }
            // now perform search
            while (range_end - range_start > 1)
            {
                int x = (int)(range_start + range_end) / 2;
                bool xValue = lambda(x);
                if (xValue)
                {
                    range_end = x;
                    endValue = xValue;
                }
                else
                {
                    range_start = x;
                    startValue = xValue;
                }
                if (isTimeout()) throw new ArgumentException("ERR:Timeout");
            }
            // calculate boundary if not calced before
            if (startValue == null) startValue = lambda(range_start);
            if (endValue == null) endValue = lambda(range_end);
            // confirm that it is found
            if ((startValue == false) && (endValue == true)) return range_end;
            if ((startValue == true) && (range_start == initStart)) return range_start;
            // if not found and dontThrow flag is set, return special value
            if (dontThrow) return initStart - 1;
            // otherwise exception - impossible to find
            throw new ArgumentException("binarySearch could not find - either there is no 'true' value in range, or boolean function is not monotonous ([false,...false,]true,...true) !");
        }




        public delegate double FunctionOfOneVariable(double x);

        // returns X that satisfy f(X)=Y_target
        // find root of specified function 'f(x)' if Y_target is not specified (==0)
        public static double find_root
                    (
                        FunctionOfOneVariable func,
                        double Y_target = 0.0,
                        double tolerance = 1e-12,
                        double left = double.NegativeInfinity,
                        double right = double.PositiveInfinity
                    )
        {
            // extra info that callers may not always want
            int iterationsUsed;
            double errorEstimate;
            // make function zero-based, and return NaN for exceptions
            FunctionOfOneVariable f = delegate (double x) {
                try
                {
                    return func(x) - Y_target;
                }
                catch
                {
                    return double.NaN;
                }
            };

            // guess brackets if not specified
            (left,right)=guessBrackets(f, left, right);
            // search for root
            return Brent(f, left, right, tolerance, out iterationsUsed, out errorEstimate);
        }

        // my custom algorithm to guess brackets (x1,x2) that enclose x for which f(x)==0
        public static void guessBracketsOld(FunctionOfOneVariable f, double Y_target, ref double left, ref double right)
        {
            bool isDefined(double x) => (!double.IsNaN(x)) && (!double.IsInfinity(x));
            if (isDefined(left) && isDefined(right))
                return;
            left = -1;
            right = +1;
            int tries = 20;
            do
            {
                tries--;
                double yl = f(left)- Y_target;
                double yr = f(right)- Y_target;
                if (yl * yr > 0)
                {
                    if (yr * Math.Sign(yr - yl) > 0)
                    {
                        right = left;
                        left = 2 * left;
                    }
                    else
                    {
                        left = right;
                        right = 2 * right;
                    }
                }
                else
                    tries = 0; // valid bracket, exit

            } while (tries > 0);
        }

        // my custom algorithm to guess brackets (x1,x2) that enclose x for which f(x)==0
        public static (double left,double right) guessBrackets(FunctionOfOneVariable f, double left, double right)
        {
            // -- inner functions
            // iteration counter, to prevent infinite searches
            int iterCount = 100;
            // is double value defined?  not NaN or Infinity ( or eg Complex etc if needed )
            bool isDefined(double x) => (!double.IsNaN(x)) && (!double.IsInfinity(x));
            // did we find correct brackets, enclosing zero ( or we did not find but max iter count reached ) - in either case, return with current left/right
            bool isDone() => (isDefined(left) && isDefined(right) && (Math.Sign(left) != Math.Sign(right))) || (iterCount<=0);
            // if one bracket is defined, find other. Return true if both correctly found
            void findOther()
            {
                // function to find one bracket, if other is defined
                double findOtherBracket(ref double bracket, double delta)
                {
                    var oldY = f(bracket);
                    while (iterCount > 0)
                    {
                        iterCount--;
                        double res = bracket + delta;
                        var y = f(res);
                        if (!isDefined(y))
                            delta /= 2;
                        else if ((Math.Sign(y) == Math.Sign(oldY))&&(Math.Sign(y)!=0))
                        {
                            bracket = res;
                            oldY = y;
                            delta *= 2;
                        }
                        else
                            return res;
                    }
                    return double.NaN;
                }
                // if left is defined, find right
                if (isDefined(left)&&!isDefined(right))
                    right = findOtherBracket(ref left, +1);
                // if right is defined, find left
                if (isDefined(right) && !isDefined(left))
                    left = findOtherBracket(ref right, -1);
            }
            // Compare if two values are same, support NaN,+/-Inf
            bool isSameVal(double y1, double y2)
            {
                if (double.IsNaN(y1) && double.IsNaN(y2)) return true; // two NaNs are 'same value'
                return y1==y2; // this will be true for +Inf == +Inf, false for +Inf==-Inf or +Inf=123
            }
            // find two points in given direction with different Y values, return NULL if not found. Support NaN,+/-Inf regions
            RectangleD findDifValues(double x0, double dx)
            {
                int maxIter = 100; // separate iteration limit from main function
                var y0 = f(x0);
                var x = x0;
                do
                {
                    maxIter--;
                    x += dx;
                    var y = f(x);
                    if (isSameVal(y, y0))
                    {
                        x0 = x;
                        dx *= 2;
                    }
                    else
                    {
                        // return smaller X first/left, in x1/y1 member
                        if (dx > 0)
                            return new RectangleD(x0, y0, x, y);
                        else
                            return new RectangleD(x, y, x0, y0);
                    }
                } while (maxIter > 0);
                // not found, return null
                return null;
            }
            // Bisect segment to find two different but comparable Y values. It starts with two different values that are not neccessary comparable.
            // eg start with (NaN,123) and find (15,123) or (-Inf, -19)
            // Return:  Change Left OR Right if found, and return true
            bool bisectSegment(RectangleD seg)
            {
                if (seg == null)
                    return false;
                int maxIter = 100; // also separate limit
                // search/bisect as long as one of Y values is NaN ( uncomparable)
                do
                {
                    maxIter--;
                    // if we found two comparable and different Y values, and at least one is not infinite, decide which direction
                    if ( !double.IsNaN(seg.y1) && !double.IsNaN(seg.y2) && (!double.IsInfinity(seg.y1) || !double.IsInfinity(seg.y2)) )
                    {
                        // if different signs (or both signs are zero), update both left and right - even if one is infinite
                        if ( (Math.Sign(seg.y1) != Math.Sign(seg.y2)) || (Math.Sign(seg.y1) == 0) )
                        {
                            left = seg.x1;
                            right = seg.x2;
                            return true; // notify that brackets were modified
                        }
                        // otherwise determine direction 
                        // dy= positive if rising function, ie y2>y1
                        double dy = 0; 
                        if (double.IsInfinity(seg.y2))
                            dy = Math.Sign(seg.y2);
                        else if (double.IsInfinity(seg.y1))
                            dy = -Math.Sign(seg.y1);
                        else
                            dy= Math.Sign(seg.y2-seg.y1);
                        // both Ys are either above or below zero - in combo with 'dy' it decide which bracket to update
                        if (Math.Sign(seg.y1) * dy > 0)  
                            // they are both above zero and y1 is smaller (rising func), or both below zero and y1 is larger (falling func)
                            right = seg.x1; // in both cases, zero is to the left of both points, so we choose x1 as right bracket
                        else
                            // they are both above zero and y1 is larger (falling func), or both below zero and y1 is smaller (raising func)
                            left = seg.x2; // in both cases, zero is to the right of both points, so we choose x2 as left bracket
                        // notify that we modified one bracket
                        return true;
                    }
                    else
                    {
                        // next bisect
                        var x = (seg.x1 + seg.x2) / 2;
                        var y = f(x);
                        if (isSameVal(y, seg.y1))
                        {
                            // y is same as left value (y1), so move y1 to y
                            seg.x1 = x;
                            seg.y1 = y;
                        }
                        else if (isSameVal(y, seg.y2))
                        {
                            // y is same as right value (y2), so move y2 to y
                            seg.x2 = x;
                            seg.y2 = y;
                        }
                        else
                        {
                            // different to both, so keep it, and keep as other value whichever was not NaN
                            if (!double.IsNaN(seg.y1))
                            {
                                seg.x2 = x;
                                seg.y2 = y;
                            }
                            else
                            {
                                seg.x1 = x;
                                seg.y1 = y;
                            }
                        }
                    }
                } while(maxIter > 0);
                return false;
            }
            // if no bracket is defined, find at least one ( either left or right or both) with actual value that is not NaN or Inf
            // return: updated Left and/or Right
            void findOneBracket()
            {
                double dx = 1.57; // best not to be integer
                var dif = findDifValues(dx/2, dx); // find two points with any different values to the righ ( one can be NaN )
                if (dif == null)
                    dif = findDifValues(-dx/2, -dx); // if not found toward right, try toward left
                if (dif == null)
                    return; // we could not find different points in any direction, so do not change left/right and just return !
                // if one of them is NaN, need to test additional segment
                RectangleD dif2 = null;
                if (double.IsNaN(dif.y1))
                    dif2= findDifValues(dif.x2, dx); // Nan, y2 =>  x3>x2,?? so  NaN,y2 | y3,??
                else if (double.IsNaN(dif.y2))
                    dif2 = findDifValues(dif.x1, -dx); // y1, NaN =>  ??,x0<x1 so  ??,y0 | y1,NaN
                // now bisect those segments to find both non-NaN, and thus comparable, and thus able to update either Left or Right
                if (!bisectSegment(dif))
                    bisectSegment(dif2);
            }


            // --- finding brackets
            // first check if one or both brackets are already defined, and try to find other one
            findOther();
            // if we have both correct brackets ( or used all available iterations) return
            if (isDone())
                    return (left, right);
            // otherwise, we do not have any bracket, so try to find initial bracket, either left or right (or both)
            findOneBracket();
            // and try again to find other bracket
            findOther();
            // return result, regardless if left/right are correctly found or still infinite
            return (left, right);
        }


        // Brent's algorithm to find root of function within brackets (modified for stepped/INT functions)
        public static double Brent
                        (
                            FunctionOfOneVariable f,
                            double left,
                            double right,
                            double tolerance,
                            out int iterationsUsed,
                            out double errorEstimate
                        )
        {
            int maxIterations = 50;
            if (tolerance <= 0.0)
            {
                string msg = string.Format("Tolerance must be positive. Recieved {0}.", tolerance);
                throw new ArgumentOutOfRangeException(msg);
            }

            errorEstimate = double.MaxValue;

            // Implementation and notation based on Chapter 4 in
            // "Algorithms for Minimization without Derivatives"
            // by Richard Brent.

            double c, d, e, fa, fb, fc, tol, m, p, q, r, s;

            // set up aliases to match Brent's notation
            double a = left; double b = right; double t = tolerance;
            iterationsUsed = 0;

            fa = f(a);
            fb = f(b);

            if (fa * fb > 0.0)
            {
                throw new ArgumentException("FindRoot error. Function must be above zero on one end and below zero on other end. f("+left+") = "+fa+" , f("+right+") = "+fb);
            }

        label_int:
            c = a; fc = fa; d = e = b - a;
        label_ext:
            if (Math.Abs(fc) < Math.Abs(fb))
            {
                a = b; b = c; c = a;
                fa = fb; fb = fc; fc = fa;
            }

            iterationsUsed++;

            tol = 2.0 * t * Math.Abs(b) + t;
            errorEstimate = m = 0.5 * (c - b);
            if (Math.Abs(m) > tol && fb != 0.0) // exact comparison with 0 is OK here
            {
                // See if bisection is forced
                if (Math.Abs(e) < tol || Math.Abs(fa) <= Math.Abs(fb))
                {
                    d = e = m;
                }
                else
                {
                    s = fb / fa;
                    if (a == c)
                    {
                        // linear interpolation
                        p = 2.0 * m * s; q = 1.0 - s;
                    }
                    else
                    {
                        // Inverse quadratic interpolation
                        q = fa / fc; r = fb / fc;
                        p = s * (2.0 * m * q * (q - r) - (b - a) * (r - 1.0));
                        q = (q - 1.0) * (r - 1.0) * (s - 1.0);
                    }
                    if (p > 0.0)
                        q = -q;
                    else
                        p = -p;
                    s = e; e = d;
                    if (2.0 * p < 3.0 * m * q - Math.Abs(tol * q) && p < Math.Abs(0.5 * s * q))
                        d = p / q;
                    else
                        d = e = m;
                }
                a = b; fa = fb;
                if (Math.Abs(d) > tol)
                    b += d;
                else if (m > 0.0)
                    b += tol;
                else
                    b -= tol;
                if (iterationsUsed >= maxIterations)
                    return b;

                fb = f(b);
                if ((fb > 0.0 && fc > 0.0) || (fb <= 0.0 && fc <= 0.0))
                    goto label_int;
                else
                    goto label_ext;
            }
            else
            {
                // round result depending to error estimate
                int digits = (int)Math.Round(-Math.Log10(Math.Abs(errorEstimate))-1);
                if (digits>3 && digits<15 )
                    b= Math.Round(b, digits);
                // check if result is close-but not exactly-integer, in which case there is risk of input function being integer/stepped, and some neighbour integer being better fit
                if (b != (int)b)
                {
                    var bi = Math.Round(b);
                    double aroundDif(double x) => ( Math.Abs(f(x - 0.3)) + Math.Abs(f(x + 0.3))) / 2; // average distance to zero around integer
                    void chkOfs(int db)
                    {
                        var fd = f(bi + db);
                        if (!double.IsNaN(fd) && !double.IsInfinity(fd)) // if Y of int x is valid
                            if ( Math.Abs(fd) < Math.Abs(fb) ||  // if integer is strictly better, use it
                                 ( Math.Abs(fd) == Math.Abs(fb) && aroundDif(bi + db) <= aroundDif(b) ))  // if same, use only if more centrally situated
                            {
                                b = bi + db;
                                fb = fd;
                            }
                    }
                    chkOfs(+1);
                    chkOfs(-1);
                    chkOfs(0);
                }
                return b;
            }
        }




        // find MINIMUM/MAXIMUM of a function, and returns X where that minimum is located
        // use Golden section search. If sign=-1 find minimum, if sign=+1 find maximum
        public static double find_extreme ( FunctionOfOneVariable f, double left, double right, double tolerance , int sign )
        {
            double invphi = (Math.Sqrt(5) - 1) / 2; // 1/phi                                                                                                                     
            double invphi2 = (3 - Math.Sqrt(5)) / 2; // 1/phi^2   
            double a, b;
            if (left <= right) { a = left; b = right; } else { a = right; b = left; }
            double h = b - a;
            if (h <= tolerance) return (right + left) / 2;
            // required steps to achieve tolerance                                                                                                                   
            int n = (int)(Math.Ceiling(Math.Log(tolerance / h) / Math.Log(invphi)));
            double c = a + invphi2 * h;
            double d = a + invphi * h;
            sign = -sign;
            double yc = f(c) * sign;
            double yd = f(d) * sign;
            // do all steps
            for (int k = 0; k < n; k++) {
                if (yc < yd) {
                    b = d;
                    d = c;
                    yd = yc;
                    h = invphi * h;
                    c = a + invphi2 * h;
                    yc = f(c) * sign;
                } else {
                    a = c;
                    c = d;
                    yc = yd;
                    h = invphi * h;
                    d = a + invphi * h;
                    yd = f(d) * sign;
                }
            }
            if (yc < yd)
                return (a + d) / 2;
            else
                return (c + b) / 2;
        }

        // find MINIMUM of a function, and returns X where that minimum is located
        public static double find_min(FunctionOfOneVariable f, double left, double right, double tolerance = 1e-12)
        {
            return find_extreme(f, left, right, tolerance, -1);
        }

        // find MAXIMUM of a function, and returns X where that maximum is located
        public static double find_max(FunctionOfOneVariable f, double left, double right, double tolerance = 1e-12)
        {
            return find_extreme(f, left, right, tolerance, +1);
        }


        // integrate function double fn (double), from x=range_start to x=range_end, optional number of steps
        public static double integral(FunctionOfOneVariable fn, double range_start, double range_end, int steps=10000)
        {
            if (range_start > range_end)
            {
                double tmp=range_start;
                range_start = range_end;
                range_end = tmp;
            }
            if (range_start == range_end)   return 0;
            if (steps < 1) steps = 1;
            // now perform integration
            double x = range_start, dx = (range_end - range_start) / steps;
            double res = 0;
            while (x + dx <= range_end)
            {
                res += dx * (fn(x) + fn(x + dx)) / 2; // trapezoidal rule
                x += dx;
                //if (isTimeout()) throw new ArgumentException("ERR:Timeout");
            }
            // last part, if range was not divisible by dx
            dx = range_end - x;
            if (dx > 0)
                res += dx * (fn(x) + fn(x + dx)) / 2; 
            // result of integration
            return res;
        }


        #endregion


        #region RANDOM functions
        // ****   RANDOM  generating functions

        // random double/int value
        public static double rndNumber(double upToD)
        {
            if ((upToD < 0) || (upToD == 1))
                return rnd.NextDouble();
            else
            {
                if (isInt(upToD))
                    return rnd.Next((int)upToD);
                else
                    return upToD * rnd.NextDouble();
            }
        }


        // randomly generate vector that fills N elements with random(max) - its [0,max>
        public static List<double> rndVector(int n, double max)
        {
            var nv = new List<double>(n);
            for (int i = 0; i < n; i++)
                nv.Add(rndNumber(max));
            return nv;
        }

        // randomly generate vector that chooses x out of N ( has x elements randomly selected between 1..N )
        public static List<int> rndChoose(int x, int N)
        {
            if ((x > N) || (x < 0))
                throw new ArgumentException("Can not choose " + x + " out of " + N + " !");
            // return empty case
            if (x == 0)
                return new List<int>();
            // select optimistic or pessimistic algorithm
            var iList = new List<int>(x);
            if (x / (double)N < 0.666)
            {
                // optimistic - will get slower for more elements ( k-th element need N/(N-k) attempts to find new, times  k search for contain: 
                // a(x,N)= if(x≤1,1, (x-1)*N/(N-x+1) + a(x-1,N) )
                while (iList.Count < x)
                {
                    int next = rnd.Next(N) + 1;
                    if (!iList.Contains(next))
                        iList.Add(next);
                }
            }
            else
            {
                // pessimistic - use more memory  and preparation time, but get faster near end ( b(x,N) gets better than a(x,N) when x>=66% N ): 
                // b(x,N)= N+x*3
                var fullList = new List<int>(x);
                for (int i = 1; i <= N; i++) fullList.Add(i);
                int remains = N;
                for (int i = 0; i < x; i++)
                {
                    int next = rnd.Next(remains--);
                    iList.Add(fullList[next]);
                    fullList.RemoveAt(next);
                }
            }
            return iList;
        }

        // randomly choose ONE integer value [0..probSize> , based on probabilities for each value to occurs, given in vector
        public static int rndNumberWeighted(List<double> v)
        {
            // number of possible values is equal to size of vector
            int n = v.Count;
            // sum of all probabilities, to be normalized to 1
            double sumP = 0;
            var ov = new double[n];
            for (int i = 0; i < n; i++)
            {
                double w = v[i];
                if (w < 0) throw new ArgumentException("rndNumberWeighted need all probabilities to be positive!");
                ov[i] = w;
                sumP += w;
            }
            // normalize: if all zero,  equally - otherwise according to weights
            var pv = new double[n];
            if (sumP == 0)
                for (int i = 1; i <= n; i++) pv[i] = i / (double)n;
            else
            {
                pv[0] = ov[0];
                for (int i = 1; i < n; i++) pv[i] = ov[i] / sumP + pv[i - 1];
            }
            // get random value up to 1
            var nRnd = rnd.NextDouble();
            // find first larger value
            int choosen = 0;
            while ((choosen < n) && (pv[choosen] < nRnd))
                choosen++;
            return choosen < n ? choosen : n - 1;
        }


        // create vector int[N] with randomly shuffled values 0..N-1
        public static int[] rndShuffle(int N)
        {
            var deck = new int[N];
            for (int i = 0; i < N; i++)
            {
                int j = rnd.Next(i + 1); 
                if (j != i) deck[i] = deck[j];
                deck[j] = i;
            }
            return deck;
        }


        #endregion


        #region VECTOR and EXTRAPOLATION functions
        //***  VECTOR functions 



        // returns standard deviation of a vector
        public static double vStdDev(List<double> vector)
        {
            if ((vector == null) || (vector.Count <= 0)) return 0;
            double avg = vector.Average();
            double sumSquareDiff = 0;
            foreach (var ev in vector)
            {
                sumSquareDiff += (ev - avg) * (ev - avg);
            }
            double stdDev = Math.Sqrt(sumSquareDiff / vector.Count);
            return stdDev;
        }

        // find Y value given X value and two vectors: vX-values and vY-values

        public static Number extrapolate(Number X, List<Number> vX, List<Number> vY)
        {
            Number exOne(int K) // y=f(x) based on Kth line segment
            {
                Number X0 = vX[K - 1], X1 = vX[K], Y0 = vY[K - 1], Y1 = vY[K];
                if (X1<=X0) throw new ArgumentException("extrapolate vecX must have increasing X values ! X["+K+"] ("+X1+") <= X["+(K-1)+"] ("+X0+")");
                return Y0 + (X - X0) / (X1 - X0) * (Y1 - Y0);
            }
            if (vX.Count!=vY.Count) throw new ArgumentException("extrapolate vecX["+vX.Count+"] must have same number of elements as vecY["+vY.Count+"] ! ");
            if (vX.Count < 2) throw new ArgumentException("extrapolate vecX & vecY must have at least two points ! ");
            for (var i = 1; i < vX.Count; i++)
                if (X <= vX[i]) return exOne(i); // this also include X<vX[0] extrapolation
            return exOne(vX.Count-1); // extrapolation for X>vX[max]
        }

        // Calculate area of extrapolated function between X1 and X2
        public static Number areapolate(Number X1, Number X2, List<Number> vX, List<Number> vY)
        {
            if (vX.Count != vY.Count) throw new ArgumentException("areapolate vecX[" + vX.Count + "] must have same number of elements as vecY[" + vY.Count + "] ! ");
            if (vX.Count < 2) throw new ArgumentException("areapolate vecX & vecY must have at least two points ! ");
            if (X2<X1) throw new ArgumentException("areapolate X2 can not be smaller than X1 ! ");
            // find right point of segment containing X1 ( if 2..3, then s1=3 ; if before first segment then s1=0 ; if after last segment,then s1= vX.Count )
            var s1 = 0;
            while ((s1 < vX.Count) && (vX[s1] < X1)) s1++;
            // find right point of segment containing X2
            var s2 = 0;
            while ((s2 < vX.Count) && (vX[s2] <= X2)) s2++;
            // extrapolate exact y=f(x) values for X1,X2
            var y1 = extrapolate(X1, vX, vY);
            var y2 = extrapolate(X2, vX, vY);
            // if same segment
            if (s1 == s2) return (y1 + y2) / 2 * (X2 - X1);
            // first area, include extrapolated to left
            var A = (vY[s1] + y1) / 2 * (vX[s1] - X1);
            // add last area, assume s2>0 ( otherwise they would be "same segment" )
            A = A + (y2 + vY[s2-1]) / 2 * (X2 - vX[s2-1]);
            // add area for segments in between, i=left segment point
            for (var i=s1; i<s2-1; i++)
                A=A+ (vY[i+1] + vY[i]) / 2 * (vX[i+1] - vX[i]);
            return A; 
        }

        // Average Y value between X1 and X2, weighted
        public static Number avgpolate(Number X1, Number X2, List<Number> vX, List<Number> vY)
        {
            if (X1 == X2) return extrapolate(X1, vX, vY);
            else if (X1 < X2) return areapolate(X1, X2, vX, vY) / (X2 - X1);
            else return areapolate(X2, X1, vX, vY) / (X1 - X2);
        }

        #endregion

    }

    // some result classes
    public class RectangleD
    {
        public double x1, y1, x2, y2;
        public RectangleD(bool isZeroed=true) 
        {
            if (isZeroed)
                x1 = y1 = x2 = y2 = 0;
            else
                x1 = y1 = x2 = y2 = double.NaN;
        }
        public RectangleD( double X1, double Y1, double X2, double Y2) 
        { 
            x1 = X1;
            y1 = Y1;
            x2 = X2;
            y2 = Y2;
        }
    }



}
