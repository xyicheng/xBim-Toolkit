using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.IO
{
    /// <summary>
    /// A transaction allowing read and write operations on a model
    /// </summary>
    public class XbimReadWriteTransaction : XbimReadTransaction
    {
        private XbimLazyDBTransaction readWriteTransaction;

        public XbimReadWriteTransaction(IfcPersistedInstanceCache theCache)
        {
            cache = theCache;
            cache.GetEntityTable();
            cursor = cache.GetEntityTable(); //use a cursor to hold open an active session for the model
            readWriteTransaction = cursor.BeginLazyTransaction();
        }

        public void Commit()
        {
            readWriteTransaction.Commit();
        }
        protected override void Dispose(bool disposing)
        {
            readWriteTransaction.Dispose();
            base.Dispose(disposing);
        }
    }
}
