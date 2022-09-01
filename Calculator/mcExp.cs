using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Numbers;


namespace CalculatorNotepad
{

    // expression , structure used during parsing of text into function tree
    public enum mcExpType { Invalid, Expression, NewDef, Comment, Empty, P_constant, P_parameter, P_function, P_symbol };
    public enum mcExpExtendedType { Other, Expression, NewFunction, NewVariable};

    public class mcExp
    {
        public string Text;
        public mcFunc func;         //  function tree for calculation
        public mcFuncParse op;      //  info about parsing rules for this operation/function
        public mcExpType expType;          // if 'NewDef',  "op" points to that new one
        public mcExpExtendedType extendedType;     // keep info if newVariable
        public bool hasRedef; // true if this expression (or its subexpressions) redefine (assign to) outside function or variable
        public int uppermostAssignedVarDepth = int.MaxValue; // used to determine possible side effects that are MADE by this expression
        public int uppermostUsedVarDepth= int.MaxValue;     // used to determine possible side effects that are USED by this expression

        public const char paramDelimiter = 'Ɵ';  // used to enclose additional info about parameters, Ɵ2#3Ɵx means variable name 'x' , in 3rd block depth, and 4th parameter in that function
        public const char secondSemicolon = '§'; // used to append to } to allow {..}{..} without need for ; like {..};{..};  



        // *** helper parse functions

        // check if = (equal sign) in at this position in text, avoiding ==,  => ,  >=, <=, !=
        static bool isEQat(string txt, int pos)
        {
            if ((pos >= 0) && (pos < txt.Length))
            {
                int i = pos;
                if (txt[i] == '=')
                {
                    bool ok = true;
                    // skip where = is valid 1st char: ==, =>
                    if (i < txt.Length - 1)
                        switch (txt[i + 1])
                        {
                            case '=':
                            case '>':
                                ok = false;
                                break;
                        }
                    // skip where = is valid 2nd char: ==, >=, <=, !=
                    if (i > 0)
                        switch (txt[i - 1])
                        {
                            case '=':
                            case '>':
                            case '<':
                            case '!':
                                ok = false;
                                break;
                        }
                    return ok;
                }
            }
            return false;
        }

        // find = (equal sign) in text, avoiding ==,  => ,  >=, <=, !=
        static int findEQ(string txt)
        {
            for (int i = 0; i < txt.Length; i++)
                if (isEQat(txt, i))
                    return i;
            return -1;
        }

        // extract parameter info, does not move pos. In future it may be int[] pDepth
        string getParamInfo(ref string txt, out int pDepth, out int pIdx)
        {
            // extract between # (actually theta) markers, without moving position
            var strIdx = mcParse.extractBetweenMarkers(ref txt, paramDelimiter.ToString(), paramDelimiter.ToString(), true, false, false);
            // Number parameter name must be next, this time move pos
            var parName = mcParse.extractName(ref txt,false,false);
            if (parName=="")
                throw new mcException("Invalid parameter name (" + strIdx + ")");
            var strNum = mcParse.extractNumber(ref strIdx, false, false);
            if (!int.TryParse(strNum, out pDepth))
                throw new mcException("Invalid depth value (" + strNum+strIdx+") for parameter: "+parName);
            if (strIdx.SubStr(0, 1) != "#")
                throw new mcException("Invalid parameter format, missing delimiter (" + strIdx+ ") for parameter: " + parName);
            mcParse.removeStart(ref strIdx, 1,false);
            strNum = mcParse.extractNumber(ref strIdx, false, false);
            if (!int.TryParse(strNum, out pIdx))
                throw new mcException("Invalid index value (" + strNum + strIdx + ") for parameter: " + parName);
            return parName;
        }

        // extract parameter names from (x,y,z) and remove those names in expression string "x+2*y/z" with parameter symbols "#0#0+2*#1#1/#1#2", where"#D#N" is Nth parameter on depth D (both zero based).
        private int ReplaceParameterNames(string paramBracketInside, ref string text, int paramLevel)
        {
            var newParams = mcParse.splitCommas(paramBracketInside);
            // test if valid parameters (names, no numbers, parentheses or symbols) and replace with #D_0,#D_1... where D=depth
            for (int i = 0; i < newParams.Length; i++)
            {
                var tmpName = newParams[i].Trim();
                var oldMark= mcParse.extractBetweenMarkers(ref tmpName, paramDelimiter.ToString(), paramDelimiter.ToString(), false, false, false);
                var tstName = mcParse.extractName(ref tmpName, false, false);
                if ((oldMark + tstName) != newParams[i].Trim())
                    throw new mcException("Invalid parameter name in function definition : " + newParams[i]);
                //if (oldMark != "") // issue warning about nested function using same variable name as parent function?
                // change this parameter name in expression to #1, #2 etc
                string pattern = @"\b" + oldMark + tstName + @"\b";
                string newMark = paramDelimiter.ToString() + paramLevel.ToString() + "#" + i + paramDelimiter;
                string replace = newMark + tstName;
                text = System.Text.RegularExpressions.Regex.Replace(text, pattern, replace);
            }
            return newParams.Length;
        }



        // convert unit string to double
        private Number convUnit(string unitStr)
        {
            Number totalVal = 1, conversionVal = 1;
            int lastOp = 1;
            while (unitStr != "")
            {
                var uName = mcParse.extractName(ref unitStr, true, false);
                if (mc.hasUnit(uName))
                {
                    Number unitVal = mc.getUnit(uName);
                    // check if this is squared or cubed unit
                    if ((unitStr.Length > 0) && (unitStr[0] == '^'))
                    {
                        mcParse.removeStart(ref unitStr, 1);
                        if ((unitStr.Length < 1) || (unitStr[0] < '1') || (unitStr[0] > '9'))
                            throw new mcException("Invalid unit power: " + uName, false);
                        int power = unitStr[0] - '0';
                        unitVal = Number.Pow(unitVal, power);
                        mcParse.removeStart(ref unitStr, 1);
                    }
                    totalVal = lastOp > 0 ? totalVal * unitVal : totalVal / unitVal;
                    if (unitStr.Length > 0)
                    {
                        var ch = unitStr[0];
                        mcParse.removeStart(ref unitStr, 1);
                        switch (ch)
                        {
                            case '/': lastOp = -1; break;
                            case '*': lastOp = +1; break;
                            case '-':
                                if ((unitStr.Length == 0) || (unitStr[0] != '>'))
                                    throw new mcException("Invalid unit : " + unitStr, false);
                                conversionVal = convUnit(unitStr.Remove(0, 1));
                                unitStr = "";
                                break;
                            case char c when (c == '>') || (c == '→'):
                                conversionVal = convUnit(unitStr);
                                unitStr = "";
                                break;
                            default:
                                throw new mcException("Invalid unit : " + unitStr, false);
                        }
                    }
                }
                else
                    throw new mcException("Invalid unit : " + (uName != "" ? uName : unitStr));
            }
            return totalVal / conversionVal;
        }


