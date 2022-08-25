using System;
using System.Collections.Generic;
using Numbers;



namespace CalculatorNotepad
{


    public partial class mcFunc
    {

        // public function fields
        public string Name; // for debugging
        public mcFuncType FuncType { get; private set; }
        public mcFuncTrinary _isRecursive= mcFuncTrinary.Unknown;       // this func calls itself 
        public mcFuncTrinary _forcedConstant = mcFuncTrinary.Unknown;   // 'Not' if this should never be constant, 'Yes' to always be const, 'Unknown' to evaluate
        public int stackCount = 0;  // number of times this func evaluation is entered without leaving
        public mcFuncFlags flags = mcFuncFlags.None;

        // Delegate
        public delegate mcValue mcEvaluate(List<mcValue[]> Params, bool resolveLambdas = false);
        public mcEvaluate Evaluate;

        // internal function fields
        mcValue constValue;                     // a) constant or parameter index
        Func<mcValue[], mcValue> directFunct;   // b) direct calculation using c# lambdas and only input parameters
        mcFunc[] factors;                       // c) subfunction with its factors, using parameters to calc factors, then using factor results as new parameters to subFunct
        mcFunc subFunct;
        int? subFunctLevel; // if ==null, this is not potential lambda func. Otherwise, it IS lambda, unless params depth >=subfuncLevel 



        // constructors

        public mcFunc(mcValue value, string name = "")
        {
            constValue = value;
            Name = name;
            FuncType = mcFuncType.Constant;
            updateEvalDelegate();
        }

        public mcFunc(double constant, string name="")
        {
            constValue = new mcValue(constant);
            Name = name;
            FuncType = mcFuncType.Constant;
            updateEvalDelegate();
        }

        public mcFunc(Number constant, string name = "")
        {
            constValue = new mcValue(constant);
            Name = name;
            FuncType = mcFuncType.Constant;
            updateEvalDelegate();
        }


        public mcFunc(int paramDepth, int paramIdx, string name = "")
        {
            FuncType = mcFuncType.Parameter;
            // combine param depth and index in one integer, max 1000 params in one depth
            constValue = new mcValue(paramDepth*1000+ paramIdx);
            Name = name+"_" + paramDepth+"#"+ paramIdx;
            updateEvalDelegate();
        }
        // direct func constructor, used mainly to define built in functions 
        public mcFunc(Func<mcValue[], mcValue> directFunc, mcFuncTrinary forcedConstant = mcFuncTrinary.Unknown)
        {
            FuncType = mcFuncType.DirectFunc;
            _forcedConstant = forcedConstant;
            directFunct = directFunc;
            Name = "";
            updateEvalDelegate();
        }
        // double direct func, should be avoided ( useful during transition from double to mcvalue)
        public mcFunc(Func<mcValue[], double> directFunc, mcFuncTrinary forcedConstant = mcFuncTrinary.Unknown ) 
        {
            FuncType = mcFuncType.DirectFunc;
            _forcedConstant = forcedConstant;
            directFunct =  args=> new mcValue( directFunc(args) );
            Name = "";
            updateEvalDelegate();
        }
        // Number direct func
        public mcFunc(Func<mcValue[], Number> directFunc, mcFuncTrinary forcedConstant = mcFuncTrinary.Unknown)
        {
            FuncType = mcFuncType.DirectFunc;
            _forcedConstant = forcedConstant;
            directFunct = args => new mcValue(directFunc(args));
            Name = "";
            updateEvalDelegate();
        }
        // subFunc constructor, used mainly by parser
        public mcFunc(mcFunc subFunction, mcFunc[] factorParameters, int? subLevel=null, string name = "", mcFuncTrinary isRecursiveCall = mcFuncTrinary.Unknown, mcFuncTrinary forcedConstant = mcFuncTrinary.Unknown)
        {
            Name = name;
            if (Name == "") Name = subFunction.Name;
            FuncType = mcFuncType.SubFunc;
            subFunct = subFunction;
            factors = factorParameters;
            _isRecursive = isRecursiveCall;
            _forcedConstant = forcedConstant;
            subFunctLevel = subLevel;
            updateEvalDelegate();
            // check if this function (and its arguments) can be optimized to constant 
            if ((!mc.cfg.disableConstantOptimization) && isConstant())
            {
                constValue = EvaluateFunc(); // same as Evaluate(new List<mcValue[]>())
                //constValue = EvaluateFunc(new List<mcValue[]>() { calcFactors(Params) });
                FuncType = mcFuncType.Constant;
                subFunct = null;
                factors = null;
                updateEvalDelegate();
            }
        }

