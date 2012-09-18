using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Xbim.Ifc.XbimExtensions.Parser
{
    public class StringConverter
    {
        private static String codePage = "ABCDEFGHI";

        public static String Decode(String inputString) 
        {
            
            Decoder decoderIso8859_1 = Encoding.GetEncoding("ISO-8859-1").GetDecoder();
            Decoder decoderUTF16 = Encoding.GetEncoding("UTF-16BE").GetDecoder();
            Decoder decoderUTF32 = Encoding.GetEncoding("UTF-32").GetDecoder();

            Decoder decoder = Encoding.Default.GetDecoder();
            bool extendedCodePage = false;
            String decodedString = "";
            char[] characterArray = inputString.ToCharArray();
            int codePoint;
            int index = -1;

            while (index < characterArray.Length - 1)
            //foreach (char item in characterArray)
	        {
                index++;

                codePoint = char.ConvertToUtf32(characterArray[index].ToString(), 0);
                if (codePoint < 32 || codePoint > 126)
                {
                    //CharacterCodingException e = new CharacterCodingException();
                    Exception eef = new Exception("CharacterCodingException: code point value is out of range - " + codePoint.ToString());
                    throw eef;
                }

                // single apostroph is encoded as two consecutive apostrophes
                if (characterArray[index] == '\'')
                {
                    decodedString += "\'";
                    index++;
                }
                else if (characterArray[index] == '\\')
                {
                    index++;
                    if (characterArray[index] == '\\')
                    { // not a control sequence but a normal back slash
                        decodedString += "\\";
                    }
                    else if (characterArray[index] == 'S')
                    {
                        index++;
                        if (extendedCodePage)
                        {
                            byte[] charBytes = BitConverter.GetBytes(characterArray[++index]);
                            char[] charResult = new char[1];
                            int charLen = decoder.GetChars(charBytes, 0, charBytes.Length, charResult, 0);
                            decodedString = decodedString + charResult.ToString();
                            //decodedString = decodedString.Concat(decoder.decode(buffer("" + characterArray[++index])).toString());
                        }
                        else
                        {
                            decodedString = decodedString + char.ConvertFromUtf32(char.ConvertToUtf32(characterArray.ToString(), ++index) + 128).ToString();
                            //decodedString = decodedString.Concat(new String(Character.toChars(Character.codePointAt(characterArray, ++index) + 128)));
                        }
                    }
                    else if (characterArray[index] == 'P')
                    {
                        index++;
                        int page = codePage.IndexOf(characterArray[index]) + 1;
                        Encoding iso = Encoding.GetEncoding("ISO-8859-" + page);

                        //CharSet charset = CharSet.forName("ISO-8859-" + page);
                        decoder = iso.GetDecoder();
                        extendedCodePage = true;
                        index++;
                    }
                    else if (characterArray[index] == 'X')
                    {
                        index++;
                        if (characterArray[index] == '\\')
                        {
                            int[] codePoints = new int[1];
                            string str = "0x" + characterArray[++index] + characterArray[++index];
                            codePoints[0] = char.ConvertToUtf32(Encoding.UTF8.GetString(Convert.FromBase64String(str)), 0);

                            byte[] charBytes = BitConverter.GetBytes(char.ConvertFromUtf32(codePoints[0])[0]);
                            char[] charResult = new char[1];
                            int charLen = decoderIso8859_1.GetChars(charBytes, 0, charBytes.Length, charResult, 0);
                            decodedString = decodedString + charResult.ToString();
                            //codePoints[0] = Integer.decode("0x" + characterArray[++index] + characterArray[++index]);
                            //decodedString = decodedString.concat(decoderIso8859_1.decode(buffer(codePoints)).toString());
                        }
                        else if (characterArray[index] == '2')
                        {
                            index++;
                            // UTF-16BE (UCS-2)
                            int[] codePoints;
                            do
                            {
                                codePoints = new int[2];
                                string str = "0x" + characterArray[++index] + characterArray[++index];
                                codePoints[0] = char.ConvertToUtf32(Encoding.UTF8.GetString(Convert.FromBase64String(str)), 0);
                                str = "0x" + characterArray[++index] + characterArray[++index];
                                codePoints[1] = char.ConvertToUtf32(Encoding.UTF8.GetString(Convert.FromBase64String(str)), 0);

                                byte[] charBytes = BitConverter.GetBytes(char.ConvertFromUtf32(codePoints[0])[0]);
                                char[] charResult = new char[1];
                                int charLen = decoderUTF16.GetChars(charBytes, 0, charBytes.Length, charResult, 0);
                                decodedString = decodedString + charResult.ToString();

                                charBytes = BitConverter.GetBytes(char.ConvertFromUtf32(codePoints[1])[0]);
                                charResult = new char[1];
                                charLen = decoderUTF16.GetChars(charBytes, 0, charBytes.Length, charResult, 0);
                                decodedString = decodedString + charResult.ToString();

                                //codePoints[0] = Integer.decode("0x" + characterArray[++index] + characterArray[++index]);
                                //codePoints[1] = Integer.decode("0x" + characterArray[++index] + characterArray[++index]);
                                //decodedString = decodedString.concat(decoderUTF16.decode(buffer(codePoints)).toString());
                            } while (characterArray[index + 1] != '\\'); // \X0\ is
                            // the
                            // end
                            // signal
                            index += 4; // . Second HEX-number is optional.
                        }
                        else if (characterArray[index] == '4')
                        {
                            index++;
                            // UTF32 (UCS-4)
                            int[] codePoints;
                            do
                            {
                                codePoints = new int[4];
                                string str = "0x" + characterArray[++index] + characterArray[++index];
                                codePoints[0] = char.ConvertToUtf32(Encoding.UTF8.GetString(Convert.FromBase64String(str)), 0);
                                str = "0x" + characterArray[++index] + characterArray[++index];
                                codePoints[1] = char.ConvertToUtf32(Encoding.UTF8.GetString(Convert.FromBase64String(str)), 0);
                                str = "0x" + characterArray[++index] + characterArray[++index];
                                codePoints[2] = char.ConvertToUtf32(Encoding.UTF8.GetString(Convert.FromBase64String(str)), 0);
                                str = "0x" + characterArray[++index] + characterArray[++index];
                                codePoints[3] = char.ConvertToUtf32(Encoding.UTF8.GetString(Convert.FromBase64String(str)), 0);

                                byte[] charBytes = BitConverter.GetBytes(char.ConvertFromUtf32(codePoints[0])[0]);
                                char[] charResult = new char[1];
                                int charLen = decoderUTF16.GetChars(charBytes, 0, charBytes.Length, charResult, 0);
                                decodedString = decodedString + charResult.ToString();

                                charBytes = BitConverter.GetBytes(char.ConvertFromUtf32(codePoints[1])[0]);
                                charResult = new char[1];
                                charLen = decoderUTF16.GetChars(charBytes, 0, charBytes.Length, charResult, 0);
                                decodedString = decodedString + charResult.ToString();

                                charBytes = BitConverter.GetBytes(char.ConvertFromUtf32(codePoints[2])[0]);
                                charResult = new char[1];
                                charLen = decoderUTF16.GetChars(charBytes, 0, charBytes.Length, charResult, 0);
                                decodedString = decodedString + charResult.ToString();

                                charBytes = BitConverter.GetBytes(char.ConvertFromUtf32(codePoints[3])[0]);
                                charResult = new char[1];
                                charLen = decoderUTF16.GetChars(charBytes, 0, charBytes.Length, charResult, 0);
                                decodedString = decodedString + charResult.ToString();

                                //codePoints[0] = Integer.decode("0x" + characterArray[++index] + characterArray[++index]);
                                //codePoints[1] = Integer.decode("0x" + characterArray[++index] + characterArray[++index]);
                                //codePoints[2] = Integer.decode("0x" + characterArray[++index] + characterArray[++index]);
                                //codePoints[3] = Integer.decode("0x" + characterArray[++index] + characterArray[++index]);
                                //decodedString = decodedString.concat(decoderUTF32.decode(buffer(codePoints)).toString());
                            } while (characterArray[index + 1] != '\\'); // \X0\ is
                            // the
                            // end
                            // signal
                            index += 4;
                        }
                    }
                }
                else
                {
                    decodedString = decodedString + characterArray[index].ToString();
                    //decodedString = decodedString.concat(new String(Character.toChars(characterArray[index])));
                }
            }

            return decodedString;
            
        }
    }
}
