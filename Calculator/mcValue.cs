using System;
using System.Collections.Generic;
using Numbers;






namespace CalculatorNotepad
{
    public enum mcValueType { Number, Vector, Func };
    // keep values (constants) of different types
    // consider making it a struct instead of class? In any case, it should be nonvolatile after creation
    public class mcValue
    {
        // different value stores (in C++, they could have been union )
        private Number numberValue;
        private List<mcValue> vectorValue;
        private mcFunc funcValue;
        private List<mcValue[]> funcParams;
        // actual value type
        public mcValueType valueType { get; private set; }
        // vector length that works for scalars too. Since mcValue is immutable, can be set in constructor
        public int vectorLength { get; private set; }
        // special flags to mark this value:  
        public mcValueFlags flags;

        // private fields
        private int? _hashCode = null;


        // some properties

        /// <summary>
        /// return scalar floating point value, which is either Double or Quad or Mpfr( depending on compile setup )
        /// </summary>
        public Number Number { get => getNumber(); }

        /// <summary>
        /// return double value of mcValue, either if it is actual 64bit double or 128bit number downcasted to double
        /// </summary>
        public double Double { get => (double)getNumber(); }

        /// <summary>
        /// return int value of mcValue, either if it is actual int or number casted to int 
        /// </summary>
        public int Int { 
            get {
                var n = getNumber();
                if (n > int.MaxValue || n < int.MinValue)
                    throw new InvalidCastException("Number " + n.ToString() + " is too large to fin into Int ");
                return (int)n; 
            } 
        }

        /// <summary>
        /// return long value of mcValue, either if it is actual int or number casted to int 
        /// </summary>
        public long Long
        {
            get
            {
                var n = getNumber();
                if (n > long.MaxValue || n < long.MinValue)
                    throw new InvalidCastException("Number " + n.ToString() + " is too large to fin into Int ");
                return (long)n;
            }
        }


        /// <summary>
        /// return vector representation of value, valid for vectors and scalars (turned in vectors).  used in scalar transformation functions
        /// </summary>
        public List<mcValue> Vector { get => getVector(); }




        // constructors
        public mcValue()
        {
            // empty constructor is number/double (most used type) zero (as default value)
            numberValue = 0;
            vectorLength = 1;
            valueType = mcValueType.Number;
        }

        public mcValue(double x)
        {
            numberValue = x;
            vectorLength = 1;
            valueType = mcValueType.Number;
        }
        public mcValue(Number x)
        {
            numberValue = x;
            vectorLength = 1;
            valueType = mcValueType.Number;
        }

        public mcValue(bool x)
        {
            numberValue = x?1:0;
            vectorLength = 1;
            valueType = mcValueType.Number;
        }

        public mcValue(mcFunc theFunc, List<mcValue[]> theParams)
        {
            funcValue = theFunc;
            funcParams = theParams;
            vectorLength = 0;
            valueType = mcValueType.Func;
        }
        public mcValue(List<mcValue> x)
        {
            makeFromList(x);
        }
        // add Number list from base type list
        private void addList<T>(List<T> x, Func<T,Number> conv)
        {
            if (x != null)
            {
                var list = new List<mcValue>(x.Count);
                for (int i = 0; i < x.Count; i++)
                    list.Add(new mcValue( conv(x[i])));
                makeFromList(list);
            }
            else
                makeFromList(null);
        }
        public mcValue(List<int> x) => addList<int>(x, (a) => a);
        public mcValue(List<long> x) => addList<long>(x, (a) => a);
        public mcValue(List<double> x) => addList<double>(x, (a) => a);
        public mcValue(List<Number> x) => addList<Number>(x, (a) => a);


        // this makes new shallow copy, although deep links (vectors, func) remains
        public mcValue(mcValue x)
        {
            CopyFrom(x);
        }

        // make from list
        private void makeFromList(List<mcValue> x)
        {
            // silent conversion to one element if vector is length of one
            // does not mean it will be scalar (can be mcFunc, ... but not vector)
            if ((x != null) && (x.Count == 1) && (x[0].vectorLength<=1))
            {
                CopyFrom(x[0]);
            }
            else
            {
                vectorValue = x;
                vectorLength = vectorValue != null ? vectorValue.Count : 0;
                valueType = mcValueType.Vector;
            }
        }

