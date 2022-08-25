﻿
using System;

namespace Mpfr.Native
{

    /// <summary>
    /// Represents the maximum width integer type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In .Net, this is a 64-bit integer. 
    /// </para>
    /// </remarks>
    public struct intmax_t
    {

        /// <summary>
        /// The value of the <see cref="intmax_t">intmax_t</see>.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public long Value;

        /// <summary>
        /// Creates a new <see cref="intmax_t">intmax_t</see>, and sets its <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value of the new <see cref="intmax_t">intmax_t</see>.</param>
        public intmax_t(long value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Converts a <see cref="Byte">Byte</see> value to an <see cref="intmax_t">intmax_t</see> value.
        /// </summary>
        /// <param name="value">A <see cref="Byte">Byte</see> value.</param>
        /// <returns>An <see cref="intmax_t">intmax_t</see> value.</returns>
        public static implicit operator intmax_t(byte value)
        {
            return new intmax_t(value);
        }

        /// <summary>
        /// Converts a <see cref="Byte">Byte</see> value to an <see cref="intmax_t">intmax_t</see> value.
        /// </summary>
        /// <param name="value">A <see cref="Byte">Byte</see> value.</param>
        /// <returns>An <see cref="intmax_t">intmax_t</see> value.</returns>
        public static implicit operator intmax_t(sbyte value)
        {
            return new intmax_t(value);
        }

        /// <summary>
        /// Converts a <see cref="UInt16">UInt16</see> value to an <see cref="intmax_t">intmax_t</see> value.
        /// </summary>
        /// <param name="value">A <see cref="UInt16">UInt16</see> value.</param>
        /// <returns>An <see cref="intmax_t">intmax_t</see> value.</returns>
        public static implicit operator intmax_t(ushort value)
        {
            return new intmax_t(value);
        }

        /// <summary>
        /// Converts an <see cref="Int16">Int16</see> value to an <see cref="intmax_t">intmax_t</see> value.
        /// </summary>
        /// <param name="value">An <see cref="Int16">Int16</see> value.</param>
        /// <returns>An <see cref="intmax_t">intmax_t</see> value.</returns>
        public static implicit operator intmax_t(short value)
        {
            return new intmax_t(value);
        }

        /// <summary>
        /// Converts a <see cref="UInt32">UInt32</see> value to an <see cref="intmax_t">intmax_t</see> value.
        /// </summary>
        /// <param name="value">A <see cref="UInt32">UInt32</see> value.</param>
        /// <returns>An <see cref="intmax_t">intmax_t</see> value.</returns>
        public static implicit operator intmax_t(uint value)
        {
            return new intmax_t(value);
        }

        /// <summary>
        /// Converts an <see cref="Int32">Int32</see> value to an <see cref="intmax_t">intmax_t</see> value.
        /// </summary>
        /// <param name="value">An <see cref="Int32">Int32</see> value.</param>
        /// <returns>An <see cref="intmax_t">intmax_t</see> value.</returns>
        public static implicit operator intmax_t(int value)
        {
            return new intmax_t(value);
        }

        /// <summary>
        /// Converts a <see cref="UInt64">UInt64</see> value to an <see cref="intmax_t">intmax_t</see> value.
        /// </summary>
        /// <param name="value">A <see cref="UInt64">UInt64</see> value.</param>
        /// <returns>An <see cref="intmax_t">intmax_t</see> value.</returns>
        public static explicit operator intmax_t(ulong value)
        {
            if (value > int.MaxValue) throw new System.OverflowException(String.Format(System.Globalization.CultureInfo.InvariantCulture, "'{0}' is out of range of the intmax_t data type.", value));
            return new intmax_t((long)value);
        }

        /// <summary>
        /// Converts an <see cref="Int64">Int64</see> value to a <see cref="intmax_t">intmax_t</see> value.
        /// </summary>
        /// <param name="value">An <see cref="Int64">Int64</see> value.</param>
        /// <returns>An <see cref="intmax_t">intmax_t</see> value.</returns>
        public static implicit operator intmax_t(long value)
        {
            return new intmax_t(value);
        }

        /// <summary>
        /// Converts an <see cref="intmax_t">intmax_t</see> value to a <see cref="Byte">Byte</see> value.
        /// </summary>
        /// <param name="value">An <see cref="intmax_t">intmax_t</see> value.</param>
        /// <returns>A <see cref="Byte">Byte</see> value.</returns>
        public static explicit operator byte(intmax_t value)
        {
            if (value.Value < 0 || value.Value > byte.MaxValue) throw new System.OverflowException(String.Format(System.Globalization.CultureInfo.InvariantCulture, "'{0}' is out of range of the Byte data type.", value));
            return (byte)value.Value;
        }

        /// <summary>
        /// Converts an <see cref="intmax_t">intmax_t</see> value to an <see cref="SByte">SByte</see> value.
        /// </summary>
        /// <param name="value">An <see cref="intmax_t">intmax_t</see> value.</param>
        /// <returns>An <see cref="SByte">SByte</see> value.</returns>
        public static explicit operator sbyte(intmax_t value)
        {
            if (value.Value < sbyte.MinValue || value.Value > sbyte.MaxValue) throw new System.OverflowException(String.Format(System.Globalization.CultureInfo.InvariantCulture, "'{0}' is out of range of the SByte data type.", value));
            return (sbyte)value.Value;
        }

        /// <summary>
        /// Converts an <see cref="intmax_t">intmax_t</see> value to a <see cref="UInt16">UInt16</see> value.
        /// </summary>
        /// <param name="value">An <see cref="intmax_t">intmax_t</see> value.</param>
        /// <returns>A <see cref="UInt16">UInt16</see> value.</returns>
        public static explicit operator ushort(intmax_t value)
        {
            if (value.Value < 0 || value.Value > ushort.MaxValue) throw new System.OverflowException(String.Format(System.Globalization.CultureInfo.InvariantCulture, "'{0}' is out of range of the UInt16 data type.", value));
            return (ushort)value.Value;
        }

        /// <summary>
        /// Converts an <see cref="intmax_t">intmax_t</see> value to an <see cref="Int16">Int16</see> value.
        /// </summary>
        /// <param name="value">An <see cref="intmax_t">intmax_t</see> value.</param>
        /// <returns>An <see cref="Int16">Int16</see> value.</returns>
        public static explicit operator short(intmax_t value)
        {
            if (value.Value < short.MinValue || value.Value > short.MaxValue) throw new System.OverflowException(String.Format(System.Globalization.CultureInfo.InvariantCulture, "'{0}' is out of range of the Int16 data type.", value));
            return (short)value.Value;
        }

        /// <summary>
        /// Converts an <see cref="intmax_t">intmax_t</see> value to a <see cref="UInt32">UInt32</see> value.
        /// </summary>
        /// <param name="value">An <see cref="intmax_t">intmax_t</see> value.</param>
        /// <returns>A <see cref="UInt32">UInt32</see> value.</returns>
        public static explicit operator uint(intmax_t value)
        {
            if (value.Value < 0 || value.Value > uint.MaxValue) throw new System.OverflowException(String.Format(System.Globalization.CultureInfo.InvariantCulture, "'{0}' is out of range of the UInt32 data type.", value));
            return (uint)value.Value;
        }

        /// <summary>
        /// Converts an <see cref="intmax_t">intmax_t</see> value to an <see cref="Int32">Int32</see> value.
        /// </summary>
        /// <param name="value">An <see cref="intmax_t">intmax_t</see> value.</param>
        /// <returns>An <see cref="Int32">Int32</see> value.</returns>
        public static explicit operator int(intmax_t value)
        {
            if (value.Value < int.MinValue || value.Value > int.MaxValue) throw new System.OverflowException(String.Format(System.Globalization.CultureInfo.InvariantCulture, "'{0}' is out of range of the Int32 data type.", value));
            return (short)value.Value;
        }

        /// <summary>
        /// Converts an <see cref="intmax_t">intmax_t</see> value to a <see cref="UInt64">UInt64</see> value.
        /// </summary>
        /// <param name="value">An <see cref="intmax_t">intmax_t</see> value.</param>
        /// <returns>A <see cref="UInt64">UInt64</see> value.</returns>
        public static explicit operator ulong(intmax_t value)
        {
            if (value.Value < 0) throw new System.OverflowException(String.Format(System.Globalization.CultureInfo.InvariantCulture, "'{0}' is out of range of the UInt64 data type.", value));
            return (ulong)value.Value;
        }

        /// <summary>
        /// Converts an <see cref="intmax_t">intmax_t</see> value to an <see cref="Int64">Int64</see> value.
        /// </summary>
        /// <param name="value">An <see cref="intmax_t">intmax_t</see> value.</param>
        /// <returns>An <see cref="Int64">Int64</see> value.</returns>
        public static implicit operator long(intmax_t value)
        {
            return value.Value;
        }

        /// <summary>
        /// Gets the string representation of the <see cref="intmax_t">intmax_t</see>.
        /// </summary>
        /// <returns>The string representation of the <see cref="intmax_t">intmax_t</see>.</returns>
        public override string ToString()
        {
            return Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns><c>True</c> if <paramref name="obj"/> is an instance of <see cref="intmax_t">intmax_t</see> and equals the value of this instance; otherwise, <c>False</c>.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is intmax_t))
                return false;

