using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

// ToDo:
// +remove need for instance methods
//      x they can remain in descandand classes for optional 'in place' math , ie a.Add(b) will change a and return a itself
//      + in Number static methods use switch (Max(a.classType, b.classType)) { }
//      x test difference in speed on Double.Add
// + make abstract properties long 'Exponent', int 'Sign' and UInt64[] 'Mantissa', and Mantissa can return null if doesnt know .
// + use Precision in ToStringFromBits
// + check mcMath and other mc functions that still use double
// x make numbers to be STRUCTs ?! 
//      x MPFR can not, since it need deconstructor
//      x alternativelly make data fields readonly, disable self ++ ( in C# "x++" is anyway interpreted as "x=x+1" )
// + check why publishing to single exe file does not work ?
// - see whats missing to cover entire new INumber interface
// + get MPFR raw bytes
//      + for GetHashCode
//      + for binary conversions
// + BinaryFloatNumber class for bitwise operations
//      - option for negative numbers to be treated as 2nd complement in bitwise ops
// - bug: compiler can not find 'mcValue' when published as single file app ?



namespace Numbers
{


    public class Number : IFloatNumber<Number>
    {

        #region Public static properties

        private static NumberClass __defaultClassType = NumberClass.Double;
        private static int __defaultPrecision = 63;

        /// <summary>
        /// which type to use when getting constants(Number.PI) or doing Number.Parse() or using Number.Create() cast 
        /// </summary>
        public static NumberClass defaultClassType {
            get => __defaultClassType;
            set
            {
                if (!Enum.IsDefined(typeof(NumberClass), value))
                    throw new NumberException("Setting invalid floating number type : " + value);
                __defaultClassType = value;
            }
        }
        /// <summary>
        /// what precision to use when creating numbers with defaultClassType 
        /// </summary>
        public static int defaultPrecision
        {
            get => __defaultPrecision;
            set
            {
                if (value < 2)
                    return;
                __defaultPrecision = value;
                MPFR.SetDefaultPrecision(__defaultPrecision);
            }
        }
        public static Number NaN => __defaultClassType switch
        {
            NumberClass.Double => new(Double.NaN),
            NumberClass.Quad => new(Quad.NaN),
            NumberClass.MPFR => new(MPFR.NaN),
            _ => throw new NumberException("Invalid floating number type : " + __defaultClassType)
        };
        public static Number PositiveInfinity => __defaultClassType switch
        {
            NumberClass.Double => new(Double.PositiveInfinity),
            NumberClass.Quad => new(Quad.PositiveInfinity),
            NumberClass.MPFR => new(MPFR.PositiveInfinity),
            _ => throw new NumberException("Invalid floating number type : " + __defaultClassType)
        };
        public static Number NegativeInfinity => __defaultClassType switch
        {
            NumberClass.Double => new(Double.NegativeInfinity),
            NumberClass.Quad => new(Quad.NegativeInfinity),
            NumberClass.MPFR => new(MPFR.NegativeInfinity),
            _ => throw new NumberException("Invalid floating number type : " + __defaultClassType)
        };
        public static Number MaxValue => __defaultClassType switch
        {
            NumberClass.Double => new(Double.MaxValue),
            NumberClass.Quad => new(Quad.MaxValue),
            NumberClass.MPFR => new(MPFR.MaxValue),
            _ => throw new NumberException("Invalid floating number type : " + __defaultClassType)
        };
        public static Number MinValue =>__defaultClassType switch
        {
            NumberClass.Double => new(Double.MinValue),
            NumberClass.Quad => new(Quad.MinValue),
            NumberClass.MPFR => new(MPFR.MinValue),
            _ =>throw new NumberException("Invalid floating number type : " + __defaultClassType)
        };
        public static Number Zero =>__defaultClassType switch
        {
            NumberClass.Double => new(Double.Zero),
            NumberClass.Quad => new(Quad.Zero),
            NumberClass.MPFR => new(MPFR.Zero),
            _ =>throw new NumberException("Invalid floating number type : " + __defaultClassType)
        };
        public static Number NegativeZero => __defaultClassType switch
        {
            NumberClass.Double => new(Double.NegativeZero),
            NumberClass.Quad => new(Quad.NegativeZero),
            NumberClass.MPFR => new(MPFR.NegativeZero),
            _ => throw new NumberException("Invalid floating number type : " + __defaultClassType)
        };
        public static Number One =>__defaultClassType switch
        {
            NumberClass.Double => new(Double.One),
            NumberClass.Quad => new(Quad.One),
            NumberClass.MPFR => new(MPFR.One),
            _ =>throw new NumberException("Invalid floating number type : " + __defaultClassType)
        };
        public static Number PI =>__defaultClassType switch
        {
            NumberClass.Double => new(Double.PI),
            NumberClass.Quad => new(Quad.PI),
            NumberClass.MPFR => new(MPFR.PI),
            _ =>throw new NumberException("Invalid floating number type : " + __defaultClassType)
        };
        public static Number PIx2 =>__defaultClassType switch
        {
            NumberClass.Double => new(Double.PIx2),
            NumberClass.Quad => new(Quad.PIx2),
            NumberClass.MPFR => new(MPFR.PIx2),
            _ =>throw new NumberException("Invalid floating number type : " + __defaultClassType)
        };
        public static Number E =>__defaultClassType switch
        {
            NumberClass.Double => new(Double.E),
            NumberClass.Quad => new(Quad.E),
            NumberClass.MPFR => new(MPFR.E),
            _ =>throw new NumberException("Invalid floating number type : " + __defaultClassType)
        };
        public static Number Euler => __defaultClassType switch
        {
            NumberClass.Double => new(Double.Euler),
            NumberClass.Quad => new(Quad.Euler),
            NumberClass.MPFR => new(MPFR.Euler),
            _ => throw new NumberException("Invalid floating number type : " + __defaultClassType)
        };
        public static Number Catalan => __defaultClassType switch
        {
            NumberClass.Double => new(Double.Catalan),
            NumberClass.Quad => new(Quad.Catalan),
            NumberClass.MPFR => new(MPFR.Catalan),
            _ => throw new NumberException("Invalid floating number type : " + __defaultClassType)
        };

        #endregion

        #region instance properties

        /// <summary>
        /// class with higher  number can 'contain' lower, based on exponent range. Quad=30, MPFR=20, Double=10
        /// </summary>
        private NumberClass _classType;
        public virtual NumberClass classType => _classType;


        /// <summary>
        /// Exponent of float value, assuming mantissa is UInt64[] in  1.xxxx format with highest bit in last element as only 'whole part' and others are fractional.
        /// So 1.0 has exponent 0 and mantissa "1000..00" 
        /// </summary>
        public unsafe long Exponent
        {
            get
            {
                switch (classType)
                {
                    case NumberClass.Double:
                        double d = (double)valueDouble;
                        ulong bits = *(ulong*)&d;
                        return (long)((bits >> 52) & 0x7FF) - 1023; // IEEE double has exponents biased for 1023
                    case NumberClass.Quad:
                        return valueQuad.Exponent + 63; // Quad has exponent shifted -63 for mantissa bits
                    case NumberClass.MPFR:
                        return valueMPFR.Exponent + 1; // MPFR assume 0.xxxx mantissa
                };
                throw new NumberException("Invalid Number class ");
            }
        }

        /// <summary>
        /// How many useful bits in Mantissa, including implied highest "1." bit
        /// </summary>
        public int Precision => classType switch
        {
            NumberClass.Double => 53,
            NumberClass.Quad => valueQuad.Precision,
            NumberClass.MPFR => valueMPFR.Precision,
            _ => throw new NumberException("Invalid floating number type : " + classType)
        };


        /// <summary>
        /// Mantissa of float value as UInt64[] array in  1.xxxx format with highest bit in last element as only 'whole part' and others are fractional.
        /// So 1.0 has exponent 0 and mantissa "1000..00" 
        /// </summary>
        public unsafe UInt64[] Mantissa
        {
            get
            {
                switch (classType)
                {
                    case NumberClass.Double:
                        double d = (double)valueDouble;
                        ulong bits = *(ulong*)&d;
                        return new UInt64[1] { (bits << 11) | (1ul << 63) }; // IEEE double has 52 mantissa in lowest 52 bits, with implied 53rd bit as 1
                    case NumberClass.Quad:
                        return new UInt64[1] { valueQuad.SignificandBits | (1ul << 63) }; // Quad has 63 bit mantissa with implied 64th bit 1, and actual 64th bit is sign
                    case NumberClass.MPFR:
                        return valueMPFR.Mantissa; // MPFR has mantissa in same format, as UInt64[] array with highest bit set to 1 in highest element
                };
                throw new NumberException("Invalid Number class ");
            }
        }

