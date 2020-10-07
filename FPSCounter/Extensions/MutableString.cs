using System;

namespace FPSCounter
{
    public class MutableString
    {
        // Constants
        public enum BaseValue
        {
            Binary = 2,
            Decimal = 10,
            Hex = 16
        }
        const uint MAX_DECIMALS = 2;
        const BaseValue DEFAULT_BASE = BaseValue.Decimal;
        static readonly char[] DIGITS_LUT = new char[]{'0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F'};

        // Setup fields
        private int m_Pos;
        private string m_valueStr;
        private char m_defaultPadChar = ' ';
        private readonly bool dontThrow = false;

        // Publics
        public char DefaultPadChar { get { return m_defaultPadChar; } set { m_defaultPadChar = value; } }
        public int Capacity { get { return m_valueStr.Length; } }
        public int Length { get { return m_Pos; } set { m_Pos = value; } }
        public string CurrentRaw { get { return m_valueStr; } }


        public MutableString(int size) : this(size, '\0', false) { }
        public MutableString(int size, bool ignoreOverflow) : this(size, '\0', ignoreOverflow) { }
        public MutableString(int size, char fillChar) : this(size, fillChar, false) { }

        public MutableString(int size, char fillChar, bool ignoreOverflow)
        {
            if (size < 2)
                throw new ArgumentException("Size cannot be 1 or less");
            m_valueStr = new string(fillChar, size);
            dontThrow = ignoreOverflow;
        }

        public string Finalize()
        {
            if (m_Pos == 0)
                return string.Empty;

            var free = m_valueStr.Length - m_Pos;
            if (free > 0)
                repeatChar('\0', free);

            m_Pos = 0;
            return m_valueStr;
        }


        //
        // LOGIC
        //
        #region BASICS
        public MutableString Append(bool value)
        {
            Append(value.ToString());
            return this;
        }
        public MutableString Append(byte value)
        {
            Append((uint)value);
            return this;
        }
        public MutableString Append(sbyte value)
        {
            Append((int)value);
            return this;
        }
        public MutableString Append(short value)
        {
            Append((int)value);
            return this;
        }
        public MutableString Append(ushort value)
        {
            Append((int)value);
            return this;
        }
        #endregion
        #region CHARS
        public MutableString Append(char[] value, int indx, int count)
        {
            if (value == null)
                return this;
            var len = value.Length;
            if (count < 1 || indx < 0 || (len < count + indx))
                return this;
            if (len > 1)
                AppendInternal(value, indx, count);
            else
                Append(value[0]);
            return this;
        }

        public MutableString Append(char[] value)
        {
            if (value == null)
                return this;
            var len = value.Length;
            if (len > 1)
                AppendInternal(value, 0, len);
            else
                Append(value[0]);
            return this;
        }

        public MutableString Append(char value)
        {
            if (m_Pos >= m_valueStr.Length)
            {
                if (dontThrow)
                    return this;
                else
                    throw new ArgumentException("Not enough free space to accomodate element!");
            }   
            singleChar(value);
            m_Pos++;
            return this;
        }

        private void AppendInternal(char[] value, int indx, int count)
        {
            var free = m_valueStr.Length - m_Pos;
            if (count > free)
            {
                if (dontThrow)
                    return;
                else
                    throw new ArgumentException(string.Format("Not enough free space to accomodate {0} elements!", count));
            }
            charCopy(value, indx, count);
            m_Pos = m_Pos + count;
        }
        #endregion

        #region STRINGS
        private void AppendInternal(string value, int indx, int count)
        {
            var free = m_valueStr.Length - m_Pos;
            if (count > free)
            {
                if (dontThrow == true)
                    return;
                else
                    throw new ArgumentOutOfRangeException(string.Format("Not enough free space to accomodate {0} elements!", count));
            }
            stringCopy(value, indx, count);
            m_Pos = m_Pos + count;
        }

        public MutableString Append(string value, int indx, int count)
        {
            if (value == null)
                return this;
            var len = value.Length;
            if (count < 1 || indx < 0 || (len < count + indx))
                return this;
            if (len > 1)
                AppendInternal(value, indx, count);
            else
                Append(value[0]);
            return this;
        }

