#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    XbimPropertyMetaData.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;

#endregion

namespace Xbim.XbimExtensions
{
    [FlagsAttribute]
    public enum XbimPropertyMetaDataOptions : short
    {
        Unset = 0,
        Affects2DAppearance = 1
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class XbimPropertyMetaData : Attribute
    {
        private XbimPropertyMetaDataOptions PropertyMetaDataOptions;

        public bool Affects2DAppearance
        {
            get
            {
                return ((PropertyMetaDataOptions & XbimPropertyMetaDataOptions.Affects2DAppearance) ==
                        XbimPropertyMetaDataOptions.Affects2DAppearance);
            }
            set
            {
                if (value)
                    PropertyMetaDataOptions |= XbimPropertyMetaDataOptions.Affects2DAppearance;
                else
                    PropertyMetaDataOptions ^= XbimPropertyMetaDataOptions.Affects2DAppearance;
            }
        }
    }
}