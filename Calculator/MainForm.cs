//   !!
//   !!!!!!!!!!!!!!    NEW   FORM   !!!!!!!!!!!!!!!!!!
//   !!


#region TO DO
/*
 * TO DO:
 * 
 * - change all vXYZ to vecXYZ , so autocomplete will work with vec... alternativelly add aliases for each, so vDim and vecDim both exists
 * - bug: after changing variable name, old variable name is still active/usable!  n=5 ; n+1==6 ... replace n->z :  z=5; n+1=6 still ! ** Detect definition name change, and recalc whole doc
 * 
 * - optimize recalc to recognize if empty block was added/deleted (just new line for example)
 * - optimize result draw to replace/insert lines instead of making entirely new 
 * 
 * - add 'interpolate' function, input is vector of points [ either 1dim with x1,y1,x2,y2..., or 2 dim with vec(vec(x1,y1),vec(x2,y2)...) ] and X, return is Y for that X
 *      - nmMath.Interpolate( double x, List<Point> points, int interType=0) , will sort 'points' by X first, and interType=0=linear, 1=cubic etc...
 *      - nmMath.Interpolate_Linear(double x, List<Point> points) and  Interpolate_Linear(double x,params double[] points) can assume already sorted
 * + rename or add alias for 'choose' to be 'nCr', and add 'nPr' as N!/(N-x)!
 * - fix 'choose' to work with fractional inputs, by using Interpolate_Linear ...although it would need interpolate in two dimensions !? 
 *      - nCr(10.3,4.6) :  first interpolate for v1=(10,4.6) and v2=(11, 4.6), then interpolate for 10.3 from [(10,v1),(11,v2)]
 *      
 * + upgrade to Core 6 , will also add better c# compiler and could help with BigDouble
 * - check for BIG MATH  (BIG potential change)
 *      - best to have bigMath class that overloads all normal operators and extend all regulars like Math.Abs, Math.Sign etc
 *      - then either make my alias  "using MyDouble= System.Double;" and replace later with "using MyDouble= BigMathClass;", and make all nmMath functions use MyDouble, eg.  "nCR( MyDouble n, MyDouble r)"
 *      - or make all nmMath functions generic, like "nCr<T>(T n, T r)"
 *      - for mcValue, either replace double with MyDouble, or add bigValue to mcValueType in addition to { Double, Vector, Func }, then choose accordingly
 *      - for internal functions, should call them with 'BigDouble' if they support, otherwise call them with (double)BigDouble if possible, otherwise report error 'funcXYZ does not support large numbers '
 *      - would need to extend auto-parameter fit for calling c# user functions, to match either myFunc(double x) and myFunc(BigDouble x)
 *      
 * - update To/From string in mcparse to real also !
 */
#endregion
#region OPTIONAL TODO
/* 
 * OPTIONAL :
 * - functions declared within blocks do not optimize to constants
 * - special 'lineEnd' characters ( eg. convert \r in those special), and possibility to fold/unfold them with button
 * - potential numerical math functions:
 *      - solve(f(x,y,...)) - or (root (f) ) ,  find vector (x,y,...) for which f(...) is zero
 *      - root(f, boundary conditions)? check wolfram
 *      - root( fa,fb,fc...) - solve for all those functions? do i even need this?
 * - isConst() evaluation to be moved from mcFunc constructor to evaluate?
 *      - currently it evaluates in mcFunc constructor, during Parse/compile phase
 *      - it does not eval again in Eal phase, since it was turned to constant, but it is still not at logical position 
 *      - very long evaluation would happen in compile of that row, which is not an issue now (since it anyway execute eval right after parse), but can be issue if refactor to do entire compile 1st, then eval)
 * 
 */
