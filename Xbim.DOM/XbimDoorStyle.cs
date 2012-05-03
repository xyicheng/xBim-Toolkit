using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.SharedBldgElements;
using Xbim.DOM.PropertiesQuantities;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.Extensions;

namespace Xbim.DOM
{
    public class XbimDoorStyle : XbimBuildingElementType
    {
        #region constructors
        //overloaded internal constructors:
        internal XbimDoorStyle(XbimDocument document, string name) 
            : base(document)
        {
            BaseInit(name);
            IfcDoorStyle.ConstructionType = IfcDoorStyleConstructionEnum.NOTDEFINED;
            IfcDoorStyle.OperationType = IfcDoorStyleOperationEnum.NOTDEFINED;
        }

        internal XbimDoorStyle(XbimDocument document, string name, string description, XbimDoorStyleConstructionEnum construction, XbimDoorStyleOperationEnum operation)
            : base(document)
        {
            BaseInit(name);

            IfcDoorStyle.Description = description;
            IfcDoorStyle.ConstructionType = construction.IfcDoorStyleConstructionEnum();
            IfcDoorStyle.OperationType = operation.IfcDoorStyleOperationEnum();
        }

        internal XbimDoorStyle(XbimDocument document, IfcDoorStyle doorStyle)
            : base(document)
        {
            _ifcTypeProduct = doorStyle;
        }

        private void BaseInit(string name)
        {
            IfcDoorStyle = _document.Model.New<IfcDoorStyle>();
            IfcDoorStyle.Name = name;
            _document.DoorStyles.Add(this);
        }
        #endregion

        #region helpers
        private IfcDoorStyle IfcDoorStyle
        {
            get { return this._ifcTypeProduct as IfcDoorStyle; }
            set { _ifcTypeProduct = value; }
        }
        #endregion

        //public NRMDoorStyleQuantities NRMQuantities { get { return new NRMDoorStyleQuantities(this); } }

        /// <summary>
        /// Direct access to the lining properties. Values could be handled simply as "double"
        /// </summary>
        public IfcDoorLiningProperties LiningProperties
        {
            get
            {
                PropertySetDefinitionSet propertySets = IfcDoorStyle.HasPropertySets;
                if (IfcDoorStyle.HasPropertySets == null)
                {
                    IfcDoorStyle.CreateHasPropertySets();
                    IfcDoorLiningProperties prop = Document.Model.New<IfcDoorLiningProperties>();
                    IfcDoorStyle.HasPropertySets.Add_Reversible(prop);
                    return prop;
                }
                else
                {
                    foreach (IfcPropertySetDefinition pSetDef in propertySets)
                    {
                        if (pSetDef is IfcDoorLiningProperties) return pSetDef as IfcDoorLiningProperties;
                    }
                    IfcDoorLiningProperties prop = Document.Model.New<IfcDoorLiningProperties>();
                    IfcDoorStyle.HasPropertySets.Add_Reversible(prop);
                    return prop;
                }
                
            }
        }


        /// <summary>
        /// Direct access to PanelProperties.
        /// </summary>
        public IfcDoorPanelProperties PanelProperties
        {
            get
            {
                PropertySetDefinitionSet propertySets = IfcDoorStyle.HasPropertySets;
                if (IfcDoorStyle.HasPropertySets == null)
                {
                    IfcDoorStyle.CreateHasPropertySets();
                    IfcDoorPanelProperties prop = Document.Model.New<IfcDoorPanelProperties>();
                    IfcDoorStyle.HasPropertySets.Add_Reversible(prop);
                    return prop;
                }
                else
                {
                    foreach (IfcPropertySetDefinition pSetDef in propertySets)
                    {
                        if (pSetDef is IfcDoorPanelProperties) return pSetDef as IfcDoorPanelProperties;
                    }
                    IfcDoorPanelProperties prop = Document.Model.New<IfcDoorPanelProperties>();
                    IfcDoorStyle.HasPropertySets.Add_Reversible(prop);
                    return prop;
                }

            }
        }

        public bool ParameterTakesPrecedence
        {
            get { return IfcDoorStyle.ParameterTakesPrecedence; }
            set { IfcDoorStyle.ParameterTakesPrecedence = value; }
        }

        public XbimDoorStyleConstructionEnum ConstructionType { get { return IfcDoorStyle.ConstructionType.XbimDoorStyleConstructionEnum(); } }
        public XbimDoorStyleOperationEnum OperationType { get { return IfcDoorStyle.OperationType.XbimDoorStyleOperationEnum(); } }

    }
}
