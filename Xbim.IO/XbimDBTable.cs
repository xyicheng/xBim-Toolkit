using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace Xbim.IO
{
    public class XbimDBTable
    {
        protected const int transactionBatchSize = 100;
        /// <summary>
        /// The ESENT instance the cursor is opened against.
        /// </summary>
        protected readonly Instance instance;
        /// <summary> 
        /// The ESENT session the cursor is using.
        /// </summary>
        protected readonly JET_SESID sesid;
        /// <summary>
        /// ID of the opened database.
        /// </summary>
        protected readonly JET_DBID dbId;

        /// <summary>
        /// ID of the opened data table.
        /// </summary>
        protected JET_TABLEID table;

        protected readonly object lockObject;

        private string database;

        public XbimDBTable(Instance instance, string database)
        {
            this.lockObject = new Object();
            this.instance = instance;
            Api.JetBeginSession(this.instance, out this.sesid, String.Empty, String.Empty);
            Api.JetAttachDatabase(this.sesid, database, AttachDatabaseGrbit.None);
            Api.JetOpenDatabase(this.sesid, database, String.Empty, out this.dbId, OpenDatabaseGrbit.None);
            
        }

        public bool TryMoveNext()
        {
            return Api.TryMoveNext(this.sesid, this.table);
        }

        public bool TryMoveFirst()
        {
            return Api.TryMoveFirst(this.sesid, this.table);
        }

        public bool TryMoveLast()
        {
            return Api.TryMoveLast(this.sesid, this.table);
        }

        public void SetCurrentIndex(string indexName)
        {
            Api.JetSetCurrentIndex(this.sesid, this.table, indexName);
        }
    }
}
