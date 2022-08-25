
using CalculatorNotepad;
using TestCases;
using Numbers;
using Mpfr.Native;
using Double = Numbers.Double;


StringTests.doTests();
SpeedTests.doTests();
InterfaceTests.doTests();

// custom tests here
void msg(string sta) => Console.WriteLine(sta);

void create_pow10(int N)
{
    MPFR.SetDefaultPrecision(200);
    string doOne(int sign=+1)
    {
        var s = Environment.NewLine + Environment.NewLine + "        private static readonly Quad[] _powers10" + (sign>0? "pos":"neg")+" = {";
        var log210 = MPFR.Log2(10);
        for (int i = 0; i <= N; i++){
            if (i % 10 == 0)
                s += Environment.NewLine+ "            ";
            //construvt Quad's amnticca and exponent for 10^(sign * 2^i )
            var y =  sign * (1L<<i) * log210;
            long exp = (long)MPFR.Floor(y) - 63;
            var man = MPFR.Round(MPFR.Exp2(MPFR.Fraction(y) + (sign > 0 ? 63 : 64)));
            ulong mantissa = (ulong)man & Quad._notHighestBit;
            // test for smaller i
            if (i < 25)
            {
                var m = MPFR.Exp10(sign * MPFR.Exp2(i));
                var q = (Quad)m;
                var q2 = new Quad(mantissa, exp);
                if (q.SignificandBits != mantissa || q.Exponent != exp)
                {
                    Console.WriteLine("Diff for i= " + (i*sign) + " : m= " + (q.SignificandBits - mantissa) + " , e= " + (q.Exponent - exp));
                }
            }
            s += "new Quad(" + mantissa.ToString() + "ul, " + exp + ",52), ";
        }
        s+= Environment.NewLine + "        };";
        return s;
    }
    var res = doOne(-1) + doOne(+1);
    File.WriteAllText("power10.txt", res);
}
//create_pow10(61);



/*
Double a = 1.2345;
Quad b = 1.2345;
 
Console.WriteLine("a.Add(b) = (" + a+").Add("+b+") = "+ a.Add(b));
Console.WriteLine("Number.Add(a,b) = (" + a + " + " + b + ") = " + Number.Add(a,b));
Console.WriteLine("Number.diffDigit(a,b) = diff(" + a + " , " + b + ") = " + Number.diffDigit(a, b));
Quad z = 1500;
Console.WriteLine("Number.Gamma("+z+")=  " + Number.Gamma(z));
Console.WriteLine("Quad.Gamma(" + z + ")=    " + Quad.Gamma(z));
z = 13;
Console.WriteLine("Number.Factorial(" + z + ")=    " + Number.Factorial(z));
Console.WriteLine("Quad.Factorial(" + z + ")=      " + Quad.Factorial(z));
*/

//msg("sin(3.14)="+Math.Sin(3.14)+" , sin(1e300)="+ Math.Sin(1e300));






void tb(params string[] vd) 
{
    foreach (var v in vd)
    {
        var m = (MPFR)v;
        var q = (Quad)m;
        var d = (double)m;
        var mB = MPFR.ToBinary(m);
        var qB = Quad.ToBinary(q);
        var dB = Double.ToBinary(d);
        if (mB != qB || qB != dB || mB != dB)
        {
            msg("Bin Diff for : " + v);
            msg("     M= " + mB);
            msg("     Q= " + qB);
            msg("     D= " + dB);
        }
        var m2 = MPFR.FromBinary(mB);
        var q2 = Quad.FromBinary(qB);
        var d2 = Double.FromBinary(dB);
        var s = "";
        if (m != m2) s += "M(" + m + "!=" + m2 + ") ";
        if (q != q2) s += "Q(" + q + "!=" + q2 + ") ";
        if (d != d2) s += "D(" + d + "!=" + d2 + ") ";
        if (s != "")
            msg("Value diff OWN : "+s);
        var m3 = MPFR.FromBinary(mB);
        var q3 = Quad.FromBinary(mB);
        var d3 = Double.FromBinary(mB);
        s = "";
        if (m != m3) s += "M(" + m + "!=" + m3 + ") ";
        if (q != q3) s += "Q(" + q + "!=" + q3 + ") ";
        if (d != d3) s += "D(" + d + "!=" + d3 + ") ";
        if (s != "")
            msg("Value diff MPFR : " + s);
    }
}



void tc(double a, double b)
{
    var ba = Double.ToBinary(a);
    var bb = Double.ToBinary(b);
    void oneI(string cmpS, int c, int cb)
    {
        var s = a.ToString() + " " + cmpS + " " + b.ToString() + " = " + c;
        if (c != cb)
            s="Double "+s+" , Binary = "+cb;
        msg(s);
    }
    void one(string cmpS, bool c, bool cb)
    {
        var s = a.ToString() + " " + cmpS + " " + b.ToString() + " = " + c;
        if (c != cb)
            s = "Double " + s + " , Binary = " + cb;
        msg(s);
    }
    oneI("CompareTo" , a.CompareTo(b), ba.CompareTo(bb));
    one( " == " , a==b, ba==bb);
    one(" != ", a != b, ba != bb);
    one( " > ", a > b, ba > bb);
    one(" < ", a < b, ba < bb);
    one(" >= ", a >= b, ba >= bb);
    one(" <= ", a <= b, ba <= bb);
}

