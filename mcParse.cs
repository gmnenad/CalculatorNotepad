using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Numbers;

namespace CalculatorNotepad
{
    class mcParse
    {

        /// <summary>
        /// Skip pointer over string until condition is met, while updating position.
        /// Condition example: f(ch, pc)=  (ch=='\n')||(pc>=N);
        /// </summary>
        /// <param name="p"></param>
        /// <param name="text"></param>
        /// <param name="testEnd"></param>
        /// <param name="movePos"></param>
        static void skipUntil(ref int p, string text, Func<char,bool> testEnd, bool movePos = true)
        {
            while ((p < text.Length) && !testEnd(text[p] ))
            {
                var ch = text[p];
                // detect cases where original source was changed
                // param replacement f(x) -> f(01#20_x) is handled when extractMarker(0,movePos=false), so no need here
                // replace } with }§  need to be handled here ( to avoid counting added § )
                if (movePos && (ch != mcExp.secondSemicolon)) {
                    if (ch == '\n')
                    {
                        curPos.Y++;
                        curPos.X = 0;
                    } else
                        curPos.X++;
                }
                p++;
            }

        }


        /// <summary>
        /// Returns next position within string after comment (\n or end of string )
        /// </summary>
        /// <param name="text"></param>
        /// <param name="startPos"></param>
        /// <returns></returns>
        public static int skipComment(string text, int fromPos, bool movePos = false)
        {
            int p = fromPos;
            if ((p < text.Length) && (text[p] == '/') && (text.SubStr(p, 2) == "//"))
                skipUntil(ref p, text, (c) => c == '\n', movePos);
            return p;
        }


        /// <summary>
        /// Returns next position within string that is NOT a whitespace.
        /// skip whitespaces ' '\r\n\t from start of string, and also include end line comments in whitespaces
        /// Does NOT change or delete input string.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="startPos"></param>
        /// <returns></returns>
        public static int skipSpaces(string text, int fromPos, bool movePos=false)
        {
            int p = fromPos , oldP=p;
            // repeatedly skip spaces/comment/spaces/comment...
            do
            {
                // remove regular whitespaces
                skipUntil(ref p, text, (c) => !char.IsWhiteSpace(c), movePos);
                // remove comment up to the end of line \n
                oldP = p;
                p = skipComment(text, p, movePos);
            } while (p!=oldP);
            // return new position, which is first one that is NOT a whitespace, or it is text.Length
            return p;
        }

        static Point curPos, lastExtractPos;
        /// <summary>
        /// remove first N characters from referenced string
        /// also update current char/line position
        /// </summary>
        /// <param name="text"></param>
        /// <param name="startPos"></param>
        /// <returns></returns>
        static void delLeft(ref string text, int N, bool movePos=true)
        {
            N = Math.Min(N, text.Length);
            // move current position
            if (movePos)
            {
                int p = 0;
                skipUntil(ref p, text, (c) => p>=N, movePos);
            }
            // remove chars
            if (N>0)
                text = text.Remove(0, N);
        }

        /// <summary>
        /// remove desired number of chars from start, and whitespaces+comment after those 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="n"></param>
        /// <param name="movePos"></param>
        public static void removeStart(ref string text, int n = 1, bool movePos = true)
        {
            delLeft(ref text, n, movePos);
            extractSpaces(ref text, true, movePos);
        }


        /// <summary>
        /// set last extractPos to current pos, optionally adding to X
        /// </summary>
        static void setExtractPos(int toAdd=0)
        {
            lastExtractPos = curPos.add(toAdd);
        }
        /// <summary>
        /// set last extractPos to given position
        /// </summary>
        static void setExtractPos(Point exPos)
        {
            lastExtractPos = exPos;
        }

        /// <summary>
        /// change current position by given amount 
        /// </summary>
        public static void setCurrentPos(int toAdd = 0)
        {
            curPos = curPos.add(toAdd);
        }
        /// <summary>
        /// set current position  to given position
        /// </summary>
        static void setCurrentPos(Point newPos)
        {
            curPos = newPos;
        }

        /// <summary>
        /// return current position in text, based on deleted portion so far
        /// optionaly relative to given point, returns (curPos-relativeTo)
        /// </summary>
        public static Point getCurrentPosition()
        {
            return curPos;
        }

        /// <summary>
        /// return position of last extracted substring, optionaly add to X value.
        /// </summary>
        public static Point lastEx(int add=0)
        {
            return lastExtractPos.add(add);
        }

        /// <summary>
        /// reset current position AND lastExtractPos to given position
        /// </summary>
        public static void resetCurrentPosition( Point startLoc )
        {
            curPos = startLoc;
            lastExtractPos = startLoc;
        }


