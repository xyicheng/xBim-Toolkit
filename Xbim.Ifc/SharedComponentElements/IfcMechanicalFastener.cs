using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions.Parser;

namespace Xbim.Ifc.SharedComponentElements
{
     [IfcPersistedEntity, Serializable]
    public class IfcMechanicalFastener : IfcFastener
    {
        #region Fields
         IfcPositiveLengthMeasure? _nominalDiameter;
         IfcPositiveLengthMeasure? _nominalLength;
        #endregion

         #region Ifc Properties
         /// <summary>
         /// The nominal diameter describing the cross-section size of the fastener.
         /// </summary>
         [IfcAttribute(9, IfcAttributeState.Optional) /*, IfcPrimaryIndex*/]
         public IfcPositiveLengthMeasure? NominalDiameter
         {
             get
             {
#if SupportActivation
                 ((IPersistIfcEntity)this).Activate(false);
#endif
                 return _nominalDiameter;
             }
             set { ModelManager.SetModelValue(this, ref _nominalDiameter, value, v => NominalDiameter = v, "NominalDiameter"); }
         }

         /// <summary>
         /// The nominal length describing the longitudinal dimensions of the fastener.
         /// </summary>
         [IfcAttribute(10, IfcAttributeState.Optional) /*, IfcPrimaryIndex*/]
         public IfcPositiveLengthMeasure? NominalLength
         {
             get
             {
#if SupportActivation
                 ((IPersistIfcEntity)this).Activate(false);
#endif
                 return _nominalLength;
             }
             set { ModelManager.SetModelValue(this, ref _nominalLength, value, v => NominalLength = v, "NominalLength"); }
         }
         #endregion

        #region IfcParse

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
                     base.IfcParse(propIndex, value);
                     break;
                 case 8:
                     _nominalDiameter = value.RealVal; break;
                 case 9:
                     _nominalLength = value.RealVal; break;
                 default:
                     throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                       this.GetType().Name.ToUpper()));
             }
         }
        #endregion
    }
}
