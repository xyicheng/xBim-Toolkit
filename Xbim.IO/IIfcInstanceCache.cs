using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Interfaces;
using System.Linq.Expressions;

namespace Xbim.IO
{
    public interface IIfcInstanceCache
    {

        bool Contains(XbimExtensions.Interfaces.IPersistIfcEntity instance);
        bool Contains(long p);
        long Count { get; }

        long HighestLabel { get;}
        /// <summary>
        /// Sets the highest label
        /// </summary>
        /// <param name="nextLabel"></param>
        void SetHighestLabel_Reversable(long nextLabel);

        long InstancesOfTypeCount(Type t);
        IEnumerable<IfcInstanceHandle> InstanceHandles { get; }
        IPersistIfcEntity GetInstance(long label, bool loadProperties = false, bool unCached = false);

        IPersistIfcEntity CreateNew_Reversable(Type t, long label);

        void Close();

        void ImportIfc(string importFrom, ReportProgressDelegate progressHandler = null);

        void ImportXbim(string importFrom, ReportProgressDelegate progressHandler = null);

        void ImportIfcXml(string importFrom, ReportProgressDelegate progressHandler = null);

        void ImportIfcZip(string importFrom, ReportProgressDelegate progressHandler = null);


        void UpdateEntity(IPersistIfcEntity toWrite, int entitiesParsed = 1);
        /// <summary>
        /// Load the properties of the entity from the data store 
        /// </summary>
        /// <param name="entity"></param>
        void Activate(IPersistIfcEntity entity);

        IEnumerable<TIfcType> OfType<TIfcType>(bool activate = false, long secondaryKey = -1);

        void SaveAs(XbimStorageType _storageType, string _storageFileName,  ReportProgressDelegate progress = null);

        IEnumerable<IfcInstanceHandle> InstanceHandlesOfType<T>();


        void Delete_Reversable(IPersistIfcEntity instance);

        void Update_Reversible(IPersistIfcEntity entity);

        bool Saved { get;}

        /// <summary>
        /// Creates a new instance of type with entity label id, this is not a reversable action
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        IPersistIfcEntity CreateNew(Type type, long id);

        IEnumerable<T> Where<T>(Expression<Func<T, bool>> expression);
    }
}
