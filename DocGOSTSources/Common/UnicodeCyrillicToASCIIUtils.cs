using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Common
{
    internal static class UnicodeCyrillicToASCIIUtils
    {
        private enum UnicodeHexCode : ushort
        {
            A = 0x0410,
            B = 0x0412,
            E = 0x0415,
            K = 0x041A,
            M = 0x041C,
            H = 0x041D,
            O = 0x041E,
            P = 0x0420,
            C = 0x0421,
            T = 0x0422
        }

        private enum ASCIIHexCode : byte
        {
            A = 0x41,
            B = 0x42,
            E = 0x45,
            K = 0x4B,
            M = 0x4D,
            H = 0x48,
            O = 0x4F,
            P = 0x50,
            C = 0x43,
            T = 0x54,
            Question = 0x3F
        }

        /// <summary>
        /// проверка что строка имеет кодировку Unicode
        /// </summary>
        /// <param name="aString">входная строка</param>
        /// <returns>
        ///   <c>true</c> unicode; иначе ascii <c>false</c>.
        /// </returns>
        public static bool IsUnicode(string aString)
        {
            return (Encoding.ASCII.GetByteCount(aString) != Encoding.UTF8.GetByteCount(aString));
        }

        /// <summary>
        /// преобразование строки unicode с русскими символами в строку с ascii символами
        /// русские символны преобразуются в латинские по их внешнему соотвестствию: О = О, К = К, и т.д.
        /// если символ не преобразовать, он заменяется на ?
        /// </summary>
        /// <param name="aUniString">входная строка в кодировке unicode, содержащая кириллицу</param>
        /// <returns>выходная строка в кодировке ascii</returns>
        public static string UnicodeRusStringToASCII(string aUniString)
        {
            Encoding ascii = Encoding.ASCII;
            Encoding unicode = Encoding.Unicode;

            // Convert the string into a byte array.
            byte[] unicodeBytes = unicode.GetBytes(aUniString);

            // Perform the conversion from one encoding to the other.
            byte[] asciiBytes = UnicodeBytesToASCIIBytes(unicodeBytes);

            // Convert the new byte[] into a char[] and then into a string.
            char[] asciiChars = new char[ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
            ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
            return new string(asciiChars);
        }

        /// <summary>
        /// Unicodes the bytes to ASCII bytes.
        /// </summary>
        /// <param name="aUnicodeBytes">a unicode bytes.</param>
        /// <returns></returns>
        private static byte[] UnicodeBytesToASCIIBytes(byte[] aUnicodeBytes)
        {
            List<byte> asciiList = new List<byte>();
            for (int i = 0; i < aUnicodeBytes.Length; i += 2)
            {
                ushort unisymbol = (ushort)(aUnicodeBytes[i+1] << 8 | aUnicodeBytes[i]);
                byte ascii = GetASCIISymbol(unisymbol);
                asciiList.Add(ascii);
            }

            return asciiList.ToArray();
        }

        /// <summary>
        /// получить символ ASCII из символа unicode
        /// </summary>
        /// <param name="aUnicodeSymbol">a unicode symbol.</param>
        /// <returns></returns>
        private static byte GetASCIISymbol(ushort aUnicodeSymbol)
        {
            if (aUnicodeSymbol >> 8 == 0)
                return (byte)aUnicodeSymbol;

            if (Enum.IsDefined(typeof(UnicodeHexCode), aUnicodeSymbol))
            {                
                switch ((UnicodeHexCode)aUnicodeSymbol)
                {
                    case UnicodeHexCode.A:
                        return (byte)ASCIIHexCode.A;
                    case UnicodeHexCode.B:
                        return (byte)ASCIIHexCode.B;
                    case UnicodeHexCode.E:
                        return (byte)ASCIIHexCode.E;
                    case UnicodeHexCode.K:
                        return (byte)ASCIIHexCode.K;
                    case UnicodeHexCode.M:
                        return (byte)ASCIIHexCode.M;
                    case UnicodeHexCode.H:
                        return (byte)ASCIIHexCode.H;
                    case UnicodeHexCode.O:
                        return (byte)ASCIIHexCode.O;
                    case UnicodeHexCode.P:
                        return (byte)ASCIIHexCode.P;
                    case UnicodeHexCode.C:
                        return (byte)ASCIIHexCode.C;
                    case UnicodeHexCode.T:
                        return (byte)ASCIIHexCode.T;
                }
            }

            return (byte)ASCIIHexCode.Question;
        }
    }
}