#endregion
#region OLD DONE
/*
 * DONE : 
 * + allow scalar operations at vectors, eg for vec():3 , or vec()*2
 * + add ':x' operator to round to x decimals
 * + distribution functions to calc E(X) and sigma(X) for all usual distributions:  dist_binomial(n, p [,x]) -> vec ( μ = expected , σ = stddev [ , pdf(x)|pmf(x) = probability of x ] )
 *      + 'usual distributions' are from dummies book > discrete: uniform_discrete, binomial, poisson, geometric, negative_binomial, hypergeometric ; continous:  uniform_continous, exponential, normal, Z(std_normal)
 * + cumulative distribution function for given x , cdf(x): cdf_normal(x, vec(μ,σ)) {since all other map to normal}, cdf_sum(x, n, vec(μ,σ)) {sum of 'n' random variables with distribution vec(μ,σ)}, cdf_avg, cdf_prop
 * + comboCount ( Counter ) : return total number of possible combinations for given counter : for 'normal' counters it is N^D or product(range_at_digit_d), for normal permutation it is D! or product((N+1)/(1+i),0..D-1) for non-ascending
 * + remember/restore size on start/exit
 * + check why calc take same time from start and end (error in mid_line?) : due to changed last numbers
 * + position at end of edit after load/help
 * + other menu items
 * + make new +-* add 'last' word instead of ~123 numbers, so that lines do not need to change
 * + dynamic delay - to instantly compute when first newline, but if second one is soon then increase to larger delay. DONE sort of, with lowered delay
 * + highlight 'units' in grey for notepad
 * + Configuration window
 * + fixed thread exceptions for c# invokes
 * + Save (only) menu item, that will save over last loaded name without asking  
 * + double click on c# error to position cursor to error line, and highlight word at error location
 * + change autocomplete to accept with TAB and Space, in addition to Enter. DONE for TABs
 * + dynamic highlighting for known names in notepad (different colors for system+predefined, and for user functions)
 * + modify help to describe panels, toolbar buttons (open c# pane, clear..) , ctrl-arrows, ctrl-space for list of all functions (or autocomplete)
 * + c# invoke to recognize optional parameters
 * + replacing with greek symbols , before calling calcLines
 * + when autoreplace to symbols happens, remember cursor position
 * + dynamic autocomplete for c#, to show methods from reflection
 * + separate math functions ( and cache functions ) from mc class to nM class , so that it can be used easier from c# functions
 * + double.PositiveInfinity not showing on c# autocomplete ? FIXED:  enumerated public fields too, in addition to properties and methods
 * + change autocomplete on notepad when user functions are changed, to include notepad function names in autocomplete too  (after each calcLine - check performance impact )
 * + decorate notepad user functions with automatic toolTip description
 * + mouse hower tooltip on notepad
 * + allow function decorations similar to c#: /// text above function will go to description
 * + if calcLines execution lasts too long, automatically save lastdocument to preserve against exception or kill
 * + autosave every 10sec or so, since exception break will prevent emergencysaves
 * + vShuffle in two variants: a) when param N=inetegr, make random shuffled N sized vector. b) when param v=vector, shuffle vector itself
 * + debug variable option, with list of variables to be shown in CSerror panel 
 * + deepCopy of functions need to change assign.subfunc to point to new variable/func locations !!
 * - alternative mcState.copyfunctions way if no func redefinition (just shallow clone functions[] and separately copy variable values, on restore set variable values). NOT NEEDED: deep copy ~1ms
 * + ignore comments inside expressions for multiline, by skipping comments in skipSpaces
 * + blocks need to set hasDefinition, or calcLine can not use lastDef optimization
 * + add hints to config
 * + option to format result numbers ( so less digits are shown )
 * + think about basic blocks again (multiline):
 *      + parse { ; }  in addition to block( , )  with same behaviour
 *      + multiline parse , so that block can span multiple lines ( and multiline results )
 * + err: Multiple parts , add "Are you missing ; ?"
 * + allow f(x)=x;  (; at end) => replace ;\r\n with \r\n ?
 * + if {  {} ; {} } , make inner ';' optional => replace every } with }; before split on ;
 * + howering mouse above result 'Error' to display entire error text. Also, display entire number too, if shortened.
 * + error  {\r\n 5; //dsfdsf\r\n }    - when ; before comment 
 * + correct errPos :  
 *      + remove initial Trim() in mcexp.parse 
 *      + in delLeft count }; as -1 FIXED: using secondSemicolon== § , and ignoring that char for movePos
 *      + and count changed variables #0#3 accordingly. Changet to theta_oldName, and use extractMarker(theta, movepos=false), so no need to catch inside delLeft
 * + on error, use errPos to mark line and word, just like c# error on odubleclick (but automatically on notepad error)
 * + allow :  expA ; expB  without block ? ie split every exp on ;, and if more than one, virtual block 
 * + before advanced blocks and stacks, add mcContext to calls of every mcExp(text)
 * + advanced blocks (inner variables)
 * + Stack support will need 'active function' param passed to every new mcExp, to include in its assign/use variable mcFuncs
 * + do not keep anymore variables as mcParseFuncs in mc.functions  - instead they will be in mc.varValues and mc.varNames
 * + adjust findName accordingly ( to search in both places )
 * + change Clone/Copy functions to make snapshots of varNames and varValues
 * + syntax highlight variables from varNames - just global (top level )
 * + autocomplete for top level variable names
 * + hover over variable - to give current global value
 * + inner variables/f-ions should not be visible on tooltips
 * + optionally simplify entire copy/restore when function redefinition is not allowed 
 * - dynamic case for variables? NO, would be easy for user to make mistake with deep-block names
 * + allow reverse apostrophe ` to separate number digits
 * + initial calculation upon starting program does not show waiting cursor
*/
#endregion
#region DONE
/*
 * + LCD, vCopy 
 * +  dowhile( work, condition ) , and optionally: do { work } while(condition);
 * + binarySearch option to extend start/end if not correct (up to maxInt). For example, 'flags' parameter:  1= extend 'end' up to maxInt if not found; 2=extend 'start' up to 'minInt' if not found ; 4=return infinite instead of exception , if not found ;...
 * + vSort(vec [,direction=+1]) to sort vector 
 * + boolean "not" unary operator 
 * + binarySearch( (i)=>bool_func, [start=-maxInt [,end=maxInt ]): find LOWEST INDEX where func(i) == true . Precondition: func(i) is monotonous (false,...false,true,...true). Return 'i' such that bool_func(i)==true and bool_func(i-1)==false
 * + display vectors on left side with v(v(1,2),v(3,4)) instead of vec(vec(1, 2), vec(3, 4)) , to reduce space used - so v instead of vec, and no space after comma ( can do that if result larger than some predefined size )
 * + vTrunc( vec, n) : reduce size of vector to N.  - optional vTrunc(vec, (element)=>...) to reduce as long as function return true, and it pass elements from last to first, so vTrunc(vec,(e)=> e==0) will remove trailing zeros
 * + maxIdx(vec) : return index of largest element in vector . Same for minIdx(vec)
 *  * + new math functions:
 *      + gcd ( a,b,c,...) greater common denominator for all of integer arguments
 *      + factors (a) - all integer divisors of interger a, in vector
 *      + primeFactors (a) - all prime factors of interger a, in vector. primeFactors(600)=vec(2,2,2,3,5,5) . use union(primeFactors()) to get distinct
 *      + isPrime(n) - true if integer n is prime
 *      + prime(i[,n] ) - ith prime number, or primes from i-th to m-th, in vector. Converts prime position to number
 *      + nextPrime(n[,k]) - next prime larger than number n ( also wolfram func). if 'k' used, gives kth prime from N. nextPrime(N,-1) gives first smaller prime...Converts number to prime number
 *      + primePi( N) - returns number of primes up to N. Converts number to prime position
 *      + primesBetween(a,b) - all primes between two specified numbers, in vector
 * + c# autocomplete int. not working (but string. does)
 * + autocomplete for notepad to show words in middle, so 'prime' shows nextPrime too
 * + if(cond,true) - allow if with just true section
 * + if()then-else version:  if(cond) expA else expB; ??
 *      + if(cA) if(cB) expB else expE; -> whose else is it? if it is A-else, it is easier
 * + while(cond) {...} version
 * + for(i=1; i<N; i=i+1) {...}
 *      + optionally for(i=1, i<N, i=i+1) {} - more in line with current syntax
 * + find block or stack bug with { r=1; {r=r+5 } r } - returns 1, ignoring incrrease inside block: FIXED, was caching anon/lambda issue
 * + count stack sizes and memory used by evals, and stop after some value, to prevent stack overflows!
 * + introduce 'return' keyword to force exit from block and returning that value:
 *      + need to exit all blocks up to parentFunction 
 *      + return should have expression with result, return(exp) or return exp ;
 * + optimize mcFunc.Eval for stack - remove non rcursive and inner functions outside, to be called/cleared easier
 * + new func def always to include invisible block, so f(x)=for(i=1,i<3, i=i+1) will not create global 'i'
 * + mark sucessful/unsuccesful closing, so on break/endtask do NOT auto-start evaluation of lastDoc, just load it into editor (to prevent infinite loops)
 * + introduce 'new' keyword to force creation of inner variable even when outside one exists: { x=5; { new x=3; .. } y=x+1; } -> outer x=5,y=6 ( not 3,4 )
 * + make generic mcFunc.Eval also use delegate parts, to have each logic at one place
 * + fix debug variables to work with new variables
 * + ctrl-arrow to jump from comma to next/prev comma inside brackets 
 * + prevent autocomplete inside comments
 * + functions within functions? Enabled, but still global name
 * + detect and mark function that use or make side effects (touch outside variables or use functions with side effects)
 * + prevent caching for functions with side effects
 * + do not overwrite compile errors with debug variables
 * + return command must be stripped/stopped on parent function boundary
 * + stack number wrong for outside variables , need to keep link between variable[0,1] and its parent function, to get stack from parent function
 * + differentiate inner functions from topmost functions (keep block chain as info in mcFuncParse)
 * + BUG:  all local variables "i" are same fullname i[0,1] - fixed: mc.calcLines now changes subbBlock count for every line , and 'findVariable' now match exact start of varname
 * 
 * 
 */
#endregion

#region CHANGES to FCTB
/*
 * - FastColoredTextbox.cs / OnToolTip() : string hoveredWord = r.GetFragment("([a-zA-Z_])").Text;     // to allow underscores in tooltip keywords
 * 
 */
#endregion


#region Using
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using FastColoredTextBoxNS;
using Range = FastColoredTextBoxNS.Range;
using Numbers;
#endregion

namespace CalculatorNotepad
{
    public partial class FormCalculator : Form
    {

        #region Main Form Events handling

        ContextMenuStrip menu;
        ToolStripMenuItem mnuSave = null;
        Stopwatch swSaver = new Stopwatch();

        public FormCalculator()
        {
            // init components
            mc.Init();
            InitializeComponent();
            // prepare edit controls
            mcAutocompleteCS.Init(fbCSharp);
            mcAutocompleteNotepad.Init(fbNotepad);
            // create menus
            menu = new ContextMenuStrip();
            menu.Items.Add("Load notepad",null, menu_Load);
            menu.Items.Add("Save as", null, menu_Save_As);
            mnuSave = new ToolStripMenuItem("Save", null, menu_Save);
            mnuSave.Enabled = false;
            menu.Items.Add(mnuSave);
            menu.Items.Add("-");
            menu.Items.Add("Options", null, menu_Config);
            menu.Items.Add("-");
            menu.Items.Add("Test performance", null, menu_Test_Performance);
            menu.Items.Add("Test calculations", null, menu_Test_Calculation);
            menu.Items.Add("Test", null, menu_Test_Tmp);
            menu.Items.Add("-");
            menu.Items.Add("Help", null, menu_Help);
            // resize window and splitters
            if ((mc.cfg.lastWinPosition.Height > 0) && (mc.cfg.lastWinPosition.Width > 0))
            {
                addBusy();
                this.Top = mc.cfg.lastWinPosition.Top;
                this.Left = mc.cfg.lastWinPosition.Left;
                this.Height = mc.cfg.lastWinPosition.Height;
                this.Width = mc.cfg.lastWinPosition.Width;
                try
                {
                    doResize();
                    splitContainerAll.SplitterDistance = mc.cfg.lastHistWidth;
                    splitContainerCode.SplitterDistance = mc.cfg.lastEditWidth;
                    splitContainerCSharp.SplitterDistance = mc.cfg.lastCSheight;
                    if (!mc.cfg.CSerrorVisible) HideCSerror(false);
                    if (!mc.cfg.CSvisible) HideCSpanel(false);
                }
                catch
                {
                }
                if (mc.cfg.lastWinState == FormWindowState.Maximized)
                    WindowState = FormWindowState.Maximized;
                subBusy();
            }
            swSaver.Start();
        }



