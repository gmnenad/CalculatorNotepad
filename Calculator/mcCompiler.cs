using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.CSharp;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Numbers;

namespace CalculatorNotepad
{
    public class mcCompiler
    {

        public static Type CScode = null;
        public static Object CSinstance = null;
        public static Dictionary<string, csFunc> csFunctions;
        public static Dictionary<string, mcFuncParse> parseFunctions;
        static List<MetadataReference> CompilerReferences = null; // to avoid multiple creations
        static bool changedCompilation = false;
        public static string lastCompiledSource = "";

        static List<MetadataReference> getReferences()
        {
            // skip recreating references if already done 
            if (CompilerReferences != null)
                return CompilerReferences;
            // get all DLLs linked to this app or current domain - may be overkill, but ...
            var refs = AppDomain.CurrentDomain.GetAssemblies();
            var refFiles = refs.Where(a => !a.IsDynamic).Select(a => a.Location);
            // add those that end in above 'common DLLs'
            CompilerReferences = new List<MetadataReference>();
            foreach (var rFile in refFiles)
                CompilerReferences.Add(MetadataReference.CreateFromFile(rFile));
            // add some extras, although they are probably already in above list
            CompilerReferences.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            CompilerReferences.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));
            return CompilerReferences;
        }


        static CSharpCompilation? doCompile(string code)
        {
            try
            {

                var tree = SyntaxFactory.ParseSyntaxTree(code);
                var compilation = CSharpCompilation.Create("CalculatorNotepad_CS.dll")
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                    .AddReferences(getReferences())
                    .AddSyntaxTrees(tree);
                return compilation;
            }
            catch (Exception ex)
            {
                return null;
            }
        }



        public static string Compile(string code, bool addNewFunctions)
        {
            // split source into lines
            lastCompiledSource = code;
            var srcLines = code.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            int curLine = 0, totLines= srcLines.Length;
            // add first header line
            string CR = "\r\n";
            string finalSrc = "using System; using System.Collections.Generic; using System.Linq; using T = CalculatorNotepad.mcValue; using MPFR=Numbers.MPFR; using Quad=Numbers.Quad; using Number=";
            if (Number.defaultClassType == NumberClass.Double) finalSrc += " System.Double;"; else finalSrc += " Numbers.Number;";
            finalSrc += CR;
            int HeaderLines = 2; // to adjust reported error lines by 2 smaller (due to two artificial lines we inserted here )
            // gather eventual initial usings
            bool doLoop = true;
            while ((curLine < totLines)&& doLoop)
            {
                if (srcLines[curLine].TrimStart().SubStr(0, 5) == "using")
                {
                    finalSrc+= srcLines[curLine] + CR;
                    //HeaderLines++; // not artificial line, so no need to further reduce error line number
                }
                else if ((srcLines[curLine].Trim() == "")|| (srcLines[curLine].Trim().StartsWith("//")))
                    HeaderLines--; // not including these empty lines or comments before/between usings in source, so adjust upward (+1) reported error lines afterwards
                else
                    doLoop = false;
                if (doLoop) curLine++;
            }
            // add second header line (CScode class and namespace )
            finalSrc += "namespace CalculatorNotepad { public class CScode { \r\n"; ;
            // add remaining source lines
            for (; curLine < totLines; curLine++)
                finalSrc+=srcLines[curLine] + CR;
            // add footer
            finalSrc += "}}\r\n";
            // convert source to string[] and compile
            var compilerResult = doCompile( finalSrc );
            // process compile result and return string if error
            if (compilerResult == null) return "Invalid compile result !";
            using (var ms = new MemoryStream())
                try
                {
                    var result = compilerResult.Emit(ms);

                    if (!result.Success)
                    {
                        var failures = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error);
                        string err = "";
                        foreach (var ce in failures)
                        {
                            var loc = ce.Location.GetLineSpan().StartLinePosition;
                            err += "[" + (loc.Line - HeaderLines+1) + " : "+ (loc.Character+1) + "] " + ce.GetMessage() + Environment.NewLine;
                        }
                        return err;
                    }
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());
                    if (assembly == null) return "Compile done without valid assembly result !";
                    CScode = assembly.GetType("CalculatorNotepad.CScode");
                    if (CScode == null) return "Compile done, but CScode class inacessible!";
                    // create dummy instance, so methods do not all have to be static
                    CSinstance = Activator.CreateInstance(CScode);
                    // iterate all compiled methods and create new user functions - prepare conditions for appendFunctions()
                    if (addNewFunctions)
                        ExploreAssembly(assembly);
                }
                catch (Exception e)
                {
                    return "Error after compilation: " + e.Message;
                }
            // succesful comilation, so mark as changed
            changedCompilation = true;
            return "";
        }


        public static object Run(string methodName, object[] input)
        {
            if (CScode == null) throw new Exception("Compile not valid!");
            var methInfo = CScode.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static); 
            if (methInfo == null) throw new ArgumentException("Undefined method : " + methodName);
            //ExploreMethod(methInfo);
            return methInfo.Invoke(CSinstance, input );
        }




        static void ExploreMethod(MethodInfo method)
        {
            ParameterInfo[] Myarray = method.GetParameters();
            foreach (ParameterInfo Myparam in Myarray)
                Console.WriteLine("Parameter #" + Myparam.Position+ " is " + Myparam.ParameterType);
        }


        // add method to list here and in mc.functions
        static void addMethod(string name)
        {
            if (csFunctions.ContainsKey(name)) throw new Exception("Compile resulted in duplicated method name : " + name);
            var newCsFunc = new csFunc(name); // find/refactor function and remember its method/parameter types
            csFunctions.Add(name, newCsFunc); // add that function to list here, just so it remains in global scope
            var toolTip = DynamicAutocollection.makeToolTipDescription(newCsFunc.methodInfo, false); // make description withou type names, so 'fib(n)' instead of 'int fib ( int n )'
            // create parse func that calls this c# func as direct lambda
            var newParse = new mcFuncParse(name, mcFuncParamType.Func, newCsFunc.numMinParams, 90, new mcFunc(args => newCsFunc.Call(args)), true, "User defined C# function" , toolTip);
            newParse.funcCalc.flags |= mcFuncFlags.CSfunc;
            parseFunctions.Add(name, newParse);
        }

        // append already compiled mcParseFunc to list. If no c# code, just skip
        static public bool appendFunctions()
        {
            string err = "";
            if (parseFunctions != null) {
                foreach (var pf in parseFunctions)
                    if (!mc.addFunc(pf.Key, pf.Value, false))  // add parse function to mc list, under given name, as user function
                        err += (err != ""?",":"")+pf.Key ;
                if (err != "") err = "Duplicated methods: " + err;
            }
            return changedCompilation;
        }


        // invalidate last compilation
        static public void Invalidate()
        {
            parseFunctions = null;
        }
        static public bool isValid()
        {
            return parseFunctions != null;
        }


        // find all defined functions and add to list
        static string[] ignoreMethods = new string[] { "ToString", "Equals", "GetHashCode", "GetType", "Finalize", "MemberwiseClone" };
        static void ExploreAssembly(Assembly assembly, bool displayNames=false)
        {
            if (displayNames) Console.WriteLine("Modules in the assembly:");
            csFunctions = new Dictionary<string, csFunc>();
            parseFunctions = new Dictionary<string, mcFuncParse>();
            foreach (Module m in assembly.GetModules())
            {
                if (displayNames) Console.WriteLine("M={0}", m);

                foreach (Type t in m.GetTypes())
                {
                    if (displayNames) Console.WriteLine("\tT={0}", t.Name);
                    bool csModule = t.Name == "CScode";
                    foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static ))
                    {
                        if (displayNames) Console.WriteLine("\t\t{0}", mi.Name);
                        // if this is method from CScode, and not predefined/ignorable, add to list
                        if (csModule && !ignoreMethods.Contains(mi.Name))
                            addMethod(mi.Name);
                    }
                }
            }
        }
    }


    public class csFunc
    {
        public string name;
        public MethodInfo methodInfo;
        public Type resultType;
        public int numParams, numMinParams;
        public ParameterInfo[] paramInfo;
        // find named c# function and store its parameter data/types
        public csFunc(string Name)
        {
            name = Name;
            // get function
            if (mcCompiler.CScode == null) throw new Exception("Compile not valid!");
            // GetMethod will break if two functions with same name exist in c# code
            try
            {
                methodInfo = mcCompiler.CScode.GetMethod(Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            }catch(Exception e)
            {
                throw new mcException( e.Message + " : " + name);
            }
            if (methodInfo == null) throw new Exception("Undefined method : " + Name);
            // get result and param types
            resultType = methodInfo.ReturnType;
            paramInfo = methodInfo.GetParameters();
            numParams = paramInfo.Length;
            numMinParams = 0;
            for (int i = 0; i < numParams; i++)
            {
                if (!paramInfo[i].IsOptional)
                    numMinParams++;
                if (paramInfo[i].Position != i)
                    throw new Exception("Misaligned position for parameter " + paramInfo[i].Name + " in method " + name);
            }
        }



        // convert mcValue[] args to object[] input
        object[] args2objects(mcValue[] args)
        {
            // convert mcValue[] to Object[] for allowed/known input types
            var input = new List<Object>(numParams);
            int pi = 0, pa = 0;
            bool end = false;
            while ((pa < args.Length) && !end)
            {
                var thisPtype = pi < numParams ? paramInfo[pi].ParameterType : typeof(mcValue);
                // if this is mcValue[], turn all remaining args into this array
                if (thisPtype == typeof(mcValue[]))
                {
                    if (pa == 0)
                        input.Add(args);
                    else
                    {
                        var shortArgs = new mcValue[args.Length - pa];
                        for (int i = 0; i < shortArgs.Length; i++)
                            shortArgs[i] = args[pa + i];
                        input.Add(shortArgs);
                    }
                    pi++;
                    end = true;
                }
                else
                {
                    try
                    {
                        // single element. Even if List<int>, it should be created from single mcValue (assume its vector)
                        if (thisPtype == typeof(mcValue)) input.Add(args[pa]);
                        else if (thisPtype == typeof(double)) input.Add(args[pa].Double);
                        else if (thisPtype == typeof(Number)) input.Add(args[pa].Number);
                        else if (thisPtype == typeof(MPFR)) input.Add(args[pa].Number.AsMPFR);
                        else if (thisPtype == typeof(int)) input.Add(args[pa].Int);
                        else if (thisPtype == typeof(long)) input.Add(args[pa].Long);
                        else if (thisPtype == typeof(bool)) input.Add(args[pa].isTrue());
                        else if (thisPtype == typeof(List<mcValue>)) input.Add(args[pa].Vector);
                        else if (thisPtype == typeof(List<double>)) input.Add(args[pa].getListDouble());
                        else if (thisPtype == typeof(List<Number>)) input.Add(args[pa].getListNumber());
                        else if (thisPtype == typeof(List<int>)) input.Add(args[pa].getListInt());
                        else if (thisPtype == typeof(List<long>)) input.Add(args[pa].getListLong());
                        else if (thisPtype == typeof(double[])) input.Add(args[pa].getListDouble().ToArray());
                        else if (thisPtype == typeof(Number[])) input.Add(args[pa].getListNumber().ToArray());
                        else if (thisPtype == typeof(int[])) input.Add(args[pa].getListInt().ToArray());
                        else if (thisPtype == typeof(long[])) input.Add(args[pa].getListLong().ToArray());
                        else
                            throw new mcException("Unsupported parameter type " + thisPtype.ToString());
                    }
                    catch (Exception e)
                    {
                        // add info about function and parameter name in exception
                        var thisPname = "#" + pi + " " + (pi < numParams ? paramInfo[pi].Name : "...");
                        throw new mcException("Paramether " + thisPname + " of method " + name + " : " + e.Message);
                    }
                    // move pointers
                    pi++;
                    pa++;
                }
            }
            // if missing optional parameters, add Type.Missing
            for (int i = input.Count; i < numParams; i++)
                input.Add(Type.Missing);
            // return as array
            return input.ToArray();
        }

        // convert object result to mcValue result
        mcValue obj2mcValue(object res)
        {
            if (resultType == typeof(mcValue)) return (mcValue)res;
            if (resultType == typeof(double)) return new mcValue((double)res);
            if (resultType == typeof(Number)) return new mcValue((Number)res);
            if (resultType == typeof(MPFR)) return new mcValue((MPFR)res);
            if (resultType == typeof(int)) return new mcValue((int)res);
            if (resultType == typeof(long)) return new mcValue((long)res);
            if (resultType == typeof(bool)) return new mcValue((bool)res ? 1 : 0);
            if (resultType == typeof(List<mcValue>)) return new mcValue((List<mcValue>)res);
            if (resultType == typeof(List<double>)) return new mcValue((List<double>)res);
            if (resultType == typeof(List<Number>)) return new mcValue((List<Number>)res);
            if (resultType == typeof(List<MPFR>)) return new mcValue((List<MPFR>)res);
            if (resultType == typeof(List<int>)) return new mcValue((List<int>)res);
            if (resultType == typeof(List<long>)) return new mcValue((List<long>)res);
            if (resultType == typeof(double[])) return new mcValue(((double[])res).ToList());
            if (resultType == typeof(Number[])) return new mcValue(((Number[])res).ToList());
            if (resultType == typeof(MPFR[])) return new mcValue(((MPFR[])res).ToList());
            if (resultType == typeof(int[])) return new mcValue(((int[])res).ToList());
            if (resultType == typeof(long[])) return new mcValue(((long[])res).ToList());
            // if return can n ot be converted to mcValue, throw an exception
            throw new mcException("Unsupported return type " + resultType.ToString() + " in method " + name);
        }

        // Invoke method synchroniously, without timeout guard (for debugs)
        mcValue callSync(object[] input)
        {
            Object res = methodInfo.Invoke(mcCompiler.CSinstance, input);
            return obj2mcValue(res);
        }

        // Invoke method using thread, with timeout guard
        mcValue callThreaded(object[] input)
        {
            Object res = null;
            Exception eThread = null;
            var iThread = new Thread(() =>
            {
                try
                {
                    res = methodInfo.Invoke(mcCompiler.CSinstance, input);
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
                    if (eThread is TargetInvocationException)
                        throw (eThread as TargetInvocationException).InnerException;
                    else
                        throw eThread;
                }
                return obj2mcValue(res);
            }
            else
            {
                iThread.Abort();
                throw new mcException("ERR:Timeout");
            }
        }


        // ** Call function, used in Lambdas
        // convert mcValue[] to correct input param types, call method, then convert result to mcValue
        public mcValue Call(mcValue[] args)
        {
            // convert mcValue[] to Object[] for allowed/known input types
            var input = args2objects(args);
            // clear csharp cache, in case different function use unnamed cache 
            mc.cacheCSclear();
            // CALL c# function. If not enough parameters pass Type.Missing and let invoke complain
            //return callSync(input); // for debug without thread
            return callThreaded(input);
        }




    }

}
