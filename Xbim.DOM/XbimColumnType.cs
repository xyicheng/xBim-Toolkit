using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM
{
    public class XbimColumnType : XbimBuildingElementType
    {
         #region constructors
        //overloaded internal constructors:
        internal XbimColumnType(XbimDocument document, string name) 
            : base(document)
        {
            BaseInit(name);
            IfcColumnType.PredefinedType = IfcColumnTypeEnum.NOTDEFINED;
        }

        internal XbimColumnType(XbimDocument document, string name, string description, XbimColumnTypeEnum type)
            : base(document)
        {
            BaseInit(name);
            IfcColumnType.Description = description;
            IfcColumnType.PredefinedType = type.IfcColumnTypeEnum();
        }

        internal XbimColumnType(XbimDocument document, IfcColumnType columType)
            : base(document)
        {
            IfcColumnType = columType;
        }

        private void BaseInit(string name)
        {
            IfcColumnType = _document.Model.New<IfcColumnType>();
            IfcColumnType.Name = name;
            _document.ColumnTypes.Add(this);
        }
        #endregion

        #region helpers
        private IfcColumnType IfcColumnType
        {
            get { return _ifcTypeProduct as IfcColumnType; }
            set { _ifcTypeProduct = value; }
        }
        #endregion

        //public NRMColumnTypeQuantities NRMQuantities { get { return new NRMColumnTypeQuantities(this); } }

        public XbimColumnTypeEnum PredefinedType { get { return IfcColumnType.PredefinedType.XbimColumnTypeEnum(); } }
    }
}
