#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcFurnitureType.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.ProductExtension;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.SharedFacilitiesElements
{
    [IfcPersistedEntity, Serializable]
    public class IfcFurnitureType : IfcFurnishingElementType
    {
        #region Fields

        private IfcAssemblyPlaceEnum _assemblyPlace;

        #endregion

        [IfcAttribute(10, IfcAttributeState.Mandatory)]
        public IfcAssemblyPlaceEnum AssemblyPlace
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _assemblyPlace;
            }
            set { ModelManager.SetModelValue(this, ref _assemblyPlace, value, v => AssemblyPlace = v, "AssemblyPlace"); }
        }

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                    base.IfcParse(propIndex, value);
                    break;
                case 9:
                    _assemblyPlace =
                        (IfcAssemblyPlaceEnum) Enum.Parse(typeof (IfcAssemblyPlaceEnum), value.EnumVal, true);
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }
    }
}