        // shallow copy from other mcValue
        private void CopyFrom(mcValue x)
        {
            valueType = x.valueType;
            numberValue = x.numberValue;
            vectorValue = x.vectorValue;
            funcValue = x.funcValue;
            vectorLength = x.vectorLength;
        }

        // deep copy from other mcValue
        public mcValue DeepCopy()
        {
            var res = new mcValue(this);
            // do not deep copy funcValue if func redefinitions are not allowed
            if ((funcValue != null) && mc.cfg.allowFuncRedefinition)
                    res.funcValue = funcValue.DeepCopy();
            return res;
        }


        // hash functions, can be cached due to immutability
        public override int GetHashCode()
        {
            if (_hashCode.HasValue)
                return _hashCode.Value;
            int result = 17;
            void addHash(int hash)
            {
                unchecked{  result = result * 23 + hash;   }
            }
            // if vector[1], treat as scalar
            switch (valueType)
            {
                case mcValueType.Number: addHash(numberValue.GetHashCode()); break;
                case mcValueType.Func:
                    if (funcValue != null)
                        addHash(funcValue.GetHashCode());  // just pointer hash, but good enough
                    break;
                case mcValueType.Vector:
                    if (vectorLength == 1)
                        result = vectorValue[0].GetHashCode();
                    else
                    {
                        addHash(vectorLength);
                        if (vectorValue != null)
                            for (int i = 0; i < vectorLength; i++) // and each of vector elements
                                addHash(vectorValue[i].GetHashCode());
                    }
                    break;
            }
            _hashCode = result;
            return _hashCode.Value;
        }

        // strict equality to other mcValue object ( so Int 1 != double 1 != vector[1] )
        // alternative would be  return CMP(a,b)==0
        public override bool Equals(object obj)
        {
            if (!(obj is mcValue)) return false;
            var other = obj as mcValue;
            if ((valueType==mcValueType.Number)&&(other.valueType==mcValueType.Number)) // most frequent
                return other.numberValue == numberValue;
            if (vectorLength != other.vectorLength) // dimensions must match
                return false;
            if (isScalar() && other.isScalar()) // vector[1]&scalar combos. isScalar() instead of (len==1), since [ [1,2] ] has len==1, but is not scalar
                return getNumber() == other.Number;
            if (valueType != other.valueType) // beyond this only vector==vector, or func==func
                return false;
            if (valueType == mcValueType.Vector) // two vectors
            {
                if (other.vectorValue == vectorValue) return true; // if same class instance or both null
                if (vectorValue != null)
                {
                    // deep compare, for each of vector elements - allows compare to newly created array of consts
                    for (int i = 0; i < vectorLength; i++)
                        if (!vectorValue[i].Equals(other.vectorValue[i]))
                            return false;
                    return true;
                }
                else
                    return false;
            }
            if (valueType == mcValueType.Func) // two func
                return other.funcValue == funcValue;  // just pointer compare, but good enough
            // all cases should be covered above
            return false;
        }


        // compare two mcValues, returns +1 if first is larger, 0 if equal, -1 if second is larger
        // not-strict:  Int 1 == double 1 == vector[1]
        public static int CMP(mcValue a, mcValue b)
        {
            if ((a == null) && (b == null)) return 0;
            if ((a != null) && (b == null)) return +1;
            if ((a == null) && (b != null)) return -1;
            // if two scalar values ( or vectors with single element)
            if (a.isScalar() && b.isScalar())
                return Number.CmpAx(a.Number, b.Number);
            // if two vectors ( or scalar & vector )
            if (a.isVector() && b.isVector())
            {
                int i = 0;
                // compare vectors like strings: first difference determine 
                while ((i < a.vectorLength) && (i < b.vectorLength))
                {
                    int res = CMP(a.ElementAt(i), b.ElementAt(i));
                    if (res != 0)
                        return res;
                    i++;
                }
                // if no difference, longer vector is larger
                return a.vectorLength.CompareTo(b.vectorLength);
            }
            // if neither combination of scalars and vectors, it means at least one is mcFunc
            // then compare hashes, so that two same mcFunc will still return equality (==0)
            return a.GetHashCode().CompareTo(b.GetHashCode());
        }


