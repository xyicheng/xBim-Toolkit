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
    public struct IfcInstanceHandle
    {
        public long EntityLabel;
        public Type EntityType;
        public readonly static IfcInstanceHandle Empty;
        static IfcInstanceHandle()
        {
            Empty = new IfcInstanceHandle(-1, null);
        }
        public bool IsEmpty
        {
            get
            {
                return (EntityLabel == Empty.EntityLabel && EntityType==Empty.EntityType);
            }
        }
       
        public IfcInstanceHandle(long entityLabel, Type type)
        {
            EntityLabel = Math.Abs(entityLabel);
            EntityType= type;
        }
        public IPersistIfcEntity GetInstance(XbimModel model)
        {
            return model.GetInstance(EntityLabel);
        }
    }
}