        public MutableString Append(string value)
        {
            if (value == null)
                return this;
            var len = value.Length;
            if (len > 1)
                AppendInternal(value, 0, len);
            else
                Append(value[0]);
            return this;
        }
        #endregion


        // VALUE TYPES
        #region UINT32
        private void AppendUINT32(uint uint_val, uint pad_base, char pad_char, bool negative)
        {
            int length = CountDigits(uint_val);
            int padLen = pad_base <= length ? 0 : (int)(pad_base - length);
            int finalLen = (Convert.ToInt32(negative) + padLen + length);
            int finalPos = m_Pos + finalLen;

            if (finalPos > m_valueStr.Length)
            {
                if (dontThrow == true)
                    return;
                else
                    throw new ArgumentOutOfRangeException(string.Format("Not enough free space to accomodate {0} elements!", finalLen));
            }
            unsafe
            {
                fixed (char* ptrDest = m_valueStr)
                {
                    char* ptrFin = ptrDest + finalPos;
                    do
                    {
                        uint div = uint_val / 10;
                        *(--ptrFin) = (char)('0' + uint_val - (div * 10));
                        uint_val = div;
                    }
                    while (uint_val != 0);

                    while (padLen > 0)
                    {
                        *(--ptrFin) = pad_char;
                        padLen--;
                    }

                    if (negative) *(--ptrFin) = '-';
                }
            }
            m_Pos = finalPos;
        }

        public MutableString Append(uint uint_val)
        {
            AppendUINT32(uint_val, 0, m_defaultPadChar, false);
            return this;
        }

        public MutableString Append(uint uint_val, uint pad_amount)
        {
            AppendUINT32(uint_val, pad_amount, m_defaultPadChar, false);
            return this;
        }

        public MutableString Append(uint uint_val, uint pad_amount, char pad_char)
        {
            AppendUINT32(uint_val, pad_amount, pad_char, false);
            return this;
        }

        #endregion


        #region INT32

        public MutableString Append(int int_val)
        {
            Append(int_val, 0, m_defaultPadChar);
            return this;
        }

        public MutableString Append(int int_val, uint pad_amount)
        {
            Append(int_val, pad_amount, m_defaultPadChar);
            return this;
        }

        public MutableString Append(int int_val, uint pad_base, char pad_char)
        {
            uint val = GetPositiveEqv(int_val, out bool neg);
            AppendUINT32(val, pad_base, pad_char, neg);
            return this;
        }
        #endregion


        #region UINT64/ULONG
        private void AppendULONG(ulong ulong_val, uint pad_base, char pad_char, bool negative)
        {
            int length = CountDigits(ulong_val);
            int padLen = pad_base <= length ? 0 : (int)(pad_base - length);
            int finalLen = (Convert.ToInt32(negative) + length + padLen);
            int finalPos = m_Pos + finalLen;

            if (finalPos > m_valueStr.Length)
            {
                if (dontThrow == true)
                    return;
                else
                    throw new ArgumentOutOfRangeException(string.Format("Not enough free space to accomodate {0} elements!", finalLen));
            }
            unsafe
            {
                fixed (char* ptrDest = m_valueStr)
                {
                    char* ptrFin = ptrDest + finalPos;
                    do
                    {
                        ulong div = ulong_val / 10;
                        *(--ptrFin) = (char)('0' + ulong_val - (div * 10));
                        ulong_val = div;
                    }
                    while (ulong_val != 0);

                    while (padLen > 0)
                    {
                        *(--ptrFin) = pad_char;
                        padLen--;
                    }

                    if (negative) *(--ptrFin) = '-';
                }
            }
            m_Pos = finalPos;
        }

        public MutableString Append(ulong ulong_val)
        {
            AppendULONG(ulong_val, 0, m_defaultPadChar, false);
            return this;
        }

        public MutableString Append(ulong ulong_val, uint pad_amount)
        {
            AppendULONG(ulong_val, pad_amount, m_defaultPadChar, false);
            return this;
        }

        public MutableString Append(ulong ulong_val, uint pad_amount, char pad_char)
        {
            AppendULONG(ulong_val, pad_amount, pad_char, false);
            return this;
        }

        #endregion


        #region INT64/LONG