        /// <summary>
        /// Extract whitespaces from start of string, including comment
        /// Update position, but does not update lastExtracted
        /// </summary>
        /// <param name="text"></param>
        /// <param name="startPos"></param>
        /// <returns></returns>
        public static string extractSpaces(ref string text,  bool skipComments=true, bool movePos=true)
        {
            int p=0 ;
            if (skipComments)
                p = skipSpaces(text, 0, movePos);
            else
                skipUntil(ref p, text, (c) => !char.IsWhiteSpace(c), movePos);
            // extract and remove those spaces
            if (p > 0)
            {
                var res = text.SubStr(0, p);
                text = text.SubStr(p);
                return res;
            }
            else
                return "";
        }



        /// <summary>
        /// Split string over given words (any of them will split string)
        /// Ignore delimiters inside parentheses and comments.
        /// Returns location of each splitted line within original input string, starting with startLoc.
        /// </summary>
        /// <param name="input">Input string that will be split</param>
        /// <param name="splitChars">Delimiter chars over which to split input</param>
        /// <param name="startLoc">Start location (Point) of input string, needed to update splitPos[]</param>
        /// <param name="splitPos">Array of start locations of each split string, relative to startLoc parameter</param>
        /// <param name="options">splitOverOptions: None[def], RemoveEmptyEntries, TrimLeft</param>
        /// <param name="pOpen">List of open parentheses.  Delimiter chars are ignored inside parentheses </param>
        /// <param name="pClose">List of closed parentheses that match open ones.</param>
        /// <returns></returns>
        public static string[] splitOver(string input, string[] splitWords, Point startLoc, out Point[] splitPos, SplitOverOptions options = SplitOverOptions.None, string pOpen = "([{", string pClose = ")]}")
        {
            if (pOpen.Length != pClose.Length)
                throw new ArgumentException("Internal error, unmatched parentheses " + pOpen + " and " + pClose);
            // get options
            bool removeEmpty = (options & SplitOverOptions.RemoveEmptyEntries) != 0;
            bool trimLeft = ((options & SplitOverOptions.TrimLeft) != 0 ) || removeEmpty; // if remove empty asked, remove whitespaces and comments too
            bool movePos = (options & SplitOverOptions.doNotMovePos) == 0;
            // inits
            var res = new List<string>();
            var locs = new List<Point>();
            var pcount = new int[pOpen.Length];
            int prevStart = 0, pos = 0;
            var oldLoc = getCurrentPosition();
            if (movePos) mcParse.resetCurrentPosition(startLoc);
            //addPart part from prevStart to before pos [prevStart,pos>
            // pos is positioned at start of separator of length separatorLen, or after end of input (separatorLen==0)
            void addPart(int separatorLen)
            {
                // trimLeft with count
                if (trimLeft)
                    //skipUntil(ref prevStart, input, (c) => (prevStart >= pos) || !char.IsWhiteSpace(c), movePos);
                    prevStart=skipSpaces(input, prevStart, movePos); // this one removes comments too
                var prevLoc = getCurrentPosition();
                // just count position in remaining part, including separator
                int pos2 = prevStart;
                skipUntil(ref pos2, input, (c) => (pos2 >= pos+ separatorLen), movePos);
                // get part
                var part = input.Substring(prevStart, pos - prevStart);
                // add to list
                if (!((part == "") && removeEmpty))
                {
                    res.Add(part);
                    locs.Add(prevLoc);
                }
                // skip over separator
                pos += separatorLen - 1; // pos will advance by 1 anyway, adjust here if separator is larger than single char
                prevStart = pos + 1; // prevStart need to advance to after separator
            }
            // test if new part
            char singleCH = (splitWords != null) && (splitWords.Length == 1) && (splitWords[0].Length == 1) ? splitWords[0][0] : (char)0;
            int SeparatorLen = 0;
            // now walk over input string
            pos = skipComment(input, pos, false);
            while (pos < input.Length)
            {
                var ch = input[pos];
                int pIdx = pOpen.IndexOf(ch);
                if (pIdx >= 0)
                    pcount[pIdx]++;
                else
                {
                    pIdx = pClose.IndexOf(ch);
                    if (pIdx >= 0)
                    {
                        pcount[pIdx]--;
                        if (pcount[pIdx] < 0)
                            pcount[pIdx] = 0;
                    }
                }
                SeparatorLen = 0;
                if (pcount.Sum() == 0) {
                    if (singleCH == (char)0)  // word or multiple chars/words are separators
                    {
                        foreach (var sep in splitWords)
                            if (input.SubStr(pos, sep.Length) == sep)
                                SeparatorLen = sep.Length;
                    }
                    else
                        SeparatorLen = (ch == singleCH)?1:0; // just single char is separator
                    if (SeparatorLen>0)
                        addPart(SeparatorLen);
                }
                pos++;
                // skip comment to ignore delimiters inside comments, but do not count position (will be done on addpart )
                pos = skipComment(input, pos,false); 
            }
            // add final part, or even empty line if previous char was separator (when "abc;" , in case empty rows are needed)
            if ((pos > prevStart) || ((pos == prevStart) && (pos > 0) && (SeparatorLen>0)) )
                addPart(0);
            // fill Point locations
            splitPos = locs.ToArray();
            if (movePos) resetCurrentPosition(oldLoc);
            // and return split strings
            return res.ToArray();
        }

