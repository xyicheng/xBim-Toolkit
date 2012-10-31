using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.IO
{
    /// <summary>
    /// A transaction allowing read and write operations on a model
    /// </summary>
    public class XbimReadWriteTransaction : XbimReadTransaction
    {
        private XbimLazyDBTransaction readWriteTransaction;

        internal XbimReadWriteTransaction(XbimModel theModel, XbimLazyDBTransaction txn)
        {
            model = theModel;
            readWriteTransaction = txn;
            inTransaction = true;
        }

        public void Commit()
        {
            try
            {
                model.Flush();
                readWriteTransaction.Commit();
            }
            finally
            {
                inTransaction = false;
            }
        }


        protected override void Dispose(bool disposing)
        {
            try
            {
                if (inTransaction) readWriteTransaction.Dispose();
            }
            finally
            {
                inTransaction = false;
                base.Dispose(disposing);
            }
        }

        public IEnumerable<IPersistIfcEntity> Modified()
        {
            return model.Cache.Modified();
        }
    }
}