        public MutableString Append(long long_val)
        {
            Append(long_val, 0, m_defaultPadChar);
            return this;
        }

        public MutableString Append(long long_val, uint pad_base)
        {
            Append(long_val, pad_base, m_defaultPadChar);
            return this;
        }

        public MutableString Append(long long_val, uint pad_base, char pad_char)
        {
            var neg = long_val < 0;
            ulong val = neg ? ulong.MaxValue - ((ulong)long_val) + 1 : (ulong)long_val;
            AppendULONG(val, pad_base, pad_char, neg);
            return this;
        }
        #endregion


        #region FLOATS
        public MutableString Append(float float_val, uint decimal_places, uint pad_base, char pad_char)
        {
            decimal_places = decimal_places > MAX_DECIMALS ? MAX_DECIMALS : decimal_places;
            bool isNegative = float_val < 0f;
            float roundUp = 5f / (float)Math.Pow(10, (1 + decimal_places));
            float finalVal = isNegative ? (float_val + -roundUp) : (float_val + roundUp);

            // Early out if we dont have proper digit value
            if (!IsFinite(float_val))
            {
                if (float_val != float_val) // le implementation detail
                    AppendInternal("NaN", 0, 3);
                else
                    AppendInternal(isNegative ? "-∞" : "+∞", 0, 2);
                return this;
            }

            var asUInt32 = GetPositiveEqv((int)(finalVal), out bool n);
            // Early out if we need basically an integer part
            if (decimal_places == 0)
            {
                AppendUINT32(asUInt32, pad_base, pad_char, isNegative);
                return this;
            }
            
            var ceilLen = CountDigits(asUInt32);
            int length = ceilLen + 1 + (int)decimal_places;
            int padLen = pad_base <= ceilLen ? 0 : (int)(pad_base - ceilLen);
            int finalLen = (Convert.ToInt32(isNegative) + padLen + length);
            int finalPos = m_Pos + finalLen;

            if (finalPos > m_valueStr.Length)
            {
                if (dontThrow == true)
                    return this;
                else
                    throw new ArgumentOutOfRangeException(string.Format("Not enough free space to accomodate {0} elements!", finalLen));
            }

            unsafe
            {
                fixed (char* ptrDest = m_valueStr)
                {
                    char* ptrFin = ptrDest + finalPos;
                    uint finalInt = (uint)(finalVal * (float)Math.Pow(10, decimal_places));

                    do
                    {
                        uint div = finalInt / 10;
                        *(--ptrFin) = (char)('0' + finalInt - (div * 10));
                        finalInt = div;
                        decimal_places--;
                    }
                    while (decimal_places != 0);

                    *(--ptrFin) = '.';

                    do
                    {
                        uint div = finalInt / 10;
                        *(--ptrFin) = (char)('0' + finalInt - (div * 10));
                        finalInt = div;
                        ceilLen--;
                    }
                    while (ceilLen != 0);

                    while (padLen > 0)
                    {
                        *(--ptrFin) = pad_char;
                        padLen--;
                    }

                    if (isNegative) *(--ptrFin) = '-';

                }
            }
            m_Pos = finalPos;
            return this;
        }

        public MutableString Append(float float_val)
        {
            return Append(float_val, MAX_DECIMALS, 0, m_defaultPadChar);
        }

        public MutableString Append(float float_val, uint decimal_places)
        {
            return Append(float_val, decimal_places, 0, m_defaultPadChar);
        }

        public MutableString Append(float float_val, uint decimal_places, uint pad_amount)
        {
            return Append(float_val, decimal_places, pad_amount, m_defaultPadChar);
        }
        #endregion


        #region HELPERS/CORECLR

        private static uint GetPositiveEqv(int val, out bool isNegative)
        {
            isNegative = val < 0;
            return isNegative ? uint.MaxValue - ((uint)val) + 1 : (uint)val;
        }

