using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.COBie.COBieExtensions
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

    public enum CopyOfCOBieAttributeState
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
        AnyType
    }

    public enum COBieKeyType
    {
        PrimaryKey,
        CompoundKey,
        ForeignKey,
        CompoundKey_ForeignKey,
        None
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class COBieAttributes : Attribute
    {
        private readonly COBieAttributeState _state;
        private int _maxLength;
        private COBieAllowedType _allowedType;
        private readonly int _order;
        private string _columnName;
        private COBieKeyType _keyType;

        public COBieAttributeState State
        {
            get { return _state; }
        }

        public string ColumnName
        {
            get { return _columnName; }
        }

        public int MaxLength
        {
            get { return _maxLength; }
        }

        public COBieAllowedType AllowedType
        {
            get { return _allowedType; }
        }

        public COBieKeyType KeyType
        {
            get { return _keyType; }
        }

        public int Order
        {
            get { return _order; }
        }

        public COBieAttributeState COBieAttributeState
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
            }
        }

        public COBieAllowedType COBieAllowedType
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
            }
        }

        public COBieAttributes(COBieAttributeState state)
        {
            _state = state;
        }

        public COBieAttributes(int order, COBieKeyType keyType, COBieAttributeState state, string columnName, int maxLength, COBieAllowedType allowedType)
        {
            _state = state;
            _maxLength = maxLength;
            _allowedType = allowedType;
            _order = order;
            _columnName = columnName;
            _keyType = keyType;
        }
    }
}