            return Equals((intmax_t)obj);
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified <see cref="intmax_t">intmax_t</see> value.
        /// </summary>
        /// <param name="other">A <see cref="intmax_t">intmax_t</see> value to compare to this instance.</param>
        /// <returns><c>True</c> if <paramref name="other"/> has the same value as this instance; otherwise, <c>False</c>.</returns>
        public bool Equals(intmax_t other)
        {
            return Value == other.Value;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// Gets a value that indicates whether the two argument values are equal.
        /// </summary>
        /// <param name="value1">A <see cref="intmax_t">intmax_t</see> value.</param>
        /// <param name="value2">A <see cref="intmax_t">intmax_t</see> value.</param>
        /// <returns><c>True</c> if the two values are equal, and <c>False</c> otherwise.</returns>
        public static bool operator ==(intmax_t value1, intmax_t value2)
        {
            return value1.Equals(value2);
        }

        /// <summary>
        /// Gets a value that indicates whether the two argument values are different.
        /// </summary>
        /// <param name="value1">A <see cref="intmax_t">intmax_t</see> value.</param>
        /// <param name="value2">A <see cref="intmax_t">intmax_t</see> value.</param>
        /// <returns><c>True</c> if the two values are different, and <c>False</c> otherwise.</returns>
        public static bool operator !=(intmax_t value1, intmax_t value2)
        {
            return !value1.Equals(value2);
        }

    }

}