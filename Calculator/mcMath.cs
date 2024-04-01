using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Numbers;

// Wrappers around math functions to convert from/to mcValues
//      - some functions perform actual math logic directly here
//      - some functions call specific number type functions, eg for integers
//      - most functions should call 'number agnostic' math functions:
//          - for funcs that differ per number class, call NM.func or MATH.func . Those func are defined in Quad/MPFR classes, or for double in nm or Math classes
//          - for funcs that are themselves agnostic, call NA.func ( 'a'=agnostic )


namespace CalculatorNotepad
{

    // math functions, including mcValue versions and their int/float variants from inherited nmMath
    public class nmc
    {

        #region UTILS

        /// <summary>
        /// invoke passed method within thread, then wait for timeout
        ///     void method() :    callThreaded( ()=>{outRes=somecall(outpar1, ouutpar2..)} ); 
        /// method should be lambda that get and inputs and set results in its calling scope
        /// </summary>
        public static void callThreaded(Action method)
        {
            Exception eThread = null;
            var iThread = new Thread(() =>
            {
                MPFR.CheckDefaultPrecision();
                try
                {
                    method();
                }
                catch (Exception e)
                {
                    eThread = e;
                }
            }
            );
            iThread.Start();
            if (iThread.Join(mc.cfg.timeoutDisabled ? Timeout.Infinite : (int)mc.cfg.timeoutFuncMs))
            {
                // check if there was thread exception?
                if (eThread != null)
                {
                    if (eThread is System.Reflection.TargetInvocationException)
                        throw (eThread as System.Reflection.TargetInvocationException).InnerException;
                    else
                        throw eThread;
                }
            }
            else
            {
                iThread.Abort();
                throw new mcException("ERR:Timeout");
            }
        }

        /// <summary>
        /// test single argument for null and types, and throw exception if invalid
        /// </summary>
        static void testArg(mcValue arg, string desc, ArgTst tst=ArgTst.None, int pos=0)
        {
            // helper functions
            string pos2str(int i) { return (i + 1).ToString() + (i == 1 ? "st" : i == 2 ? "nd" : i == 3 ? "rd" : "th"); }
            void doThrow(string typeName) { throw new ArgumentException(desc + " require " + pos2str(pos) + " argument to be a " + typeName + "  !"); }
            // do tests
            if (arg == null) throw new ArgumentException(desc + " has undefined " + pos2str(pos) + " argument  !");
            if (tst != ArgTst.None)
            {
                if ((tst & ArgTst.Int) != 0) if (!arg.isInt()) doThrow("Integer");
                if ((tst & ArgTst.Number) != 0) if (!arg.isScalar()) doThrow("Number");
                if ((tst & ArgTst.Vector) != 0) if (!arg.isVector()) doThrow("Vector");
                if ((tst & ArgTst.Func) != 0) if (!arg.isVector()) doThrow("Func");
            }
        }

        /// <summary>
        /// test args[] for nulls, size and types, and throw exception if invalid
        /// </summary>
        static void testArgs(mcValue[] args, string desc, int minParams, int maxParams=0, ArgTst commonTests=ArgTst.None, ArgTst[] eachParamTest=null)
        {
            if (args == null) throw new ArgumentException(desc + " arguments not defined !");
            if (args.Length < minParams) throw new ArgumentException(desc + " requires at least " + minParams + " arguments ( not " + args.Length + " ) !");
            if ((maxParams > 0) && (args.Length > maxParams)) throw new ArgumentException(desc + " requires at max " + maxParams + " arguments ( not " + args.Length + " ) !");
            // test each argument
            for (int i = 0; i < args.Length; i++)
            {
                ArgTst tst = commonTests;
                if ((eachParamTest != null) && (eachParamTest.Length < i)) tst |= eachParamTest[i];
                testArg(args[i], desc, tst, i);
            }
        }

        /// <summary>
        /// throw an exception with supplied text. used when function should not be called ever, like 'else'
        /// </summary>
        public static  mcValue errArg(string text)
        {
            throw new ArgumentException(text);
        }

        #endregion


        #region Counter functions
        //**** COUNTER functions


        /// <summary>
        /// create  ArrayCounter with given max values, and return it as complex vector
        /// - counterCreate( vec_maxValues [,vec_minValues] )
        /// It will have as many digits as given vectors. If vec_minValues is not specified, it default to zeros
        /// </summary>
        public static mcValue counterCreate(mcValue[] args)
        {
            testArgs(args, "counterCreate", 1, 3, ArgTst.None, new ArgTst[] { ArgTst.Vector, ArgTst.Vector, ArgTst.Int });
            // create counter
            mcMathCounter ac;
            if (args.Length > 1)
            {
                if (args[1].vectorLength != args[0].vectorLength) throw new ArgumentException("counterCreate( vec_maxValues ,vec_minValues ) require both vectors of same size !");
                bool canRepeat = true;
                if (args.Length > 2) canRepeat = (args[2].Int != 0);
                ac = new mcMathCounter(args[0].getListInt().ToArray(), args[1].getListInt().ToArray(), canRepeat);
            }
            else
                ac = new mcMathCounter(args[0].getListInt().ToArray(),null);
            // return complex vector representing counter
            return ac.getValueVector();
        }

        /// <summary>
        /// create combination ArrayCounter ( order does not matter, so always non-descending, reduced number of combos )
        /// - counterCreateComb( numDigits, maxValue [, minValue=1 [, canRepeat=false]] )
        /// Will count only ascending combos, so 012, 013, 234.. not 432  
        /// If canRepeat is true, then 000, 001 etc is allowed too, ie it can repeat numbers, but will still be ascending
        /// </summary>
        public static mcValue counterCreateComb(mcValue[] args)
        {
            testArgs(args, "counterCreateComb", 2, 4, ArgTst.Int);
            //public ArrayCounter(int nDigits_p, int permN, bool canRepeatNumber, int startFrom = 0)
            int numDigits = args[0].Int;
            int maxValue = args[1].Int;
            int minValue = args.Length > 2 ? args[2].Int : 1;
            bool canRepeat = args.Length > 3 ? args[3].isTrue() : false;
            // create counter
            var ac = new mcMathCounter( numDigits, maxValue, canRepeat, minValue);
            // return complex vector representing counter
            return ac.getValueVector();
        }

        public static mcValue counterCreatePerm(mcValue[] args)
        {
            testArgs(args, "counterCreatePerm", 1, 1);
            int[] elements;
            if (args[0].isInt()) {
                int N = args[0].Int;
                elements = new int[N];
                for (int i = 0; i < N; i++) elements[i] = i + 1;
            } else {
                if (!args[0].isVector()) throw new ArgumentException("counterCreatePerm( elements ) must have 'eleemnts' as either vector or integer !");
                elements = args[0].getListInt().ToArray();
            }
            // create counter
            var ac = new mcMathCounter(elements);
            // return complex vector representing counter
            return ac.getValueVector();
        }


        /// <summary>
        /// increment counter vector and return new counter vector
        /// </summary>
        public static mcValue counterNext(mcValue counterVector)
        {
            if (counterVector.vectorLength != 4) throw new ArgumentException("Invalid counterVector");
            var ac = new mcMathCounter(counterVector);
            ac.Next();
            return ac.getValueVector();
        }


        /// <summary>
        /// return true while counter is not finished
        /// </summary>
        public static mcValue counterNotFinished(mcValue counterVector)
        {
            if (counterVector.vectorLength != 4) throw new ArgumentException("Invalid counterVector");
            bool finished = (counterVector.Vector[3].Int & (int)ArrayCounterOptions.Finished) != 0;
            return new mcValue(!finished);
        }


        /// <summary>
        /// return vector with current counter values
        /// </summary>
        public static mcValue counterValues(mcValue counterVector)
        {
            if (counterVector.vectorLength != 4) throw new ArgumentException("Invalid counterVector");
            return counterVector.Vector[0];
        }

        /// <summary>
        /// return vector with current counter values
        /// </summary>
        public static mcValue counterTotalCount(mcValue counterVector)
        {
            if (counterVector.vectorLength != 4) throw new ArgumentException("Invalid counterVector");
            var ac = new mcMathCounter(counterVector);
            return new mcValue(ac.TotalCount());
        }


        /// <summary>
        /// Returns in how many different ways given vector could be arranged
        /// If all N elements of vector are different, it will be N!
        /// But if some elements are repeating within vector, it will be less than N!
        /// </summary>
        public static mcValue comboCount(mcValue[] Vector)
        {
            testArgs(Vector, "comboReplicas", 1,1, ArgTst.Vector);
            var v = Vector[0].getListDouble();
            int n = ArrayCounter.NumberOfReplicas(v);
            return new mcValue(n);
        }


