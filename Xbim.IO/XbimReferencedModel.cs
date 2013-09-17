using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.ExternalReferenceResource;

namespace Xbim.IO
{
    /// <summary>
    /// A model that is referenced by  other XbimModels
    /// </summary>
    public class XbimReferencedModel
    {
        public IfcDocumentInformation DocumentInformation;
        public XbimModel Model;
        public XbimReferencedModel(IfcDocumentInformation documentInformation,   XbimModel model)
        {
            DocumentInformation = documentInformation;
            Model = model;
        }

        /// <summary>
        /// Returns the identifier for this reference within the scope of the referencing model
        /// </summary>
        public string Identifier
        {
            get
            {
                return DocumentInformation.DocumentId;
            }            
        }
        public string Owner
        {
            get
            {
                return DocumentInformation.DocumentOwner.ToString();
            }
        }
        public string Name
        {
            get
            {
                return DocumentInformation.Name;
            }
        }

        internal void Dispose()
        {
            Model.Dispose();
        }
    }
}