        private async void FormCalculator_Load(object sender, EventArgs e)
        {
            // calc values based on already drawn controls
            resultCharWidth = fbResults.Width / fbResults.CharWidth;
            // load preset, and then last used document
            if (LoadPreset() && mc.cfg.openLastOnStart)
            {
                var fileName = AppDomain.CurrentDomain.BaseDirectory + "\\_lastDocument.calc";
                await LoadFileAsync(fileName);
            }
            if (mc.notProperlyClosed) msg("Application was not closed properly last time, calculation is suspended until next text change !");
            focusNotepad();
        }


        void doResize()
        {
            // set bottom at end of window
            int margin = 4;
            int newHeight = ClientSize.Height - splitContainerAll.Location.Y - margin;
            int newWidth = ClientSize.Width - splitContainerAll.Location.X - margin;
            splitContainerAll.Size = new Size(newWidth, newHeight);
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            doResize();
        }


        static FormWindowState lastWinState = FormWindowState.Normal;
        private void FormCalculator_SizeChanged(object sender, EventArgs e)
        {
            // cach minimize/maximize, but NOT actual resize (since it is step-by-step here)
            if (WindowState != lastWinState)
            {
                lastWinState = WindowState;
                doResize();
            }
        }

        private void FormCalculator_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveLastDoc();
            mc.cfg.lastWinState = WindowState;
            if (WindowState == FormWindowState.Normal)
            {
                mc.cfg.lastWinPosition = new Rectangle(this.Left, this.Top, this.Width, this.Height);
                mc.cfg.lastHistWidth = splitContainerAll.SplitterDistance;
                mc.cfg.CSvisible = !splitContainerCode.Panel2Collapsed;
                mc.cfg.CSerrorVisible = !splitContainerCSharp.Panel2Collapsed;
                if (mc.cfg.CSvisible)
                    mc.cfg.lastEditWidth = splitContainerCode.SplitterDistance;
                if (mc.cfg.CSvisible && mc.cfg.CSerrorVisible)
                    mc.cfg.lastCSheight = splitContainerCSharp.SplitterDistance;
            }
            mc.cfg.notProperlyClosed = false;
            mc.cfg.Save();
        }

        //capture CTRL-arrow keys
        static char[] parenthesesOpenPairs = { '(', '[', '{' };
        static char[] parenthesesClosePairs = { ')', ']', '}' };
        static char[] ctrlNonPairs = { ',' };

        protected override bool ProcessCmdKey(ref Message mwssage, Keys keyData)
        {
            void moveCursorOverBrackets(int dx)
            {
                FastColoredTextBox rt = null;
                bool multiline = false;
                if (fbNotepad.Focused)
                    rt = fbNotepad;
                else
                if (fbCSharp.Focused)
                {
                    rt = fbCSharp;
                    multiline = true;
                }
                if (rt == null) return; // not notepad or CS edit
                var sel = rt.Selection;
                if (sel.Start != sel.End) return; // not single caret, but range selection
                int Line = sel.Start.iLine, Column = sel.Start.iChar;
                var thisLine = rt.Lines[Line];
                int openCount = 0;
                bool doWork = false;
                // helper function:  move column/line to next position, or mark end of doWork
                void nextPos()
                {
                    Column += dx;
                    if ((Column >= thisLine.Length) || (Column <= 0))
                        if (multiline)
                        {
                            if (dx > 0)
                            {
                                if (Line < rt.LinesCount - 1)
                                {
                                    Line++;
                                    thisLine = rt.Lines[Line];
                                    Column = 0;
                                }
                                else
                                    doWork = false;
                            }
                            else
                            {
                                if (Line > 0)
                                {
                                    Line--;
                                    thisLine = rt.Lines[Line];
                                    Column = thisLine.Length - 1;
                                }
                                else
                                    doWork = false;
                            }
                        }
                        else
                            doWork = false;
                }
                // check if it is some parentheses next to cursor
                if (dx < 0) Column--; // since cursor is shown to the left of its index
                if ((Column < thisLine.Length) && (Column >= 0))
                {
                    char openP, closeP;
                    // find what type of parentheses is at cursor, or if it is 'non parentheses' char like comma
                    int pIdx = Array.IndexOf(dx > 0 ? parenthesesOpenPairs : parenthesesClosePairs, thisLine[Column]);
                    // if it is valid parenthesis, find match
                    if (pIdx >= 0)
                    {
                        openP = parenthesesOpenPairs[pIdx];
                        closeP = parenthesesClosePairs[pIdx];
                        doWork = true;
                        // find matching parentheses
                        do
                        {
                            char ch = thisLine[Column];
                            if (ch == openP) openCount++;
                            if (ch == closeP) openCount--;
                            if (openCount != 0)
                                nextPos();
                        } while (doWork && (openCount != 0));
                    }
                    else
                    {
                        // or this is non-parentheses jump marker, like comma
                        pIdx = Array.IndexOf(ctrlNonPairs, thisLine[Column]);
                        if (pIdx >= 0)
                        {
                            openP = ctrlNonPairs[pIdx];
                            doWork = true;
                            nextPos(); // move pos, since next found will close
                            // find matching char
                            bool found = false;
                            if (doWork)
                                do
                                {
                                    char ch = thisLine[Column];
                                    if (parenthesesOpenPairs.Contains(ch)) openCount++;
                                    if (parenthesesClosePairs.Contains(ch)) openCount--;
                                    if ((openCount == 0) && (ch == openP))
                                        found = true;
                                    else
                                        nextPos();
                                    // prevent  moving cursor to next comma if broken parentheses: " |, abc ) + (d , "
                                    if ((openCount != 0) && (Math.Sign(openCount) != Math.Sign(dx))) doWork = false;
                                } while (doWork && !found);
                            // keep cursor on same side of comma, to allow multiple jumps
                            if (found) Column -= dx;
                        }
                    }
                    // if valid match found, move cursor
                    if (doWork)
                    {
                        if (dx > 0) Column++;
                        var newSel = new Range(rt, Column, Line, Column, Line);
                        rt.Selection = newSel;
                    }
                }
            }

            if (keyData == (Keys.Right | Keys.Control))
            {
                moveCursorOverBrackets(+1);
                return true;
            }
            else
            if (keyData == (Keys.Left | Keys.Control))
            {
                moveCursorOverBrackets(-1);
                return true;
            }
            else
            if (keyData == (Keys.Space | Keys.Control))
            {
                if ((mcAutocompleteNotepad.popupMenuNotepad != null) && fbNotepad.Focused)
                {
                    mc.showEntireAutocomplete = true;
                    mcAutocompleteNotepad.popupMenuNotepad.Show(true);
                    mc.showEntireAutocomplete = false;
                    return true;
                }
            }
            return base.ProcessCmdKey(ref mwssage, keyData);
        }

        #endregion


        #region Utils
        void msg(string sta)
        {
            eMsg.Text = sta;
            if (sta != "")
                eMsg.ForeColor = sta.SubStr(0, 2) == "OK" ? Color.Green : Color.Red;
            eMsg.Refresh();
        }

        private void HideCSpanel(bool visible)
        {
            splitContainerCode.Panel2Collapsed = !visible;
        }

        private void HideCSerror(bool visible)
        {
            splitContainerCSharp.Panel2Collapsed = !visible;
        }

        void focusNotepad(Range selection = null)
        {
            try
            {
                if (selection == null)
                {
                    // focus on last line, ONLY if Notepad not already focused on some other posiiton than (0,0)
                    var curSel = fbNotepad.Selection;
                    if (curSel.IsEmpty && (curSel.Start.iLine == 0) && (curSel.Start.iChar == 0))
                    {
                        var lastRange = fbNotepad.GetLine(fbNotepad.LinesCount - 1);
                        selection = new Range(fbNotepad, lastRange.End, lastRange.End);
                        fbNotepad.Selection = selection;
                    }
                }
            }
            catch { };
            fbNotepad.Focus();
        }

        void changeTBline(int line, string newLineText)
        {
            addBusy();
            FastColoredTextBox tb = fbNotepad;
            if ((tb == null) || (tb.LinesCount <= line)) return;
            var linSel = tb.GetLine(line);
            tb.Selection = linSel;
            tb.ClearSelected();
            tb.InsertText(newLineText);
            subBusy();
        }

