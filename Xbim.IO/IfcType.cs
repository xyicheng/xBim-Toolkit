using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Xbim.XbimExtensions.SelectTypes;

namespace Xbim.IO
{
    public class IfcType
    {
        public Type Type;
        public ushort TypeId = 0;
        public SortedList<int, IfcMetaProperty> IfcProperties = new SortedList<int, IfcMetaProperty>();
        public List<IfcMetaProperty> IfcInverses = new List<IfcMetaProperty>();
        public IfcType IfcSuperType;
        public List<IfcType> IfcSubTypes = new List<IfcType>();
        private List<Type> _nonAbstractSubTypes;
        private PropertyInfo _primaryIndex;
        private List<PropertyInfo> _secondaryIndices;
        private List<IfcMetaProperty> _expressEnumerableProperties;
        internal int PrimaryKeyIndex = -1; 
        
        public List<IfcMetaProperty> ExpressEnumerableProperties
        {
            get
            {
                if (_expressEnumerableProperties == null)
                {
                    _expressEnumerableProperties = new List<IfcMetaProperty>();
                    foreach (IfcMetaProperty prop in IfcProperties.Values)
                    {
                        if (typeof(ExpressEnumerable).IsAssignableFrom(prop.PropertyInfo.PropertyType))
                            _expressEnumerableProperties.Add(prop);
                    }
                }
                return _expressEnumerableProperties;
            }
        }

        public override string ToString()
        {
            return Type.Name;
        }

        public IList<Type> NonAbstractSubTypes
        {
            get
            {
                if (_nonAbstractSubTypes == null)
                {
                    _nonAbstractSubTypes = new List<Type>();
                    AddNonAbstractTypes(this, _nonAbstractSubTypes);
                }
                return _nonAbstractSubTypes;
            }
        }

        public List<PropertyInfo> SecondaryIndices
        {
            get { return _secondaryIndices; }
            internal set { _secondaryIndices = value; }
        }

        public PropertyInfo PrimaryIndex
        {
            get { return _primaryIndex; }
            internal set { _primaryIndex = value; }
        }

        /// <summary>
        ///   Returns true if there is a primary or secondar index on this class
        /// </summary>
        public bool HasIndex
        {
            get { return _primaryIndex != null || (_secondaryIndices != null && _secondaryIndices.Count > 0); }
        }


        private void AddNonAbstractTypes(IfcType ifcType, List<Type> nonAbstractTypes)
        {
            if (!ifcType.Type.IsAbstract) //this is a concrete type so add it
                nonAbstractTypes.Add(ifcType.Type);
            foreach (IfcType subType in ifcType.IfcSubTypes)
                AddNonAbstractTypes(subType, nonAbstractTypes);
        }

      
    }

}
