using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculatorNotepad
{



    // mcMath , used in argument tests
    [FlagsAttribute]
    enum ArgTst
    {
        None = 0,
        Int = 1,
        Number = 2,
        Vector = 4,
        Func = 8
    };



    // mcValue 
    [FlagsAttribute]
    public enum mcValueFlags
    {
        None = 0,
        Return = 1,     // this is value from 'return' function, and need to abort loops and be propagated further
        Incomplete = 2    // return value was not set, so return last evaluated from block/loop
    };


    // mcFunc type
    public enum mcFuncType
    {
        Constant,
        Parameter,
        DirectFunc,
        SubFunc
    };

    // generic trinary Yes/No/Unknown, used for isRecursive, isConstant...
    public enum mcFuncTrinary
    {
        Unknown,
        Not,
        Yes
    };

    // mcFunc flags
    [FlagsAttribute]
    public enum mcFuncFlags
    {
        None = 0,
        BuiltIn = 1,      // set when this is builtin function, as opposed to user defined functions. They do not need DeepCopy
        User = 2,         // set for user defined topmost functions. User f-ions in preset file may have both BuiltIn and User flag . Only User functions can be cached
        TopNamed = 4,     // set for named topmost functions (listed in functions[])- both user and builtin, but NOT anon/lambdas
        HasSideEffects=8, // set for functions that use or set variables outside their scope, or use other functions with side effects. Used to prevent caching
        CSfunc=16,        // set for compiled c# functions from right panel
    };


    // disabled optimizations, in parsing or calculating
    [FlagsAttribute]
    public enum mcOptimization
    {
        None = 0,
        CalcBelowChange = 1,        // only recalculate below changed line (optimization in mc.CalcLines, involves parsing and calculating)
        ConstantFunctions = 2,      // detect constant functions and convert them to constant values (optimization in mcExp, done while parsing )
        CacheFunctions = 4          // cache results of top level recursive functions (detected while parsing, done in runtime calculations)
    };


    // mcParse extracts
    [FlagsAttribute]
    enum ExtractOptions
    {
        None = 0,
        RemoveFound = 1,
        SkipSpaces = 12,
        RemoveAndSkip = 13,
        returnBrackets = 2,
        skipSpacesStart = 4,
        skipSpacesEnd = 8,
        doNotMovePos = 16
    };

    //mcParse splits
    [FlagsAttribute]
    enum SplitOverOptions
    {
        None = 0,
        RemoveEmptyEntries = 1,
        TrimLeft = 4,
        doNotMovePos = 16
    };



}
