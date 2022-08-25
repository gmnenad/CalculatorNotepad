using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculatorNotepad;
using System.Diagnostics;
using Mpfr.Gmp;
using System.Numerics;
using Numbers;


namespace TestCases
{
    internal class StringTests
    {
        public static void stringtests()
        {
            var x = MPFR.One / 3;
            void Print(double d)
            {
                MPFR x = d;
                Console.WriteLine("Print(" + d + ") = '" + x.ToString() + "'");
            }

            void one(string format)
            {
                Console.WriteLine(format + " = '" + x.ToString() + "'");
            }
            Console.WriteLine("ToString() = '" + x.ToString() + "'");
            one("%Rg");
            one("%Re");
            one("%14Rg");
            one("%20.14Rg");
            x = -1000 * MPFR.One / 3;
            one("%16.14Rg");
            one("%13.14Rg");
            x *= 1e-66;
            one("%13.14Rg");
            one("%1.16Rg");

            Print(1.0 / 3);
            Print(0.0001234);
            Print(0.1234);
            Print(1234);
            Print(-123.444444444444444444444444);
            Print(1.2345);
            Print(123.4567);
            Print(12340000);

            Print(double.NaN);
            Print(double.PositiveInfinity);
            Print(double.NegativeInfinity);
            Print(0);
            Print(1.234e18);
            Print(1.234e-8);
            Print(-123.444444444444444444444444e77);
            Print(-1.444444444444444444444444e-88);
        }

        static void conversionTests()
        {
            void mq(double d)
            {
                MPFR m;
                Quad q;
                // test explicit MPFR to Quad
                m = d;
                q = (Quad)m;
                Console.WriteLine("MPFR(" + m.ToString() + ") ->  Quad(" + q + ")  for " + d);
                // test explicit Quad to MPFR
                q = d;
                m = (MPFR)q;
                Console.WriteLine("Quad(" + q + ") -> MPFR(" + m.ToString() + ") for " + d);

            }

            Console.WriteLine("Bits per limb = " + gmp_lib.mp_bits_per_limb);


            mq(123.456);
            mq(-654.321);
            mq(1);
            mq(0);
            mq(1.23e-65);
            mq(-3.21e-57);
        }


        static void t1(string s, int SigDigits = -1)
        {
            /*
            real d;
            if (NA.TryParse(s, out d))
            {
                var rFormat = NA.FormatNumberBase(s, 10, 12);
                var rConv = NA.ToString(d, 10, SigDigits);
                var d2 = NA.Parse(rConv);
                var diff = (double)real.Abs(d2 / d - 1) * 100;
                string dsp = s + " == (format12str) " + rFormat + " == (real) " + d.ToString() + " == (conv) " + rConv + " [ diff= " + diff.ToString("N2") + "% ] ";
                Console.WriteLine(dsp);
            }
            else
                Console.WriteLine("Invalid number : " + s);
            */
        }

        static void t(string s, int SigDigits = -1)
        {
            t1(s, SigDigits);
            t1("-" + s, SigDigits);
        }
        static void tb(double d0, int Base, int SigDigits = -1)
        {
            void doOne(double d)
            {
                /*
                var rConv = NA.ToString(d, Base, SigDigits);
                var dsp = "(real) " + d.ToString() + " in base " + Base + " == (conv) " + rConv;
                Console.WriteLine(dsp);
                */
            }
            doOne(d0);
            doOne(-d0);
        }

        static void test_ToString()
        {
            tb(0xFF, 16);
            tb(1234.567e89, 16);
            tb(0.05, 2);
            tb(5.5, 2);
            tb(5.5e-17, 2);
            tb(12.3e+22, 2);
            t(".0123");
            t("11.3");
            t("1.234e7");
            t("12.34e-77");
            t("0.00123");
            t("0.00000000123");
            t("0");
            t("0.00");
            //t("1.234e-700000000");

        }


        static void test_large_exp()
        {
            //t("1.234e-700000000");
            //t1("1e12345678901234567");
            bool tstExp(long exp)
            {
                
                bool diff = true;
                /*
                real d1 = real.Pow(10, exp);
                var s1 = NA.ToString(d1);
                real d2;
                if (NA.TryParse(s1, out d2))
                {
                    var s2 = NA.ToString(d2);
                    var diffQ = (double)real.Abs(d2 / d1 - 1) * 100;
                    var orig = "1e" + exp;
                    diff = s2 != orig;
                    string dsp = "10^" + exp.ToString("N0") + " == (orig) " + orig + " == (1st) " + s1 + " == (2nd) " + s2 + " [ ~ " + diffQ.ToString("N2") + "% ] " + (diff ? "DIFF!" : "");
                    Console.WriteLine(dsp);

                }
                else
                    Console.WriteLine("Error parsing : " + s1);
                */
                return diff;
            }
            long exp = 1234567890;
            for (int i = 1; i <= 9; i++)
            {
                exp = exp * 10 + i;
                tstExp(exp);
            }
            exp = 1_000_000_000_000_000l;
            for (int i = 0; i < 16; i++)
                tstExp(exp * i + 345_678_901_234_567l);
        }


        // test double parses
        void tdp(params string[] doubleStr)
        {

            foreach (var ns in doubleStr)
            {
                double d;
                Number x;
                string r = "[" + ns + "] == (double)";
                try
                {
                    d = double.Parse(ns);
                    r += d;
                }
                catch (Exception ex)
                {
                    r += ex.Message;
                }
                r += " , (mine)";
                try
                {
                    x = Number.Parse(ns);
                    r += x;
                }
                catch (Exception ex)
                {
                    r += ex.Message;
                }
                Console.WriteLine(r);
            }
        }



        public static void doTests() 
        {
            //stringtests();
            //conversionTests();
            //test_ToString();
            //test_large_exp();
            //Console.WriteLine(real.ToString(real.Parse("3321928094887362347"), 2));

            //tdp("123",".123","12.34e56","0001234.56","12.","12.e5","-12","-.32");
            //tdp(" -123", "-123 ","- 123", " - 123","1.2 e3","1.2e 23");
            //tdp("123-4", "_-123", "NaN!-3", "NaN", "-inf!","-Infinity", "1.2e3D","-_12.3_e_4`5_-6");

            //Double a = 1.234, b=1.2345;
            //Console.WriteLine("a ="+a.ToString(10)+" , b="+b.ToString(10) + " , diff= "+NA.diffDigit(a, b));

        }




    }
}