        // true if value is scalar:  int or double, or vector with one scalar element
        // if isScalar==true, it is safe to use getDouble()
        public static bool isScalar(mcValue x)
        {
            if (x == null) return false;
            if (x.valueType == mcValueType.Number)
                return true;
            // check recursively first vector element, so that [ [1] ] is also scalar
            if ((x.valueType == mcValueType.Vector) && (x.vectorLength == 1) && x.vectorValue[0].isScalar())
                return true;
            return false;
        }
        public bool isScalar()
        {
            return isScalar(this);
        }

        // true if value is vector:  vector of any length (even zero) , or int or double
        // if isVector==true, it is safe to use ElementAt(index) for 0 <= index < vectorLength
        public static bool isVector(mcValue x)
        {
            if (x == null) return false;
            return (x.valueType != mcValueType.Func);
        }
        public bool isVector()
        {
            return (valueType != mcValueType.Func);
        }

        // true if this mcValue is function
        public static bool isFunction(mcValue x)
        {
            return (x != null) && (x.valueType == mcValueType.Func);
        }
        public bool isFunction()
        {
            return (valueType == mcValueType.Func);
        }



        // index operation, for vector (but works for int/double too if size[1]) - return element at that index position, starting with zero
        public mcValue ElementAt(int idx)
        {
            if (valueType == mcValueType.Vector)
                return vectorValue[idx]; // will throw on its own if out of bounds
            if ((idx == 0) && (valueType == mcValueType.Number))
                return this;
            throw new ArgumentException("Invalid index [" + idx + "] for this value  !");
        }


        // isConst is true if all parts are constants
        // recursively determine if this func is constant equivalent (can not be changed later on) - meaning it does not use outside variables or non-constant parameters
        public bool isConstant(bool[] constFactors = null)
        {
            switch (valueType)
            {
                case mcValueType.Number: return true;
                case mcValueType.Func:
                    if (funcValue == null) return true;
                    return funcValue.isConstant(constFactors);
                case mcValueType.Vector:
                    if (vectorValue != null)
                    {
                        // it is constant if every member of vector is constant
                        for (int i = 0; i < vectorValue.Count; i++)
                            if (!vectorValue[i].isConstant(constFactors))
                                return false;
                        return true;
                    }
                    else
                        return true;
            }
            return true;
        }

        // return float value of mcValue (if scalar, ie int or double, or vector[1] is scalar), otherwise throw "not scalar" exceptio
        // used in all float math functions that only work with scalars, slike sin, cos, even power ...
        private Number getNumber()
        {
            switch(valueType)
            {
                case mcValueType.Number: return numberValue;
                case mcValueType.Vector:
                    if ((vectorValue==null)||(vectorValue.Count!=1))
                        throw new ArgumentException("Can not convert vectors of size [" + vectorValue.Count + "] to scalar !");
                    return vectorValue[0].Number;
                default:
                    throw new ArgumentException("Can not convert to scalar !");
            }
        }


        // return vector representation of value, valid for vectors and scalars (turned in vectors).  used in scalar transformation functions
        public List<mcValue> getVector()
        {
            switch (valueType)
            {
                case mcValueType.Number: return new List<mcValue>() { new mcValue(numberValue) };
                case mcValueType.Vector: return vectorValue;
                default:
                    throw new ArgumentException("Can not convert to vector !");
            }
        }

        // return List<Number> if possible
        public List<Number> getListNumber()
        {
            var vec = getVector();
            var res = new List<Number>(vec.Count);
            for (int i = 0; i < vec.Count; i++)
                res.Add(vec[i].Number);
            return res;
        }

        // return List<double> if possible
        public List<double> getListDouble()
        {
            var vec = getVector();
            var res = new List<double>(vec.Count);
            for (int i = 0; i < vec.Count; i++)
                res.Add(vec[i].Double);
            return res;
        }

        // return List<int> if possible
        public List<int> getListInt()
        {
            var vec = getVector();
            var res = new List<int>(vec.Count);
            for (int i = 0; i < vec.Count; i++)
                res.Add(vec[i].Int);
            return res;
        }

        // return List<long> if possible
        public List<long> getListLong()
        {
            var vec = getVector();
            var res = new List<long>(vec.Count);
            for (int i = 0; i < vec.Count; i++)
                res.Add(vec[i].Long);
            return res;
        }


        // return function stored in mcValue
        // used in assignments, when direct manipulaiton of that func is needed
        public mcFunc getFunc()
        {
            return funcValue;
        }