        #endregion


        #region DOUBLE custom MATH functions ( Number )
        //**** custom MATH functions


        // harmonic( ( n [,coverage=100%] )  - return harmonic number Hn= sum(1/i) for i=1..n = 1/N+1/(N-1)+...1/1
        //     N*harmonic(N) = expected tries to complete all set (all different coupons or get all six dice numbers (N=6) ...)
        //                     assume probability to get each of N different set items is equal, 1/N
        // when coverage != 100% , return partial harmonic number Hnp= sum(1/i) for i= (1-coverage)*n+1..n = Hn-H(1+(1-coverage)*n)
        //     N*harmonic(N, 30%) = expected tries to complete 30% of a set
        public static mcValue Harmonic(mcValue[] args)
        {
            testArgs(args, "Harmonic", 1, 2, ArgTst.Number);
            if (args.Length < 2)
                return new mcValue(Number.Harmonic(args[0].Number, 1));
            else
                return new mcValue(Number.Harmonic(args[0].Number, args[1].Double));
        }


        // ln(x [,base]) - return logarithm of x in given base, or natural base (e) if not specified
        public static mcValue Ln(mcValue[] args)
        {
            testArgs(args, "ln", 1, 2, ArgTst.Number);
            if (args.Length > 1)
                return new mcValue(Number.Log(args[0].Number, args[1].Number));
            else
                return new mcValue(Number.Log(args[0].Number));
        }

        // log(x [,base]) - return logarithm of x in given base, or base 10 if not specified
        public static mcValue Log(mcValue[] args)
        {
            testArgs(args, "log", 1, 2, ArgTst.Number);
            if (args.Length > 1)
                return new mcValue(Number.Log(args[0].Number, args[1].Number));
            else
                return new mcValue(Number.Log10(args[0].Number));
        }



        // max( (a,b,...)  - returns largest number from specified list
        public static mcValue max(mcValue[] args)
        {
            // single max(x)  - if vector, returns largest element, otherwise return this number
            Number max(mcValue x)
            {
                if ((x == null) || (x.isFunction()))
                    throw new ArgumentException("Max need non-function values  !");
                if (x.isScalar())
                    return x.Number;
                var v = x.Vector;
                Number max1 = Number.NegativeInfinity;
                foreach (var e in v)
                {
                    Number me = max(e); // deep dive for vectors as elements of other vectors
                    if (me > max1) max1 = me;
                }
                return max1;
            }
            // iterate each element
            if (args.Length == 0)
                throw new ArgumentException("Max need at least one parameter !");
            Number maxSoFar = Number.NegativeInfinity;
            foreach (var v in args)
            {
                Number mv = max(v);
                if (mv > maxSoFar) maxSoFar = mv;
            }
            return new mcValue(maxSoFar);
        }

        // min( (a,b,...)  - returns smallest number from specified list
        public static mcValue min(mcValue[] args)
        {
            // single max(x)  - if vector, returns largest element, otherwise return this number
            Number min(mcValue x)
            {
                if ((x == null) || (x.isFunction()))
                    throw new ArgumentException("Min need non-function values  !");
                if (x.isScalar())
                    return x.Number;
                var v = x.Vector;
                Number min1 = Number.PositiveInfinity;
                foreach (var e in v)
                {
                    Number me = min(e); // deep dive for vectors as elements of other vectors
                    if (me < min1) min1 = me;
                }
                return min1;
            }
            // iterate each element
            if (args.Length == 0)
                throw new ArgumentException("Min need at least one parameter !");
            Number minSoFar = Number.PositiveInfinity;
            foreach (var v in args)
            {
                Number mv = min(v);
                if (mv < minSoFar) minSoFar = mv;
            }
            return new mcValue(minSoFar);
        }


        //Returns true if supplied scalar value is infinite: isInfinity ( Value )
        public static mcValue isInfinity(mcValue value)
        {
            if ((value==null)|| !value.isScalar()) throw new ArgumentException("isInf require scalar parameter (Number or int) !");
            Number val = value.Number;
            return new mcValue( Number.IsInfinity(val)  );
        }



        // helper function to check and unpack variable number of Numbers from argument list (starting from given index), allowing Numbers inside vectors  too
        public static List<Number> unpackVectorNumber(mcValue vector, string funcName)
        {
            var aVector = vector.Vector;
            var res = new List<Number>();
            foreach (var ar in aVector)
            {
                if (ar.isScalar()) res.Add(ar.Number);
                else
                if (ar.isVector()) res.AddRange(unpackVectorNumber(ar, funcName));
                else
                    throw new ArgumentException(funcName + " requires all arguments to be numbers or number vectors!");
            }
            return res;
        }

        public static List<Number> unpackArgsNumber(mcValue[] args, int startIdx, string funcName)
        {
            if (args == null) return new List<Number>();
            var v = new List<mcValue>();
            for (int i = startIdx; i < args.Length; i++) v.Add(args[i]);
            return unpackVectorNumber(new mcValue(v), funcName);
        }


        // genNumber(args, Number(Number,Number))  - returns generic Number function applied to all values in list
        public static mcValue genNumber(mcValue[] args, Func<Number, Number, Number> gf, string funcName)
        {
            var numList = unpackArgsNumber(args, 0, funcName);
            // iterate each element
            if (numList.Count < 2)
                throw new ArgumentException(funcName + " need at least two parameters !");
            Number res = gf(numList[0],numList[1]);
            for (int i=2; i<numList.Count; i++)
                res = gf(res, numList[i]);
            return new mcValue(res);
        }



        #endregion


        #region INTEGER custom MATH functions ( INT/double )
        // functions that take only integers as arguments, and/or return integer results only (with their mcValue wrappers)
        // use long as Int64 , and mcValue.Long when 64bits needed

        // helper function to check and unpack variable number of integers from argument list (starting from given index), allowing integers inside vectors  too
        public static List<int> unpackArgsInts(mcValue[] args, int startIdx, string funcName)
        {
            if (args == null) return new List<int>();
            var v = new List<mcValue>();
            for (int i = startIdx; i < args.Length; i++) v.Add(args[i]);
            return unpackVectorInts(new mcValue(v), funcName);
        }
        public static List<int> unpackVectorInts(mcValue vector, string funcName)
        {
            var aVector = vector.Vector;
            var res = new List<int>();
            foreach(var ar in aVector)
            {
                if (ar.isInt()) res.Add(ar.Int);  else
                if (ar.isVector()) res.AddRange(unpackVectorInts(ar, funcName));
                else
                    throw new ArgumentException(funcName + " requires all arguments to be integers or integer vectors!");
            }
            return res;
        }

        // genInt(args, int(int,int))  - returns generic int function applied to all values in list
        public static mcValue genInt(mcValue[] args, Func<int, int, int> gf, string funcName)
        {
            var intList = unpackArgsInts(args, 0, funcName);
            // iterate each element
            if (intList.Count == 0)
                throw new ArgumentException(funcName + " need at least one parameter !");
            int res = 0;
            foreach (var v in intList)
            {
                res = gf(res, v);
            }
            return new mcValue(res);
        }


        // is even integer
        public static mcValue isEven(mcValue nv)
        {
            testArg(nv, "isEven", ArgTst.Int);
            return new mcValue(((nv.Int & 1) == 0) ? 1 : 0);
        }
        // is odd integer
        public static mcValue isOdd(mcValue nv)
        {
            testArg(nv, "isOdd", ArgTst.Int);
            return new mcValue(((nv.Int & 1) != 0) ? 1 : 0);
        }

        // bitxor(args)  - Returns bitwise XOR from the specified integer list
        public static mcValue bitxor(mcValue[] args)=> genNumber(args, (a, b) => a ^ b, "xor");

        // bitand(args)  - Returns bitwise AND from the specified integer list
        public static mcValue bitand(mcValue[] args) => genNumber(args, (a, b) => a & b, "and");

        // bitor(args)  - Returns bitwise OR from the specified integer list
        public static mcValue bitor(mcValue[] args) => genNumber(args, (a, b) => a | b, "or");




        // GCD with mcValues, accepting multiple numbers
        public static mcValue gcd(mcValue[] args)
        {
            // test if all arguments are integers. Cant use testArgs due to unlimited number of allowed arguments
            if ((args==null)||(args.Length<1)) throw new ArgumentException("GCD function requires at least one integer argument!");
            foreach(var ar in args) if ((ar==null)|| !ar.isInt()) throw new ArgumentException("GCD function requires all arguments to be integers !");
            // if only one argument, return it
            int a = args[0].Int;
            if (args.Length == 1) return new mcValue(a);
            int b = args[1].Int;
            int res = nm.gcd(a, b);
            // if more than two arguments, gcd(a,b,c)= gcd( gcd(a,b) , c )...
            for (int i = 2; i < args.Length; i++)
                res = nm.gcd(res, args[i].Int);
            return new mcValue(res);
        }

