using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.IO.Parser;

namespace Xbim.Tests
{
    [TestClass]
    public class P21Tests
    {
        private XbimP21StringDecoder decoder = new XbimP21StringDecoder();

        [TestMethod]
        [ExpectedException(typeof(XbimP21EofException))]
        public void Unescape_UpperDecimal_NotFollowedByChar_ThrowsEof()
        {
            decoder.Unescape(@"\S\");
        }

        [TestMethod]
        public void Unescape_UpperDecimal_NotFollowedByEscape_ReturnsSame()
        {
            const string expected = @"\S";
            var actual = decoder.Unescape(new IfcText(expected));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Unescape_UpperDecimal()
        {
            const string escaped = @"\S\D";
            const string expected = "Ä";
            var value = new IfcText(escaped);
            var unescaped = decoder.Unescape(value);
            // Assert.AreEqual(escaped, value.ToPart21.Trim('\''));
            Assert.AreEqual(expected, unescaped);
        }

        [TestMethod]
        public void Unescape_Iso8859()
        {
            const string escaped = @"\PA\\S\D";
            const string expected = "Ä";
            var value = new IfcText(escaped);
            var unescaped = decoder.Unescape(value);
            // Assert.AreEqual(escaped, value.ToPart21.Trim('\''));
            Assert.AreEqual(expected, unescaped);
        }

        [TestMethod]
        [ExpectedException(typeof(XbimP21EofException))]
        public void Unescape_8BitHex_NotFollowedByHexChar_ThrowsEof()
        {
            decoder.Unescape(@"\X\");
        }

        [TestMethod]
        [ExpectedException(typeof(XbimP21InvalidCharacterException))]
        public void Unescape_8BitHex_InvalidHex_ThrowsInvalidCharacter()
        {
            decoder.Unescape(@"\X\G0");
        }

        [TestMethod]
        public void Unescape_8BitHex()
        {
            const string escaped = @"\X\C4";
            const string expected = "Ä";
            var value = new IfcText(escaped);
            var unescaped = decoder.Unescape(value);
            // Assert.AreEqual(escaped, value.ToPart21.Trim('\''));
            Assert.AreEqual(expected, unescaped);
        }

        [TestMethod]
        [ExpectedException(typeof(XbimP21EofException))]
        public void Unescape_16BitHex_NotFollowedByHexChar_ThrowsEof()
        {
            decoder.Unescape(@"\X2\");
        }

        [TestMethod]
        [ExpectedException(typeof(XbimP21InvalidCharacterException))]
        public void Unescape_16BitHex_InvalidHex_ThrowsInvalidCharacter()
        {
            decoder.Unescape(@"\X2\00G0\X0\");
        }

        [TestMethod]
        [ExpectedException(typeof(XbimP21EofException))]
        public void Unescape_16BitHex_NotCompleted_ThrowsInvalidCharacter()
        {
            // the abcd sequence is not a termination, so it gets parsed, and would pass,
            // but before that the system checks that there is space for its termination.
            decoder.Unescape(@"\X2\00F0abcd");
        }

        [TestMethod]
        public void Unescape_16BitHex()
        {
            const string escaped = @"\X2\00C4\X0\";
            const string expected = "Ä";
            var value = new IfcText(escaped);
            var unescaped = decoder.Unescape(value);
            // Assert.AreEqual(escaped, value.ToPart21.Trim('\''));
            Assert.AreEqual(expected, unescaped);
        }

        [TestMethod]
        public void MiscCases()
        {
            Unescape_MiscStrings("NÆRING", @"N\X2\00C6\X0\RING");
            Unescape_MiscStrings("Kjøkken/Stue", @"Kj\X2\00F8\X0\kken/Stue");
            Unescape_MiscStrings("Kjøøkken/Stue", @"Kj\X2\00F800F8\X0\kken/Stue");
            Unescape_MiscStrings(@"\MÆØÅ\Tæøå\Z\0", @"\M\X\C6\S\X\X\C5\T\X2\00E6\X0\\S\x\X2\00E5\X0\\Z\0");
            Unescape_MiscStrings(@"ъет NÆÆRING is  'apostrophe' 平仮名 then 4 bytes: 𠜎 𠜱𠝹 \\OtherChars", @"\PE\\S\j\S\U\S\b N\X2\00C600C6\X0\RING is  ''apostrophe'' \X2\5E734EEE540D\X0\ then 4 bytes: \X4\0002070E\X0\ \X4\0002073100020779\X0\ \\\\OtherChars");
        }

        public void Unescape_MiscStrings(string expected, string input)
        {
            string unescaped = decoder.Unescape(input);
            Assert.AreEqual(expected, unescaped);
        }
    }
}
