using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Numbers;


namespace CalculatorNotepad
{


    public class mc
    {
        // config
        public static mcConfig cfg { get; set; }
        public static bool notProperlyClosed = false;
        // function state
        public static Dictionary<string, mcFuncParse> functions = null;
        private static Dictionary<string, List<string>> functionsCased;
        public static Dictionary<string, List<List<int>>> varNames = null; // ( shortName, List<blocks> ) : all blocks where this name appears
        public static Dictionary<string, Stack< Tuple<int,mcValue> >> varValues = null; // (fullName, Stack) : stack of values for specific variable in specific block 
        public static Dictionary<string, mcFunc> varParents = null; // (fullName, parentFunc) : parent functions for given variable 
        public static Dictionary<string, Number> units; // unit multiplicator constants. SI units are *1.0 , others are accordingly
        private static Dictionary<string, List<string>> unitsCased;
        public static int defaultNumberBase = 10;
        // display state
        public static CalcResult lastResult;
        public static mcTotalResult lastTotalResult;
        public static bool showEntireAutocomplete = false;
        // parse and eval states
        private static Stopwatch swFunc, swDoc;
        public static Dictionary<mcFuncCacheKey, mcValue> cacheFunc = null; // cache for mcFunc (args) results, useful for recursions
        
        private static List<string> volatileNames; // for new func definitions in case of exception to delete
        public static Dictionary<mcFunc, mcFunc> deepCopyLinks; // for mcFunc mass deep copy

        // store current state as 'initial' one, for reset
        private static mcState stored_after_Preset = null;
        public static string CSpresetSource = "";
        public static mcState stored_Mid = null;

        // global variables or consts
        public const string unitSymbol = "'";


