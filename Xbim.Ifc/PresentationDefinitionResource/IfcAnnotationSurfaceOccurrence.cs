using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.GeometricModelResource;
using Xbim.XbimExtensions;

namespace Xbim.Ifc.PresentationDefinitionResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcAnnotationSurfaceOccurrence : IfcAnnotationOccurrence, IfcDraughtingCalloutElement
    {
        #region Fields
        
        #endregion

        #region Ifcroperties
        
        #endregion

        #region IfcParse
        
        #endregion

        #region Ifc Schema Validation Methods

        public override string WhereRule()
        {
            string baseErr = base.WhereRule();
            if (Item != null && !(Item is IfcSurface || Item is IfcFaceBasedSurfaceModel || Item is IfcShellBasedSurfaceModel || Item is IfcSolidModel))
                baseErr +=
                    "WR31 AnnotationSurfaceOccurrence : 	The Item that is styled by an IfcAnnotationSurfaceOccurrence relation shall be (if provided) a subtype of IfcSurface, IfcSolidModel, IfcShellBasedSurfaceModel, IfcFaceBasedSurfaceModel. ";
            return baseErr;
        }

        #endregion

    }
}
