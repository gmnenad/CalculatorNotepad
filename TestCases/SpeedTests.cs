using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculatorNotepad;
using System.Diagnostics;
using System.Numerics;
using Numbers;
using Double = Numbers.Double;


namespace TestCases
{
    static class SpeedTests
    {


        // repeat multiple times
        static Stopwatch sw = new Stopwatch();

        static SpeedTests()
        {
            sw.Start();
        }

        static void msg(string sta)
        {
            Console.WriteLine(sta);
            sw.Restart();
        }


        static void msgTm(string sta) => msg(sta + " ( in " + sw.ElapsedMilliseconds.ToString("N0") + " ms )");


        // Choose functions as single parameter versions (k=N/2), so they canbe tested

        static double chooseD(double N)
        {
            var k = N / 2;
            if (k > N) return 0;
            if ((N - k) < k) k = N - k;
            double res = 1;
            for (int i = 1; i <= k; i++)
                res *= (N - k + i) / i;
            return res;
        }




        static Quad chooseQ(double N)
        {
            var k = N / 2;
            if (k > N) return 0;
            if ((N - k) < k) k = N - k;
            Quad res = 1;
            for (int i = 1; i <= k; i++)
                res *= (N - k + i) / i;
            return res;
        }

        static MPFR chooseM(double N)
        {
            var k = N / 2;
            if (k > N) return 0;
            if ((N - k) > k) k = N - k;
            MPFR res = 1;
            for (int i = 1; i <= k; i++)
                res *= (N - k + i) / i;
            return res;
        }





        const int plusCount = 10000000;
        // simple '+'
        static double plusD(double N)
        {
            double tstPlus(double a, double b) => a + b;
            double res = 0;
            for (int i = 0; i < plusCount; i++)
                //; 16ms just loop
                res += 1.5; // 33ms with +Double [ op ~ 17ms]
                            //res = tstPlus(res,1.5); // 47 ms with one func call [call ~14ms]
            return res;
        }

        static Quad plusQ(double N)
        {
            Quad res = 0;
            for (int i = 0; i < plusCount; i++)
                res += 1.5; // 391ms with +Quad [ op ~375ms ]  22x slower than Double
            return res;
        }

        static MPFR plusM(double N)
        {
            MPFR res = 0;
            for (int i = 0; i < plusCount; i++)
                res += 1.5; // 14,760ms with +MPFR ,  870x slower than Double ( 40x slower than Quad )
            return res;
        }



        static double whileD(double N) { double res = N; while (res > 0) res--; return res; }
        static Number whileR(double N) { Number res = N; while (res > 0) res--; return res; }
        static Quad whileQ(double N) { Quad res = N; while (res > 0) res--; return res; }
        static MPFR whileM(double N) { MPFR res = N; while (res > 0) res--; return res; }

        static double factD(double N) => nm.Factorial(N);
        static Number factR(double N)  { Number x = (Quad)N; return Number.Factorial(x); }
        static Quad factQ(double N) => Quad.Factorial(N);
        static MPFR factM(double N) => MPFR.Factorial(N);





        // Both tests show  RealMix ~ 20x Double, Quad ~ 20x Double, MPFR ~ 800x Double (40xQuad)


        // test results for value 'x': display once, then repeat 'nRepat' times and show relative speeds, then halve x and do repeats again, up to 'steps' times
        static void doSpeedTest(double x, int nRepeat, int steps, Func<double, double> fD, Func<double, Number> fR, Func<double, Quad> fQ, Func<double, MPFR> fM, string funcName, Action<double> fEmpty=null)
        {
            // display results
            msg(">> " + funcName + " will be tested up to " + steps + " times with " + nRepeat.ToString("N0") + " repeats : ");
            // prime functions 
            if (fD != null) fD(1);
            if (fR != null) fR(1);
            if (fQ != null) fQ(1);
            if (fM != null) fM(1);
            // get empty/base time
            long tmD, tmR, tmQ, tmM, tmE=0;
            if (fEmpty != null)
            {
                sw.Restart();
                for (int i = 0; i < nRepeat; i++) fEmpty(x);
                tmE = sw.ElapsedMilliseconds;
            }
            while (steps > 0 && x >= 1)
            {
                tmD = tmR = tmQ = tmM =  -1;
                double res = 0;
                Quad resQ = 0;
                Number resR = 0;
                MPFR resM = 0;
                // repeat multiple times and time it
                if (fD != null)
                {
                    sw.Restart();
                    for (int i = 0; i < nRepeat; i++) res = fD(x);
                    tmD = sw.ElapsedMilliseconds;
                }
                if (fQ != null)
                {
                    sw.Restart();
                    for (int i = 0; i < nRepeat; i++) resQ = fQ(x);
                    tmQ = sw.ElapsedMilliseconds;
                }
                if (fR != null)
                {
                    sw.Restart();
                    for (int i = 0; i < nRepeat; i++) resR = fR(x);
                    tmR = sw.ElapsedMilliseconds;
                }
                if (fM != null)
                {
                    sw.Restart();
                    for (int i = 0; i < nRepeat; i++) resM = fM(x);
                    tmM = sw.ElapsedMilliseconds;
                }

                // show relative speeds
                if (tmD > 0)
                {
                    //string sOne(string name, long tm) => tm < 0 ? "" : " : " + name + "  *" + (tm / tmD).ToString("N0").PadLeft(4)+" ("+tm.ToString("N0")+"ms)";
                    //var s = " X= " + x.ToString("N0").PadLeft(5) + sOne("double", tmD) + sOne("Real", tmR) + sOne("Quad", tmQ) + sOne("MPFR", tmM);
                    //msg(s + Environment.NewLine);
                    void sMsg(string name, string res, long tm) {
                        if (tm < 0) return;
                        string s = name + "(" + x + ")= " + res + " in " + (tm - tmE).ToString("N0") + "ms [ ";
                        double ratio = (tm - tmE) / (double)(tmD - tmE);
                        if (ratio > 10)
                            s += "* " + ratio.ToString("N0");
                        else if (ratio > 2)
                            s += "* " + ratio.ToString("N2");
                        else {
                            ratio=(ratio-1)*100;
                            s += (ratio>0?"+":"") + ratio.ToString("N0")+"%";
                        }
                        msg(s+ " ]");
                    }
                    if (tmE > 0) msg("Empty execution in " + tmE.ToString("N0") + "ms");
                    sMsg("double", res.ToString(), tmD);
                    sMsg("Number", resR.ToString(), tmR);
                    sMsg("Quad",   resQ.ToString(), tmQ);
                    sMsg("MPFR", resM.ToString(), tmM);
                }
                else
                    return;
                // next step
                steps--;
                x /= 2;
            };
            msg("Done.");
        }


        public static void doTests()
        {
            //doSpeedTest(1200, 1000, 10, chooseD, null, chooseQ, chooseM, null, "CHOOSE");
            //doSpeedTest(1, 1, 1, plusD, plusR, plusQ, plusM, null, "PLUS");
            //doSpeedTest(10000000, 1, 0, whileD, whileR, whileQ, null, "WHILE");
            //doSpeedTest(10, 100000, 1, factD, factR, factQ, factM, "Factorial");

        }



    }
}