        public void doElements(List<mcExp> elements, int startIndex, int Count)
        {
            // find highest priority unfused function, fuse it, and repeat while there is any function left
            int posPriority = 0;
            while (posPriority >= 0)
            {
                int maxPriority = 0;
                posPriority = -1;
                for (int i = 0; i < elements.Count; i++)
                    if ((elements[i].func == null) && (elements[i].op != null) && (elements[i].op.Priority > maxPriority))
                    {
                        maxPriority = elements[i].op.Priority;
                        posPriority = i;
                    }
                if (posPriority >= 0)
                {
                    var f = elements[posPriority].op;
                    var fName = f.Name != "" ? f.Name : elements[posPriority].Text;
                    // if this is binary middle function
                    if (f.ParamType == mcFuncParamType.Between)
                    {
                        if ((posPriority >= elements.Count - 1) || (elements[posPriority + 1].func == null))
                            throw new mcException("Invalid right side argument for function " + fName);
                        if ((posPriority <= 0) || (elements[posPriority - 1].func == null))
                            throw new mcException("Invalid left side argument for function " + fName);
                        elements[posPriority].func = new mcFunc(f.funcCalc, new mcFunc[] { elements[posPriority - 1].func, elements[posPriority + 1].func });
                        // remove arguments
                        elements.RemoveAt(posPriority + 1);
                        elements.RemoveAt(posPriority - 1);
                    }
                    else
                    // if this is unary sufix function, like !%
                    if (f.ParamType == mcFuncParamType.Sufix)
                    {
                        if ((posPriority <= 0) || (elements[posPriority - 1].func == null))
                            throw new mcException("Invalid left side argument for function " + fName);
                        elements[posPriority].func = new mcFunc(f.funcCalc, new mcFunc[] { elements[posPriority - 1].func });
                        // remove arguments
                        elements.RemoveAt(posPriority - 1);
                    }
                    else
                    // if this is unary prefic function, like unary +-
                    if (f.ParamType == mcFuncParamType.Prefix)
                    {
                        if ((posPriority >= elements.Count - 1) || (elements[posPriority + 1].func == null))
                            throw new mcException("Invalid right side argument for function " + fName);
                        elements[posPriority].func = new mcFunc(f.funcCalc, new mcFunc[] { elements[posPriority + 1].func });
                        // remove arguments
                        elements.RemoveAt(posPriority + 1);
                    }
                    else
                        // func and variables were already resolved in parse
                        throw new mcException("Unknown function type " + f.ParamType + " for function " + fName);
                }
            }
            // check if only one, valid, expression remained
            if (elements.Count != 1)
                throw new mcException("Invalid expression, multiple parts:  " + elements.Count + ".  Are you missing ';' ?");
            if (elements[0].func == null)
                throw new mcException("Invalid expression  " + elements[0].Text);
            // copy that expression to this main one
            this.func = elements[0].func;
        }


        // split block into parts, and handle special cases like else or do-while
        string[] splitBlock(string input, Point startLoc, out Point[] locs)
        {
            var exps = mcParse.splitOver(input, new string[] { ";", secondSemicolon.ToString() }, startLoc, out locs, SplitOverOptions.RemoveEmptyEntries);
            if (exps.Length < 2) return exps;
            // find if we have 'else' after 'if', and join in one expression
            var res = new List<string>(exps.Length);
            int p = 0;
            while (p < exps.Length)
            {
                var line = exps[p];
                if (p+1 < exps.Length){
                    if (  ( exps[p].StartsWith("if") && exps[p+1].StartsWith("else")) || ( exps[p].StartsWith("do") && exps[p + 1].StartsWith("while")))
                    {
                        p++;
                        line += " " + exps[p];
                    }
                }
                p++;
                res.Add(line);
            }
            return res.ToArray();
        }