        public Double AsDouble
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (classType)
                {
                    case NumberClass.Double:
                        return (Double)valueDouble;
                    case NumberClass.Quad:
                        return (Double)valueQuad;
                    case NumberClass.MPFR:
                        return (Double)valueMPFR;
                };
                throw new NumberException("Invalid Number class ");
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                throw new NumberException("Can not change Number type - yet ");
            }
        }

        public Quad AsQuad
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (classType)
                {
                    case NumberClass.Double:
                        return (Quad)valueDouble;
                    case NumberClass.Quad:
                        return (Quad)valueQuad;
                    case NumberClass.MPFR:
                        return (Quad)valueMPFR;
                };
                throw new NumberException("Invalid Number class ");
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                throw new NumberException("Can not change Number type - yet ");
            }
        }

        public MPFR AsMPFR
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (classType)
                {
                    case NumberClass.Double:
                        return (MPFR)valueDouble;
                    case NumberClass.Quad:
                        return (MPFR)valueQuad;
                    case NumberClass.MPFR:
                        return (MPFR)valueMPFR;
                };
                throw new NumberException("Invalid Number class ");
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                throw new NumberException("Can not change Number type - yet ");
            }
        }

        /// <summary>
        /// Checked int conversion, since it does not have Inf
        /// </summary>
        public int AsInt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (this > int.MaxValue || this < int.MinValue)
                    throw new InvalidCastException("Number " + ToString() + " is too large to fit into Int ");
                return (int)this;
            }
        }

        /// <summary>
        /// Checked Long conversion, since it does not have Inf
        /// </summary>
        public long AsLong
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (this > long.MaxValue || this < long.MinValue)
                    throw new InvalidCastException("Number " + ToString() + " is too large to fit into Long ");
                return (long)this;
            }
        }

        /// <summary>
        /// Checked Ulong conversion, since it does not have Inf
        /// </summary>
        public ulong AsUlong
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (this > ulong.MaxValue || this < ulong.MinValue)
                    throw new InvalidCastException("Number " + ToString() + " is too large to fit into Ulong ");
                return (ulong)this;
            }
        }


        #endregion

        #region Constructors and Data fields

        private Double valueDouble;
        private Quad valueQuad;
        private MPFR valueMPFR;

        protected Number()
        {
        }

        public Number(double a)
        {
            switch (__defaultClassType)
            {
                case NumberClass.Double: valueDouble = a; break;
                case NumberClass.Quad: valueQuad = a; break;
                case NumberClass.MPFR: valueMPFR = a; break;
                default:
                    throw new NumberException("Invalid floating number type : " + __defaultClassType);
            }
            _classType = __defaultClassType;
        }

        public Number(long a)
        {
            switch (__defaultClassType)
            {
                case NumberClass.Double: valueDouble = a; break;
                case NumberClass.Quad: valueQuad = a; break;
                case NumberClass.MPFR: valueMPFR = a; break;
                default:
                    throw new NumberException("Invalid floating number type : " + __defaultClassType);
            }
            _classType = __defaultClassType;
        }

        public Number(ulong a)
        {
            switch (__defaultClassType)
            {
                case NumberClass.Double: valueDouble = a; break;
                case NumberClass.Quad: valueQuad = a; break;
                case NumberClass.MPFR: valueMPFR = a; break;
                default:
                    throw new NumberException("Invalid floating number type : " + __defaultClassType);
            }
            _classType = __defaultClassType;
        }

        // must use copy-on-construct new NumberType(a) if in-place changes are allowed in NumberType ( eg Q++ also changes Q ) and NumberType is class instead of struct
        // would also be needed if NumberType is struct that has class data member ( eg if MPFR is made to be struct, but data field X is class )
        // otherwise can directly store 
        public Number(Double a)
        {
            valueDouble = a;
            _classType = NumberClass.Double;
        }
        public Number(Quad a)
        {
            valueQuad = a;
            _classType = NumberClass.Quad;
        }
        public Number(MPFR a)
        {
            valueMPFR = new MPFR(a); // copy on create
            _classType = NumberClass.MPFR;
        }

        #endregion

        #region Casts


        public static implicit operator Number(double a) => new Number(a);
        public static implicit operator Number(long a) => new Number(a);
        public static implicit operator Number(ulong a) => new Number(a);
        public static implicit operator Number(int a) => new Number((long)a);
        public static implicit operator Number(uint a) => new Number((ulong)a);
        public static implicit operator Number(Double a) => new Number(a);
        public static implicit operator Number(Quad a) => new Number(a);
        public static implicit operator Number(MPFR a) => new Number(a);



        // explicit casts from Number descendants to basic C# types
        public static explicit operator double(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return (double) a.valueDouble;
                case NumberClass.Quad: return (double) a.valueQuad;
                case NumberClass.MPFR: return (double) a.valueMPFR;
            }
            throw new NumberException("Invalid Number class ");
        }
        public static explicit operator long(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return (long)a.valueDouble;
                case NumberClass.Quad: return (long)a.valueQuad;
                case NumberClass.MPFR: return (long)a.valueMPFR;
            }
            throw new NumberException("Invalid Number class ");
        }

        public static explicit operator ulong(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return (ulong)a.valueDouble;
                case NumberClass.Quad: return (ulong)a.valueQuad;
                case NumberClass.MPFR: return (ulong)a.valueMPFR;
            }
            throw new NumberException("Invalid Number class ");
        }
        public static explicit operator int(Number a) => (int)(long)a;
        public static explicit operator uint(Number a) => (uint)(ulong)a;


        public static Number FromBinary(BinaryFloatNumber bf)
        {
            switch (__defaultClassType)
            {
                case NumberClass.Double: return new(Double.FromBinary(bf));
                case NumberClass.Quad: return new(Quad.FromBinary(bf));
                case NumberClass.MPFR: return new(MPFR.FromBinary(bf));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static BinaryFloatNumber ToBinary(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return Double.ToBinary(a.valueDouble);
                case NumberClass.Quad: return Quad.ToBinary(a.valueQuad);
                case NumberClass.MPFR: return MPFR.ToBinary(a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }



        #endregion

        #region OPERATORS ( static, use instance overrides )
        // unary operations
        public static Number operator -(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(-a.valueDouble);
                case NumberClass.Quad: return new(-a.valueQuad);
                case NumberClass.MPFR: return new(-a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }
        public static Number operator ++(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: a.valueDouble++;  return a;  
                case NumberClass.Quad: a.valueQuad++; return a; 
                case NumberClass.MPFR: a.valueMPFR++; return a;
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number operator --(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: a.valueDouble--; return a;
                case NumberClass.Quad: a.valueQuad--; return a;
                case NumberClass.MPFR: a.valueMPFR--; return a;
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number operator <<(Number a, int shift)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(a.valueDouble << shift);
                case NumberClass.Quad: return new(a.valueQuad << shift);
                case NumberClass.MPFR: return new(a.valueMPFR << shift);
            }
            throw new NumberException("Invalid Number class ");
        }


        public static Number operator >>(Number a, int shift)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(a.valueDouble >> shift);
                case NumberClass.Quad: return new(a.valueQuad >> shift);
                case NumberClass.MPFR: return new(a.valueMPFR >> shift);
            }
            throw new NumberException("Invalid Number class ");
        }


        // binary operations

        public static Number operator +(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return new(a.AsDouble + b.AsDouble);
                case NumberClass.Quad: return new(a.AsQuad + b.AsQuad);
                case NumberClass.MPFR: return new(a.AsMPFR + b.AsMPFR);
            }
            throw new NumberException("Invalid Number class combination");
        }
        public static Number operator -(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return new(a.AsDouble - b.AsDouble);
                case NumberClass.Quad: return new(a.AsQuad - b.AsQuad);
                case NumberClass.MPFR: return new(a.AsMPFR - b.AsMPFR);
            }
            throw new NumberException("Invalid Number class combination");
        }

        public static Number operator *(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return new(a.AsDouble * b.AsDouble);
                case NumberClass.Quad: return new(a.AsQuad * b.AsQuad);
                case NumberClass.MPFR: return new(a.AsMPFR * b.AsMPFR);
            }
            throw new NumberException("Invalid Number class combination");
        }


        public static Number operator /(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return new(a.AsDouble / b.AsDouble);
                case NumberClass.Quad: return new(a.AsQuad / b.AsQuad);
                case NumberClass.MPFR: return new(a.AsMPFR / b.AsMPFR);
            }
            throw new NumberException("Invalid Number class combination");
        }

        public static Number operator %(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return new(a.AsDouble % b.AsDouble);
                case NumberClass.Quad: return new(a.AsQuad % b.AsQuad);
                case NumberClass.MPFR: return new(a.AsMPFR % b.AsMPFR);
            }
            throw new NumberException("Invalid Number class combination");
        }

        public static Number operator |(Number a, Number b) => FromBinary(ToBinary(a) | ToBinary(b));
        public static Number operator &(Number a, Number b) => FromBinary(ToBinary(a) & ToBinary(b));
        public static Number operator ^(Number a, Number b) => FromBinary(ToBinary(a) ^ ToBinary(b));


        #endregion

        #region static Comparisons and instance GetHashCode/EqualsTo


        public static bool operator ==(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return a.AsDouble == b.AsDouble;
                case NumberClass.Quad: return a.AsQuad == b.AsQuad;
                case NumberClass.MPFR: return a.AsMPFR == b.AsMPFR;
            }
            throw new NumberException("Invalid Number class combination");
        }
        public static bool operator !=(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return a.AsDouble != b.AsDouble;
                case NumberClass.Quad: return a.AsQuad != b.AsQuad;
                case NumberClass.MPFR: return a.AsMPFR != b.AsMPFR;
            }
            throw new NumberException("Invalid Number class combination");
        }

        public static bool operator <(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return a.AsDouble < b.AsDouble;
                case NumberClass.Quad: return a.AsQuad < b.AsQuad;
                case NumberClass.MPFR: return a.AsMPFR < b.AsMPFR;
            }
            throw new NumberException("Invalid Number class combination");
        }

        public static bool operator >(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return a.AsDouble > b.AsDouble;
                case NumberClass.Quad: return a.AsQuad > b.AsQuad;
                case NumberClass.MPFR: return a.AsMPFR > b.AsMPFR;
            }
            throw new NumberException("Invalid Number class combination");
        }

        public static bool operator >=(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return a.AsDouble >= b.AsDouble;
                case NumberClass.Quad: return a.AsQuad >= b.AsQuad;
                case NumberClass.MPFR: return a.AsMPFR >= b.AsMPFR;
            }
            throw new NumberException("Invalid Number class combination");
        }

        public static bool operator <=(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return a.AsDouble <= b.AsDouble;
                case NumberClass.Quad: return a.AsQuad <= b.AsQuad;
                case NumberClass.MPFR: return a.AsMPFR <= b.AsMPFR;
            }
            throw new NumberException("Invalid Number class combination");
        }


        public override int GetHashCode() => classType switch
        {
            NumberClass.Double => valueDouble.GetHashCode(),
            NumberClass.Quad => valueQuad.GetHashCode(),
            NumberClass.MPFR => valueMPFR.GetHashCode(),
            _ => throw new NumberException("Invalid floating number type : " + classType)
        };

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            try
            {
                return this == (Number)obj;
            }
            catch
            {
                return false;
            }
        }


        #endregion

        #region isSpecial
        public static bool IsNaN(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return Double.IsNaN(a.valueDouble);
                case NumberClass.Quad: return Quad.IsNaN(a.valueQuad);
                case NumberClass.MPFR: return MPFR.IsNaN(a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }

        public static bool IsInfinity(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return Double.IsInfinity(a.valueDouble);
                case NumberClass.Quad: return Quad.IsInfinity(a.valueQuad);
                case NumberClass.MPFR: return MPFR.IsInfinity(a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }
        public static bool IsPositiveInfinity(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return Double.IsPositiveInfinity(a.valueDouble);
                case NumberClass.Quad: return Quad.IsPositiveInfinity(a.valueQuad);
                case NumberClass.MPFR: return MPFR.IsPositiveInfinity(a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }
        public static bool IsNegativeInfinity(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return Double.IsNegativeInfinity(a.valueDouble);
                case NumberClass.Quad: return Quad.IsNegativeInfinity(a.valueQuad);
                case NumberClass.MPFR: return MPFR.IsNegativeInfinity(a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }
        public static bool IsNegative(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return Double.IsNegative(a.valueDouble);
                case NumberClass.Quad: return Quad.IsNegative(a.valueQuad);
                case NumberClass.MPFR: return MPFR.IsNegative(a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }
        public static bool IsZero(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return Double.IsZero(a.valueDouble);
                case NumberClass.Quad: return Quad.IsZero(a.valueQuad);
                case NumberClass.MPFR: return MPFR.IsZero(a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }
        public static bool IsNumber(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return Double.IsNumber(a.valueDouble);
                case NumberClass.Quad: return Quad.IsNumber(a.valueQuad);
                case NumberClass.MPFR: return MPFR.IsNumber(a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }
        public static bool IsRegular(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return Double.IsRegular(a.valueDouble);
                case NumberClass.Quad: return Quad.IsRegular(a.valueQuad);
                case NumberClass.MPFR: return MPFR.IsRegular(a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }
        public static SpecialFloatValue IsSpecial(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return Double.IsSpecial(a.valueDouble);
                case NumberClass.Quad: return Quad.IsSpecial(a.valueQuad);
                case NumberClass.MPFR: return MPFR.IsSpecial(a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }
        #endregion

        #region Rounding and precision functions
        public static Number Truncate(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Truncate(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Truncate(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Truncate(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }
        public static Number Fraction(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Fraction(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Fraction(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Fraction(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }
        public static Number Floor(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Floor(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Floor(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Floor(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }
        public static Number Ceiling(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Ceiling(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Ceiling(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Ceiling(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }
        public static Number Round(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Round(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Round(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Round(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }
        public static Number Round(Number a, int decimals)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Round(a.valueDouble, decimals));
                case NumberClass.Quad: return new(Quad.Round(a.valueQuad, decimals));
                case NumberClass.MPFR: return new(MPFR.Round(a.valueMPFR, decimals));
            }
            throw new NumberException("Invalid Number class ");
        }
        #endregion

        # region math functions

        public static Number Max(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return new(Double.Max(a.AsDouble, b.AsDouble));
                case NumberClass.Quad: return new(Quad.Max(a.AsQuad, b.AsQuad));
                case NumberClass.MPFR: return new(MPFR.Max(a.AsMPFR, b.AsMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Min(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return new(Double.Min(a.AsDouble, b.AsDouble));
                case NumberClass.Quad: return new(Quad.Min(a.AsQuad, b.AsQuad));
                case NumberClass.MPFR: return new(MPFR.Min(a.AsMPFR, b.AsMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }


        public static Number Pow(Number num, Number exp)
        {
            switch (num.classType > exp.classType ? num.classType : exp.classType)
            {
                case NumberClass.Double: return new(Double.Pow(num.AsDouble, exp.AsDouble));
                case NumberClass.Quad: return new(Quad.Pow(num.AsQuad, exp.AsQuad));
                case NumberClass.MPFR: return new(MPFR.Pow(num.AsMPFR, exp.AsMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Log(Number num, Number Base)
        {
            switch (num.classType > Base.classType ? num.classType : Base.classType)
            {
                case NumberClass.Double: return new(Double.Log(num.AsDouble, Base.AsDouble));
                case NumberClass.Quad: return new(Quad.Log(num.AsQuad, Base.AsQuad));
                case NumberClass.MPFR: return new(MPFR.Log(num.AsMPFR, Base.AsMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }


        public static Number Abs(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Abs(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Abs(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Abs(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }
        public static int Sign(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return Double.Sign(a.valueDouble);
                case NumberClass.Quad: return Quad.Sign(a.valueQuad);
                case NumberClass.MPFR: return MPFR.Sign(a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }
        public static Number Gamma(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Gamma(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Gamma(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Gamma(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }
        public static Number Factorial(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Factorial(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Factorial(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Factorial(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }
        #endregion

        #region powers and logarithms
        public static Number Log(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Log(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Log(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Log(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Log2(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Log2(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Log2(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Log2(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Log10(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Log10(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Log10(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Log10(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Exp(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Exp(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Exp(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Exp(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Exp2(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Exp2(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Exp2(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Exp2(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Exp10(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Exp10(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Exp10(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Exp10(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Sqrt(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Sqrt(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Sqrt(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Sqrt(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }
        #endregion


        #region Trigonometric functions
        public static Number Sin(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Sin(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Sin(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Sin(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Sinh(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Sinh(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Sinh(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Sinh(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Asin(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Asin(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Asin(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Asin(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Asinh(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Asinh(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Asinh(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Asinh(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Cos(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Cos(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Cos(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Cos(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Cosh(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Cosh(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Cosh(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Cosh(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Acos(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Acos(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Acos(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Acos(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Acosh(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Acosh(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Acosh(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Acosh(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Tan(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Tan(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Tan(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Tan(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Tanh(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Tanh(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Tanh(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Tanh(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Atan(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Atan(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Atan(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Atan(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Atanh(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.Atanh(a.valueDouble));
                case NumberClass.Quad: return new(Quad.Atanh(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.Atanh(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Atan2(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return new(Double.Atan2(a.AsDouble, b.AsDouble));
                case NumberClass.Quad: return new(Quad.Atan2(a.AsQuad, b.AsQuad));
                case NumberClass.MPFR: return new(MPFR.Atan2(a.AsMPFR, b.AsMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }
        #endregion

        #region combinatorics

        public static Number nCr(Number n, Number r)
        {
            switch (n.classType > r.classType ? n.classType : r.classType)
            {
                case NumberClass.Double: return new(Double.nCr(n.AsDouble, r.AsDouble));
                case NumberClass.Quad: return new(Quad.nCr(n.AsQuad, r.AsQuad));
                case NumberClass.MPFR: return new(MPFR.nCr(n.AsMPFR, r.AsMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number nPr(Number n, Number r)
        {
            switch (n.classType > r.classType ? n.classType : r.classType)
            {
                case NumberClass.Double: return new(Double.nPr(n.AsDouble, r.AsDouble));
                case NumberClass.Quad: return new(Quad.nPr(n.AsQuad, r.AsQuad));
                case NumberClass.MPFR: return new(MPFR.nPr(n.AsMPFR, r.AsMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number DoubleFactorial(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return new(Double.DoubleFactorial(a.valueDouble));
                case NumberClass.Quad: return new(Quad.DoubleFactorial(a.valueQuad));
                case NumberClass.MPFR: return new(MPFR.DoubleFactorial(a.valueMPFR));
            }
            throw new NumberException("Invalid Number class ");
        }

        public static Number Harmonic(Number n, double coverage)
        {
            switch (n.classType)
            {
                case NumberClass.Double: return new(Harmonic(n.valueDouble, coverage));
                case NumberClass.Quad: return new(Harmonic(n.valueQuad, coverage));
                case NumberClass.MPFR: return new(Harmonic(n.valueMPFR, coverage));
            }
            throw new NumberException("Invalid Number class ");
        }

        #endregion

        #region calculator functions
        public static bool isInt(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return isInt(a.valueDouble);
                case NumberClass.Quad: return isInt(a.valueQuad);
                case NumberClass.MPFR: return isInt(a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }
        public static bool isIntAx(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return isIntAx(a.valueDouble);
                case NumberClass.Quad: return isIntAx(a.valueQuad);
                case NumberClass.MPFR: return isIntAx(a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }
        public static bool isTrueAx(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return isTrueAx(a.valueDouble);
                case NumberClass.Quad: return isTrueAx(a.valueQuad);
                case NumberClass.MPFR: return isTrueAx(a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }


        public static int MantissaBitSize(Number a)
        {
            switch (a.classType)
            {
                case NumberClass.Double: return Double.MantissaBitSize(a.valueDouble);
                case NumberClass.Quad: return Quad.MantissaBitSize(a.valueQuad); 
                case NumberClass.MPFR: return MPFR.MantissaBitSize(a.valueMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }

        public static int validDigitsInBase(Number num, int Base) => BinaryFloatNumber.validDigitsInBase(num.Precision, Base);

        public static int CmpAx(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return CmpAx(a.AsDouble, b.AsDouble);
                case NumberClass.Quad: return CmpAx(a.AsQuad, b.AsQuad);
                case NumberClass.MPFR: return CmpAx(a.AsMPFR, b.AsMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }
        public static double diffDigit(Number a, Number b)
        {
            switch (a.classType > b.classType ? a.classType : b.classType)
            {
                case NumberClass.Double: return diffDigit(a.AsDouble, b.AsDouble);
                case NumberClass.Quad: return diffDigit(a.AsQuad, b.AsQuad);
                case NumberClass.MPFR: return diffDigit(a.AsMPFR, b.AsMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }
        #endregion


        #region string functions

        public override string ToString() => ToString(new NumberFormat() { ShowNumberType=true, ShowPrecision=true});


        public static string ClassName(NumberClass classType) => classType switch
        {
            NumberClass.Double => "Double",
            NumberClass.MPFR => "MPFR",
            NumberClass.Quad => "Quad",
            _=>"Unknown?"
        };


        /// <summary>
        /// Parse number from string in given Base. Assume exponent is decimal, sign is e/E for Base<=10 or @ otherwise.
        /// Return float number or raise NumberException if invalid
        /// </summary>
        public static Number Parse(string NumStr, NumberFormat F=null)
        {
            // split string number into parts and check if number type is specified in suffix, eg '12q'
            string Suffix;
            if (F == null) F = new NumberFormat();
            string? err = BinaryFloatNumber.ParseParts(out _, out _, out _, out Suffix, out _, out _, out _, out _, out _, NumStr, F);
            if (err == null && Suffix.Length==1 )
                switch (Suffix[0])
                {
                    case 'd': return new(Parse<Double>(NumStr, F));
                    case 'q': return new(Parse<Quad>(NumStr, F));
                    case 'm': return new(Parse<MPFR>(NumStr, F));
                };
            // otherwise parse using default type
            switch (defaultClassType )
            {
                case NumberClass.Double: return new(Parse<Double>(NumStr, F));
                case NumberClass.Quad:   return new(Parse<Quad>(NumStr, F));
                case NumberClass.MPFR:   return new(Parse<MPFR>(NumStr, F));
            };
            throw new NumberException("Invalid Number class ");
        }

        public static bool TryParse(string input, out Number result, NumberFormat F=null)
        {
            try
            {
                result = Parse(input,F);
                return true;
            }
            catch
            {
                result = NaN;
                return false;
            }
        }

        #endregion

        #region lambda functions - they do not have instance counterparts, use defaultType to decide calculation number type and assume it was same default type used for Func<Number>
        public static Number find_extreme(Func<Number, Number> func, Number left, Number right, int precisionDigits, int sign, Func<bool> isTimeout = null)
        {
            switch (defaultClassType)
            {
                case NumberClass.Double:
                    Double fnD(Double a) => func(new(a)).AsDouble;
                    return new(find_extreme(fnD, left.AsDouble, right.AsDouble, precisionDigits, sign, isTimeout));
                case NumberClass.Quad:
                    Quad fnQ(Quad a) => func(new(a)).AsQuad;
                    return new(find_extreme(fnQ, left.AsQuad, right.AsQuad, precisionDigits, sign, isTimeout));
                case NumberClass.MPFR:
                    MPFR fnM(MPFR a) => func(new(a)).AsMPFR;
                    return new(find_extreme(fnM, left.AsMPFR, right.AsMPFR, precisionDigits, sign, isTimeout));
            };
            throw new NumberException("Invalid default number type : " + defaultClassType);
        }
        public static Number find_min(Func<Number, Number> f, Number left, Number right, int precisionDigits, Func<bool> isTimeout = null) => find_extreme(f, left, right, precisionDigits, -1, isTimeout);
        public static Number find_max(Func<Number, Number> f, Number left, Number right, int precisionDigits, Func<bool> isTimeout = null) => find_extreme(f, left, right, precisionDigits, +1, isTimeout);
        public static Number integral(Func<Number, Number> f, Number range_start, Number range_end, int steps = 10000, Func<bool> isTimeout = null)
        {
            switch (defaultClassType)
            {
                case NumberClass.Double:
                    Double fnD(Double a) => f(new(a)).AsDouble;
                    return new(integral(fnD, range_start.AsDouble, range_end.AsDouble, steps, isTimeout));
                case NumberClass.Quad:
                    Quad fnQ(Quad a) => f(new(a)).AsQuad;
                    return new(integral(fnQ, range_start.AsQuad, range_end.AsQuad, steps, isTimeout));
                case NumberClass.MPFR:
                    MPFR fnM(MPFR a) => f(new(a)).AsMPFR;
                    return new(integral(fnM, range_start.AsMPFR, range_end.AsMPFR, steps, isTimeout));
            };
            throw new NumberException("Invalid default number type : " + defaultClassType);
        }
        public static Number find_root(Func<Number, Number> f, Number Y_target, Number tolerance, Number left, Number right, Func<bool> isTimeout = null)
        {
            switch (defaultClassType)
            {
                case NumberClass.Double:
                    Double fnD(Double a) => f(new(a)).AsDouble;
                    return new(find_root(fnD, Y_target.AsDouble, tolerance.AsDouble, left.AsDouble, right.AsDouble, isTimeout));
                case NumberClass.Quad:
                    Quad fnQ(Quad a) => f(new(a)).AsQuad;
                    return new(find_root(fnQ, Y_target.AsQuad, tolerance.AsQuad, left.AsQuad, right.AsQuad, isTimeout));
                case NumberClass.MPFR:
                    MPFR fnM(MPFR a) => f(new(a)).AsMPFR;
                    return new(find_root(fnM, Y_target.AsMPFR, tolerance.AsMPFR, left.AsMPFR, right.AsMPFR, isTimeout));
            };
            throw new NumberException("Invalid default number type : " + defaultClassType);

        }

        #endregion




        #region default string VIRTUAL implementations

        public int CompareTo(Number other)
        {
            if (other is null) return +1;
            switch (classType > other.classType ? classType : other.classType)
            {
                case NumberClass.Double: return AsDouble.CompareTo(other.AsDouble);
                case NumberClass.Quad: return AsQuad.CompareTo(other.AsQuad);
                case NumberClass.MPFR: return AsMPFR.CompareTo(other.AsMPFR);
            }
            throw new NumberException("Invalid Number class ");
        }

        // String functions


        /// <summary>
        /// Format simple string representation of a number in given base : round, decide on exponents etc. Default use static FormatNumberString(), but descendants can override
        /// </summary>
        protected virtual string? FormatNumberBase(string? NumStr, NumberFormat F) => BinaryFloatNumber.FormatNumberString(NumStr, F);



        /// <summary>
        /// Convert THIS number to simplest form in given base, used by ToString(Format). 
        /// </summary>
        /// <param name="Base"></param>
        /// <param name="SignificantDigits">output, report number of accurate significant digits after conversion ( even if more returned ) </param>
        /// <returns>Return string with mantissa.fraction[e or @ exponent]ent, without any base marks like '0x'</returns>
        protected virtual string? ToStringSimple(int Base, out int SignificantDigits)
        {
            string? res = null;
            SignificantDigits = validDigitsInBase(this, Base);
            // see what classes can directly convert ( double can do base10, Quad nothing, MPFR all bases )
            switch (classType)
            {
                case NumberClass.Double:
                    if (Base == 10) return AsDouble.ToString();
                    break;
                case NumberClass.MPFR:
                    return MPFR.ToString(AsMPFR, Base);
            }
            // if binary base, try direct binary convert
            if (IsNumber(this) && BinaryFloatNumber.CountOnes(Base).count == 1)
            {
                var M = Mantissa;
                if (M!=null)
                    res = BinaryFloatNumber.ToStringFromBits(Base, Sign(this), Exponent, SignificantDigits, M);
            }
            // if not base 10 , binary or failed, use base class generic conversion
            if (res == null)
                res = Number.ToStringBase(this, Base, out SignificantDigits);
            return res;
        }

        /// <summary>
        /// convert this number to specified format (base, digits, separators...)
        /// </summary>
        public virtual string? ToString(NumberFormat F)
        {
            // use virtual simple conversion, overwritten by descendant type
            int sigDigits;
            var NumStr = ToStringSimple( F.Base, out sigDigits);
            // determine significant digits
            if (F.SignificantDigits <= 0)
                sigDigits += F.SignificantDigits;
            else
                sigDigits = Math.Min(sigDigits, F.SignificantDigits);
            if (sigDigits < 1) sigDigits = 1;
            var newF = new NumberFormat(F);
            newF.SignificantDigits = sigDigits;
            // add type char if needed, since Format does not know type any more
            if (newF.ShowNumberType == true || (newF.ShowNumberType == null && classType != defaultClassType))
                NumStr += classType switch
                {
                    NumberClass.Double => "d",
                    NumberClass.MPFR => "m",
                    NumberClass.Quad => "q",
                    _ => "?"
                };
            // call string formatter, virtual function that does not depend on T type
            var resF= FormatNumberBase(NumStr, newF);
            // add debug precision info if needed
            if (F.ShowPrecision)
                resF += ":" + Precision;
            return resF;
        }



        #endregion



        #region fully GENERIC helper functions, usable from outside classes

        #region Math generics


        public static readonly double epsApprox = 1e-15; // rounding value, numbers under this are considered false(zero)

        /// <summary>
        /// is this exact integer that fits into long
        /// </summary>
        public static bool isInt<T>(T x) where T : IFloatNumber<T> => x == (long)x;

        /// <summary>
        /// is this value integer, allowing for epsilon
        /// </summary>
        public static bool isIntAx<T>(T x) where T : IFloatNumber<T> =>  (T.Abs(x) % 1) <= epsApprox;

        /// <summary>
        /// is this value true, allowing for epsilon
        /// </summary>
        public static bool isTrueAx<T>(T x) where T : IFloatNumber<T> => T.Abs(x) > epsApprox;


        /// <summary>
        /// compare, but ignore epsilon (very small differences)
        /// </summary>
        public static int CmpAx<T>(T a, T b) where T : IFloatNumber<T>
        {
            var diff = T.Abs(a - b);
            // test if they are very close to each other, within double error margin
            if (diff <= epsApprox) return 0;
            // otherwise return depending on which one is larger
            return a > b ? 1 : -1;
        }

        /// <summary>
        /// Return number 'a' rounded to 'digits' significant digits. Eg: 1.2345:3->1.23 , 0.012345:3->0.0123
        /// </summary>
        public static T RoundSignificantDigits<T>(T a, int digits) where T : IFloatNumber<T>
        {
            if (!T.IsRegular(a)) return a; // return Zero, NaN or Inf
            var shift = digits - (int)T.Floor(T.Log10(T.Abs(a))) - 1; // how many digits to shift to the RIGHT, so all digits we need are to the left of decimal point
            var shift10 = T.Pow(10, shift); // multiplier for shift, 1000...0 or 0.00...001 - use Pow only once
            var res = T.Round(a * shift10) / shift10;
            return res;
        }


        /// <summary>
        /// at what digit two numbers differ. Larger = better precision . Can return fractions, or +Inf if numbers are exactly same
        /// </summary>
        public static double diffDigit<T>(T a, T b) where T: IFloatNumber<T>
        {
            if (a == b) return double.PositiveInfinity;
            T ratio;
            if ((a != 0) && (b != 0))
            {
                T diff = T.Abs(a - b);
                T big = T.Abs(a) > T.Abs(b) ? T.Abs(a) : T.Abs(b);
                ratio = diff / big;
            }
            else
            {
                ratio = T.Abs(a + b);
            }
            return (double)-T.Log10(ratio);
        }


        /// <summary>
        /// n!  Factorial, uses Gamma and round for integers
        /// </summary>
        public static T Factorial<T>(T a) where T : IFloatNumber<T>
        {
            var r = T.Gamma(a+ T.One);
            return T.isInt(a) ? T.Round(r) : r;
        }


        /// <summary>
        /// Gamma function, using Number types with double constants
        /// </summary>
        public static T Gamma<T>(T z) where T : IFloatNumber<T>
        {
            var p = new T[]
            {
                 676.5203681218851
                ,-1259.1392167224028
                ,771.32342877765313
                ,-176.61502916214059
                ,12.507343278686905
                ,-0.13857109526572012
                ,9.9843695780195716e-6
                ,1.5056327351493116e-7
            };
            if (z < 0.5)
            {
                var y = T.PI / (T.Sin(T.PI * z) * T.Gamma(1 - z)); // reflection, since z<0.5 not allowed 
                return y;
            }
            else
            {
                z--;
                T x = 0.99999999999980993;
                for (int i = 0; i < p.Length; i++)
                    x += p[i] / (z + i + 1);
                var t = z + p.Length - 0.5;
                var y = T.Sqrt(T.PIx2) * T.Pow(t, z + 0.5) * T.Exp(-t) * x;
                return y;
            }
        }

        #endregion

        #region Combinatoric generics


        /// <summary>
        /// n!! , integer double factorial ( 5!!= 1*3*5 , 6!!=2*4*6 )
        /// </summary>
        public static T DoubleFactorial<T>(T x) where T : IFloatNumber<T>
        {
            // detect if this is exact integer
            if (T.isInt(x))
            {
                int xi = (int)T.Round(x);
                T res = 1;
                if (xi >= 0)
                    while (xi > 0)
                    {
                        res *= xi;
                        xi -= 2;
                    }
                else
                if (xi % 2 != 0)
                {
                    while (xi < -1)
                    {
                        xi += 2;
                        res *= xi;
                    }
                    res = 1 / res;
                }
                else
                    throw new NumberException("DoubleFactorial negative arguments must be odd !");
                return res;
            }
            else
            {
                // not defined for float
                throw new NumberException("DoubleFactorial require integer arguments !");
            }
        }



        /// <summary>
        /// harmonic( ( n [,coverage=100%] )  - return harmonic number Hn= sum(1/i) for i=1..n = 1/N+1/(N-1)+...1/1
        ///    N*harmonic(N) = expected tries to complete all set (all different coupons or get all six dice numbers (N=6) ...)
        ///                    assume probability to get each of N different set items is equal, 1/N    
        /// when coverage != 100% , return partial harmonic number Hnp= sum(1/i) for i= (1-coverage)*n+1..n = Hn-H(1+(1-coverage)*n)                    
        ///    N*harmonic(N, 30%) = expected tries to complete 30% of a set
        /// </summary>
        /// <param name="n"></param>
        /// <param name="coverage">when coverage != 100% , return partial harmonic number Hnp= sum(1/i) for i= (1-coverage)*n+1..n = Hn-H(1+(1-coverage)*n)</param>
        /// <returns></returns>
        public static T Harmonic<T>(T n, double coverage=1) where T : IFloatNumber<T>
        {
            T res = 0;
            T m = T.Round(1 + (1 - coverage) * n);
            if (m < 1) m = 1;
            if (n < 100000)
            {
                // actual sum of all elements for low N
                for (int i = (int)m; i <= n; i++)
                    res += T.One / (T)i;
            }
            else
            {
                // approximation, Hn ~ ln(n)+y+1/2n+..., where y= 0.5772156649 = Euler's constant
                // and H(n,m)= H(n)-H(m-1)
                T y = T.Euler;
                res = T.Log(n) + 0.5 / n + y;
                m--;
                if (m > 0)
                    res -= T.Log(m) + 0.5 / m + y;
            }
            return res;
        }


        /// <summary>
        /// How many COMBINATIONS or ways to choose 'r' out of 'n', when order does NOT matter
        /// </summary>
        public static T nCr<T>(T n, T r) where T : IFloatNumber<T>
        {
            if (r < -1 || n < 0) return 0;
            // standard formula, nCr = n!/r!/(n-r)!   , support fractions
            T res = T.Factorial(n) / T.Factorial(r) / T.Factorial(n - r);
            // if above overflowed, try to optimize
            if (T.IsNaN(res) || T.IsInfinity(res))
            {
                if (n - r > r) r = n - r; // so that A is smaller
                var k = n - r + 1;
                T i = 1;
                res = 1;
                while (k <= n && !T.IsNaN(res) && !T.IsInfinity(res))
                {
                    res *= k / i;
                    k++;
                    i++;
                }
            }
            return res;
        }

        /// <summary>
        /// How many PERMUTATIONS or ways to choose 'r' out of 'n', when order DOES matter, 
        /// </summary>
        public static T nPr<T>(T n, T r) where T : IFloatNumber<T>
        {
            if (r > n) return 0;
            // standard formula, nPr= n!/(n-r)!   , support fractions
            T res = T.Factorial(n) / T.Factorial(n - r);
            // if above overflowed, try to optimize with integer calc
            if (T.IsNaN(res) || T.IsInfinity(res))
            {
                res = 1;
                for (int i = (int)T.Round(r + 1); i <= n; i++)
                    res *= i;
            }
            return res;
        }

        #endregion

        #region LAMBDA generics ( root, max/min, integral )

        // Brent's algorithm to find root of function within brackets (modified for stepped/INT functions)
        public static T Brent<T> 
                        (
                            Func<T,T> f,
                            T left,
                            T right,
                            T tolerance,
                            out int iterationsUsed,
                            out T errorEstimate,
                            Func<bool> isTimeout = null
                        ) where T : IFloatNumber<T>
        {
            int maxIterations = 50;
            if (tolerance <= 0.0)
            {
                string msg = string.Format("Tolerance must be positive. Recieved {0}.", tolerance);
                throw new ArgumentOutOfRangeException(msg);
            }

            errorEstimate = T.MaxValue;

            // Implementation and notation based on Chapter 4 in
            // "Algorithms for Minimization without Derivatives"
            // by Richard Brent.

            T c, d, e, fa, fb, fc, tol, m, p, q, r, s;

            // set up aliases to match Brent's notation
            T a = left; T b = right; T t = tolerance;
            iterationsUsed = 0;

            fa = f(a);
            fb = f(b);

            if (fa * fb > 0.0)
            {
                throw new NumberException("FindRoot error. Function must be above zero on one end and below zero on other end. f(" + left + ") = " + fa + " , f(" + right + ") = " + fb);
            }

        label_int:
            c = a; fc = fa; d = e = b - a;
        label_ext:
            if (T.Abs(fc) < T.Abs(fb))
            {
                a = b; b = c; c = a;
                fa = fb; fb = fc; fc = fa;
            }

            iterationsUsed++;
            if (isTimeout != null && isTimeout()) throw new Exception("ERR:Timeout");


            tol = 2.0 * t * T.Abs(b) + t;
            errorEstimate = m = 0.5 * (c - b);
            if (T.Abs(m) > tol && fb != 0.0) // exact comparison with 0 is OK here
            {
                // See if bisection is forced
                if (T.Abs(e) < tol || T.Abs(fa) <= T.Abs(fb))
                {
                    d = e = m;
                }
                else
                {
                    s = fb / fa;
                    if (a == c)
                    {
                        // linear interpolation
                        p = 2.0 * m * s; q = 1.0 - s;
                    }
                    else
                    {
                        // Inverse quadratic interpolation
                        q = fa / fc; r = fb / fc;
                        p = s * (2.0 * m * q * (q - r) - (b - a) * (r - 1.0));
                        q = (q - 1.0) * (r - 1.0) * (s - 1.0);
                    }
                    if (p > 0.0)
                        q = -q;
                    else
                        p = -p;
                    s = e; e = d;
                    if (2.0 * p < 3.0 * m * q - T.Abs(tol * q) && p < T.Abs(0.5 * s * q))
                        d = p / q;
                    else
                        d = e = m;
                }
                a = b; fa = fb;
                if (T.Abs(d) > tol)
                    b += d;
                else if (m > 0.0)
                    b += tol;
                else
                    b -= tol;
                if (iterationsUsed >= maxIterations)
                    return b;

                fb = f(b);
                if ((fb > 0.0 && fc > 0.0) || (fb <= 0.0 && fc <= 0.0))
                    goto label_int;
                else
                    goto label_ext;
            }
            else
            {
                // round result depending to error estimate
                T errAbs = T.Abs(errorEstimate);
                if (errAbs > 0 && errAbs < 1)
                {
                    int digits = (int)T.Round(-T.Log10(errAbs) - 1);
                    if (digits > 3 && digits < 15)
                        b = T.Round(b, digits);
                    // check if result is close-but not exactly-integer, in which case there is risk of input function being integer/stepped, and some neighbour integer being better fit
                    if (!T.isInt(b))
                    {
                        var bi = T.Round(b);
                        T aroundDif(T x) => (T.Abs(f(x - 0.3)) + T.Abs(f(x + 0.3))) / 2; // average distance to zero around integer
                        void chkOfs(int db)
                        {
                            var fd = f(bi + db);
                            if (!T.IsNaN(fd) && !T.IsInfinity(fd)) // if Y of int x is valid
                                if (T.Abs(fd) < T.Abs(fb) ||  // if integer is strictly better, use it
                                     (T.Abs(fd) == T.Abs(fb) && aroundDif(bi + db) <= aroundDif(b)))  // if same, use only if more centrally situated
                                {
                                    b = bi + db;
                                    fb = fd;
                                }
                        }
                        chkOfs(+1);
                        chkOfs(-1);
                        chkOfs(0);
                    }
                }
                return b;
            }
        }


        // my custom algorithm to guess brackets (x1,x2) that enclose x for which f(x)==0
        public static (T left, T right) guessBrackets<T>(Func<T, T> f, T left, T right, Func<bool> isTimeout = null) where T : IFloatNumber<T>
        {
            // -- inner functions
            // iteration counter, to prevent infinite searches
            int iterCount = 100;
            // is T value defined?  not NaN or Infinity ( or eg Complex etc if needed )
            bool isDefined(T x) => (!T.IsNaN(x)) && (!T.IsInfinity(x));
            // did we find correct brackets, enclosing zero ( or we did not find but max iter count reached ) - in either case, return with current left/right
            bool isDone() => (isDefined(left) && isDefined(right) && (T.Sign(left) != T.Sign(right))) || (iterCount <= 0);
            // if one bracket is defined, find other. Return true if both correctly found
            void findOther()
            {
                // function to find one bracket, if other is defined
                T findOtherBracket(ref T bracket, T delta)
                {
                    var oldY = f(bracket);
                    while (iterCount > 0)
                    {
                        iterCount--;
                        if (isTimeout != null && isTimeout()) throw new Exception("ERR:Timeout");
                        T res = bracket + delta;
                        var y = f(res);
                        if (!isDefined(y))
                            delta /= 2;
                        else if ((T.Sign(y) == T.Sign(oldY)) && (T.Sign(y) != 0))
                        {
                            bracket = res;
                            oldY = y;
                            delta *= 2;
                        }
                        else
                            return res;
                    }
                    return T.NaN;
                }
                // if left is defined, find right
                if (isDefined(left) && !isDefined(right))
                    right = findOtherBracket(ref left, +1);
                // if right is defined, find left
                if (isDefined(right) && !isDefined(left))
                    left = findOtherBracket(ref right, -1);
            }
            // Compare if two values are same, support NaN,+/-Inf
            bool isSameVal(T y1, T y2)
            {
                if (T.IsNaN(y1) && T.IsNaN(y2)) return true; // two NaNs are 'same value'
                return y1 == y2; // this will be true for +Inf == +Inf, false for +Inf==-Inf or +Inf=123
            }
            // find two points in given direction with different Y values, return NULL if not found. Support NaN,+/-Inf regions
            RectangleReal<T> findDifValues(T x0, T dx)
            {
                int maxIter = 100; // separate iteration limit from main function
                var y0 = f(x0);
                var x = x0;
                do
                {
                    maxIter--;
                    if (isTimeout != null && isTimeout()) throw new Exception("ERR:Timeout");
                    x += dx;
                    var y = f(x);
                    if (isSameVal(y, y0))
                    {
                        x0 = x;
                        dx *= 2;
                    }
                    else
                    {
                        // return smaller X first/left, in x1/y1 member
                        if (dx > 0)
                            return new RectangleReal<T>(x0, y0, x, y);
                        else
                            return new RectangleReal<T>(x, y, x0, y0);
                    }
                } while (maxIter > 0);
                // not found, return null
                return null;
            }
            // Bisect segment to find two different but comparable Y values. It starts with two different values that are not neccessary comparable.
            // eg start with (NaN,123) and find (15,123) or (-Inf, -19)
            // Return:  Change Left OR Right if found, and return true
            bool bisectSegment(RectangleReal<T> seg)
            {
                if (seg == null)
                    return false;
                int maxIter = 100; // also separate limit
                // search/bisect as long as one of Y values is NaN ( uncomparable)
                do
                {
                    maxIter--;
                    if (isTimeout!=null && isTimeout()) throw new Exception("ERR:Timeout");
                    // if we found two comparable and different Y values, and at least one is not infinite, decide which direction
                    if (!T.IsNaN(seg.y1) && !T.IsNaN(seg.y2) && (!T.IsInfinity(seg.y1) || !T.IsInfinity(seg.y2)))
                    {
                        // if different signs (or both signs are zero), update both left and right - even if one is infinite
                        if ((T.Sign(seg.y1) != T.Sign(seg.y2)) || (T.Sign(seg.y1) == 0))
                        {
                            left = seg.x1;
                            right = seg.x2;
                            return true; // notify that brackets were modified
                        }
                        // otherwise determine direction 
                        // dy= positive if rising function, ie y2>y1
                        T dy = 0;
                        if (T.IsInfinity(seg.y2))
                            dy = T.Sign(seg.y2);
                        else if (T.IsInfinity(seg.y1))
                            dy = -T.Sign(seg.y1);
                        else
                            dy = T.Sign(seg.y2 - seg.y1);
                        // both Ys are either above or below zero - in combo with 'dy' it decide which bracket to update
                        if (T.Sign(seg.y1) * dy > 0)
                            // they are both above zero and y1 is smaller (rising func), or both below zero and y1 is larger (falling func)
                            right = seg.x1; // in both cases, zero is to the left of both points, so we choose x1 as right bracket
                        else
                            // they are both above zero and y1 is larger (falling func), or both below zero and y1 is smaller (raising func)
                            left = seg.x2; // in both cases, zero is to the right of both points, so we choose x2 as left bracket
                        // notify that we modified one bracket
                        return true;
                    }
                    else
                    {
                        // next bisect
                        var x = (seg.x1 + seg.x2) / 2;
                        var y = f(x);
                        if (isSameVal(y, seg.y1))
                        {
                            // y is same as left value (y1), so move y1 to y
                            seg.x1 = x;
                            seg.y1 = y;
                        }
                        else if (isSameVal(y, seg.y2))
                        {
                            // y is same as right value (y2), so move y2 to y
                            seg.x2 = x;
                            seg.y2 = y;
                        }
                        else
                        {
                            // different to both, so keep it, and keep as other value whichever was not NaN
                            if (!T.IsNaN(seg.y1))
                            {
                                seg.x2 = x;
                                seg.y2 = y;
                            }
                            else
                            {
                                seg.x1 = x;
                                seg.y1 = y;
                            }
                        }
                    }
                } while (maxIter > 0);
                return false;
            }
            // if no bracket is defined, find at least one ( either left or right or both) with actual value that is not NaN or Inf
            // return: updated Left and/or Right
            void findOneBracket()
            {
                T dx = 1.57; // best not to be integer
                var dif = findDifValues(dx / 2, dx); // find two points with any different values to the righ ( one can be NaN )
                if (dif == null)
                    dif = findDifValues(-dx / 2, -dx); // if not found toward right, try toward left
                if (dif == null)
                    return; // we could not find different points in any direction, so do not change left/right and just return !
                // if one of them is NaN, need to test additional segment
                RectangleReal<T> dif2 = null;
                if (T.IsNaN(dif.y1))
                    dif2 = findDifValues(dif.x2, dx); // Nan, y2 =>  x3>x2,?? so  NaN,y2 | y3,??
                else if (T.IsNaN(dif.y2))
                    dif2 = findDifValues(dif.x1, -dx); // y1, NaN =>  ??,x0<x1 so  ??,y0 | y1,NaN
                // now bisect those segments to find both non-NaN, and thus comparable, and thus able to update either Left or Right
                if (!bisectSegment(dif))
                    bisectSegment(dif2);
            }


            // --- finding brackets
            // first check if one or both brackets are already defined, and try to find other one
            findOther();
            // if we have both correct brackets ( or used all available iterations) return
            if (isDone())
                return (left, right);
            // otherwise, we do not have any bracket, so try to find initial bracket, either left or right (or both)
            findOneBracket();
            // and try again to find other bracket
            findOther();
            // return result, regardless if left/right are correctly found or still infinite
            return (left, right);
        }



        /// <summary>
        /// returns X that satisfy f(X)=Y_target 
        /// find root of specified function 'f(x)' if Y_target is not specified (==0)
        /// </summary>
        public static T find_root<T>(Func<T, T> func, T Y_target, T tolerance, T left, T right, Func<bool> isTimeout = null) where T : IFloatNumber<T>
        {
            // extra info that callers may not always want
            int iterationsUsed;
            T errorEstimate;
            // make function zero-based, and return NaN for exceptions
            Func<T, T> f = delegate (T x) {
                try
                {
                    return func(x) - Y_target;
                }
                catch
                {
                    return T.NaN;
                }
            };

            // guess brackets if not specified
            (left, right) = guessBrackets(f, left, right, isTimeout);
            // search for root
            return Brent(f, left, right, tolerance, out iterationsUsed, out errorEstimate, isTimeout);

        }



        /// <summary>
        /// integrate function T fn(T), from x=range_start to x=range_end, optional number of steps 
        /// </summary>
        public static T integral<T>(Func<T, T> fn, T range_start, T range_end, int steps = 10000, Func<bool> isTimeout = null) where T : IFloatNumber<T>
        {
            if (range_start > range_end)
            {
                T tmp = range_start;
                range_start = range_end;
                range_end = tmp;
            }
            if (range_start == range_end) return 0;
            if (steps < 1) steps = 1;
            // now perform integration
            T x = range_start, dx = (range_end - range_start) / steps;
            T res = 0;
            while (x + dx <= range_end)
            {

                res += dx * (fn(x) + fn(x + dx)) / 2; // trapezoidal rule
                x += dx;
                if (isTimeout != null && isTimeout()) throw new NumberException("ERR:Timeout");
            }
            // last part, if range was not divisible by dx
            dx = range_end - x;
            if (dx > 0)
                res += dx * (fn(x) + fn(x + dx)) / 2;
            // result of integration
            return res;
        }


        /// <summary>
        /// find MINIMUM (sign lt 0) or MAXIMUM (sign gt 0) of a function, and returns X where that minimum is located
        /// use Golden section search. If sign=-1 find minimum, if sign=+1 find maximum 
        /// </summary>
        public static T find_extreme<T>(Func<T, T> f, T left, T right, int precisionDigits, int sign, Func<bool> isTimeout = null) where T : IFloatNumber<T>
        {
            T invphi = (T.Sqrt(5) - 1) / 2; // 1/phi                                                                                                                     
            T invphi2 = (3 - T.Sqrt(5)) / 2; // 1/phi^2   
            T a, b;
            if (left <= right) { a = left; b = right; } else { a = right; b = left; }
            T h = b - a;
            T tolerance = T.Pow(10, -precisionDigits);
            if (h <= tolerance) return (right + left) / 2;
            // required steps to achieve tolerance                                                                                                                   
            int n = (int)(T.Ceiling(T.Log(tolerance / h) / T.Log(invphi)));
            T c = a + invphi2 * h;
            T d = a + invphi * h;
            sign = -sign;
            T yc = f(c) * sign;
            T yd = f(d) * sign;
            // do all steps
            for (int k = 0; k < n; k++)
            {
                if (yc < yd)
                {
                    b = d;
                    d = c;
                    yd = yc;
                    h = invphi * h;
                    c = a + invphi2 * h;
                    yc = f(c) * sign;
                }
                else
                {
                    a = c;
                    c = d;
                    yc = yd;
                    h = invphi * h;
                    d = a + invphi * h;
                    yd = f(d) * sign;
                }
            }
            T res;
            if (yc < yd)
                res = (a + d) / 2;
            else
                res = (c + b) / 2;
            return T.Round(res, precisionDigits);
        }

        #endregion

        #region String Parse generics and functions

        /// <summary>
        /// Parse number from string in given Base. Assume exponent is decimal, sign is e/E for Base<=10 or @ otherwise.
        /// Detect base from string ( 0x = hex, 0b=bin, 0o=oct, 0[12]=base12 ) or use specified Base if not detected, with default 10. Return float number or raise NumberException if invalid
        /// Used fields from StringFormat F:
        ///     - Base: can be -1 for detect, or suggested actual base
        ///     - separators: additional separators to ignore in input string
        /// </summary>
        public static T Parse<T>(string NumStr, NumberFormat F) where T : IFloatNumber<T>
        {
            string Mantissa, Fraction, Suffix, CleanNumber;
            int Sign, iBase, RemainderLength;
            long Exponent;
            double specValue;
            // split string number into parts
            string? err = BinaryFloatNumber.ParseParts(out CleanNumber, out Mantissa, out Fraction, out Suffix, out Sign, out iBase, out Exponent, out RemainderLength, out specValue, NumStr, F);
            // if special value, convert double.NaN or Inf to actual type
            if (specValue != -1)
                return specValue;
            // if error in splitting parts, throw it
            if (err != null)
                throw new NumberException(err);
            // combine mantissa and fraction, and change exponent
            long exp = Exponent - Fraction.Length;
            string num = Mantissa + Fraction;
            // extract integer digits, already checked for validity
            T res = 0, tBase=iBase;
            for (int i = 0; i < num.Length; i++)
                res = res * tBase + BinaryFloatNumber.baseDigit(num[i], iBase);
            // combine mantisa and exponent using single Pow
            if (exp != 0)
                res *= T.Pow(tBase, exp);
            res *= Sign;
            return res;
        }


        /// <summary>
        /// Try to parse float number from string in given Base. Assume exponent is decimal, sign is e/E for Base<=10 or @ otherwise.
        /// Return false & NaN if number is not valid, OR false & 0 if unable to parse in given base !
        /// </summary>
        public static bool TryParse<T>(string NumStr, out T result, NumberFormat F) where T : IFloatNumber<T>
        {
            try
            {
                result = Parse<T>(NumStr, F);
                return true;
            }
            catch
            {
                result = T.NaN;
                return false;
            }
        }




        #endregion

        #region ToString generics and functions

        /// <summary>
        /// converts number to string in given base. It may be in simple exponential form, eg 1234 -> 1.234e3 or 0.1234e4
        /// If base >16, NaN must be in 'NaN!' form, and infinities with +/- '∞' 
        /// Return string with mantissa.fraction[e or @ exponent]ent, without any base marks like '0x'
        /// </summary>
        public static string? ToStringBase<T>(T num, int Base, out int SignificantDigits) where T : IFloatNumber<T>
        {
            // check for special cases
            SignificantDigits = validDigitsInBase(T.MantissaBitSize(num),Base);
            if (T.IsZero(num)) return T.Sign(num)<0? "-0":"0";
            if (T.IsPositiveInfinity(num)) return "∞";
            if (T.IsNegativeInfinity(num)) return "-∞";
            if (T.IsNaN(num)) return Base > 16 ? "NaN!" : "NaN";
            // if base is too large, return error null
            if (Base > 62) return null;
            // get sign
            int sign = T.IsNegative(num) ? -1 : +1;
            if (sign < 0)
                num = -num;
            // normalize so that number is in form 0.xxxx*10^exp
            var e10 = T.Log(num, Base); // fractional log10
            long exp = (long)T.Floor(e10) + 1;  // will support exponents up to 'long', which covers Quad
            var e10i = T.Pow(Base, exp); // 10^integer, should be exactly 1E(??+1)
            T x = num / e10i;  // shift number so it is 0.12345 and positive
            // verify that number is really in 0.1234 format - in case of faulty Pow<->Log for very large exponents !!
            var x10 = T.Log(x, Base);
            if (x10 >= 0 || x10 < -1)
            {
                long eCor = (long)T.Floor(x10) + 1;
                x /= T.Pow(Base, eCor);
                exp += eCor;
                // second verification
                x10 = T.Log(x, Base);
                if (x10 >= 0 || x10 < -1)
                    throw new NumberException("ToStringSimple encountered invalid Pow-Log math !");
            }
            int maxDigits = SignificantDigits + 1; // max number of digits to show +1 ( so they can be rounded later), based on number itself
            SignificantDigits = validDigitsInBase(T.MantissaBitSize(x), Base); // correct reported digits, based on precision of Pow/Log, can be smaller ( eg 52 instead of 63 ) 
            string res = "0.";
            // process fractions
            do
            {
                x *= Base;
                ulong digit = (ulong)x;
                x -= digit;
                res += BinaryFloatNumber.baseDigit((int)digit, Base);
            } while ((x > 0) && (res.Length < maxDigits + 2));
            if (exp != 0)
                res += (Base != 10 ? BinaryFloatNumber.expChar : "e") + exp;
            return (sign < 0 ? "-" : "") + res;
        }





        #endregion

        #endregion fully Generic


    }
}
