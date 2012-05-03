using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.ProductExtension;

namespace Xbim.DOM.PropertiesQuantities
{
    /// <summary>
    /// Class providing access to Pset_SpaceCommon set of properties of the space:
    /// Definition from IAI: Properties common to the definition of all occurrences of IfcSpace. 
    /// Please note that several space attributes are handled directly at the IfcSpace instance, 
    /// the space number (or short name) by IfcSpace.Name, the space name (or long name) by 
    /// IfcSpace:LongName, and the description (or comments) by IfcSpace.Description. Actual space 
    /// quantities, like space perimeter, space area and space volume are provided by IfcElementQuantities, 
    /// and space classification according to national building code by IfcClassificationReference. The level 
    /// above zero (relative to the building) for the slab row construction is provided by the IfcBuildingStorey.
    /// Elevation, the level above zero (relative to the building) for the floor finish is provided 
    /// by the IfcSpace.ElevationWithFlooring.
    /// </summary>
    public class XbimSpaceCommonProperties : XbimProperties
    {
        internal XbimSpaceCommonProperties(IfcSpace space) : base(space, "Pset_SpaceCommon") { }

        /// <summary>
        /// Reference ID for this specified type in this project (e.g. type 'A-1')
        /// </summary>
        public string Reference
        {
            get
            {
                IfcValue value = GetProperty("Reference");
                if (value != null) return (IfcIdentifier)value;
                return null;
            }
            set
            {
                if (value != null)
                {
                    IfcIdentifier val = (IfcIdentifier)value;
                    SetProperty("Reference", val);
                }
                else
                {
                    RemoveProperty("Reference");
                }

            }
        }

        /// <summary>
        /// Category of space usage or utilization of the area. 
        /// It is defined according to the presiding national building code.
        /// </summary>
        public string Category
        {
            get
            {
                IfcValue value = GetProperty("Category");
                if (value != null) return (IfcLabel)value;
                return null;
            }
            set
            {
                if (value != null)
                {
                    IfcLabel val = (IfcLabel)value;
                    SetProperty("Category", val);
                }
                else
                {
                    RemoveProperty("Category");
                }

            }
        }

        /// <summary>
        /// Label to indicate the material or finish of the space flooring. 
        /// The label is used for room book information and often displayed in room stamp.
        /// </summary>
        public string FloorCovering
        {
            get
            {
                IfcValue value = GetProperty("FloorCovering");
                if (value != null) return (IfcLabel)value;
                return null;
            }
            set
            {
                if (value != null)
                {
                    IfcLabel val = (IfcLabel)value;
                    SetProperty("FloorCovering", val);
                }
                else
                {
                    RemoveProperty("FloorCovering");
                }

            }
        }

        /// <summary>
        /// Label to indicate the material or finish of the space flooring. 
        /// The label is used for room book information and often displayed in room stamp.
        /// </summary>
        public string WallCovering
        {
            get
            {
                IfcValue value = GetProperty("WallCovering");
                if (value != null) return (IfcLabel)value;
                return null;
            }
            set
            {
                if (value != null)
                {
                    IfcLabel val = (IfcLabel)value;
                    SetProperty("WallCovering", val);
                }
                else
                {
                    RemoveProperty("WallCovering");
                }

            }
        }

        /// <summary>
        /// Label to indicate the material or finish of the space flooring. 
        /// The label is used for room book information and often displayed in room stamp.
        /// </summary>
        public string CeilingCovering
        {
            get
            {
                IfcValue value = GetProperty("CeilingCovering");
                if (value != null) return (IfcLabel)value;
                return null;
            }
            set
            {
                if (value != null)
                {
                    IfcLabel val = (IfcLabel)value;
                    SetProperty("CeilingCovering", val);
                }
                else
                {
                    RemoveProperty("CeilingCovering");
                }

            }
        }

