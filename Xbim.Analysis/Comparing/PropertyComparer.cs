using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.PropertyResource;

namespace Xbim.Analysis.Comparing
{
    public class PropertyComparer : IModelComparerII
    {
        #region Model Comparer implementation
        public string Name
        {
            get { return "Xbim Property Comparer"; }
        }

        public string Description
        {
            get { return "Compares objects based on their simple properties."; }
        }

        public ComparisonType ComparisonType
        {
            get { return Comparing.ComparisonType.PROPERTIES; }
        }

        private int _weight = 60;
        public int Weight
        {
            get
            {
                return _weight;
            }
            set
            {
                _weight = value;
            }
        }

        private List<IfcRoot> _processed = new List<IfcRoot>();
        public ComparisonResult Compare<T>(T baseline, IO.XbimModel revisedModel) where T : Ifc2x3.Kernel.IfcRoot
        {
            var baseModel = baseline.ModelOf;
            if (baseModel == revisedModel)
                throw new ArgumentException("Baseline should be from the different model than revised model.");

            var baseObj = baseline as IfcObject;
            var baseTypeObj = baseline as IfcTypeObject;
            var baseMaterial = baseline as IfcMaterial;
            var result = new ComparisonResult(baseline, this);
            if (baseObj != null)
            {
                var pSets = baseObj.GetAllPropertySets();
                if (pSets.Count == 0) //return null if there is nothing to compare
                    return null;
                var candidates = revisedModel.Instances.Where<IfcObject>(o => CompareObjects(pSets, o)).ToList();
                result.Candidates.AddRange(candidates);
                _processed.AddRange(candidates);
            }
            else if (baseTypeObj != null)
            {
                var pSets = baseTypeObj.GetAllPropertySets();
                if (pSets.Count == 0) //return null if there is nothing to compare
                    return null;
                var candidates = revisedModel.Instances.Where<IfcTypeObject>(o => ComparePsetsSet(pSets, o.GetAllPropertySets()));
                result.Candidates.AddRange(candidates);
                _processed.AddRange(candidates);
            }
            else if (baseMaterial != null)
            {
                return null; //material is not a root object so it is not supported in a comparison
            }
            return result; ;
        }

        public ComparisonResult GetResidualsFromRevision<T>(IO.XbimModel revisedModel) where T : Ifc2x3.Kernel.IfcRoot
        {
            var result = new ComparisonResult(null, this);
            result.Candidates.AddRange(revisedModel.Instances.Where<T>(r => !_processed.Contains(r)));
            return result;
        }

        public IEnumerable<ComparisonResult> Compare<T>(IO.XbimModel baseline, IO.XbimModel revised) where T : Ifc2x3.Kernel.IfcRoot
        {
            foreach (var b in baseline.Instances.OfType<T>())
            {
                yield return Compare<T>(b, revised);
            }
            yield return GetResidualsFromRevision<T>(revised);
        }
        #endregion

        #region Helpers

        private bool CompareObjects(IEnumerable<IfcPropertySet> baseline, IfcObject obj)
        {
            return ComparePsetsSet(baseline, obj.GetAllPropertySets());
        }

        private bool ComparePsetsSet(IEnumerable<IfcPropertySet> baseline, IEnumerable<IfcPropertySet> revision)
        {
            if (baseline.Count() != revision.Count())
                return false;
            var baseList = baseline.ToList();
            var revisionList = revision.ToList();
            baseList.Sort(psetsOrder);
            revisionList.Sort(psetsOrder);

            for (int i = 0; i < baseList.Count; i++)
                if (!ComparePSets(baseList[i], revisionList[i]))
                    return false;
            return true;
        }

        private static int psetsOrder(IfcPropertySet x, IfcPropertySet y)
        {
            if (x.Name == null && y.Name != null) return -1;
            if (x.Name != null && y.Name == null) return 1;
            if (x.Name == null && y.Name == null) return 0;

            return x.Name.ToString().CompareTo(y.Name.ToString());
        }

