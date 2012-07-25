#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.IO
// Filename:    XbimModelServer.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#if SupportActivation

#region Directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml;
using Xbim.Ifc2x3.ActorResource;
using Xbim.Ifc2x3.DateTimeResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.UtilityResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.DataProviders;
using Xbim.XbimExtensions.Transactions;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using Xbim.XbimExtensions.Interfaces;
using Xbim.IO.Parser;

#endregion

namespace Xbim.IO
{
    public abstract class XbimModelServer : XbimModel
    {
       

        

        /// <summary>
        ///   Removes any references in the entity properties that are in the entity Label list, returns true if the stream has been modified
        /// </summary>
        protected bool DerefenceEntities(byte[] propertyStream, HashSet<ulong> entityLabels, out byte[] derefencedStream)
        {
            bool modified = false;
            derefencedStream = new byte[propertyStream.Length];
            BinaryReader br = new BinaryReader(new MemoryStream(propertyStream));
            P21ParseAction action = (P21ParseAction)br.ReadByte();

            BinaryWriter bw = new BinaryWriter(new MemoryStream(derefencedStream));

            while (action != P21ParseAction.EndEntity)
            {
                switch (action)
                {
                    case P21ParseAction.BeginList:
                        bw.Write((byte)P21ParseAction.BeginList);
                        break;
                    case P21ParseAction.EndList:
                        bw.Write((byte)P21ParseAction.EndList);
                        break;
                    case P21ParseAction.BeginComplex:
                        break;
                    case P21ParseAction.EndComplex:
                        break;
                    case P21ParseAction.SetIntegerValue:
                        bw.Write((byte)P21ParseAction.SetIntegerValue);
                        bw.Write(br.ReadInt64());
                        break;
                    case P21ParseAction.SetHexValue:
                        bw.Write((byte)P21ParseAction.SetHexValue);
                        bw.Write(br.ReadInt64());
                        break;
                    case P21ParseAction.SetFloatValue:
                        bw.Write((byte)P21ParseAction.SetFloatValue);
                        bw.Write(br.ReadDouble());
                        break;
                    case P21ParseAction.SetStringValue:
                        bw.Write((byte)P21ParseAction.SetStringValue);
                        bw.Write(br.ReadString());
                        break;
                    case P21ParseAction.SetEnumValue:
                        bw.Write((byte)P21ParseAction.SetEnumValue);
                        bw.Write(br.ReadString());
                        break;
                    case P21ParseAction.SetBooleanValue:
                        bw.Write((byte)P21ParseAction.SetBooleanValue);
                        bw.Write(br.ReadBoolean());
                        break;
                    case P21ParseAction.SetNonDefinedValue:
                        bw.Write((byte)P21ParseAction.SetNonDefinedValue);
                        break;
                    case P21ParseAction.SetOverrideValue:
                        bw.Write((byte)P21ParseAction.SetOverrideValue);
                        break;
                    case P21ParseAction.SetObjectValueUInt16:
                        ushort label16 = br.ReadUInt16();
                        if (entityLabels.Contains(label16))
                        {
                            bw.Write((byte)P21ParseAction.SetNonDefinedValue);
                            modified = true;
                        }
                        else
                        {
                            bw.Write((byte)P21ParseAction.SetObjectValueUInt16);
                            bw.Write(label16);
                        }
                        break;
                    case P21ParseAction.SetObjectValueUInt32:
                        uint label32 = br.ReadUInt32();
                        if (entityLabels.Contains(label32))
                        {
                            bw.Write((byte)P21ParseAction.SetNonDefinedValue);
                            modified = true;
                        }
                        else
                        {
                            bw.Write((byte)P21ParseAction.SetObjectValueUInt32);
                            bw.Write(label32);
                        }
                        break;
                    case P21ParseAction.SetObjectValueInt64:
                        ulong label64 = br.ReadUInt64();
                        if (entityLabels.Contains(label64))
                        {
                            bw.Write((byte)P21ParseAction.SetNonDefinedValue);
                            modified = true;
                        }
                        else
                        {
                            bw.Write((byte)P21ParseAction.SetObjectValueInt64);
                            bw.Write(label64);
                        }
                        break;
                    case P21ParseAction.BeginNestedType:
                        bw.Write((byte)P21ParseAction.BeginNestedType);
                        bw.Write(br.ReadString());
                        break;
                    case P21ParseAction.EndNestedType:
                        bw.Write((byte)P21ParseAction.EndNestedType);
                        break;
                    case P21ParseAction.EndEntity:
                        bw.Write((byte)P21ParseAction.EndEntity);
                        break;
                    case P21ParseAction.NewEntity:
                        bw.Write((byte)P21ParseAction.NewEntity);
                        break;
                }
                action = (P21ParseAction)br.ReadByte();
            }
            bw.Write((byte)P21ParseAction.EndEntity);
            bw.Flush();
            return modified;
        }

       

      



        #region IDisposable Members

       

        #endregion

        #region IModel Members

      

        protected IPersistIfcEntity CreateEntity(long label, Type type)
        {
            IPersistIfcEntity entity = (IPersistIfcEntity)Activator.CreateInstance(type);
            entity.Bind(this, (label * -1));
            return entity;
        }


       

        private void TransactionReversed()
        {

        }

        protected abstract void TransactionFinalised();

        #endregion

        #region Access Properties

        #endregion





    }
}

#endif