        // LCM with mcValues, accepting multiple numbers and vectors
        public static mcValue lcm(mcValue[] args)
        {
            List<int> v = unpackArgsInts(args, 0, "LCM");
            // test if all arguments are integers. Cant use testArgs due to unlimited number of allowed arguments
            if (v.Count<1) throw new ArgumentException("LCM function requires at least one integer argument!");
            int res = nm.lcm(v);
            return new mcValue(res);
        }


        #endregion


        #region LAMBDA functions : integral, sum, product, root ... ( Number )

        // call( (a,b,...)=> ... , a , b ,... ) - call lambda function with given parameters
        public static mcValue call(mcValue[] args)
        {
            if (!mcValue.isFunction(args[0]))
                throw new ArgumentException("Call need lambda as first argument  !");
            // copy rest of arguments to new parameter array
            var param = new mcValue[args.Length - 1];
            for (int i = 1; i < args.Length; i++)
                param[i - 1] = args[i];
            // now call lambda
            return args[0].EvaluateFunc(param);
        }

        // integrate function #n, from x=range_start to x=range_end, optional number of steps
        // done directly here instead of calling mn.integral, due to timeout
        public static mcValue integral(mcValue[] args)
        {
            if (args.Length < 3)
                throw new ArgumentException("Integrate need at least 3 parameters ( (x)=>... , x_start, x_end, [num_steps] ) !");
            var lambda = args[0];
            if ((lambda == null) || (lambda.valueType != mcValueType.Func))
                throw new ArgumentException("Integrate need lambda function as first parameter: ( (x)=>... , x_start, x_end, [num_steps] )  !");
            Number fx(Number x) => lambda.EvaluateFunc(new mcValue[] { new mcValue(x) }).Number; 
            Number range_start = args[1].Number;
            Number range_end = args[2].Number;
            int steps = args.Length > 3 ? args[3].Int : 10000;
            return new mcValue(Number.integral(fx, range_start, range_end, steps));
        }

        // product function #n, from x=range_start to x=range_end, optional steps = default 1
        public static mcValue product(mcValue[] args)
        {
            if (args.Length < 3)
                throw new ArgumentException("Product need at least 3 parameters ( (i)=>... , i_start, i_end, [i_step]) !");
            var lambda = args[0];
            if ((lambda == null) || (lambda.valueType != mcValueType.Func))
                throw new ArgumentException("Product need lambda function as first parameter: ( (i)=>... , i_start, i_end, [i_step]) !");
            Number range_start = args[1].Number;
            Number range_end = args[2].Number;
            Number step = (args.Length > 3) ? args[3].Number : 1;
            if (range_start > range_end) return new mcValue(0);//  throw new ArgumentException("Invalid range for Product, start > end !");
            if (step <= 0)
                throw new ArgumentException("Invalid step for Product, must be positive number : " + step.ToString());
            // now perform Product
            Number x = range_start;
            mcValue res = new mcValue(1.0);
            while (x <= range_end)
            {
                var func_value = lambda.EvaluateFunc(new mcValue[] { new mcValue(x) });
                res = mcValue.Mul(res, func_value);
                x += step;
                if (mc.isFuncTimeout()) throw new ArgumentException("ERR:Timeout");
            }
            // result of summation
            return res;
        }


        // pSim( ()=> bool , N ) - takes lambda that returns bool, calls it N times and returns probability of true, as sum(N_results)/N
        public static mcValue pSim(mcValue lambda, mcValue N)
        {
            if ((lambda == null) || (lambda.valueType != mcValueType.Func))
                throw new ArgumentException("pSim need lambda function as first parameter: pSim( ()=> bool , N ) !");
            if ((!N.isInt()) || (N.Int <= 0))
                throw new ArgumentException("pSim need positive integer number as second parameter nSimulations !");
            int nSim = N.Int;
            // now perform simulations
            double sum = 0;
            for (int i = 1; i <= nSim; i++)
            {
                sum += lambda.EvaluateFunc(null).Double;
                if (mc.isFuncTimeout()) throw new ArgumentException("ERR:Timeout");
            }
            // return probability
            return new mcValue(sum / nSim);
        }

        // sum with LAMBDA function:  sum( (i)=>... , i_start, i_end, [i_step])
        public static mcValue sum(mcValue[] args)
        {
            if (args.Length < 3)
                throw new ArgumentException("SUM need at least 3 parameters ( (i)=>... , i_start, i_end, [i_step]) !");
            var lambda = args[0];
            if ((lambda == null) || (lambda.valueType != mcValueType.Func))
                throw new ArgumentException("SUM need lambda function as first parameter: ( (i)=>... , i_start, i_end, [i_step]) !");
            Number range_start = args[1].Number;
            Number range_end = args[2].Number;
            Number step = (args.Length > 3) ? args[3].Number : 1;
            if (range_start > range_end) return new mcValue(0); // throw new ArgumentException("Invalid range for SUM, start > end !");
            if (step <= 0)
                throw new ArgumentException("Invalid step for SUM, must be positive number : " + step.ToString());
            // now perform sum
            Number x = range_start;
            mcValue res = new mcValue(0.0);
            while (x <= range_end)
            {
                var x_param = new mcValue[] { new mcValue(x) };
                var func_value = lambda.EvaluateFunc(x_param);
                res = mcValue.Add(res, func_value);
                x += step;
                if (mc.isFuncTimeout()) throw new ArgumentException("ERR:Timeout");
            }
            // result of summation
            return res;
        }


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

        // mcValue wrapper around  integer binarySearch ( (i)=> boolFunc(i) [,i_start=-maxInt [,i_end=+maxInt    [,flags=0]]] );
        public static mcValue binarySearch(mcValue[] args)
        {
            // get parameters
            if (args.Length < 1) throw new ArgumentException("binarySearch need at least one parameter:  binarySearch ( (i)=> boolFunc(i) ) !");
            var lambda = args[0];
            if ((lambda == null) || (lambda.valueType != mcValueType.Func)) throw new ArgumentException("binarySearch need boolean lambda function as first parameter:  binarySearch ( (i)=> boolFunc(i) ) !");
            int range_start = (args.Length >= 2) ? args[1].Int : int.MinValue;
            int range_end = (args.Length >= 3) ? args[2].Int : int.MaxValue;
            int flags = (args.Length >= 4) ? args[3].Int : 0;
            // convert mcValue int/boolean lambda function to func(int)=bool
            Func<int, bool> intLambda =  x => lambda.EvaluateFunc(new mcValue[] { new mcValue(x) }).Int != 0;
            // call int version
            return new mcValue( nm.binarySearch(intLambda, range_start, range_end, flags) );
        }


        // find_root( f(x) , Y_target=0, tolerance=1e-6, left=guess, right=guess )
        // returns X that satisfy f(X)=Y_target
        // find root of specified function 'f(x)' if Y_target is n ot specified (==0)
        public static mcValue find_root(mcValue[] args)
        {
            testArgs(args, "find_root", 1, 5, ArgTst.None, new ArgTst[] { ArgTst.Func, ArgTst.Number, ArgTst.Number, ArgTst.Number, ArgTst.Number });
            // create FunctionOfOneVariable from mcValue lambda
            var lambda = args[0];
            Number fx(Number x)
            {
                return lambda.EvaluateFunc(new mcValue[] { new mcValue(x) }).Number;
            }
            // get optional parameters
            Number Y_target = 0.0;
            Number left = Number.NegativeInfinity;
            Number right = Number.PositiveInfinity;
            Number tolerance = 1e-12;
            if (args.Length >= 2) Y_target = args[1].Number;
            if (args.Length >= 3) tolerance = args[2].Number;
            if (args.Length >= 4) left = args[3].Number;
            if (args.Length >= 5) right = args[4].Number;
            // call c# function
            return new mcValue(Number.find_root(fx, Y_target, tolerance, left, right));
        }

        // returns X from[left, right] range for which f(X) has minimal value
        // assume single minimum of function f(x) within[left, right] range
        // find_min(f(x) , left , right[, tolerance = 1e-6] )
        public static mcValue find_min(mcValue[] args)
        {
            testArgs(args, "find_min", 3, 4, ArgTst.None, new ArgTst[] { ArgTst.Func, ArgTst.Number, ArgTst.Number, ArgTst.Number });
            // create FunctionOfOneVariable from mcValue lambda
            var lambda = args[0];
            Number fx(Number x)
            {
                return lambda.EvaluateFunc(new mcValue[] { new mcValue(x) }).Number;
            }
            // get optional parameters
            Number left = args[1].Number;
            Number right = args[2].Number;
            int tolerance = 6;
            if (args.Length >= 4) tolerance = args[3].Int;
            // call c# function
            return new mcValue(Number.find_min(fx, left, right, tolerance));
        }


