﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Xbim.COBie.Rows
{
    [Serializable()]
    public class COBieCoordinateRow : COBieRow
    {
        public COBieCoordinateRow(ICOBieSheet<COBieCoordinateRow> parentSheet)
            : base(parentSheet) { }

        [COBieAttributes(0, COBieKeyType.CompoundKey, "", COBieAttributeState.Required, "Name", 255, COBieAllowedType.AlphaNumeric)]
        public string Name { get; set; }

        [COBieAttributes(1, COBieKeyType.ForeignKey, "Contact.Email", COBieAttributeState.Required, "CreatedBy", 255, COBieAllowedType.Email)]
        public string CreatedBy { get; set; }

        [COBieAttributes(2, COBieKeyType.None, "", COBieAttributeState.Required, "CreatedOn", 19, COBieAllowedType.ISODate)]
        public string CreatedOn { get; set; }

        [COBieAttributes(3, COBieKeyType.CompoundKey_ForeignKey, "PickLists.CoordinateType", COBieAttributeState.Required, "Category", 255, COBieAllowedType.AlphaNumeric)]
        public string Category { get; set; }

        [COBieAttributes(4, COBieKeyType.CompoundKey_ForeignKey, "PickLists.CoordinateSheet", COBieAttributeState.Required, "SheetName", 255, COBieAllowedType.AlphaNumeric)]
        public string SheetName { get; set; }


        [COBieAttributes(5, COBieKeyType.CompoundKey, "", COBieAttributeState.Required, "RowName", 255, COBieAllowedType.AlphaNumeric)]
        public string RowName { get; set; }

        [COBieAttributes(6, COBieKeyType.None, "", COBieAttributeState.Required, "CoordinateXAxis", Constants.DOUBLE_MAXSIZE, COBieAllowedType.AlphaNumeric)]
        public string CoordinateXAxis { get; set; }

        [COBieAttributes(7, COBieKeyType.None, "", COBieAttributeState.Required, "CoordinateYAxis", Constants.DOUBLE_MAXSIZE, COBieAllowedType.AlphaNumeric)]
        public string CoordinateYAxis { get; set; }

        [COBieAttributes(8, COBieKeyType.None, "", COBieAttributeState.Required, "CoordinateZAxis", Constants.DOUBLE_MAXSIZE, COBieAllowedType.AlphaNumeric)]
        public string CoordinateZAxis { get; set; }

        [COBieAttributes(9, COBieKeyType.None, "", COBieAttributeState.System, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(10, COBieKeyType.None, "", COBieAttributeState.System, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject { get; set; }

        [COBieAttributes(11, COBieKeyType.None, "", COBieAttributeState.System, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier { get; set; }

        [COBieAttributes(12, COBieKeyType.None, "", COBieAttributeState.As_Specified, "ClockwiseRotation", 255, COBieAllowedType.AlphaNumeric)]
        public string ClockwiseRotation { get; set; }

        [COBieAttributes(13, COBieKeyType.None, "", COBieAttributeState.As_Specified, "ElevationalRotation", 255, COBieAllowedType.AlphaNumeric)]
        public string ElevationalRotation { get; set; }

        [COBieAttributes(14, COBieKeyType.None, "", COBieAttributeState.As_Specified, "YawRotation", 255, COBieAllowedType.AlphaNumeric)]
        public string YawRotation { get; set; }
    }
}