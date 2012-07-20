#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IModel.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using Xbim.XbimExtensions.Transactions;


#endregion

namespace Xbim.XbimExtensions.Interfaces
{
    public delegate void InitProperties<TInit>(TInit initFunction);
    // Declare a delegate type for processing a P21 value:
    public delegate void ReportProgressDelegate(int percentProgress, object userState);

    [Flags]
    public enum XbimStorageType
    {
        /// <summary>
        ///   IFC in XML format
        /// </summary>
        IFCXML = 1,

        /// <summary>
        ///   Native IFC format
        /// </summary>
        IFC = 2,

        /// <summary>
        ///   compressed IFC format
        /// </summary>
        IFCZIP = 4,

        // IFCXMLX = 8,
        /// <summary>
        ///   Compressed IfcXml
        /// </summary>
        /// <summary>
        ///   Xbim binary format
        /// </summary>
        XBIM = 16
    }
    public interface IModel
    {
        


        IEnumerable<TIfcType> InstancesOfType<TIfcType>() where TIfcType : IPersistIfcEntity;

        IEnumerable<TIfcType> InstancesWhere<TIfcType>(Expression<Func<TIfcType, bool>> expression)
            where TIfcType : IPersistIfcEntity;

        TIfcType New<TIfcType>() where TIfcType : IPersistIfcEntity, new();
        TIfcType New<TIfcType>(InitProperties<TIfcType> initPropertiesFunc) where TIfcType : IPersistIfcEntity, new();

        bool Delete(IPersistIfcEntity instance);
        bool ContainsInstance(IPersistIfcEntity instance);
        bool ContainsInstance(long entityLabel);
        IEnumerable<IPersistIfcEntity> Instances { get; }
        long InstancesCount { get; }

        IPersistIfcEntity AddNew(Type ifcType, long label);

        int ParsePart21(Stream inputStream, ReportProgressDelegate progressHandler);

        IPersistIfcEntity OwnerHistoryAddObject { get; }
        IPersistIfcEntity OwnerHistoryModifyObject { get; }
       // IfcCoordinatedUniversalTimeOffset CoordinatedUniversalTimeOffset { get; }
        IPersistIfcEntity IfcProject { get; }
        IEnumerable<IPersistIfcEntity> IfcProducts { get; }

        IPersistIfcEntity DefaultOwningApplication { get; }
        IPersistIfcEntity DefaultOwningUser { get; }
        Transaction BeginTransaction(string operationName);
        IIfcFileHeader Header { get; set; }
        IEnumerable<Tuple<string, long>> ModelStatistics();
        int Validate(TextWriter errStream, ReportProgressDelegate progressDelegate, ValidationFlags validateFlags);
        int Validate(TextWriter errStream, ReportProgressDelegate progressDelegate);
        int Validate(TextWriter errStream);
        string Validate(ValidationFlags validateFlags);
        void Export(XbimStorageType fileType, string outputFileName);
        string Open(string inputFileName);
        string Open(string inputFileName, ReportProgressDelegate progDelegate);
        bool Save();
        bool SaveAs(string outputFileName);
        void Import(string inputFileName);
        long Activate(IPersistIfcEntity entity, bool write);
        IPersistIfcEntity GetInstance(long label);
       
        void Close();

        UndoRedoSession UndoRedo { get; }




    }
}