        // returns X from[left, right] range for which f(X) has maximum value
        // assume single maximum of function f(x) within[left, right] range
        // find_max(f(x) , left , right[, tolerance = 1e-6] )
        public static mcValue find_max(mcValue[] args)
        {
            testArgs(args, "find_max", 3, 4, ArgTst.None, new ArgTst[] { ArgTst.Func, ArgTst.Number, ArgTst.Number, ArgTst.Number });
            // create FunctionOfOneVariable from mcValue lambda
            var lambda = args[0];
            Number fx(Number x)
            {
                return lambda.EvaluateFunc(new mcValue[] { new mcValue(x) }).Number;
            }
            // get optional parameters
            Number left = args[1].Number;
            Number right = args[2].Number;
            int tolerance = 6;
            if (args.Length >= 4) tolerance = args[3].Int;
            // call c# function
            return new mcValue(Number.find_max(fx, left, right, tolerance));
        }

        #endregion


        #region RANDOM functions  ( double )
        // ****   RANDOM  generating functions

        // random double/int value
        public static mcValue rndNumber(mcValue upTo = null)
        {
            if (upTo == null)
                return new mcValue(nm.rndNumber(-1));
            return new mcValue(nm.rndNumber(upTo.Double));
        }


        // randomly generate vector that fills N elements with random(max) - its [0,max>
        public static mcValue rndVector(mcValue N, mcValue max)
        {
            if (!N.isInt()) throw new ArgumentException("rndVector first argument (vector size) must be integer !");
            if (!max.isScalar()) throw new ArgumentException("rndVector second argument (max random value) must be a number !");
            return new mcValue(nm.rndVector(N.Int, max.Double));
        }

        // randomly generate vector that chooses x out of N ( has x elements randomly selected between 1..N )
        public static mcValue rndChoose(mcValue xV, mcValue NV)
        {
            if (!(mcValue.isInt(xV) && mcValue.isInt(NV)))
                throw new ArgumentException("rndChoose need integer arguments !");
            return new mcValue(nm.rndChoose(xV.Int, NV.Int));
        }

        // randomly choose ONE integer value [0..probSize> , based on probabilities for each value to occurs, given in vector
        public static mcValue rndNumberWeighted(mcValue vecProbs)
        {
            if (!mcValue.isVector(vecProbs))
                throw new ArgumentException("rndNumberWeighted need vector argument, with probabilities or weights for each value 0..vecLen-1 !");
            return new mcValue(nm.rndNumberWeighted(vecProbs.getListDouble()));
        }


        // create vector int[N] with randomly shuffled values 0..N-1
        // mcValue version, support both arg=int (then it creates new array with values 0..N-1), or arg=vector (shuffle values from that array)
        public static mcValue rndShuffle(mcValue arg)
        {
            if ((arg==null)||(arg.isFunction())||(arg.isScalar()&&!arg.isInt()))
                throw new ArgumentException("rndShuffle argument must be either integer number or vector !");
            if (arg.isScalar())
            {
                // if single number passed, treat it as new vector dimension
                var newDeck = nm.rndShuffle(arg.Int);
                return new mcValue(newDeck.ToList());
            }
            else
            {
                // otherwise treat as already populated vector
                var v = arg.Vector;
                int N = v.Count;
                var res = new List<mcValue>(N);
                var newDeck = nm.rndShuffle(N);
                for (int i = 0; i < N; i++)
                    res.Add(v[newDeck[i]]);
                return new mcValue(res);
            }
        }



        #endregion


        #region STATISTICAL random functions and distributions  ( double )
        //**** various probability distributions
        // Sources:  - book, internet searches, links like:  https://www.cse.wustl.edu/~jain/books/ftp/ch5f_slides.pdf


        // Error function - used for cumulative distribution function of normal distribution
        // approximation with max error 1.5E-7 
        public static mcValue erf(mcValue x)
        {
            testArg(x, "erf", ArgTst.Number);
            return new mcValue(nm.erf(x.Double));
        }


        // Error Margin: +- value around expected one, in order to have desired confidence
        //  - error_margin( Confidence_Level_percent, stddev ): CLp= confidence level percent, σ = stddev  
        //  - return: MOE = Margin of Error
        //      - MOE = Zvalue(CLp)* stddev
        //      - usual Confidence Level is 95% (Z value ~ 1.96 )
        //      - Rule of thumb: number of trials needed (at confidence level 95%) is: n=1/MOE^2 
        public static mcValue error_margin(mcValue[] args)
        {
            testArgs(args, "error_margin", 1, 2, ArgTst.Number);
            double CLp = args[0].Double;
            double stddev = args.Length < 2 ? 1.0 : args[1].Double;
            return new mcValue(nm.error_margin(CLp,stddev));
        }



        //*** DISCRETE distributions



        // Uniform distribution (discrete): equaly likely finite outcomes
        //  - dist_uniform(a , b ): a = min possible value, b= max possible random value
        //  - return: dist_vec= vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2] = 100, x_min[3]=a , x_max[4]=b )
        public static mcValue dist_uniform_discrete(mcValue[] args)
        {
            testArgs(args, "dist_uniform_discrete", 2, 2, ArgTst.Int);
            int a = args[0].Int;
            int b = args[1].Int;
            return new mcValue(nm.dist_uniform_discrete(a, b));
        }



        // Binomial distribution (discrete): x = # of successes in n fixed trials
        //  - dist_binomial( n , p ): n= number of fixed trials, p= probability of success on each independent trial
        //  - return: dist_vec= vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2] = 101, x_min[3]=0 , x_max[4]=n, n[5], p[6] )
        //  - for large values, pmf/cdf can be approximated with normal distribution (  n > 30 )
        public static mcValue dist_binomial(mcValue[] args)
        {
            testArgs(args, "dist_binomial", 2, 2, ArgTst.None,new ArgTst[] { ArgTst.Int, ArgTst.Number});
            int n = args[0].Int;
            double p = args[1].Double;
            return new mcValue(nm.dist_binomial(n,p));
        }


        // Poisson distribution (discrete): x = number of events in fixed time period, given rate
        //  - dist_poisson( ƛ ): ƛ= lambda = rate ( eg. average number of events in fixed period)
        //  - return: dist_vec= vec ( μ[0] = expected = ƛ, σ[1] = stddev = √ƛ  , dist_ID[2] = 102, x_min[3]=0 , x_max[4]= +inf, ƛ[5] )
        //  - for large values, pmf/cdf can be approximated with normal distribution (  ƛ > 30 )
        public static mcValue dist_Poisson(mcValue[] args)
        {
            testArgs(args, "dist_Poisson", 1, 1, ArgTst.Number);
            double lambda = args[0].Double;
            return new mcValue(nm.dist_Poisson(lambda));
        }


        // Geometric distribution (discrete): x = number of trials up to first success
        //  - dist_geometric( p ): p= probability of success on each independent trial
        //  - return: dist_vec= vec ( μ[0] = expected = 1/p, σ[1] = stddev  , dist_ID[2] = 103, x_min[3]=1 , x_max[4]= +inf, p[5] )
        public static mcValue dist_geometric(mcValue[] args)
        {
            testArgs(args, "dist_geometric", 1, 1, ArgTst.Number);
            double p = args[0].Double;
            return new mcValue(nm.dist_geometric(p));
        }


        // Negative binomial distribution (discrete): x = number of trials until K successes
        //  - dist_negative_binomial( p , k ): p= probability of success on each independent trial, k= number of successes needed
        //  - return: dist_vec= vec ( μ[0] = expected = k/p, σ[1] = stddev  , dist_ID[2] = 104, x_min[3]= k , x_max[4]= +inf, p[5] , k[6] )
        //  - when k==1, this becomes geometric distribution (both have fixed sucesses, unlimited X - as opposed to binomial, which has fixed X )
        //  - for large values, pmf/cdf MAYBE can be approximated with normal distribution (  k > 30 ) - need test
        public static mcValue dist_negative_binomial(mcValue[] args)
        {
            testArgs(args, "dist_negative_binomial", 2, 2, ArgTst.None, new ArgTst[] { ArgTst.Number, ArgTst.Int });
            double p = args[0].Double;
            int k = args[1].Int;
            return new mcValue(nm.dist_negative_binomial(p, k));
        }