        /// <summary>
        /// Split string over given char 
        /// Ignore delimiters inside parentheses and comments.
        /// Returns location of each splitted line within original input string, starting with startLoc.
        /// </summary>
        public static string[] splitOver(string input, char splitChar, Point startLoc, out Point[] splitPos, SplitOverOptions options = SplitOverOptions.None, string pOpen = "([{", string pClose = ")]}")
        {
            return splitOver(input, new string[] { splitChar.ToString() }, startLoc, out splitPos, options, pOpen, pClose);
        }


        /// <summary>
        /// Split string over given words ( ignoring delimiters inside parentheses and comments)
        /// </summary>
        public static string[] splitOver(string input, string[] splitWords, SplitOverOptions options = SplitOverOptions.None, string pOpen = "([{", string pClose = ")]}")
        {
            Point[] splitPos;
            return splitOver(input, splitWords, new Point(0, 0), out splitPos, options, pOpen, pClose);
        }
        /// <summary>
        /// Split string over given char ( ignoring delimiters inside parentheses and comments)
        /// </summary>
        public static string[] splitOver(string input, char splitChar, SplitOverOptions options = SplitOverOptions.None, string pOpen = "([{", string pClose = ")]}")
        {
            Point[] splitPos;
            return splitOver(input, new string[] { splitChar.ToString() }, new Point(0, 0), out splitPos, options, pOpen, pClose);
        }



        // split commas, taking into account parentheses
        public static string[] splitCommas(string input)
        {
            return splitOver(input, ',', SplitOverOptions.doNotMovePos);
        }

        // wrapper that do optional space skipping, and set extraction point
        delegate string InnerDelegate(ref string inputStr);
        static string wrapInner(ref string inputStr, bool skipSpaces, bool movePos, InnerDelegate inner)
        {
            if (skipSpaces) extractSpaces(ref inputStr, true, movePos);
            Point exPos = getCurrentPosition();
            var rs = inner(ref inputStr);
            if (movePos&&(rs != "")) setExtractPos(exPos);
            if (skipSpaces) extractSpaces(ref inputStr, true, movePos);
            return rs;

        }


        // extracts valid number from start of string , or leave string as it is and return "" if no valid number
        public static string extractNumber(ref string inputStr, bool skipSpaces = true, bool movePos = true)
        {
            string inner(ref string input)
            {
                string Mantissa, Fraction, Suffix, CleanNumber;
                int Sign, Remainderlength, iBase;
                long Exponent;
                double specValue;
                // split string number into parts, only to detect if number is valid and length of remainder
                var F = new NumberFormat { Base= -1, ThousandsSeparator= "_,`" + mc.cfg.resFormatSeparator, FractionSeparator= mc.cfg.resFractionSeparator };
                string ? err = BinaryFloatNumber.ParseParts(out CleanNumber, out Mantissa, out Fraction, out Suffix, out Sign, out iBase, out Exponent, out Remainderlength, out specValue, input, F);
                if (err != null)
                    return "";
                int pos = input.Length - Remainderlength;
                string res = input.SubStr(0, pos);
                delLeft(ref input, pos);
                return res;
            }
            // remove spaces if needed, and set last extraction point
            return wrapInner(ref inputStr, skipSpaces, movePos, inner);
        }

