// This class is INDEPENDENT of other nmUnits, and can be used separately

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculatorNotepad
{

    // mcMath , used for counter options
    [FlagsAttribute]
    public enum ArrayCounterOptions
    {
        None = 0,
        CanRepeat = 1,
        Combinations = 2,
        Permutations = 4,
        Finished = 32
    };


    public class ArrayCounter
    {
        protected int nDigits;
        protected int[] minValue;
        protected int[] maxValue;
        protected int[] ox;
        protected bool isComb, isPerm;
        protected bool canRepeat;
        public bool finished { get; protected set; }

        public int Length { get; protected set; }


        /// <summary>
        /// default constructor from ox/min/max lists (can be different sizes, nDigits from ox)
        /// </summary>
        protected void ArrayCounterDef(List<int> oxList, List<int> minList, List<int> maxList )
        {
            nDigits = oxList.Count;
            Length = nDigits;
            ox = oxList.ToArray();
            minValue = minList.ToArray();
            maxValue = maxList.ToArray();
            isComb = false;
            isPerm = false;
            finished = false;
            canRepeat = true;
        }

        /// <summary>
        /// default constructor - will count n digits
        /// </summary>
        protected void ArrayCounterDef(int nDigits_p)
        {
            var oxList = (new int[nDigits_p]).ToList();
            var minList = (new int[nDigits_p]).ToList();
            var maxList = (new int[nDigits_p]).ToList();
            ArrayCounterDef(oxList, minList, maxList);
        }


        // null constructor, used by inheriting classes
        protected ArrayCounter()
        {

        }

        /// <summary>
        /// constructor - will count n digits, each with specified max and min value (inclusive)
        ///             - number of digits is derived from size of max[] array
        ///             - if minValues[] are not specified, zero is assumed
        /// </summary>
        public ArrayCounter(int[] maxValues, int[] minValues, bool _canRepeat=true)
        {
            ArrayCounterDef(maxValues.Length);
            canRepeat = _canRepeat;
            for (int i = 0; i < nDigits; i++)
            {
                maxValue[i] = maxValues[i];
                if (minValues != null)
                    minValue[i] = minValues[i];
                if (minValue[i] > maxValue[i]) minValue[i] = maxValue[i];
            }
            resetCounter();
        }

        /// <summary>
        /// short version of constructor, if min/max values are same for all numbers 
        /// </summary>
        public ArrayCounter(int nDigits_p, int minValue_p, int maxValue_p, bool _canRepeat = true)
        {
            ArrayCounterDef(nDigits_p);
            canRepeat = _canRepeat;
            //fill min/max array with same values
            for (int i = 0; i < nDigits; i++)
            {
                minValue[i] = minValue_p;
                maxValue[i] = maxValue_p;
            }
            resetCounter();
        }

        /// <summary>
        /// Combination version of constructor, min is 0, max is combN ( order does not matter, so always ascending ). 
        /// Will count only ascending combos, so 012, 013, 234.. not 432  
        /// If canRepeat is true, then 000, 001 etc is allowed too, ie it can repeat numbers, but will still be ascending
        /// </summary>
        public ArrayCounter(int nDigits_p, int combN, bool canRepeatNumber, int startFrom = 0)
        {
            ArrayCounterDef(nDigits_p);
            isComb = true;
            canRepeat = canRepeatNumber;
            //fill min with 1 (although it will be dynamic) , max with combN
            for (int i = 0; i < nDigits; i++)
            {
                if (canRepeat)
                {
                    minValue[i] = startFrom;
                    maxValue[i] = combN;
                }
                else
                {
                    minValue[i] = i + startFrom;
                    maxValue[i] = combN - nDigits + i + 1;
                }
            }
            resetCounter();
        }

        /// <summary>
        /// Permutation version of constructor, will permutate given values. 
        /// If values=[1,2,3] it will iterate 123,132,213,231,312,321 for 3!=6 permutations
        /// </summary>
        public ArrayCounter(int[] values)
        {
            ArrayCounterDef(values.Length);
            isPerm = true;
            // put original values in 'minValues' to remember original order for reset
            for (int i = 0; i < values.Length; i++)
                minValue[i] = values[i];
            resetCounter(); // will copy originals from minValues to 'ox' and create/clear maxValues[len+1]
        }

        /// <summary>
        /// Construct from current values
        /// Used mostly when counter data kept in separate structures, like for mcValue intergation
        /// </summary>
        public ArrayCounter(int[] currentValues, int[] maxValues, int[] minValues, ArrayCounterOptions options)
        {
            ArrayCounterDef(currentValues.Length);
            canRepeat = (options & ArrayCounterOptions.CanRepeat) != 0;
            isComb = (options & ArrayCounterOptions.Combinations) != 0;
            isPerm = (options & ArrayCounterOptions.Permutations) != 0;
            finished = (options & ArrayCounterOptions.Finished) != 0;
            for (int i = 0; i < nDigits; i++)
            {
                ox[i] = currentValues[i];
                minValue[i] = minValues[i];
                maxValue[i] = maxValues[i];
            }
        }

        /// <summary>
        /// Construct from another counter
        /// Used in similar situations as constructor from current values
        /// </summary>
        public ArrayCounter(ArrayCounter other)
        {
            ArrayCounterDef(other.nDigits);
            canRepeat = other.canRepeat;
            isComb = other.isComb;
            isPerm = other.isPerm;
            finished = other.finished;
            for (int i = 0; i < nDigits; i++)
            {
                ox[i] = other.ox[i];
                minValue[i] = other.minValue[i];
                maxValue[i] = other.maxValue[i];
            }
        }


        /// <summary>
        /// return current value (all digits) as array
        /// </summary>
        public int[] Num
        {
            get { return ox; }
        }

        /// <summary>
        /// return specified digit from current array value
        /// </summary>
        public int this[int index]
        {
            get { return ox[index]; }
            set { ox[index] = value; }
        }

        /// <summary>
        /// clear current array value to zero or minValues
        /// </summary>
        public void resetCounter()
        {
            for (int i = 0; i < nDigits; i++) ox[i] = minValue[i];
            if (isPerm)
            {
                maxValue = new int[nDigits + 1]; // 'maxValue[1..nDigits]' will be used as stack counter, base index 1 so need to have one more element
                maxValue[0] = 1; //  maxValue[0] is stack pointer for non-recursive Heap algorithm
            }
            finished = false;
            _validityCheck(); // to move from 000 if canRepeat==false
        }

        /// <summary>
        /// return string representation of some array, using first nElem digits
        /// </summary>
        public static string Array2String(int nElem, int[] a)
        {
            string s = "[ ";
            for (int i = 0; (i < nElem) && (i < a.Length); i++)
            {
                s += a[i].ToString();
                if (i < nElem - 1) s += ", ";
            }
            s += " ]";
            return s;
        }

        /// <summary>
        /// return string representation of current array value
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Array2String(nDigits, ox);
        }

        /// <summary>
        /// Non-recursive permutation, based on Heap's algorithm, doing just single permutation
        /// Based on current permutation in 'ox', stack in maxValue[1..nDigits] and stack pointer in maxValue[0]
        /// </summary>
        private void NextPermutation()
        {
            int sp = maxValue[0];
            while (sp < nDigits)
            {
                if (maxValue[sp] < sp)
                {
                    // swap 'sp' position with either 0th(if sp even) or maxValue[sp]-th (if sp odd), but from right side
                    int sp1 = nDigits - 1 - sp;
                    int sp2 = nDigits - 1 - ((sp & 1) == 1? maxValue[sp] : 0); 
                    int tmp = ox[sp1];
                    ox[sp1] = ox[sp2];
                    ox[sp2] = tmp;
                    // update counters and return current permutation
                    maxValue[sp]++;
                    maxValue[0] = 1;
                    return;
                }
                else
                {
                    maxValue[sp] = 0;
                    sp++;
                }
            }
            finished = true;
        }

        // increase current value by one, returns true if valid number (false when finished counting). Does NOT check validity (repeated numbers)
        protected void _next()
        {
            if (finished) return;
            if (isPerm)
            {
                NextPermutation();
                return;
            }
            int op;
            for (op = nDigits - 1; (op >= 0) && (ox[op] >= maxValue[op]); op--) ;
            if (op >= 0)
            {
                ox[op]++;
                for (op++; op < nDigits; op++)
                {
                    if (!isComb)
                        ox[op] = minValue[op];
                    else
                        // WARNING:  This will reliably work only for 'allSame' digits, when min/max values are same for all digits. But combination counters always have 'allSame'
                        if (canRepeat) ox[op] = ox[op - 1]; else ox[op] = ox[op - 1] + 1;
                }
            }
            finished = op < 0;
        }

        // check validity of current number, and repeat _next until valid number achieved
        protected void _validityCheck()
        {
            if (isPerm) return; // permutations are always valid, they only permute valid set
            bool valid=false;
            while ((!finished) && !valid)
            {
                // check if number is valid regarding repeating 
                valid = true;
                if ((!canRepeat) && !isComb)
                {
                    for (int a = 0; a < nDigits - 1; a++)
                        for (int b = a + 1; b < nDigits; b++)
                            if (ox[a] == ox[b]) valid = false;
                }
                // if not valid, inc to next number
                if (!valid)
                    _next();
            } 
        }


        /// <summary>
        /// increase current value by one, returns true if valid number (false when finished counting)
        /// can use do{...}while(ar.Next());
        /// </summary>
        public bool Next()
        {
            _next();
            _validityCheck();
            return !finished;
        }

        /// <summary>
        /// math f-on, C(n,k)- numer of ways that N different numbers can be set in k places
        /// C(n,k)= n!/k!/(n-k)!
        /// </summary>
        public static int combin(int n, int k)
        {
            // boundary
            if ((n <= 0) || (k <= 0)) return 1;
            // get k to be whichever is higher between k and (n-k)
            int nk = n - k;
            if (nk > k)
            {
                int swp = nk;
                nk = k;
                k = swp;
            }
            // find n!/k!
            int nf = 1; for (int i = k + 1; i <= n; i++) nf *= i;
            // find (n-k)!
            int kf = 1; for (int i = 1; i <= nk; i++) kf *= i;
            // divide for result
            return nf / kf;
        }

        /// <summary>
        /// Returns in how many different ways given List<double> could be arranged
        /// If all N elements of vector are different, it will be N!
        /// But if some elements are repeating within vector, it will be less than N!
        /// </summary>
        public static int NumberOfReplicas(List<double> a)
        {
            int res = 1;
            int p = 0;
            int remPlaces = a.Count, nDigits = a.Count;
            a.Sort(); // need to sort, to ensure finding all same elements next to each other
            do
            {
                int cnt = 1;
                p++;
                // count number of repeats
                while ((p < nDigits) && (a[p - 1] == a[p])) { p++; cnt++; }
                // add/mult to result
                res *= combin(remPlaces, cnt);
                remPlaces -= cnt;
            } while (p < nDigits);
            return res;
        }

        /// <summary>
        /// Returns in how many different ways given integer array could be arranged
        /// </summary>
        public static int NumberOfReplicas(int[] a)
        {
            var da = new List<double>(a.Length);
            foreach (var ia in a) da.Add(ia);
            return NumberOfReplicas(da);
        }


        /// <summary>
        /// calculate in how many different ways CURRENT array value could be arranged if not ascending order
        /// </summary>
        /// <returns></returns>
        public int NumberOfReplicas()
        {
            return NumberOfReplicas(ox);
        }


        /// <summary>
        /// count total number of combination that this counter can have ( depends on nDigits,isComb, canRepeat, min/max values )
        /// </summary>
        /// <returns></returns>
        public int TotalCount()
        {
            int res = 1;
            // if permutations, return nDigits!
            if (isPerm)
            {
                for (int i = 1; i <= nDigits; i++)
                    res *= i;
                return res;
            }
            // otherwise check if all digits have same possible values, to allow fast calc
            bool allSame = true;
            for (int i = 1; i < nDigits; i++) if ((minValue[i] != minValue[i - 1]) || (maxValue[i] != maxValue[i - 1])) allSame = false;
            if (allSame)
            {
                int span = maxValue[0] - minValue[0] + 1;
                if (isComb && !canRepeat) return combin(span, nDigits); // standard combinations: 012,021,102 ... without repeats of same digit
                if (isComb && canRepeat) return combin(nDigits+span-1, span-1); // non-standard permutation: 000,001,002,011,012,022,111... can repeat same digit but still position does not matter
                if (canRepeat && !isComb) return (int)Math.Pow(span, nDigits); // standard counter: 000...999
                if ((!canRepeat) && !isComb) // non-standard counter: 012,013,... can not repeat same number
                {
                    for (int i = 0; i < nDigits; i++) res *= (span - i);
                    return res;
                }

            }
            // if order does matter, and if repeats are allowed (standard counter, but with different digit ranges), can still do fast calc:
            if (canRepeat && !isComb)
            {
                for (int i = 0; i < nDigits; i++) res *= maxValue[i] - minValue[i] + 1;
                return res;
            }
            // otherwise (if not all digits are same) it is too complex to calculate, so just count
            var nc = new ArrayCounter(this); // create copy of this counter
            nc.resetCounter(); // reset to start
            while (nc.Next()) res++;
            return res;

        }



    }
}