        // Hypergeometric distribution (discrete): x = number of marked individuals in sample taken without replacement
        //  - dist_hypergeometric( n , N , M ):  n= size of sample, N=total number of individuals, M= number of marked individuals
        //  - return: dist_vec= vec ( μ[0] = expected = n*M/N, σ[1] = stddev  , dist_ID[2] = 105, x_min[3]= max(0,n-N+M) , x_max[4]= min(M,n), n[5] , N[6] , M[7])
        // for example: if we take n=10 balls out of box with total N=20 balls where there are M=5 black balls, what is the chance to get x=3 black balls in sample?
        public static mcValue dist_hypergeometric(mcValue[] args)
        {
            testArgs(args, "dist_hypergeometric", 3, 3, ArgTst.Int);
            int n = args[0].Int;
            int N = args[1].Int;
            int M = args[2].Int;
            return new mcValue(nm.dist_hypergeometric(n,N,M));
        }



        //*** CONTINUOUS distributions


        // Uniform distribution (continuous): equaly likely uncountable outcomes in floating point range [a,b]
        //  - dist_uniform_continuous(a , b ): a = min possible value, b= max possible random value
        //  - return: dist_vec= vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2] = 200, x_min[3]=a , x_max[4]=b )
        public static mcValue dist_uniform_continuous(mcValue[] args)
        {
            testArgs(args, "dist_uniform_continuous", 2, 2, ArgTst.Number);
            double a = args[0].Double;
            double b = args[1].Double;
            return new mcValue(nm.dist_uniform_continuous(a, b));
        }



        // Normal distribution (continuous): standard bell shaped distribution
        //  - dist_normal( μ , σ ) : μ = expected , σ = stddev  
        //  - return: dist_vec= vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2] = 201, x_min[3]= -inf , x_max[4]= +inf )
        public static mcValue dist_normal(mcValue[] args)
        {
            testArgs(args, "dist_normal", 2, 2, ArgTst.Number);
            double ex = args[0].Double;
            double stddev = args[1].Double;
            return new mcValue(nm.dist_normal(ex, stddev));
        }



        // Exponential distribution (continuous): x = time between or until an event, given rate (also birth,decay,interest rates...)
        //  - dist_exponential( ƛ ): ƛ= lambda ~ rate ( eg. average number of events in period)
        //  - return: dist_vec= vec ( μ[0] = expected time between events = 1/ƛ, σ[1] = stddev = 1/ƛ  , dist_ID[2] = 202, x_min[3]=0 , x_max[4]= +inf, ƛ[5] )
        public static mcValue dist_exponential(mcValue[] args)
        {
            testArgs(args, "dist_exponential", 1, 1, ArgTst.Number);
            double lambda = args[0].Double;
            return new mcValue(nm.dist_exponential(lambda));
        }



        // Statistical sample distribution: actually sampled data
        //  - dist_sample( vec(x1,x2,...) : vec= vector with sample values
        //  - return: dist_vec= vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2] = 209, x_min[3]= -inf , x_max[4]= +inf , n[5]=number of samples )
        //  - for pmf/cdf , assume normal distribution
        public static mcValue dist_sample(mcValue[] args)
        {
            testArgs(args, "dist_sample", 1, 1, ArgTst.Vector);
            var v = args[0].getListDouble();
            return new mcValue(nm.dist_sample(v));
        }


        // Distribution of total (sum) of n random numbers
        //   - dist_trials_sum( n, vec(μ,σ) ): n= number of trials , vec(μ,σ) = vector (can be dist_vec) describing individual random variables from that sum
        //   - return: dist_vec= vec ( μ[0] = expected sum = μ * n , σ[1] = stddev = σ * √n , dist_ID[2] = 210,  x_min[3]= -inf , x_max[4]= +inf , n[5] )
        //   - due to Central Limit Theorem (CLT), distribution of sums approaches normal distribution (if n>=30!) regardless of individual distribution ( variance_sum = variance * n )
        public static mcValue dist_trials_sum(mcValue[] args)
        {
            testArgs(args, "dist_trials_sum", 2, 2, ArgTst.None, new ArgTst[] { ArgTst.Int, ArgTst.Vector });
            int n = args[0].Int;
            var v = args[1].getListDouble();
            return new mcValue(nm.dist_trials_sum(n,v));
        }


        // Distribution  of average of n random variables
        //   - dist_trials_avg( n, vec(μ,σ) ): n= number of trials , vec(μ,σ) = vector (can be dist_vec) describing individual random variables from that sum
        //   - return: dist_vec= vec ( μ[0] = expected average = μ ,  σ[1] = stddev = σ / √n , dist_ID[2] = 211,  x_min[3]= -inf , x_max[4]= +inf , n[5] )
        //   - due to Central Limit Theorem (CLT), distribution of averages approaches normal distribution (if n>=30!) regardless of individual distribution ( variance_avg = variance / n )
        public static mcValue dist_trials_avg(mcValue[] args)
        {
            testArgs(args, "dist_trials_avg", 2, 2, ArgTst.None, new ArgTst[] { ArgTst.Int, ArgTst.Vector });
            int n = args[0].Int;
            var v = args[1].getListDouble();
            return new mcValue(nm.dist_trials_avg(n,v));
        }


        // Distribution of proportion of successes over n trials\ , where each of 'n' trials has same independent chance 'p' to be success
        //   - dist_trials_proportion( n , p ): n= number of trials , p= chance of success for each trial
        //   - return: dist_vec= vec ( μ[0] = expected proportion = p ,  σ[1] = stddev = √(p*(1-p)/n), dist_ID[2] = 212,  x_min[3]= 0 , x_max[4]= 1 , n[5] , p[6] )
        //   - due to Central Limit Theorem (CLT), distribution of proportions approaches normal distribution (if n>=30!) regardless of individual distribution ( variance_proportion = p * (1-p) / n )
        public static mcValue dist_trials_proportion(mcValue[] args)
        {
            testArgs(args, "dist_trials_proportion", 2, 2, ArgTst.None, new ArgTst[] { ArgTst.Int, ArgTst.Number });
            int n = args[0].Int;
            double p = args[1].Double;
            return new mcValue(nm.dist_trials_proportion(n,p));
        }






        //*** PMF


        // Probability Mass Function (pmf) value for given x :  probability that random variable in specified distribution is exactly equal to x ( p(X) == x ) 
        // pmf( x , dist_vec ) : it takes type of distribution from dist_vec: = vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2], x_min[3] , x_max[4], [5]... other params )
        public static mcValue pmf(mcValue[] args)
        {
            testArgs(args, "pmf", 2, 2, ArgTst.None, new ArgTst[] { ArgTst.Number, ArgTst.Vector });
            var dist_vec = args[0].getListDouble();
            double x = args[1].Double;
            return new mcValue(nm.pmf(x, dist_vec));
        }



        //*** CDFs

        // Cumulative Distribution Function (cdf) value for given x :  probability that random variable in specified distribution is less or equal to x ( p(X) <= x )
        // cdf( x , dist_vec ) : it takes type of distribution from dist_vec: = vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2], x_min[3] , x_max[4], [5]... other params )
        public static mcValue cdf(mcValue[] args)
        {
            testArgs(args, "cdf", 2, 3, ArgTst.None, new ArgTst[] { ArgTst.Vector, ArgTst.Number, ArgTst.Number });
            var dist_vec = args[0].getListDouble();
            double x = args[1].Double;
            double? x2 = null;
            if (args.Length > 2) x2= args[2].Double;
            return new mcValue(nm.cdf(dist_vec, x, x2));
        }


        // cumulative distribution function of normal distribution
        // approximation based on erf:  cdfnZ(x)== (erf(x/√2)+1)/2
        // cdf_normal( x [, vec(μ,σ)] ) 
        public static mcValue cdf_normal(mcValue[] args)
        {
            double ex = 0, stddev = 1;
            if (args.Length < 2)
            {
                testArgs(args, "cdf_normal", 1, 1, ArgTst.Number);
            }
            else
            {
                // 2nd parameter must be vec (μ,σ)
                testArgs(args, "cdf_normal", 2, 2, ArgTst.None, new ArgTst[] { ArgTst.Number, ArgTst.Vector }); // to report nulls or invalid first parameter or non-vector 2nd parameter
                var v = args[1].Vector;
                ex = v[0].Double;
                stddev = v[1].Double;
            }
            return new mcValue(nm.cdf_normal(args[0].Double, ex, stddev));
        }


        //*** RANDOM from distribution vector


        // return random number based on given probability distribution 
        // rndNumber_dist( dist_vec ) : it takes type of distribution from dist_vec = vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2], x_min[3] , x_max[4], [5]... other params )
        public static mcValue rndNumber_dist(mcValue[] args)
        {
            testArgs(args, "rndNumber_dist", 1, 1, ArgTst.Vector);
            var dist_vec = args[0].getListDouble();
            return new mcValue(nm.rndNumber_dist(dist_vec));
        }


