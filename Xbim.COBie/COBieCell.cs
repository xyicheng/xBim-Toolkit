
using System.Text.RegularExpressions;
namespace Xbim.COBie
{
    public class COBieCell
    {
        public COBieCell()
        {

        }

        public COBieCell(string cellValue)
        {
            CellValue = cellValue;
        }

        public string CellValue { get; set; }
        public COBieColumn CobieCol { get; set; }
        public COBieAttributeState COBieState { get; set; }



        public static Regex RegExAlphaNumeric = new Regex("^[a-zA-Z0-9]*$");
        public static Regex RegExEmail = new Regex("[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?");
    }
}
