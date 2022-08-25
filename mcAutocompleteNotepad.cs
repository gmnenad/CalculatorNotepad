using FastColoredTextBoxNS;
using Range = FastColoredTextBoxNS.Range;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CalculatorNotepad
{

    /// <summary>
    /// Notepad box: static class to initialize Autocomplete and Syntax Highlighting
    /// </summary>
    public class mcAutocompleteNotepad
    {
        public static AutocompleteMenu popupMenuNotepad; // used in Ctrl-Space forced show

        public static void Init(FastColoredTextBox tb)
        {
            tb.SyntaxHighlighter = new NotepadHighlighter(tb);
            tb.DelayedTextChangedInterval = 500;
            tb.DelayedEventsInterval = 100;
            tb.ChangedLineColor = Color.FromArgb(255, 230, 230, 255); //changedLineColor;
            tb.CurrentLineColor = Color.FromArgb(100, 210, 210, 255); //currentLineColor;
            tb.HighlightingRangeType = HighlightingRangeType.VisibleRange;
            tb.AutoIndentChars = false; // with true, it prevent deleting spaces around equal sign in " x = 2*x " !
            // setup Autocomplete
            popupMenuNotepad = new AutocompleteMenu(tb);
            popupMenuNotepad.AppearInterval = 500;
            //popupMenuNotepad.SearchPattern = @"[\w\.:=!<>]";
            popupMenuNotepad.SearchPattern = @"[\w\.]"; 
            popupMenuNotepad.MinFragmentLength = mc.cfg.autocompleteChars;
            popupMenuNotepad.Enabled = mc.cfg.autocompleteChars > 0;
            popupMenuNotepad.AllowTabKey = true;
            popupMenuNotepad.AlwaysShowTooltip = true; // not working
            popupMenuNotepad.ToolTipDuration = 200000;
            popupMenuNotepad.Items.SetAutocompleteItems(new DynamicNotepadAutocollection(popupMenuNotepad, tb));
        }
    }



    /// <summary>
    /// class that manage highlighting Notepad source
    /// </summary>
    class NotepadHighlighter : SyntaxHighlighter
    {

        protected Regex NotepadCommentRegex, NotepadNumberRegex, NotepadUserRegex, NotepadVarRegex, NotepadKeywordRegex, NotepadUnitsRegex;
        public readonly Style SimpleBoldStyle = new TextStyle(null, null, FontStyle.Bold);

        public NotepadHighlighter(FastColoredTextBox currentTb) : base(currentTb)
        {
            StringStyle = BrownStyle;
            CommentStyle = GreenStyle;
            NumberStyle = MagentaStyle;
            AttributeStyle = GreenStyle;
            ClassNameStyle = BlueBoldStyle;
            KeywordStyle = BlueStyle;
            CommentTagStyle = GrayStyle;
            NotepadCommentRegex = new Regex(@"//.*$", RegexOptions.Multiline | RegexCompiledOption);
            NotepadNumberRegex = new Regex(@"\b\-?(((0x|0b|0o|0\[[\d]+\])[0-9a-zA-Z_,`]+[\.]?[0-9a-zA-Z_,`]*)|[0-9][0-9_,`]*[\.]?[0-9_,`]*)([eE@]\-?[0-9][0-9_,`]*)?[dDqQmM]?", RegexCompiledOption);
            NotepadUnitsRegex = new Regex(@"'(.*?)'", RegexCompiledOption);
            NotepadKeywordRegex = new Regex(@"\b(pSim|call|vSum|random|vDim)\b|∑", RegexCompiledOption);
            NotepadUserRegex = new Regex("", RegexCompiledOption);
            NotepadVarRegex = new Regex("", RegexCompiledOption);
            UpdatedRegex();
        }

        public override void AutoIndentNeeded(object sender, AutoIndentEventArgs args)
        {
            //CSharpAutoIndentNeeded(sender, args);
        }

        // get known functions from mc to highlight
        public bool UpdatedRegex()
        {
            void add(ref string str, string toAdd)
            {
                if (str.Length > 0) str += "|" + toAdd; else str += toAdd;
            }

            void addNameSymbol(ref string names, ref string symbols, mcFuncParse pf)
            {
                if (pf.Name.Length == 0) return;
                if (char.IsLetter(pf.Name[0]))
                    add(ref names, pf.Name);
                else
                    if ((pf.ParamType == mcFuncParamType.Func) && (pf.Name.Length == 1))
                    add(ref symbols, pf.Name);
            }

            string makeRegex(string names, string symbols)
            {
                string regex = "";
                if (names != "") regex = @"\b(" + names + @")\b";
                if (symbols != "") regex += (regex != "" ? "|" : "") + symbols;
                return regex;
            }

            bool changed = false;
            // add all functions to be highlighted
            if (mc.functions != null)
            {
                string userNames = "", userSymbols = "", builtNames = "", builtSymbols = "";
                foreach (var dicF in mc.functions)
                {
                    var f = dicF.Value;
                    if (f.isUserFunction)
                        addNameSymbol(ref userNames, ref userSymbols, f);
                    else
                        addNameSymbol(ref builtNames, ref builtSymbols, f);
                }
                // format new regex and check if changed
                var newBuiltRegex = makeRegex(builtNames, builtSymbols);
                if (newBuiltRegex != NotepadKeywordRegex.ToString())
                {
                    NotepadKeywordRegex = new Regex(newBuiltRegex, RegexCompiledOption);
                    changed = true;
                }
                var newUserRegex = makeRegex(userNames, userSymbols);
                if (newUserRegex != NotepadUserRegex.ToString())
                {
                    NotepadUserRegex = new Regex(newUserRegex, RegexCompiledOption);
                    changed = true;
                }
            }
            // add all global variables to be highlighted
            if (mc.varNames != null)
            {
                string userVariables = "";
                foreach (var dicN in mc.varNames)
                    if (mc.isGlobalVar(dicN.Value))
                        add(ref userVariables, dicN.Key);
                // format new regex and check if changed
                var newVarRegex = makeRegex(userVariables, "");
                if (newVarRegex != NotepadVarRegex.ToString())
                {
                    NotepadVarRegex = new Regex(newVarRegex, RegexCompiledOption);
                    changed = true;
                }

            }
            // if changed, reHighlight
            if (changed)
                HighlightSyntax(currentTb.Language, currentTb.Range);
            return changed;
        }


        public override void HighlightSyntax(Language language, Range range)
        {
            range.tb.CommentPrefix = "//";
            range.tb.LeftBracket = '(';
            range.tb.RightBracket = ')';
            range.tb.LeftBracket2 = '{';
            range.tb.RightBracket2 = '}';
            range.tb.BracketsHighlightStrategy = BracketsHighlightStrategy.Strategy2;
            //clear style of changed range
            range.ClearStyle(StringStyle, CommentStyle, NumberStyle, AttributeStyle, ClassNameStyle, KeywordStyle, SimpleBoldStyle, CommentTagStyle);
            //comment highlighting
            range.SetStyle(CommentStyle, NotepadCommentRegex);
            //number highlighting
            range.SetStyle(NumberStyle, NotepadNumberRegex);
            //user functions highlighting
            range.SetStyle(ClassNameStyle, NotepadUserRegex);
            //user variables highlighting
            range.SetStyle(SimpleBoldStyle, NotepadVarRegex);
            //keyword (predefined functions)  highlighting
            range.SetStyle(KeywordStyle, NotepadKeywordRegex);
            //units  highlighting
            range.SetStyle(CommentTagStyle, NotepadUnitsRegex);
        }
    }





    /// <summary>
    /// Class that dynamically fill Autocomplete for Notepad source
    /// </summary>
    public class DynamicNotepadAutocollection : IEnumerable<AutocompleteItem>
    {
        private AutocompleteMenu menu;
        private FastColoredTextBox tb;

        public DynamicNotepadAutocollection(AutocompleteMenu menu, FastColoredTextBox tb)
        {
            this.menu = menu;
            this.tb = tb;
        }



        public IEnumerator<AutocompleteItem> GetEnumerator()
        {
            ToolTipClass tt;
            // if we want to show entire autocomplete
            if (mc.showEntireAutocomplete)
            {
                var tList = new List<ToolTipClass>(mc.functions.Count);
                foreach (var dicF in mc.functions)
                {
                    tt = mc.getToolTips(dicF.Value);
                    if (tt.valid) tList.Add(tt);
                }
                tList.Sort((x, y) => x.Name.CompareTo(y.Name));
                foreach(var stt in tList)
                    yield return new NotepadAutocompleteItem(stt.Name) { ToolTipTitle = stt.Title, ToolTipText = stt.Text };
            }
            else
            // standard enumeration based on entered fragment
            {
                //get current fragment and preliminary checks
                if ((mc.functions == null) || (mc.varNames == null)) yield break; // not yet created
                var fragment = menu.Fragment.Text.ToLower();
                if (fragment == "") yield break; // no fragment, should not be here
                int fragLen = fragment.Length;
                if (fragLen < mc.cfg.autocompleteChars) yield break; // fragment shorter than config 
                                                                     // check if this is inside comment (in which case disable autocomplete)
                int lin = tb.Selection.Start.iLine;
                if (lin < tb.Lines.Count)
                {
                    int col = tb.Selection.Start.iChar;
                    var thisLine = tb.Lines[lin];
                    int cmnt = thisLine.IndexOf("//");
                    if ((cmnt >= 0) && (cmnt < col)) yield break; // fragment is after // comment start 
                }
                // returns true if name starts with fragment
                bool fStart(string name)
                {
                    return ((name.Length >= fragLen) && (name.Substring(0, fragLen).ToLower() == fragment));
                }
                // returns true if name contains fragment, but does NOT start with fragment
                bool fContains(string name)
                {
                    if (name.Length < fragLen) return false;
                    name = name.ToLower();
                    return (name.Contains(fragment) && (name.Substring(0, fragLen) != fragment));
                }
                // first return those that start with this fragment:  functions, then variables
                foreach (var dicF in mc.functions)
                    if (fStart(dicF.Key))
                    {
                        tt = mc.getToolTips(dicF.Value);
                        if (tt.valid) yield return new NotepadAutocompleteItem(tt.Name) { ToolTipTitle = tt.Title, ToolTipText = tt.Text };
                    }
                foreach (var dicF in mc.varNames)
                    if (fStart(dicF.Key))
                    {
                        tt = mc.getToolTips(dicF.Key);
                        if (tt.valid) yield return new NotepadAutocompleteItem(tt.Name) { ToolTipTitle = tt.Title, ToolTipText = tt.Text };
                    }
                // then return those that contain this fragment somewhere inside (if fragment is larger than 2 chars):  functions, then variables
                if (fragLen >= 3)
                {
                    foreach (var dicF in mc.functions)
                        if (fContains(dicF.Key))
                        {
                            tt = mc.getToolTips(dicF.Value);
                            if (tt.valid) yield return new NotepadAutocompleteItem(tt.Name) { ToolTipTitle = tt.Title, ToolTipText = tt.Text };
                        }
                    foreach (var dicF in mc.varNames)
                        if (fContains(dicF.Key))
                        {
                            tt = mc.getToolTips(dicF.Key);
                            if (tt.valid) yield return new NotepadAutocompleteItem(tt.Name) { ToolTipTitle = tt.Title, ToolTipText = tt.Text };
                        }
                }
            }
        }


        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }


    /// <summary>
    /// custom autocomplete class, to allow items that donot start with fragment
    /// </summary>
    public class NotepadAutocompleteItem : AutocompleteItem
    {

        public NotepadAutocompleteItem(string text)
            : base(text)
        {
        }

        public override CompareResult Compare(string fragmentText)
        {
            return CompareResult.Visible;
        }

    }



}
