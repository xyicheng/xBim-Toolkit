using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.IO
{
    /// <summary>
    /// A transaction allowing read only operations on a model
    /// </summary>
    public class XbimReadTransaction : IDisposable
    {
        /// <summary>
        /// to detect redundant calls
        /// </summary>
        private bool disposed = false; 
        /// <summary>
        /// True if we are in a transaction.
        /// </summary>
        protected bool inTransaction = false;
        protected IfcPersistedInstanceCache cache;
        private XbimReadOnlyDBTransaction readTransaction;
        protected XbimEntityCursor cursor;

        protected XbimReadTransaction()
        {

        }

        internal XbimReadTransaction(IfcPersistedInstanceCache theCache)
        {
            cache = theCache;
            cache.GetEntityTable();
            cursor = cache.GetEntityTable(); //use a cursor to hold open an active session for the model
            readTransaction = cursor.BeginReadOnlyTransaction();
            inTransaction = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {                
                    if(inTransaction) readTransaction.Dispose();                    
                    cache.FreeTable(cursor); //release back for reuse the cursor
                    cache.ReleaseCache();
                }
                disposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
