using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Diagnostics;
using Numbers;


// Intro on new features :  https://devblogs.microsoft.com/dotnet/dotnet-7-generic-math/
// Definition and hierarchy of interfaces :   https://github.com/dotnet/designs/tree/main/accepted/2021/statics-in-interfaces

namespace TestCases
{
    public class InterfaceTests
    {
        public static T Add<T>(T left, T right)
            where T : INumber<T>
        {
            return left + right;
        }


        public static bool testVec2<T>(T x, Func<T, T, T> op2func) where T : INumber<T>
        {
            T r= op2func(x, x);
            var res = x + x == r;
            Console.WriteLine("vec2("+x+")= "+r+(res?"":"  FAIl !"));
            return res;

        }



        static Func<dynamic, dynamic, dynamic> storeF = null;
        public static void testVec1set(Func<dynamic, dynamic, dynamic> op1func) 
        {
            storeF = op1func;
        }
        public static void testVec1setF(Func<dynamic, dynamic, dynamic> op1func)
        {
            Func<double, double, double> mvFunc = (a, b) => (double) op1func(a, b);
            storeF = op1func;
        }
        public static bool testVec1(dynamic x, dynamic y)
        {
            var r =  storeF(x,y);
            var res = x + x == r;
            Console.WriteLine("vec2(" + x + ")= " + r + (res ? "" : "  FAIl !"));
            return res;

        }

        public static void doTests()
        {
            //testVec2(5.1, Add);
            //testVec2(3L, Add);
            //testVec2(3L, (a, b) => a + b);
            
            //testVec1set((a, b) => a + b);
            //testVec1(5.1,5.1);
            //testVec1(3L,3.0);
            //testVec1(3.0,(Quad)3.0);

        }


    }





}