        // methods


        public override string ToString()
        {
            string res = "";
            switch (FuncType)
            {
                case mcFuncType.Constant:  return constValue.ToString();
                case mcFuncType.Parameter: return "#" + constValue.ToString();
                case mcFuncType.DirectFunc: return "L_'" + Name + "'";
                case mcFuncType.SubFunc:
                    {
                        //return Name + "(" + factors.Length + ")";
                        res = Name;
                        res+= "("; 
                        if (factors != null)
                            for (int i = 0; i < factors.Length; i++)
                                res += ((factors[i]!=null)?factors[i].ToString():"") + (i < factors.Length - 1 ? "," : "");
                        res += ")";
                        return res;
                    }
                default: return "??_'" + Name + "'["+(factors!=null?factors.Length:0)+"]";
            }
        }

        public void CopyFrom(mcFunc source) // shallow copy from other function
        {
            FuncType = source.FuncType;
            constValue = source.constValue;
            factors = source.factors;
            subFunct = source.subFunct;
            directFunct = source.directFunct;
            Name = source.Name;
            _isRecursive = source._isRecursive;
            updateEvalDelegate();
        }

        // recursively determine if this function is recursive (which itself is recursion :p)
        public bool isRecursive()
        {
            if (_isRecursive == mcFuncTrinary.Unknown)
            {
                // constants, variables, params, even lambdas ... are not recursive
                _isRecursive = mcFuncTrinary.Not;
                // but functions can be recursive
                if (FuncType == mcFuncType.SubFunc)
                {
                    // if any of factors is recursive, this one is also recursive
                    if (factors!=null)
                    foreach (var fc in factors)
                        if ((fc!=null) &&fc.isRecursive())
                            _isRecursive = mcFuncTrinary.Yes;
                    // otherwise this one is recursive if subfunc is recursive
                    if ((_isRecursive != mcFuncTrinary.Yes) && subFunct.isRecursive())
                        _isRecursive = mcFuncTrinary.Yes;
                }
            }
            // return result, and in future it will be cached 
            return (_isRecursive == mcFuncTrinary.Yes);
        }



        // recursively determine if this func is constant equivalent (can not be changed later on) - meaning it does not use outside variables or non-constant parameters
        public bool isConstant(bool[] constFactors=null)
        {
            // if forced constant, return desired force (yes= always constant, Not=never constant)
            if (_forcedConstant != mcFuncTrinary.Unknown)
                return (_forcedConstant == mcFuncTrinary.Yes);
            // otherwise normal evaluate
            if (_isRecursive == mcFuncTrinary.Yes) // if this function is recursive, mark it as not constant to avod infinite loops ( even if fact(5) is constant )
                return false;
            // otherwise find out
            bool inner()
            {
                switch (FuncType)
                {
                    case mcFuncType.Constant: return true; // constant is obviously constant (unless it is of type mcValue.Func, TODO!!! )
                    case mcFuncType.DirectFunc:
                        {
                            if (constFactors == null) return false; // directFunc with no arguments is not a constant (eg. lastResult ). 
                                                                    // since we do not know which arguments directFunc use, we must assume ALL - meaning it is only constant if ALL factors are constant
                            foreach (var cf in constFactors)
                                if (cf == false)
                                    return false;
                            return true;
                        }
                    case mcFuncType.Parameter:
                        // if parameter is used, and that parameter is not passed as constant, then this mcFunc can not be constant
                        {
                            int parNum = constValue.Int;
                            if ((constFactors == null) || (parNum >= constFactors.Length)) return false; // if constfactors[] undefined, assume open parameters - so not const 
                            return constFactors[parNum];    // if this parameter is passed as constant, then func remains constant
                        }
                    case mcFuncType.SubFunc:
                        // assuming function itself can not be redefined in future, if all arguments are constants - then result is unchangeable constant also
                        {
                            // if functions can be redefined, then this one can not be constant
                            if (mc.cfg.allowFuncRedefinition) return false;
                            // if this is lambda function, prevent constamt
                            if (subFunctLevel.HasValue) return false;
                            // if no factors, this func is constant if subfunc is constant
                            if (factors == null) return subFunct.isConstant(null);
                            // otherwise calculate if each factor is constant or not, based on input arguments
                            var thisConstFactors = new bool[factors.Length];
                            for (int i = 0; i < factors.Length; i++)
                                thisConstFactors[i] = factors[i]!=null?factors[i].isConstant(constFactors):true;
                            // this func is constant if subfunc is constant, given info about constantability of arguments
                            return subFunct.isConstant(thisConstFactors);
                        }
                }
                return true;
            }
            // recursive test if this is constant
            var res = inner();
            // can not just simply cache to avoid future testing:  named functions may be constant with const parameters, but not with others: sqrt(5) vs sqrt(x)
            if (res && (constFactors == null)) _forcedConstant = mcFuncTrinary.Yes; // but if it is constant even without any const parameter, it will always be const (like pi, e ..)
            //_forcedConstant = res? mcFuncTrinary.Yes: mcFuncTrinary.Not; 
            return res;
        }

