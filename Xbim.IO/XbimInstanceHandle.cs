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
        public short? EntityTypeId;

        public Type EntityType
        {
            get
            {
                return EntityTypeId.HasValue ? IfcMetaData.GetType(EntityTypeId.Value) : null;
            }
        }

        public IfcType EntityIfcType
        {
            get
            {
                return EntityTypeId.HasValue ? IfcMetaData.IfcType(EntityTypeId.Value) : null;
            }
        }
        
        public bool IsEmpty
        {
            get
            {
                return (EntityLabel == 0 && EntityTypeId==0);
            }
        }

        
        public XbimInstanceHandle(int entityLabel, short? type)
        {
            EntityLabel = Math.Abs(entityLabel);
            EntityTypeId= type;
        }

        public XbimInstanceHandle(int entityLabel, Type type)
        {
            EntityLabel = Math.Abs(entityLabel);
            EntityTypeId = IfcMetaData.IfcTypeId(type);
        }

        public IPersistIfcEntity GetInstance(XbimModel model)
        {
            return model.GetInstance(EntityLabel);
        }

        internal IfcType IfcType()
        {
            return EntityTypeId.HasValue ? IfcMetaData.IfcType(EntityTypeId.Value) : null;
        }
    }
}