        // initialize first time
        public static void Init()
        {
            swFunc = new Stopwatch();
            swDoc = new Stopwatch();
            // load config
            try
            {
                cfg = mcConfig.Load();
            }
            catch
            {
                cfg = new mcConfig();
                // default values for new config
                cfg.openLastOnStart = true;
                cfg.autoLastResultAppend = true;
                cfg.sensitivity = mcCaseSensitivity.Dynamic;
                cfg.timeoutFuncMs = 1000;
                cfg.timeoutDocMs  = 5000;
                cfg.autocompleteChars = 1;
                cfg.DebugVars = "";
                cfg.PresetFile = "";
                cfg.resFormatDecimals = -1;
                cfg.numberType = NumberClass.Double;
                cfg.numberPrecision = 53;
            }
            Number.defaultClassType = cfg.numberType;
            Number.defaultPrecision = cfg.numberPrecision;
            // save config with flag marking it as 'not properly closed'
            notProperlyClosed = cfg.notProperlyClosed;
            cfg.notProperlyClosed = true;
            cfg.Save();
            // setup builtin functions
            units = new Dictionary<string, Number>();
            unitsCased = new Dictionary<string, List<string>>();
            // length
            addUnit("m", 1); addUnit("km", 1000); addUnit("dm", 0.1); addUnit("cm", 0.01); addUnit("mm", 1e-3); addUnit("um", 1e-6); addUnit("nm", 1e-9); addUnit("pm", 1e-12);
            addUnit("mi", 1609.344); addUnit("mile", 1609.344); addUnit("yd", 0.9144); addUnit("yard", 0.9144); addUnit("ft", 0.3048); addUnit("foot", 0.3048); addUnit("in", 25.4e-3); addUnit("inch", 25.4e-3);
            addUnit("ly", 9460730472580800); addUnit("pc", 30856775814671900); addUnit("kpc", 30856775814671900e+3); addUnit("mpc", 30856775814671900e+6);
            // time
            addUnit("s", 1); addUnit("min", 60); addUnit("h", 60 * 60); addUnit("d", 24 * 60 * 60); addUnit("year", 24 * 60 * 60 * 365); addUnit("yr", 24 * 60 * 60 * 365);
            addUnit("ms", 1e-3); addUnit("us", 1e-6); addUnit("ns", 1e-9); addUnit("ps", 1e-12);
            // weight
            addUnit("kg", 1); addUnit("g", 1e-3); addUnit("gram", 1e-3); addUnit("tonne", 1000); addUnit("mg", 1e-6);
            addUnit("ounce", 28.349523125e-3); addUnit("oz", 28.349523125e-3); addUnit("pound", 0.45359237); addUnit("lb", 0.45359237);
            // derived units
            addUnit("mph", 1609.344 / 3600); addUnit("kmh", 1000.0 / 3600); addUnit("knot", 1852.0 / 3600); addUnit("c", 299792458);
            // other units
            addUnit("rad", 1); addUnit("deg", Number.PI / 180);
            //  priority: 100=constants, 90=func, 70= !% 60=unary +- 50=^  40=*/ 30=+- 20= ><= 10=|&
            functions = new Dictionary<string, mcFuncParse>();
            functionsCased = new Dictionary<string, List<string>>();
            varNames = new Dictionary<string, List<List<int>>>();
            varValues = new Dictionary<string, Stack<Tuple<int, mcValue>>>();
            varParents = new Dictionary<string, mcFunc>();
            // internal functions
            addFunc("[", new mcFuncParse("", mcFuncParamType.Between, 2, 100, new mcFunc(args => index(args[0], args[1])), "N/A"));  //  element(s) of args[0] at given index args[1] ( ... args[2]) based on indices in vector
            addFunc("new", new mcFuncParse("", mcFuncParamType.Between, 3, 100, new mcFunc(args => nmc.errArg("Invalid 'new' syntax !")), "Denote forced declaration of a new variable. Optional, since new variables are declared even without 'new' keyword.\r\nWithout 'new' keyword, new variable is created in current scope only if same-name variable does not exists in enclosing scopes,\r\nbut if upper variable exists, that one is used. This allows declaring variables without need of keywords like 'var':\r\n    { x=1; { x=5 } y=x}  // y=5 : since x=5 refers to same variable as x=1 \r\nWhen 'new' keyword is used, new variable is created in that scope even if same-named variable existed above:\r\n    { x=1; { new x=5 } y=x} // y=1 : since x=5 refers to new variable, different from x=1 ", "new varName= exp"));
            addFunc(unitSymbol, new mcFuncParse(unitSymbol, mcFuncParamType.Between, 2, 55, new mcFunc(args => args[0].Number * args[1].Number), "N/A"));  // unitSymbol== ' , multiply number with unit conversion, high priority so e^-10[km/s] works
            addFunc("graph", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => 0), "Display execution graph for specified expression\r\nUsed mainly for debugging","graph ( expression )"));  
            // math constants or parameterless functions
            addFunc("last", new mcFuncParse("", mcFuncParamType.Variable, 0, 100, new mcFunc(args => mc.lastResult.Value),"Represents last calculated value, result of last evaluated expression before this line\r\nCan use symbol ~ as alternative, or even ~Number ( like ~12.45 ), where number will be ignored"));
            addFunc("~", new mcFuncParse("", mcFuncParamType.Variable, 0, 100, new mcFunc(args => mc.lastResult.Value)));
            addFunc("e", new mcFuncParse("", mcFuncParamType.Variable, 0, 100, new mcFunc(Number.E),"Constant e = 2.7182..."));
            addFunc("pi", new mcFuncParse("", mcFuncParamType.Variable, 0, 100, new mcFunc(Number.PI), "Constant pi = 3.1415...\r\nCan alternatively use symbol π instead"));
            addFunc("π", new mcFuncParse("", mcFuncParamType.Variable, 0, 100, new mcFunc(Number.PI), "Constant pi = 3.1415..."));
            addFunc("true", new mcFuncParse("", mcFuncParamType.Variable, 0, 100, new mcFunc(1), "Boolean constant 'true' \r\nEquivalent to integer 1 ( any !=0 number is considered to be 'true' )"));
            addFunc("false", new mcFuncParse("", mcFuncParamType.Variable, 0, 100, new mcFunc(0), "Boolean constant 'false' \r\nEquivalent to integer 0 "));
            // bitwise operators (  xor i snot using '^' since it is reserved for power !)
            addFunc("|", new mcFuncParse("|", mcFuncParamType.Between, 2, 10, new mcFunc(args => nmc.bitor(args)), "Returns bitwise OR from the scecified integer list\r\n   if vectors specified, find OR of vector values\r\n   for logical OR (faster) use 'a || b'", "bitor ( a[,b[,c,...]] )"));
            addFunc("&", new mcFuncParse("&", mcFuncParamType.Between, 2, 10, new mcFunc(args => nmc.bitand(args)), "Returns bitwise AND from the scecified integer list\r\n   if vectors specified, find AND of vector values\r\n   for logical AND (faster) use 'a && b'", "bitand ( a[,b[,c,...]] )"));
            addFunc("bitor", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.bitor(args)), "Returns bitwise OR from the scecified integer list\r\n   if vectors specified, find OR of vector values", "bitor ( a[,b[,c,...]] )"));
            addFunc("bitand", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.bitand(args)), "Returns bitwise AND from the scecified integer list\r\n   if vectors specified, find AND of vector values", "bitand ( a[,b[,c,...]] )"));
            addFunc("bitxor", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.bitxor(args)), "Returns bitwise XOR from the scecified integer list\r\n   if vectors specified, find XOR of vector values", "bitxor ( a[,b[,c,...]] )"));
            // logic operators
            addFunc("||", new mcFuncParse("||", mcFuncParamType.Between, 2, 10, new mcFunc(args => new mcValue(args[0].isTrue() || args[1].isTrue() ? 1 : 0)), "Logical OR, return 1 (true) if at least one of a or b are true\r\n  'true' means !=0 (default 1), while 'false' means == 0\r\n  for bitwise OR use 'a | b', or 'bitor(a,b)'", "a || b"));
            addFunc("&&", new mcFuncParse("&&", mcFuncParamType.Between, 2, 10, new mcFunc(args => args[0].isTrue() && args[1].isTrue() ? 1 : 0), "Logical AND, return 1 (true) if both a and b are true\r\n  'true' means !=0 (default 1), while 'false' means == 0\r\n  for bitwise AND use 'a & b', or 'bitand(a,b)'", "a && b"));
            addFunc("xor", new mcFuncParse("xor", mcFuncParamType.Between, 2, 10, new mcFunc(args => args[0].isTrue() && args[1].isTrue() ? 1 : 0), "Logical XOR, return 1 (true) if either a or b (but not both) are true\r\n  'true' means !=0 (default 1), while 'false' means == 0\r\n  for bitwise XOR use 'bitxor(a,b)'", "a xor b"));
            addFunc(">", new mcFuncParse(">", mcFuncParamType.Between, 2, 20, new mcFunc(args => mcValue.CMP(args[0], args[1]) > 0 ? 1 : 0)));
            addFunc(">=", new mcFuncParse(">=", mcFuncParamType.Between, 2, 20, new mcFunc(args => mcValue.CMP(args[0], args[1]) >= 0 ? 1 : 0)));
            addFunc("≥", new mcFuncParse("≥", mcFuncParamType.Between, 2, 20, new mcFunc(args => mcValue.CMP(args[0], args[1]) >= 0 ? 1 : 0), "Symbol for 'greater or equal', alternative to >=", "a ≥ b"));
            addFunc("<", new mcFuncParse("<", mcFuncParamType.Between, 2, 20, new mcFunc(args => mcValue.CMP(args[0], args[1]) < 0 ? 1 : 0)));
            addFunc("<=", new mcFuncParse("<=", mcFuncParamType.Between, 2, 20, new mcFunc(args => mcValue.CMP(args[0], args[1]) <= 0 ? 1 : 0)));
            addFunc("≤", new mcFuncParse("≤", mcFuncParamType.Between, 2, 20, new mcFunc(args => mcValue.CMP(args[0], args[1]) <= 0 ? 1 : 0), "Symbol for 'less or equal', alternative to <=", "a ≤ b"));
            addFunc("==", new mcFuncParse("==", mcFuncParamType.Between, 2, 19, new mcFunc(args => mcValue.CMP(args[0], args[1]) == 0 ? 1 : 0)));
            addFunc("≡", new mcFuncParse("≡", mcFuncParamType.Between, 2, 19, new mcFunc(args => mcValue.CMP(args[0], args[1]) == 0 ? 1 : 0), "Symbol for 'equal', alternative to ==", "a ≡ b"));
            addFunc("!=", new mcFuncParse("!=", mcFuncParamType.Between, 2, 19, new mcFunc(args => mcValue.CMP(args[0], args[1]) != 0 ? 1 : 0)));
            addFunc("≠", new mcFuncParse("≠", mcFuncParamType.Between, 2, 19, new mcFunc(args => mcValue.CMP(args[0], args[1]) != 0 ? 1 : 0),"Symbol for 'different', alternative to !=", "a ≠ b"));
            // math operators
            addOpVec2("+", 30, (a, b) => a + b);
            addOpVec2("-", 30, (a, b) => a - b);
            addOpVec2("*", 40, (a, b) => a * b);
            addOpVec2("/", 40, (a, b) => a / b);
            addOpVec2("<<", 25, (a, b) => a << b.AsInt);
            addOpVec2(">>", 25, (a, b) => a >> b.AsInt);
            addFunc("^", new mcFuncParse("^", mcFuncParamType.Between, 2, 50, new mcFunc(args => Number.Pow(args[0].Number, args[1].Number))));
            addFunc("unaryminus", new mcFuncParse("", mcFuncParamType.Prefix, 1, 60, new mcFunc(args => -args[0].Number),"N/A"));
            addFunc("unaryplus", new mcFuncParse("", mcFuncParamType.Prefix, 1, 60, new mcFunc(args => args[0].Number), "N/A"));
            addFunc("!", new mcFuncParse("!", mcFuncParamType.Sufix, 1, 70, new mcFunc(args => Number.Factorial(args[0].Number))));
            addFunc("!!", new mcFuncParse("!!", mcFuncParamType.Sufix, 1, 70, new mcFunc(args => Number.DoubleFactorial(args[0].Number))));
            addFunc("‼", new mcFuncParse("‼", mcFuncParamType.Sufix, 1, 70, new mcFunc(args => Number.DoubleFactorial(args[0].Number)), "Symbol for 'double factorial', alternative to !!", "5‼"));
            addFunc("√", new mcFuncParse("√", mcFuncParamType.Prefix, 1, 55, new mcFunc(args => Number.Sqrt(args[0].Number)), "Square root, equivalent to sqrt(x)\r\n   does not need brackets for single value: √x\r\n   but allows brackets: √(x+1)", "√5"));
            addFunc("%", new mcFuncParse("%", mcFuncParamType.Sufix, 1, 70, new mcFunc(args => args[0].Number / 100))); //  as percentage
            addFunc("not", new mcFuncParse("", mcFuncParamType.Prefix, 1, 60, new mcFunc(args => (args[0].isTrue())?0:1  ), "Boolean unary operator return true if right side is false and vice versa\r\n  if (not (a==b)) ..."," not <expression>"));
            addOpVec2(":", 5, (a, b) => Number.Round(a, b.AsInt), "x:d Rounds value x to a number of digits d specified after : sign, lowest priority.\r\nSame as round(x,d) function.", "1/13:2");
            // very special functions - different evaluation or flow logic
            addFunc("if", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => args[0].isTrue() ? args[1] : args[2]), "Check condition, then execute/evaluate expression. Has three versions:\r\nif(cond) expIFtrue [else expIFfalse]; \r\n   return expIFtrue if cond==true, otherwise returns expIFfalse\r\n   in case there is no 'else' section, returns zero\r\n   only one of expIFtrue|expIFfalse is evaluated, and they can be {} blocks.\r\nif( cond, expIFtrue [, expIFfalse] )\r\n   same logic, Excel-like syntax suitable for single line", "if (cond) expTrue [else expFalse];  <or>  if( cond ,resIfTrue [,resIfFalse] )"));
            addFunc("else", new mcFuncParse("", mcFuncParamType.Func, 0, 45, new mcFunc(args => nmc.errArg("Invalid ELSE execution !")), "Denote second part of IF clause, to be executed if condition is not true.\r\nCan not be used outside IF statement.", "if (cond) expTrue else expFalse"));
            addFunc("while", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => new mcValue()), "Repeat 'body' while 'condition' is true\r\n   while(x<10) x=x+1;\r\n   while(i<5) { s=s+i^2; i+i+1 }\r\nWhile loop returns last evaluated body value. Alternative version in form of function:\r\n   while(x=1, x<3, d=d/2;x=x+1)\r\n   while( ,rnd<0.5,z=2*z, 10*z) -returns 10*z after doubling z few times", "while(condition) body;  <or>  while (initialDef, condition [,body [,returnValue]] )"));
            addFunc("for", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => new mcValue()), "After 'initializer', while 'condition' is true, repeat 'body' and 'iterator' \r\nInitializer, condition and iterator (but not body) can have multiple statements separated by semicolon ';'\r\nAll of them, including body, can have multiple statements inside { block }\r\nBy default, FOR returns last evaluated body value (or last iterator value if no body)\r\n   for(i=1, i<5, i=i+1) { s=s+i^2  }\r\n   for(i=1, i<5, s=s+i^2 ; i=i+1)", "for ( [initializer] , condition , [iterator] )  [body] "));
            addFunc("dowhile", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => new mcValue()), "Repeat 'body' while 'condition' is true\r\n   dowhile(x=1, d=d/2;x=x+1, x<3)\r\n   dowhile( ,z=2*z,rnd<0.5, 10*z) -returns 10*z after doubling z few times\r\nWhile loop returns last evaluated body value. Alternative version in form of statement:\r\n    do {body} while(condition);", "dowhile ([initialDef], [body] , condition [,returnValue]] )"));
            addFunc("do", new mcFuncParse("", mcFuncParamType.Prefix, 1, 90, new mcFunc(args => new mcValue()), "Repeat 'body' while 'condition' is true\r\n   do {x=x+1} while(x<10);\r\n   do { s=s+i^2; i+i+1 } while(i<5); \r\nWhile loop returns last evaluated body value. Alternative version in form of function:\r\n   dowhile ([initialDef], [body] , condition [,returnValue]] )", "do {body} while(condition);"));
            addFunc("return", new mcFuncParse("", mcFuncParamType.Prefix, 1, 60, new mcFunc(args => nmc.errArg("Invalid RETURN execution !")), "Return specified value and exit function. If no value is specified, returns last evaluated value from block.\n\rIt escape all inner blocks and present specified value as result of first enclosing function.\r\n    return  x+a^2;","return  resultExpression"));
            addFunc("blockintr", new mcFuncParse("", mcFuncParamType.Func, 0, 90, new mcFunc(args => new mcValue()), "N/A"));  //Evaluate multiple expressions and returns value of last one
            // functions using lambdas
            addFunc("sum", new mcFuncParse("", mcFuncParamType.Func, 3, 90, new mcFunc(args => nmc.sum(args)), "Sum all values of single-argument function where i_start <= argument <= i_end\r\nCan also use symbol ∑ instead of 'sum': ∑(func,start,end...)", "sum ( (i)=> ..., i_start, i_end [, i_step])")); 
            addFunc("∑", new mcFuncParse("", mcFuncParamType.Func, 3, 90, new mcFunc(args => nmc.sum(args)),"Sum all values of single-argument function where i_start <= argument <= i_end\r\nCan also use 'sum' instead of symbol ∑", "∑( (i)=> ..., i_start, i_end [, i_step])"));
            addFunc("product", new mcFuncParse("", mcFuncParamType.Func, 3, 90, new mcFunc(args => nmc.product(args)), "Multiply all values of single-argument function where i_start <= argument <= i_end\r\nCan also use symbol ∏ instead of 'product': ∏(func, start, end...)", "product ( (i)=> ..., i_start, i_end [, i_step])"));
            addFunc("∏", new mcFuncParse("", mcFuncParamType.Func, 3, 90, new mcFunc(args => nmc.product(args)), "Multiply all values of single-argument function where i_start <= argument <= i_end\r\nCan also use 'product' instead of symbol ∏", "∏( (i)=> ..., i_start, i_end [, i_step])"));
            addFunc("integral", new mcFuncParse("", mcFuncParamType.Func, 3, 90, new mcFunc(args => nmc.integral(args)), "Integrate function value in given range\r\nCan also use symbol ∫: ∫(func, start, end...)", "integral ( (x)=>... , x_start, x_end, [num_steps] )"));
            addFunc("∫", new mcFuncParse("", mcFuncParamType.Func, 3, 90, new mcFunc(args => nmc.integral(args)), "Integrate function value in given range\r\nCan also use 'integral' instead of symbol ∫", "∫ ( (x)=>... , x_start, x_end, [num_steps] )"));
            addFunc("pSim", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.pSim(args[0], args[1])), "Call function N times, and return success rate (num.true/N)\r\n  - useful in simulations where parametersless lambda will evaluate random state\r\n  - equivalent to pSum( (i)=> returns 1|0, 1,N)/N\r\n  - returns percentage of 'true' lambda results", "pSim ( ()=> bool , N )")); 
            addFunc("call", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.call(args)), "Call lambda function with given parameters\r\n  - useful to calculate complex expression once, and use it many times in single-line expressions:\r\n     call((x,y)=>vec(x+y,x-y,2*x*y), (complex calc of x),(complex calc of y))\r\n  - useful to generate random value once, and use many times in simulations\r\n     call((r)=>vSum(r)+r[0], rndChoose(3,7) )  ", "call( (a,b,...)=> ... , a , b ,... )"));
            addFunc("binarySearch", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.binarySearch(args)), "find lowest INDEX 'i' where boolFunc(i) == true \r\n    Precondition: boolFunc must be monotonous (false,...false,true,...true)\r\n    Return integer 'i' such that boolFunc(i)==true and boolFunc(i-1)==false\r\n    It searches within start..end range, and if not found it raises an exception. Default range is INT32 range, but boundaries can be larger if needed.\r\n    Using 'flag' bitwise parameters can change behaviour when not found:\r\n        - 1: extend 'end' of range ( useful when we do not know actual range)\r\n        - 2: do not raise exception, return 'start'-1 when not found\r\n    Examples:\r\n        binarySearch( (i)=> i^3 >= 4*i^2 , 3,10 ) : 4 \r\n        v=vec(2,5,5,7,9,11,13) ; binarySearch( (i)=> v[i] >= 7 , 0, vLen(v)-1 ) : 3 ", "binarySearch ( (i)=> boolFunc(i) [,i_start=-maxInt [,i_end=+maxInt    [,flags=0]]] )"));
            addFunc("solve", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.find_root(args)), "solve for X that satisfy f(X)=Y_target\r\n    - require monotonous function f(x), otherwise may fail\r\n    - find root of specified function 'f(x)' if Y_target is not specified (Y==0)\r\n    - same as (alias of) 'find_root(..)' function\r\n    - solve((x)=>3*x,6) -> 2  ", "solve(  f(x)  [, Y_target=0 [,tolerance=1e-12 [, left, right ]]] ) "));
            addFunc("find_root", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.find_root(args)), "finds and returns X that satisfy f(X)=Y_target\r\n    - require monotonous function f(x), otherwise may fail\r\n    - find root of specified function 'f(x)' if Y_target is n ot specified (Y==0)\r\n    - find_root((x)=>3*x,6) -> 2  ", "find_root(  f(x)  [, Y_target=0 [,tolerance=1e-12 [, left, right ]]] ) "));
            addFunc("find_min", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.find_min(args)), "finds and returns X from [left,right] range for which f(X) has minimal value\r\n    - assume single unimodal minimum of function f(x) within [left,right] range\r\n    - find_min((x)=> (x-3)^2, -3, 7) -> 3  ", "find_min(  f(x) , left , right [, tolerance=1e-12 ] ) "));
            addFunc("find_max", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.find_max(args)), "finds and returns X from [left,right] range for which f(X) has maximal value\r\n    - assume single unimodal maximum of function f(x) within [left,right] range\r\n    - find_max((x)=> -(x-1)^2, -3, 3) -> 1  ", "find_max(  f(x) , left , right [, tolerance=1e-12 ] ) "));

            // vector functions
            addFunc("vec", new mcFuncParse("", mcFuncParamType.Func, 0, 90, new mcFunc(args => nmc.vec(args)), "Create vector from list of elements.\r\nFirst elements will be at index[0], second at index [1] etc\r\n   v=vec(1,2,3) :  v==vec(1,2,3) and v[0]==1 \r\n   vec(5.3) == 5.3 , single element vector is equivalent to float value 5.3\r\n   vec() : empty vector\r\n   vec(3, vec(1,2)) :  elements can also be vector ", "vec( [e1[,e2[,...]])"));
            addFunc("vAvg", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.vAvg(args)), "Returns average value of a vector\r\n  vAvg( vec(10,20,30) ) : returns 20\r\nIf vector have subvectors, their average value is used\r\n  vAvg( vec(10,vec(0,40)) ) returns 15", "vAvg ( vector )"));
            addFunc("vDim", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.vDim(args)), "Create vector of size N with optional default value that can even be lambda:\r\n   vDim(4) : vec(0,0,0,0)\r\n   vDim(4,7) : vec(7,7,7,7)\r\n   vDim(4,(i)=>i+1) : vec(1,2,3,4)\r\n   vDim(4,(i)=> i==2) : vec(0,0,1,0)", "vDim ( N [,defValue] )"));
            addFunc("vCopy", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.vCopy(args[0])), "Create copy of input vector\r\n   newVec= vCopy( oldVec)\r\nThat is useful since v1=v2 is just reference\r\n    v2= v1; v1[1]=3; v2[1]=55; // -> v1[1] is also 55 now ! \r\n    v2= vCopy(v1);  v1[1]=3; v2[1]=55; // -> v1[1] remains 3 ", "vCopy ( vector )"));
            addFunc("vFor", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.vFor(args[0], args[1])), "Loop equivalent to:\r\n   for (int i=0; i<N; i++) v[i]= lambda(i); return v\r\n   vFor( (i)=>5*i, 3) returns vec(0,5,10)", "vFor( func(i) , N )"));
            addFunc("vFunc", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.vFunc(args)), "Apply scalar function to each element of input vectors, result in new vector\r\n   vFunc( (a)=> a+1 , vec(5,7) ) : vec(6,8)\r\n   vFunc( (a,b)=> a+2*b , vec(5,7), vec(1,2) ) : vec(7,11)", "vFunc ( (a,b,c)=>..., va,vb,vc,...)"));  
            addFunc("vIntersect", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.vIntersect(args[0], args[1])),"Intersection of multiple vectors (even single one), resulting in vector with elements common to all those vectors\r\nConvert result to set (no duplicate elements)", "vIntersect ( va [,vb [,vc...]]] )"));
            addFunc("intersect", new mcFuncParse("", mcFuncParamType.Between, 2, 45, new mcFunc(args => nmc.vIntersect(args[0], args[1])),"Intersection of two vectors, resulting in vector with elements common to both of them\r\nConvert result to set (no duplicate elements)\r\nCan use symbol ∩ instead of 'intersect'", "vecA intersect vecB"));
            addFunc("vLen", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => args[0].vectorLength), "Returns length (number of elements) of a vector", "vLen ( vector )"));
            addFunc("vMax", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.vMax(args[0])), "Returns maximal value from vector\r\n  vMax( vec(7,-3,9,2) ) :  9 ", "vMax ( vector )"));
            addFunc("vMin", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.vMin(args[0])), "Returns minimal value from vector\r\n  vMin( vec(7,-3,9,2) ) :  -3 ", "vMin ( vector )"));
            addFunc("vMaxIdx", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.vMaxIdx(args[0])), "Returns index of maximal value from vector\r\n  vMaxIdx( vec(7,-3,9,2) ) :  2 ", "vMaxIdx ( vector )"));
            addFunc("vMinIdx", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.vMinIdx(args[0])), "Returns index of minimal value from vector\r\n  vMinIdx( vec(7,-3,9,2) ) :  1 ", "vMinIdx ( vector )"));
            addFunc("vMul", new mcFuncParse("", mcFuncParamType.Func, 0, 90, new mcFunc(args => nmc.vMul(args[0])), "Multiply elements of single vector to produce scalar result", "vMul( vector )"));
            addFunc("vDigits", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.vDigits(args[0])), "Returns integer assuming vector elements are digits\r\n   vDigits(vec(1,2,3))=123", "vDigits ( vector )"));
            addFunc("∩", new mcFuncParse("", mcFuncParamType.Between, 2, 45, new mcFunc(args => nmc.vIntersect(args[0], args[1])), "Intersection of two vectors, resulting in vector with elements common to both of them\r\nConvert result to set (no duplicate elements)\r\nCan also use 'intersect' instead of symbol ∩", "vecA ∩ vecB"));
            addFunc("vUnion", new mcFuncParse("", mcFuncParamType.Func, 0, 90, new mcFunc(args => nmc.vUnion(args)), "Union of multiple vectors (even single one), resulting in vector with elements from all those vectors\r\nConvert result to set (no duplicate elements)", "vUnion ( va [,vb [,vc...]]] )")); 
            addFunc("union", new mcFuncParse("", mcFuncParamType.Between, 2, 45, new mcFunc(args => nmc.vUnion(args)), "Operator for union of two vectors, resulting in vector with elements from both of them\r\nConvert result to set (no duplicate elements)\r\nCan use letter U instead of 'union'", "vecA union vecB"));
            addFunc("U", new mcFuncParse("", mcFuncParamType.Between, 2, 45, new mcFunc(args => nmc.vUnion(args)), "Operator for union of two vectors, resulting in vector with elements from both of them\r\nConvert result to set (no duplicate elements)\r\nCan use also 'union' instead letter U", "vecA U vecB"));
            addFunc("vConcat", new mcFuncParse("", mcFuncParamType.Func, 0, 90, new mcFunc(args => nmc.vConcat(args)), "Concatenation of multiple vectors, resulting in vector with elements from all those vectors\r\nDuplicate elements are preserved, so different from Union.", "vConcat ( va [,vb [,vc...]]] )"));
            addFunc("vStdDev", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.vStdDev(args[0])), "Returns standard deviation of a vector", "vStdDev ( vector )"));
            addFunc("vSum", new mcFuncParse("", mcFuncParamType.Func, 0, 90, new mcFunc(args => nmc.vSum(args[0])), "Sum elements of single vector to produce scalar result", "vSum( vector )"));
            addFunc("vTrunc", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.vTrunc(args[0],args[1])), "Truncate length of vector to specified size\r\n    vTrunc( vec(1,2,3,4,5,6), 3):  vec(1,2,3)", "vTrunc ( vec , newSize )"));
            addFunc("vSort", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.vSort(args)), "Sort vector of scalar values\r\n    direction >=0 : sort in ascending order (default)\r\n    direction <0 : sort in descending order\r\n    vSort(vec(7,-1,5,3)) : vec(-1,3,5,7)", "vSort(vec [,direction=+1/-1])"));
            // random generator functions
            addFunc("rnd", new mcFuncParse("", mcFuncParamType.Variable, 0, 100, new mcFunc(args => nmc.rndNumber()), "Returns random float number between 0 and 1\r\n  - value is in range [0,1> : 0 <= rnd < 1\r\n   equivalent to rndNumber(1)\r\n   it is not a function, so does not need parentheses","rnd"));
            addFunc("rndNumber", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.rndNumber(args[0]), mcFuncTrinary.Not), "Returns random number up to given maximum [0,Max>\r\n   if Max is integer value, random results are integer values\r\n   if Max is float, random results are float values\r\n   rndNumber(1) returns float between 0 and 1", "rndNumber ( Max )"));
            addFunc("rndVector", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.rndVector(args[0], args[1]), mcFuncTrinary.Not), "Generate vector of size N, where each element will be random value [0,max>\r\nFor example: rndVector(4,2) can return vec(0,1,1,0) ", "rndVector ( N , max )"));
            addFunc("rndChoose", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.rndChoose(args[0], args[1]), mcFuncTrinary.Not), "Returns vector with N different elements, each up to given max value\r\nThis randomly chooses N values out of Max\r\nFor example: rndChoose(2,8) can return vec(1,5)", "rndChoose ( N , Max )"));
            addFunc("rndNumberWeighted", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.rndNumberWeighted(args[0]), mcFuncTrinary.Not), "Randomly returns integer number between [0.. vectorLength> based on vector with weights\r\nWeights in vector will be normalized, so vector does not necessary need to hold probabilities summing up to 100%\r\nLarger weight at vector[k] means that number k will have larger chance.  For example:\r\n   rndNumberWeighted ( vec(7,3) ) - returns 0 in 70% cases and 1 in 30% cases\r\n   rndNumberWeighted( vec(10%,10%,80%) ) - returns 0 or 1 with probability 10%, or 2 with probability 80%", "rndNumberWeighted( vec(p0,p1..pN) )"));
            addFunc("rndShuffle", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.rndShuffle(args[0]), mcFuncTrinary.Not), "Returns randomly shuffled vector\r\nIf argument is number, it is vector of size N with new values [0..N-1]\r\n   rndShuffle(5) : vec(3,0,2,4,1)\r\nIf argument is vector, it returns those values shuffled in new vector\r\n   rndShuffle(vec(20,vec(1,2),50) ) : vec( vec(1,2),50,20 )", "rndShuffle ( N | vector )"));
            // counter functions
            addFunc("counterCreate", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.counterCreate(args)), "Create normal ArrayCounter (order does matter) with given max [and min] values, and return it as complex vector\r\nCounter will have as many digits as vec_maxValues have items. Each digit will count from its vec_min value to (and including) its vec_max value.\r\nIf vec_minValues is not given, counting starts from zero. Values in both vec_maxValues and vec_minValues must be integers.\r\nBy default canRepeat is enabled, which allows same digit to appear at multiple positions (000,001...), but it can be disabled by setting last param to zero. \r\n    cv=counterCreate(vec(2,1)) - would count  00,01,10,11,20,21 and finish\r\n    cv=counterCreate(vec(1,3,2),vec(0,2,1)) - would count 021,022,031,032,121,122,131,132 and finish\r\nUse counterNext(cv) to advance counter, counterNotFinished(cv) to check for finish and counterValues(cv) to get current digits\r\n    for( ac= counterCreate(,,,) , counterNotFinished(ac), ac= counterNext(ac) ) { .. use counterValues(ac) ..}", "counterCreate( vec_maxValues [, vec_minValues [, canRepeat=1]] )"));
            addFunc("counterCreateComb", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.counterCreateComb(args)), "Create combination ArrayCounter (order does NOT matter, always non-descending), and return it as complex vector.\r\nIt is 'combinational' since order does not matter, so 12==21 and 233==323 (if canRepeat). Therefore it has smaller number of combinations than normal counter.\r\nCounter will have numDigits digits, each counting from minValue [default 1] to (and including) maxValue, which are both single integer arguments.\r\n    cv=counterCreateComb( 2,4 ) - would count  12,13,14,23,24,34 . This is equivalent to 'choose 2 out of 4'\r\n    cv=counterCreateComb( 2,2,1 ) - would only count  12 (!)\r\nIf canRepeat is true (!= 0), it allows repeating same digit (111), but still non-descending (11,12,22...):\r\n    cv=counterCreateComb( 2,4,1,1 ) - would count  11,12,13,14,22,23,24,33,34,44\r\nUse counterNext(cv) to advance counter, counterNotFinished(cv) to check for finish and counterValues(cv) to get current digits\r\n    for( ac= counterCreateComb(,,,) , counterNotFinished(ac), ac= counterNext(ac) ) { .. use counterValues(ac) ..}", "counterCreateComb( numDigits, maxValue [, minValue=1 [, canRepeat=0]] )"));
            addFunc("counterCreatePerm", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.counterCreatePerm(args)), "Create permutation ArrayCounter (order does matter), and return it as complex vector.\r\nIt is 'permutational' since order does matter, so 12<>21\r\nCounter will permutate given elements and will have as many digits as number of those elements\r\n    cv=counterCreatePerm( vec(1,2,3) ) - would count  123,132,213,231,312,321\r\n    cv=counterCreatePerm( N ) - will count as if vec(1,2,...N) are elements\r\n    for( ac= counterCreatePerm(el) , counterNotFinished(ac), ac= counterNext(ac) ) { ..use ac[k]..}", "counterCreatePerm(vecElements) or counterCreatePerm( numElements )"));
            addFunc("counterNext", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.counterNext(args[0])), "Advance given ArrayCounter by one, returning new modified counter vector\r\n    cv= counterNext(cv)\r\nCheck if counter reached finish with counterNotFinished:\r\n    while( counterNotFinished(cv) ){.. use counterValues(cv) ...; cv= counterNext(cv); }\r\n    for( ac= counterCreate(,,,) , counterNotFinished(ac), ac= counterNext(ac) ) { .. use counterValues(ac) ..}", "counterNext( counterVector )"));
            addFunc("counterNotFinished", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.counterNotFinished(args[0])), "Returns true (1) if counter is finished - was not able to advance on last counterNext()\r\n    while( counterNotFinished(cv) ){.. use counterValues(cv) ...; cv= counterNext(cv); }\r\n    for( ac= counterCreate(,,,) , counterNotFinished(ac), ac= counterNext(ac) ) { .. use counterValues(ac) ..}", "counterNotFinished( counterVector )"));
            addFunc("counterValues", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.counterValues(args[0])), "Return vector with current counter values\r\nIt will have number of elements same as number of digits specifiec with counterCreate\r\nMost significant digit is at index 0, least significant at index vLen(counterVector)-1\r\n  counterValues(cv)[1] - it is 2nd most significant digit", "counterValues( counterVector )"));
            addFunc("counterTotalCount", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.counterTotalCount(args[0])), "Return total number of combinations for given counter.\r\nIt will calculate it (fast) for normal counters or if all digits have same min/max values\r\nOtherwise it will count, in which case it can be slow!\r\n  counterTotalCount(cv) - 720 if cv was for noRepeat noPerm ", "counterTotalCount( counterVector )"));
            addFunc("comboCount", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.comboCount(args)), "Returns in how many different ways given vector could be arranged\r\nIf all N elements of vector are different, it will be N!\r\nBut if some elements are repeating within vector, it will be less than N!\r\n   comboCount(vec(1,2,3))==6 : 123,132,213,231,312,321\r\n    comboCount(vec(1,1,3))==3 : 113,131,311", "comboCount( Vector )"));
            // custom FLOAT math functions
            addFunc("nPr", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => Number.nPr(args[0].Number, args[1].Number)), "Returns number of permutations, nPr = N!/(N-r)!\r\n  number of ways to choose 'r' out of 'n' when order DOES matter \r\n  generally N,R and result are integers , but notepad will support float too\r\nSee also 'nCr' and 'choose'", " nPr ( n, r ) "));
            addFunc("nCr", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => Number.nCr(args[0].Number, args[1].Number)), "Returns number of combinations, nCr = N!/r!/(N-r)!\r\n  number of ways to choose 'r' out of 'n' when order does NOT matter \r\n  generally N,R and result are integers , but notepad will support float too\r\nEquivalent to 'choose', see also 'nPr'", " nCr ( n, r ) "));
            addFunc("choose", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => Number.nCr(args[0].Number, args[1].Number)), "Returns number for 'choose k out of N' , nCk = N!/k!/(N-k)!\r\n  represent number of possible ways to select k numbers out of N (k<=N) \r\n  generally N,k and result are integers , but notepad will support float too\r\nEquivalent to 'nCr'", " choose ( N, r ) "));
            addFunc("gamma", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Gamma(args[0].Number)), "Gamma function\r\nSimilar to factoriel, since x!= gamma(x+1) , but works with float values", " gamma ( x )"));
            addFunc("harmonic", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.Harmonic(args)), "return harmonic number Hn= sum(1/i) for i=1..n = 1/N+1/(N-1)+...1/1\r\n     N*harmonic(N) = expected tries to complete all set (all different coupons or get all six dice numbers (N=6) ...)\r\n     assume probability to get each of N different set items is equal, 1/N\r\nwhen coverage argument is used ( <100% ) , return partial harmonic number\r\n     Hnp= sum(1/i) for i= (1-coverage)*n+1..n = Hn-H(1+(1-coverage)*n)\r\n     N*harmonic(N, 30%) = expected tries to complete 30% of a set", "harmonic( ( n [,coverage=100%] )"));
            addFunc("max", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.max(args)), "Returns largest float value from the scecified list\r\n   if vectors specified, find largest number in vector", "max ( a[,b[,c,...]] )"));
            addFunc("min", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.min(args)), "Returns smallest float value from the scecified list\r\n   if vectors specified, find smallest number in vector", "min ( a[,b[,c,...]] )"));
            addFunc("isInfinity", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.isInfinity(args[0])), "Returns true is supplied scalar value is infinite", "isInfinity ( Value )"));
            // custom INTEGER math functions
            addFunc("isInt", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.isIntAx(args[0].Number) ? (double)1 : 0), "Returns true(1) is number is integer\r\nSince values are by default float, floats very close to integers are also counted as integers", "isInt ( x )"));
            addFunc("isEven", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.isEven(args[0])), "Returns true if number is even\r\nValid only for integers, and result in true/false (1/0)\r\n    isEven(4): 1\r\n    isEven(-5): 0", "isEven ( x )"));
            addFunc("isOdd", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.isOdd(args[0])), "Returns true if number is odd\r\nValid only for integers, and result in true/false (1/0)\r\n    isOdd(4): 0\r\n    isOdd(-5): 1", "isOdd ( x )"));
            addFunc("gcd", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.gcd(args)), "Returns GCD ( Greater Common Denominator ) of multiple integer numbers\r\n    gcd(54,24):  6", "gcd ( a,b[,c[,d...]] )"));
            addFunc("lcm", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.lcm(args)), "Returns LCM ( Least Common Multiplier ) of multiple integer numbers\r\n    lcm(21,6):  42", "lcm ( a,b[,c[,d...]] )"));
            addFunc("mod", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => args[0].Number % args[1].Number), "Remainder of a division\r\n   mod(5,3) == 2, mod(4.5,3)==1.5", "mod (a,b)"));
            addFunc("isPrime", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.isPrime(args[0])), "Returns true if number is prime\r\n   isPrime(7) = true \r\n   isPrime(10) = false", "isPrime ( n )"));
            addFunc("prime", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.prime(args)), "Returns n-th prime number, or list of primes [n-th, m-th]\r\n   prime(1000) = 7919 \r\n   prime(2,4) = vec(3,5,7)", "prime ( n [,m] )"));
            addFunc("primeNext", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.nextPrime(args)), "Returns next prime number larger than specified value, or k-th next if second parameter given\r\nIf k is negative, returns k-th prime smaller than number\r\n   primeNext(14) == 17\r\n   primeNext(14,-1) == 13", "primeNext ( n [,k] )"));
            addFunc("primePi", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.primePi(args[0])), "Returns number of primes smaller or equal to given N \r\n   primePi(10) = 4  (2,3,5,7) ", "primePi ( N )"));
            addFunc("primesBetween", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.primesBetween(args)), "Returns vector with all primes between two specified numbers\r\nUnlike prime(a,b) which returns from a-th to b-th prime,\r\nprimesBetween(a,b) return a<=primes<=b\r\n    prime(10,13) = vec(29,31,37,41)\r\n    primesBetween(10,13) = vec(11,13)", "primesBetween ( a , b )"));
            addFunc("primeFactors", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.primeFactors(args[0])), "Returns vector with prime factors of a given number\r\nTo get distinct factors, use primeFactorsDisctinct(n)\r\n   primeFactors(600) = vec(2,2,2,3,5,5) ", "primeFactors ( N )"));
            addFunc("primeFactorsDistinct", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.primeFactorsDistinct(args[0])), "Returns vector with distinct prime factors of a given number\r\n     primeFactorsDistinct(600) = vec(2,3,5)\r\nSlower alternative is:    vUnion( primeFactors(N))\r\nTo get all factors, use primeFactors(n)", "primeFactorsDistinct ( N )"));
            addFunc("primeFactorsPowers", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.primeFactorsPowers(args[0])), "Returns vector with pairs of distinct prime factors of a given number and their repeat number.\r\n     primeFactorsPowers(600) = v( v(2,3), v(3,1), v(5,2) )  // 600= 2^3 * 3^1 * 5^2\r\nTo get just distinct factors, use primeFactorsDistinct(n)\r\n     primeFactorsDistinct(600) = vec(2,3,5)\r\nTo get all factors, use primeFactors(n)r\n   primeFactors(600) = vec(2,2,2,3,5,5) // 600= 2*2*2*3*5*5", "primeFactorsDistinct ( N )"));
            addFunc("factors", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.factors(args[0])), "Returns vector with factors (divisors ) of a given number\r\n   factors(12) = vec(2,3,4,6) ", "factors ( N )"));
            // STATISTICAL functions
            const string dist_vec = "\r\n\r\nReturns dist_vec= vec ( μ = expected , σ = stddev , dist_ID = type of distribution, x_min= minimal possible random value, x_max, input_parameters {n,p,ƛ...} )\r\n   - μ = E(x) = expected value = ∑ x*pmf(x)  ; average value to get after many random numbers from this distribution\r\n   - σ^2 = variance (not in result) = ∑ p(x)*(x-μ)^2  ; square of standard deviation\r\n   - σ = stddev = standard deviation = √ variance ; expected variability\r\n   - dist_ID for discrete is 100..199, for uniforms is 200..299 (find it in dist_xyz tooltips)\r\n   - min_x and max_x are minimal and maximal random variables x that are possible/valid in this distribution\r\n   - rest of values in result vector are input parameters to this function\r\nReturned dist_vec is used in cdf, pmf/pdf and dist_Random functions:\r\n   - pmf( x, dist_vec): Probability Mass Function, returns probability to get exactly 'x' from this random distribution\r\n   - pdf( x, dist_vec): Probability Density Function, same as pmf but for continuous distributions (they are interchangeable)\r\n   - cdf( x, dist_vec) Cumulative Distribution Function, returns probability to get <= x\r\n   - rndNumber_dist(dist_vec): generate random number from given distribution";
            addFunc("dist_uniform", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.dist_uniform_discrete(args)), "Uniform distribution (discrete): x = equally probable for any integer between a and b\r\n   - input: a = min possible value, b= max possible random value\r\nFor uniform distrubution:\r\n   μ = expected = (a+b)/2\r\n   V = variance = (b-a+2)*(b-a)/12\r\n   σ = stddev = √ variance\r\n   pmf(x)= 1/(b-a+1)  " + dist_vec, "dist_uniform( a , b )"));
            addFunc("dist_binomial", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.dist_binomial(args)), "Binomial distribution (discrete): x = # of successes in n fixed trials\r\n   - input: n= number of fixed trials, p= probability of success on each independent trial\r\nFor binomial distrubution:\r\n   μ = expected = n*p\r\n   V = variance = n*p*(1-p)\r\n   σ = stddev = √ variance = √ n*p*(1-p)\r\n   pmf(x)= choose(n,x)*p^x*(1-p)^(n-x)" + dist_vec, "dist_binomial( n , p )"));
            addFunc("dist_Poisson", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.dist_Poisson(args)), " Poisson distribution (discrete): x = number of events in fixed time period, given rate\r\n   - input: ƛ= lambda = rate ( eg. average number of events in fixed period)\r\nFor Poisson distrubution:\r\n   μ = expected = ƛ\r\n   V = variance = ƛ \r\n   σ = stddev = √ variance = √ ƛ \r\n   pmf(x)= e^-ƛ*ƛ^x/x!" + dist_vec, "dist_poisson( ƛ )"));
            addFunc("dist_geometric", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.dist_geometric(args)), "Geometric distribution (discrete): x = number of trials up to first success\r\n   - input: p= probability of success on each independent trial\r\nFor geometric distrubution:\r\n   μ = expected = 1/p\r\n   V = variance = (1-p)/p^2\r\n   σ = stddev = √ variance = √(1-p)/p\r\n   pmf(x)= p*(1-p)^(x-1) "+dist_vec, "dist_geometric( p )"));
            addFunc("dist_negative_binomial", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.dist_negative_binomial(args)), "Negative binomial distribution (discrete): x = number of trials until K successes\r\n   - input:  p= probability of success on each independent trial, k= number of successes needed\r\nFor negative binomial distrubution:\r\n   μ = expected = k / p\r\n   V = variance = k*(1-p)/p^2\r\n   σ = stddev = √ variance = √k*√(1-p)/p\r\n   pmf(x)= (1-p)^(x-k)*p^k* choose(x-1,k-1) "+dist_vec, "dist_negative_binomial( p , k )"));
            addFunc("dist_hypergeometric", new mcFuncParse("", mcFuncParamType.Func, 3, 90, new mcFunc(args => nmc.dist_hypergeometric(args)), "Hypergeometric distribution (discrete): x = number of marked individuals in sample taken without replacement\r\n   - input:  n= size of sample, N=total number of individuals, M= number of marked individuals\r\n   - for example: if we take n=10 balls out of box with total N=20 balls where there are M=5 black balls, what is the chance to get x=3 black balls in sample?\r\nFor hypergeometric distrubution:\r\n   μ = expected = n* M / N\r\n   V = variance = n*M(N-M)(N-n)/N^2/(N-1)\r\n   σ = stddev = √ variance\r\n   pmf(x)= choose(M,x)*choose(N-M,n-x)/choose(N,n) " + dist_vec, "dist_hypergeometric( n , N , M )"));
            addFunc("dist_uniform_continuous", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.dist_uniform_continuous(args)), "Uniform distribution (continuous): equaly likely uncountable outcomes in floating point range [a,b]\r\n   - input: a = min possible value, b= max possible random value\r\nFor uniform continuous distrubution:\r\n   μ = expected = (a+b)/2\r\n   V = variance = (b-a)^2/12\r\n   σ = stddev = √ variance = (b-a)/√12\r\n   pdf(x)= 1/(b-a)\r\n   cdf(x)= (x-a)/(b-a)  " + dist_vec, "dist_uniform_continuous( a , b )"));
            addFunc("dist_normal", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.dist_normal(args)), "Normal distribution (continuous): standard bell shaped distribution\r\n   - input: μ = expected , σ = stddev\r\n   μ = expected = same as input  μ\r\n   σ = stddev = √ variance = same as input σ\r\n   pdf(x)= e^(-1/2*((x-μ)/σ)^2)/σ/√(2*pi)\r\n   cdf(x)=   (erf(z/√2)+1)/2  ; where z is normalized x , z = (x-μ)/σ" + dist_vec+ "\r\n\r\nAlso look at:\r\n  - cdf_normal(x) : cdf for Gauss distribution, does not need dist_vec\r\n  - erf(x): error function, erf(0)=0, erf(+-3.5)~1\r\n  - error_margin(Confidence_Level_percent, stddev ) : +/- around stddev to get given confidence", "dist_normal( μ , σ )"));
            addFunc("dist_exponential", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.dist_exponential(args)), "Exponential distribution (continuous): x = time between or until an event, given rate (also birth,decay,interest, queuing rates...)\r\n   - input: ƛ= lambda ~ rate ( eg. average number of events in period)\r\nFor normal distribution:\r\n   μ = expected = 1/ƛ  ; if ƛ=rate, then epected time between events is 1/ƛ\r\n   V = variance = 1/ƛ^2 \r\n   σ = stddev = √ variance = 1/ƛ \r\n   pdf(x)= ƛ*e^(-ƛ*x)\r\n   cdff(x)= 1-e^(-ƛ*x)" + dist_vec, "dist_exponential( ƛ )"));
            addFunc("dist_sample", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.dist_sample(args)), "Statistical sample distribution : actually sampled data\r\n   - input:  vector with n sampled values\r\n   μ = expected = vSum()/n\r\n   V = variance = ∑(x-μ)^2/(n-1) ; where (n-1) instead of 'n' for correction\r\n   σ = stddev = √ variance\r\n " + dist_vec, "dist_sample( vec(x1,x2,...) )"));
            addFunc("dist_trials_sum", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.dist_trials_sum(args)), "Return distribution of total (sum) of n random numbers\r\n   - input: n= number of trials , vec(μ,σ) = vector (can be dist_vec) describing individual random variables from that sum\r\n   - due to Central Limit Theorem (CLT), distribution of sums approaches normal distribution (if n>=30!) regardless of individual distribution\r\n   - μ_sum = μ * n\r\n   - variance_sum = variance * n \r\n   - σ_sum = σ * √n\r\n   - cdf(x) = cdf of normal distribution due to Central Limit Theorem  " + dist_vec, "dist_trials_sum( n, vec(μ,σ) )"));
            addFunc("dist_trials_avg", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.dist_trials_avg(args)), "Return distribution of average of n random variables\r\n   - input: n= number of trials , vec(μ,σ) = vector (can be dist_vec) describing individual random variables from that average\r\n   - due to Central Limit Theorem (CLT), distribution of averages approaches normal distribution (if n>=30!)  regardless of individual distribution\r\n   - μ_avg = μ\r\n   - variance_avg = variance / n\r\n   - σ_avg = σ / √n\r\n   - cdf(x) = cdf of normal distribution due to Central Limit Theorem  " + dist_vec, "dist_trials_avg( n, vec(μ,σ) )"));
            addFunc("dist_trials_proportion", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.dist_trials_proportion(args)), "Return distribution of proportion of successes over n trials\r\n   - input: n= number of trials , p= chance of success for each trial\r\n   - due to Central Limit Theorem (CLT), distribution of proportions approaches normal distribution if n*min(p,1-p) >= 5 !\r\n   - μ_prop = p\r\n   - variance_prop = p * (1-p) / n\r\n   - σ_prop= √(p*(1-p)/n)  ;  it is σ_binomial / n , or proportion of successes in binomial\r\n   - cdf(x) = cdf of normal distribution due to Central Limit Theorem " + dist_vec, "dist_trials_proportion( n , p )"));
            addFunc("erf", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.erf(args[0])), "Return error function for given x : probability that normal random value is within +/- X of its average \r\n   - used for cdf of normal distribution: z=(x-μ)/σ ; cdf= [erf(z/√2)+1]/2\r\n   - erf(0)=0, erf(+-3.5)~1 ", "erf( x )"));
            addFunc("error_margin", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.error_margin(args)), "return Error Margin: +- value around expected one, in order to have desired confidence\r\n   - input parameters are ( CLp= confidence level percent, σ = stddev )\r\n       - MOE = Zvalue(CLp)* stddev\r\n       - to get Z value, omit σ or use σ = 1 \r\n       - usual Confidence Level is 95% (Z value ~ 1.96 )\r\n       - Rule of thumb for number of trials needed (at confidence level 95%), to achieve desired MOE: \r\n                   nTrials = 1 / MOE^2  ( only valid IF value and MOE are percentages, like in proportion sampling )\r\n       - usually used as:    Expected = value +/- MOE ", "error_margin( Confidence_Level_percent [,stddev] )"));
            addFunc("pmf", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.pmf(args)), "Return Probability Mass Function (pmf) value for given x :  probability that random variable in specified distribution is exactly equal to x ( p(X) == x )\r\n   - inputs: x = value for which we need pmf(x) , dist_vec = vec(μ,σ,dist_ID,dist_params{n,p,ƛ...}) obtained from dist_xyz() functions \r\n   - for continuous distributions pmf(x) will actually return pdf(x) : Probability Density Function\r\n   - pmf(x)/pdf(x) depend on distribution, and their formula is shown in specific dist_xyz tooltips\r\n   -  for large values, some bell-shaped discrete distributions are approximated using pdf(x) of normal distribution", "pmf( dist_vec, x )"));
            addFunc("pdf", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.pmf(args)), "Return Probability Distribution Function (pdf) value for given x :  probability that random variable in specified continuous distribution will be around x ( p(X) ~ x )\r\n   - inputs: x = value for which we need pdf(x) , dist_vec = vec(μ,σ,dist_ID,dist_params{n,p,ƛ...}) obtained from dist_xyz() functions \r\n   - for discrete distributions pdf(x) will actually return pmf(x) : Probability Mass Function\r\n   - pdf(x)/pmf(x) depend on distribution, and their formula is shown in specific dist_xyz tooltips", "pdf( dist_vec, x )"));
            addFunc("cdf", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => nmc.cdf(args)), "Return Cumulative Distribution Function (cdf) value for given x :  probability that random variable in specified distribution is less or equal to x ( p(X) <= x )\r\n   - inputs:  dist_vec = vec(μ,σ,dist_ID,dist_params{n,p,ƛ...}) obtained from dist_xyz() functions , x = value for which we need cdf(x)\r\n   - if 3rd input [x2] is given, returns probability that random variable is in range [x,x2] inclusive, with proper continuity corrections if approximated [-0.5,+0.5]\r\n   - for discrete distributions:  cdf(x) = sum( -inf .. x ) pmf(x)\r\n   - for continuous distributions:  cdf(x) = integral( -inf .. x ) pdf(x)\r\n   - for normal continuous distributions:  cdf(x)=   (erf(z/√2)+1)/2  ; where z is normalized x , z = (x-μ)/σ\r\n   -  for large values, some bell-shaped distributions are approximated using cdf(x) of normal distribution\r\nProbability that random variable is in some range a..b is obtained by subtracting cdf:  p(x in a..b)= cdf(b)- cdf(a):\r\n   - for continuous distribution p(x in a..b) = cdf(b) - cdf(a) ,  regardless if boundaries are included or excluded ( a<x<b or a<=x<=b or a<x<=b...)\r\n   - for discrete distributions in cdf(b)-cdf(a), it matters if  x<=b ( use cdf(b) ) or x<b ( use cdf(b-1) ) ; similarly for a<x (not included) use cdf(a) and for a<=x use cdf(a-1)\r\n   - or simply use cdf(a,b) to get probability in range, it will autocorrect for discrete distributions\r\n   - note that all examples here using 'cdf(a)' or 'cdf(a,b)' are omitting necessary first parameter 'd=distribution vector', so 'cdf(d,a[,b])' is correct form ", "cdf( dist_vec , x [, x2] )"));
            addFunc("cdf_normal", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.cdf_normal(args)), "Return cumulative distribution function (cdf) value of normal distribution for given x\r\n   - if only x given,  Z distribution assume μ=0 and σ=1 \r\n   - cdf_normal(-3.5)~0  cdf_normal(0)=0.5  cdf_normal(+3.5)~1\r\n   - unlike regular 'cdf(d,a,b)', 'cdf_normal' does not support 'a..b' range in single call\r\n   - so use cdf_normal(B)-cdf_normal(A) = probability that A<= x <=B ", "cdf_normal( x [, vec(μ,σ)] )"));
            addFunc("rndNumber_dist", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.rndNumber_dist(args)), "Return random number based on given probability distribution\r\n   - inputs: dist_vec = vec(μ,σ,dist_ID,dist_params{n,p,ƛ...}) obtained from dist_xyz() functions \r\n   - it will return random number between x_min (dist_vec[3]) and x_max (dist_vec[4])\r\n   - for disctrete distributions it returns integer numbers, and for continuous distributions it returns floating point number", "rndNumber_dist( dist_vec )"));

            // other functions
            addFunc("diffdigit", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => Number.diffDigit(args[0].Number, args[1].Number)), "Compare two numbers and return at which decimal digit they differ", " diffdigit (5.12305, 5.12345"));
            addFunc("hex", new mcFuncParse("", mcFuncParamType.None, 0, 100, new mcFunc(args => { defaultNumberBase = 16; return defaultNumberBase; }), "After specified, numbers are in hexadecimal format in results\r\nConstant hexa values can be always specified using 0x3CD5 format "));
            addFunc("dec", new mcFuncParse("", mcFuncParamType.None, 0, 100, new mcFunc(args => { defaultNumberBase = 10; return defaultNumberBase; }), "After specified, numbers are in decimal format in results\r\nConstant values in expressions arre by default in decimal format "));
            addFunc("bin", new mcFuncParse("", mcFuncParamType.None, 0, 100, new mcFunc(args => { defaultNumberBase = 2; return defaultNumberBase; }), "After specified, numbers are in binary format in results\r\nConstant binary values can be always specified using 0b1001 format "));
            // System.Math functions
            addFuncVec1("abs", (a) => Number.Abs(a), "Returns absolute value of a number", "abs (x)");
            addFunc("acos", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Acos(args[0].Number)),"Returns the angle whose cosine is the specified number","acos (x)"));
            addFunc("acosh", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Acosh(args[0].Number)), "Returns the angle whose hyperbolic cosine is the specified number", "acosh (x)"));
            addFunc("asin", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Asin(args[0].Number)),"Returns the angle whose sine is the specified number", "asin (x)"));
            addFunc("asinh", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Asinh(args[0].Number)), "Returns the angle whose hyperbolic sine is the specified number", "asinh (x)"));
            addFunc("atan", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Atan(args[0].Number)), "Returns the angle whose tangent is the specified number", "atan (x)"));
            addFunc("atanh", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Atanh(args[0].Number)), "Returns the angle whose hyperbolic tangent is the specified number", "atanh (x)"));
            addFunc("atan2", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => Number.Atan2(args[0].Number, args[1].Number)), "Returns the angle whose tangent is the quotient of two specified numbers", "atan2 (y,x)"));
            addFuncVec1("ceiling", (a) => Number.Ceiling(a), "Returns smallest integer that is greater or equal to specified number", "ceiling (x)");
            addFunc("cos", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Cos(args[0].Number)),"Return the cosine of the angle specified in radians\r\nIf cosine of value in degrees is needed, use cos(45'deg')", " cos ( angle )"));
            addFunc("cosh", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Cosh(args[0].Number)),"Returns the hyperbolic cosine of the specified angle","cosh ( angle )"));
            addFunc("exp", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Exp(args[0].Number)),"Returns e raised to the specified power\r\n   equivalent to : e^x","exp ( x)"));
            addFuncVec1("floor", (a) => Number.Floor(a), "Returns largest integer that is smaller or equal to specified number\r\nFor negative values it differs from truncate: floor(-5.5)== -6", "floor (x)");
            addFunc("ln", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.Ln(args)), "Returns logarithm of x in given base, or natural (base e) if base is not specified\r\n    ln(1000)=6.91\r\n    ln(256,2)=8", "ln (x [,base])"));
            addFunc("log", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => nmc.Log(args) ), "Returns logarithm of x in given base, or base 10 if not specified\r\n    log(1000)=3\r\n    log(256,2)=8", "log (x [,base])"));
            addFunc("log2", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Log2(args[0].Number)), "Returns logarithm of x in base 2\r\n    log2(1024)=10\r\n    log(256)=8", "log2(x)"));
            addFunc("log10", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Log10(args[0].Number)), "Returns logarithm of x in base 10\r\n    log10(1000)=3\r\n    log10(100)=2", "log10(x)"));
            addFunc("pow", new mcFuncParse("", mcFuncParamType.Func, 2, 90, new mcFunc(args => Number.Pow(args[0].Number, args[1].Number)),"Returns  the specified number raised to the specified power\r\n   equivalent to : value^power","pow (value,power)"));
            addFuncVec2("round", (a, b) => Number.Round(a, b.AsInt), "Rounds value to a specified number of digits.\r\nTo only round display, use ': digits' , as in 0.1234:2 ", "round ( value [,digits] )");
            addFuncVec1("sign", (a) => Number.Sign(a), "Returns integer that indicate the sign of specified value\r\n   positive values return +1, negative -1 and zero returns 0", "sign (x)");
            addFunc("sin", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Sin(args[0].Number)), "Return the sine of the angle specified in radians\r\nIf sine of value in degrees is needed, use sin(45'deg')", " sin ( angle )"));
            addFunc("sinh", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Sinh(args[0].Number)), "Returns the hyperbolic sine of the specified angle", "sinh ( angle )"));
            addFunc("sqrt", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Sqrt(args[0].Number)), "Returns square root of specified value\r\n   can also use symbol √ as in: √4 or √(x+3)","sqrt ( value ) "));
            addFunc("tan", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Tan(args[0].Number)),"Returns the tangent of the specified angle","tan ( angle )"));
            addFunc("tanh", new mcFuncParse("", mcFuncParamType.Func, 1, 90, new mcFunc(args => Number.Tanh(args[0].Number)), "Returns the hyperbolic tangent of the specified angle", "tanh ( angle )"));
            addFuncVec1("truncate", (a) => Number.Truncate(a), "Return integer part of specified number\r\nFor negative values it differs from floor: truncate(-5.5)== -5", "truncate (x)");



            lastResult = new CalcResult();
            cacheCSclear();

            // store all that for future resets
            StoreInit();
        }


        public static bool isTimeout()
        {
            return (!cfg.timeoutDisabled) && (swFunc.ElapsedMilliseconds > cfg.timeoutFuncMs);
        }
        public static void testTimeout()
        {
            if ((!cfg.timeoutDisabled) && (swFunc.ElapsedMilliseconds > cfg.timeoutFuncMs))
                throw new mcException("ERR:Timeout");
        }



        public static void restartFuncTimeout()
        {
            swFunc.Restart();
        }

        public static bool isDocTimeout()
        {
            if (!emergencyDocSaved && (swDoc.ElapsedMilliseconds > 3000))
            {   // if execution lasts over 3sec, save document in case of potential break
                emergencyDocSaved = true;
                SaveFileBasic(AppDomain.CurrentDomain.BaseDirectory + "\\_lastDocument.calc", emergencyNotepadText, mcCompiler.lastCompiledSource);
            }
            return (!cfg.timeoutDisabled) && (swDoc.ElapsedMilliseconds > cfg.timeoutDocMs);
        }


        // store state after builtin and preset are processed
        public static void StoreInit()
        {
            lastResult = new CalcResult();
            lastTotalResult = new mcTotalResult();
            defaultNumberBase = 10; // always reset at start of new doc, even if preset file changes it
            stored_after_Preset = StoreState();
        }

        // store list of functions, constants and variables to newly created state
        public static mcState StoreState(mcTotalResult currentRes = null)
        {
            return new mcState(functions, functionsCased, varNames, varValues, varParents, units, unitsCased, lastResult, defaultNumberBase, currentRes);
        }

        // load list of functions, constants and variables from given state
        public static void LoadState(mcState saved_state)
        {
            saved_state.CopyTo(out functions, out functionsCased, out varNames, out varValues, out varParents, out units, out unitsCased, out lastResult, out defaultNumberBase);
        }



        // reset list of functions, constants and variables to new document - remains built in functions, preset and CSfunctions
        public static void Reset()
        {
            LoadState(stored_after_Preset);
            mcCompiler.appendFunctions(); // returns true if compile was changed
            stored_Mid = null;
        }

        // recompile CS functions and mark midpoint as invalid, but do not redraw/recalc notepad side
        // if 'addNewFunctions' is specified, add user c# functions to notepad list
        public static string recompileCS(string code, bool addNewFunctions)
        {
            var res = mcCompiler.Compile(mc.CSpresetSource + code, addNewFunctions);
            stored_Mid = null;
            return res;
        }

        // Cache management
        public static void cacheFuncClear()
        {
            mc.cacheFunc = null;
        }
        public static void cacheCSclear()
        {
            nmCache.Clear();
        }



        // used by mcExp to add list of newly defined func
        public static void addVolatile(string name)
        {
            volatileNames.Add(name);
        }
        public static bool hasVolatile(string name)
        {
            return volatileNames.Contains(name);
        }


        /// <summary>
        /// call supplied lambda function within thread, abort on timeout (default mc.cfg.timeoutFuncMs)
        /// </summary>
        static public Toutput CallThread<Toutput>(Func<Toutput> theFunc, long execTimeout=-1, int stackSize=4000000) where Toutput:class
        {
            //return theFunc(); // debug no-thread version
            Exception eThread = null;
            Toutput res = null;
            var iThread = new Thread(() =>
            {
                try
                {
                    res = theFunc();
                }
                catch (Exception e)
                {
                    eThread = e;
                }
            } , stackSize );
            if (execTimeout < 0) execTimeout = mc.cfg.timeoutFuncMs;
            iThread.Start();
            if (iThread.Join(mc.cfg.timeoutDisabled ? Timeout.Infinite : (int)execTimeout))
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
                try
                {
                    iThread.Abort(); 
                }
                catch { } // throwing 'obsolete' warning on NET Core/5+, but still does abort ?!
                throw new mcException("ERR:Timeout");
            }
            return res;
        }


        public static CalcResult Evaluate(string input, int srcLine)
        {
            try
            {
                return mc.CallThread<CalcResult>(() => Evaluate_inner(input, srcLine));
            }
            catch (Exception e)
            {
                // this should be called only in case of Timeout exception
                var res = new CalcResult();
                res.isValue = false;
                res.isError = true;
                res.Text = e.Message.Replace("\r\n", " ").Replace("\n", " ");
                return res;
            }
        }


        public static mcExp lastExp = null;
        // updating, compiling and evaluating one single row
        public static CalcResult Evaluate_inner(string input, int srcLine)
        {
            if (input == "") return new CalcResult();
            CalcResult res;
            Stopwatch sw = new Stopwatch();
            sw.Start(); // this one can not be reseted by compound commands like while
            swFunc.Restart();
            try
            {
                // parsing/compiling - this is part that can throw parse exceptions
                volatileNames = new List<string>();
                var ctx = new mcContext() { startLoc = new Point(0, srcLine) };
                ctx.numSubblocks = srcLine;
                lastExp = new mcExp(input, ctx);
                // executing, this handle its exception
                mcFunc.resetGuards();
                res = lastExp.Evaluate();
                res.hasRedefinition = lastExp.hasRedef;
                if (res.isValue)
                    lastResult = res;
            }
            catch (Exception e)
            {
                // this was parse exception, so create dummy return
                res = new CalcResult();
                res.isError = true;
                res.Text = "Syntax: "+e.Message;
                res.errPos = mcParse.getCurrentPosition();
                res.errLastPos = mcParse.lastEx();
                // cleanup dangling new functions
                foreach (var vol in volatileNames)
                    removeFunc(vol);
            }
            swFunc.Stop();
            res.time_ms = sw.ElapsedMilliseconds;
            sw.Stop();
            return res;
        }


        // recreate lastResult from parsedlines, given line
        static public CalcResult getLastResult(List<ParseLine> parsedLines, int lastLine = -1)
        {
            CalcResult lastResult = null;
            if (lastLine < 0) lastLine = parsedLines.Count - 1;
            for (int i = lastLine; (i >= 0) && (lastResult == null); i--)
                if (parsedLines[i].res.isValue)
                    lastResult = parsedLines[i].res;
            if (lastResult == null)
                lastResult = new CalcResult();
            return lastResult;
        }


        static bool emergencyDocSaved = false;
        static string emergencyNotepadText = "";


        // calculate and evaluate multiline text, updating function lists etc
        static public mcTotalResult calcLines(string text, LineRange changedRange=null )
        {
            var res = new mcTotalResult();
            res.parsedLines = new List<ParseLine>();
            res.sourceHash = text.GetHashCode();
            // Split into lines, skipping over {([])}:  '\r' removed since sometimes new lines are "\n" (copy-paste, load xaml) and sometimes "\r\n"
            res.cLines =  compLine.makeArray( mcParse.splitOver( text.Replace("\r", "") , '\n' ) ); 
            res.errorTxt = "";
            int cursorSource = -1;
            if (changedRange != null)
                cursorSource = changedRange.currentLine;
            int cursorCLin = compLine.mapSourceLine(res.cLines, cursorSource);
            lastExp = null;
            int startLine = 0;
            emergencyDocSaved = false;
            emergencyNotepadText = text;
            swDoc.Restart();
            // if processing from middle of document is possible, get last saved state
            if (!cfg.disablePartialParse)
            {
                int validOldLines = 0;
                if ((cursorCLin > 0) && (stored_Mid != null) && (stored_Mid.currentTotal != null))
                {
                    int maxLin = Math.Min(stored_Mid.currentTotal.cLines.Length, res.cLines.Length); // check up to smaller num of lines
                    maxLin = Math.Min(maxLin, cursorCLin); // also check only up to cursor 
                    // check number of unchanged lines, up to maxLin
                    int unchangedLin = 0;
                    while ((unchangedLin < maxLin) && (res.cLines[unchangedLin].cLineText == stored_Mid.currentTotal.cLines[unchangedLin].cLineText))
                        unchangedLin++;
                    // if it is larger than last redefinition, it is allowed
                    if (unchangedLin > stored_Mid.currentTotal.lastDefLine)
                        validOldLines = unchangedLin;
                }
                if (validOldLines > 0)
                {
                    // copy previous calculation state
                    mc.LoadState(stored_Mid);
                    // copy previous results, only validOldLines  lines
                    mcTotalResult.copyParsedLines(stored_Mid.currentTotal, res, validOldLines);
                    res.lastDefLine = stored_Mid.currentTotal.lastDefLine;
                    // recreate lastResult from new parsedlines in res, it was already shortened
                    lastResult = getLastResult(res.parsedLines);
                    // skip lines already in that state
                    startLine = validOldLines;
                }
                else
                    // copy preset state, without middle stored state
                    mc.Reset();
                // invalidate old stored state
                stored_Mid = null;
            }
            else
                mc.Reset();
            // show debug state
            resetDbg();
            currentDbgLine = startLine;
            dbgShow(0,"start state>");
            bool noTimeout = true; // will be set after first doc timeout
            int srcLine = 0;
            for (int i = 0; i < startLine; i++) srcLine += res.cLines[i].sNumLines;
            // now process every line
            for (int l = startLine; l < res.cLines.Length; l++)
            {
                currentDbgLine = l;
                var thisLine = res.cLines[l].cLineText;
                if (noTimeout)
                {
                    // if this is cursor line, save state until now (above it)
                    if ((cursorCLin > 0) && (l == cursorCLin))
                    {
                        var copyRes = res.Clone(cursorCLin);
                        stored_Mid = StoreState(copyRes);
                    }
                }
                // save this line in results even if doc timeout
                CalcResult eRes;
                if (noTimeout)
                {
                    // now calculate this line
                    eRes = mc.Evaluate(thisLine, srcLine);
                    // modify result text if error
                    if (eRes.isError)
                    {
                        if (eRes.Text.SubStr(0, 4) == "DSP:")
                        {
                            // not an error, but some display text
                            eRes.isError = false;
                            eRes.isValue = false;
                            eRes.Text = eRes.Text.Remove(0, 4);
                            eRes.color = Color.Green;
                        }
                        else
                        {
                            res.errnum++;
                            // for unintended or long errors, display just "Error", and rest in top box
                            if (eRes.Text.SubStr(0, 4) != "ERR:")
                            {
                                eRes.errorText = eRes.Text;
                                eRes.Text = "Error" + (res.errnum > 1 ? " #" + res.errnum : "");
                                if (res.errorTxt == "") // just first error in upper box
                                { 
                                    res.errorTxt = eRes.Text + ": " + eRes.errorText;
                                    res.errorLineNum = l;
                                }
                            }
                            else
                            {
                                eRes.Text = eRes.Text.SubStr(4); // for short known errors like "ERR:Timeout"
                                eRes.errorText = eRes.Text;
                            }
                            eRes.color = res.errnum > 1 ? Color.Maroon : Color.Red;
                        }
                    }
                    // remember if there was any redefinition in this line, because optimization can not go above this
                    if (eRes.hasRedefinition)
                        res.lastDefLine = l;
                }
                else
                    eRes = new CalcResult();
                // create highlight and history lists even if timeout
                var pLine = new ParseLine();
                pLine.res = eRes;
                // add to list
                res.parsedLines.Add(pLine);
                srcLine += res.cLines[l].sNumLines;
                // check doc timeout only first time
                if (noTimeout && isDocTimeout())
                {
                    res.errorTxt = "Document calculation Timeout ,  over " + cfg.timeoutDocMs + " ms !";
                    noTimeout = false;
                    res.errnum++;
                }
                // debug info
                dbgShow(0, "End CalcLine");
            }
            swDoc.Stop();
            // combined result data
            res.debugLines = totalDbgText;
            lastTotalResult = res;
            return res;
        }



        //  *** methods to allow  Dynamic Casing ( or case insensitive or case sensitive )

        // add new cased name, and keep connection between cased and uncased names
        public static void addCased(string name, Dictionary<string, List<string>> cdict)
        {
            var loName = name.ToLower();
            if (cdict.ContainsKey(loName))
            {
                var nameList = cdict[loName];
                nameList.Add(name);
            }
            else
            {
                var newList = new List<string>();
                newList.Add(name);
                cdict.Add(loName, newList);
            }
        }

        // remove cased name 
        public static void removeCased(string name, Dictionary<string, List<string>> cdict)
        {
            // first remove name from uncased list
            var loName = name.ToLower();
            if (cdict.ContainsKey(loName))
            {
                var nameList = cdict[loName];
                if (nameList.Contains(name))
                {
                    nameList.Remove(name);
                    if (nameList.Count == 0)
                        cdict.Remove(loName);
                }
            }
        }

        // check and return valid name, based on current case sensitivity, or empty if name not valid
        public static string uncaseName(string name, Dictionary<string, List<string>> cdict)
        {
            if (name != "")
            {
                var loName = name.ToLower();
                if (cdict.ContainsKey(loName))
                {
                    var nameList = cdict[loName];
                    if (nameList.Contains(name)) // if exact match exists, return true
                        return name;
                    if (cfg.sensitivity == mcCaseSensitivity.Sensitive)
                    {
                        // since exact name match did not exist, skip to return false
                    }
                    else
                    {
                        // for both insensitive and dynamic cases
                        // if no exact match, match only if unambiguous :  only one different casing (but same name) exist
                        if (nameList.Count == 1)
                            return nameList[0];
                    }
                }
            }
            // if name match not found return empty == false
            return "";
        }

        // is this allowed new case name
        public static bool allowedNewCaseName(string name, Dictionary<string, List<string>> cdict, Func<string, bool> isBuiltin = null)
        {
            if (name == "") return false;
            var loName = name.ToLower();
            if (cdict.ContainsKey(loName))
            {
                var nameList = cdict[loName];
                // if that exact name already exists, that is not allowed under any casing scheme
                if (nameList.Contains(name))
                    return false;
                if (cfg.sensitivity == mcCaseSensitivity.Insensitive)
                {
                    return false; // only one name version can exist when insensitive
                }
                else
                if (cfg.sensitivity == mcCaseSensitivity.Sensitive)
                {
                    return true; // allow since that exact name does not exists (already checked at start)
                }
                else
                // dynamic sensitivity naming
                {
                    if (nameList.Count != 1) return true; // allow since multiple names already allowed for this one
                    // otherwise check if this is inbuilt function, for which different cases are not allowed
                    if ((isBuiltin != null) && isBuiltin(nameList[0]))
                        return false;
                    // if previous cases was not builtin, allow this one too
                    return true;
                }
            }
            else
                return true; // no case variation of this name exists
        }



        // *** CASED versions for <functions>

        // add new function, and keep connection between cased and uncased names
        public static bool addFunc(string name, mcFuncParse value, bool setBuiltIn=true)
        {
            if (functions.ContainsKey(name)) return false;
            functions.Add(name, value);
            if (value != null)
            {
                if (value.Name == "") value.Name = name;
                if (value.funcCalc != null)
                {
                    if (value.funcCalc.Name == "") value.funcCalc.Name = name;
                    if (setBuiltIn) value.funcCalc.flags|=mcFuncFlags.BuiltIn;
                    value.funcCalc.flags |= mcFuncFlags.TopNamed;
                }
                if (!setBuiltIn) value.isUserFunction = true; // parse is by default System, not user, while mcFunc is by default user, not System
            }
            addCased(name, functionsCased);
            return true;
        }
        // add new built in vectorized function
        public static bool addFuncVec2(string name, Func<Number, Number, Number> op2func, string Description="", string FormatExample="", int priority=90, mcFuncParamType paramType= mcFuncParamType.Func, bool OnlyLeftVector = false, bool OnlySingleVector = false, bool OnlyBothVectors = false, bool AllowLeftNULL = true, bool AllowRightNULL = true)
        {
            var mf = new mcFunc(args => mcValue.vec2(args, op2func, OnlyLeftVector: OnlyLeftVector, OnlySingleVector: OnlySingleVector, OnlyBothVectors: OnlyBothVectors, AllowLeftNULL: AllowLeftNULL, AllowRightNULL: AllowRightNULL));
            return addFunc(name, new mcFuncParse(name, paramType, AllowRightNULL ? 1 : 2, priority, mf, Description, FormatExample) );
        }
        public static bool addOpVec2(string name, int priority, Func<Number, Number, Number> op2func, string Description = "", string FormatExample = "", mcFuncParamType paramType = mcFuncParamType.Between, bool OnlyLeftVector = false, bool OnlySingleVector = false, bool OnlyBothVectors = false, bool AllowLeftNULL = true, bool AllowRightNULL = true)
        {
            var mf = new mcFunc(args => mcValue.vec2(args, op2func, OnlyLeftVector: OnlyLeftVector, OnlySingleVector: OnlySingleVector, OnlyBothVectors: OnlyBothVectors, AllowLeftNULL: AllowLeftNULL, AllowRightNULL: AllowRightNULL));
            return addFunc(name, new mcFuncParse(name, paramType, AllowRightNULL ? 1 : 2, priority, mf, Description, FormatExample));
        }
        public static bool addFuncVec2(string name, Func<mcValue, mcValue, mcValue> op2func, string Description = "", string FormatExample = "", int priority=90, mcFuncParamType paramType = mcFuncParamType.Func, bool OnlyLeftVector = false, bool OnlySingleVector = false, bool OnlyBothVectors = false, bool AllowLeftNULL = true, bool AllowRightNULL = true)
        {
            var mf = new mcFunc(args => mcValue.vec2(args, op2func, OnlyLeftVector: OnlyLeftVector, OnlySingleVector: OnlySingleVector, OnlyBothVectors: OnlyBothVectors, AllowLeftNULL: AllowLeftNULL, AllowRightNULL: AllowRightNULL));
            return addFunc(name, new mcFuncParse(name, paramType, AllowRightNULL ? 1 : 2, priority, mf, Description, FormatExample) );
        }
        public static bool addFuncVec1(string name, Func<Number, Number> op1func, string Description = "", string FormatExample = "", int priority=90, mcFuncParamType paramType = mcFuncParamType.Func, bool AllowNULL = true)
        {
            var mf = new mcFunc(args => mcValue.vec1(args[0], op1func, AllowNULL: AllowNULL));
            return addFunc(name, new mcFuncParse(name, paramType, 1, priority, mf, Description, FormatExample) );
        }
        public static bool addOpVec1(string name, int priority, Func<Number, Number> op1func, string Description = "", string FormatExample = "", mcFuncParamType paramType = mcFuncParamType.Between, bool AllowNULL = true)
        {
            var mf = new mcFunc(args => mcValue.vec1(args[0], op1func, AllowNULL: AllowNULL));
            return addFunc(name, new mcFuncParse(name, paramType, 1, priority, mf, Description, FormatExample));
        }


        // remove existing function, and update functionsCased
        public static void removeFunc(string name)
        {
            removeCased(name, functionsCased);
            if (functions.ContainsKey(name))
                functions.Remove(name);
        }
        // return name of valid function, based on current case sensitivity, or empty if name not valid
        public static string uncaseFuncName(string name)
        {
            return uncaseName(name, functionsCased);
        }
        // is this allowed new function name
        public static bool allowedNewName(string name)
        {
            return allowedNewCaseName(name, functionsCased, n => !functions[n].isUserFunction);
        }
        // return true if function with this name exists
        public static bool hasFunc(string name)
        {
            return uncaseFuncName(name) != "";
        }
        // return function with this name if exists, otherwise null
        public static mcFuncParse getFunc(string name, List<int> callingBlock=null)
        {
            var realName = uncaseFuncName(name);
            if (realName == "") return null;
            var res= functions[realName];
            // check if valid block
            if ((callingBlock != null)&&(res.block!=null))
            {
                if (res.block.Length > callingBlock.Count)
                    return null; // calling block must be deeper than called block
                for (int i = 0; i < res.block.Length; i++)
                    if (res.block[i] != callingBlock[i])
                        return null; // they must be in same block hierarchy
            }
            return res;
        }
        // mark all functions as system functions, used after preset to make even user supplied preset functions as inbuilt
        public static void MarkAllFuncAsSystem()
        {
            if (functions == null) return;
            foreach(var fn in functions)
            {
                fn.Value.isUserFunction = false;
                if (fn.Value.funcCalc != null)
                    fn.Value.funcCalc.flags|=mcFuncFlags.BuiltIn;
            }
        }




        // *** CASED versions for <units>

        // add new unit
        public static void addUnit(string name, Number value)
        {
            units.Add(name, value);
            addCased(name, unitsCased);
        }
        // return name of valid function, based on current case sensitivity, or empty if name not valid
        public static string uncaseUnitName(string name)
        {
            return uncaseName(name, unitsCased);
        }
        // return true if unit with this name exists
        public static bool hasUnit(string name)
        {
            return uncaseUnitName(name) != "";
        }
        // return value of unit with this name if exists, otherwise -1
        public static Number getUnit(string name)
        {
            var realName = uncaseUnitName(name);
            return realName == "" ? -1 : units[realName];
        }
        // is this allowed new unit name
        public static bool allowedNewUnit(string name)
        {
            return allowedNewCaseName(name, unitsCased, null);
        }


        // element at given index of vector (if two arguments), or subvector of arg[0] from index arg[1][0] to index arg[1][1], inclusive
        public static mcValue index(mcValue vec, mcValue indices)
        {
            // if empty mcValue, return empty vector instead of exception
            if ((indices == null) || (indices.vectorLength < 1) || (vec == null) || (vec.vectorLength <= 0))
                return new mcValue(new List<mcValue>());
            // if single element at given index
            if (indices.vectorLength == 1)
            {
                int idx = indices.ElementAt(0).Int;
                if ((idx < 0) || (idx >= vec.vectorLength))
                    return new mcValue(new List<mcValue>()); // or throw exception?
                return vec.ElementAt(idx);
            }
            else
            {
                // check index range ( [idx0 .. idx1] ). Allow null indices for 0 or max ( [5..] or [..3] )
                int idx0 = indices.ElementAt(0)!= null ? indices.ElementAt(0).Int : 0;
                int idx1 = indices.ElementAt(1)!= null ? indices.ElementAt(1).Int : vec.vectorLength - 1;
                if (idx0 < 0) idx0 = 0;
                if (idx1 >= vec.vectorLength) idx1 = vec.vectorLength - 1;
                // fill sub vector
                var res = new List<mcValue>();
                for (int i = idx0; i <= idx1; i++)
                    res.Add(vec.ElementAt(i));
                return new mcValue(res);
            }
        }


        // fills tooltip name, title and description based on parseFunction, and set valid=true if it should be shown
        public static ToolTipClass getToolTips(mcFuncParse func)
        {
            var res = new ToolTipClass();
            res.Name = func.Name;
            res.Title = func.formatExample != "" ? func.formatExample : func.Name;
            res.Text = func.description;
            res.valid = (func.Name != "") && (func.description != "N/A") && ((char.IsLetter(func.Name[0]) || (func.description != "")));  //(!f.isUserFunction)
            return res;
        }
        // find function OR variable with given name, then fills tooltip and set valid if it should be shown
        public static ToolTipClass getToolTips(string funcName)
        {
            // first search functions
            var func = getFunc(funcName);
            if (func != null)
                return getToolTips(func);
            // then global variables
            var res = new ToolTipClass();
            var gValue = mc.varGlobalValue(funcName);
            if (gValue != "")
            {
                res.Name = funcName;
                res.Title = funcName + " - global variable";
                res.Text = "Current global value = " + gValue;
                res.valid = true;
            }
            return res;
        }

        // basic save file function
        public static string csMarker = "\r\n//#C#\r\n";
        // merge  notepad and c# part and save into file 
        public static bool SaveFileBasic(string fileName, string notepadText, string CStext)
        {
            try
            {
                if (CStext.TrimEnd(new char[] { '\r', '\n', ' ' }) != "")
                    notepadText += csMarker + CStext;
                File.WriteAllText(fileName, notepadText);
                return true;
            }
            catch
            {
            }
            return false;
        }

        #region Variable handling
        // *** VARIABLES

        // find variable blocks based on its name
        // -returns null if name not found, or even if name exists but above that particular block
        static public List<int> findVariable(string name, List<int> blockIdx)
        {
            List<List<int>> foundBlocks;
            if (varNames.TryGetValue(name, out foundBlocks))
            {
                // var block must completely match start of blockIdx 
                bool matches(List<int> blk)
                {
                    if (blk.Count > blockIdx.Count) return false;
                    for (int i = 0; i < blk.Count; i++)
                        if (blk[i] != blockIdx[i])
                            return false;
                    return true;
                }
                // now check whick found block is best match
                int bestDepth = 0, bestMatch = -1;
                for (int i=0; i<foundBlocks.Count; i++)
                {
                    if (matches(foundBlocks[i])){
                        var matchDepth = foundBlocks[i].Count;
                        if (matchDepth > bestDepth)
                        {
                            bestDepth = matchDepth;
                            bestMatch = i;
                        }
                    }
                }
                // return best match, or null if none
                if (bestMatch >= 0)
                    return foundBlocks[bestMatch];
                else
                    return null;
            }
            else
                return null;
        }
        // return full variable name instead of blocks, or "" if not found
        static public string findVariableName(string name, List<int> blockIdx)
        {
            var block = findVariable(name, blockIdx);
            if (block != null)
                return fullVarName(name, block);
            else
                return "";
        }


        // adds variable+block to list of existing ones
        static public void addVariable(string name, List<int> varBlockIdx, mcFunc parent)
        {
            // make clone of input blockIdx, in case it will be changed later
            var blockIdx = new List<int>(varBlockIdx);
            // find blocks list, or create new one if this is first time this name is seen
            List<List<int>> foundBlocks;
            if (!varNames.TryGetValue(name, out foundBlocks))
            {
                foundBlocks = new List<List<int>>();
                varNames.Add(name, foundBlocks);
            }
            // append new block to list (it will update directly item held in dictionary)
            foundBlocks.Add(blockIdx); // will throw exception here if that exact name/blocks already exists!
            // append new parent for full variable name
            varParents.Add(fullVarName(name, varBlockIdx), parent);
        }


        // get full variable name, from its name and block position:  x[0,3,5]
        // used as key in varValues dictionary
        static public string fullVarName(string name, List<int> blockIdx)
        {
            string inside = "";
            if (blockIdx != null)
                for (int i = 0; i < blockIdx.Count; i++)
                    inside += (inside != "" ? "," : "") + blockIdx[i];
            return name + "[" + inside + "]";
        }

        // find stack for this fullname variable, create if new, and clean before return
        // stack tuple: Item1= stackLevel, Item2=value
        static Stack<Tuple<int, mcValue>> getCleanStack(string varFullName, int stackLevel, bool removeSameLevel)
        {
            // find stack for this full name variable, or create one if its first time
            Stack<Tuple<int, mcValue>> theStack;
            if (!varValues.TryGetValue(varFullName, out theStack))
            {
                theStack = new Stack<Tuple<int, mcValue>>();
                varValues.Add(varFullName, theStack);
            }
            // cleanup stack - if previous function calls were at higher level
            while ((theStack.Count > 0) && (theStack.Peek().Item1 > stackLevel))
                theStack.Pop();
            // or if even same level need removal (for assignments)
            if (removeSameLevel&& (theStack.Count > 0) && (theStack.Peek().Item1 == stackLevel))
                theStack.Pop();
            //return cleaned stack
            return theStack;
        }

        // assign value to fully named variable 
        public static mcValue doVarAssign(string varFullName, mcValue newValue, mcFunc parentFunc, mcValue idxValue)
        {
            if (newValue==null)
                throw new ArgumentException("Variable assignment invalid (empty) !");
            // evaluate expValue not needed when passed via factors
            // var newValue = expValue.Evaluate();
            // find stack corresponding to this full name and parent func stack level
            int stackLevel = (parentFunc != null) ? parentFunc.stackCount : 0;
            // if this is index change, it needs to get old value first
            if (idxValue != null)
            {
                // index must be integer
                if (!idxValue.isInt()) throw new ArgumentException("Index of " + varFullName+ " must be integer");
                // read old value
                var theStack = getCleanStack(varFullName, stackLevel, false);
                if (theStack.Count == 0)  throw new ArgumentException("Undefined variable " + varFullName);
                var topValue = theStack.Peek();
                if (topValue == null)   throw new ArgumentException("Undefined or invalid variable " + varFullName);
                var oldValue= topValue.Item2;
                if (oldValue == null) throw new ArgumentException("Undefined indexed variable " + varFullName);
                // now change that old variable 'in situ'  ()
                oldValue.setVectorIndex(idxValue.Int, newValue);
                // SLOWER alternative was pop it out, then push entirely new value in
                //theStack = getCleanStack(varFullName, stackLevel, true); // to pop old vlaue out
                //theStack.Push(new Tuple<int, mcValue>(stackLevel, newValue)); // to push entirely new value
            }
            else
            {
                // otherwise this is entire new value, and can remove old one
                var theStack = getCleanStack(varFullName, stackLevel, true);
                // push new value (old was removed if existed )
                theStack.Push(new Tuple<int, mcValue>(stackLevel, newValue));
            }
            // return calculated new value
            return newValue;
        }
        // assign value to fully named variable , version where args are used
        // this version allow deep copy to change mcFunc pointers of valExp & parent
        // args[0]==  valExp as mcValue (factor[0].calc,   args[1]==ctx.parentFunc
        public static mcValue doVarAssign(string varFullName, mcValue[] args)
        {
            var expValue = args[0];
            var parentFunc = args[1]?.getFunc()?.getSubFunc();
            var idxValue = args.Length > 2 ? args[2] : null;
            return doVarAssign(varFullName, expValue, parentFunc, idxValue);
        }


        // retrieve 'value' of fully named variable 
        public static mcValue doVarGet(string varFullName, mcFunc parentFunc)
        {
            // find stack corresponding to this full name and parent func stack level
            int stackLevel = (parentFunc != null) ? parentFunc.stackCount : 0;
            var theStack = getCleanStack(varFullName, stackLevel, false);
            if (theStack.Count==0)
                throw new ArgumentException("Undefined variable " + varFullName);
            // return topmost value
            var topValue = theStack.Peek();
            if (topValue==null)
                throw new ArgumentException("Undefined or invalid variable "+varFullName);
            // return value from stack
            return topValue.Item2;
        }
        // retrieve 'value' of fully named variable 
        // this version allow deep copy to change mcFunc pointers of parent
        // args[0]==ctx.parentFunc
        public static mcValue doVarGet(string varFullName, mcValue[] args)
        {
            var parentFunc = args[0]?.getFunc()?.getSubFunc();
            return doVarGet(varFullName, parentFunc);
        }

        // get variable stats 
        // result: Tuple < bool itExists, int minDepth, int maxDepth, int numDiff, mcValue globalValue >
        static Tuple<bool,int,int,int,mcValue> getVarStats(string name)
        {
            bool itExists = false;
            int minDepth = -1, maxDepth = -1, numDiff = 0;
            mcValue globalValue = null;
            Tuple<bool, int, int, int, mcValue> retValue()
            {
                return new Tuple<bool, int, int, int, mcValue>(itExists, minDepth, maxDepth, numDiff, globalValue);
            }
            List<List<int>> foundBlocks;
            if ((varNames!=null)&& varNames.TryGetValue(name, out foundBlocks))
            {
                numDiff = foundBlocks.Count;
                itExists = numDiff > 0;
                foreach(var vn in foundBlocks)
                {
                    if (vn.Count > maxDepth) maxDepth = vn.Count;
                    if (vn.Count < minDepth) minDepth = vn.Count;
                    if (vn.Count == 1)
                    {
                        // find value this global variable
                        if (varValues != null) {
                            Stack<Tuple<int, mcValue>> theStack;
                            if (varValues.TryGetValue( fullVarName(name,vn) , out theStack)&&(theStack.Count>0))
                            {
                                globalValue = theStack.ToArray()[0].Item2;
                            }
                        }
                    }
                }
            }
            return retValue();
        }

        /// <summary>
        /// Find if variable name is global and return its global value
        /// Returns empty if variable is not global or does not exist
        /// </summary>
        public static string varGlobalValue(string name)
        {
            var vstat = getVarStats(name);
            if (vstat.Item1 && (vstat.Item5 != null))
                return vstat.Item5.ToString("",-1,true);
            else
                return "";
        }

        /// <summary>
        /// Returns true if variable with given indices is global
        /// </summary>
        public static bool isGlobalVar(List<List<int>> indices)
        {
            if (indices!=null)
            foreach (var idx in indices)
                if (idx.Count == 1)
                    return true;
            return false;
        }



        #endregion

        // *** DEBUG functions

        static public bool showDbgValues = false;
        static int currentDbgLine = 0, numDbgShown = 0;
        static string lastDbgText = "";
        static string totalDbgText = "";
        static List<string> debugVars;
        static Dictionary<string, string> oldDbgVarValues;
        static bool dbgShowAllVars = false;

        static void resetDbg()
        {
            numDbgShown = 0;
            lastDbgText = "";
            totalDbgText = "";
            oldDbgVarValues = new Dictionary<string, string>();
            debugVars = new List<string>();
            showDbgValues = false;
            if (cfg.DebugVars != null)
            {
                if ((cfg.DebugVars.Trim() == "*")|| (cfg.DebugVars.Trim() == "\"*\""))
                {
                    dbgShowAllVars = true;
                    showDbgValues = true;
                }
                else
                {
                    // specifically listed names
                    dbgShowAllVars = false;
                    var dv = cfg.DebugVars.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var dvs in dv)
                        debugVars.Add(dvs.Trim());
                    showDbgValues = debugVars.Count > 0;
                }
            }
            if (showDbgValues)
                addDbgLine("Debug: variable values after specified events:  <stackDepth> name[blockDepth]= value");
        }


        // add variable value for full variable name 
        static void dbgAddVarValue(string vFullName, int stackDepth, ref string res)
        {
            // find stack of values for that full name
            Stack<Tuple<int, mcValue>> theStack;
            if (varValues.TryGetValue(vFullName, out theStack))
            {
                // convert it to array, since we need to potentially look deeper, without disturbing stack
                var aStack = theStack.ToArray();
                // find first value that is at same or lower stack depth
                int i = -1;
                while ((i + 1 < aStack.Length) && (aStack[i + 1].Item1 <= stackDepth)) i++;
                // if any found, add to string result
                if (i >= 0)
                {
                    var value = aStack[i].Item2 == null ? "null" : aStack[i].Item2.ToString();
                    // find if old value exists
                    string oldValue = "";
                    oldDbgVarValues.TryGetValue(vFullName, out oldValue);
                    if (value != oldValue)
                    {
                        oldDbgVarValues[vFullName] = value;
                        res += (res != "" ? ", " : "") + vFullName + "=" + value;
                    }
                }
            }

        }

        // find all variables with given short name, and add their values (based on given stack depth) to ref string res
        // but only add to result if value is different than last reported 
        static void dbgAddNameValues( string name, int stackDepth, ref string res)
        {
                // find variables with specific names
                List<List<int>> varBlocks;
                if (varNames.TryGetValue(name, out varBlocks))
                {
                    // for each different block depth, show variable name (in future, maybe pass current block depth as additional parameter - need to store it in mcFunc on create)
                    foreach (var bd in varBlocks)
                    {
                        var vFullName = fullVarName(name, bd);
                        dbgAddVarValue(vFullName, stackDepth, ref res);
                    }
                }
        }

        // list current values of all variables specified in debug options,  in one line
        public static string dbgVariables(int stackDepth)
        {
            var res = "";
            if (dbgShowAllVars)
            {
                foreach (var vn in varValues)
                    dbgAddVarValue(vn.Key, stackDepth, ref res);
            }
            else
            { 
                foreach (var vn in debugVars)
                    dbgAddNameValues(vn, stackDepth, ref res);
            }
            return res;
        }
        // add debug line to output and totalDbgText
        static void addDbgLine(string dbgLine)
        {
            // add to totalDbgText, which will be displayed in CSerror panel
            totalDbgText += dbgLine + Environment.NewLine;
            // and also show to console output
            Console.WriteLine("Dbg# [" + currentDbgLine + "] " + dbgLine);
            numDbgShown++;
            if (numDbgShown == 100)
                Console.WriteLine("Dbg  ... output limit reached (100 lines ) ...");
        }
        // debug function that list current values of all variables specified in debug options,  if any value changed
        public static void dbgShow(int stackDepth, string addText="", bool showIfNoChange=false)
        {
            if (showDbgValues && (numDbgShown<100))
            {
                var varValues= mc.dbgVariables(stackDepth);
                //if ((varValues != "") || showIfNoChange)
                {
                    var newtxt = "  " + addText + " <" + stackDepth + "> " + varValues;
                    if (newtxt != lastDbgText)
                    {
                        lastDbgText = newtxt;
                        addDbgLine(newtxt);
                    }
                }
            }
        }

    }
}
