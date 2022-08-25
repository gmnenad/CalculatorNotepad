using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using Numbers;

namespace CalculatorNotepad
{

    // configuration
    public enum mcCaseSensitivity { Insensitive, Sensitive, Dynamic };
    public class mcConfig
    {
        // user definable parameters
        public string PresetFile { get; set; }
        public bool openLastOnStart { get; set; }
        public mcCaseSensitivity sensitivity { get; set; }
        public bool allowFuncRedefinition { get; set; }
        public bool allowBuiltInRedefinition { get; set; }
        public bool autoLastResultAppend { get; set; }
        public long timeoutFuncMs { get; set; }
        public long timeoutDocMs { get; set; }
        public bool timeoutDisabled { get; set; }
        public bool isBinHexExponentNative { get; set; }
        public bool displayBinHexFloat { get; set; }
        public bool replaceKnownSymbols { get; set; }
        public mcOptimization disabledOptimizations;
        public bool showExecutionTime { get; set; }
        public int  autocompleteChars { get; set; }
        public string DebugVars { get; set; }
        public int resFormatDecimals { get; set; }
        public string resFormatSeparator { get; set; }
        public string resFractionSeparator { get; set; }
        public bool autoFocusError { get; set; }
        public NumberClass numberType { get; set; }
        public int numberPrecision { get; set; }

        // private states
        public Rectangle lastWinPosition;
        public FormWindowState lastWinState;
        public int  lastHistWidth, lastEditWidth, lastCSheight;
        public bool CSvisible, CSerrorVisible;
        public string LastFileName, LastFileDirectory;
        public bool notProperlyClosed;

        static private string defPath(string path)
        {
            if (path != "") return path;
            return AppDomain.CurrentDomain.BaseDirectory + "\\calc.config";
        }
        public void Save(string path = "")
        {
            TextWriter writer = null;
            try
            {
                var serializer = new XmlSerializer(typeof(mcConfig));
                writer = new StreamWriter(defPath(path), false);
                serializer.Serialize(writer, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error while saving config file");
            }
            finally
            {
                if (writer != null) writer.Close();
            }
        }
        static public mcConfig Load(string path = "")
        {
            TextReader reader = null;
            try
            {
                var serializer = new XmlSerializer(typeof(mcConfig));
                reader = new StreamReader(defPath(path));
                var newConfig = (mcConfig)serializer.Deserialize(reader);
                return newConfig;
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }
        public mcConfig makeClone()
        {
            return (mcConfig)MemberwiseClone();
        }

        // properties that convert some values for more convenient use
        public bool disablePartialParse { get { return (disabledOptimizations & mcOptimization.CalcBelowChange) != 0; } }
        public bool disableConstantOptimization { get { return (disabledOptimizations & mcOptimization.ConstantFunctions) != 0; } }
        public bool disableCacheOptimization { get {
                return (disabledOptimizations & mcOptimization.CacheFunctions) != 0;
            } }

    }

    // class for results of calculations
    public class CalcResult
    {
        public mcValue Value;
        public string Text;
        public bool isValue;
        public bool isError;
        public long time_ms;
        public Color color;
        public string errorText;
        public mcExpExtendedType expType;
        public bool isConstant;
        public bool hasRedefinition;
        public Point errPos, errLastPos;
        public CalcResult()
        {
            isError = false;
            isValue = false;
            Text = "";
            errorText = "";
            Value = new mcValue();
            time_ms = 0;
            color = Color.Black;
            expType = mcExpExtendedType.Other;
            isConstant = false;
        }

        public override string ToString()
        {
            if (isError) return "Error";
            if (!isValue) return Text;
            return ToString(Value);
        }

        public static string ToString(mcValue Value)
        {
            if ( (Value.valueType == mcValueType.Number) && (Value.Number < 0) )
                return "~(" + Value.ToString() + ")"; // put negative values in parentheses
            else if (Value.valueType == mcValueType.Vector)
                return "~" + Value.ToString("", 2); // shorten to two decimals for vectors
            // otherwise ~ in front of just default ToString
            return "~" + Value.ToString();
        }

        public CalcResult ShallowCopy()
        {
            return (CalcResult)MemberwiseClone();
        }

        public int calcHashCode()
        {
            int result = 17;
            void addHash(int hash)  {  unchecked { result = result * 23 + hash; }  }
            if (Value != null) addHash(Value.GetHashCode());
            if (Text != null) addHash(Text.GetHashCode());
            addHash(isError.GetHashCode());
            addHash(color.GetHashCode());
            addHash(expType.GetHashCode());
            return result;
        }


    }


    // class holding one compile line, with linkt to original text lines
    public class compLine
    {
        public string cLineText;   // compile line text ( can contain multiple lines eparated by \n )
        public int sLine;       // line number in source text
        public int sNumLines;   // number of lines from source text folded into this one line
        // construct from one multiline string and original source line position
        public compLine(string sourceLineText, int sourceLine)
        {
            cLineText = sourceLineText;
            sLine = sourceLine;
            sNumLines = countLines(sourceLineText);
        }
        // tostring
        public override string ToString()
        {
            return cLineText;
        }
        // create array of compLines from array of strings
        public static compLine[] makeArray(string[] lines)
        {
            var res = new compLine[lines.Length];
            int srcLine = 0;
            for(int i=0; i<lines.Length; i++)
            {
                res[i] = new compLine(lines[i], srcLine);
                srcLine += res[i].sNumLines;
            }
            return res;
        }

        // count number of lines in one multiline
        public static int countLines(string multiline)
        {
            int res = 1;
            foreach (var ch in multiline)
                if (ch == '\n')
                    res++;
            return res;
        }
        // count number of lines in multiline array
        public static int countLinesArray(compLine[] lines)
        {
            int res = 0;
            foreach (var ln in lines)
                res += ln.sNumLines;
            return res;
        }
        /// <summary>
        /// Return compLine array index corresponding to source line number, or -1 if not possible
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="sourceLineNumber"></param>
        /// <returns></returns>
        public static int mapSourceLine(compLine[] lines, int sourceLineNumber)
        {
            if (sourceLineNumber < 0) return -1;
            int srcSum = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                srcSum += lines[i].sNumLines;
                if (sourceLineNumber < srcSum)
                    return i;
            }
            // if not found (source line larger than all lines in array), return -1
            return -1;
        }
    }