        // extracts name from start of string , assume first letter already checked
        static public string extractName(ref string inputStr, bool skipSpaces = true, bool movePos=true)
        {
            string inner(ref string input)
            {
                var ch = input.Length > 0 ? input[0] : ' ';
                string res = "";
                if ("π∑∫∏".Contains(ch))
                {
                    res = input.Substring(0, 1);
                    delLeft(ref input, 1, movePos);
                    return res;
                }
                // variable or function name must start with letter ( or Ɵ for param names)
                if (ch == mcExp.paramDelimiter)
                {
                    res = mcParse.extractBetweenMarkers(ref input, mcExp.paramDelimiter.ToString(), mcExp.paramDelimiter.ToString(), false, false, false);
                }
                if (char.IsLetter(ch)) 
                {
                    int pos = 0;
                    bool wasOk = true;
                    while ((pos < input.Length) && wasOk)
                    {
                        ch = input[pos];
                        // other chars can be also digits or underscore
                        wasOk = char.IsLetterOrDigit(ch) || "_".Contains(ch);
                        if (wasOk)  pos++;
                    }
                    res += input.Substring(0, pos);
                    delLeft(ref input, pos, movePos);
                    return res;
                }
                else
                    return "";
            }
            // remove spaces if needed, and set last extraction point
            return wrapInner(ref inputStr, skipSpaces, movePos, inner);
        }


        // Out string between brackets in 'insideBrackets' , and return true if valid parentheses
        //    - if ExtractOptions.RemoveFound is given , remove parentheses and their insides (and spaces) from input text
        //    - start at 'pos' location and extracts between given bracket type hierarchically [ leave [] brackets inside brackets ]  
        //    - skip whitespaces at start if ExtractOptions.skipSpacesStart
        //    - if it does not have given brackets at start, or does not have matching closing brackets, fail

        static public bool extractBracketType(ref string input, out string insideBrackets, char openP, char closeP, int pos=0, ExtractOptions options= ExtractOptions.None)
        {
            // get flags
            bool removeFound = (options & ExtractOptions.RemoveFound)!= 0;
            bool returnBrackets = (options & ExtractOptions.returnBrackets) != 0;
            bool skipSpacesStart = (options & ExtractOptions.skipSpacesStart) != 0;
            bool skipSpacesEnd = (options & ExtractOptions.skipSpacesEnd) != 0;
            bool movePos = ((options & ExtractOptions.doNotMovePos) == 0) &&  removeFound;
            // process
            insideBrackets = "";
            int iLen = input.Length;
            // skip starting spaces if needed
            int posStart = pos;
            if (movePos)
            {
                if (skipSpacesStart)  extractSpaces(ref input);
                setExtractPos(returnBrackets?0:1);
            }else
                if (skipSpacesStart)
                    pos = skipSpaces(input, pos, false);
            // check if this start with given parentheses, and has at least space for 2 chars (open/close)
            int posOpenP = pos;
            if ((pos >= iLen-1) || (input[pos] != openP))
                return false;
            // skip over other parentheses, until end of first one is found
            pos++;
            int pcount = 1; // number of open parentheses
            while ((pos < input.Length) && (pcount > 0))
            {
                pos=skipSpaces(input, pos, false); // skip spaces inside, in order to skip over comments and any ()} within those comments
                if (pos < input.Length)
                {
                    if (input[pos] == openP) pcount++;
                    if (input[pos] == closeP) pcount--;
                    pos++;
                }
            }
            int posCloseP = pos-1;
            if ((pcount != 0)|| (posCloseP>=iLen)|| (posOpenP>=posCloseP))
            {
                return false; // parentheses not closing properly
            }
            else
            {
                // extract result
                if (returnBrackets)
                    insideBrackets = input.Substring(posOpenP, posCloseP - posOpenP + 1);
                else
                    insideBrackets = input.Substring(posOpenP+1, posCloseP - posOpenP - 1);
                // if removal of found is needed
                if (removeFound)
                {
                    // skip also spaces at end if needed
                    if (skipSpacesEnd)
                        pos = skipSpaces(input, pos,false);
                    // 'pos' is first char that does not need removal
                    if (movePos)
                        delLeft(ref input, pos); 
                    else
                        input = input.Remove(posStart, pos - posStart);
                }
                return true;
            }
        }

        // extracts parenthesses group from start of string , anything within leading parentheses. throw exception if invalid
        public static string extractParentheses(ref string inputStr, bool skipSpaces = true)
        {
            string res;
            var options = ExtractOptions.RemoveFound;
            if (skipSpaces) options |= ExtractOptions.SkipSpaces;
            if (extractBracketType(ref inputStr, out res, '(', ')', 0, options))
                return res;
            else
                throw new ArgumentException("Invalid ( ) parentheses!");
        }


        // extracts square brackets, hierarchically [ leave [] brackets inside brackets ] . return empty if not present or invalid
        static public string extractBrackets(ref string inputStr, bool removeBrackets = true, bool skipSpaces = true)
        {
            string res;
            var options = ExtractOptions.RemoveFound;
            if (skipSpaces) options |= ExtractOptions.SkipSpaces;
            if (extractBracketType(ref inputStr, out res, '[', ']', 0, options))
                return res;
            else
                return "";
        }


