﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcObject.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.Kernel
{
    [IfcPersistedEntity, Serializable]
    public class IfcObjectSet : XbimSet<IfcObject>
    {
        internal IfcObjectSet(IPersistIfcEntity owner)
            : base(owner)
        {
        }
    }

    /// <summary>
    ///   An IfcObject is the generalization of any semantically treated thing or process.
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: An IfcObject is the generalization of any semantically treated thing or process. Objects are things as they appear - i.e. occurrences. 
    ///   NOTE Examples of IfcObject include physically tangible items, such as wall, beam or covering, physically existing items, such as spaces, or conceptual items, such as grids or virtual boundaries. It also stands for processes, such as work tasks, for controls, such as cost items, for actors, such as persons involved in the design process, etc. 
    ///   Objects can be named, using the inherited Name attribute, which should be a user recognizable label for the object occurrance. Further explanations to the object can be given using the inherited Description attribute. The ObjectType attribute is used:
    ///   to store the user defined value for all subtypes of IfcObject, where a PredefinedType attribute is given, and its value is set to USERDEFINED. 
    ///   to provide a type information (could be seen as a very lightweight classifier) of the subtype of IfcObject, if no PredefinedType attribute is given. This is often the case, if no comprehensive list of predefined types is available. 
    ///   Objects are independent pieces of information that might contain or reference other pieces of information. There are four essential kind of relationships in which objects can be involved:
    ///   Assignment of other objects - an assignment relationship that refers to other types of objects. See supertype IfcObjectDefinition for more information. 
    ///   Association to external resources - an association relationship that refers to external sources of information. See supertype IfcObjectDefinition for more information. 
    ///   Aggregation of other objects - an aggregation relationship that establishes a whole/part relation. See supertype IfcObjectDefinition for more information.
    ///   Refinement by type and properties - a refinement relationship (IfcRelDefines) that uses a type definition or (partial) property set definition to define the properties of the object instance. It is a specific - occurrence relationship with implied dependencies (as the occurrence properties depend on the specific properties). 
    ///   HISTORY New Entity in IFC Release 1.0
    ///   Formal Propositions:
    ///   WR1   :   Only maximum of one relationship to an underlying type (by an IfcRelDefinesByType relationship) should be given for an object instance.
    /// </remarks>
    [IfcPersistedEntity, Serializable]
    public abstract class IfcObject : IfcObjectDefinition
    {
        #region Fields and Events

        private IfcLabel? _objectType;

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The type denotes a particular type that indicates the object futher. The use has to be established at the level of instantiable subtypes. In particular it holds the user defined type, if the enumeration of the attribute PredefinedType is set to USERDEFINED.
        /// </summary>
        [DataMember(Order = 4, IsRequired = false, EmitDefaultValue = false)]
        [IfcAttribute(5, IfcAttributeState.Optional)]
        public IfcLabel? ObjectType
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _objectType;
            }
            set { ModelManager.SetModelValue(this, ref _objectType, value, v => ObjectType = v, "ObjectType"); }
        }


        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    base.IfcParse(propIndex, value);
                    break;
                case 4:
                    _objectType = value.StringVal;
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }

        #endregion

        #region Inverse Relationships

        /// <summary>
        ///   Set of relationships to type or property (statically or dynamically defined) information that further define the object. In case of type information, the associated IfcTypeObject contains the specific information (or type, or style), that is common to all instances of IfcObject refering to the same type.
        /// </summary>
        [XmlIgnore]
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class)]
        public virtual IEnumerable<IfcRelDefines> IsDefinedBy
        {
            get { return ModelManager.ModelOf(this).InstancesWhere<IfcRelDefines>(r => r.RelatedObjects.Contains(this)); }
        }

        #endregion

        #region Ifc PropertySets

        /// <summary>
        /// </summary>
        [XmlIgnore]
        public IEnumerable<IfcRelDefinesByProperties> IsDefinedByProperties
        {
            get
            {
                return
                    ModelManager.ModelOf(this).InstancesWhere<IfcRelDefinesByProperties>(
                        r => (r.RelatedObjects != null && r.RelatedObjects.Contains(this)));
            }
        }

        /// <summary>
        ///   Returns a collection of PropertySets for the entity
        /// </summary>
        [XmlIgnore]
        public IEnumerable<IfcPropertySet> PropertySets
        {
            get
            {
                return
                    IsDefinedBy.OfType<IfcRelDefinesByProperties>().Select(def => def.RelatingPropertyDefinition).OfType
                        <IfcPropertySet>();
            }
        }

        #endregion

        #region Ifc Schema Validation Methods

        public override string WhereRule()
        {
            if (IsDefinedBy.OfType<IfcRelDefinesByType>().Count() > 1)
                return
                    "WR1 IfcObject: Only maximum of one relationship to an underlying type (by an RelDefinesByType relationship) should be given for an object instance.\n";
            return "";
        }

        #endregion
    }
}