    // class that returns result of entire notepad calculation
    public class mcTotalResult
    {
        // output parameters
        public compLine[] cLines;
        public List<ParseLine> parsedLines;
        public int errnum;
        public string errorTxt;
        public int errorLineNum;
        public int lastDefLine;
        public string debugLines;
        public int sourceHash;

        // make clone of total result, up to specified compile line number
        public mcTotalResult Clone(int numLines = -1)
        {
            var res = new mcTotalResult();
            res.errnum = errnum;
            res.errorTxt = errorTxt;
            res.errorLineNum = errorLineNum;
            res.lastDefLine = lastDefLine;
            res.sourceHash = sourceHash;
            // partial copy of cLines
            if (cLines != null) {
                int nLin = (numLines < 0) || (numLines > cLines.Length) ? cLines.Length : numLines;
                res.cLines = new compLine[nLin];
                for (int i = 0; i < nLin; i++)
                    res.cLines[i] = cLines[i];
            }
            // partial copy of parsed lines
            copyParsedLines(this, res, numLines);
            return res;
        }

        public static void copyParsedLines(mcTotalResult source, mcTotalResult dest, int numLines = -1)
        {
            if (source.parsedLines != null)
            {
                int maxLin = (numLines < 0) || (numLines > source.parsedLines.Count) ? source.parsedLines.Count  : numLines;
                dest.parsedLines = new List<ParseLine>(maxLin);
                for (int i = 0; i < maxLin; i++)
                    dest.parsedLines.Add(source.parsedLines[i]); // assume those parsed lines are immutable once line is complete - otherwise would need clone of those !
            }
            else
                dest.parsedLines = null;
        }
    }


    // class to store func name and arguments as Key for mcFunc cache results
    public struct mcFuncCacheKey
    {
        public mcFunc func;
        public List<mcValue[]> args;
        public mcFuncCacheKey(mcFunc Func, List<mcValue[]> Args)
        {
            func = Func;
            args = Args;
        }
    }

