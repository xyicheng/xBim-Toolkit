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
using Xbim.Common;
using System.Collections;


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
        /// Invalid Xbim storage type
        /// </summary>
        INVALID = 0,

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


        XbimModelFactors GetModelFactors { get; }

        IXbimInstanceCollection Instances { get; }
        int Activate(IPersistIfcEntity owningEntity, bool write);
        void Delete(IPersistIfcEntity instance);

        IPersistIfcEntity IfcProject { get; }
        IEnumerable<IPersistIfcEntity> IfcProducts { get; }

        IPersistIfcEntity OwnerHistoryAddObject { get; }
        IPersistIfcEntity OwnerHistoryModifyObject { get; }
        IPersistIfcEntity DefaultOwningApplication { get; }
        IPersistIfcEntity DefaultOwningUser { get; }

        IIfcFileHeader Header { get;}  

        bool CreateFrom(string importFrom, string xbimDbName = null, ReportProgressDelegate progDelegate = null);
        bool Create(string fileName);

  
        bool SaveAs(string saveFileName, XbimStorageType storageType, ReportProgressDelegate progDelegate = null);

        bool Open(string fileName, XbimDBAccess accessMode = XbimDBAccess.Read, ReportProgressDelegate progDelegate = null);
        
        void Close();

        int Validate(TextWriter errStream, ReportProgressDelegate progressDelegate, ValidationFlags? validateFlags = null);


    }
}