using System.Reflection;
using System.Text;

namespace FPSCounter
{
    internal class FixedString
    {
        // Constants
        const string MONO20_SB = "_str";
        const string NET35_SB = "m_StringValue";
        const string NET4X_SB = "m_ChunkChars";
        const uint MAX_DECIMALS = 2;
        const char DEFAULT_PAD_CHAR = ' ';
        static readonly char[] DIGITS_LUT = new char[]{'0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F'};


        // Setup fields
        private readonly StringBuilder builder;
        private string rawValue;
        private readonly int rawSize;
        private readonly char[] charData;
        private readonly bool useChunk;

        // Worker fields
        // work Cache set to 21 to accomodate ulong.MaxValue; Float is always clamped
        private readonly char[] workCache = new char[21];


        // Publics
        public int Length { get { return builder.Length; } }
        public int DefinedSize { get { return rawSize; } }
        public bool IsChunkImplementation { get { return useChunk; } }

        // Own logic

        public FixedString(int strSize)
        {
            if (strSize <= 0)
                throw new System.ArgumentException("FixedString length cannot be 0 or less");

            rawSize = strSize;
            builder = new StringBuilder(rawSize, rawSize);
            var fields = typeof(StringBuilder).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            bool found = false;
            for (int i = 0; i < fields.Length; i++)
            {
                useChunk = fields[i].Name.Equals(NET4X_SB);
                if (useChunk)
                {
                    rawValue = new string(' ', rawSize);
                    charData = (char[])fields[i].GetValue(builder);
                    found = true;
                    break;
                }
                else if (fields[i].Name.Equals(MONO20_SB) || fields[i].Name.Equals(NET35_SB))
                {
                    rawValue = (string)fields[i].GetValue(builder);
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
                if (builder.Length >= charData.Length)
                    throw new ArgumentOutOfRangeException("Builder lenght inconsistent with char array");

                var len = charData.Length;
                for (int i = builder.Length; i < len; i++)
                    charData[i] = '\0';

                RawCopyIntoString();
            }
            else
            {
                var len = (builder.MaxCapacity - builder.Length);
                builder.Append('\0', len);
            }

            builder.Length = 0;
            return rawValue;
        }

        private unsafe void RawCopyIntoString()
        {
            if (rawSize > rawValue.Length || builder.Length > rawSize || charData.Length < rawSize)
                throw new ArgumentOutOfRangeException("Size mismatch between supplied data and max string size");

            unsafe
            {
                fixed (char* dest_fixed = rawValue)
                {
                    for (int i = 0; i < rawSize; i++)
                        dest_fixed[i] = charData[i];
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
     }
}
