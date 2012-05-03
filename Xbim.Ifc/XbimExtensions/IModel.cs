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
using Xbim.Ifc.ActorResource;
using Xbim.Ifc.DateTimeResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.UtilityResource;
using Xbim.XbimExtensions.DataProviders;
using Xbim.XbimExtensions.Parser;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc.SharedBldgElements;

#endregion

namespace Xbim.XbimExtensions
{
    public interface IModel
    {
        IEnumerable<TIfcType> InstancesOfType<TIfcType>() where TIfcType : IPersistIfcEntity;

        IEnumerable<TIfcType> InstancesWhere<TIfcType>(Expression<Func<TIfcType, bool>> expression)
            where TIfcType : IPersistIfcEntity;

        TIfcType New<TIfcType>() where TIfcType : IPersistIfcEntity, new();
        TIfcType New<TIfcType>(InitProperties<TIfcType> initPropertiesFunc) where TIfcType : IPersistIfcEntity, new();
        bool Delete(IPersistIfcEntity instance);
        bool ContainsInstance(IPersistIfcEntity instance);
        IEnumerable<IPersistIfcEntity> Instances { get; }
        long InstancesCount { get; }

        int ParsePart21(Stream inputStream, FilterViewDefinition filter, TextWriter errorLog,
                        ReportProgressDelegate progressHandler);

        IfcOwnerHistory OwnerHistoryAddObject { get; }
        IfcOwnerHistory OwnerHistoryModifyObject { get; }
        IfcCoordinatedUniversalTimeOffset CoordinatedUniversalTimeOffset { get; }
        IfcProject IfcProject { get; }
        IfcProducts IfcProducts { get; }

        IfcApplication DefaultOwningApplication { get; }
        IfcPersonAndOrganization DefaultOwningUser { get; }
        Transaction BeginTransaction(string operationName);
        IfcFileHeader Header { get; }
        IEnumerable<Tuple<string, long>> ModelStatistics();
        int Validate(TextWriter errStream, ReportProgressDelegate progressDelegate, ValidationFlags validateFlags);
        int Validate(TextWriter errStream, ReportProgressDelegate progressDelegate);
        int Validate(TextWriter errStream);
        string Validate(ValidationFlags validateFlags);
#if SupportActivation
        long Activate(IPersistIfcEntity entity, bool write);
        IPersistIfcEntity GetInstance(long label);
        
#endif

        bool ReOpen();
        void Close();

        UndoRedoSession UndoRedo { get; }
        //Simple accessor to standard object
        IEnumerable<IfcWall> Walls { get; }
        IEnumerable<IfcSlab> Slabs { get; }
        IEnumerable<IfcDoor> Doors { get; }
        IEnumerable<IfcRoof> Roofs { get; }

    }
}