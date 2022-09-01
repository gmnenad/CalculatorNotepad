using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculatorNotepad
{

    // parts of mcFunc that are doing run-time evaluation of value
    public partial class mcFunc
    {



        // evaluate without parameters, used for top level functions, called from mcExp or mc.
        public mcValue EvaluateFunc()
        {
            // empty evaluations need to have one depth level for params, so can not use Evaluate(null) - lambdas will report lambdaDepth< paramsDepth
            // also cannot use Evaluate(new List<mcValue[]>() { null }) - since ...?
            //return Evaluate(new List<mcValue[]>() { new mcValue[0] });

            // call Evaluate delegate,  initially this points to InitialEvaluate
            return Evaluate(new List<mcValue[]>());
        }



        // determine if fixed subdelegate can be linked instead of full eval delegate
        // it is done here, after first eval call, instead of compile time, since new functions are created with dummy subFunc allow recursion, which are later changed,
        // and logic of determining specific delegate depends on those elements that are later changed. So instead this relinking is done in runtime
        public mcValue InitialEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            // this is first call, so define which will be future EVAL delegate
            switch (FuncType)
            {
                case mcFuncType.Constant:
                    Evaluate = ConstEvaluate;
                    break;
                case mcFuncType.Parameter:
                    Evaluate = ParameterEvaluate;
                    break;
                case mcFuncType.DirectFunc:
                    Evaluate = DirectEvaluate;
                    break;
                case mcFuncType.SubFunc:
                    // try to specialize deeper, based on subFunc Name and Type
                    // unless function is lambda (has subFunctLevel) , in which case it will remain on generic SubFuncEvaluate
                    Evaluate = SubFuncEvaluate;
                    if (!subFunctLevel.HasValue)
                    {
                        if (subFunct.FuncType == mcFuncType.Constant) Evaluate = SubConst_FuncEvaluate;
                        else
                        if (subFunct.FuncType == mcFuncType.Parameter)  Evaluate = SubParameter_FuncEvaluate;
                        else
                        if (subFunct.Name == "return") Evaluate = Return_FuncEvaluate;
                        else
                        if (factors == null) Evaluate = NoFactors_FuncEvaluate;
                        else
                        if (subFunct.Name == "blockintr") Evaluate = Block_FuncEvaluate;
                        else
                        if (subFunct.Name == "while") Evaluate = While_FuncEvaluate;
                        else
                        if (subFunct.Name == "for") Evaluate = For_FuncEvaluate;
                        else
                        if ((subFunct.Name == "dowhile")|| (subFunct.Name == "do")) Evaluate = DoWhile_FuncEvaluate;
                        else
                        if (subFunct.Name == "if") Evaluate = IF_FuncEvaluate;
                        else
                            Evaluate = Normal_FuncEvaluate;
                    }
                    break;
                default:
                    throw new ArgumentException("Invalid mcFuncType for Evaluate !");
            }
            // and now call generic delegate first time, to avoid calling itself
            //return TheEvaluate(Params, resolveLambdas); 
            return Evaluate(Params, resolveLambdas);
        }


        // combined Evaluate, used just once at start - redirect based on mcFuncType
        // resolveLambdas is used only from mcValue.Evaluate() , to force lambda evaluation
        public mcValue TheEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            switch (FuncType)
            {
                case mcFuncType.Constant: return ConstEvaluate(Params, resolveLambdas);
                case mcFuncType.Parameter: return ParameterEvaluate(Params, resolveLambdas);
                case mcFuncType.DirectFunc: return DirectEvaluate(Params, resolveLambdas);
                case mcFuncType.SubFunc: return SubFuncEvaluate(Params, resolveLambdas);
                default:
                    throw new ArgumentException("Invalid mcFuncType for Evaluate !");
            }
        }



        // reset eval func delegate, it will be switched to specific on next Evaluate() call
        // called from all constructors, and on DeepCopy and CopyFrom
        // it just link to InitialEvaluare, that will do actual specification in runtime
        void updateEvalDelegate()
        {
            // set to generic 'Initial' evaluate, which will select specific one only on first call
            // this delayed switch is needed, since recursive functions start as 'const' in parse/create phase, and thus would incorrectly select
            // by using InitialEvaluate, selection is moved to runtime, ie Evaluate() phase
            Evaluate = InitialEvaluate;
        }



        // evaluate for mcFuncType.Constant 
        public mcValue ConstEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            // no stack increases (since it immediatelly return
            // no stack guards (since it can not call further functions)
            // no timeout check (since it is fast, and at least one top level func need to be called also )
            return constValue;
        }

        // evaluate for mcFuncType.Parameter
        public mcValue ParameterEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            return evalParam(Params);
        }

        // evaluate for mcFuncType.DirectFunc
        public mcValue DirectEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            return directFunct((Params != null) && (Params.Count > 0) ? Params[0] : null);
        }


        //****************************************
        //***** Evaluates for mcFuncType.SubFunc :

        // collective delegate for subFunc
        // it is called only when more specific sub_delegates were not possible to determine up front (if subFuncLevel.HasValue)
        // it calls each individual sub_delegate, so logic is kept only in sub_delegate
        public mcValue SubFuncEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            // detecting lambda call ( anonymous function call without last depth parameters )
            // in that case return this function wrapped in mcValue, with Params[] also stored in mcValue
            // but if resolveLambdas=true (when called from mcValue.Evaluate() ), do not wrap but actually evaluate function
            // there is no need for caching those, so it can return immediatelly (finally will catch stack--)
            // this is only function that is also not sub_delegate, since it depends on resolveLambda (false when called from mc.Evaluate, true when called from mcValue.Evaluate)
            if (subFunctLevel.HasValue && !resolveLambdas)
                return evalLambdas(Params);

            // if subfunc returns parameter, variable or constant, there is no need to evaluate factors, nor there is need for caching 
            // also, paramater subfunc needs params as input, not factors
            // this is not covered simply by (factors==null) clause below, since some optimized constants/params retain factor list
            if ((subFunct.FuncType == mcFuncType.Parameter) || (subFunct.FuncType == mcFuncType.Constant))
                return SubConst_FuncEvaluate(Params, resolveLambdas);

            // if this is 'return'  command, it does not benefit from cache, and it also can be without factors
            if (subFunct.Name == "return")
                return Return_FuncEvaluate(Params, resolveLambdas);

            // if factors are not defined, pass parameters, so wrapped anon functions can do work also
            // null factors may be passed for lambda resolutions, on lambda parameter array[]={  null,null, lastDepthParams }
            if (factors == null)
                return NoFactors_FuncEvaluate(Params, resolveLambdas);

            // block( expA, expB, expC )
            // block just execute each factor, and returns value of last one
            if (subFunct.Name == "blockintr")
                return Block_FuncEvaluate(Params, resolveLambdas);

            // while(cond) body; - is using below func case also
            // while( [initialDef] , condition [,loopExp [,returnValue]] );
            if (subFunct.Name == "while")
                return While_FuncEvaluate(Params, resolveLambdas);

            // for( initializer; condition; iterator) body;
            if (subFunct.Name == "for")
                return For_FuncEvaluate(Params, resolveLambdas);

            // do {body} while(cond); - is using below func case also
            // dowhile ([initialDef], [body] , condition [,returnValue]] )
            if (subFunct.Name == "dowhile")
                return DoWhile_FuncEvaluate(Params, resolveLambdas);

            // for IF, evaluate only condition and needed argument, to allow recursions
            if (subFunct.Name == "if")
                return IF_FuncEvaluate(Params, resolveLambdas);

            //*************************************************************************
            // for all other functions, evaluate all factors, then execute subFunc
            //*************************************************************************
            return Normal_FuncEvaluate(Params, resolveLambdas);

        }





        // guards against stack overflows, used in most of specific eval delegates
        static long stackGuard0; // stack value at start (depth 0)
        static long stackLastFree; // last free stack size
        static double stackAvgPerCall; // average bytes per call/depth used on stack
        static long stackCountCall; // stack count for each function, that is not rolled back by function returns
        static long stackCountDepth; // highest stackCount seen - since fib(x-1)+fib(x-2) can break on '+' with stackCount==1

        static StackGuard stackGuard = new StackGuard();

        public static void resetGuards() =>  stackGuard.resetCounters();

        // stack count increase and timeout/overflow checks
        // called from most delegates at start, and doing stackCount--; in finally section
        void stackPlus()
        {
            if (mc.isFuncTimeout()) throw new ArgumentException("ERR:Timeout");
            stackCount++;
            stackGuard?.checkStackOverflow(stackCount);
        }




        // eval Params - separated to reduce stack
        mcValue evalParam(List<mcValue[]> Params)
        {
            int paramIdx2 = constValue.Int;
            int paramDepth = paramIdx2 / 1000;
            int paramNum = paramIdx2 % 1000;
            // sometimes when optimizing for constant, evaluation can be called without parameters even if func have one in def. Eg:  c(x)=5  ;  evaluate(null) ... so return 0 to avoid exceptions
            if ((Params == null) || ((Params.Count == 1) && (Params[0] == null)))
                return new mcValue();
            // check params index
            if ((Params.Count <= paramDepth) || (Params[paramDepth] == null) || (Params[paramDepth].Length <= paramNum))
                //return new mcValue();
                throw new ArgumentException("Undefined index #" + paramNum + (paramDepth > 0 ? " on depth " + paramDepth : ""));
            // return param value
            return Params[paramDepth][paramNum];
        }

        // eval Lambdas - separated to reduce stack
        // this wraps current function and its parameters in mcValue, and returns it as 'lambda' value
        // it does not do any evaluation on its own, so no need to stack/cache...
        mcValue evalLambdas(List<mcValue[]> Params)
        {
            if (Params == null)
                Params = new List<mcValue[]>();
            else
                Params = new List<mcValue[]>(Params); // make shallow copy of list, so that input list is unchanged
            // params for unresolved lambda should always be one level under subFunctLevel, so when resolve appends its final parameters, they match subfunclevel
            while (Params.Count > subFunctLevel)
                Params.RemoveRange(subFunctLevel.Value, Params.Count - subFunctLevel.Value);
            while (Params.Count < subFunctLevel)
                Params.Add(null);
            // wrap this function and modified Params in mcValue
            return new mcValue(this, Params);
        }

        // eval Return - separated to reduce stack
        mcValue evalReturn(List<mcValue[]> Params)
        {
            // evaluate result value (factor[0]) if present, and return from function
            mcValue res = null;
            if ((factors != null) && (factors.Length > 0))
                res = factors[0].Evaluate(Params);
            else
            {
                res = new mcValue();
                res.flags |= mcValueFlags.Incomplete; // return value was not set, so return last evaluated from block/loop
            }
            res.flags |= mcValueFlags.Return; // this is value from 'return' function, and need to abort loops and be propagated further
            return res;
        }

        // Helper subfunc to test for return command
        // return true if we need to abort block/loop, and return lastRest as result
        bool testReturn(mcValue previousRes, ref mcValue lastRes)
        {
            if (previousRes != null)
            {
                // is this return request
                if ((previousRes.flags & mcValueFlags.Return) != 0)
                {
                    // is it incomplete, so we need to return our lastRes?
                    if (((previousRes.flags & mcValueFlags.Incomplete) != 0) && (lastRes != null))
                    {
                        previousRes = lastRes;
                        previousRes.flags |= mcValueFlags.Return;
                    }
                    // set this result as lastRes, and indicate we need to return it
                    lastRes = previousRes;
                    // strip return flag if this is parent/user function
                    if ((flags & mcFuncFlags.User)!=0)
                        lastRes.flags &= ~mcValueFlags.Return;
                    //Console.WriteLine("return upwards");
                    return true;
                }
                // otherwise just set lastRes to previousRes and return false (means we do not need to abort current block)
                lastRes = previousRes;
            }
            return false;
        }

        // subfunc to calculate values of each factor function in 'factors' based on passed parameters in 'Params' - convert mcFunc[] to mcValue[]
        mcValue[] calcFactors(List<mcValue[]> Params)
        {
            if (factors == null)
                return new mcValue[0];
            var factorValues = new mcValue[factors.Length];
            for (int i = 0; i < factors.Length; i++)
                if (factors[i] != null)
                    factorValues[i] = factors[i].Evaluate(Params);
                else
                    factorValues[i] = null;
            return factorValues;
        }


        // SubConst delegate, if subFunc is either constant or parameter
        public mcValue SubConst_FuncEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            try
            {
                stackPlus();
                return subFunct.Evaluate(Params, resolveLambdas);
            }
            finally { stackCount--; }
        }

        // SubParameter delegate, if subFunc is parameter ( should be called only for constant parameters )
        public mcValue SubParameter_FuncEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            try
            {
                stackPlus();
                if ((Params == null || Params.Count == 0) && factors != null && factors.Length>0 )
                {
                    // if no parameters are passes for this single parameter, assume it is param#0 and use first factor value
                    return factors[0].Evaluate(Params);
                }
                else
                    return subFunct.Evaluate(Params, resolveLambdas);
            }
            finally { stackCount--; }
        }


        // RETURN delegate
        public mcValue Return_FuncEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            try
            {
                stackPlus();
                mcValue res;
                if (TryFuncCache(Params, out res)) return res;
                res = evalReturn(Params);
                AddFuncCache(Params, res);
                return res;
            }
            finally { stackCount--; }
        }

        // NULL FACTOR delegate (for lambda calls)
        public mcValue NoFactors_FuncEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            try
            {
                stackPlus();
                mcValue res;
                if (TryFuncCache(Params, out res)) return res;
                // if factors are not defined, pass parameters, so wrapped anon functions can do work also
                // null factors may be passed for lambda resolutions, on lambda parameter array[]={  null,null, lastDepthParams }
                if (!resolveLambdas)
                    throw new ArgumentException("Null factor parameters outside of lambda resolution !");
                res = subFunct.Evaluate(Params, resolveLambdas);
                AddFuncCache(Params, res);
                return res;
            }
            finally { stackCount--; }
        }

        // NORMAl functions delegate 
        public mcValue Normal_FuncEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            try
            {
                mc.testFuncTimeout();
                stackPlus();
                mcValue res;
                if (TryFuncCache(Params, out res)) return res;
                // for all other functions, evaluate all factors, then execute subFunc
                var factorValues = calcFactors(Params);
                // call operation calculation with these new parameters. 
                res = subFunct.Evaluate(new List<mcValue[]>() { factorValues }, resolveLambdas);
                AddFuncCache(Params, res);
                return res;
            }
            finally { stackCount--; }
        }


        // IF delegate
        public mcValue IF_FuncEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            try
            {
                stackPlus();
                if ((factors == null) || (factors.Length < 2)) throw new ArgumentException("Invalid IF syntax !");
                // for IF, evaluate only condition and needed argument, to allow recursions
                mcValue res;
                if (TryFuncCache(Params, out res)) return res;
                if (factors[0].Evaluate(Params).isTrue())
                    res = factors[1].Evaluate(Params);
                else
                {
                    if ((factors.Length > 2) && (factors[2] != null))
                        res = factors[2].Evaluate(Params);
                    else
                        res = new mcValue();// if cond==false, but without else, retufn zero
                }
                AddFuncCache(Params, res);
                return res;
            }
            finally { stackCount--; }
        }


        // block delegate
        public mcValue Block_FuncEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            try
            {
                stackPlus();
                // block( expA, expB, expC )
                // block just execute each factor, and returns value of last one
                mcValue res, newRes, lastRes = null;
                if (TryFuncCache(Params, out res)) return res;
                for (int i = 0; i < factors.Length; i++)
                    if (factors[i] != null)
                    {
                        mc.testFuncTimeout();
                        newRes = factors[i].Evaluate(Params);
                        if (testReturn(newRes, ref lastRes))
                            break;
                        if (mc.showDbgValues)
                            mc.dbgShow(stackCount, Name.Replace("_blockintr","")+" line#" + i + " >");
                    }
                res = lastRes;
                AddFuncCache(Params, res);
                return res;
            }
            finally { stackCount--; }
        }


        // Helper sub func used by for & while  
        // prepare loop factors
        void prepLoop(List<mcValue[]> Params, int _initializer, int _condition, int _body, int _iterator, int _returnValue,
                          out mcFunc condition, out mcFunc body, out mcFunc iterator, out mcFunc returnValue, string formatExample = "")
        {
            // helper func, return factor at that index or null if factors too short
            mcFunc getFact(int n)
            {
                if ((factors == null) || (factors.Length <= n) || (n < 0)) return null;
                return factors[n];
            }
            // map all parameters to factors[]
            mcFunc initializer = getFact(_initializer);
            condition = getFact(_condition);
            body = getFact(_body);
            iterator = getFact(_iterator);
            returnValue = getFact(_returnValue);
            // check parameters
            if (condition == null) throw new ArgumentException(subFunct.Name + " must have condition defined. " + formatExample);
            // execute initializer first
            if (initializer != null)
                initializer.Evaluate(Params);
            mc.dbgShow(stackCount, subFunct.Name + " loop initializer >");
        }


        // called inside loop, check timeouts and optionally display debug info
        void loopStats(ref int nLoops)
        {
            nLoops++;
            // check document timeout and  reset on each step for single expressions
            if (mc.isDocTimeout()) throw new ArgumentException("ERR:Timeout");
            mc.testFuncTimeout(); // check Func timeout
            mc.dbgShow(stackCount, subFunct.Name + " loop step [" + nLoops + "] >");
            //mc.restartFuncTimeout(); // do not reset timeout - one for/while loop is called within one 'function'
        }


        // while delegate
        public mcValue While_FuncEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            // while(cond) body; - is using below func case also
            // while( [initialDef] , condition [,loopExp [,returnValue]] );
            try
            {
                stackPlus();
                mcValue newRes, lastRes = null;
                if (TryFuncCache(Params, out newRes)) return newRes;
                // mcValue res = doLoop(Params, 0, 1, 2, -1, 3);   // map all parameters to factors[]
                mcFunc condition, body, iterator, returnValue;
                prepLoop(Params, 0, 1, 2, -1, 3, out condition, out body, out iterator, out returnValue);
                // now do loop
                int nLoops = 0;
                while (condition.Evaluate(Params).isTrue())
                {
                    // if body defined, execute it
                    if (body != null)
                    {
                        newRes = body.Evaluate(Params);
                        if (testReturn(newRes, ref lastRes)) break;
                    }
                    // if iterator defined, execute it
                    if (iterator != null)
                    {
                        newRes = iterator.Evaluate(Params);
                        if (body == null)
                            if (testReturn(newRes, ref lastRes)) break;
                    }
                    // check document timeout and  reset on each step for single expressions
                    loopStats(ref nLoops);
                }
                // if returnValue is specified (and it was not return break above) , calc returnValue
                if ((returnValue != null) && ((lastRes == null) || ((lastRes.flags & mcValueFlags.Return) == 0)))
                    lastRes = returnValue.Evaluate(Params);
                // put in cache if needed, then return
                AddFuncCache(Params, lastRes);
                return lastRes;
            }
            finally { stackCount--; }
        }

        // FOR delegate
        public mcValue For_FuncEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            // for( initializer; condition; iterator) body;
            try
            {
                stackPlus();
                mcValue newRes, lastRes = null;
                if (TryFuncCache(Params, out newRes)) return newRes;
                //mcValue res = doLoop(Params, 0, 1, 3, 2, -1);  // map all parameters to factors[]
                mcFunc condition, body, iterator, returnValue;
                prepLoop(Params, 0, 1, 3, 2, -1, out condition, out body, out iterator, out returnValue);
                // now do loop
                int nLoops = 0;
                while (condition.Evaluate(Params).isTrue())
                {
                    // if body defined, execute it
                    if (body != null)
                    {
                        newRes = body.Evaluate(Params);
                        if (testReturn(newRes, ref lastRes)) break;
                    }
                    // if iterator defined, execute it
                    if (iterator != null)
                    {
                        newRes = iterator.Evaluate(Params);
                        if (body == null)
                            if (testReturn(newRes, ref lastRes)) break;
                    }
                    // check document timeout and  reset on each step for single expressions
                    loopStats(ref nLoops);
                }
                // if returnValue is specified (and it was not return break above) , calc returnValue
                if ((returnValue != null) && ((lastRes == null) || ((lastRes.flags & mcValueFlags.Return) == 0)))
                    lastRes = returnValue.Evaluate(Params);
                // put in cache if needed, then return
                AddFuncCache(Params, lastRes);
                return lastRes;
            }
            finally { stackCount--; }
        }

        // do-while delegate
        public mcValue DoWhile_FuncEvaluate(List<mcValue[]> Params, bool resolveLambdas = false)
        {
            // do {body} while(cond); - is using below func case also
            // dowhile ([initialDef], [body] , condition [,returnValue]] )
            try
            {
                stackPlus();
                mcValue newRes, lastRes = null;
                if (TryFuncCache(Params, out newRes)) return newRes;
                // mcValue res = doLoop(Params, 0, 1, 2, -1, 3);   // map all parameters to factors[]
                mcFunc condition, body, iterator, returnValue;
                prepLoop(Params, 0, 2, 1, -1, 3, out condition, out body, out iterator, out returnValue);
                // now do loop
                int nLoops = 0;
                do
                {
                    // if body defined, execute it
                    if (body != null)
                    {
                        newRes = body.Evaluate(Params);
                        if (testReturn(newRes, ref lastRes)) break;
                    }
                    // if iterator defined, execute it
                    if (iterator != null)
                    {
                        newRes = iterator.Evaluate(Params);
                        if (body == null)
                            if (testReturn(newRes, ref lastRes)) break;
                    }
                    // check document timeout and  reset on each step for single expressions
                    loopStats(ref nLoops);
                } while (condition.Evaluate(Params).isTrue());
                // if returnValue is specified (and it was not return break above) , calc returnValue
                if ((returnValue != null) && ((lastRes == null) || ((lastRes.flags & mcValueFlags.Return) == 0)))
                    lastRes = returnValue.Evaluate(Params);
                // put in cache if needed, then return
                AddFuncCache(Params, lastRes);
                return lastRes;
            }
            finally { stackCount--; }
        }



    }
}
