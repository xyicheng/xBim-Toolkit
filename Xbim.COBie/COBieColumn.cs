using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Xbim.COBie.COBieExtensions;

namespace Xbim.COBie
{
    public class COBieColumnCollection : KeyedCollection<string, COBieColumn>
    {
        protected override string GetKeyForItem(COBieColumn item)
        {
            return item.ColumnName;
        }
    }

    public class COBieColumn
    {
        public bool IsPrimaryKey { get; set; }
        public bool IsForeignKey { get; set; }
        public string ColumnName { get; set; }
        public int ColumnLength { get; set; }
        public COBieAllowedType AllowedType { get; set; }
        public COBieKeyType KeyType { get; set; }
        //public int Order { get; set;}
        public COBieColumn()
        {
        }

        public COBieColumn(string columnName, bool isPrimaryKey = false, bool isForeignKey = false)
        {
            IsPrimaryKey = isPrimaryKey;
            IsForeignKey = isForeignKey;
            ColumnName = columnName;
            //Order = order;
        }

        public COBieColumn(string columnName, int columnLength, COBieAllowedType allowedType, COBieKeyType keyType)
        {
            ColumnName = columnName;
            ColumnLength = columnLength;
            AllowedType = allowedType;
            KeyType = keyType;
            //Order = order;
        }

    }
}
