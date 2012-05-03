#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcFileHeader.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.IO;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions.Parser;
using System.Reflection;

#endregion

namespace Xbim.XbimExtensions
{
    [Serializable]
    public class FileDescription : IPersistIfc, ExpressHeaderType
    {
        public FileDescription()
        {
        }

        public FileDescription(string implementationLevel)
        {
            ImplementationLevel = implementationLevel;
        }

        public List<string> Description = new List<string>(2);
        public string ImplementationLevel;
        public int EntityCount;

        #region ISupportIfcParser Members

        public void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    Description.Add(value.StringVal);
                    break;
                case 1:
                    ImplementationLevel = value.StringVal;
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }

        #endregion

        #region ISupportIfcParser Members

        public string WhereRule()
        {
            return "";
        }

        #endregion

        internal void Write(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(Description.Count);
            foreach (string desc in Description)
                binaryWriter.Write(desc);
            binaryWriter.Write(ImplementationLevel);
        }

        internal void Read(BinaryReader binaryReader)
        {
            int count = binaryReader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Description.Add(binaryReader.ReadString());
            }
            ImplementationLevel = binaryReader.ReadString();
        }
    }

    [Serializable]
    public class FileName : IPersistIfc, ExpressHeaderType
    {
        public FileName(DateTime time)
        {
            TimeStamp = string.Format("{0:0000}-{1:00}-{2:00}T{3:00}:{4:00}:{5:00}", time.Year, time.Month, time.Day,
                                      time.Hour, time.Minute, time.Second);
        }

        public FileName()
        {
            SetTimeStampNow();
        }

        public string Name;
        public string TimeStamp;

        public void SetTimeStampNow()
        {
            DateTime now = DateTime.Now;
            TimeStamp = string.Format("{0:0000}-{1:00}-{2:00}T{3:00}:{4:00}:{5:00}", now.Year, now.Month, now.Day,
                                      now.Hour, now.Minute, now.Second);
        }

        public List<string> AuthorName = new List<string>(2);
        public List<string> AuthorMailingAddress = new List<string>(6);
        public string PreprocessorVersion;
        public string OriginatingSystem;
        public string AuthorizationName = "";
        public List<string> AuthorizationMailingAddress = new List<string>(6);

        #region ISupportIfcParser Members

        public void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    Name = value.StringVal;
                    break;
                case 1:
                    TimeStamp = value.StringVal;
                    break;
                case 2:
                    AuthorName.Add(value.StringVal);
                    break;
                case 3:
                    AuthorMailingAddress.Add(value.StringVal);
                    break;
                case 4:
                    PreprocessorVersion = value.StringVal;
                    break;
                case 5:
                    OriginatingSystem = value.StringVal;
                    break;
                case 6:
                    AuthorizationName = value.StringVal;
                    break;
                case 7:
                    AuthorizationMailingAddress.Add(value.StringVal);
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }

        #endregion

        #region ISupportIfcParser Members

        public string WhereRule()
        {
            return "";
        }

        #endregion

        internal void Write(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(Name);
            binaryWriter.Write(TimeStamp);
            binaryWriter.Write(AuthorName.Count);
            foreach (string item in AuthorName)
                binaryWriter.Write(item);
            binaryWriter.Write(AuthorMailingAddress.Count);
            foreach (string item in AuthorMailingAddress)
                binaryWriter.Write(item);
            binaryWriter.Write(PreprocessorVersion);
            binaryWriter.Write(OriginatingSystem);
            binaryWriter.Write(AuthorizationName);
            binaryWriter.Write(AuthorizationMailingAddress.Count);
            foreach (string item in AuthorizationMailingAddress)
                binaryWriter.Write(item);
        }

        internal void Read(BinaryReader binaryReader)
        {
            Name = binaryReader.ReadString();
            TimeStamp = binaryReader.ReadString();
            int count = binaryReader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                AuthorName.Add(binaryReader.ReadString());
            }
            count = binaryReader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                AuthorMailingAddress.Add(binaryReader.ReadString());
            }
            PreprocessorVersion = binaryReader.ReadString();
            OriginatingSystem = binaryReader.ReadString();
            AuthorizationName = binaryReader.ReadString();
            count = binaryReader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                AuthorizationMailingAddress.Add(binaryReader.ReadString());
            }
        }
    }

    [Serializable]
    public class FileSchema : IPersistIfc, ExpressHeaderType
    {
        public List<string> Schemas = new List<string>();

        public FileSchema()
        {
        }

        public FileSchema(string version)
        {
            Schemas.Add(version);
        }

        #region ISupportIfcParser Members

        public void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    Schemas.Add(value.StringVal);
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }

        #endregion

        #region ISupportIfcParser Members

        public string WhereRule()
        {
            return "";
        }

        #endregion

        internal void Write(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(Schemas.Count);
            foreach (string item in Schemas)
                binaryWriter.Write(item);
        }

        internal void Read(BinaryReader binaryReader)
        {
            int count = binaryReader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Schemas.Add(binaryReader.ReadString());
            }
        }
    }

    [Serializable]
    public class IfcFileHeader
    {
        public FileDescription FileDescription = new FileDescription("2;1");
        public FileName FileName = new FileName(DateTime.Now)
                                        {
                                            PreprocessorVersion =
                                                string.Format("Xbim.Ifc File Processor version {0}",
                                                              Assembly.GetAssembly(typeof (P21Parser)).GetName().Version),
                                            OriginatingSystem =
                                                string.Format("Xbim version {0}",
                                                              Assembly.GetExecutingAssembly().GetName().Version),
                                        };
        public FileSchema FileSchema = new FileSchema("IFC2X3");


        public void Write(BinaryWriter binaryWriter)
        {
            FileDescription.Write(binaryWriter);
            FileName.Write(binaryWriter);
            FileSchema.Write(binaryWriter);
        }

        public void Read(BinaryReader binaryReader)
        {
            FileDescription.Read(binaryReader);
            FileName.Read(binaryReader);
            FileSchema.Read(binaryReader);
        }
    }
}