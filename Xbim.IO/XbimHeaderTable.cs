using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;
using System.IO;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.IO
{
    public class XbimHeaderTable : IDisposable
    {

        Session _jetSession;
        JET_DBID _jetDatabaseId;

        private const string ifcHeaderTableName = "IfcHeader";

        private const string _colNameHeaderData = "HeaderData";
        private const string _colNameHeaderId = "HeaderId";

        private Table _jetCursor;

        private JET_COLUMNID _colIdHeaderId;
        private JET_COLUMNID _colIdHeaderData;

        Int32ColumnValue _colValHeaderId;
        BytesColumnValue _colValHeaderData;

        /// <summary>
        /// Constructs and opens a header table
        /// </summary>
        /// <param name="jetSession"></param>
        /// <param name="jetDatabaseId"></param>
        /// <param name="mode"></param>
        public XbimHeaderTable(Session jetSession, JET_DBID jetDatabaseId, OpenTableGrbit mode)
        {
            _jetSession = jetSession;
            _jetDatabaseId = jetDatabaseId;
            Open(mode);
        }
        /// <summary>
        /// Constructs but does not open a header table
        /// </summary>
        /// <param name="jetSession"></param>
        /// <param name="jetDatabaseId"></param>
        public XbimHeaderTable(Session jetSession, JET_DBID jetDatabaseId)
        {
            _jetSession = jetSession;
            _jetDatabaseId = jetDatabaseId;
        }
        internal static void CreateTable(Session session, JET_DBID dbid)
        {
            JET_TABLEID tableid;
            Api.JetCreateTable(session, dbid, ifcHeaderTableName, 1, 100, out tableid);

            using (var transaction = new Microsoft.Isam.Esent.Interop.Transaction(session))
            {
                JET_COLUMNID columnid;
                var columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.LongBinary,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };

                Api.JetAddColumn(session, tableid, _colNameHeaderData, columndef, null, 0, out columnid);
                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.Long,
                    grbit = ColumndefGrbit.ColumnAutoincrement
                };
                Api.JetAddColumn(session, tableid, _colNameHeaderId, columndef, null, 0, out columnid);

                transaction.Commit(CommitTransactionGrbit.LazyFlush);
            }
        }

        public void Open(OpenTableGrbit mode)
        {
            _jetCursor = new Table(_jetSession, _jetDatabaseId, ifcHeaderTableName, mode);
            InitColumns();
        }

        private void InitColumns()
        {
            IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(_jetSession, _jetCursor);
            _colIdHeaderId = columnids[_colNameHeaderId];
            _colIdHeaderData = columnids[_colNameHeaderData];

            _colValHeaderId = new Int32ColumnValue { Columnid = _colIdHeaderId };
            _colValHeaderData = new BytesColumnValue { Columnid = _colIdHeaderData };
           

        }
        public void Close()
        {
            Api.JetCloseTable(_jetSession, _jetCursor);
            _jetCursor = null;
        }

        public void Dispose()
        {
            if(_jetCursor!=null) Close();
        }

        public IfcFileHeader IfcFileHeader
        {
            get
            {
                Api.JetSetCurrentIndex(_jetSession, _jetCursor, null);
                Api.TryMoveFirst(_jetSession, _jetCursor); //this should never fail
                byte[] hd = Api.RetrieveColumn(_jetSession, _jetCursor, _colIdHeaderData);
                BinaryReader br = new BinaryReader(new MemoryStream(hd));
                IfcFileHeader hdr = new IfcFileHeader();
                hdr.Read(br);
                return hdr;
            }
        }

        internal void UpdateHeader(IIfcFileHeader ifcFileHeader)
        {
            MemoryStream ms = new MemoryStream(4096);
            BinaryWriter bw = new BinaryWriter(ms);
            ifcFileHeader.Write(bw);
            if (!Api.TryMoveFirst(_jetSession, _jetCursor)) //there is nothing in
            {
                using (var update = new Update(_jetSession, _jetCursor, JET_prep.Insert))
                {
                    Api.SetColumn(_jetSession, _jetCursor, _colIdHeaderData, ms.ToArray());
                    update.Save();
                }
            }
            else
            {
                using (var update = new Update(_jetSession, _jetCursor, JET_prep.Replace))
                {
                    Api.SetColumn(_jetSession, _jetCursor, _colIdHeaderData, ms.ToArray());
                    update.Save();
                }
            }
        }
    }

}
