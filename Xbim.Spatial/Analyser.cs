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
    public class Analyser: ISpatialFunctions, ISpatialRelations, ISpatialDirections
    {
        private GeometricAnalyser _geometryAnalyser;
        private SemanticAnalyser _semanticAnalyser;
        private MODE _mode;

        public Analyser(IModel model, MODE mode)
        {
            _geometryAnalyser = new GeometricAnalyser();
            _semanticAnalyser = new SemanticAnalyser(model);
            _mode = mode;
        }

        public enum MODE
        {
            SEMANTIC_ONLY,
            GEOMETRY_ONLY,
            SEMANTIC_AND_GEOMETRY
        }

        #region Spatial relations
        public bool? Equals(IfcProduct first, IfcProduct second)
        {
            return Compare(first, second, _semanticAnalyser.Equals, _geometryAnalyser.Equals);
        }

        public bool? Disjoint(IfcProduct first, IfcProduct second)
        {
            return Compare(first, second, _semanticAnalyser.Disjoint, _geometryAnalyser.Disjoint);
        }

        public bool? Intersects(IfcProduct first, IfcProduct second)
        {
            return Compare(first, second, _semanticAnalyser.Intersects, _geometryAnalyser.Intersects);
        }

        public bool? Touches(IfcProduct first, IfcProduct second)
        {
            return Compare(first, second, _semanticAnalyser.Touches, _geometryAnalyser.Touches);
        }

        public bool? Crosses(IfcProduct first, IfcProduct second)
        {
            return Compare(first, second, _semanticAnalyser.Crosses, _geometryAnalyser.Crosses);
        }

        public bool? Within(IfcProduct first, IfcProduct second)
        {
            return Compare(first, second, _semanticAnalyser.Within, _geometryAnalyser.Within);
        }

        public bool? Contains(IfcProduct first, IfcProduct second)
        {
            return Compare(first, second, _semanticAnalyser.Contains, _geometryAnalyser.Contains);
        }

        public bool? Overlaps(IfcProduct first, IfcProduct second)
        {
            return Compare(first, second, _semanticAnalyser.Overlaps, _geometryAnalyser.Overlaps);
        }

        public bool? Relate(IfcProduct first, IfcProduct second)
        {
            return Compare(first, second, _semanticAnalyser.Relate, _geometryAnalyser.Relate);
        }

        private bool? Compare(IfcProduct first, IfcProduct second, Func<IfcProduct, IfcProduct, bool?> semComparer, Func<IfcProduct, IfcProduct, bool?> geomComparer)
        {
            switch (_mode)
            {
                case MODE.SEMANTIC_ONLY:
                    return semComparer(first, second);
                case MODE.GEOMETRY_ONLY:
                    return geomComparer(first, second);
                case MODE.SEMANTIC_AND_GEOMETRY:
                    if (semComparer(first, second) ?? false) return true;
                    if (geomComparer(first, second) ?? false) return true;
                    return null;
                default:
                    return null;
            }
        }
        #endregion

        #region Spatial directions
        public bool? NorthOf(IfcProduct first, IfcProduct second)
        {
            return _geometryAnalyser.NorthOf(first, second);
        }

        public bool? SouthOf(IfcProduct first, IfcProduct second)
        {
            return _geometryAnalyser.SouthOf(first, second);
        }

        public bool? WestOf(IfcProduct first, IfcProduct second)
        {
            return _geometryAnalyser.WestOf(first, second);
        }

        public bool? EastOf(IfcProduct first, IfcProduct second)
        {
            return _geometryAnalyser.EastOf(first, second);
        }

        public bool? Above(IfcProduct first, IfcProduct second)
        {
            return _geometryAnalyser.Above(first, second);
        }

        public bool? Below(IfcProduct first, IfcProduct second)
        {
            return _geometryAnalyser.Below(first, second);
        }

        #endregion

        #region Spatial functions
        public double? Distance(IfcProduct first, IfcProduct second)
        {
            return _geometryAnalyser.Distance(first, second);
        }

        public IfcProduct Buffer(IfcProduct product)
        {
            return _geometryAnalyser.Buffer(product);
        }

        public IfcProduct ConvexHull(IfcProduct product)
        {
            return _geometryAnalyser.ConvexHull(product);
        }

        public IfcProduct Intersection(IfcProduct first, IfcProduct second)
        {
            return _geometryAnalyser.Intersection(first, second);
        }

        public IfcProduct Union(IfcProduct first, IfcProduct second)
        {
            return _geometryAnalyser.Union(first, second);
        }


        public IfcProduct Difference(IfcProduct first, IfcProduct second)
        {
            return _geometryAnalyser.Difference(first, second);
        }

        public IfcProduct SymDifference(IfcProduct first, IfcProduct second)
        {
            return _geometryAnalyser.SymDifference(first, second);
        }
        #endregion
    }
}
