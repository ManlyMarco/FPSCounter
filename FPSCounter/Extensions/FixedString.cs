using System;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace FPSCounter
{
    public class FixedString
    {
        // Constants
        const string MONO20_SB = "_str";
        const string NET35_SB = "m_StringValue";
        const string NET4X_SB = "m_ChunkChars";
        const uint MAX_DECIMALS = 2;
        const uint DEFAULT_BASE = 10;
        const char DEFAULT_PAD_CHAR = ' ';
        static readonly char[] DIGITS_LUT = new char[]{'0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F'};


        // Setup fields
        public readonly StringBuilder builder;
        private string rawStrValue;
        private readonly int definedSize;
        private readonly char[] charData;
        private readonly bool useChunk;

        // Worker fields
        // work Cache set to 21 to accomodate ulong.MaxValue; Float is always clamped
        private readonly char[] workCache = new char[21];


        // Publics
        public int Length { get { return builder.Length; } }
        public int DefinedSize { get { return definedSize; } }
        public bool IsChunkImplementation { get { return useChunk; } }

        // Own logic

        public FixedString(int strSize)
        {
            if (strSize <= 0)
                throw new System.ArgumentException("FixedString length cannot be 0 or less");

            definedSize = strSize;
            builder = new StringBuilder(definedSize, definedSize);
            var fields = typeof(StringBuilder).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            bool found = false;
            for (int i = 0; i < fields.Length; i++)
            {
                useChunk = fields[i].Name.Equals(NET4X_SB);
                if (useChunk)
                {
                    rawStrValue = new string(' ', definedSize);
                    charData = (char[])fields[i].GetValue(builder);
                    found = true;
                    break;
                }
                else if (fields[i].Name.Equals(MONO20_SB) || fields[i].Name.Equals(NET35_SB))
                {
                    rawStrValue = (string)fields[i].GetValue(builder);
                    found = true;
                    break;
                }
            }

            if (!found)
                throw new System.NotImplementedException("Unknown StringBuilder version");
        }

        public string PopValue()
        {
            if (builder.Length == 0)
                return string.Empty;

            if (useChunk)
            {
                RawCopyIntoString();
            }
            else
            {
                var len = (builder.MaxCapacity - builder.Length);
                builder.Append('\0', len);
            }

            builder.Length = 0;
            return rawStrValue;
        }

        private unsafe void RawCopyIntoString()
        {
            if (rawStrValue.Length != definedSize || builder.Length > definedSize || charData.Length < definedSize)
                throw new ArgumentOutOfRangeException(string.Format("Internal structure mismatch! Defined size: {0}, string: {1}, builder: {2}, char buffer: {3}", definedSize, rawStrValue.Length, builder.Length, charData.Length));

            unsafe
            {
                fixed (char* fxdStrPntr = rawStrValue)
                {
                    var len = builder.Length;
                    for (int i = 0; i < len; i++)
                        fxdStrPntr[i] = charData[i];

                    for (int j = len; j < definedSize; j++)
                        fxdStrPntr[j] = '\0';
                }
            }
        }


        // StringBuilder Wrapper

        #region BASICS
        public void Concat(string str_val)
		{
			builder.Append(str_val);
		}

        public void Concat(string str_val, int startPos, int count)
		{
			builder.Append(str_val, startPos, count);
		}

        public void Concat(bool bool_val)
		{
			builder.Append(bool_val);
		}

        public void Concat(char char_val)
		{
			builder.Append(char_val);
		}

        public void Concat(char char_val, int repeat)
		{
			builder.Append(char_val, repeat);
		}

        public void Concat(char[] charArr_val)
		{
			builder.Append(charArr_val);
		}

        public void Concat(char[] charArr_val, int startPos, int count)
		{
			builder.Append(charArr_val,startPos, count);
		}

        #endregion

        #region UINT32
        private void ConcatInternal(uint uint_val, uint pad_base, char pad_char, uint base_val, bool negative)
        {
            pad_base = Convert.ToUInt32(pad_base <= 10) * pad_base;
            uint length = 0;
            uint calcVal = uint_val;

            do
            {
                calcVal /= base_val;
                length++;
            }
            while (calcVal > 0);

            int padLen = (int)(pad_base - length);
            int strpos = workCache.Length;

            while (length > 0)
            {
                strpos--;
                workCache[strpos] = DIGITS_LUT[uint_val % base_val];
                uint_val /= base_val;
                length--;
            }
            while (padLen > 0)
            {
                strpos--;
                workCache[strpos] = pad_char;
                padLen--;
            }
            if (negative) workCache[--strpos] = '-';

            builder.Append(workCache, strpos, workCache.Length - strpos);
        }

        public void Concat(uint uint_val, uint pad_base, char pad_char, uint base_val)
        {
            ConcatInternal(uint_val, pad_base, pad_char, base_val, false);
        }

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume no padding and base ten.
        public void Concat(uint uint_val)
        {
            ConcatInternal(uint_val, 0, DEFAULT_PAD_CHAR, 10, false);
        }

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public void Concat(uint uint_val, uint pad_amount)
        {
            ConcatInternal(uint_val, pad_amount, DEFAULT_PAD_CHAR, 10, false);
        }

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public void Concat(uint uint_val, uint pad_amount, char pad_char)
        {
            ConcatInternal(uint_val, pad_amount, pad_char, 10, false);
        }
        #endregion

        #region INT32
        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Any base value allowed.
        public void Concat(int int_val, uint pad_base, char pad_char, uint base_val)
        {
            if (int_val < 0)
            {
                uint uint_val = uint.MaxValue - ((uint)int_val) + 1; //< This is to deal with Int32.MinValue
                ConcatInternal(uint_val, pad_base, pad_char, base_val, true);
            }
            else
            {
                ConcatInternal((uint)int_val, pad_base, pad_char, base_val, false);
            }
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Assume no padding and base ten.
        public void Concat(int int_val)
        {
            Concat(int_val, 0, DEFAULT_PAD_CHAR, 10);
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public void Concat(int int_val, uint pad_amount)
        {
            Concat(int_val, pad_amount, DEFAULT_PAD_CHAR, 10);
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public void Concat(int int_val, uint pad_amount, char pad_char)
        {
            Concat(int_val, pad_amount, pad_char, 10);
        }
        #endregion

        #region UINT64/ULONG
        //! Convert a given unsigned long value to a string and concatenate onto the stringbuilder. Any base value allowed.
        public void ConcatInternal(ulong ulong_val, uint pad_base, char pad_char, uint base_val, bool negative)
        {
            pad_base = Convert.ToUInt32(pad_base <= 20) * pad_base;
            uint length = 0;
            ulong calcVal = ulong_val;

            do
            {
                calcVal /= base_val;
                length++;
            }
            while (calcVal > 0);

            int padLen = (int)(pad_base - length);
            int strpos = workCache.Length;

            while (length > 0)
            {
                strpos--;
                workCache[strpos] = DIGITS_LUT[ulong_val % base_val];
                ulong_val /= base_val;
                length--;
            }
            while (padLen > 0)
            {
                strpos--;
                workCache[strpos] = pad_char;
                padLen--;
            }
            if (negative) workCache[--strpos] = '-';

            builder.Append(workCache, strpos, workCache.Length - strpos);
        }

        public void Concat(ulong ulong_val)
        {
            ConcatInternal(ulong_val, 0, DEFAULT_PAD_CHAR, 10, false);
        }

        //! Convert a given unsigned long value to a string and concatenate onto the stringbuilder. Assume base ten.
        public void Concat(ulong ulong_val, uint pad_amount)
        {
            ConcatInternal(ulong_val, pad_amount, DEFAULT_PAD_CHAR, 10, false);
        }

        //! Convert a given unsigned long value to a string and concatenate onto the stringbuilder. Assume base ten.
        public void Concat(ulong ulong_val, uint pad_amount, char pad_char)
        {
            ConcatInternal(ulong_val, pad_amount, pad_char, 10, false);
        }

        public void Concat(ulong ulong_val, uint pad_amount, char pad_char, uint base_val)
        {
            ConcatInternal(ulong_val, pad_amount, pad_char, 10, false);
        }
        #endregion

        #region INT64/LONG
        //! Convert a given signed long value to a string and concatenate onto the stringbuilder. Assume base ten.
        public void Concat(long long_val, uint pad_amount, char pad_char, uint base_val)
        {
            if (long_val < 0)
            {
                ulong ulong_val = ulong.MaxValue - ((ulong)long_val) + 1;
                ConcatInternal(ulong_val, pad_amount, pad_char, base_val, true);
            }
            else
            {
                ConcatInternal((ulong)long_val, pad_amount, pad_char, base_val, false);
            }
        }

        public void Concat(long long_val)
        {
            Concat(long_val, 0, DEFAULT_PAD_CHAR, 10);
        }

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public void Concat(long long_val, uint pad_amount)
        {
            Concat(long_val, pad_amount, DEFAULT_PAD_CHAR, 10);
        }

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public void Concat(long long_val, uint pad_amount, char pad_char)
        {
            Concat(long_val, pad_amount, pad_char, 10);
        }
        #endregion

        #region FLOATS
        //! Convert a given float value to a string and concatenate onto the stringbuilder
        public void Concat(float float_val, uint decimal_places, uint pad_base, char pad_char)
        {
            pad_base = Convert.ToUInt32(pad_base <= 5) * pad_base;
            decimal_places = decimal_places > MAX_DECIMALS ? MAX_DECIMALS : decimal_places;
            if (decimal_places == 0)
            {
                int int_val;
                if (float_val >= 0.0f)
                {
                    int_val = (int)(float_val + 0.5f);
                }
                else
                {
                    int_val = (int)(float_val - 0.5f);
                }
                Concat(int_val, pad_base, pad_char, 10);
            }
            else
            {
                int int_part = (int)float_val;
                Concat(int_part, pad_base, pad_char, 10);
                float remainder = Math.Abs(float_val - int_part);
                int pos = 0;
                workCache[pos] = '.';

                do
                {
                    pos++;
                    remainder *= 10;
                    workCache[pos] = DIGITS_LUT[(uint)remainder % 10];
                    decimal_places--;
                }
                while (decimal_places > 0);
               
                builder.Append(workCache, 0, ++pos);
            }
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder. Assumes five decimal places, and no padding.
        public void Concat(float float_val)
        {
            Concat(float_val, MAX_DECIMALS, 0, DEFAULT_PAD_CHAR);
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder. Assumes no padding.
        public void Concat(float float_val, uint decimal_places)
        {
            Concat(float_val, decimal_places, 0, DEFAULT_PAD_CHAR);
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder.
        public void Concat(float float_val, uint decimal_places, uint pad_amount)
        {
            Concat(float_val, decimal_places, pad_amount, DEFAULT_PAD_CHAR);
        }
        #endregion

        #region Format
        public void ConcatFormat<A>(string format_string, A arg1 )
            where A : IConvertible
        {
            ConcatFormat<A, int, int, int>(format_string, arg1, 0, 0, 0);
        }

        //! Concatenate a formatted string with arguments
        public void ConcatFormat<A, B>(string format_string, A arg1, B arg2)
            where A : IConvertible
            where B : IConvertible
        {
            ConcatFormat<A, B, int, int>(format_string, arg1, arg2, 0, 0);
        }

        //! Concatenate a formatted string with arguments
        public void ConcatFormat<A, B, C>(string format_string, A arg1, B arg2, C arg3)
            where A : IConvertible
            where B : IConvertible
            where C : IConvertible
        {
            ConcatFormat<A, B, C, int>(format_string, arg1, arg2, arg3, 0);
        }

        //! Concatenate a formatted string with arguments
        public void ConcatFormat<A,B,C,D>(string format_string, A arg1, B arg2, C arg3, D arg4)
            where A : IConvertible
            where B : IConvertible
            where C : IConvertible
            where D : IConvertible
        {
            int verbatim_range_start = 0;

            for (int indx = 0; indx < format_string.Length; indx++)
            {
                if (format_string[indx] == '{')
                {
                    // Formatting bit now, so make sure the last block of the string is written out verbatim.
                    if (verbatim_range_start < indx)
                    {
                        // Write out unformatted string portion
                        builder.Append(format_string, verbatim_range_start, indx - verbatim_range_start);
                    }

                    uint base_value = DEFAULT_BASE;
                    uint padding = 0;
                    uint decimal_places = MAX_DECIMALS; // Default decimal places in .NET libs

                    indx++;
                    char format_char = format_string[indx];
                    if (format_char == '{')
                    {
                        builder.Append('{');
                        indx++;
                    }
                    else
                    {
                        indx++;
                        if (format_string[indx] == ':')
                        {
                            // Extra formatting. This is a crude first pass proof-of-concept. It's not meant to cover
                            // comprehensively what the .NET standard library Format() can do.
                            indx++;

                            // Deal with padding
                            while (format_string[indx] == '0')
                            {
                                indx++;
                                padding++;
                            }
                            if (format_string[indx] == 'X')
                            {
                                indx++;

                                // Print in hex
                                base_value = 16;

                                // Specify amount of padding ( "{0:X8}" for example pads hex to eight characters
                                if ((format_string[indx] >= '0') && (format_string[indx] <= '9'))
                                {
                                    padding = (uint)(format_string[indx] - '0');
                                    indx++;
                                }
                            }
                            else if (format_string[indx] == '.')
                            {
                                indx++;

                                // Specify number of decimal places
                                decimal_places = 0;

                                while (format_string[indx] == '0')
                                {
                                    indx++;
                                    decimal_places++;
                                }
                            }        
                        }
                   

                        // Scan through to end bracket
                        while (format_string[indx] != '}')
                        {
                            indx++;
                        }

                        // Have any extended settings now, so just print out the particular argument they wanted
                        switch (format_char)
                        {
                            case '0': ConcatFormatValue<A>(arg1, padding, base_value, decimal_places); break;
                            case '1': ConcatFormatValue<B>(arg2, padding, base_value, decimal_places); break;
                            case '2': ConcatFormatValue<C>(arg3, padding, base_value, decimal_places); break;
                            case '3': ConcatFormatValue<D>(arg4, padding, base_value, decimal_places); break;
                            default: Debug.Assert(false, "Invalid parameter index"); break;
                        }
                    }

                    // Update the verbatim range, start of a new section now
                    verbatim_range_start = (indx + 1);
                }
            }

            // Anything verbatim to write out?
            if (verbatim_range_start < format_string.Length)
            {
                // Write out unformatted string portion
                builder.Append(format_string, verbatim_range_start, format_string.Length - verbatim_range_start);
            }
        }

        //! The worker method. This does a garbage-free conversion of a generic type, and uses the garbage-free Concat() to add to the stringbuilder
        private void ConcatFormatValue<T>(T arg, uint padding, uint base_value, uint decimals) where T : IConvertible
        {
            switch (arg.GetTypeCode())
            {
                case TypeCode.Int64:
                {
                    Concat(arg.ToInt64(System.Globalization.NumberFormatInfo.CurrentInfo), padding, DEFAULT_PAD_CHAR, base_value);
                    break;
                }
                case TypeCode.UInt64:
                {
                    Concat(arg.ToUInt64(System.Globalization.NumberFormatInfo.CurrentInfo), padding, DEFAULT_PAD_CHAR, base_value);
                    break;
                }
                case TypeCode.UInt32:
                {
                    Concat(arg.ToUInt32(System.Globalization.NumberFormatInfo.CurrentInfo), padding, DEFAULT_PAD_CHAR, base_value);
                    break;
                }
                case TypeCode.Int32:
                {
                    Concat(arg.ToInt32(System.Globalization.NumberFormatInfo.CurrentInfo), padding, DEFAULT_PAD_CHAR, base_value);
                    break;
                }
                case TypeCode.Single:
                {
                    Concat(arg.ToSingle(System.Globalization.NumberFormatInfo.CurrentInfo), decimals, padding, DEFAULT_PAD_CHAR);
                    break;
                }
                case TypeCode.Int16:
                {
                    Concat(arg.ToInt32(System.Globalization.NumberFormatInfo.CurrentInfo), padding, DEFAULT_PAD_CHAR, base_value);
                    break;
                }
                case TypeCode.UInt16:
                {
                    Concat(arg.ToUInt32(System.Globalization.NumberFormatInfo.CurrentInfo), padding, DEFAULT_PAD_CHAR, base_value);
                    break;
                }
                case TypeCode.Byte:
                {
                    Concat(arg.ToUInt32(System.Globalization.NumberFormatInfo.CurrentInfo), padding, DEFAULT_PAD_CHAR, base_value);
                    break;
                }
                case TypeCode.SByte:
                {
                    Concat(arg.ToInt32(System.Globalization.NumberFormatInfo.CurrentInfo), padding, DEFAULT_PAD_CHAR, base_value);
                    break;
                }
                case TypeCode.String:
                {
                    builder.Append(arg.ToString(System.Globalization.NumberFormatInfo.CurrentInfo));
                    break;
                }
                case TypeCode.Boolean:
                {
                    builder.Append(arg.ToBoolean(System.Globalization.NumberFormatInfo.CurrentInfo));
                    break;
                }
                case TypeCode.Char:
                {
                    builder.Append(arg.ToChar(System.Globalization.NumberFormatInfo.CurrentInfo));
                    break;
                }
                default:
                {
                    throw new ArgumentException("Unknown parameter type");
                    break;
                }
            }
        }
        #endregion
    }
}
