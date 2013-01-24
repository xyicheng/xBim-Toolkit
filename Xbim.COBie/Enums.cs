﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.COBie
{
    public enum COBieAttributeState
    {
                                            // Responsibility Matrix key
        Required_PrimaryKey = 0,            // RP
        Required_CompoundKeyPart = 1,       // RC
        Required_Information = 2,           // RI

        Required_Reference_PrimaryKey = 3,  // RS
        Required_Reference_ForeignKey = 4,  // RF
        Required_Reference_PickList = 5,    // RL

        Required_System = 6,                // RA

        Required_IfSpecified = 7            // RS (again)
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