        // funcCache management
        public bool TryFuncCache(List<mcValue[]> args, out mcValue res)
        {
            if ((flags & mcFuncFlags.User) == 0) { res = null; return false; } // only cache topmost USER functions, those : a) certainly do not depend on outside variables(?!) and b) are not direct functions like builtins
            if ((flags & mcFuncFlags.HasSideEffects) != 0) { res = null; return false; } // do not cache if function has sideeffects (if use outside var, we can not predict result; if set outside var, we can not skip evaluation to allow that setting )
            if (!isRecursive()) {  res = null;  return false; } // and only for recursive functions (no need to cache others)
            if (mc.cfg.disableCacheOptimization) { res = null; return false; }; // cache optimizations are disabled
            if (mc.cacheFunc == null)
                mc.cacheFunc = new Dictionary<mcFuncCacheKey, mcValue>(new mcFuncCacheEqualityComparer());
            var key = new mcFuncCacheKey(this, args);
            bool ok= mc.cacheFunc.TryGetValue(key, out res);
            return ok;
        }
        public void AddFuncCache(List<mcValue[]> args, mcValue res)
        {
            if ((flags & mcFuncFlags.User) == 0) return; // only cache topmost USER functions, those : a) certainly do not depend on outside variables(?!) and b) are not direct functions like builtins
            if ((flags & mcFuncFlags.HasSideEffects) != 0) return; // do not cache if function has sideeffects (if use outside var, we can not predict result; if set outside var, we can not skip evaluation to allow that setting )
            if (!isRecursive())  return;  // and only for recursive functions (no need to cache others)
            if (mc.cfg.disableCacheOptimization) return; // cache optimizations are disabled
            if (mc.cacheFunc == null)
                mc.cacheFunc = new Dictionary<mcFuncCacheKey, mcValue>(new mcFuncCacheEqualityComparer());
            var key = new mcFuncCacheKey(this, args);
            mc.cacheFunc[key] = res;
        }