        // functions to test random distributions
        //addFunc("testRndD", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nm.testRndD(args)), "Debug discrete test distribution, return vec( avgDiff%, diff%r0, diff%r1 ... , diff%rest)\r\n  - N = number of test rounds, should be large enough", "testRndD( N, dist_vec )"));
        //addFunc("testRndC", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nm.testRndC(args)), "Debug continuous test distribution, return vec( avgDiff%, diff%r0, diff%r1 ... , diff%rest)\r\n  - N = number of test rounds, should be large enough", "testRndC( N, dist_vec )"));

        public static List<double> testRndD(int N, List<double> dist_vec, int range=-1)
        {
            int mx = 20, sumMax = 0;
            int x_min = (int)dist_vec[3], x_max=x_min+range;
            if (range < 0)
            {
                double ex = dist_vec[0];
                x_max = (int)(2 * ex);
            }
            double dx = (x_max - x_min + 1) / (double)mx;
            if (dx < 1)
            {
                dx = 1;
                mx = x_max - x_min + 1;
            }
            int[] stat = new int[mx];
            // generate random values and count
            for (int i = 0; i < N; i++)
            {
                double rn = nm.rndNumber_dist(dist_vec);
                int x = (int)((rn - x_min) / dx);
                if (x < mx)
                    stat[x]++;
                else
                    sumMax++;
            }
            // compare distribution
            var res = new double[mx + 2];
            double totDiff = 0, chkExdx = 0;
            int chkStat=sumMax;
            for (int i = 0; i < mx; i++)
            {
                double x = x_min + i * dx;
                double exdx = nm.cdf(dist_vec, (int)x, (int)(x + dx-1)) * N;
                chkExdx += exdx;
                int statCnt = stat[i];
                chkStat += statCnt;
                double diff = statCnt - exdx;
                if (exdx > 0.5) diff /= exdx;
                res[i + 1] = Math.Round(diff*100);
                totDiff += Math.Abs(diff);
            }
            if (chkStat != N) throw new mcException("testRndD:  chkStat != N");
            if (Math.Abs(chkExdx - N) / N > 0.03) throw new mcException("testRndD:  chkExdx ("+(int)chkExdx+") != N ");
            double xE = x_min + mx * dx;
            double exdxE = (1 - nm.cdf(dist_vec, (int)xE)) * N;
            double diffE = sumMax - exdxE ;
            if (exdxE > 0.5) diffE /= exdxE;
            res[mx + 1] = Math.Round(diffE*100);
            totDiff += Math.Abs(diffE);
            res[0] = Math.Round(totDiff / (mx+1) * 100,2);
            return res.ToList();
        }
        public static mcValue testRndD(mcValue[] args)
        {
            testArgs(args, "testRndD", 2, 3, ArgTst.None, new ArgTst[] { ArgTst.Int, ArgTst.Vector, ArgTst.Int });
            int n = args[0].Int;
            var dist_vec = args[1].getListDouble();
            int xMax = args.Length > 2 ? args[2].Int : -1;
            var res = testRndD(n, dist_vec, xMax);
            return new mcValue(res);
        }


        public static List<double> testRndC(int N, List<double> dist_vec)
        {
            int mx = 20, sumMax = 0, sumMin=0;
            int[] stat = new int[mx];
            double ex = dist_vec[0], stddev = dist_vec[1], x_min = ex-5*stddev, x_max = ex + 5 * stddev;
            double dx = (x_max - x_min) / mx;
            // generate random values and count
            for (int i = 0; i < N; i++)
            {
                int idx = (int)((nm.rndNumber_dist(dist_vec) - x_min) / dx);
                if (idx < 0) sumMin++; else
                if (idx >= mx) sumMax++; else
                    stat[idx]++;
            }
            // compare distribution
            var res = new double[mx + 3];
            double totDiff = 0, chkExdx = 0, chkStat =0;
            void updRes(int actual, double expected, int resIdx)
            {
                double diff = actual - expected;
                //if (expected > 0.3) diff /= expected;
                res[resIdx] = Math.Round(diff);
                totDiff += Math.Abs(diff);
                chkExdx += expected;
                chkStat += actual;
            }
            // before range
            double exB = nm.cdf(dist_vec, x_min) * N;
            updRes(sumMin, exB, 1);
            // inside range
            for (int i = 0; i < mx; i++)
            {
                double x = x_min + i * dx;
                double exI = nm.cdf(dist_vec, x, x + dx) * N;
                updRes(stat[i], exI, i + 2);
            }
            // after range
            double exA = (1 - nm.cdf(dist_vec, x_max)) * N;
            updRes(sumMax, exA, mx+2);
            // test sums and aggregate res
            if (chkStat != N) throw new mcException("testRndC:  chkStat != N");
            if (Math.Abs(chkExdx - N) / N > 0.03) throw new mcException("testRndC:  chkExdx (" + (int)chkExdx + ") != N ");
            res[0] = Math.Round(totDiff / N * 100, 2);
            return res.ToList();
        }
        public static mcValue testRndC(mcValue[] args)
        {
            testArgs(args, "testRndC", 2, 2, ArgTst.None, new ArgTst[] { ArgTst.Int, ArgTst.Vector });
            int n = args[0].Int;
            var dist_vec = args[1].getListDouble();
            var res = testRndC(n, dist_vec);
            return new mcValue(res);
        }



        #endregion


        #region VECTOR functions ( Number , vector )
        //***  VECTOR functions

        // generate vector from list of elements
        public static mcValue vec(mcValue[] args)
        {
            var res = new List<mcValue>(); // empty vector
            for (int i = 0; i < args.Length; i++)
                res.Add(args[i]);
            return new mcValue(res);
        }

        // vAvg( vector ) - return average scalar value of vector
        public static Number vAvg(mcValue vector)
        {
            if ((vector == null) || !vector.isVector())
                throw new ArgumentException("vAvg can work only with scalar and vector values   !");
            if (vector.isScalar())
                return vector.Number;
            var v = vector.Vector;
            Number sum = 0;
            int N = 0;
            foreach (var e in v)
                if (e.vectorLength > 0)
                {
                    sum += vAvg(e);
                    N++;
                }
            if (N > 0)
                return sum / N;
            else
                return 0; // or throw exception
        }
        public static mcValue vAvg(mcValue[] args)
        {
            if ((args == null) || (args.Length != 1) || !args[0].isVector())
                throw new ArgumentException("vAvg need exactly one parameter of type vector   !");
            return new mcValue(vAvg(args[0]));
        }


        // vDim( N [,defValue] ) - create vector of size N with optional default value that can even be lambda: vecDim(4,(i)=>i) 
        public static mcValue vDim(mcValue[] args)
        {
            if (args.Length < 1)
                throw new ArgumentException("vDim need as first argument size of vector to be created  !");
            if (!mcValue.isInt(args[0]))
                throw new ArgumentException("vDim need integer value for size of new vector  !");
            int N = args[0].Int;
            var res = new List<mcValue>(N);
            if ((args.Length == 1) || (args[1] == null))
            {
                // if no default values, fill with zeros
                var defVal = new mcValue(0);
                for (int i = 0; i < N; i++) res.Add(defVal);
            }
            else
            {
                var defVal = args[1];
                // if default value is not a lambda, fill with that value ( or copy of that value? mcValue is supposed to be immutbale, but ...)
                if (defVal.valueType != mcValueType.Func)
                {
                    for (int i = 0; i < N; i++) res.Add(defVal);
                }
                else
                {
                    // defVal is lambda, so call it for each element
                    for (int i = 0; i < N; i++)
                    {
                        var elVal = defVal.EvaluateFunc(new mcValue[] { new mcValue(i) });
                        res.Add(elVal);
                    }
                }
            }
            //return mcValue vector
            return new mcValue(res);
        }

        // vCopy( vector ) - create copy of input vector
        // useful, since vector assignment (v2=v1) just make reference copy
        public static mcValue vCopy(mcValue vector)
        {
            if ((vector==null)|| !mcValue.isVector(vector))
                throw new ArgumentException("vCopy need vector as parameter !");
            var v = vector.Vector;
            var res = new List<mcValue>(v.Count);
            for (int i = 0; i < v.Count; i++) res.Add(v[i]);
            //return new mcValue vector
            return new mcValue(res);
        }

