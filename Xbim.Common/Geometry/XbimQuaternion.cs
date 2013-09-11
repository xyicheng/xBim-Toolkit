using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.Common.Geometry
{
    public struct XbimQuaternion
    {
        #region members

        private static readonly XbimQuaternion _identity;
        private float _x;
        private float _y;
        private float _z;
        private float _w;
        private bool _isNotDefaultInitialised;

        public float X
        {
            get
            {
                if (!_isNotDefaultInitialised) this = _identity;
                return _x;
            }
            set
            {
                if (!_isNotDefaultInitialised) this = _identity;
                this._x = value;
            }
        }
        public float Y
        {
            get
            {
                if (!_isNotDefaultInitialised) this = _identity;
                return _y;
            }
            set
            {
                if (!_isNotDefaultInitialised) this = _identity;
                this._y = value;
            }
        }
        public float Z
        {
            get
            {
                if (!_isNotDefaultInitialised) this = _identity;
                return _z;
            }
            set
            {
                if (!_isNotDefaultInitialised) this = _identity;
                this._z = value;
            }
        }
        public float W
        {
            get
            {
                if (!_isNotDefaultInitialised) this = _identity;
                return _w;
            }
            set
            {
                if (!_isNotDefaultInitialised) this = _identity;
                this._w = value;
            }
        }
        #endregion

        static XbimQuaternion()
        {
            _identity = new XbimQuaternion(0.0f, 0.0f, 0.0f, 1.0f);
            _identity._isNotDefaultInitialised = true;
        }

        public XbimQuaternion(Single x, Single y, Single z, Single w)
        {
            this._x = x;
            this._y = y;
            this._z = z;
            this._w = w;
            this._isNotDefaultInitialised = false;
        }

        public XbimQuaternion(double x, double y, double z, double w)
        {
            this._x = (float)x;
            this._y = (float)y;
            this._z = (float)z;
            this._w = (float)w;
            this._isNotDefaultInitialised = false;
        }      
    }
}
