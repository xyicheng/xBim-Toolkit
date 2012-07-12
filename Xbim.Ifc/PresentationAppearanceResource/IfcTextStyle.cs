#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcTextStyle.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.PresentationAppearanceResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcTextStyle : IfcPresentationStyle, IfcPresentationStyleSelect
    {
        #region Fields

        private IfcCharacterStyleSelect _textCharacterAppearance;
        private IfcTextStyleSelect _textStyle;
        private IfcTextFontSelect _textFontStyle;

        #endregion

        [IfcAttribute(2, IfcAttributeState.Optional)]
        public IfcCharacterStyleSelect TextCharacterAppearance
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _textCharacterAppearance;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _textCharacterAppearance, value, v => TextCharacterAppearance = v,
                                           "TextCharacterAppearance");
            }
        }

        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcTextStyleSelect TextStyleVal
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _textStyle;
            }
            set { ModelManager.SetModelValue(this, ref _textStyle, value, v => TextStyleVal = v, "TextStyleVal"); }
        }

        [IfcAttribute(4, IfcAttributeState.Optional)]
        public IfcTextFontSelect TextFontStyle
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _textFontStyle;
            }
            set { ModelManager.SetModelValue(this, ref _textFontStyle, value, v => TextFontStyle = v, "TextFontStyle"); }
        }

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    base.IfcParse(propIndex, value);
                    break;
                case 1:
                    _textCharacterAppearance = (IfcCharacterStyleSelect) value.EntityVal;
                    break;
                case 2:
                    _textStyle = (IfcTextStyleSelect) value.EntityVal;
                    break;
                case 3:
                    _textFontStyle = (IfcTextFontSelect) value.EntityVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        public override string WhereRule()
        {
            return "";
        }
    }
}