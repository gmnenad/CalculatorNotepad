using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Numbers
{
    public static partial class nmDoubleExtensions
    {

        /*
        
         
         
        */



        // testing normal double values



        // n!,  int and float factorial
        public static double Factorial(this double x)
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
        public static double Gamma(this double z)
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

        // Logarithm in base
        public static double Log(this double a, double Base) => Math.Log(a, Base);



        /// <summary>
        /// convert given number to specified base if base is 10 or 16, return null for unsupported bases
        /// </summary>
        public static unsafe string? ToString(this double a, int Base, int SignificantDigits = -1, int decZeros = 4)
        {
            // use innate double conversion for base 10
            if (Base == 10)
                return a.ToString();
            if (!IsNumber(a))
                return null; // skip if NaN, Inf...
            // otherwise, try to convert from bits for binary divisible bases: 2,4,8,16,32
            ulong bits = *((ulong*)&a);
            ulong _highestBit = 1ul << 63;
            int sign = (bits & _highestBit) != 0 ? -1 : +1;
            long exp = (((long)bits >> 52) & 0x7ffL) - 1023 - 63; // 1023 is IEEE bias, -63 since we are passing as integer isntead of IEEE 1.xxx fraction
            ulong mantissa = (bits & 0xfffffffffffffUL) << 11; // shift so that highest bit is 2nd to left ( with leftmost being implicit 1 )
            // this will return null if base is not valid
            return nmExtensions.ToStringFromBits(Base, sign, exp, validDigitsInBase(a, Base), mantissa | _highestBit);
        }


    }



}