        /// <summary>
        /// Label to indicate the material or construction of the skirting board 
        /// around the space flooring. The label is used for room book information 
        /// and often displayed in room stamp.
        /// </summary>
        public string SkirtingBoard
        {
            get
            {
                IfcValue value = GetProperty("SkirtingBoard");
                if (value != null) return (IfcLabel)value;
                return null;
            }
            set
            {
                if (value != null)
                {
                    IfcLabel val = (IfcLabel)value;
                    SetProperty("SkirtingBoard", val);
                }
                else
                {
                    RemoveProperty("SkirtingBoard");
                }

            }
        }

        /// <summary>
        /// Total planned area for the space. Used for programming the space.
        /// </summary>
        public double? GrossPlannedArea
        {
            get
            {
                IfcValue value = GetProperty("GrossPlannedArea");
                if (value != null) return (IfcAreaMeasure)value;
                return null;
            }
            set
            {
                if (value != null)
                {
                    IfcAreaMeasure val = (IfcAreaMeasure)value;
                    SetProperty("GrossPlannedArea", val);
                }
                else
                {
                    RemoveProperty("GrossPlannedArea");
                }

            }
        }

        public double? NetPlannedArea
        {
            get
            {
                IfcValue value = GetProperty("NetPlannedArea");
                if (value != null) return (IfcAreaMeasure)value;
                return null;
            }
            set
            {
                if (value != null)
                {
                    IfcAreaMeasure val = (IfcAreaMeasure)value;
                    SetProperty("NetPlannedArea", val);
                }
                else
                {
                    RemoveProperty("NetPlannedArea");
                }

            }
        }

        /// <summary>
        /// Indication whether this space (in case of e.g., a toilet) is designed to 
        /// serve as a publicly accessible space, e.g., for a public toilet (TRUE) or not (FALSE).
        /// </summary>
        public bool? PubliclyAccessible
        {
            get
            {
                IfcValue value = GetProperty("PubliclyAccessible");
                if (value != null) return (IfcBoolean)value;
                return null;
            }
            set
            {
                if (value != null)
                {
                    IfcBoolean val = (IfcBoolean)value;
                    SetProperty("PubliclyAccessible", val);
                }
                else
                {
                    RemoveProperty("PubliclyAccessible");
                }

            }
        }

        /// <summary>
        /// Indication whether this space (in case of e.g., a toilet) is designed to serve as an 
        /// accessible space for handicapped people, e.g., for a public toilet (TRUE) or not (FALSE). 
        /// This information is often used to declare the need for access for the disabled and for 
        /// special design requirements of this space.
        /// </summary>
        public bool? HandicapAccessible
        {
            get
            {
                IfcValue value = GetProperty("HandicapAccessible");
                if (value != null) return (IfcBoolean)value;
                return null;
            }
            set
            {
                if (value != null)
                {
                    IfcBoolean val = (IfcBoolean)value;
                    SetProperty("HandicapAccessible", val);
                }
                else
                {
                    RemoveProperty("HandicapAccessible");
                }

            }
        }

        /// <summary>
        /// Indication whether this space is declared to be a concealed flooring (TRUE) or not (FALSE). 
        /// A concealed flooring is normally meant to be the space beneath a raised floor.
        /// </summary>
        public bool? ConcealedFlooring
        {
            get
            {
                IfcValue value = GetProperty("ConcealedFlooring");
                if (value != null) return (IfcBoolean)value;
                return null;
            }
            set
            {
                if (value != null)
                {
                    IfcBoolean val = (IfcBoolean)value;
                    SetProperty("ConcealedFlooring", val);
                }
                else
                {
                    RemoveProperty("ConcealedFlooring");
                }

            }
        }

        /// <summary>
        /// Indication whether this space is declared to be a concealed ceiling (TRUE) or not (FALSE). 
        /// A concealed ceiling is normally meant to be the space between a slab and a suspended ceiling.
        /// </summary>
        public bool? ConcealedCeiling
        {
            get
            {
                IfcValue value = GetProperty("ConcealedCeiling");
                if (value != null) return (IfcBoolean)value;
                return null;
            }
            set
            {
                if (value != null)
                {
                    IfcBoolean val = (IfcBoolean)value;
                    SetProperty("ConcealedCeiling", val);
                }
                else
                {
                    RemoveProperty("ConcealedCeiling");
                }

            }
        }
    }
}
