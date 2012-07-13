#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    ModelManager.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.XbimExtensions.Transactions;
using Xbim.XbimExtensions.Transactions.Extensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.XbimExtensions
{
    /// <summary>
    /// Static class to hold functions shared by all models
    /// </summary>
    public static class ModelHelper
    {
      
        /// <summary>
        ///   Set a property /field value, if a transaction is active it is transacted and undoable, if the owner supports INotifyPropertyChanged, the required events will be raised
        /// </summary>
        /// <typeparam name = "TProperty"></typeparam>
        /// The property type to be set
        /// <param name = "field"></param>
        /// The field to be set
        /// <param name = "newValue"></param>
        /// The value to set the field to
        /// <param name = "setter"></param>
        /// The function to set and unset the field
        /// <param name = "notifyPropertyName"></param>
        /// A list of property names of the owner to raise notification on
        internal static void SetModelValue<TProperty>(IPersistIfcEntity target, ref TProperty field, TProperty newValue,
                                                      ReversibleInstancePropertySetter<TProperty> setter,
                                                      string notifyPropertyName)
        {
            //The object must support Property Change Notification so notify
            ISupportChangeNotification iPropChanged = target as ISupportChangeNotification;

            if (iPropChanged != null)
            {
                Transaction.AddPropertyChange(setter, field, newValue);               
                target.Activate(true);
                iPropChanged.NotifyPropertyChanging(notifyPropertyName);
                field = newValue;
                iPropChanged.NotifyPropertyChanged(notifyPropertyName);
            }
            else
                throw new Exception(
                    string.Format(
                        "Request to Notify Property Changes on type {0} that does not support ISupportChangeNotification",
                        target.GetType().Name));
        }
    }
}