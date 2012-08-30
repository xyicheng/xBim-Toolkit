using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Xbim.COBie
{
    
    public class COBieColumn 
    {
        public bool IsPrimaryKey { get; set; }
        public bool IsForeignKey { get; set; }
        public string ColumnName { get; set; }
        public int ColumnLength { get; set; }
        public COBieAllowedType AllowedType { get; set; }
        public COBieKeyType KeyType { get; set; }
        public List<string> Aliases { get; set; }


        public COBieColumn(string columnName, int columnLength, COBieAllowedType allowedType, COBieKeyType keyType)
            : this(columnName, columnLength, allowedType, keyType, new List<string>())
        {
        }

        public COBieColumn(string columnName, int columnLength, COBieAllowedType allowedType, COBieKeyType keyType, List<string> aliases)
        {
            ColumnName = columnName;
            ColumnLength = columnLength;
            AllowedType = allowedType;
            KeyType = keyType;
            Aliases = aliases;
        }

        /// <summary>
        /// Determines if this COBieColumn is a match for the supplied column name, using a basic heuristic match
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public bool IsMatch(string columnName)
        {
            if (IsMatchImpl(columnName))
                return true;

            if(IsMatchImpl(StripPunctuation(columnName)))
                return true;

            string singular = MakeSingular(columnName);
            if (singular != columnName)
            {
                // call back into ourself if we are an obvious plural
                return IsMatch(singular);
            }

            return false;
        }

        
        private bool IsMatchImpl(string sourceName)
        {
            // Straight match, ignoring case
            if (String.Compare(sourceName, ColumnName, true) == 0)
            {
                return true;
            }
            // Check against known aliases. e.g. covers languages differences such as Colour/Color
            if (Aliases != null)
            {
                foreach (string alias in Aliases)
                {
                    if (String.Compare(sourceName, alias, true) == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Removes all punctuation and white space
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        private string StripPunctuation(string columnName)
        {
            var r = from ch in columnName
                   where (!Char.IsPunctuation(ch) && !Char.IsWhiteSpace(ch))
                   select ch;

            return new string(r.ToArray());
        }

        
        private string MakeSingular(string columnName)
        {
            // TODO: make less naive!
            if (columnName.EndsWith("s"))
                return (columnName.TrimEnd('s'));
            else
                return columnName;
        }

    }
}
