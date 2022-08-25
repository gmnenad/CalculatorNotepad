using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Numbers;


namespace CalculatorNotepad
{

    public class mcMathCounter : ArrayCounter
    {

        // *** integration with mcValue

        /// <summary>
        /// Construct from mcValue vector holding counter state
        /// </summary>
        public mcMathCounter(mcValue counterVec)
        {
            if (counterVec.vectorLength != 4) throw new ArgumentException("Invalid counter vector!");
            var v = counterVec.Vector;
            var current = v[0].getListInt();
            var max= v[1].getListInt();
            var min = v[2].getListInt();
            var opt = (ArrayCounterOptions) v[3].Int;
            ArrayCounterDef(current,min,max);
            isComb = (opt & ArrayCounterOptions.Combinations) != 0;
            isPerm = (opt & ArrayCounterOptions.Permutations) != 0;
            canRepeat = (opt & ArrayCounterOptions.CanRepeat) != 0;
            finished = (opt & ArrayCounterOptions.Finished) != 0;
        }


        // standard arrayCounter constructors

        /// <summary>
        /// constructor - will count n digits, each with specified max and min value (inclusive)
        ///             - number of digits is derived from size of max[] array
        ///             - if minValues[] are not specified, zero is assumed
        /// </summary>
        public mcMathCounter(int[] maxValues, int[] minValues, bool canRepeat=true): base (maxValues, minValues, canRepeat) { }


        /// Combination version of constructor, min is 0, max is combN. 
        /// Will count only ascending combos, so 012, 013, 234.. not 432  
        /// If canRepeat is true, then 000, 001 etc is allowed too, ie it can repeat numbers, but will still be ascending
        /// </summary>
        public mcMathCounter(int nDigits_p, int combN, bool canRepeatNumber, int startFrom = 0) : base( nDigits_p, combN, canRepeatNumber, startFrom ) { }

        /// <summary>
        /// Permutation version of constructor, will permutate given values. 
        /// If values=[1,2,3] it will iterate 123,132,213,231,312,321 for 3!=6 permutations
        /// </summary>
        public mcMathCounter(int[] values) : base(values) { }



        /// <summary>
        /// Construct from current values
        /// Used mostly when counter data kept in separate structures, like for cmcValue intergation
        /// </summary>
        public mcMathCounter(int[] currentValues, int[] maxValues, int[] minValues, ArrayCounterOptions options) : base(currentValues, maxValues, minValues, options) { }



        /// <summary>
        /// create mcValue from specified counter
        /// </summary>
        public static mcValue getValueVector(mcMathCounter ac)
        {
            var opt = ArrayCounterOptions.None;
            if (ac.canRepeat) opt |= ArrayCounterOptions.CanRepeat;
            if (ac.isComb) opt |= ArrayCounterOptions.Combinations;
            if (ac.isPerm) opt |= ArrayCounterOptions.Permutations;
            if (ac.finished) opt |= ArrayCounterOptions.Finished;
            var options = new mcValue((int)opt);
            var current = new mcValue(ac.ox.ToList());
            var max = new mcValue(ac.maxValue.ToList());
            var min = new mcValue(ac.minValue.ToList());
            // create complex vector holding all counter data
            return new mcValue(new List<mcValue>(4) { current, max, min, options });
        }



        /// <summary>
        /// create mcValue from current counter
        /// </summary>
        public mcValue getValueVector()
        {
            return getValueVector(this);
        }

    }



}
