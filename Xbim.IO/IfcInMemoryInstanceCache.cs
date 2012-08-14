﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Interfaces;
using System.Xml;
using System.IO;
using Xbim.XbimExtensions;

namespace Xbim.IO
{
    public class IfcInMemoryInstanceCache : Dictionary<long, IPersistIfcEntity>, IIfcInstanceCache
    {
        private XbimModel xbimModel;

        public IfcInMemoryInstanceCache(XbimModel xbimModel)
        {
            // TODO: Complete member initialization
            this.xbimModel = xbimModel;
        }

        public bool Contains(IPersistIfcEntity instance)
        {
            throw new NotImplementedException();
        }

        public bool Contains(long p)
        {
            throw new NotImplementedException();
        }

        public new long Count
        {
            get { throw new NotImplementedException(); }
        }

        public long HighestLabel
        {
            get { throw new NotImplementedException(); }
        }

        public void SetHighestLabel_Reversable(long nextLabel)
        {
            throw new NotImplementedException();
        }

        public IPersistIfcEntity CreateNew_Reversable(Type t)
        {
            throw new NotImplementedException();
        }

        public long InstancesOfTypeCount(Type t)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IfcInstanceHandle> InstanceHandles
        {
            get { throw new NotImplementedException(); }
        }

        public IPersistIfcEntity GetInstance(long label, bool loadProperties = false, bool unCached = false)
        {
            throw new NotImplementedException();
        }

        public IPersistIfcEntity CreateNew_Reversable(Type t, long label)
        {
            throw new NotImplementedException();
        }

        public void ImportIfc(string importFrom, ReportProgressDelegate progressHandler = null)
        {
            throw new NotImplementedException();
        }

        public void ImportXbim(string importFrom, ReportProgressDelegate progressHandler = null)
        {
            throw new NotImplementedException();
        }

        public void ImportIfcXml(string importFrom, ReportProgressDelegate progressHandler = null)
        {
            throw new NotImplementedException();
        }

        public void ImportIfcZip(string importFrom, ReportProgressDelegate progressHandler = null)
        {
            throw new NotImplementedException();
        }

        public IPersistIfcEntity CreateInstance(Type type, long id)
        {
            throw new NotImplementedException();
        }

        public void UpdateEntity(IPersistIfcEntity toWrite, int entitiesParsed = 1)
        {
            throw new NotImplementedException();
        }

        public void Activate(IPersistIfcEntity entity)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TIfcType> OfType<TIfcType>(bool activate = false, long secondaryKey = -1)
        {
            throw new NotImplementedException();
        }

        public void SaveAs(XbimStorageType _storageType, string _storageFileName, HashSet<IPersistIfcEntity> ToWrite, HashSet<IPersistIfcEntity> ToDelete, ReportProgressDelegate progress = null)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IfcInstanceHandle> InstanceHandlesOfType<T>()
        {
            throw new NotImplementedException();
        }


        public void Delete_Reversable(IPersistIfcEntity instance)
        {
            throw new NotImplementedException();
        }

        public void Update_Reversible(IPersistIfcEntity entity)
        {
            throw new NotImplementedException();
        }

        public void SaveAs(XbimStorageType storageType, string outputFileName, ReportProgressDelegate progress = null)
        {
            throw new NotImplementedException();
        }

        public bool Saved
        {
            get { throw new NotImplementedException(); }
        }

        public IPersistIfcEntity CreateNew(Type type, long id)
        {
            throw new NotImplementedException();
        }


        public void Close()
        {
            throw new NotImplementedException();
        }


        public IEnumerable<T> Where<T>(System.Linq.Expressions.Expression<Func<T, bool>> expression)
        {
            throw new NotImplementedException();
        }


        public void AddXbimGeometry(Ifc2x3.Kernel.IfcProduct product, byte[] geoemtryMesh, byte[] boundingBox)
        {
            throw new NotImplementedException();
        }


        public XbimGeometryTable BeginGeometryUpdate()
        {
            throw new NotImplementedException();
        }



        public void EndGeometryUpdate(XbimGeometryTable table)
        {
            throw new NotImplementedException();
        }