        // extracts between two markers, non- hierrchically. return empty if no markers, otherwise return what is between markers, and markers if removeMarkers = false
        static public string extractBetweenMarkers(ref string inputStr, string markerStart, string markerEnd, bool removeMarkers = true, bool skipSpaces = true, bool movePos=true)
        {
            string inner(ref string input)
            {
                if (input.SubStr(0, markerStart.Length) != markerStart)
                    return "";
                int pEnd = input.IndexOf(markerEnd, markerStart.Length);
                if (pEnd > 0)
                {
                    var res = removeMarkers ? input.SubStr(markerStart.Length, pEnd - markerStart.Length) : input.SubStr(0, pEnd + markerEnd.Length).Trim();
                    delLeft(ref input, pEnd + markerEnd.Length, movePos);
                    return res;
                }
                else
                    return "";
            }
            // remove spaces if needed, and set last extraction point
            return wrapInner(ref inputStr, skipSpaces, movePos, inner);
        }


        // extracts result : ~, ~123e-17, ~(-123.45), last  ... no spaces
        static public string extractLastResult(ref string input, bool skipSpaces = true, bool movePos=true)
        {
            string inner(ref string inputStr)
            {
                string txt = inputStr;
                if ((mc.cfg.sensitivity == mcCaseSensitivity.Insensitive) || (mc.cfg.sensitivity == mcCaseSensitivity.Dynamic))
                    txt = txt.ToLower();
                if (txt.SubStr(0, 1) == "~")
                {
                    if (txt.SubStr(0, 3) == "~vec")
                    {
                        delLeft(ref inputStr, 4);
                        extractParentheses(ref inputStr);
                        return "~vec"+inputStr; // vector is also a number, ie it need replacing
                    }
                    if (txt.SubStr(0, 3) == "~(-")
                    {
                        delLeft(ref inputStr, 3);
                        extractNumber(ref inputStr);
                        if (inputStr.SubStr(0, 1) == ")")
                            delLeft(ref inputStr, 1);
                        return "-" + inputStr;
                    }
                    // check if this is number behind
                    delLeft(ref inputStr, 1);
                    var num = extractNumber(ref inputStr);
                    // if it was number, it needs replacing
                    return num;
                }
                return "";
            }
            // remove spaces if needed, and set last extraction point
            return wrapInner(ref input, skipSpaces, movePos, inner);
        }



        // TryParseNumber from string, allowing for hexa, decimals, exponents, leading sign etc...
        public static bool TryParseNumber(string input, out Number result) {
            var F = new NumberFormat(-1, -1, true, 4, -1, mc.cfg.resFormatSeparator+"`", mc.cfg.resFractionSeparator);
            return Number.TryParse(input, out result, F);
        }

        // convert float to string, use exponents when needed, dec/hex/bin based on current settings, also allow group separators (_,)
        public static string NumberToStr(Number num, string groupSeparator = "", int maxDecimals = -1, bool? ShowNumberType=null)
        {
            // call float type conversion
            var F = new NumberFormat(mc.defaultNumberBase, maxDecimals, true, 4, -1, mc.cfg.resFormatSeparator, mc.cfg.resFractionSeparator, -1,-1,ShowNumberType);
            return num.ToString(F);
        }


        /// <summary>
        /// Return comment text if line starts with at least 3 slashes (///)
        /// Removes starting backslashes and split in multilines if \r\n exists
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public static string toolTipComment(string comment, bool addNewLine=false)
        {
            var res = comment.Trim();
            int p = 0;
            while ((res.Length > p) && (res[p] == '/')) p++;
            if (p > 2)
            {
                // there are more than two backslashes, so this is descriptor ///
                res= res.SubStr(p).Replace("\\r", "\r").Replace("\\n", "\n").Replace("\r\n","\n").Replace("\r","\n"); // replace \r\n with actual new lines
                var lines = res.Split('\n');
                res = "";
                foreach(var ln in lines)
                {
                    if (res != "") res += Environment.NewLine;
                    // remove ONLY first space, to allow /// text1\n text2\n text3  - spaces between lines, but also /// text1\n    text2 - idented 2nd line
                    if ((ln.Length > 0) && (ln[0] == ' '))
                        res += ln.SubStr(1);
                    else
                        res += ln;
                }
                if (addNewLine) res += Environment.NewLine; // add final newline if requested
                return res;
            }
            else
                return "";
        }


    }




}