        private bool ComparePSets(IfcPropertySet baseline, IfcPropertySet revision)
        {
            if (baseline.Name != revision.Name) 
                return false;
            if (baseline.HasProperties.Count != revision.HasProperties.Count)
                return false;
            foreach (var prop in baseline.HasProperties)
                if (!HasEquivalent(prop, revision))
                    return false;
            return true;
        
        }

        private bool HasEquivalent(IfcProperty property, IfcPropertySet revisionPset)
        {
            //check if property with the same name even exist
            var candidate = revisionPset.HasProperties.Where(p => p.Name == property.Name).FirstOrDefault();
            if (candidate == null)
                return false;

            //check actual value
            switch (property.GetType().Name)
            {
                case "IfcPropertySingleValue":
                    var single = candidate as IfcPropertySingleValue;
                    if (single == null) return false;
                    var revVal = single.NominalValue;
                    var baseVal = ((IfcPropertySingleValue)(property)).NominalValue;
                    var revStr = revVal == null ? "" : revVal.ToString();
                    var baseStr = baseVal == null ? "" : baseVal.ToString();
                    if (baseStr != revStr)
                        return false;
                    break;
                case "IfcPropertyEnumeratedValue":
                    var enumerated = candidate as IfcPropertyEnumeratedValue;
                    if (enumerated == null) return false;
                    var baseEnum = property as IfcPropertyEnumeratedValue;
                    if (baseEnum.EnumerationValues.Count != enumerated.EnumerationValues.Count)
                        return false;
                    foreach (var e in baseEnum.EnumerationValues)
                        if (!enumerated.EnumerationValues.Contains(e))
                            return false;
                    break;
                case "IfcPropertyBoundedValue":
                    var bounded = candidate as IfcPropertyBoundedValue;
                    if (bounded == null) return false;
                    var baseBounded = property as IfcPropertyBoundedValue;
                    if (bounded.LowerBoundValue != baseBounded.LowerBoundValue)
                        return false;
                    if (baseBounded.UpperBoundValue != bounded.UpperBoundValue)
                        return false;
                    break;
                case "IfcPropertyTableValue":
                    var table = candidate as IfcPropertyTableValue;
                    if (table == null) return false;
                    var baseTable = property as IfcPropertyTableValue;
                    if (baseTable.DefiningValues.Count != table.DefiningValues.Count)
                        return false;
                    //check all table items
                    foreach (var item in baseTable.DefiningValues)
                    {
                        var revDefiningValue = table.DefiningValues.Where(v => v == item).FirstOrDefault();
                        if (revDefiningValue == null) return false;
                        var revIndex = table.DefiningValues.IndexOf(revDefiningValue);
                        var baseIndex = baseTable.DefiningValues.IndexOf(item);
                        if (table.DefinedValues[revIndex] != baseTable.DefinedValues[baseIndex])
                            return false;
                    }
                    break;
                case "IfcPropertyReferenceValue":
                    var reference = candidate as IfcPropertyReferenceValue;
                    if (reference == null) return false;
                    var baseRef = property as IfcPropertyReferenceValue;
                    if (reference.UsageName != baseRef.UsageName)
                        return false;
                    if (reference.PropertyReference.GetType() != baseRef.PropertyReference.GetType())
                        return false;
                    //should go deeper but it would be too complicated for now
                    break;
                case "IfcPropertyListValue":
                    var list = candidate as IfcPropertyListValue;
                    if (list == null) return false;
                    var baseList = property as IfcPropertyListValue;
                    if (baseList.ListValues.Count != list.ListValues.Count)
                        return false;
                    foreach (var item in baseList.ListValues)
                        if (!list.ListValues.Contains(item))
                            return false;
                    break;
                default:
                    break;
            }
            return true;
        }

        #endregion
    }
}