    public class mcFuncCacheEqualityComparer : IEqualityComparer<mcFuncCacheKey>
    {
        public bool Equals(mcFuncCacheKey x, mcFuncCacheKey y)
        {
                bool cmpOne(mcValue[] xo, mcValue[] yo)
                {
                    int lenX = xo != null ? xo.Length : 0;
                    int lenY = yo != null ? yo.Length : 0;
                    if (lenX != lenY)
                        return false;
                    for (int i = 0; i < lenX; i++)
                    {
                        if (!xo[i].Equals(yo[i]))
                            return false;
                    }
                    return true;
                }
            // test edge cases
            if (x.func != y.func)
                return false;
            if ((x.args == null) && (y.args == null))  return true;
            if ((x.args == null) || (y.args == null))  return false;
            int depth = x.args.Count;
            if (depth != y.args.Count)
                return false;
            // compare each depth level
            for (int i=0; i<depth; i++)
            {
                if (!cmpOne(x.args[i], y.args[i]))
                    return false;
            }
            return true;
        }

        public int GetHashCode(mcFuncCacheKey obj)
        {
            int result = 17;
            void addHash(int hash)
            {
                unchecked { result = result * 23 + hash;  }
            }
            // add hashes of all inner objects
            if (obj.func != null)
                addHash(obj.func.GetHashCode());
            if (obj.args != null)
            {
                addHash(obj.args.Count);
                for (int d = 0; d < obj.args.Count; d++)
                if (obj.args[d]!=null)
                {
                    addHash(obj.args[d].Length);
                    for (int i = 0; i < obj.args[d].Length; i++)
                        addHash(obj.args[d][i].GetHashCode());
                }
            }
            return result;
        }
    }




    // class to store state of currently parsed/compiled functions, units etc
    public class mcState
    {
        public  Dictionary<string, mcFuncParse> functions = null;
        public  Dictionary<string, List<string>> functionsCased;
        public  Dictionary<string, List<List<int>>> varNames = null; 
        public  Dictionary<string, Stack<Tuple<int, mcValue>>> varValues = null;
        public  Dictionary<string, mcFunc> varParents = null;
        public  Dictionary<string, Number> units;
        public  Dictionary<string, List<string>> unitsCased;
        public  CalcResult lastResult;
        public  int defaultNumberBase;
        public  mcTotalResult currentTotal;


        public mcState( Dictionary<string, mcFuncParse> _functions , Dictionary<string, List<string>> _functionsCased,
                        Dictionary<string, List<List<int>>> _varNames, Dictionary<string, Stack<Tuple<int, mcValue>>> _varValues, Dictionary<string, mcFunc> _varParents,
                        Dictionary<string, Number> _units, Dictionary<string, List<string>> _unitsCased, 
                        CalcResult _lastResult, int _defaultNumberBase, mcTotalResult _currentTotal)
        {
            CopyFrom(_functions, _functionsCased, _varNames, _varValues, _varParents, _units,  _unitsCased, _lastResult, _defaultNumberBase, _currentTotal);
        }


        private static void CopyFunctions(Dictionary<string, mcFuncParse> src_functions,   out Dictionary<string, mcFuncParse> dst_functions)
        {
            // clear link between original and deep copy functions. Store not only named functions from dictionary, but also clones of all anonymous subfunctions
            mc.deepCopyLinks = new Dictionary<mcFunc, mcFunc>();
            // this add new deep copy of function to new dictionary, and also store link with old function in deepCopyLinks
            dst_functions = new Dictionary<string, mcFuncParse>();
            // first copy variables, since they can be referenced inside functions (obsolete, variables are now stored separately)
            foreach (var pf in src_functions)
                if (pf.Value.ParamType== mcFuncParamType.Variable)
                  dst_functions.Add(pf.Key, pf.Value.DeepCopy());
            // then copy functions
            foreach ( var pf in src_functions)
                if (pf.Value.ParamType != mcFuncParamType.Variable)
                    dst_functions.Add(pf.Key, pf.Value.DeepCopy());
        }