        // Append new element to existing vector
        // Returns expanded vector, but that vector is already expanded.  In essence: vAppend(ref vector, newElement)
        // RISKY , due to immutability violation !
        public static mcValue vAppend(mcValue[] args)
        {
            if ((args.Length < 1) || !mcValue.isVector(args[0]))  throw new ArgumentException("vAppend need vector as first parameter !");
            if ((args.Length < 2) ) throw new ArgumentException("vAppend need new element value as second parameter !");
            //return expanded vector
            return args[0].appendVector(args[1]);
        }


        // vFor( (i)=> ...return nv[i], N ) - for (int i=0; i<N; i++) v[i]= lambda(i); return v
        public static mcValue vFor(mcValue lambda, mcValue n)
        {
            if ((lambda == null) || (lambda.valueType != mcValueType.Func))
                throw new ArgumentException("vFor need lambda function as first parameter: vFor( (i)=> ...return nv[i], N )  !");
            if (!n.isInt())
                throw new ArgumentException("vFor need integer size of vector as second argument!");
            int N = n.Int;
            var vector = new List<mcValue>(N);
            for (int i = 0; i < N; i++)
                vector.Add(new mcValue(i));
            // call vFunc over this 0..N-1 vector
            return vFunc(new mcValue[] { lambda, new mcValue(vector) });
        }

        // apply Func to individual elements of vectors, resulting in new vector
        // vecFunc ( (a,b,c)=>..., va,vb,vc,...)
        public static mcValue vFunc(mcValue[] args)
        {
            if (args.Length < 2)
                throw new ArgumentException("vFunc need at least one vector input: vecFunc ( (a,b,c)=>..., va,vb,vc,...)  !");
            var lambda = args[0];
            if ((lambda == null) || (lambda.valueType != mcValueType.Func))
                throw new ArgumentException("vFunc need lambda function as first parameter: vecFunc ( (a,b,c)=>..., va,vb,vc,...)  !");
            if (!args[1].isVector())
                throw new ArgumentException("vFunc need vectors as 2nd+ arguments  !");
            int N = args.Length - 1;
            int vSz = args[1].vectorLength;
            var vectors = new List<mcValue>[N];
            for (int i = 0; i < N; i++)
            {
                if (args[i + 1].vectorLength != vSz)
                    throw new ArgumentException("vFunc need all vectors to be same size  !");
                vectors[i] = args[i + 1].Vector;
            }
            // for each element of vectors, apply function
            var res = new List<mcValue>(vSz);
            for (int i = 0; i < vSz; i++)
            {
                // create parameter list, as each element of vectors at this position
                var factors = new mcValue[N];
                for (int u = 0; u < N; u++)
                    factors[u] = vectors[u][i];
                // call function to calculate result for this position
                var ri = lambda.EvaluateFunc(factors);
                // append result to final vector
                res.Add(ri);
            }
            // final result is mcValue of vector type
            return new mcValue(res);
        }

        // intersect two vectors as if they are sets - return only common elements. Works on first level only (not on vectors inside vectors). Remove doubles.
        public static mcValue vIntersect(mcValue a, mcValue b)
        {
            if (!mcValue.isVector(a)) throw new ArgumentException("vIntersect first argument must be a vector ");
            if (!mcValue.isVector(b)) throw new ArgumentException("vIntersect second argument must be a vector ");
            var av = a.Vector;
            var bv = b.Vector;
            var nv = new List<mcValue>();
            // insert elemnts of first vector (check for duplicates) only if they exists in second vector too
            foreach (var e in av)
                if (bv.Contains(e) && !nv.Contains(e))
                    nv.Add(e);
            return new mcValue(nv);
        }

        // return length of mcValue vector
        public static int vLen(mcValue vector)
        {
            return vector.vectorLength;
        }

        // return index of smallest scalar element of vector 
        public static int vMinIdx(mcValue vector)
        {
            if ((vector == null) || !vector.isVector())
                throw new ArgumentException("vMinIdx argument must be a vector: vMinIdx(vector)->scalar ");
            // if this is single number , return index zero
            if (vector.isScalar()) return 0;
            // search each element
            Number res = Number.PositiveInfinity;
            int idx = -1;
            var v = vector.Vector;
            for (int i = 0; i < v.Count; i++)
            {
                var vm = vMin(v[i]);
                if (vm < res) { 
                    res = vm;
                    idx = i;
                }
            }
            return idx;
        }

        // return index of largest scalar element of vector 
        public static int vMaxIdx(mcValue vector)
        {
            if ((vector == null) || !vector.isVector())
                throw new ArgumentException("vMaxIdx argument must be a vector: vMaxIdx(vector)->scalar ");
            // if this is single number , return index zero
            if (vector.isScalar()) return 0;
            // search each element
            Number res = Number.NegativeInfinity;
            int idx = -1;
            var v = vector.Vector;
            for (int i = 0; i < v.Count; i++)
            {
                var vm = vMin(v[i]);
                if (vm > res)
                {
                    res = vm;
                    idx = i;
                }
            }
            return idx;
        }


        // return smallest scalar element of vector , search subvectors also
        public static Number vMin(mcValue vector)
        {
            if ((vector == null) || !vector.isVector())
                throw new ArgumentException("vMin argument must be a vector: vMin(vector)->scalar ");
            // if this is single number , return it
            if (vector.isScalar())
                return vector.Number;
            // search each element
            Number res = Number.PositiveInfinity;
            foreach (var v in vector.Vector)
            {
                var vm = vMin(v);
                if (vm < res)
                    res = vm;
            }
            return res;
        }

        // return largest scalar element of vector 
        public static Number vMax(mcValue vector)
        {
            if ((vector == null) || !vector.isVector())
                throw new ArgumentException("vMax argument must be a vector: vMax(vector)->scalar ");
            // if this is single number , return it
            if (vector.isScalar())
                return vector.Number;
            // search each element
            Number res = Number.NegativeInfinity;
            foreach (var v in vector.Vector)
            {
                var vm = vMax(v);
                if (vm > res)
                    res = vm;
            }
            return res;
        }


        // multiply all elements of vector to produce scalar
        public static mcValue vMul(mcValue vector)
        {
            Number getMul(mcValue vecValue)
            {
                // if this is single number , return it
                if (mcValue.isScalar(vecValue))
                    return vecValue.Number;
                // otherwise this must be a vector, not a func
                if (!mcValue.isVector(vecValue))
                    throw new ArgumentException("vMul argument must be a vector: vMul(vector)->scalar ");
                // sum each element
                Number res = 1;
                foreach (var v in vecValue.Vector)
                    res *= getMul(v);
                return res;
            }
            // call recursively, in case vector elements are also vectors
            return new mcValue(getMul(vector));
        }

        // Returns integer assuming vector elements are digits
        public static mcValue vDigits(mcValue vector)
        {
            // if this is single number , return it
            if (mcValue.isScalar(vector))
                return vector;
            // otherwise this must be a vector, not a func
            if (!mcValue.isVector(vector))
                throw new ArgumentException("vDigits argument must be a vector: vDigits(vector)->scalar ");
            // concatenate each element
            Number res = 0;
            foreach (var v in vector.Vector)
            {
                if (! v.isInt())
                    throw new ArgumentException("all elements of input vector for vDigits() must be integers !");
                int digit = v.Int;
                if (digit<0 || digit>9)
                    throw new ArgumentException("vDigits elements of input vector must be integers between 0..9 !");
                res *= 10;
                res += digit;
            }
            return new mcValue(res);
        }


        // returns standard deviation of a vector
        public static mcValue vStdDev(mcValue vector)
        {
            if ((vector == null) || !vector.isVector() || (vector.vectorLength == 0))
                throw new ArgumentException("vStdDev argument must be a non-empty vector: vStdDev(vector)->scalar ");
            Number avg = vAvg(vector);
            Number sumSquareDiff = 0;
            foreach (var e in vector.Vector)
            {
                if (!e.isScalar())
                    throw new ArgumentException("vStdDev need all elements of argument vector to be scalars ! ");
                Number ev = e.Number;
                sumSquareDiff += (ev - avg) * (ev - avg);
            }
            Number stdDev = Number.Sqrt(sumSquareDiff / vector.vectorLength);
            return new mcValue(stdDev);
        }


        // sum elements of vector to produce scalar
        public static mcValue vSum(mcValue vector)
        {
            Number getSum(mcValue vecValue)
            {
                // if this is single number , return it
                if (mcValue.isScalar(vecValue))
                    return vecValue.Number;
                // otherwise this must be a vector, not a func
                if (!mcValue.isVector(vecValue))
                    throw new ArgumentException("vSum argument must be a vector: vSum(vector)->scalar ");
                // sum each element
                Number res = 0;
                foreach (var v in vecValue.Vector)
                    res += getSum(v);
                return res;
            }
            // call recursively, in case vector elements are also vectors
            return new mcValue(getSum(vector));
        }


