using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.ProductExtension;
using Xbim.XbimExtensions;
using Xbim.Ifc.Extensions;
namespace Xbim.COBieExtensions
{
    public class COBieP01Registration
    {
        IModel _model;

        public COBieP01Registration(IModel model)
        {
            _model = model;
        }
        /// <summary>
        /// Returns the first Building in the model
        /// </summary>
        public COBieFacility Facility
        {
            get
            {
                return new COBieFacility( _model);
            }
        }

        /// <summary>
        /// All the floors in the building
        /// </summary>
        public IEnumerable<COBieFloor> Floors
        {
            get
            {
                
                int level = 0;
                List<IfcBuildingStorey> storeys = _model.InstancesOfType<IfcBuildingStorey>().ToList();
                storeys.Sort(BuildingStoreyExtensions.CompareStoreysByElevation);
                foreach (IfcBuildingStorey storey in storeys)
                {
                    yield return new COBieFloor(storey, level++);
                }
                
            }
        }
    }
}