        // deep copy of mcFunc, manage func chains - use global mc dictionary for links, that needs to be cleared before mass copy
        // deep copy is needed when func can be redefined later by USER : 
        //      - for example at save point f(x)=3*x, but few lines later f(x)=2+x ... if it was not deep copy , it would change saved one too 
        //      - variables always need deep copy to preserve their value, since they can always be reassigned later. BUT they are now stored separately
        //      - optimization possible if func redefinition not allowed 
        //      - alternative way:  if func redef not allowed, ONLY save/restore mcParse[] array (with same pointers to fions that will exist at end, only missing future fions added), and save separately variable values, but do not clone functions
        public mcFunc DeepCopy()
        {
            // check if copy was already made ( assume Dictionary hash and compare mcFunc key by reference )
            mcFunc thecopy;
            if (mc.deepCopyLinks.TryGetValue(this, out thecopy))
                return thecopy;
            // optimizations: 
            //{ Constant, Parameter, DirectFunc, Variable, SubFunc }
            //      - if redefinition of all func is disabled, then it does not need deep copy if it is function (variables still need copy)
            //      - if this function is built in function and redefinition of builtins or all func  is disabled, then it does not need deep copy
            //      - BUT if func uses or assign variables, and variables will be cloned, then func also need to be cloned !!! Only builtIn func are safe, since they do not use/declare variables

            //if (FuncType!= mcFuncType.Variable) // variables can always be redefined by user. (obsolete) NOT stored as mcFuncs anymore
            if ( (((flags&mcFuncFlags.BuiltIn)!=0) && !mc.cfg.allowBuiltInRedefinition) || !mc.cfg.allowFuncRedefinition)
            {
                mc.deepCopyLinks.Add(this, this); // but still insert selfreference in dictionary, to speedup future search
                return this;  // then return itself
            }
            // otherwise make shallow copy as first step, and immediately insert in dictionary (in case of recursions)
            thecopy = (mcFunc)MemberwiseClone();
            mc.deepCopyLinks.Add(this, thecopy);
            // since value is mcValue, and can also store functions, make deep copy of that one too
            if (constValue != null)
                thecopy.constValue = constValue.DeepCopy();
            // since factors are also mcFunc, make deep copy of each factor
            if (factors != null)
            {
                thecopy.factors = new mcFunc[factors.Length];
                for (int i = 0; i < factors.Length; i++)
                    thecopy.factors[i] = factors[i]!=null? factors[i].DeepCopy():null;
            }
            // since subFunc is also mcFunc, make deep copy of that too
            if (thecopy.subFunct != null)
            {
                thecopy.subFunct = thecopy.subFunct.DeepCopy();
            }
            // update eval delegate in copy, probably not needed since types of func/subfunc remain same
            thecopy.updateEvalDelegate();
            // since reference to new clone was already made and stored in Dictionary, just return it
            return thecopy;
        }


        // gets internal constant value, used by debug functions
        public mcValue getConstantValue()
        {
             return constValue;
        }

        // gets internal subfunc
        // used in assignments, when direct manipulaiton of that func is needed
        public mcFunc getSubFunc()
        {
            return subFunct;
        }


    }

    // class with parsing data for functions (number of parameters, type of operands...)
    public enum mcFuncParamType { None, Prefix, Sufix, Between, Func, Variable };

    public class mcFuncParse
    {
        public string Name;
        public mcFuncParamType ParamType;
        public int NumParameters; // 0 = constant function, 1= for most prefix/sufix, 2= for operators like +-*/, -1= for unknown/variable 
        public int Priority; //  90=func, 70=unary +- 50=^  40=*/ 30=+- 20= ><= 10=|&
        public mcFunc funcCalc; // actual function for calculation
        public bool isUserFunction; // true for user defined functions
        public string formatExample; // pSim( ()=> bool , N )
        public string description;   // call function N times, and return success rate (num.true/N)
        public int[] block;     // block hierarchy. null for built-in and C# functions, Length==1 for topmost functions, Length > 1 for inner functions
        public bool hasSideEffects; // true if this function uses outside variables or other functions with side effects
        public int sideEffectsDepth; // uppermost used sideeffect outside variable
        // constructor
        public mcFuncParse(string name, mcFuncParamType paramType, int numParameters, int priority, mcFunc funcTree, bool isUser = false, string Description="", string FormatExample = "" )
        {
            Name = name;
            ParamType = paramType;
            NumParameters = numParameters;
            Priority = priority;
            funcCalc = funcTree;
            isUserFunction = isUser;
            description = Description;
            formatExample = FormatExample;
            if (funcCalc.Name == "")
                funcCalc.Name = name;
        }
        // constructot that requires description
        public mcFuncParse(string name, mcFuncParamType paramType, int numParameters, int priority, mcFunc funcTree, string Description, string FormatExample="", bool isUser = false)
        {
            Name = name;
            ParamType = paramType;
            NumParameters = numParameters;
            Priority = priority;
            funcCalc = funcTree;
            description = Description;
            formatExample = FormatExample;
            if (funcCalc.Name == "")
                funcCalc.Name = name;
            isUserFunction = isUser;
        }

        // deep copy, that does also mcFunc deep copy - use global mc dictionary for links, that needs to be cleared before mass copy
        public mcFuncParse DeepCopy()
        {
            var thecopy= (mcFuncParse)MemberwiseClone();
            if (thecopy.funcCalc != null)
                thecopy.funcCalc = thecopy.funcCalc.DeepCopy();
            return thecopy;
        }
    }


}
