using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.IO
{
    /// <summary>
    /// A lightweight structure for obtaining a handle to an Ifc Instance, the instance is not loaded into memory unless the GetInstance function is called
    /// IfcInstanceHandle are specific to the model they were generated from
    /// </summary>
    public struct XbimInstanceHandle
    {
        public int EntityLabel;
        public short EntityTypeId;

        public static bool operator ==(XbimInstanceHandle a, XbimInstanceHandle b)
        {
            return a.EntityLabel == b.EntityLabel && a.EntityTypeId == b.EntityTypeId;
        }

        public static bool operator !=(XbimInstanceHandle a, XbimInstanceHandle b)
        {
            return a.EntityLabel != b.EntityLabel || a.EntityTypeId == b.EntityTypeId;
        }

        public override int GetHashCode()
        {
            return (EntityLabel << sizeof(int) + EntityTypeId).GetHashCode();
        }
        public override bool Equals(object b)
        {
            return this.EntityLabel == ((XbimInstanceHandle)b).EntityLabel && this.EntityTypeId == ((XbimInstanceHandle)b).EntityTypeId;
        }

        public Type EntityType
        {
            get
            {
                return IfcMetaData.GetType(EntityTypeId);
            }
        }

        public IfcType EntityIfcType
        {
            get
            {
                return  IfcMetaData.IfcType(EntityTypeId);
            }
        }
        
        public bool IsEmpty
        {
            get
            {
                return (EntityLabel == 0);
            }
        }

        
        public XbimInstanceHandle(int entityLabel, short type = 0)
        {
            EntityLabel = Math.Abs(entityLabel);
            EntityTypeId= type;
        }

        public XbimInstanceHandle(int entityLabel, Type type)
        {
            EntityLabel = Math.Abs(entityLabel);
            EntityTypeId = IfcMetaData.IfcTypeId(type);
        }

        public XbimInstanceHandle(int? label, short? type)
        {
            this.EntityLabel = label ?? 0;
            this.EntityTypeId = type ?? 0;  
        }

        public IPersistIfcEntity GetInstance(XbimModel model)
        {
            return model.Instances[EntityLabel];
        }

        internal IfcType IfcType()
        {
            return IfcMetaData.IfcType(EntityTypeId);
        }
       
    }
}
