using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.GeometricConstraintResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.Extensions;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Spatial
{
    public class Analyser: ISpatialFunctionsAnalyser, ISpatialRelationsAnalyser, ISpatialDirectionsAnalyser
    {
        private GeometricAnalyser _geometryAnalyser;
        private SemanticAnalyser _semanticAnalyser;

        public Analyser(IModel model)
        {
            _geometryAnalyser = new GeometricAnalyser();
            _semanticAnalyser = new SemanticAnalyser(model);
        }


        #region Spatial relations
        public bool Equals(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public bool Disjoint(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public bool Intersects(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public bool Touches(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public bool Crosses(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public bool Within(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public bool Contains(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public bool Overlaps(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public bool Relate(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }
        #endregion

        #region Spatial directions
        public bool NorthOf(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public bool SouthOf(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public bool WestOf(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public bool EastOf(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public bool Above(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public bool Bellow(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region Spatial functions
        public double Distance(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public GeometryStruct Buffer(IfcProduct product)
        {
            throw new System.NotImplementedException();
        }

        public GeometryStruct ConvexHull(IfcProduct product)
        {
            throw new System.NotImplementedException();
        }

        public GeometryStruct Intersection(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public GeometryStruct Union(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }


        public GeometryStruct Difference(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }

        public GeometryStruct SymDifference(IfcProduct first, IfcProduct second)
        {
            throw new System.NotImplementedException();
        }
        #endregion
    }

    public struct GeometryStruct
    {
        public IfcObjectPlacement placement;
        public IfcProductRepresentation representation;
    }
}