        // evaluate function, using lambda (additional) parameters, with its own stored before
        public mcValue EvaluateFunc(mcValue[] lambdaParams)
        {
            if (funcValue==null)
                throw new ArgumentException("Can not evaluate undefined function in mcValue !");
            if (funcParams == null)
                funcParams = new List<mcValue[]>(); // should not be happening often, only for functions without any parameters passed before
            // append new layer of params
            // last depth must not be null, since null would indicate this is still incomplete lambda - and here we want to resolve incomplete lambda into complete one
            funcParams.Add(lambdaParams!=null? lambdaParams: new mcValue[0]); 
            // calculate function
            var res = funcValue.Evaluate(funcParams,true);
            // remove appended layer
            funcParams.RemoveAt(funcParams.Count - 1);
            //return result
            return res;
        }



        // true value (!= 0). maybe will need list of params as input
        public static bool isTrue(mcValue x) 
        {
            if (x == null) return false;
            switch (x.valueType)
            {
                case mcValueType.Number: return  Number.isTrueAx(x.numberValue);
                case mcValueType.Vector: if ((x.vectorValue != null) && (x.vectorValue.Count >0)) return true; break;
            }
            // what about Func?  it should evaluate it to decide if this is true?
            return false;
        }
        public bool isTrue()
        {
            return isTrue(this);
        }

        // return true if this is "integer", either by being Number int type, or double close to int
        // maybe will need list of params as input, for Func ?
        public static bool isInt(mcValue x) 
        {
            if (x == null) return false;
            switch (x.valueType)
            {
                case mcValueType.Number: return Number.isIntAx(x.numberValue);
                case mcValueType.Vector: if ((x.vectorValue!=null)&&(x.vectorValue.Count==1)) return x.vectorValue[0].isInt(); break;
            }
            return false;
        }
        public bool isInt()
        {
            return isInt(this);
        }


        // string representation, both of scalar values and vectors 
        public override string ToString()
        {
            return ToString("",-1);
        }


        public string ToString(string groupSeparator, int maxDecimals = -1, bool? showType=null)
        {
            switch (valueType)
            {
                case mcValueType.Number: return mcParse.NumberToStr(Number, groupSeparator, maxDecimals, showType);
                case mcValueType.Vector:
                    if (vectorValue == null) return "null";
                    var res = "v(";
                    for (int i = 0; i < vectorValue.Count; i++)
                        res += (vectorValue[i] != null? vectorValue[i].ToString(groupSeparator, maxDecimals):"null") + (i < vectorValue.Count - 1 ? "," : "");
                    return res + ")";
                case mcValueType.Func:
                    if (funcValue == null) return "null";
                    return "L_"+funcValue.Name;
            }
            return "";
        }


        // change value of vector at given index  ; used in mc.doVarAssign for v[5]= ... 
        // this invalidate mcValue immutability , and should be used with care
        // meaning no change of vector size or type 
        // and only used for changes inside variables, which are stored separately and can handle change of value
        // SLOWER alternative would be to always create new mcValue (nrrd changein mc.doVarAssign also )
        public mcValue setVectorIndex(int idx, mcValue newValue)
        {
            if (!isVector()) throw new ArgumentException("Setting index to value which is not a vector !");
            if (idx>= vectorLength) throw new ArgumentException("Setting index["+idx+"] out of bounds for a vector["+vectorLength+"] !");
            // if this is single number vector, change its own value (only if new value is also Number/double)
            if (vectorLength == 1)
            {
                if (newValue.valueType != mcValueType.Number)
                    throw new ArgumentException("Changing index 0 with non-Number value  !");
                numberValue = newValue.numberValue;
            }
            else
            {
                // if this is multivalue vector, change element in vector array
                vectorValue[idx] = newValue;
            }
            return this;
        }

