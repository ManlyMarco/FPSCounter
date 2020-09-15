using System.Reflection;
using System.Text;

namespace FPSCounter
{
    public class FixedString
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
                cachedFld = fields[i];
                if (fields[i].Name.Equals("_str") || fields[i].Name.Equals("m_StringValue"))
                {
                    value = (string)fields[i].GetValue(builder);
                    isChunky = false;
                    break;
                }
                else if (fields[i].Name.Equals("m_ChunkChars"))
                {
                    charData = (char[])fields[i].GetValue(builder);
                    isChunky = true;
                    break;
                }
            }
        }

        public string PopValue()
        {
            if (isChunky)
            {
                CopyIntoString(value, charData, builder.Length);
            }
            else
            {
                for (int i = builder.Length; i < builder.Capacity; i++)
	                builder.Append((char)0);
                value = (string)cachedFld.GetValue(builder);
            }

            builder.Length = 0;
            return value;
        }

        public static unsafe void CopyIntoString(string dest_string, char[] char_buffer, int length)
        {
            System.Diagnostics.Debug.Assert( dest_string.Length >= length );
            unsafe
            {
                fixed ( char* dest_fixed = dest_string )
                {
                    // Copy in the string data
                    for ( int i = 0; i < length; i++ )
                        dest_fixed[i] = char_buffer[i];
                    // NULL terminate the dest string
                    if ( length < dest_string.Length )
                        dest_fixed[length] = (char)0;
                }
            }
        }
 
    }
}
