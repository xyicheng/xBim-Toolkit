using System;using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Interfaces;
using System.Collections;
using System.Linq.Expressions;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.UtilityResource;
using Xbim.Ifc2x3.ActorResource;

namespace Xbim.IO
{
    /// <summary>
    /// A class providing access to a collection of in,stances in a model
    /// </summary>
    public class XbimInstanceCollection : IXbimInstanceCollection
    {
        private IfcPersistedInstanceCache cache;
        #region OwnerHistory Fields


        [NonSerialized]
        private IfcOwnerHistory _ownerHistoryDeleteObject;

        [NonSerialized]
        private IfcOwnerHistory _ownerHistoryAddObject;

        [NonSerialized]
        private IfcOwnerHistory _ownerHistoryModifyObject;

        [NonSerialized]
        private IfcPersonAndOrganization _defaultOwningUser;

        [NonSerialized]
        private IfcApplication _defaultOwningApplication;
        #endregion
        internal XbimInstanceCollection(IfcPersistedInstanceCache theCache)
        {
            cache = theCache;
        }

        /// <summary>
        /// Returns the total number of Ifc Instances in the model
        /// </summary>
        public long Count
        {
            get
            {
                return cache.Count;
            }
        }

        /// <summary>
        /// Returns all instances in the model of IfcType, IfcType may be an abstract Type
        /// </summary>
        /// <param name="activate">if true each instance is fullly populated from the database, if false population is deferred until the entity is activated</param>
        /// <returns></returns>
        public IEnumerable<TIfc> OfType<TIfc>(bool activate) where TIfc : IPersistIfcEntity
        {
            foreach (var item in cache.OfType<TIfc>(activate))
                yield return (TIfc)item;
        }
        public IEnumerable<TIfc> OfType<TIfc>() where TIfc : IPersistIfcEntity
        {
            foreach (var item in cache.OfType<TIfc>(false))
                yield return (TIfc)item;
        }
        //public IEnumerable<TIfcType> OfType<TIfcType>() where TIfcType : IPersistIfcEntity
        //{
        //    return cache.OfType<TIfcType>(false);
        //}

        /// <summary>
        ///   Filters the Ifc Instances based on their Type and the predicate
        /// </summary>
        /// <typeparam name = "TIfcType">Ifc Type to filter</typeparam>
        /// <param name = "expression">function to execute</param>
        /// <returns></returns>
        public IEnumerable<TIfcType> Where<TIfcType>(Expression<Func<TIfcType, bool>> expression) where TIfcType : IPersistIfcEntity
        {
            foreach (var item in cache.Where(expression))
                yield return item;
            
        }
        /// <summary>
        /// Returns an enumerabale of all the instance handles in the model
        /// </summary>
        public IEnumerable<XbimInstanceHandle> Handles()
        {
            foreach (var item in cache.InstanceHandles)
                yield return item; 
        }


        /// <summary>
        /// Returns an enumerable of all handles of the specified type in the model
        /// </summary>
        /// <typeparam name="T">The type of entity required</typeparam>
        /// <returns></returns>
        public IEnumerable<XbimInstanceHandle> Handles<T>()
        {
            foreach (var item in cache.InstanceHandlesOfType<T>())
                yield return item;
        }

        /// <summary>
        /// Returns an instance from the Model with the corresponding label
        /// </summary>
        /// <param name="label">entity label to retrieve</param>
        /// <returns></returns>
        public IPersistIfcEntity this[int label]
        {
            get
            {
                return cache.GetInstance(label, true, true);
            }
        }
    
        /// <summary>
        ///   Creates a new Ifc Persistent Instance, this is an undoable operation
        /// </summary>
        /// <typeparam name = "TIfcType"> The Ifc Type, this cannot be an abstract class. An exception will be thrown if the type is not a valid Ifc Type  </typeparam>
        public TIfcType New<TIfcType>() where TIfcType : IPersistIfcEntity, new()
        {
            Type t = typeof(TIfcType);
            int nextLabel = cache.HighestLabel + 1;
            return (TIfcType)New(t, nextLabel);
        }
        /// <summary>
        ///   Creates and Instance of TIfcType and initializes the properties in accordance with the lambda expression
        ///   i.e. Person person = CreateInstance&gt;Person&lt;(p =&lt; { p.FamilyName = "Undefined"; p.GivenName = "Joe"; });
        /// </summary>
        /// <typeparam name = "TIfcType"></typeparam>
        /// <param name = "initPropertiesFunc"></param>
        /// <returns></returns>
        public TIfcType New<TIfcType>(InitProperties<TIfcType> initPropertiesFunc) where TIfcType : IPersistIfcEntity, new()
        {
            TIfcType instance = New<TIfcType>();
            initPropertiesFunc(instance);
            return instance;
        }

        /// <summary>
        /// Creates and returns a new instance of Type t, sets the label to the specificed value.
        /// This is a reversabel operation
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public IPersistIfcEntity New(Type t, int label)
        {
            int nextLabel = Math.Abs(label);

            IPersistIfcEntity entity = cache.CreateNew_Reversable(t, nextLabel);
            if (typeof(IfcRoot).IsAssignableFrom(t))
                ((IfcRoot)entity).OwnerHistory = OwnerHistoryAddObject;
            return entity;

        }

       
        /// <summary>
        /// Returns true if the instance label is in the current model, 
        /// Use with care, does not check that the instance is in the current model, only the label exists
        /// </summary>
        /// <param name="entityLabel"></param>
        /// <returns></returns>
        public  bool Contains(int entityLabel)
        {
            return cache.Contains(entityLabel);
        }

        /// <summary>
        /// Returns true if the instance is in the current model
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public  bool Contains(IPersistIfcEntity instance)
        {
            return cache.Contains(instance);
        }

        internal IfcOwnerHistory OwnerHistoryModifyObject
        {
            get
            {
                return _ownerHistoryModifyObject;
            }
        }

        internal IfcOwnerHistory OwnerHistoryAddObject
        {
            get
            {
                return _ownerHistoryAddObject;
            }
        }

        internal IfcOwnerHistory OwnerHistoryDeleteObject
        {
            get
            {
                if (_ownerHistoryDeleteObject == null)
                {
                    _ownerHistoryDeleteObject = this.New<IfcOwnerHistory>();
                    _ownerHistoryDeleteObject.OwningUser = _defaultOwningUser;
                    _ownerHistoryDeleteObject.OwningApplication = _defaultOwningApplication;
                    _ownerHistoryDeleteObject.ChangeAction = IfcChangeActionEnum.DELETED;
                }
                return _ownerHistoryDeleteObject;
            }
        }



        internal IfcApplication DefaultOwningApplication
        {
            get { return _defaultOwningApplication; }
        }

        internal IfcPersonAndOrganization DefaultOwningUser
        {
            get { return _defaultOwningUser; }
        }

        public IEnumerator<int> GetEnumerator()
        {
           return cache.GetEntityTable();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return cache.GetEntityTable();
        }
    }

}
