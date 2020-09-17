using System.Reflection;
using System.Text;

namespace FPSCounter
{
    internal class FixedString
    {
        public readonly StringBuilder builder;
        private string value;
        private readonly char[] charData;
        private readonly FieldInfo cachedFld;
        private readonly bool isChunky;

        public FixedString(int strSize)
        {
            value = new string(' ', strSize);
            builder = new StringBuilder(strSize, strSize);

            var fields = typeof(StringBuilder).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                isChunky = fields[i].Name.Equals("m_ChunkChars");
                if (isChunky)
                {
                    cachedFld = fields[i];
                    charData = (char[])fields[i].GetValue(builder);
                    break;
                }
                else if (fields[i].Name.Equals("_str") || fields[i].Name.Equals("m_StringValue"))
                {
                    cachedFld = fields[i];
                    value = (string)fields[i].GetValue(builder);
                    break;
                }
            }

            if (cachedFld == null)
                throw new System.NotImplementedException("Unknown StringBuilder version");
        }

        public string PopValue()
        {
            if (isChunky)
                CopyIntoString(value, charData, builder.Length);
            else
                for (int i = builder.Length; i < builder.Capacity; i++)
                    builder.Append((char)0);

            builder.Length = 0;
            return value;
        }

        public static unsafe void CopyIntoString(string dest_string, char[] char_buffer, int length)
        {
            System.Diagnostics.Debug.Assert(dest_string.Length >= length);
            fixed (char* dest_fixed = dest_string)
            {
                // Copy in the string data
                for (int i = 0; i < length; i++)
                    dest_fixed[i] = char_buffer[i];
                // NULL terminate the dest string
                if (length < dest_string.Length)
                    dest_fixed[length] = (char)0;
            }
        }

    }
}
