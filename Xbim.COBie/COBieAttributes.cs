﻿using System;

namespace Xbim.COBie
{

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