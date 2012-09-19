using System.Text.RegularExpressions;
using System;

namespace Xbim.COBie
{
    public class COBieCell
    {
       
        public COBieCell(string cellValue)
        {
            CellValue = cellValue;
        }

        public string CellValue { get; set; }
        public COBieColumn CobieCol { get; set; }
        public COBieAttributeState COBieState { get; set; }

        public bool IsAlphaNumeric()
        {
            return RegExAlphaNumeric.IsMatch(this.CellValue);
        }

        public bool IsEmailAddress()
        {
            bool isEmail = false;
            try
            {
                System.Net.Mail.MailAddress address = new System.Net.Mail.MailAddress(this.CellValue);
                isEmail = true;
            }
            catch (FormatException)
            {
                // Do nothing
            }
            return isEmail;
        }

        static Regex RegExAlphaNumeric = new Regex(@"\w");
        
    }
}