        private static int CountDigits(ulong value)
        {
            int digits = 1;
            uint part;
            if (value >= 10000000)
            {
                if (value >= 100000000000000)
                {
                    part = (uint)(value / 100000000000000);
                    digits += 14;
                }
                else
                {
                    part = (uint)(value / 10000000);
                    digits += 7;
                }
            }
            else
            {
                part = (uint)value;
            }

            if (part < 10)
            {
                // no-op
            }
            else if (part < 100)
            {
                digits++;
            }
            else if (part < 1000)
            {
                digits += 2;
            }
            else if (part < 10000)
            {
                digits += 3;
            }
            else if (part < 100000)
            {
                digits += 4;
            }
            else if (part < 1000000)
            {
                digits += 5;
            }
            else
            {
                digits += 6;
            }

            return digits;
        }

        private static int CountDigits(uint value)
        {
            int digits = 1;
            if (value >= 100000)
            {
                value /= 100000;
                digits += 5;
            }

            if (value < 10)
            {
                // no-op
            }
            else if (value < 100)
            {
                digits++;
            }
            else if (value < 1000)
            {
                digits += 2;
            }
            else if (value < 10000)
            {
                digits += 3;
            }
            else
            {
                digits += 4;
            }

            return digits;
        }

        /*public static int CountHexDigits(ulong value)
        {
            return (64 - BitOperations.LeadingZeroCount(value | 1) + 3) >> 2;
        }*/

        private static int CountDecimalTrailingZeros(uint value, out uint valueWithoutTrailingZeros)
        {
            int zeroCount = 0;

            if (value != 0)
            {
                while (true)
                {
                    uint temp = value / 10;
                    if (value != (temp * 10))
                    {
                        break;
                    }

                    value = temp;
                    zeroCount++;
                }
            }

            valueWithoutTrailingZeros = value;
            return zeroCount;
        }

        private static uint Low32(ulong value) => (uint)value;

        private static uint High32(ulong value) => (uint)((value & 0xFFFFFFFF00000000) >> 32);

        private static unsafe int SingleToInt32Bits(float value)
        {
            return *((int*)&value);
        }

        private static bool IsFinite(float f)
        {
            int bits = SingleToInt32Bits(f);
            return (bits & 0x7FFFFFFF) < 0x7F800000;
        }

        #endregion


        // Copy logic

        private unsafe void stringCopy(string value, int indx, int charCount)
        {
            fixed (char* ptrDest = m_valueStr, ptrSrc = value)
            {
                wstrCpy(ptrDest + m_Pos, ptrSrc + indx, charCount);
            }
        }

        private unsafe void charCopy(char[] value, int indx, int charCount)
        {
            fixed (char* ptrDest = m_valueStr, ptrSrc = value)
            {
                wstrCpy(ptrDest + m_Pos, ptrSrc + indx, charCount);
            }
        }

        private unsafe void singleChar(char value)
        {
            fixed (char* ptrDest = m_valueStr)
            {
                ptrDest[m_Pos] = value;  
            }
        }

        private unsafe void repeatChar(char value, int count)
        {
            var fin = m_Pos + count;
            fixed (char* ptrDest = m_valueStr)
            {
                for (int i = m_Pos; i < fin; i++)
                {
                    ptrDest[i] = value;
                }
            }
            m_Pos = fin;
        }

        private unsafe void rawCopy(char* dest, char* src, int charCount)
        {
            for (int i = 0; i < charCount; i++)
                dest[i] = src[i];
        }

        private unsafe static void wstrCpy(char* dmem, char* smem, int charCount)
        {
            if (((int)dmem & 2) != 0)
            {
                *dmem = *smem;
                dmem++;
                smem++;
                charCount--;
            }

            while (charCount >= 8)
            {
                *(uint*)dmem = *(uint*)smem;
                *(uint*)(dmem + 2) = *(uint*)(smem + 2);
                *(uint*)(dmem + 4) = *(uint*)(smem + 4);
                *(uint*)(dmem + 6) = *(uint*)(smem + 6);
                dmem += 8;
                smem += 8;
                charCount -= 8;
            }

            if ((charCount & 4) != 0)
            {
                *(uint*)dmem = *(uint*)smem;
                *(uint*)(dmem + 2) = *(uint*)(smem + 2);
                dmem += 4;
                smem += 4;
            }

            if ((charCount & 2) != 0)
            {
                *(uint*)dmem = *(uint*)smem;
                dmem += 2;
                smem += 2;
            }

            if ((charCount & 1) != 0)
            {
                *dmem = *smem;
            }
        }
    }
}