        /// <summary>
        /// Mark error word on fb source box, based on error line and column.
        /// Both Line and Column are assumed to start from 0 ( so c# numbers need -- )
        /// </summary>
        /// <param name="errLine"></param>
        /// <param name="errColumn"></param>
        /// <param name="markBox"></param>
        private void markError(int errLine, int errColumn, FastColoredTextBox markBox)
        {
            // position at error line in c# code panel
            if ((markBox.LinesCount > errLine) && (errLine >= 0))
            {
                var srcText = markBox.Lines[errLine];
                // sanity check for column range
                if (errColumn < 0) errColumn = 0;
                if (errColumn > srcText.Length) errColumn = srcText.Length;
                int colLeft = errColumn;
                int colRight = errColumn;
                bool markWord()
                {
                    if ((srcText != "") && (colRight < srcText.Length) && char.IsLetterOrDigit(srcText[colLeft]))
                    {
                        while ((colLeft >= 0) && char.IsLetterOrDigit(srcText[colLeft])) colLeft--;
                        colLeft++;
                        while ((colRight < srcText.Length) && char.IsLetterOrDigit(srcText[colRight])) colRight++;
                        return true;
                    }
                    else
                        return false;
                }

                // mark word around error column
                if ((!markWord()) && (colLeft > 0))
                {
                    colLeft--;
                    markWord(); // try to mark word on left side if cursor not in middle of word
                }
                // create selection around that word
                var newSel = new Range(markBox, colLeft, errLine, colRight, errLine);
                markBox.Selection = newSel;
                markBox.Focus(); // set focus to box editor
                markBox.GoToLine(errLine - 4); // scroll to show line with error (also 3 lines above err )
            }
        }


        // mark error line on fbNotepad, and change error msg text to this one
        private void markNotepadError(int cLin, int linY = -1)
        {
            if (cLin >= 0)
            {
                if (linY < 0) linY = mc.lastTotalResult.cLines[cLin].sLine;
                var res = mc.lastTotalResult.parsedLines[cLin].res;
                var maxY = linY + mc.lastTotalResult.cLines[cLin].sNumLines - 1;
                if (res.isError)
                {
                    // sanity check error coordinate
                    var ePos = res.errPos;
                    if (ePos.Y < linY) { ePos.Y = linY; ePos.X = 0; }
                    else
                    if (ePos.Y > maxY) { ePos.Y = maxY; ePos.X = maxY < fbNotepad.LinesCount ? fbNotepad.Lines[maxY].Length : 100; }
                    if (ePos.Y < fbNotepad.LinesCount)
                    {
                        var srcText = fbNotepad.Lines[ePos.Y];
                        markError(ePos.Y, ePos.X, fbNotepad);
                        string errMsg = res.Text + ": " + res.errorText; // + "                 [ at line  " + (ePos.Y + 1) + ":" + (ePos.X + 1) + " ]   " + srcText;
                        msg(errMsg);
                    }
                }
            }
        }


        #endregion


        #region  MENUS

        // MENU BUTTONS
        private void pbMenu_Click(object sender, EventArgs e)
        {
            menu.Show(this, new Point(pbMenu.Location.X + 8, pbMenu.Location.Y + 8));
        }

        private void pbCSpanel_Click(object sender, EventArgs e)
        {
            HideCSpanel(splitContainerCode.Panel2Collapsed);
        }

        private void pbHelp_Click(object sender, EventArgs e)
        {
            menu_Help(sender, e);
        }

