using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.SharedBldgElements;
using Xbim.DOM.PropertiesQuantities;
using Xbim.Ifc.Kernel;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.DOM
{
    public class XbimWindowStyle : XbimBuildingElementType
    {
        #region constructors
        //overloaded internal constructors:
        internal XbimWindowStyle(XbimDocument document, string name) 
            : base(document)
        {
            BaseInit(name);
            IfcWindowStyle.ConstructionType = IfcWindowStyleConstructionEnum.NOTDEFINED;
            IfcWindowStyle.OperationType = IfcWindowStyleOperationEnum.NOTDEFINED;
        }

        internal XbimWindowStyle(XbimDocument document, string name, string description, XbimWindowStyleConstructionEnum construction, XbimWindowStyleOperationEnum operation)
            : base(document)
        {
            BaseInit(name);
            IfcWindowStyle.Description = description;
            IfcWindowStyle.ConstructionType = construction.IfcWindowStyleConstructionEnum();
            IfcWindowStyle.OperationType = operation.IfcWindowStyleOperationEnum();
        }

        internal XbimWindowStyle(XbimDocument document, IfcWindowStyle windowStyle)
            : base(document)
        {
            _ifcTypeProduct = windowStyle;
        }

        private void BaseInit(string name)
        {
            IfcWindowStyle = _document.Model.New<IfcWindowStyle>();
            IfcWindowStyle.Name = name;
            _document.WindowStyles.Add(this);
        }
        #endregion

        #region helpers
        private IfcWindowStyle IfcWindowStyle
        {
            get { return this._ifcTypeProduct as IfcWindowStyle; }
            set { _ifcTypeProduct = value; }
        }
        #endregion

        //public NRMWindowStyleQuantities NRMQuantities { get { return new NRMWindowStyleQuantities(this); } }

        public XbimWindowStyleConstructionEnum ConstructionType { 
            get { return IfcWindowStyle.ConstructionType.XbimWindowStyleConstructionEnum(); }
            set
            {
                EnumConvertor<XbimWindowStyleConstructionEnum, IfcWindowStyleConstructionEnum> conv = new EnumConvertor<XbimWindowStyleConstructionEnum, IfcWindowStyleConstructionEnum>();
                IfcWindowStyleConstructionEnum constr = conv.Conversion(value);
                IfcWindowStyle.ConstructionType = constr;
            }
        }
        public XbimWindowStyleOperationEnum OperationType { 
            get { return IfcWindowStyle.OperationType.XbimWindowStyleOperationEnum(); }
            set 
            {
                EnumConvertor<XbimWindowStyleOperationEnum, IfcWindowStyleOperationEnum> conv = new EnumConvertor<XbimWindowStyleOperationEnum, IfcWindowStyleOperationEnum>();
                IfcWindowStyleOperationEnum oper = conv.Conversion(value);
                IfcWindowStyle.OperationType = oper;
            }
        }

        /// <summary>
        /// Direct access to the lining properties. Values could be handled simply as "double"
        /// </summary>
        public IfcWindowLiningProperties LiningProperties
        {
            get
            {
                PropertySetDefinitionSet propertySets = IfcWindowStyle.HasPropertySets;
                
                //create new property set if no one exists
                if (propertySets == null)
                {
                    IfcWindowStyle.CreateHasPropertySets();
                }

                //try to find existing property set
                foreach (IfcPropertySetDefinition pSetDef in propertySets)
                {
                    if (pSetDef is IfcWindowLiningProperties) return pSetDef as IfcWindowLiningProperties;
                }

                //if no property set has been returned new one is created
                IfcWindowLiningProperties prop = Document.Model.New<IfcWindowLiningProperties>();
                IfcWindowStyle.HasPropertySets.Add_Reversible(prop);
                return prop;
            }
        }


        /// <summary>
        /// Direct access to PanelProperties.
        /// </summary>
        public XbimWindowPanelProperties PanelProperties
        {
            get
            {
                return new XbimWindowPanelProperties(this.IfcWindowStyle);
            }
        }
    }

    public class XbimWindowPanelProperties
    {
        private IfcWindowStyle _style;
        private IfcWindowStyle Style { get { return _style; } }

        internal XbimWindowPanelProperties(IfcWindowStyle style)
        {
            _style = style;
        }

        public IfcWindowPanelProperties First { get { return GetPanelProperties(0); } }

        public IfcWindowPanelProperties Second { get { return GetPanelProperties(1); } }

        public IfcWindowPanelProperties Third { get { return GetPanelProperties(2); } }

        public int Count { get { return All.Count(); } }

        public bool Single { get { return Count == 1; } }

        /// <summary>
        /// Returns panel properties of specified index or creates new one.
        /// </summary>
        /// <param name="index">Index of the panel properties</param>
        private IfcWindowPanelProperties GetPanelProperties(int index)
        {
            int existCount = All.Count();
            if ( existCount < index + 1)
            {
                CreatePanelProperties(index + 1 - existCount);
            }

            return All.ToList()[index];
        }

        private void CreatePanelProperties(int count)
        {
            for (int i = 0; i < count; i++)
            {
                IfcWindowPanelProperties prop = (Style as IPersistIfcEntity).ModelOf.New<IfcWindowPanelProperties>();
                Style.HasPropertySets.Add_Reversible(prop);
            }
        }

        private PropertySetDefinitionSet PropertySets
        { 
            get
            {
                PropertySetDefinitionSet propertySets = Style.HasPropertySets;

                //create new property set if no one exists
                if (propertySets == null)
                {
                    Style.CreateHasPropertySets();
                }

                return Style.HasPropertySets;
            }
        }

        public IEnumerable<IfcWindowPanelProperties> All
        {
            get
            {
                foreach (IfcPropertySetDefinition pSetDef in PropertySets)
                {
                    if (pSetDef is IfcWindowPanelProperties) yield return pSetDef as IfcWindowPanelProperties;
                }
            }
        }
    }
}