        public XbimGeometryData GetGeometry(Ifc2x3.Kernel.IfcProduct product, XbimGeometryType geomType)
        {
            throw new NotImplementedException();
        }

        bool IIfcInstanceCache.Contains(IPersistIfcEntity instance)
        {
            throw new NotImplementedException();
        }

        bool IIfcInstanceCache.Contains(long p)
        {
            throw new NotImplementedException();
        }

        long IIfcInstanceCache.Count
        {
            get { throw new NotImplementedException(); }
        }

        long IIfcInstanceCache.HighestLabel
        {
            get { throw new NotImplementedException(); }
        }

        void IIfcInstanceCache.SetHighestLabel_Reversable(long nextLabel)
        {
            throw new NotImplementedException();
        }

        long IIfcInstanceCache.InstancesOfTypeCount(Type t)
        {
            throw new NotImplementedException();
        }

        IEnumerable<IfcInstanceHandle> IIfcInstanceCache.InstanceHandles
        {
            get { throw new NotImplementedException(); }
        }

        IPersistIfcEntity IIfcInstanceCache.GetInstance(long label, bool loadProperties, bool unCached)
        {
            throw new NotImplementedException();
        }

        IPersistIfcEntity IIfcInstanceCache.CreateNew_Reversable(Type t, long label)
        {
            throw new NotImplementedException();
        }

        void IIfcInstanceCache.Close()
        {
            throw new NotImplementedException();
        }

        void IIfcInstanceCache.ImportIfc(string importFrom, ReportProgressDelegate progressHandler)
        {
            throw new NotImplementedException();
        }

        void IIfcInstanceCache.ImportXbim(string importFrom, ReportProgressDelegate progressHandler)
        {
            throw new NotImplementedException();
        }

        void IIfcInstanceCache.ImportIfcXml(string importFrom, ReportProgressDelegate progressHandler)
        {
            throw new NotImplementedException();
        }

        void IIfcInstanceCache.ImportIfcZip(string importFrom, ReportProgressDelegate progressHandler)
        {
            throw new NotImplementedException();
        }

        void IIfcInstanceCache.UpdateEntity(IPersistIfcEntity toWrite, int entitiesParsed)
        {
            throw new NotImplementedException();
        }

        void IIfcInstanceCache.Activate(IPersistIfcEntity entity)
        {
            throw new NotImplementedException();
        }

        IEnumerable<TIfcType> IIfcInstanceCache.OfType<TIfcType>(bool activate, long secondaryKey)
        {
            throw new NotImplementedException();
        }

        void IIfcInstanceCache.SaveAs(XbimStorageType _storageType, string _storageFileName, ReportProgressDelegate progress)
        {
            throw new NotImplementedException();
        }

        IEnumerable<IfcInstanceHandle> IIfcInstanceCache.InstanceHandlesOfType<T>()
        {
            throw new NotImplementedException();
        }

        void IIfcInstanceCache.Delete_Reversable(IPersistIfcEntity instance)
        {
            throw new NotImplementedException();
        }

        void IIfcInstanceCache.Update_Reversible(IPersistIfcEntity entity)
        {
            throw new NotImplementedException();
        }

        bool IIfcInstanceCache.Saved
        {
            get { throw new NotImplementedException(); }
        }

        IPersistIfcEntity IIfcInstanceCache.CreateNew(Type type, long id)
        {
            throw new NotImplementedException();
        }

        IEnumerable<T> IIfcInstanceCache.Where<T>(System.Linq.Expressions.Expression<Func<T, bool>> expression)
        {
            throw new NotImplementedException();
        }

        XbimGeometryTable IIfcInstanceCache.BeginGeometryUpdate()
        {
            throw new NotImplementedException();
        }

        void IIfcInstanceCache.EndGeometryUpdate(XbimGeometryTable table)
        {
            throw new NotImplementedException();
        }

        XbimGeometryData IIfcInstanceCache.GetGeometry(Ifc2x3.Kernel.IfcProduct product, XbimGeometryType geomType)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<XbimGeometryData> Shapes(XbimGeometryType ofType)
        {
            throw new NotImplementedException();
        }
    }
}