/*
// anything compared to NaN is false, except != is true. any CompareTo assume NaN is SMALLEST number
tc(double.NaN,double.NaN);
tc(double.PositiveInfinity, double.NaN);
tc(double.NaN, double.PositiveInfinity);
tc(double.NegativeZero, double.NaN);
tc(double.NegativeInfinity, double.NaN);
tc(double.NaN, double.NegativeInfinity);
tc(double.NaN,5);
tc(5,double.NaN);

tc(double.PositiveInfinity, double.PositiveInfinity);
tc(double.NegativeInfinity, double.PositiveInfinity);
tc(5, double.PositiveInfinity);
tc(double.PositiveInfinity,5);
tc(double.NegativeZero, 0);
*/

//tb("0");
//tb("3.1415926535897932384626","-1","-Inf","0","-0", "-1.1");

void dspRuler(string prefix, int N, bool simple=false, int bitCount = -1, int dstPos = int.MinValue, int srcPos = int.MinValue )
{
    string sH = prefix, sD = sH, sJ = sH, sL = sH;
    for (int i = N-1; i >= 0; i--)
    {
        if (i % 8 == (simple?7:6)) { sH += " "; sD += " "; sJ += " "; sL += " "; }
        if (i % 10 == 0)
        {
            sH += ((i / 100) % 10).ToString();
            sD += ((i / 10) % 10).ToString();
        }
        else
        {
            sH += " ";
            sD += " ";
        }
        sJ += (i % 10).ToString();
        if (bitCount > 0 && dstPos > int.MinValue && i >= dstPos && i < dstPos + bitCount)
            sL += "D";
        else if (bitCount > 0 && srcPos > int.MinValue && i >= srcPos && i < srcPos + bitCount)
            sL += "S";
        else
            sL += "-";
    }
    msg(sH);
    msg(sD);
    msg(sJ);
    msg(sL);
}

void tstArrayCopy()
{
    int srcPos = -10, dstPos = 30, bitCount = 20;
    dspRuler("     ", 128, false, bitCount, dstPos, srcPos);
    // set initial binary values
    MPFR m = 8;
    var a = MPFR.ToBinary(m / 7);
    msg("S= " + a.ToString());
    var b = MPFR.ToBinary(m / 6);
    msg("D= " + b.ToString());
    // copy part of B into A
    BinaryFloatNumber.ArrayCopyBits(a.Mantissa, srcPos, b.Mantissa, dstPos, bitCount);
    msg("D'=" + b.ToString());
}
//tstArrayCopy();

void tstArrayShift()
{
    dspRuler("        ", 128, true);
    // set initial binary value
    MPFR m = 15;
    var a = MPFR.ToBinary(m);
    BinaryFloatNumber.ArrayShiftRightBits(a.Mantissa, 124);
    msg("Source=" + a.ToStringMantissa());
    // shift a by count, inplace
    for (int i = 0; i < 130; i++)
    {
        var b = new BinaryFloatNumber(a);
        BinaryFloatNumber.ArrayShiftLeftBits(b.Mantissa, i);
        msg(i.ToString().PadLeft(3)+" << " + b.ToStringMantissa());
    }
}
//tstArrayShift();

void tstArrayAdd()
{
    dspRuler("        ", 128, true);
    // set initial binary value
    var a = new BinaryFloatNumber(SpecialFloatValue.None);
    a.Mantissa = new ulong[2];
    a.Mantissa[0] = 0xFFFFFFFFFFFFFFFFul;
    //a.Mantissa[1] = 0xFFFFFFFFFFFFFFFFul;
    msg("S= " + a.ToStringMantissa());
    UInt64 d = 0xFFFF;
    var b = new BinaryFloatNumber(a);
    bool carry = BinaryFloatNumber.ArrayAdd(b.Mantissa,d);
    msg("R= " + b.ToStringMantissa());
    msg("... with carry= "+carry+" , after adding " +d.ToString("X")+" = "+ Convert.ToString((long)d, 2));
}
//tstArrayAdd();

void tstAllign()
{
    MPFR ma = 9, mb = 1;
    var a = MPFR.ToBinary(ma);
    var b = MPFR.ToBinary(mb);
    dspRuler("     ", 128, false);
    msg("A ="+ a.ToStringMantissa());
    msg("B ="+ b.ToStringMantissa());
    (var a2, var b2) = BinaryFloatNumber.AllignNumbers(a, b);
    msg("a2=" + a2.ToStringMantissa());
    msg("b2=" + b2.ToStringMantissa());
}
//tstAllign();

void tstBitwise()
{
    // generate numbers and do bitwise op
    MPFR m = 8;
    var a = MPFR.ToBinary(m / 7);
    var b = MPFR.ToBinary(m / 6);
    var r = BinaryFloatNumber.BitwiseOperation(a, b, (a, b) => a ^ b, true );
    // show as numbers
    dspRuler("     ", 128, false);
    msg("A= " + a.ToString());
    msg("B= " + b.ToString());
    msg("^= " + r.ToString());
    // show as arrays
    dspRuler("    ", 128, true);
    msg("A =" + a.ToStringMantissa());
    msg("B =" + b.ToStringMantissa());
    msg("^= " + r.ToStringMantissa());
}
tstBitwise();


/*
int x = -8;
for (int i = 1; i < 8; i++)
{
    int y = x >> 1;
    msg(x.ToString() + " >> 1 = " + y);
    x = y;
}
  */



