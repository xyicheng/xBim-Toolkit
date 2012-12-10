using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.UtilityResource;
using Xbim.XbimExtensions;
using Xbim.Ifc.ActorResource;
using Xbim.Ifc.MeasureResource;

namespace Xbim.COBie.Serialisers.XbimSerialiser
{
    class COBieXBimEditScope : IDisposable
    {
        private IfcOwnerHistory ifcOwnerHistory {  get;  set; }
        private IModel Model {  get;  set; }

        public COBieXBimEditScope(IModel model, IfcOwnerHistory owner)
        {
            Model = model;

            ifcOwnerHistory = Model.OwnerHistoryAddObject;
            Model.SetCurrentCOBieOwner(owner);
            
        }


        public void Dispose()
        {
            Model.SetCurrentCOBieOwner(ifcOwnerHistory);
        }
    }
}
