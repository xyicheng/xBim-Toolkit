using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.COBie
{
    public enum COBieAttributeState
    {
        Required,
        Reference_OtherSheet_Or_PickList,
        Reference_External,
        Reference_Specified,
        Secondary_Information,
        System,
        As_Specified,
        Notes,
        None
    }

    public enum COBieAllowedType
    {
        AlphaNumeric,
        Email,
        ISODate,
        Numeric,
        Text,
        AnyType,
        ISODateTime
    }

    public enum COBieKeyType
    {
        PrimaryKey,
        CompoundKey,
        ForeignKey,
        CompoundKey_ForeignKey,
        None
    }
}
