#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    PsetSpaceCommon.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

namespace Xbim.Ifc2x3.PropertySetDefinitions
{
    public static class PsetSpaceCommon
    {
        //static string pSetName = "Pset_SpaceCommon";

        //public static IfcIdentifier Reference(this Space space)
        //{
        //    return GetPropertySingleValue<IfcIdentifier>(space, pSetName, "Reference"); 
        //}

        //public static void Reference(this Space space, IfcIdentifier value)
        //{
        //    SetPropertySingleValue<IfcIdentifier>(space, value, pSetName, "Reference", null);        
        //}

        //public static IfcLabel Category(this Space space)
        //{
        //    return GetPropertySingleValue<IfcLabel>(space, pSetName, "Category");
        //}

        //public static void Category(this Space space, IfcLabel value)
        //{
        //    SetPropertySingleValue<IfcLabel>(space, value, pSetName, "Category", null);
        //}

        //public static IfcLabel FloorCovering(this Space space)
        //{
        //    return GetPropertySingleValue<IfcLabel>(space, pSetName, "FloorCovering");
        //}

        //public static void FloorCovering(this Space space, IfcLabel value)
        //{
        //    SetPropertySingleValue<IfcLabel>(space, value, pSetName, "FloorCovering", null);
        //}

        //public static IfcLabel WallCovering(this Space space)
        //{
        //    return GetPropertySingleValue<IfcLabel>(space, pSetName, "WallCovering");
        //}

        //public static void WallCovering(this Space space, IfcLabel value)
        //{
        //    SetPropertySingleValue<IfcLabel>(space, value, pSetName, "WallCovering", null);
        //}

        //public static IfcLabel CeilingCovering(this Space space)
        //{
        //    return GetPropertySingleValue<IfcLabel>(space, pSetName, "CeilingCovering");
        //}

        //public static void CeilingCovering(this Space space, IfcLabel value)
        //{
        //    SetPropertySingleValue<IfcLabel>(space, value, pSetName, "CeilingCovering", null);
        //}

        //public static IfcLabel SkirtingBoard(this Space space)
        //{
        //    return GetPropertySingleValue<IfcLabel>(space, pSetName, "SkirtingBoard");
        //}

        //public static void SkirtingBoard(this Space space, IfcLabel value)
        //{
        //    SetPropertySingleValue<IfcLabel>(space, value, pSetName, "SkirtingBoard", null);
        //}


        //public static AreaMeasure GrossAreaPlanned(this Space space)
        //{
        //    return GetPropertySingleValue<AreaMeasure>(space, pSetName, "GrossAreaPlanned");
        //}

        //public static void GrossAreaPlanned(this Space space, AreaMeasure value)
        //{
        //    SetPropertySingleValue<AreaMeasure>(space, value, pSetName, "GrossAreaPlanned", null);
        //}

        //public static void GrossAreaPlanned(this Space space, AreaMeasure value, Unit unit)
        //{
        //    SetPropertySingleValue<AreaMeasure>(space, value, pSetName, "GrossAreaPlanned", unit);
        //}


        //private static T GetPropertySingleValue<T>(IfcObject obj, string propSetName, string propName)
        //{
        //    IEnumerable<RelDefinesByProperties> rels = obj.IsDefinedByProperties;
        //    RelDefinesByProperties rel = rels.FirstOrDefault<RelDefinesByProperties>(r => r.RelatingPropertyDefinition.Name == propSetName);
        //    PropertySet pset = null;
        //    if (rel != null) pset = rel.RelatingPropertyDefinition as PropertySet;
        //    if (pset == null)
        //    {
        //        return default(T);
        //    }
        //    else
        //    {

        //        PropertySingleValue prop = pset.HasProperties.OfType<PropertySingleValue>().FirstOrDefault<PropertySingleValue>(p => p.Name == propName);
        //        if (prop == null) return default(T);
        //        else return (T)prop.NominalValue;
        //    }
        //}

        //private static void SetPropertySingleValue<T>(IfcObject obj, T value, string propSetName, string propName, Unit unit)
        //{
        //    if (obj.Model == null)
        //        throw new Exception("Properties cannot be added to an Object until it has been added into the model by a valid transaction");
        //    IEnumerable<RelDefinesByProperties> rels = obj.IsDefinedByProperties;
        //    RelDefinesByProperties rel = rels.FirstOrDefault<RelDefinesByProperties>(r => r.RelatingPropertyDefinition.Name == propSetName);
        //    PropertySet pset = null;
        //    if (rel != null) pset = rel.RelatingPropertyDefinition as PropertySet;
        //    if (pset == null)
        //    {
        //        PropertySingleValue prop = new PropertySingleValue(propName) { NominalValue = value, Unit = unit };
        //        pset = new PropertySet() { Name = propSetName };
        //        pset.HasProperties.Add_Reversible(propName, prop);
        //        rel = new RelDefinesByProperties() { RelatingPropertyDefinition = pset };
        //        rel.RelatedObjects.Add_Reversible(obj);
        //    }
        //    else //we have a PropertySet for this definition
        //    {
        //        PropertySingleValue prop = pset.HasProperties.FirstOrDefault<Property>(p => p.Key == propName) as PropertySingleValue;
        //        if (prop == null)
        //        {
        //            prop = new PropertySingleValue(propName) { NominalValue = value, Unit = unit };
        //            pset.HasProperties.Add_Reversible(propName, prop);
        //        }
        //        else
        //        {
        //            prop.NominalValue = value;
        //            prop.Unit = unit;
        //        }
        //    }
        //}
    }
}