        // union of two vectors as if they are sets - return all elements, but doubles are removed. Works on first level only (not on vectors inside vectors). Works on multiple inputs.
        public static mcValue vUnion(mcValue[] args)
        {
            var nv = new List<mcValue>();
            if (args.Length == 0) return new mcValue(nv);
            foreach (var a in args)
            {
                if (!mcValue.isVector(a)) throw new ArgumentException("vUnion argument must be a vector ");
                var av = a.Vector;
                // insert elemnts of this vector (check for duplicates)
                foreach (var e in av)
                    if (!nv.Contains(e))
                        nv.Add(e);
            }
            return new mcValue(nv);
        }

        // Concatenation of multiple vectors, resulting in vector with elements from all those vectors\r\nDuplicate elements are preserved, so different from Union.
        public static mcValue vConcat(mcValue[] args)
        {
            var nv = new List<mcValue>();
            if (args.Length == 0) return new mcValue(nv);
            foreach (var a in args)
            {
                if (!mcValue.isVector(a)) throw new ArgumentException("vUnion argument must be a vector ");
                var av = a.Vector;
                // insert elemnts of this vector (check for duplicates)
                foreach (var e in av)
                        nv.Add(e);
            }
            return new mcValue(nv);
        }


        // Truncate length of vector to specified size,    vTrunc( vec(1,2,3,4,5,6), 3):  vec(1,2,3)", "vTrunc ( vec , newSize )
        public static mcValue vTrunc(mcValue vector, mcValue newSize)
        {
            if ((vector == null) || !vector.isVector())    throw new ArgumentException("vTrunc first argument must be a vector: vTrunc(vector, newSize)->vector ");
            if ((newSize == null) || !newSize.isInt()) throw new ArgumentException("vTrunc second argument must be integer: vTrunc(vector, newSize)->vector ");
            int sz = newSize.Int;
            if (sz<0) throw new ArgumentException("vTrunc newSize argument can not be negative ! ");
            var v = vector.Vector;
            if (v.Count<=sz)
                return new mcValue(v);
            var nv = new List<mcValue>(sz);
            for (int i = 0; i < sz; i++)
                nv.Add(v[i]);
            return new mcValue(nv);
        }


        // Sort vector of scalar values :  vSort(vec [,direction=+1/-1])
        public static mcValue vSort(mcValue[] args)
        {
            // get params
            if (args.Length < 1) throw new ArgumentException("vSort need at least one vector as input: vSort( vec )  !");
            var vector = args[0];
            if ((vector == null) || !vector.isVector()) throw new ArgumentException("vSort first argument must be a vector: vSort( vec ) ! ");
            double direction = +1;
            if (args.Length > 1) direction = args[1].Double;
            // now sort
            var vList=vector.getListNumber();
            if (direction >= 0)
                vList.Sort();
            else
                vList.Sort((a, b) => -a.CompareTo(b));
            return new mcValue(vList);
        }

        // reverse order of elements in vector :  vReverse(vec)
        public static mcValue vReverse(mcValue[] args)
        {
            // get params
            if (args.Length < 1) throw new ArgumentException("vReverse need at least one vector as input: vReverse( vec )  !");
            var vector = args[0];
            if ((vector == null) || !vector.isVector()) throw new ArgumentException("vReverse first argument must be a vector: vReverse( vec ) ! ");
            var vList = vector.getVector();
            var newVec = new List<mcValue>();
            for(var i= vList.Count-1; i>=0; i--)
                newVec.Add(vList[i]);
            return new mcValue(newVec);
        }

        #endregion


        #region Extrapolation functions ( vector based )

        // find Y value given X value and two vectors: vX-values and vY-values
        public static mcValue extrapolate(mcValue[] args)
        {
            testArgs(args, "extrapolate", 1, 3, ArgTst.None, new ArgTst[] { ArgTst.Number, ArgTst.Vector, ArgTst.Vector });
            return new mcValue( nm.extrapolate(args[0].Number, args[1].getListNumber(), args[2].getListNumber()) );
        }

        // Calculate area of extrapolated function between X1 and X2
        public static mcValue areapolate(mcValue[] args)
        {
            testArgs(args, "areapolate", 1, 4, ArgTst.None, new ArgTst[] { ArgTst.Number, ArgTst.Number, ArgTst.Vector, ArgTst.Vector });
            return new mcValue(nm.areapolate(args[0].Number, args[1].Number, args[2].getListNumber(), args[3].getListNumber()));
        }

        // Average Y value between X1 and X2, weighted
        public static mcValue avgpolate(mcValue[] args)
        {
            testArgs(args, "avgpolate", 1, 4, ArgTst.None, new ArgTst[] { ArgTst.Number, ArgTst.Number, ArgTst.Vector, ArgTst.Vector });
            return new mcValue(nm.avgpolate(args[0].Number, args[1].Number, args[2].getListNumber(), args[3].getListNumber()));
        }

        #endregion


        #region PRIME functions ( INT/double )

        // get prime range, from n-th to m-th prime
        public static mcValue prime(mcValue[] args)
        {
            testArgs(args, "Prime(n[,m])", 1, 2, ArgTst.Int);
            int n = args[0].Int;
            if (args.Length == 1) return new mcValue(nm.prime(n));
            // if two arguments, return range
            int m = args[1].Int;
            return new mcValue(nm.prime(n, m));
        }

        // return true if number is prime
        public static mcValue isPrime(mcValue nv)
        {
            testArg(nv, "isPrime(n)", ArgTst.Int);
            return new mcValue(nm.isPrime(nv.Int));
        }

        // find position of next prime larger or equal to given number. 
        // used in nextPrime() and primePi()
        // in undefined case (k==0, and it is not prime), ofsZero decide what to return:
        //      ofsZero=-1: return next smaller
        //      ofsZero= 0: throw an exception if k==0 and N is not a prime
        //      ofsZero=+1: return next larger (+2 is 2nd next larger etc...)
        public static mcValue nextPrime(mcValue[] args)
        {
            testArgs(args, "nextPrime(n[,k]) ", 1, 2, ArgTst.Int);
            int n = args[0].Int;
            if (args.Length == 1) return new mcValue(nm.nextPrime(n));
            // if two arguments, return range
            int k = args[1].Int;
            return new mcValue(nm.nextPrime(n, k));
        }

        // number of primes smaller or equal to N
        public static mcValue primePi(mcValue nv)
        {
            testArg(nv, "primePi(n)", ArgTst.Int);
            return new mcValue(nm.primePi(nv.Int));
        }



        // primesBetween(a,b) - all primes between and including two numbers
        // unlike prime(a,b) which returns from a-th to b-th prime, this returns a<= prime <=b
        //   primesBetween( 10, 13) = vec (11,13)
        //   prime(10,13) = vec( 41,...)
        public static mcValue primesBetween(mcValue[] args)
        {
            testArgs(args, "primesBetween(a,b)", 2, 2, ArgTst.Int);
            int a = args[0].Int;
            int b = args[1].Int;
            return new mcValue(nm.primesBetween(a, b));
        }


        // primeFactorsTuples(n) return all prime factors of n as List< Tuple<int,int>>
        // where each Tuple<int,int> represent prime, and how many time it repeats
        // so primeFactorsTuples(600)=[(2,3),(3,1),(5,2)]
        public static mcValue primeFactorsPowers(mcValue nv)
        {
            testArg(nv, "primeFactorsPowers(n)", ArgTst.Int);
            var pfp = nm.primeFactorsTuples(nv.Int);
            // convert it to list of vectors
            var res = new List<mcValue>();
            foreach (var f in pfp)
            {
                var v = new List<long>(2) { f.Item1, f.Item2 };
                res.Add(new mcValue(v));
            }
            return new mcValue(res);
        }



        // primeFactors(n) return all prime factors of n as List<int>
        // where primes can be repeated,as in : primeFactors(600)= [2,2,2,3,5,5]
        public static mcValue primeFactors(mcValue nv)
        {
            testArg(nv, "primeFactors(n)", ArgTst.Int);
            return new mcValue(nm.primeFactors(nv.Int));
        }

        // primeFactorsDistinct(n) return distinct prime factors of n
        public static mcValue primeFactorsDistinct(mcValue nv)
        {
            testArg(nv, "primeFactorsDistinct(n)", ArgTst.Int);
            return new mcValue(nm.primeFactorsDistinct(nv.Int));
        }


        // factors(n) return all integer divisors of number N
        public static mcValue factors(mcValue nv)
        {
            testArg(nv, "factors(n)", ArgTst.Int);
            return new mcValue(nm.factors(nv.Int));
        }



        #endregion

    }



}
