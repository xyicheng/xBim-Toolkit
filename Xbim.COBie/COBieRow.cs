using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Xbim.COBie
{
    abstract public class COBieRow
    {
        static protected Dictionary<int, COBieColumn> _columns; 
        protected static PropertyInfo[] Properties;
        static public Dictionary<int, COBieColumn> Columns
        {
            get
            {
                return _columns;
            }
        }
      
       
    }
}