        // append new element to vector, increasing length by 1  ; used in vAppend(...)
        //  RISKY !!!  Invalidate immutability:
        //      - v1=1; res=vec(); vAppend(res,v1); vAppend(v1,2); vAppend(res,v2) -> res== v( v(1,2), v(1,2) ) !! instead of v( 1, v(1,2) ) 
        // addFunc("vAppend", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nm.vAppend(args)), "Append new element to existing vector\r\nReturns expanded vector, but that vector is already expanded. In essence: vAppend(ref vector, newElement)\r\n     v1=vec(1,2); vAppend(v1, 3) // -> v1==v(1,2,3)", "vAppend ( vector, newElement )"));
        public mcValue appendVector(mcValue addValue)
        {
            if (!isVector()) throw new ArgumentException("Adding element to value which is not a vector !");
            // if this was empty vector, create scalar
            if (vectorLength == 0)
            {
                CopyFrom(addValue);
                //var list = new List<mcValue>(1) { addValue };
                //makeFromList(list);
            }
            else
            // if this is single element vector, it is actually scalar, so change to vector type
            if (vectorLength == 1)
            {
                var nv = new mcValue(this); // shallow copy of old scalar/func/etc single value
                var list = new List<mcValue>(2) { nv, addValue };
                makeFromList(list);
            }
            else
            // if this is already multivalue vector, append new element
            {
                vectorValue.Add(addValue);
                vectorLength = vectorValue != null ? vectorValue.Count : 0;
            }
            return this;
        }


        //  *************************************
        //  * math opperations like +-*/
        //  *************************************


        // apply operand either to single scalar value or to each scalar element of vector
        public static mcValue vec1(mcValue A, Func<Number, Number> op1func, bool AllowNULL = true)
        {
            // check for null
            if (A == null)
            {
                if (!AllowNULL) throw new ArgumentException("Argument is NULL !");
                A = new mcValue((double)0);
            }
            // scalars are always allowed
            if (A.valueType == mcValueType.Number)
                return new mcValue(op1func(A.numberValue));
            // functions can not be arguments
            if (A.valueType == mcValueType.Func) throw new ArgumentException("Functions can not be argument here !");
            // apply to each element of a vector
            var resA = new List<Number>(A.vectorLength);
            for (int i = 0; i < A.vectorLength; i++)
                resA.Add(op1func(A.vectorValue[i].numberValue));
            return new mcValue(resA);
        }
        public static mcValue vec1(mcValue[] Arg, Func<Number, Number> op1func, bool AllowNULL = true)
        {
            mcValue A= (Arg==null)||(Arg.Length==0)? null : Arg[0];
            return vec1(A, op1func, AllowNULL);
        }