        // cannot make shallow copy of varNames, since List<blockIdxes> for some variables can be extended in future
        private static void CopyVariableNames(Dictionary<string, List<List<int>>> src_varNames, out Dictionary<string, List<List<int>>> dst_varNames)
        {
            dst_varNames = new Dictionary<string, List<List<int>>>();
            foreach (var vn in src_varNames) {
                // but copy of that List<blockIdxes> can be shallow, since blockIdx itself is not changeable
                var newList = new List<List<int>>(vn.Value);
                dst_varNames.Add(vn.Key, newList);
            }
        }

        // cannot make shallow copy of varValues, since their stacks will be expanded/contracted in runtime (although deepcopy is always done at depth zero, in precompile time)
        private static void CopyVariableValues(Dictionary<string, Stack<Tuple<int, mcValue>>> src_varValues, out Dictionary<string, Stack<Tuple<int, mcValue>>> dst_varValues)
        {
            dst_varValues = new Dictionary<string, Stack<Tuple<int, mcValue>>>();
            foreach (var vn in src_varValues)
            {
                // also stacks can not be shallowcopied, since values inside can reference functions
                var newStack = new Stack<Tuple<int, mcValue>>();
                // this rely on foreach over stack going from oldest to newest
                foreach (var st in vn.Value)
                    newStack.Push(new Tuple<int, mcValue>(st.Item1, st.Item2!=null?st.Item2.DeepCopy():null));
                // insert new stack in varValues
                dst_varValues.Add(vn.Key, newStack);
            }
        }

        // cannot make shallow copy of varparents, since parents are mcFunc, and those were potentially recreated. Must be called AFTER copyFunctions
        private static void CopyVariableParents(Dictionary<string, mcFunc> src_varParents, out Dictionary<string, mcFunc> dst_varParents)
        {
            dst_varParents = new Dictionary<string, mcFunc>();
            foreach (var vn in src_varParents)
            {
                // if old parent was null (topmost variable), just copy
                if (vn.Value == null)
                {
                    dst_varParents.Add(vn.Key, vn.Value);
                }
                else
                {
                    // find new mcFunc corresponding to old one
                    mcFunc thecopy;
                    if (mc.deepCopyLinks.TryGetValue(vn.Value, out thecopy))
                        dst_varParents.Add(vn.Key, thecopy);
                    //else throw new Exception("Unknown mcFunc in CopyVariableParents!"); // if invalid func name used, like "calc(N)={..}", it will get here, so skipped throw
                }
            }
        }




        private static Dictionary<string, List<string>> CopyCased(Dictionary<string, List<string>> src_cased)
        {
            var res = new Dictionary<string, List<string>>();
            foreach (var cs in src_cased)
            {
                var listCopy = new List<string>();
                foreach (var name in cs.Value)
                    listCopy.Add(name);
                res.Add(cs.Key, listCopy);
            }
            return res;
        }


        public void CopyFrom(   Dictionary<string, mcFuncParse> _functions, Dictionary<string, List<string>> _functionsCased,
                                Dictionary<string, List<List<int>>> _varNames, Dictionary<string, Stack<Tuple<int, mcValue>>> _varValues, Dictionary<string, mcFunc> _varParents, 
                                Dictionary<string, Number> _units, Dictionary<string, List<string>> _unitsCased, 
                                CalcResult _lastResult,  int _defaultNumberBase, mcTotalResult _currentTotall)
        {
            // deep copy functions and lambdas
            CopyFunctions(_functions, out functions);
            // deep-ish copy of variables
            CopyVariableNames(_varNames, out varNames);
            CopyVariableValues(_varValues, out varValues);
            CopyVariableParents(_varParents, out varParents);
            // units can be directly copied, since they are shallow
            units = new Dictionary<string, Number>(_units);
            // cased dictionaries also need deep copy due to List<string>
            functionsCased = CopyCased(_functionsCased);
            unitsCased = CopyCased(_unitsCased);
            // lastresult, lineresults (display states) and defaultNumberBase need only shallow copy
            lastResult = _lastResult.ShallowCopy();
            defaultNumberBase = _defaultNumberBase;
            currentTotal = _currentTotall;
        }