        private void pbClear_Click(object sender, EventArgs e)
        {
            if (isBusy())
            {
                Task.Delay(500);
                if (isBusy())
                    if (MessageBox.Show("Do you really want to clear all ?", "Calculator is busy", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        return;
            }
            inCalc_cnt = 0;
            addBusy(true);
            lastFileName = "";
            fbCSharp.Text = "";
            fbResults.Text = "";
            mcCompiler.Invalidate();
            fbNotepad.Text = "";
            HideCSpanel(false);
            processAll();
            subBusy();
            inCalc_cnt = 0;
        }


        // MENU ITEMS
        private async void menu_Load(object sender, EventArgs e)
        {
            if (isBusy()) return;
            var dlg = new OpenFileDialog();
            dlg.InitialDirectory = string.IsNullOrEmpty(mc.cfg.LastFileDirectory)? AppDomain.CurrentDomain.BaseDirectory : mc.cfg.LastFileDirectory;
            dlg.Filter = "txt files (*.txt)|*.txt|RTF files (*.rtf)|*.rtf|Calc files (*.calc)|*.calc|All files (*.*)|*.*";
            dlg.FilterIndex = 1;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                mc.cfg.LastFileDirectory = Path.GetDirectoryName(dlg.FileName);
                if (!await LoadFileAsync(dlg.FileName))
                    msg("Error loading " + dlg.FileName);
            }
            focusNotepad();
        }

        private void menu_Save_As(object sender, EventArgs e)
        {
            Save_As(true);
        }

        private void menu_Save(object sender, EventArgs e)
        {
            Save_As(false);
        }


        private void menu_Config(object sender, EventArgs e)
        {
            // save old notepad cursor position
            var oldSel = fbNotepad.Selection;
            // show config form
            new ConfigForm() { Owner = this }.ShowDialog();
            // to completely reload and reinit all
            mc.Init();
            LoadPreset();
            processAll();
            focusNotepad(oldSel);
        }


        private async void menu_Help(object sender, EventArgs e)
        {
            addBusy();
            fbCSharp.Text =
                "// ** EXAMPLE of C# User functions ( lang.ver. c#10 )" + Environment.NewLine +
                "" + Environment.NewLine +
                "// fibonacci standard recursive, slow" + Environment.NewLine +
                "int fibS(int n) => n <= 1 ? n : fibS(n - 1) + fibS(n - 2);" + Environment.NewLine +
                "" + Environment.NewLine +
                "" + Environment.NewLine +
                "// fibonacci with cache helper, much faster " + Environment.NewLine +
                "// type 'nm.' to get autocomplete suggestion for other calculator specific" + Environment.NewLine +
                "// functions like nm.rnd.Next(N)  or  nm.nCr(n,r) etc" + Environment.NewLine +
                "double fibC(int n)" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    if (nmCache.Try(\"fibC\", n)) return nmCache.Result;" + Environment.NewLine +
                "    var res= n <= 1 ? n : fibC(n - 1) + fibC(n - 2);" + Environment.NewLine +
                "    return nmCache.Add(\"fibC\", n, res);" + Environment.NewLine +
                "}"
            ;
            HideCSpanel(true);
            mcCompiler.Invalidate();
            fbNotepad.Text =
                "//********************************************************************************************************" + Environment.NewLine +
                "//** Calculator Notepad " + Environment.NewLine +
                "//** panels: Notepad (center), Results(left), C#code(optional right), Error/Dbg(optional lower right) " + Environment.NewLine +
                "//**    - simple use of math functions, variables and user defined functions " + Environment.NewLine +
                "//**    - support using of units ('kg','in'..), their conversion and user defined units" + Environment.NewLine +
                "//**    - support vectors and many functions working on vectors (vec,vDim,vFunc,vSum...)" + Environment.NewLine +
                "//**    - provide random generating functions for simulations ( random, rndChoose/Weighted ...)  " + Environment.NewLine +
                "//**    - allows easy definition of new user functions in single line" + Environment.NewLine +
                "//**        - notepad user functions can be recursive (notepad does auto cache optimization) and multiline {}" + Environment.NewLine +
                "//**    - allow definition of C# user functions (in right C# panel, enabled by 2nd toolbar icon )" + Environment.NewLine +
                "//**        - instantly usable in notepad, allow for complex/faster functions" + Environment.NewLine +
                "//**    - Syntax Highlighting of both Notepad and C# panels" + Environment.NewLine +
                "//**        - matching parentheses are highlighted, use Ctrl-Arrows to jump between them" + Environment.NewLine +
                "//**    - Autocomplete for Notepad and C# panels" + Environment.NewLine +
                "//**        - Notepad autocomplete also show help/descriptions for builtin functions" + Environment.NewLine +
                "//**        - Ctrl-Space to show all, or automaticaly shown after first characters" + Environment.NewLine +
                "//**    - Menu (leftmost toolbar icon) allows Load,Save and Options settings" + Environment.NewLine +
                "//**        - Preset file allow permanent user defined functions and constants" + Environment.NewLine +
                "//********************************************************************************************************" + Environment.NewLine +
                "" + Environment.NewLine +
                "   f(x)= 3*x      // simple definition of user functions, without types (int,double...) or blocks {} overhead" + Environment.NewLine +
                "   z= 2+f(5)      // equally simple definition of user variables, and using of previously defined functions" + Environment.NewLine +
                "" + Environment.NewLine +
                "//** EXAMPLES  " + Environment.NewLine +
                "123 / 4    //  press ENTER after 123/4 to calculate " + Environment.NewLine +
                "last*5     //  type *5 : using */+-^ etc on empty line append last result " + Environment.NewLine +
                "// Built-in functions ( c# Math f-ons, vector f-ons, random f-ons ...), constants (pi,e)" + Environment.NewLine +
                "5!+3 * sin(pi / 2) - e ^ 2" + Environment.NewLine +
                "5.5!    // float factorial, using Gamma function" + Environment.NewLine +
                "vv = vDim(5, (i) => i ^ 2)  // create vector with 5 elemenets, using lambda function" + Environment.NewLine +
                "vv[2..4] ∩ vec(3, 4, 4, 9, 10)  // intersection of two vectors, vv[2]==4, vv[2..4]==vec(4,9,16)" + Environment.NewLine +
                "if (2 / 3 < 1, 33, 44)    //  if (2/3<1) then 33 else 44 " + Environment.NewLine +
                "integral((x) => sin(x), 0, 0.7) // numerical integration (approximate) " + Environment.NewLine +
                "// statistical functions and simulations" + Environment.NewLine +
                "pmf(dist_binomial(10, 50 %),2 ) // probability to have exactly 2 Tails after 10 coin tosses" + Environment.NewLine +
                "pSim(() => 2≡∑((i) => rnd < 0.5, 1, 10), 10000) // simulating same coin tosses 10k times and counting 'just 2 tails'" + Environment.NewLine +
                "// Units (length,time,weight units, deg/rad etc)" + Environment.NewLine +
                "100'kmh->mph'  // converts to desired units, '->' or '>' " + Environment.NewLine +
                "22'lb/in^2'    // auto converts to SI units, here 'kg/m^2' , and demonstrate complex units " + Environment.NewLine +
                "sin(90'deg')   // trig functions require radians, use 'deg' to easily convert to radians if needed" + Environment.NewLine +
                "// User defined variables, constants and functions" + Environment.NewLine +
                "AU = 149.6e6'km'      // user defined variable or constant" + Environment.NewLine +
                "1`000`000 'ly' / AU   // using constant, and also using ` (reverse apostrophe) for digit group separation" + Environment.NewLine +
                "tmul(L, Va, Vc) = {  /// multiline user f-on. Hover over func name to see custom tooltip ( defined by /// )" + Environment.NewLine +
                "          t= (e ^ (Vc / Va) - 1) ;   // in {} blocks, use semicolon ';' to separate expressions " + Environment.NewLine +
                "          L / Vc * t                 // in blocks, last expression is result value ( or use 'return x;' ) " + Environment.NewLine +
                "}" + Environment.NewLine +
                "tmul(0.1'mi', 0.1'mph', 10'mi/h') / 1'year'     // using user f-on with non-Si units" + Environment.NewLine +
                "fib(x) = if (x≤1,x,fib(x - 1) + fib(x - 2))  // fibonacci f-on with recursion. uses autocaching optimization" + Environment.NewLine +
                "fib(205)   // using user f-on , demonstrate autocache speedup" + Environment.NewLine +
                "fibC(205)  // using C# user function from right panel, or fibS(205) for slow version" + Environment.NewLine
             ;
            focusNotepad();
            subBusy();
            await processAllAsync();
        }

        #endregion


        #region TESTS

        private void menu_Test_Performance(object sender, EventArgs e)
        {
            // performance test
            processAll();
            var sw = new Stopwatch();
            int N = 10;
            long tmCalcHigh = 0, tmDraw = 0;
            int CursorLine = !mc.cfg.disablePartialParse ? fbNotepad.Selection.Start.iLine : -1;
            var chgRange = new LineRange(CursorLine);
            sw.Start();
            for (int i = 0; i < N; i++)
            {
                long t1 = sw.ElapsedMilliseconds;
                var ARes = mc.calcLines(fbNotepad.Text, chgRange);
                long t2 = sw.ElapsedMilliseconds;
                redrawLines(ARes);
                tmDraw += sw.ElapsedMilliseconds - t2;
                tmCalcHigh += t2 - t1;
            }
            long tmAll = sw.ElapsedMilliseconds;
            // test compile separatelly, since it is not part of standard calc/draw cycle
            sw.Restart();
            for (int i = 0; i < N; i++)
                mc.recompileCS(fbCSharp.Text, true);
            var tmCompile = sw.ElapsedMilliseconds;
            sw.Stop();
            processAll();
            //string m(string name, long ms) { return name + "= " +   (ms / (double)N)+"   ";  }
            string m(string name, long ms) { return String.Format("{0}= {1,5:0.0}", name, ms / (double)N)+" ms    "; }
            var resMsg = m("CalcH", tmCalcHigh) + m("Draw", tmDraw) + m("BOTH", tmAll)+ m("Compile",tmCompile);
            // **OLD Results**
            // CalcH=  30.6    Draw=  36.8    ALL=  67.6        ;   Examples    TOP
            // CalcH=   0.4    Draw=  31.6    ALL=  32          ;   Examples    BOTTOM (calc skipped due to optimization)
            // CalcH= 186.9    Draw=  73.1    ALL= 260.5        ;   Test        TOP    - long calc since test page is long and has many sums/integrals/recursions... Also Draw is twice as long, proportional to num lines
            // CalcH=   1.8    Draw=  72.3    ALL=  74.3        ;   Test        BOTTOM (calc skipped due to optimization)

            // **New Results**
            // CalcH=  27.3    Draw=  65.8    ALL=  93.1        ;   Examples    TOP
            // CalcH=   0.6    Draw=  62.6    ALL=  63.2        ;   Examples    BOTTOM
            // CalcH = 56.2    Draw = 80.4    ALL = 136.6       ;   Test        TOP
            // CalcH =  0.4    Draw = 77.7    ALL = 78.1        ;   Test        BOTTOM
            msg(resMsg);

        }

        private void menu_Test_Calculation(object sender, EventArgs e)
        {
            // temporary set format to default, since expected results are in that format
            var oldSeparator = mc.cfg.resFormatSeparator;
            var oldDecimals = mc.cfg.resFormatDecimals;
            mc.cfg.resFormatSeparator = "";
            mc.cfg.resFormatDecimals = -1;
            // do test
            if (LoadFile("_TESTS.txt"))
            {
                string err = "";
                // test each line 
                int i = 0;
                string line = "";
                for (; (err == "") && (i < mc.lastTotalResult.cLines.Length); i++)
                {
                    line = mc.lastTotalResult.cLines[i].cLineText.Trim().ToUpper();
                    // get expected result from comment. If no comment, or lin starts with comment, ignore line
                    int cp = line.IndexOf("//");
                    if (cp > 0)
                    {
                        var expectedResults = line.SubStr(cp + 2).Trim();
                        line = line.SubStr(0, cp);
                        var actualRes = mc.lastTotalResult.parsedLines[i].res.Text.ToUpper();
                        cp = expectedResults.IndexOf("//"); // check for comments within comments
                        if (cp > 0) expectedResults = expectedResults.SubStr(0, cp);
                        cp = expectedResults.IndexOf(";"); // treat as comment also
                        if (cp > 0) expectedResults = expectedResults.SubStr(0, cp);
                        // func to compare with one expected result
                        string cmpRes(string expectedRes)
                        {
                            // compare results
                            if (expectedRes.SubStr(0, 1) == "~")
                            {
                                // for random simulation stuff, just check if double result is within 20%
                                double dExp, dAct;
                                if (double.TryParse(expectedRes.SubStr(1), out dExp) && double.TryParse(actualRes, out dAct))
                                {
                                    if (dExp == 0)
                                    {
                                        if (dAct != 0) return "Actual value is not close to zero";
                                    }
                                    else if (Math.Abs((dExp - dAct) / dExp) > 0.2)
                                        return "Actual value " + actualRes + " is not close to expected value " + expectedRes;
                                }
                                else
                                    return "Not valid double numbers for ~ approximate comparison !";
                            }
                            else
                            {
                                // split around exponent
                                var expParts = expectedRes.Split("E");
                                var actParts = actualRes.Split("E");
                                // if expected ends with ..., ignore result digits after those
                                if (expParts[0].EndsWith("..."))
                                {
                                    expParts[0] = expParts[0].SubStr(0, expParts[0].Length - 3);
                                    actParts[0] = actParts[0].SubStr(0, expParts[0].Length);
                                }
                                // recombine
                                expectedRes = String.Join('E', expParts);
                                var newActualRes = String.Join('E', actParts);
                                // for direct comparisons, without changeable values
                                if (expectedRes != newActualRes)
                                    return "Actual value " + actualRes + " is different than expected value " + expectedRes;
                            }
                            return ""; // same = match
                        }
                        // split expected resluts around '|' for multiple matches
                        var parts = expectedResults.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        err = "";
                        foreach (var part in parts)
                        {
                            err = cmpRes(part);
                            if (err == "") break; // any match is returned as match, otherwise last err returned
                        }
                    }
                    if (err != "") break;
                }
                // show result
                if (err != "")
                {
                    msg(err + "   @ line " + i + " : " + line);
                    fbNotepad.Selection = new Range(fbNotepad, i);
                }
                else
                    msg("OK:  calculation test did not find any differences.");

            }
            else
                msg("Unable to test: Could not find file _TESTS.txt in local folder !");
            // return old format
            mc.cfg.resFormatSeparator = oldSeparator;
            mc.cfg.resFormatDecimals = oldDecimals;
        }


        private void menu_Test_Tmp(object sender, EventArgs e)
        {

        }

        #endregion


        #region Files Load/Save

        public string lastFileName
        {
            get { return mc.cfg.LastFileName??""; }
            set
            {
                mc.cfg.LastFileName = value.Trim();
                Text = "Calculator Notepad" + (mc.cfg.LastFileName != "" ? " - " + Path.GetFileName(mc.cfg.LastFileName) : "");
                if (mnuSave != null) mnuSave.Enabled = (mc.cfg.LastFileName != "");
            }
        }

        private bool LoadPreset()
        {
            mc.CSpresetSource = "";
            if (mc.cfg.PresetFile != null)
            {
                var fileName = mc.cfg.PresetFile.Trim();
                string srcLeft, srcRight;
                if (LoadFileSources(fileName, out srcLeft, out srcRight))
                {
                    //var cRes = mc.calcLines(srcLeft);
                    mcTotalResult cRes;
                    string compileErr;
                    // compile/interpret to see if preset is good - but do not add c# functions to list
                    bool goodPreset = cpuProcessAll(0, true, srcLeft, srcRight, out cRes, out compileErr, false);
                    if (!goodPreset)
                    {
                        // error in preset, do not store it, but show ereror and load it main edit
                        LoadFile(fileName);
                        MessageBox.Show("Correct error and save over old preset file, or select other preset file in Configuration !", "Error in Preset File : " + fileName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    else
                    {
                        // this was valid preset, so update reset pointers and store preset c#
                        mc.MarkAllFuncAsSystem();   // make even user supplied preset functions as inbuilt
                        mc.StoreInit();             // store save point that will be used on each new calculation start (meaning preset expressions will not be recalculated). 
                        mc.CSpresetSource = Environment.NewLine + srcRight + Environment.NewLine;  // But preset c# will still be recompiled every time , so it is stored here
                    }
                }
                else
                {
                    mc.CSpresetSource = "";
                    if (fileName != "")
                        MessageBox.Show("Select other preset file in Configuration, or remove it !", "Preset File does not exists : " + fileName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            mcCompiler.Invalidate();
            return true;
        }


        // merge  notepad and c# part and save into file 
        private bool SaveFile(string fileName)
        {
            string text;
            // get notepad source in desired format
            var ext = System.IO.Path.GetExtension(fileName).ToLower();
            if (ext == ".rtf")
                text = fbNotepad.Rtf;
            else
                text = fbNotepad.Text;
            // append c# source if any, in plain text format
            if (mc.SaveFileBasic(fileName, text, fbCSharp.Text))
            {
                if (Path.GetFileName(fileName) != "_lastDocument.calc") lastFileName = fileName;
                return true;
            }
            return false;
        }



        private void SaveLastDoc()
        {
            // save last document if needed
            if (mc.cfg.openLastOnStart)
                SaveFile(AppDomain.CurrentDomain.BaseDirectory + "\\_lastDocument.calc");
        }


        // load file sources, and split c# part
        private bool LoadFileSources(string fileName, out string srcLeft, out string srcRight)
        {
            srcLeft = "";
            srcRight = "";
            if (File.Exists(fileName))
                try
                {
                    string readText = File.ReadAllText(fileName);
                    // detect if there is c# part
                    int pc = readText.IndexOf(mc.csMarker);
                    if (pc >= 0)
                    {
                        srcLeft = readText.Substring(0, pc);
                        srcRight = readText.SubStr(pc + mc.csMarker.Length);
                    }
                    else
                    {
                        srcLeft = readText;
                        srcRight = "";
                    }
                    return true;
                }
                catch
                {
                }
            return false;

        }

        // load file, set edits,  and recompile/recalculate
        private bool preLdFile(string fileName)
        {
            string srcLeft, srcRight;
            if (LoadFileSources(fileName, out srcLeft, out srcRight))
                try
                {

                    addBusy(true); // to prevent events while changing values of edits
                    // set c# part and invalidate any previous compilation 
                    fbCSharp.Text = srcRight;
                    mcCompiler.Invalidate(); // also invalidate mid store
                    // hide c# panel if no c# code, otherwise show it
                    HideCSpanel(srcRight != "");
                    // set notepad edit part, depending of saved format
                    var ext = System.IO.Path.GetExtension(fileName).ToLower();
                    if (ext == ".rtf")
                    {
                        throw new Exception("Can not load RTF files, only Save is supported for RTF!");
                        //fbNotepad.Rtf= srcLeft;
                    }
                    else
                        fbNotepad.Text = srcLeft;
                    if (Path.GetFileName(fileName) != "_lastDocument.calc")
                    {
                        lastFileName = fileName;
                        msg(""); // to show new file name
                    }
                    return true;
                }
                finally
                {
                    subBusy();
                }
            return false;

        }


        private bool LoadFile(string fileName)
        {
            if (preLdFile(fileName))
            {
                processAll();
                return true;
            }
            return false;
        }

        private async Task<bool> LoadFileAsync(string fileName)
        {
            if (preLdFile(fileName))
            {
                await processAllAsync();
                return true;
            }
            return false;
        }


        private void Save_As(bool isSaveAs)
        {
            if (isBusy()) return;
            if ((lastFileName == "") || isSaveAs)
            {
                var dlg = new SaveFileDialog();
                dlg.Filter = "txt files (*.txt)|*.txt|RTF files (*.rtf)|*.rtf|Calc files (*.calc)|*.calc|All files (*.*)|*.*";
                dlg.FilterIndex = 1;
                if (lastFileName != "")
                {
                    dlg.InitialDirectory = Path.GetDirectoryName(lastFileName);
                    dlg.FileName = Path.GetFileName(lastFileName);
                    // set proper filterindex
                    var knownExt = new string[] { ".txt", ".rtf", ".calc" };
                    var thisExt = Path.GetExtension(lastFileName).ToLower();
                    int idx = Array.IndexOf(knownExt, thisExt);
                    if (idx >= 0)
                        dlg.FilterIndex = idx + 1;
                    else
                        dlg.FilterIndex = 4; // all files if unknown
                }
                if (string.IsNullOrEmpty(dlg.InitialDirectory))
                    dlg.InitialDirectory = string.IsNullOrEmpty(mc.cfg.LastFileDirectory) ? AppDomain.CurrentDomain.BaseDirectory : mc.cfg.LastFileDirectory;
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    mc.cfg.LastFileDirectory = Path.GetDirectoryName(dlg.FileName);
                    if (!SaveFile(dlg.FileName))
                        msg("Error saving " + dlg.FileName);
                }
            }
            else
            {
                if (!SaveFile(lastFileName))
                    msg("Error saving " + lastFileName);
            }
        }


        #endregion


        #region Busy protection and Spinner
        int inCalc_cnt = 0;

        bool isBusy() { return inCalc_cnt > 0; }
        void addBusy(bool addSpinner = false)
        {
            if (inCalc_cnt >= 0) inCalc_cnt++; else inCalc_cnt = 1;
            if (addSpinner)
            {
                Application.UseWaitCursor = true; // instead of  Cursor.Current = Cursors.WaitCursor; since second one does not show while processing
                Cursor.Current = Cursors.WaitCursor;
            }
        }
        void subBusy()
        {
            if (inCalc_cnt > 0) inCalc_cnt--; else inCalc_cnt = 0;
            //if ((inCalc_cnt == 0) && (Cursor != Cursors.Default)) Cursor = Cursors.Default;
            if ((inCalc_cnt == 0) && Application.UseWaitCursor)
            {
                Application.UseWaitCursor = false;
                Cursor = Cursors.Default; // in addition to above, since without it cursor remains wait until mouse move
            }
        }
        bool enterBusy(bool addSpinner = false)
        {
            if (isBusy()) return false;
            addBusy(addSpinner);
            return true;
        }
        void leaveBusy() { subBusy(); }


        #endregion


        #region Redraw Result panel

        static int resultCharWidth = 28;
        static readonly Style BlueStyle = new TextStyle(Brushes.Blue, null, FontStyle.Regular);
        static readonly Style GreenStyle = new TextStyle(Brushes.Green, null, FontStyle.Italic);
        static readonly Style RedStyle = new TextStyle(Brushes.Red, null, FontStyle.Regular);
        static readonly Style BlackStyle = new TextStyle(Brushes.Black, null, FontStyle.Regular);
        static readonly Style GrayStyle = new TextStyle(Brushes.Gray, null, FontStyle.Regular);


        // right allign string to exactly given length, and put ... as last char if overflow
        string rightAlign(string input, int Len)
        {
            if (Len <= 0) return "";
            // make result right aligned
            if (Len > input.Length)
                input = input.PadLeft(Len);
            if (input.Length > Len)
                input = input.SubStr(0, Len - 1) + "…";
            return input;
        }

        // append colored line to result, and right-align it
        void appendResultLine(string input, Color color, string info)
        {
            // if info on its own takes too much space, remove it
            if (info.Length > resultCharWidth / 2)
                info = "";
            // make result right aligned
            int maxResLen = resultCharWidth - info.Length;
            input = rightAlign(input, maxResLen);
            // define result color
            Style rowStyle;
            if (color == Color.Black) rowStyle = BlackStyle;
            else if (color == Color.Red) rowStyle = RedStyle;
            else if (color == Color.Green) rowStyle = GreenStyle;
            else rowStyle = BlueStyle;
            // append
            if (info == "")
                fbResults.AppendText(input + Environment.NewLine, rowStyle);
            else
            {
                fbResults.AppendText(input, rowStyle);
                fbResults.AppendText(info + Environment.NewLine, GrayStyle);
            }
        }


        // redraw result box and error box based on calculation result
        void redrawLines(mcTotalResult cRes)
        {
            addBusy();
            // change notepad syntax highlighting
            (fbNotepad.SyntaxHighlighter as NotepadHighlighter).UpdatedRegex();
            // recreate results
            fbResults.Text = "";
            int showTime0 = mc.cfg.showExecutionTime ? 2 : 0; // 0=do not show, 1= 0b001= show short(s), 2= 0b001x= show long(ms)
            if (cRes.errorTxt.Contains("Timeout")) showTime0 |= 1; // if document timeout, show time for all rows
            int nLines = cRes.parsedLines.Count;
            for (int i = 0; i < nLines; i++)
            {
                // format result line, insert time if needed
                var resText = cRes.parsedLines[i].res.Text;
                int showTime = showTime0;
                if (resText.Contains("Timeout")) showTime |= 1; // if single line is timeout, show time for that line even if globaly it is not shown
                if ((cRes.parsedLines[i].res.time_ms == 0) && (cRes.parsedLines[i].res.Text == "")) showTime = 0; // do not show time if both zero and empty line (so show time for empty lines that take time )
                string resInfo = "";
                if (showTime > 1)
                    resInfo = string.Format("{0,5}ms", cRes.parsedLines[i].res.time_ms);
                else if (showTime > 0)
                    resInfo = string.Format("{0,4:0.0}s", cRes.parsedLines[i].res.time_ms / 1000.0);
                // append to result edit
                appendResultLine(resText, cRes.parsedLines[i].res.color, resInfo);
                // if this was multiline, append additional empty lines
                for (int a = 1; a < cRes.cLines[i].sNumLines; a++)
                    fbResults.AppendText(Environment.NewLine);
                // if this was first error, remember line
            }

            // set scroll positions
            fbResults.HorizontalScroll.Value = fbResults.HorizontalScroll.Maximum;
            scrollResultsToNotepadVertical();
            // display error
            if (cRes.errnum != 0)
            {
                if (mc.cfg.autoFocusError)
                    markNotepadError(cRes.errorLineNum);
                else
                    msg(cRes.errorTxt);
            }
            else
                msg("");
            // reset change indicators
            resetChangeMonitors();
            subBusy();
        }


        // change text of fbNotepad if needed, replacing to greek symbols
        void autoReplace()
        {
            string text = "";
            void repR(string regex, string replacement) // regex replacement
            {
                text = Regex.Replace(text, regex, replacement);
            }
            void repW(string old, string replacement) // word replacement
            {
                text = Regex.Replace(text, @"\b" + old + @"\b", replacement);
            }
            void rep(string old, string replacement) // any match replacement
            {
                text = text.Replace(old, replacement);
            }

            if (mc.cfg.replaceKnownSymbols)
                try
                {
                    text = fbNotepad.Text;
                    var oldText = text;
                    rep(">=", "≥");
                    rep("<=", "≤");
                    rep("!!", "‼");
                    rep("!=", "≠");
                    rep("==", "≡");
                    rep("->", "→");
                    repR(@"(\S +)\.\.(\S +)", "…");
                    repW("pi", "π");
                    repW("sqrt", "√");
                    repW("sum", "∑");
                    repW("product", "∏");
                    repW("integral", "∫");
                    repW("intersect", "∩");
                    repW("union", "U");
                    if (text != oldText)
                    {
                        addBusy();
                        var oldSel = fbNotepad.Selection.Clone();
                        fbNotepad.Text = text;
                        fbNotepad.Selection = oldSel;
                        subBusy();
                    }
                }
                catch { }
        }



        #endregion


        #region ProcessAll calculations
        // **** PROCESS / RUN notepad 


        static bool inProcessAll = false;

        void preProcessAll(out int CursorLine, out bool needCompile, out string sourceLeft, out string sourceRight)
        {
            inProcessAll = true;
            // save last doc if not saved in long time (10sec)
            if (swSaver.ElapsedMilliseconds > 10000)
            {
                SaveLastDoc();
                swSaver.Restart();
            }
            // get what I need from UI thread
            addBusy(true);
            CursorLine = !mc.cfg.disablePartialParse ? fbNotepad.Selection.Start.iLine : -1;
            changedRange.currentLine = CursorLine;
            needCompile = !mcCompiler.isValid();
            // check if auto-replacements are needed
            autoReplace();
            // get notepad text
            sourceLeft = fbNotepad.Text;
            sourceRight = needCompile ? fbCSharp.Text : "";
            // clear compile/debug results
            fbErrors.Text = "";
        }

        void postProcessAll(bool needCompile, string compileErr, mcTotalResult cRes)
        {
            if (needCompile)
            {
                fbErrors.Text = compileErr;
                HideCSerror(compileErr != "");
                if (compileErr != "")
                    msg("Syntax Error in C# compilation");
            }
            redrawLines(cRes);
            if ((cRes.debugLines != "") && (fbErrors.Text == ""))
            {
                fbErrors.Text = cRes.debugLines;
                HideCSpanel(true);
                HideCSerror(true);
            }
            subBusy();
            inProcessAll = false;
        }

        bool cpuProcessAll(int CursorLine, bool needCompile, string sourceLeft, string sourceRight, out mcTotalResult cRes, out string compileErr, bool addNewFunctions = true)
        {
            compileErr = needCompile ? mc.recompileCS(sourceRight, addNewFunctions) : "";
            cRes = mc.calcLines(sourceLeft, changedRange);
            return (cRes.errnum == 0) && (compileErr == "");
        }

        // compile fully or partially c# and notepad (in separate thread), replace to symbols if needed, and redraw results
        void processAll()
        {
            if (mc.notProperlyClosed && (swSaver.ElapsedMilliseconds > 3000)) 
                mc.notProperlyClosed = false;
            if (!inProcessAll && !mc.notProperlyClosed)
            {
                // preprocess
                int CursorLine;
                bool needCompile;
                string sourceLeft, sourceRight, compileErr;
                mcTotalResult cRes;
                preProcessAll(out CursorLine, out needCompile, out sourceLeft, out sourceRight);
                // do sync
                cpuProcessAll(CursorLine, needCompile, sourceLeft, sourceRight, out cRes, out compileErr);
                // postprocess, change GUI elements
                postProcessAll(needCompile, compileErr, cRes);
            }
        }

        async Task processAllAsync()
        {
            if (mc.notProperlyClosed && (swSaver.ElapsedMilliseconds > 3000)) mc.notProperlyClosed = false;
            if (!inProcessAll && !mc.notProperlyClosed)
            {
                // preprocess
                int CursorLine;
                bool needCompile;
                string sourceLeft, sourceRight, compileErr = "";
                mcTotalResult cRes = null;
                preProcessAll(out CursorLine, out needCompile, out sourceLeft, out sourceRight);
                // do async
                await Task.Run(() =>
                {
                    MPFR.CheckDefaultPrecision(); // since this will run on different thread, and MPFR has defaults per thread
                    cpuProcessAll(CursorLine, needCompile, sourceLeft, sourceRight, out cRes, out compileErr);
                }
                );
                // postprocess, change GUI elements
                postProcessAll(needCompile, compileErr, cRes);
                // test if maybe source was changed while work was async done
                if ((mc.lastTotalResult != null) && (mc.lastTotalResult.sourceHash != fbNotepad.Text.GetHashCode()))
                    BusyProcessAll();
            }
        }

        // process all if not already in busy section
        void BusyProcessAll()
        {
            if (!isBusy())
            {
                addBusy();
                processAll();
                resetChangeMonitors();
                subBusy();
            }
        }

        #endregion


        #region Edit fields Events ( excluding Notepad )

        int lastNumLines = 1;
        LineRange changedRange = new LineRange();
        bool needRecalc = false;
        Stopwatch tmChanges = new Stopwatch();


        void resetChangeMonitors()
        {
            changedRange = new LineRange();
            lastNumLines = fbNotepad.LinesCount;
            needRecalc = false;
            tmChanges.Restart();
        }

        void scrollResultsToNotepadVertical()
        {
            int v = fbNotepad.VerticalScroll.Value;
            if (v >= fbResults.VerticalScroll.Maximum)
                fbResults.VerticalScroll.Maximum = fbNotepad.VerticalScroll.Maximum;
            fbResults.VerticalScroll.Value = v;
            fbResults.Invalidate();
        }

        private void fbResults_SizeChanged(object sender, EventArgs e)
        {
            if (fbResults.CharWidth > 0)
            {
                resultCharWidth = fbResults.Width / fbResults.CharWidth;
                if (!isBusy()) processAll(); // to redraw after resize
            }
        }

        // show full (error or otherwise) text for this Result in hover tooltip
        private void fbResults_ToolTipNeeded(object sender, ToolTipNeededEventArgs e)
        {
            if (mc.lastTotalResult == null) return;
            var p = fbResults.PointToPlace(fbResults.PointToClient(Cursor.Position));
            var cLin = compLine.mapSourceLine(mc.lastTotalResult.cLines, p.iLine);
            if (cLin >= 0)
            {
                var res = mc.lastTotalResult.parsedLines[cLin].res;
                if (res.isError)
                {
                    e.ToolTipText = (mc.cfg.showExecutionTime ? "[" + res.errPos + "]" : "") + res.errorText + Environment.NewLine + "( double-click here to select this error in notepad source )";
                }
                else
                {
                    if (res.isValue)
                        e.ToolTipText = res.Value.ToString("", -1, true); // show value with data type
                    else
                        e.ToolTipText = res.Text; // show result text
                }
            }
        }

        // mark error line on fbNotepad, and change error msg text to this one
        private void fbResults_DoubleClick(object sender, EventArgs e)
        {
            int linY = fbResults.Selection.Start.iLine;
            var cLin = compLine.mapSourceLine(mc.lastTotalResult.cLines, linY);
            markNotepadError(cLin, linY);
        }

        private void fbCSharp_Leave(object sender, EventArgs e)
        {
            // this recompile CS panel code and notepad, upon leaving CS panel
            mcCompiler.Invalidate();
            BusyProcessAll();
        }

        private void fbErrors_DoubleClick(object sender, EventArgs e)
        {
            var line = fbErrors.Selection.Start.iLine;
            var err = fbErrors.Lines[line];
            if (err.SubStr(0, 1) == "[")
            {
                mcParse.removeStart(ref err, 1);
                var strLine = mcParse.extractNumber(ref err);
                if (err.SubStr(0, 1) == ":")
                {
                    mcParse.removeStart(ref err, 1);
                    var strColumn = mcParse.extractNumber(ref err);
                    int errLine, errColumn;
                    if (int.TryParse(strLine, out errLine) && int.TryParse(strColumn, out errColumn))
                    {
                        // both lines and column numberings start from one
                        errLine--;
                        errColumn--;
                        // position at error line in c# code panel and mark word
                        markError(errLine, errColumn, fbCSharp);
                    }
                }
            }
        }

        #endregion


        #region Notepad Edit field Events

        private void fbNotepad_TextChanged(object sender, TextChangedEventArgs e)
        {
            int lineY = fbNotepad.Selection.Start.iLine;
            changedRange.updateRange(lineY);
            // manage appending last result
            if (mc.cfg.autoLastResultAppend && !isBusy() && (mc.lastTotalResult.parsedLines != null))
            {
                var thisLine = fbNotepad.Lines[lineY];
                var Column = fbNotepad.Selection.Start.iChar;
                if (thisLine != "")
                {
                    // if started with operation, insert last result
                    if ((thisLine.Length == 1) && (Column == 1) && ("+-*/^!%&|<>=".IndexOf(thisLine[0]) >= 0))
                    {
                        if ((lineY > 0) && (lineY <= mc.lastTotalResult.parsedLines.Count))
                        {
                            var lastRes = mc.getLastResult(mc.lastTotalResult.parsedLines, lineY - 1).Value;
                            //changeTBline(lineY, lastRes + thisLine);
                            changeTBline(lineY, "last" + thisLine);
                        }
                    }
                    else
                    // if double -- or // 
                    if ((lineY > 0) && (lineY <= mc.lastTotalResult.parsedLines.Count))
                    {
                        var doubleChar = thisLine.SubStrNeg(-2, 2);
                        if ((doubleChar == "--") || (doubleChar == "//"))
                        {
                            // only replace if exactly last result precedes it
                            var lastRes = mc.getLastResult(mc.lastTotalResult.parsedLines, lineY - 1).Value;
                            var leftSide = thisLine.SubStr(0, thisLine.Length - 2);
                            if ((leftSide == "last") || (leftSide == CalcResult.ToString(lastRes)))
                            {
                                // if double minus pressed at start of line, make it single negative prefix, instead of "lastResult--"
                                if (doubleChar == "--")
                                    changeTBline(lineY, "-");
                                else
                                // if double slash (//) pressed at start of line, it is start of comment, so remove lastResult
                                if (doubleChar == "//")
                                    changeTBline(lineY, "//");
                            }
                        }
                    }
                }
            }

        }

        private void fbNotepad_SelectionChanged(object sender, EventArgs e)
        {
            var Line = fbNotepad.Selection.Start.iLine;
            // mark for (future) recalc if:
            if (
                    ((changedRange.startLine >= 0) && (changedRange.startLine != Line)) ||  // - we moved away from changed line
                    (fbNotepad.LinesCount != lastNumLines) ||               // - total number of lines changed
                    (changedRange.startLine != changedRange.endLine)                        // - more than one line was changed
               )
                needRecalc = true;
        }

        private void fbNotepad_SelectionChangedDelayed(object sender, EventArgs e)
        {
            // this recalculate notepad results upon leaving current line, if that line was changed
            if (needRecalc)
            {
                BusyProcessAll();
            }
        }

        private void fbNotepad_ScrollbarsUpdated(object sender, EventArgs e)
        {
            scrollResultsToNotepadVertical();
        }

        // hower mouse over word to get tooltip
        private void fbNotepad_ToolTipNeeded(object sender, ToolTipNeededEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.HoveredWord))
            {
                ToolTipClass tt = mc.getToolTips(e.HoveredWord);
                // search in functions
                if (tt.valid)
                {
                    e.ToolTipTitle = tt.Title;
                    e.ToolTipText = tt.Text;
                }
            }
        }



        #endregion

    }
}