        // vectorize binary operands, allowing vector to be either of them, using mcType lambda
        public static mcValue vec2(mcValue A, mcValue B, Func<mcValue, mcValue, mcValue> op2func, bool OnlyLeftVector=false, bool OnlySingleVector=false, bool OnlyBothVectors = false, bool AllowLeftNULL=true, bool AllowRightNULL = true)
        {
            if (A == null) {
                if (!AllowLeftNULL) throw new ArgumentException("Left argument is NULL !");
                A = new mcValue((double)0);
            }
            if (B == null)
            {
                if (!AllowRightNULL) throw new ArgumentException("Right argument is NULL !");
                B = new mcValue((double)0);
            }
            // if both are Number/scalars, it is always allowed
            if ((A.valueType == mcValueType.Number) && (B.valueType == mcValueType.Number))
                return op2func(A, B);
            // functions can not be arguments
            if ((A.valueType == mcValueType.Func) || (B.valueType == mcValueType.Func))
                throw new ArgumentException("functions can not be arguments !");
            // result is vector
            var res = new List<mcValue>(Math.Max(B.vectorLength,A.vectorLength));
            void addOne(mcValue Left, mcValue Right, int idx)
            {
                if (Left.valueType != mcValueType.Number) throw new ArgumentException("Element of left vector argument at [" + idx + "] is not scalar !");
                if (Right.valueType != mcValueType.Number) throw new ArgumentException("Element of right vector argument at [" + idx + "] is not scalar !");
                try
                {
                    res.Add(op2func(Left, Right));
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(ex.Message + " at index [" + idx + "]");
                }

            }
            // check if right argument is vector
            if (B.vectorLength > 1)
            {
                if (OnlyLeftVector) throw new ArgumentException("Right argument can not be a vector !");
                if (A.vectorLength > 1)
                {
                    if (OnlySingleVector) throw new ArgumentException("Only single argument can be a vector !");
                    // if both can be vectors, they must be same size
                    if (A.vectorLength != B.vectorLength) throw new ArgumentException("Arguments can not be vectors of different sizes, here [" + A.vectorLength + "] != [" + B.vectorLength + "] !");
                    // apply operations in pairs
                    for (int i = 0; i < B.vectorLength; i++)
                        addOne(A.vectorValue[i], B.vectorValue[i], i);
                }
                else
                {
                    if (OnlyBothVectors) throw new ArgumentException("Both arguments must be either scalars or vectors, here scalar [" + A.vectorLength + "] and vector [" + B.vectorLength + "] !");
                    // if A is scalar 
                    for (int i = 0; i < B.vectorLength; i++)
                        addOne(A, B.vectorValue[i], i);
                }
            }
            else
            {
                // otherwise left is vector and right is scalar
                if (OnlyBothVectors) throw new ArgumentException("Both arguments must be either scalars or vectors, here vector [" + A.vectorLength + "] and scalar [" + B.vectorLength + "] !");
                for (int i = 0; i < A.vectorLength; i++)
                    addOne(A.vectorValue[i], B, i);
            }
            return new mcValue(res);
        }
        // vectorize binary operands, allowing vector to be either of them, using Number lambda
        public static mcValue vec2(mcValue A, mcValue B, Func<Number, Number, Number> op2func, bool OnlyLeftVector = false, bool OnlySingleVector = false, bool OnlyBothVectors = false, bool AllowLeftNULL = true, bool AllowRightNULL = true)
        {
            Func<mcValue, mcValue, mcValue> mvFunc = (a, b) => new mcValue(op2func(a.numberValue, b.numberValue));
            return vec2(A, B, mvFunc, OnlyLeftVector: OnlyLeftVector, OnlySingleVector: OnlySingleVector, OnlyBothVectors: OnlyBothVectors, AllowLeftNULL: AllowLeftNULL, AllowRightNULL: AllowRightNULL);
        }
        // versions that accept argument list instead of individual mcValues ( usable from mc.cs mostly )
        public static mcValue vec2(mcValue[] Arg, Func<mcValue, mcValue, mcValue> op2func, bool OnlyLeftVector = false, bool OnlySingleVector = false, bool OnlyBothVectors = false, bool AllowLeftNULL = true, bool AllowRightNULL = true)
        {
            mcValue A = (Arg == null) || (Arg.Length < 1 ) ? null : Arg[0];
            mcValue B = (Arg == null) || (Arg.Length < 2 ) ? null : Arg[1];
            return vec2(A, B, op2func, OnlyLeftVector: OnlyLeftVector, OnlySingleVector: OnlySingleVector, OnlyBothVectors: OnlyBothVectors, AllowLeftNULL: AllowLeftNULL, AllowRightNULL: AllowRightNULL);
        }
        public static mcValue vec2(mcValue[] Arg, Func<Number, Number, Number> op2func, bool OnlyLeftVector = false, bool OnlySingleVector = false, bool OnlyBothVectors = false, bool AllowLeftNULL = true, bool AllowRightNULL = true)
        {
            mcValue A = (Arg == null) || (Arg.Length < 1) ? null : Arg[0];
            mcValue B = (Arg == null) || (Arg.Length < 2) ? null : Arg[1];
            return vec2(A, B, op2func, OnlyLeftVector: OnlyLeftVector, OnlySingleVector: OnlySingleVector, OnlyBothVectors: OnlyBothVectors, AllowLeftNULL: AllowLeftNULL, AllowRightNULL: AllowRightNULL);
        }



        public static mcValue Add(mcValue A, mcValue B) => vec2(A, B, (a, b) => a + b);
        public static mcValue Sub(mcValue A, mcValue B) => vec2(A, B, (a, b) => a - b, AllowLeftNULL:false);
        public static mcValue Mul(mcValue A, mcValue B) => vec2(A, B, (a, b) => a * b, AllowLeftNULL: false, AllowRightNULL: false);
        public static mcValue Div(mcValue A, mcValue B) => vec2(A, B, (a, b) => a / b, AllowLeftNULL: false, AllowRightNULL: false);

        // remainder of division
        public static mcValue Mod(mcValue A, mcValue B)
        {
            if ((A == null) || (B == null)) throw new ArgumentException("division null argument !");
            // if both are double
            if (!(A.isInt()&&B.isInt())) throw new ArgumentException("Mod  a%b requires both arguments to be integer !");
            return new mcValue(A.Int % B.Int);
        }





    }



}