        // parse text and create expression
        private void Parse(mcContext ctx)
        {
            expType = mcExpType.Invalid;
            // start positioning
            mcParse.resetCurrentPosition(ctx.startLoc);
            var txt = Text.Replace("}"+secondSemicolon, "}"); // prevent multiple appending, {}ssss{}ssss
            txt = txt.Replace("}", "}" + secondSemicolon); // to allow {..}{..} without ; between
            mcParse.extractSpaces(ref txt, false); // instead of Trim(), to update position
            ctx.startLoc = mcParse.getCurrentPosition();
            if (mc.cfg.sensitivity == mcCaseSensitivity.Insensitive) txt = txt.ToLower();
            // find comment if any
            string thisComment = "";
            if (txt.IndexOf("//") >= 0)
            {
                // find first comment, up to newline
                var pc = txt.IndexOf("//");
                var pcn = txt.IndexOf('\n', pc);
                if (pcn > 0)
                    thisComment = txt.SubStr(pc, pcn - pc);
                else
                    thisComment = txt.SubStr(pc);
                // if this is comment only line, return it
                var startSpaces = mcParse.extractSpaces(ref txt);
                if (txt == "")
                {
                    expType = mcExpType.Comment;
                    Text = startSpaces.Trim();
                    return;
                }
            }
            mcParse.extractSpaces(ref txt);
            // check if anything to parse
            if (txt == "")
            {
                expType = mcExpType.Empty;
                return;
            }

            // set initial values
            mcFunc newFunc = null;
            mcFuncParse newFuncParse = null;
            int newFuncLevel = 0; // 0=not a func definition, 1= redefining existing function, 2= new function
            var newFuncName = "";
            int newFuncNumParams = 0;
            bool newFuncIsVariable = true;
            var elements = new List<mcExp>();

            // *** UTIL subfunctions
            // helper to add new mcExp to doElements list
            // other outside parameters is: elements (to add parsed)
            void addElement(mcExp newElement)
            {
                elements.Add(newElement);
                if (newElement.hasRedef) hasRedef = true;
                // update uppermost used variables
                if (newElement.uppermostAssignedVarDepth < uppermostAssignedVarDepth) uppermostAssignedVarDepth = newElement.uppermostAssignedVarDepth;
                if (newElement.uppermostUsedVarDepth < uppermostUsedVarDepth) uppermostUsedVarDepth = newElement.uppermostUsedVarDepth;
            }

            // helper to vreate new parsing mcExp, and update own(or given) context afterwards
            mcExp newExp(string newTxt, Point newLoc, mcContext theCtx = null)
            {
                if (theCtx == null) theCtx = ctx;
                var newCtx = theCtx.next(newLoc);
                var res = new mcExp(newTxt, newCtx); // parsing is done here
                theCtx.numSubblocks = newCtx.numSubblocks; // if furthes toplevel subblocks were used inside, update here
                // update uppermost used variables
                if (res.uppermostAssignedVarDepth < uppermostAssignedVarDepth) uppermostAssignedVarDepth = res.uppermostAssignedVarDepth;
                if (res.uppermostUsedVarDepth < uppermostUsedVarDepth) uppermostUsedVarDepth = res.uppermostUsedVarDepth;
                // return parsed expression
                return res;
            }

            // helper to insert anon function given parsFunction and parameter string[]
            // other outside parameters are: op (with desired function found) and elements (to add parsed)
            void insertFunc(string[] eachParam, Point[] eachPos, bool isVirtualBlock = false)
            {
                var pList = new List<mcFunc>();
                bool isBlock = (op.Name == "blockintr");
                // compile value of each param
                var newCtx = ctx;
                if (isBlock && !isVirtualBlock)
                {
                    newCtx = new mcContext(ctx);
                    ctx.numSubblocks++;
                    newCtx.blockIdx.Add(ctx.numSubblocks);
                    newCtx.numSubblocks = 0;
                }
                var oldPosIns = mcParse.getCurrentPosition();
                for (int i = 0; i < eachParam.Length; i++)
                {
                    var mExp = newExp(eachParam[i], eachPos[i], newCtx);
                    if (mExp.expType != mcExpType.NewDef)
                    {
                        // do not insert empty/comment lines for blocks (Number or virtual)
                        // for normal functions allow - it result in factor[i]==null
                        // func can be null in cases like : while( , cond, work )  - when first param is omitted
                        if ((!(isBlock|| isVirtualBlock))|| ((mExp.expType != mcExpType.Comment) && (mExp.expType != mcExpType.Empty)))
                        {
                            pList.Add(mExp.func);
                            if (mExp.hasRedef) hasRedef = true;
                        }
                    }
                    else
                    {
                        hasRedef = true;
                        if (!isBlock) throw new mcException("New function definitions are not allowed as parameters here !");
                    }
                }
                // check if this is recursive call
                mcFuncTrinary isRecursiveCall = mc.hasVolatile(op.Name) ? mcFuncTrinary.Yes : mcFuncTrinary.Unknown;
                // add this function with its parameters
                addElement(new mcExp(new mcFunc(op.funcCalc, pList.ToArray(), null, "", isRecursiveCall), null, op.Name)); // function with parameters
               // reset position
                mcParse.resetCurrentPosition(oldPosIns);
            }

            // check for multiple expressions within same line
            Point[] locs;
            var exps = splitBlock(txt, ctx.startLoc, out locs);
            if (exps.Length > 1)
            {
                // split on ;  as expression delimiter inside this type of block
                op = mc.getFunc("blockintr");
                insertFunc(exps, locs, true); // 'virtual' block, do not increase depth
                txt = "";
            }
            else
                if (exps.Length==1)
                    txt = exps[0];  // to remove sigmas from  {}s
            // test if this is assigment = definition
            // is this new definition ?
            var txt2 = txt;
            var oldPos = mcParse.getCurrentPosition();
            bool isNewDef = false;
            bool potFunc = false;
            bool potUnit = false;
            bool potIndex = false;
            bool potNew = false;
            string potInParentheses = "";
            // check syntax : [new] name [[idx]] | [(x,y,z..)] = exp , or 'Mpc'= 23
            //    'mpg' = 1.13          - declaring new unit
            //    func(x,y) =  3*x+y    - declaring new function
            //    x = vec(1,2,3)        - declaring new variable (if not already declared), and assigning value
            //    x[1]=6                - assigning new value to one element of (already declared) array 
            //    new x=7               - force new declaration of 'x':  {x=1; {x=3;} y=x} -> y=1, not 3
            var potName = mcParse.extractName(ref txt2);
            if (potName == "new")
            {
                potNew = true;
                potName = mcParse.extractName(ref txt2);
            }
            if (potName != "")
            {
                // is this variable, x=6 ,  or function definition f(x)=7 ?
                if (txt2.SubStr(0, 1) == "(")
                {
                    potFunc = true;
                    potInParentheses = mcParse.extractParentheses(ref txt2);
                }
                else
                // if this is variable, is it maybe index setting like:  v[expA]= expB ;  ?
                if (txt2.SubStr(0, 1) == "[")
                {
                    potIndex = true;
                    potInParentheses = mcParse.extractBrackets(ref txt2, true);
                }
            }
            else
            {
                // maybe new unit definition 'ly'=1e76 ?
                potName = mcParse.extractBetweenMarkers(ref txt2, "'", "'");
                if (potName != "")
                {
                    potUnit = true;
                }
            }
            if ((potName!="")&&isEQat(txt2, 0))
            {
                isNewDef = true;
                mcParse.removeStart(ref txt2, 1);
            }
            if (isNewDef)
            {
                // it IS new def, set remainder to txt
                txt = txt2;
                mcParse.extractSpaces(ref txt);
                if (txt == "") throw new mcException("Invalid definition : empty",false);
                if (potUnit)
                {
                    if (potNew) throw new mcException("Invalid use of 'new' keyword - it is applicable only to variables !", true);
                    // check if this is unit definition
                    if (mc.allowedNewUnit(potName))
                    {
                        // calculate value for new unit
                        var oldPos2 = mcParse.getCurrentPosition();
                        var uExp =  newExp( txt,  mcParse.getCurrentPosition() );
                        var uVal = uExp.Evaluate();
                        mcParse.resetCurrentPosition(oldPos2);
                        if (uVal.isValue && !uVal.isError && uVal.Value.isScalar())
                        {
                            mc.addUnit(potName, uVal.Value.Number);
                            expType = mcExpType.NewDef;
                            return;
                        }
                        else
                            throw new mcException("Invalid unit value :" + txt);
                    }
                    else
                        throw new mcException("Duplicated unit name :" + potName);
                }
                // if not unit, then it is new variable or function
                newFuncName = potName;
                expType = mcExpType.NewDef;
                newFuncNumParams = 0;
                if (potFunc)
                {
                    if (potNew) throw new mcException("Invalid use of 'new' keyword - it is applicable only to variables !", true);
                    newFuncIsVariable = false;
                    newFuncNumParams = ReplaceParameterNames(potInParentheses, ref txt, ctx.parameterLevel);
                }
                // NEW: if this is variable, do separate storing
                if (newFuncIsVariable)
                {
                    // otherwise this is normal new variable assigment, ie: v= expB
                    // check if already defined here or above
                    var vBlock = mc.findVariable(newFuncName, ctx.blockIdx);
                    if (vBlock == null)
                    {
                        if (!mc.allowedNewName(newFuncName)) // only check func override if not already defined variable
                            throw new mcException("New variable would hide function with same name : '" + newFuncName + "'");
                        // does not exists, create new one here
                        vBlock = ctx.blockIdx;
                        mc.addVariable(newFuncName, vBlock, ctx.parentFunc);
                    }
                    else
                    {
                        // if 'new' used, force variable to this scope
                        if (potNew)
                            vBlock= ctx.blockIdx; 
                    }
                    // store uppermost assigned variable depth, to determine side effects
                    if (uppermostAssignedVarDepth > vBlock.Count)  uppermostAssignedVarDepth = vBlock.Count;
                    // get full variable name
                    var fullName = mc.fullVarName(newFuncName, vBlock);
                    // if this is index value assignment, this is mix of get/set, and need to parse index also
                    mcExp idxExp = null;
                    if (potIndex)
                    {
                        idxExp= newExp(potInParentheses, mcParse.lastEx());
                        if ((idxExp.expType != mcExpType.Expression) || (idxExp.func == null))
                            throw new mcException("Invalid index value : " + potInParentheses);
                    }
                    // parse remainder of source, as evaluation body
                    var valExp = newExp(txt, mcParse.getCurrentPosition());
                    if ((valExp.expType != mcExpType.Expression)|| (valExp.func == null))
                        throw new mcException("Invalid assignment to variable " + newFuncIsVariable);
                    // create assignment lambda, baking inside full function name
                    var theAssign = new mcFunc((args)=> {
                                                            var fName = fullName; // to make capture copy
                                                            return mc.doVarAssign(fName, args);
                                                        }
                                                , mcFuncTrinary.Not);
                    theAssign.Name = fullName; // for debug view
                    extendedType = mcExpExtendedType.NewVariable;
                    expType = mcExpType.Expression;  // assignment returns value of variable, so this is also expression
                    // can not use ctx.parentFunc, since that would be parent of assignment code, but not necessary parent of initial variable declaration
                    mcFunc declarationParent=null;
                    if (!mc.varParents.TryGetValue(fullName, out declarationParent)) throw new mcException("Invalid variable parent for "+fullName);
                    // parent function, but as special lambda , so func addr of (ctx.parentFunc) will be passed to direct f-on, instead of evaluating its value
                    var lambdaParent = declarationParent != null?new mcFunc(declarationParent, null, 0, "", mcFuncTrinary.Unknown, mcFuncTrinary.Not):null;
                    // factors for assign wrapper - valExp for new value expression, lambdaParent to point allw ay up to parent func
                    // and [optionally] idxExp for index expression (v[idxExp]=valExp;)
                    var assFactors = new mcFunc[potIndex ? 3 : 2];
                    assFactors[0] = valExp.func;
                    assFactors[1] = lambdaParent;
                    if (potIndex) assFactors[2] = idxExp.func;
                    // another wrapper, so { valExp.func, ctx.parentFunc } are in factors, and can be deep-copied
                    var thisAssign = new mcFunc(theAssign, assFactors, null, "", mcFuncTrinary.Unknown, mcFuncTrinary.Not);
                    thisAssign.Name = newFuncName + "= val,parent";//for debug view
                    this.func = thisAssign; // since this will be returned as expression result
                    return;
                }
                // otherwise NEW Function
                // check if name already exists
                if (mc.allowedNewName(newFuncName))
                {
                    // it does not exists in function names, check variable names
                    var varName = mc.findVariableName(newFuncName, ctx.blockIdx);
                    if (varName!="")
                        throw new mcException("New function would hide variable with same name : '" + newFuncName + "'", mcParse.lastEx(-1));
                    // if this is param name, it can not be redefined
                    if (newFuncName[0] == paramDelimiter)
                    {
                        int tmpDepth, tmpIdx;
                        var parName = getParamInfo(ref newFuncName, out tmpDepth, out tmpIdx);
                        throw new mcException("Parameters can not be redefined, use new variable name instead of '" + parName+"'");
                    }
                    // create /// comment descriptions  
                    string commDesc = "";
                    if ((mc.lastExp != null) && (mc.lastExp.expType == mcExpType.Comment)) commDesc = mcParse.toolTipComment(mc.lastExp.Text, true); //if any /// comment on line above this definition
                    commDesc += mcParse.toolTipComment(thisComment,true); // also include any /// comment in same line
                    // insert new variable or function at start, to enable recursive calls
                    newFuncLevel = 2;
                    if (newFuncIsVariable)
                    {
                        throw new mcException("Obsolete variable definition for  : '" + newFuncName + "'"); // even predefined variables like 'last' do not use new mcExp
                    }
                    else
                    {
                        // create new USER function
                        // recursion guard for functions
                        newFunc = new mcFunc(0); // temporary one, its subFunc/type will be updated (but newFunc not be replaced), so type does not matter
                        newFunc.Name = newFuncName;
                        mc.addVolatile(newFuncName); // to allow removal in case of exception
                        newFunc.flags |= mcFuncFlags.User; // mark this as user defined topmost function
                        newFuncParse = new mcFuncParse(newFuncName, mcFuncParamType.Func, newFuncNumParams, 90, newFunc, true, commDesc != "" ? commDesc : "user defined notepad Function", newFuncName+" ( "+ potInParentheses+" )");
                        newFuncParse.block = ctx.blockIdx.ToArray();
                        mc.addFunc(newFuncName, newFuncParse, false); // insert future func, in case it uses itself. newfuncParse.mcFunc will be replaced
                        ctx.parentFunc = newFunc; // update parent function to this one, so further body parsing can use it
                    }
                    // make artificial block if new function is not already starting with block
                    if ((txt != "") && (txt.SubStr(0, 1) != "{"))
                    {
                        ctx.numSubblocks++;
                        ctx.blockIdx.Add(ctx.numSubblocks); // make all following parsing happening in one block deeper
                        ctx.numSubblocks = 0; // and reset number of subblocks to zero
                    }
                }
                else
                {
                    newFuncLevel = 1;
                    // find which function is redefined
                    var oldFuncParse = mc.getFunc(newFuncName);
                    if (oldFuncParse == null)
                        throw new mcException("Invalid function name for redefinition : " + newFuncName);
                    // check that it has same number of parameters
                    if (oldFuncParse.NumParameters != newFuncNumParams)
                        throw new mcException("Invalid redefinition, different number of parameters : " + newFuncName);
                    // redefinition of existing function, see if allowed
                    if ((!mc.cfg.allowFuncRedefinition)&& (oldFuncParse.ParamType != mcFuncParamType.Variable))
                        throw new mcException("Function redefinition are not allowed : " + newFuncName);
                    if ((!mc.cfg.allowBuiltInRedefinition) && !oldFuncParse.isUserFunction)
                        throw new mcException("Redefinition of buit in functions is disabled : " + newFuncName);
                    // redefining function to variable
                    if (newFuncIsVariable && (oldFuncParse.ParamType != mcFuncParamType.Variable))
                        throw new mcException("Redefinition of function to variable is not allowed : " + newFuncName);
                    // redefining variable to function
                    if ((!newFuncIsVariable) && (oldFuncParse.ParamType == mcFuncParamType.Variable))
                        throw new mcException("Redefinition of variable to function is not allowed : " + newFuncName);
                    // check that old function is, in fact, a function or variable
                    if ((oldFuncParse.ParamType != mcFuncParamType.Func) && (oldFuncParse.ParamType != mcFuncParamType.Variable))
                        throw new mcException("Invalid type for redefinition : " + newFuncName);
                    // set newFunc to point to already existing func 
                    newFunc = oldFuncParse.funcCalc;
                    // if this is function and not variable, insert volatile name to detect recursions. Note: func will remain undefined after error in redefinition
                    if (!newFuncIsVariable)
                        mc.addVolatile(newFuncName); 
                }
            }
            else
            {
                // not new def
                mcParse.resetCurrentPosition(oldPos);
            }
            // parse elements = numbers, names (of variables,user functions, math functions...), symbols (operators, parenthenses, commas...), ParenthesesGroup
            if (expType == mcExpType.Invalid) expType = mcExpType.Expression;
            mcParse.extractSpaces(ref txt);
            while (txt != "")
            {
                char ch = txt[0];
                switch (ch)
                {
                    case '(':
                        var inBrackets = mcParse.extractParentheses(ref txt);
                        //test for lambda, if (x,y,z)=> lambda_expression 
                        if (txt.SubStr(0, 2) == "=>")
                        {
                            mcParse.removeStart(ref txt, 2);
                            // replace lambda parameters with #D#N, on next dept level
                            int numLambdaparams = ReplaceParameterNames(inBrackets, ref txt, ctx.parameterLevel + 1);
                            // add/parse lambda subexpression, on next depth level
                            var newCtx = ctx.next(mcParse.lastEx(2));
                            newCtx.parameterLevel++;
                            newCtx.thisIsLambda = true;
                            addElement(new mcExp(txt, newCtx)); // lambda compiled here
                            ctx.numSubblocks = newCtx.numSubblocks;
                            // lambda_expression is assumed to be until end of text
                            txt = "";
                        }
                        else
                            // regular subexpression, just add it on same depth level
                            addElement( newExp(inBrackets, mcParse.lastEx()) );
                        break;
                    case ')':
                        throw new mcException("Closing parentheses without opening!",false);
                    case '{':
                        string inBlock;
                        if ( mcParse.extractBracketType(ref txt, out inBlock, '{', '}', 0, ExtractOptions.RemoveAndSkip))
                        {
                            // split on ;  as expression delimiter inside this type of block
                            exps = splitBlock(inBlock, mcParse.lastEx(), out locs);
                            op = mc.getFunc("blockintr");
                            insertFunc(exps,locs);
                            break;
                        }
                        else
                            throw new mcException("Invalid {} block!");
                    case '}':
                        throw new mcException("Closing } block without opening { !",false);
                    case '[':
                        var sIdx = mcParse.extractBrackets(ref txt, true).Replace("...", "…").Replace("..", "…");
                        var range = mcParse.splitOver(sIdx, '…', mcParse.lastEx(), out locs);
                        if ((range.Length < 1) || (range.Length > 2))
                            throw new mcException("Invalid index range !", mcParse.lastEx());
                        // first insert binary unresolved op 'index' (whose op.func will be resolved in doElements)
                        op = mc.getFunc("[");
                        addElement(new mcExp(op));
                        // then insert indices function that take index elements as parameters, ie x[2..3] becomes x "index"  vec(2,3)
                        op = mc.getFunc("vec"); 
                        insertFunc(range,locs);
                        break;
                    case ']':
                        throw new mcException("Closing index bracket without opening!",false);
                    case paramDelimiter: //'#'
                        // parameter index format #0#3 - 4th parameter in first depth (numbers are zero based )
                        int pDepth, pIdx;
                        var parName = getParamInfo(ref txt, out pDepth, out pIdx);
                        addElement(new mcExp(pDepth, pIdx, parName)); 
                        break;
                    case char c when (c == '.') || (c == '∞') || ((c >= '0') && (c <= '9')) :
                        var number = mcParse.extractNumber(ref txt);
                        Number Value;
                        if (!mcParse.TryParseNumber(number, out Value))
                            throw new mcException("Invalid double value : " + number);
                        addElement(new mcExp(Value, number));
                        break;
                    case char c when ((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z'))  || "π∑∫∏".Contains(c):
                        var name = mcParse.extractName(ref txt);
                        // special functions
                        if (name.ToLower() == "graph")
                        {
                            var target= mcParse.extractParentheses(ref txt);
                            // is this just name of function
                            op= mc.getFunc(target);
                            var graphMsg = "";
                            if (op != null)
                            {
                                graphMsg = "DSP:" + op.Name + "[" + op.NumParameters + "]" + (op.funcCalc.isConstant() ? "C" : "") + (op.funcCalc.isRecursive() ? "R" : "") + "= " + op.funcCalc.ToString();
                            }
                            else
                            {
                                var innerExp =  newExp( target, mcParse.lastEx());
                                if ((innerExp == null) || (innerExp.func == null))  throw new Exception("DSP: null !"); // not valid function
                                // if valid expressin, display its graph
                                graphMsg = "DSP:" + innerExp.func.ToString();
                            }
                            // set exp as invalid, this will return text description
                            expType = mcExpType.Invalid;
                            Text = graphMsg;
                            return;
                        }
                        else
                        if (name == "else")
                        {
                            throw new mcException("ELSE can not be used outside IF statement !", mcParse.lastEx());
                        }

                        // standard name resolution
                        op = mc.getFunc(name, ctx.blockIdx);
                        bool hasParams = txt.SubStr(0, 1) == "(";
                        // check if this is variable name
                        var fullVarName = "";
                        if (!hasParams)
                        {
                            var block = mc.findVariable(name, ctx.blockIdx);
                            if (block != null)
                            {
                                fullVarName= mc.fullVarName(name, block);
                                if (uppermostUsedVarDepth > block.Count) uppermostUsedVarDepth = block.Count;
                            }
                        }
                        if (fullVarName != "") // && (op==null)
                        {
                            //**** VARIABLE NAME ***
                            // this is variable name, so create mcExp that returns its value
                            // create retrieval lambda
                            var theGet = new mcFunc((args) =>
                            {
                                var fName = fullVarName; // to make capture copy
                                return mc.doVarGet(fName, args);
                            }
                                                        , mcFuncTrinary.Not);
                            theGet.Name = fullVarName;
                            // can not use ctx.parentFunc, since that would be parent of assignment code, but not necessary parent of initial variable declaration
                            mcFunc declarationParent = null;
                            if (!mc.varParents.TryGetValue(fullVarName, out declarationParent)) throw new mcException("Invalid variable parent for " + fullVarName);
                            // parent function, but as special lambda , so func addr of (ctx.parentFunc) will be passed to direct f-on, instead of evaluating its value
                            var lambdaParent = declarationParent != null ? new mcFunc(declarationParent, null, 0, "", mcFuncTrinary.Unknown, mcFuncTrinary.Not) : null;
                            // wrapper to put {  ctx.parentFunc } in factors, so it can be deep-copied [ otherwise could have captured it just like fName ]
                            var thisGet = new mcFunc(theGet, new mcFunc[] { lambdaParent }, null, "", mcFuncTrinary.Unknown, mcFuncTrinary.Not);
                            thisGet.Name = name + " &parent";
                            // create new mcExp and add to element list
                            addElement(new mcExp(thisGet, null, name));
                        }
                        else
                        if (op != null)
                        {
                            // **** FUNCTION NAME ***
                            // check for sideeffects, if used function reaches outside of our scope
                            if (op.hasSideEffects && (op.sideEffectsDepth < uppermostAssignedVarDepth))
                                uppermostAssignedVarDepth = op.sideEffectsDepth;
                            // variable are handled above, so this should be error
                            if (op.ParamType == mcFuncParamType.Variable)
                            {
                                addElement(new mcExp(op.funcCalc, null, op.Name));
                                //throw new mcException("Obsolete variable get : " + name , mcParse.lastEx());  // still used for predefined constant variables
                            }
                            else
                            // for operators that have names instead of symbols, like 'and' or 'intersect' ... adds just operator, resolve in doElements
                            if (op.ParamType == mcFuncParamType.Between)
                                addElement(new mcExp(op));
                            else
                            // functions need to parse parameters, then insert func
                            if (op.ParamType == mcFuncParamType.Func)
                            {
                                // function, so check parameters in parentheses
                                if ((txt == "") || (txt[0] != '('))
                                {
                                    // insert new function which is sure to be treated as potential lambda at depth zero - since this is outside func previously defined, which need #0#0 #0#1 etc 
                                    addElement(new mcExp(new mcFunc(op.funcCalc, null, 0, "", mcFuncTrinary.Unknown, mcFuncTrinary.Not), null, op.Name));
                                    //throw new mcException("Missing function parameters for : " + name);
                                }
                                else
                                {
                                    var funcParams = mcParse.extractParentheses(ref txt);
                                    var eachParam = mcParse.splitOver(funcParams, ',', mcParse.lastEx(), out locs);
                                    if (eachParam.Length < op.NumParameters)
                                        throw new mcException("Function " + name + " need at least " + op.NumParameters + " arguments!", mcParse.lastEx());
                                    // check for special functions cases
                                    // in case of statement IF version
                                    if ((name == "if") && (eachParam.Length == 1))
                                    {
                                        // if(cond) expTrue [ else expFalse]
                                        Point[] locs2, newLocs;
                                        string[] newParams;
                                        var ifParts = mcParse.splitOver(txt, new string[] { "else" }, mcParse.lastEx(), out locs2);
                                        if (ifParts.Length < 1) throw new mcException("IF function missing expressison !", mcParse.lastEx());
                                        if (ifParts.Length > 2) throw new mcException("IF function have multiple ELSE sections !", mcParse.lastEx());
                                        if (ifParts.Length == 1)
                                        {
                                            // missing else: if (cond) expTrue;
                                            newParams = new string[2] { eachParam[0], ifParts[0] };
                                            newLocs = new Point[2] { locs[0], locs2[0] };
                                        }
                                        else
                                        {
                                            // with else: if(cond) expTrue else expFalse ;
                                            newParams = new string[3] { eachParam[0], ifParts[0], ifParts[1] };
                                            newLocs = new Point[3] { locs[0], locs2[0], locs2[1] };
                                        }
                                        insertFunc(newParams, newLocs);
                                        txt = ""; //should laready be empty, to finish parsing and process doElements
                                    }
                                    else
                                    // in case of statement WHILE version
                                    if ((name == "while") && (eachParam.Length == 1))
                                    {
                                        // while (cond) loopExp ; loopExp is all way to the end of input
                                        // while(initialDef, condition [,work [,returnValue]] ) - this is format that needs to be constructed
                                        mcParse.extractSpaces(ref txt);
                                        if (txt == "") throw new mcException("WHILE statement missing loop body !", mcParse.lastEx());
                                        var newParams = new string[3] { "", eachParam[0], txt };
                                        var newLocs = new Point[3] { locs[0], locs[0], mcParse.getCurrentPosition() };
                                        insertFunc(newParams, newLocs);
                                        txt = ""; // rest of txt was passed as loopExp, so clear it 
                                    }
                                    else
                                    // in case of statement FOR:  for ( [initializer] , condition , [iterator] )  body ;
                                    if (name == "for")
                                    {
                                        mcParse.extractSpaces(ref txt);
                                        //if (txt == "") throw new mcException("FOR statement missing loop body !", mcParse.lastEx());
                                        var newParams = new string[4] { eachParam[0], eachParam[1], eachParam.Length > 2 ? eachParam[2] : "", txt };
                                        var newLocs = new Point[4] { locs[0], locs[1], locs.Length > 2 ? locs[2] : locs[1], mcParse.getCurrentPosition() };
                                        insertFunc(newParams, newLocs);
                                        txt = ""; // rest of txt was passed as body/loopExp, so clear it 
                                    }
                                    else
                                    // standard FUNCTIONS :
                                    // create function and insetr for doElements
                                    {
                                        insertFunc(eachParam, locs);
                                    }
                                }
                            }
                            else
                            // for statements like 'return'
                            if (op.ParamType == mcFuncParamType.Prefix)
                            {
                                mcParse.extractSpaces(ref txt);    // empty result is allowed - will return last evaluated body in block
                                var newParams = new string[1] { txt };
                                var newLocs = new Point[1] { mcParse.getCurrentPosition() };
                                // in case of statement DO-WHILE version
                                if (name == "do")
                                {
                                    // do {body} while(cond); - is using below func case 
                                    // dowhile ([initialDef], [body] , condition [,returnValue]] )  - this is format that needs to be constructed
                                    Point[] locs2;
                                    var doParts = mcParse.splitOver(txt, new string[] { "while" }, mcParse.lastEx(), out locs2);
                                    if (doParts.Length < 2) throw new mcException("do-while missing 'while' expressison !", mcParse.lastEx());
                                    if (doParts.Length > 2) throw new mcException("do-while have multiple 'while' sections !", mcParse.lastEx());
                                    var body = doParts[0];
                                    var whileCond = mcParse.extractParentheses(ref doParts[1]);
                                    var conds = mcParse.splitOver(whileCond, ',', mcParse.lastEx(), out locs);
                                    if (conds.Length != 1) throw new mcException("while condition need exactly one argument!", mcParse.lastEx());
                                    newParams = new string[3] { "", body, conds[0] };
                                    newLocs = new Point[3] { locs2[0], locs2[0], locs2[1] };
                                }
                                else
                                // in case of RETURN statement :  return resultExpression
                                if (name == "return")
                                {
                                    op.funcCalc._forcedConstant = mcFuncTrinary.Not; // to prevent making "return 0;" into constant, since we need result propagation
                                }
                                insertFunc(newParams, newLocs);
                                txt = ""; // rest of txt was passed to prefix function (not or return or ...), so clear it 
                            }
                            else
                            // for parameterless functions, like 'bin' 
                            if (op.ParamType == mcFuncParamType.None)
                            {
                                insertFunc(new string[0], new Point[1] { mcParse.getCurrentPosition() });
                            }
                            else
                                throw new mcException("Unknown function type " + op.ParamType +" for "+  name);
                        }
                        else
                            throw new mcException("Unknown name: " + name);
                        break;
                    default:
                        bool doubleSymbol = false;
                        if (txt.Length > 1)
                        {
                            var symbol = txt.Substring(0, 2);
                            op = mc.getFunc(symbol);
                            if (op != null)  // double length symbole like == >= 
                            {
                                mcParse.removeStart(ref txt, 2);
                                addElement(new mcExp(op));
                                doubleSymbol = true;
                            }
                        }
                        if (!doubleSymbol)
                        {
                            var symbol = txt.SubStr(0, 1);
                            if (symbol != "")
                            {
                                op = mc.getFunc(symbol);
                                if (op == null)
                                    throw new mcException("Invalid symbol: " + symbol,false);
                                if (symbol == "~") // 'last' value
                                {
                                    addElement(new mcExp(op.funcCalc, null, op.Name));
                                    // remove lastResult textual representation
                                    mcParse.extractLastResult(ref txt);
                                }
                                else
                                if (symbol == mc.unitSymbol)  // unit conversion operator 'km'
                                {
                                    addElement(new mcExp(op));
                                    // calculate unit multiplicator
                                    var unitStr = mcParse.extractBetweenMarkers(ref txt, mc.unitSymbol, mc.unitSymbol);
                                    if (unitStr == "")
                                        throw new mcException("Invalid unit : " + txt);
                                    var unitMul = convUnit(unitStr);
                                    addElement(new mcExp(unitMul, mc.unitSymbol + unitStr + mc.unitSymbol));
                                }
                                else  // other symbols like +-*/^
                                {
                                    mcParse.removeStart(ref txt, 1);
                                    addElement(new mcExp(op));
                                }
                            }
                        }
                        break;
                }
                mcParse.extractSpaces(ref txt);
            }
            // check if unary - or + is among elements
            for (int i = 0; i < elements.Count(); i++)
                if ((elements[i].op != null) && (elements[i].func == null) && ((elements[i].op.Name == "-") || (elements[i].op.Name == "+")))
                {
                    // check if left side is not evaluated (func, const...)
                    if ((i == 0) || ((elements[i - 1].func == null) && (elements[i - 1].op != null) && (elements[i - 1].op.ParamType == mcFuncParamType.Between)))
                    {
                        if (elements[i].op.Name == "-")
                            elements[i] = new mcExp(mc.getFunc("unaryminus"), "unaryminus");
                        else
                            elements[i] = new mcExp(mc.getFunc("unaryplus"), "unaryplus");
                    }
                }
            // now parse those elements
            doElements(elements, 0, elements.Count);
            extendedType = (expType!= mcExpType.NewDef)&&(expType!=mcExpType.Expression)? mcExpExtendedType.Other: mcExpExtendedType.Expression;
            // update new user function if needed
            if (expType == mcExpType.NewDef)
            {
                hasRedef = true;
                // if this is variable , evaluate rightside function and store only value (to prevent infinite recursions for x=x+3)
                if ((newFuncNumParams == 0) && newFuncIsVariable)
                {
                    extendedType = mcExpExtendedType.NewVariable;
                    // optional call to evaluate value of variable at compile time, needed only to give 'undefined x' error in x=x+1  if x was not defined previously. Not mandatory.
                    //var funcVal = func.Evaluate();
                    // create anonymous assignment function, that will be returned as result, using current result (expression) to assign to variable
                    var fAssign = mc.getFunc("assign"); // assign( ()=>variable, expression);
                    // first factor/param is variable, but as special lambda , so func addr of variable (newFunc) will be passed to direct f-on, instead of variable value
                    var varLambda= new mcFunc(newFunc, null, 0, "", mcFuncTrinary.Unknown, mcFuncTrinary.Not);
                    // second factor is already formed func graph from expression, in this func, so create instance anon func for assign
                    var thisAssign = new mcFunc(fAssign.funcCalc, new mcFunc[] { varLambda, func}, null, "", mcFuncTrinary.Unknown, mcFuncTrinary.Not);
                    expType = mcExpType.Expression;  // assignment returns value of variable, so this is also expression
                    this.func = thisAssign; // since this will be returned as expression result of x=x+1
                    throw new mcException("Obsolete variable finisher : " + newFuncName, mcParse.lastEx());
                }
                else
                {
                    // this is function, so update newFunc to final version (but keep same newFunc, since it was already referenced )
                    extendedType = mcExpExtendedType.NewFunction;
                    newFunc.CopyFrom(this.func); // copy from parsed function to one previously created, to allow recursion and redefinitions
                    // if this was entirely new function, append function name to top operator name
                    if (newFuncLevel >= 2)
                        newFunc.Name = newFuncName + "_" + newFunc.Name;
                    // update info about side effects
                    int uppermost = Math.Min(uppermostAssignedVarDepth, uppermostUsedVarDepth);
                    if (uppermost<= ctx.blockIdx.Count)
                    {
                        newFuncParse.hasSideEffects = true;
                        newFuncParse.sideEffectsDepth = uppermost;
                        newFuncParse.funcCalc.flags |= mcFuncFlags.HasSideEffects; // if it change or uses outside vars, it can not be cached later on
                    }
                }
            }
            else
            if ((expType == mcExpType.Expression)&& (func != null))
            {
                // optimize result function to constant, if all in graph are non-mutable
                if ( (!mc.cfg.disableConstantOptimization) && (func.FuncType != mcFuncType.Constant) && func.isConstant())
                {
                    var funcVal = func.EvaluateFunc();
                    var newConstFunc = new mcFunc(funcVal);
                    this.func = newConstFunc;
                }
                // if this was lambda body, it must be wrapped into lambda
                if (ctx.thisIsLambda)
                {
                    var wrapFunc = new mcFunc(func, null, ctx.parameterLevel, "L_" + func.Name, mcFuncTrinary.Unknown, mcFuncTrinary.Not);
                    func = wrapFunc;
                }

            }
        }



        // parse text and create expression, main creation
        //public mcExp(string text, int parameterLevel, bool thisIsLambda=false, Point startLoc= new Point())
        public mcExp(string text, mcContext ctx)
        {
            Text = text;
            Parse(ctx);
        }


        // create expression from double constant , used for direct insertion of constants
        public mcExp(Number constant, string text = "")
        {
            Text = text;
            func = new mcFunc((Number)constant);
            expType = mcExpType.P_constant;
        }

        // create expression from parameter index, used for direct insert of parameter index type
        public mcExp(int paramDepth, int paramIdx, string name="")
        {
            func = new mcFunc(paramDepth, paramIdx, name);
            Text = name;
            expType = mcExpType.P_parameter;
        }


        // create expression from function and parameters, used for direct insert of named functions
        public mcExp(mcFunc funct, mcExp[] fParameters, string text)
        {
            Text = text;
            func = funct;
            // check if this function can be optimized to constant
            if ((func != null) && (func.FuncType != mcFuncType.Constant) && func.isConstant())
            {
                var funcVal = func.EvaluateFunc();
                var newConstFunc = 
                func = new mcFunc(funcVal);
                expType = mcExpType.P_constant;
            }
            else
            {
                // otherwise mark as regular function
                expType = mcExpType.P_function;
            }
        }

        // create expression from function Parsing data, used for partial expressions like parsed symbols
        public mcExp(mcFuncParse functOp, string text = "")
        {
            Text = text;
            op = functOp;
            if (Text == "") Text = op.Name;
            expType = mcExpType.P_symbol;
        }



        // evaluate value of expression
        public CalcResult Evaluate()
        {
            var res = new CalcResult();
            // check first invalid type, to preserve error text
            if (expType== mcExpType.Invalid)
            {
                res.isError = true;
                res.Text = Text;
                return res;
            }
            switch (expType)
            {
                case mcExpType.Expression:
                    try
                    {
                        mc.cacheFuncClear();
                        //res.Value = mc.CallThread<mcValue>(() => func.EvaluateFunc());
                        res.Value = func.EvaluateFunc();
                        res.isValue = (res.Value != null) && !res.Value.isFunction();
                        res.Text = "";
                        if (res.Value != null)
                        {
                            int nDec = mc.cfg.resFormatDecimals;
                            if ((res.Value.valueType == mcValueType.Vector) && (nDec < 0)) nDec = Math.Min(2, nDec); // vectors with 2 decimals max
                            res.Text = res.Value.ToString(mc.cfg.resFormatSeparator ?? "", nDec);
                        }
                        res.isConstant = func.isConstant();
                    }
                    catch (Exception e)
                    {
                        res.isValue = false;
                        res.isError = true;
                        res.Text = e.Message.Replace("\r\n", " ").Replace("\n", " ");
                    }
                    finally
                    {
                        mc.cacheFuncClear();
                    }
                    break;
                case mcExpType.NewDef:
                    Text = ""; // Text="new def";
                    break;
            }
            // correct some error messsages
            if (res.isError)
            {
                res.Text = res.Text.Replace("Parameter name: x", "");
            }
            res.expType = extendedType;
            return res;
        }


    }



}
