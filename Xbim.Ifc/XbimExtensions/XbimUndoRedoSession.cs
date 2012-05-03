#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    XbimUndoRedoSession.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using Xbim.XbimExtensions.Transactions;

#endregion

namespace Xbim.XbimExtensions
{
    public class XbimUndoRedoSession : UndoRedoSession
    {
        private IModel _model;

        public XbimUndoRedoSession(IModel model)
        {
            _model = model;
        }

        public new Transaction Begin(string operationName, bool useExisting)
        {
            if (Suspended || (useExisting && Current != null && Current != this))
                //cannot start a new transaction if things are suspended
                return null; //just use the exisitng transaction if it is ont the undoredo session
            Current = this;
            return Transaction.Begin(operationName);
        }


        /// <summary>
        ///   Makes this UndoRedoSession active and initiates a new transaction in it
        /// </summary>
        /// <param name = "operationName">Name of new Transaction.</param>
        /// <returns>A new Transaction</returns>
        /// <remarks>
        ///   When the returned transaction is commited or rollbacked,
        ///   this UndoSession will still the active Transaction.
        /// </remarks>
        public new Transaction Begin(string operationName)
        {
            if (Suspended) return null; //cannot start a new transaction if things are suspended
            Current = this;
            return Transaction.Begin(operationName);
        }

        /// <summary>
        ///   Makes this UndoRedoSession active and initiates a new transaction in it
        /// </summary>
        /// <returns></returns>
        public new Transaction Begin()
        {
            return this.Begin(null);
        }
    }
}