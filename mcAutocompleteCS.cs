using FastColoredTextBoxNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace CalculatorNotepad
{

    /// <summary>
    /// CS box: static class to initialize Autocomplete and Syntax Highlighting
    /// </summary>
    public class mcAutocompleteCS
    {
        public static void Init(FastColoredTextBox tb)
        {
            tb.Language = Language.CSharp;
            tb.DelayedTextChangedInterval = 1000;
            tb.DelayedEventsInterval = 500;
            tb.ChangedLineColor = Color.FromArgb(255, 230, 230, 255); //changedLineColor;
            tb.CurrentLineColor = Color.FromArgb(100, 210, 210, 255); //currentLineColor;
            tb.ShowFoldingLines = true;
            tb.HighlightingRangeType = HighlightingRangeType.VisibleRange;
            tb.AutoIndentChars = false; // with true, it prevent deleting spaces around equal sign in " x = 2*x " !
            //create autocomplete popup menu
            AutocompleteMenu popupMenuCS = new AutocompleteMenu(tb);
            popupMenuCS.ForeColor = Color.White;
            popupMenuCS.BackColor = Color.Gray;
            popupMenuCS.SelectedColor = Color.Purple;
            //popupMenuCS.SearchPattern = @"[\w\.]";
            popupMenuCS.SearchPattern = @"[\w\.:=!<>]"; // default used, but my FindType can not find Array<int> anyway
            popupMenuCS.AllowTabKey = true;
            popupMenuCS.AlwaysShowTooltip = true;
            popupMenuCS.ToolTipDuration = 200000;
            //assign DynamicCollection as items source
            popupMenuCS.Items.SetAutocompleteItems(new DynamicAutocollection(popupMenuCS, tb));
        }
    }


    public class DynamicAutocollection : IEnumerable<AutocompleteItem>
    {
        private AutocompleteMenu menu;
        private FastColoredTextBox tb;

        public DynamicAutocollection(AutocompleteMenu menu, FastColoredTextBox tb)
        {
            this.menu = menu;
            this.tb = tb;
        }

        static string[] cs_keywords = { "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while", "add", "alias", "ascending", "descending", "dynamic", "from", "get", "global", "group", "into", "join", "let", "orderby", "partial", "remove", "select", "set", "value", "var", "where", "yield" };


        // return full description of one method
        static int recursiveGuard = 0;

        //**** Helper f-ons for enumerate methods

        // format generic arguments
        static string genArgs(Type[] typeParameters)
        {
            string name = "<";
            for (int i = 0; i < typeParameters.Length; i++)
            {
                var tp = typeParameters[i];
                name += (i > 0 ? "," : "") + TypeName(tp);
                //if (tp.IsGenericParameter)  name += "#" + tp.GenericParameterPosition;
            }
            name += ">";
            return name;
        }
        // return short Type name, replacing Int32 to int, also checking for generic types
        static string TypeName(Type t, bool showTypes = true)
        {
            if (!showTypes) return "";
            // test circular reference, since generic arguments also call typename...
            recursiveGuard++;
            if (recursiveGuard > 100)
            {
                invalidEnumerator = true;
                return "???";
            }
            string name = t.Name;
            int pap = name.IndexOf('`');
            if (pap > 0) name = name.SubStr(0, pap); // remove Name'1  used for some generic types
            // simplify some names
            if (t == typeof(double)) name = "double";
            else    // Double
            if (t == typeof(int)) name = "int";
            else    // Int32
            if (t == typeof(long)) name = "long";
            else    // Int64
            if (t == typeof(bool)) name = "bool";
            else    // Boolean
            if (t == typeof(char)) name = "char";
            else    // Char
            if (t == typeof(string)) name = "string";
            else    // String
            if (t == typeof(int[])) name = "int[]";
            else    // Int32[]
            if (t == typeof(double[])) name = "double[]";
            else    // Double[]
            if (t == typeof(void)) name = "void";                       // Void
            // check for generic types eg: List<int>
            if (t.IsGenericType)
                name += genArgs(t.GetGenericArguments());
            return name;
        }


        static string desc(MethodInfo method, string prefix = " ", bool showTypes = true)
        {
            try
            {
                string tips = "";
                int startParam = 0;
                recursiveGuard = 0;
                if (method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute)))
                {   // this is extension method, so it include 'this extensionType' as first param
                    startParam = 1; // skip first parameter, since it is always 'this extensionType'
                    tips += "E"; // marks extension methods
                }
                else
                    tips += " ";
                if ((method.CallingConvention & CallingConventions.HasThis) != 0)
                {   // this is instance method, so it includes 'this' as first param
                    //startParam = 1; // no need to skip first param, already removed by GetParameters()
                    tips += "*"; // marks instance-only (non-static) methods
                }
                else
                    tips += " ";
                // if no prefix is passed, do not show Extension/Instance indicators
                tips = prefix != "" ? prefix + tips + " " : "";
                tips += TypeName(method.ReturnType, showTypes) + " " + method.Name;
                if (method.IsGenericMethod) tips += genArgs(method.GetGenericArguments());
                tips += " ( ";
                string inner = "";
                var paramInfo = method.GetParameters();
                for (int i = startParam; i < paramInfo.Length; i++)
                {
                    var p = paramInfo[i];
                    if (p!=null && p?.Name !=null )
                        inner += (inner != "" ? ", " : "") + TypeName(p.ParameterType, showTypes) + " " + p?.Name.ToString();
                }
                tips += inner + " )";
                return tips;
            }
            catch
            {
                invalidEnumerator = true;
                return "???";
            }
        }

        public static string makeToolTipDescription(MethodInfo method, bool showTypes = true)
        {
            return desc(method, showTypes ? "  " : "", showTypes);
        }


        static bool invalidEnumerator = false;
        public IEnumerator<AutocompleteItem> GetEnumerator()
        {
            if (invalidEnumerator) yield break; // in case of circular references or exceptions... to skip further attempts
            //get current fragment of the text
            var text = menu.Fragment.Text;
            if (text == "") yield break;
            //check if this is method ( with class name before dot)
            var parts = text.Split(new char[] { '.' }, StringSplitOptions.None);
            if (parts.Length < 2)
            {
                int len = text.Length;
                // not method, so return keywords
                foreach (var kw in cs_keywords)
                    if (kw.SubStr(0, len) == text)
                        yield return new AutocompleteItem(kw);
            }
            else
            {
                var partNames = parts.ToList();
                partNames.RemoveAt(partNames.Count - 1); // remove last (partial) method name

                //find type for given className
                var type = FindTypeByName(partNames);

                if (type == null)
                    yield break;

                //**** BEGIN enumerate methods

                // return public constants and fields
                foreach (var memberInfo in type.FindMembers(MemberTypes.Field, BindingFlags.Public | BindingFlags.Static, (x, y) => { return true; }, "tt"))
                {
                    if (memberInfo is FieldInfo)
                    {
                        var fi = memberInfo as FieldInfo;
                        recursiveGuard = 0;
                        yield return new MethodAutocompleteItem(fi.Name)
                        {
                            ToolTipTitle = type.FullName + "." + fi.Name,
                            ToolTipText = "    " + TypeName(fi.FieldType) + " " + fi.Name
                        };
                    }
                }
                if (invalidEnumerator) yield break;

                //return static properties of the class
                foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    recursiveGuard = 0;
                    yield return new MethodAutocompleteItem(pi.Name)
                    {
                        ToolTipTitle = type.FullName + "." + pi.Name,
                        ToolTipText = "    " + TypeName(pi.PropertyType) + " " + pi.Name
                    };
                }
                if (invalidEnumerator) yield break;

                //enumerate methods of the class 
                var allMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).ToList();
                // get extension methods for this class
                var extendedMethods = getExtensions(type);
                // add them together, they will be sorted inside
                allMethods.AddRange(extendedMethods);
                allMethods.Sort((m1, m2) => m1.Name.CompareTo(m2.Name));
                for (int m = 0; (m < allMethods.Count) && !invalidEnumerator; m++)
                    if ((allMethods[m].Attributes & MethodAttributes.SpecialName) == 0) // skip special methods
                    {
                        MethodAutocompleteItem newItem = null;
                        try
                        {
                            var method = allMethods[m];
                            string tips = desc(method);
                            // check if multiple methods with same name
                            int nDup = 1;
                            while ((m < allMethods.Count - 1) && (method.Name == allMethods[m + 1].Name))
                            {
                                m++;
                                nDup++;
                                tips += Environment.NewLine + desc(allMethods[m]);
                            }
                            newItem = new MethodAutocompleteItem(method.Name + "()")
                            {
                                ToolTipTitle = type.FullName + "." + method.Name + "()" + (nDup > 1 ? "   [x" + nDup + "]" : ""),
                                ToolTipText = tips
                            };
                        }
                        catch
                        {
                            invalidEnumerator = true;
                        }
                        if (!invalidEnumerator)
                            yield return newItem;
                        else
                            yield break;
                    }
            }
        }

        // get extension methods
        List<MethodInfo> getExtensions(Type extendedType)
        {
            var res = new List<MethodInfo>(10);
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var a in assemblies)
                    foreach (var t in a.GetTypes())
                        if (t.IsSealed && !t.IsGenericType && !t.IsNested)
                            foreach (var m in t.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ))
                                if (m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                                {
                                    Type pt = m.GetParameters()[0].ParameterType;
                                    if (pt.IsAssignableFrom(extendedType))
                                        res.Add(m);
                                }
            }
            catch
            {
                invalidEnumerator = true;
            }
            return res;
        }

        // find type by name, among all modules
        Type FindTypeByName(List<string> parts)
        {
            Type foundType = null;
            try
            {
                bool isSameCapital(string A, string B)
                {
                    if ((A == "") || (B == "")) return false;
                    return ((char.ToLower(A[0]) == char.ToLower(B[0])) && (A.SubStr(1) == B.SubStr(1)));
                }
                if (parts.Count == 0) return null;
                string name = parts[parts.Count - 1];
                // customize few names that differ in more than just casing
                if (name == "int") name = "Int32";
                else
                if (name == "long") name = "Int64";
                var loName = name.ToLower();
                // make case insensitive search
                var found = new List<Type>();
                bool foundExact = false;
                bool foundCapital = false;
                int foundParts = -1;
                int foundMatching = -1;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var a in assemblies)
                    foreach (var t in a.GetTypes())
                        if (t.Name.ToLower() == loName)
                        {
                            // how good is this match
                            var tParts = t.FullName.Split('.');
                            bool thisExact = (t.Name == name);
                            bool thisCapital = isSameCapital(name, t.Name);
                            int thisParts = tParts.Length;
                            int thisMatching = 0;
                            while ((thisMatching < tParts.Length) && (thisMatching < parts.Count) && (tParts[tParts.Length - thisMatching - 1].ToLower() == parts[parts.Count - thisMatching - 1].ToLower())) thisMatching++;
                            // see if this is better than previous match
                            bool better = foundType == null;
                            if (thisExact && !foundExact)  better = true;
                            if (thisMatching > foundMatching)  better = true;
                            if (thisCapital && (!foundExact) && (!foundCapital) ) better = true;
                            if (thisParts > foundParts) better = true;
                            // mark this one as best if needed
                            if (better)
                            {
                                foundType = t;
                                foundExact = thisExact;
                                foundCapital = thisCapital;
                                foundParts = thisParts;
                                foundMatching = thisMatching;
                            }
                        }
            }
            catch
            {
                invalidEnumerator = true;
            }
            // return best found match
            return foundType;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