        public void CopyTo( out Dictionary<string, mcFuncParse> _functions, out Dictionary<string, List<string>> _functionsCased,
                            out Dictionary<string, List<List<int>>> _varNames, out Dictionary<string, Stack<Tuple<int, mcValue>>> _varValues, out Dictionary<string, mcFunc> _varParents,
                            out Dictionary<string, Number> _units, out Dictionary<string, List<string>> _unitsCased, 
                            out CalcResult _lastResult,  out int _defaultNumberBase)
        {
            // deep copy functions and lambdas
            CopyFunctions(functions, out _functions);
            // deep-ish copy of variables
            CopyVariableNames(varNames, out _varNames);
            CopyVariableValues(varValues, out _varValues);
            CopyVariableParents(varParents, out _varParents);
            // units can be directly copied, since they are shallow
            _units = new Dictionary<string, Number>(units);
            // cased dictionaries also need deep copy due to List<string>
            _functionsCased = CopyCased(functionsCased);
            _unitsCased = CopyCased(unitsCased);
            // lastresult and lineresults (display states) need only shallow copy
            _lastResult = lastResult.ShallowCopy();
            _defaultNumberBase = defaultNumberBase;
        }

    }


    // LineRange contains starting and ending line of a region that was changed (inclusive)
    public class LineRange
    {
        public int startLine;
        public int endLine;
        public int currentLine;
        public LineRange(int start=-1, int end = -1)
        {
            startLine = start;
            endLine = end;
            if (endLine < startLine) endLine = startLine;
            currentLine = endLine;
        }
        public void updateRange(int line)
        {
            currentLine = line;
            if ((line < startLine) || (startLine < 0)) startLine = line;
            if (line > endLine) endLine = line;
        }
    }


    // parse line, contains color line, description of line type (definition, constant...) and result for this line
    public class ParseLine
    {
        public bool isDef;
        public CalcResult res;
    }


    // custom exception type
    public class mcException : Exception
    {
        public mcException()
            : base()
        {
        }
        public mcException(string message, Exception inner)
            : base(message, inner)
        {
        }
        public mcException(string message, Point errPos)
            : base(message)
        {
            mcParse.resetCurrentPosition(errPos);
        }
        public mcException(string message, bool moveToLastExtraction=true)
            : base(message)
        {
            if (moveToLastExtraction)
                mcParse.resetCurrentPosition(mcParse.lastEx());
        }
    }


    // context for mcExp parser
    public class mcContext
    {
        public List<int> blockIdx;    // {0,2,1}:  {0,2} is address of this block, with last element {1} showing how many subblocks were previously created in this block (so inc last and pass to new inner block)
        public int numSubblocks;      // how many subblocks were already used/found within this block. Reset on entering new { block . 
        public mcFunc parentFunc;     // (maybe List<mcFunc>?) point to instance of function to which this subExpression belongs (used to link variable functions to stack depth)
        public int parameterLevel;    // increased for lambdas, to allow lambda using same f(x)= call((x)=>3*x, x+1)  , or even nested lambdas  f(x)= call( (x)=>3*call((x)=>2*x,x+2), x+1)
        public bool thisIsLambda;     // indicate that this expression is body of lambda (so it needs to be wrapped in lambda wrapper )
        public Point startLoc;        // start location in original source text for this expression, used to track error location in case of parse/syntax errors
        // empty initial context
        public mcContext()
        {
            blockIdx = new List<int>() { 0 };
            numSubblocks = 0;
            parentFunc = null;
            parameterLevel = 0;
            thisIsLambda = false;
            startLoc = new Point();
        }
        // create from previous context
        public mcContext(mcContext other)
        {
            blockIdx = new List<int>(other.blockIdx);
            numSubblocks = other.numSubblocks;
            parentFunc = other.parentFunc;
            parameterLevel = other.parameterLevel;
            thisIsLambda = false; // need to be explicitly set!
            startLoc = other.startLoc;
        }
        public mcContext next(Point startLoc )
        {
            var res = new mcContext(this);
            res.startLoc = startLoc;
            return res;
        }

    }


    // class that holds tooltip data ( autocomplete and regular)
    public class ToolTipClass
    {
        public string Name, Title, Text;
        public bool valid;
    }



    // extension methods that are Calculator specific , added to any suitable type
    public static partial class _Other_Extensions
    {

        /// <summary>
        /// Increase X coordinate of point by given value.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Point add(this Point point, int value)
        {
            return new Point(point.X + value, point.Y);
        }




    }




}
