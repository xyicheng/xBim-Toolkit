using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.DOM.ExportHelpers
{
    public class ExportMaterialHelper
    {
        private XbimDocumentSource _source;
        private XbimDocumentSource Source { get { return _source; } }
        private IBimTarget Target {get {return Source.Target;}}
        
        public ExportMaterialHelper(XbimDocumentSource source)
        {
            _source = source;
        }

        public IBimMaterial Convert (XbimMaterial material)
        {
            string matName = material.Name;
            IBimMaterial tMaterial = Target.GetMaterial(matName);
            if (tMaterial != null) return null;

            string lookUpMaterial = material.SingleProperties.GetProperty_string("nbl_MaterialCommon", "RevitMaterial");

            try
            {
                if (lookUpMaterial == null)
                {
                    tMaterial = Target.NewMaterial(matName);
                }
                else
                {
                    tMaterial = Target.NewMaterial(matName, lookUpMaterial);
                }
            }
            catch (Exception e)
            {
                
                throw new Exception("Error while creating new material '" + matName + "':" + e.Message);
            }
            
            

            Source.PropertiesHelper.Convert(tMaterial.SingleProperties, material.SingleProperties);
            Source.AddConvertedObject(material);

            return tMaterial;
        }
    }
}
