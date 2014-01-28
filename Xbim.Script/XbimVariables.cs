using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Interfaces;
using System.Text.RegularExpressions;

namespace Xbim.Script
{
    public class XbimVariables
    {

        private Dictionary<string, IEnumerable<IPersistIfcEntity>> _data = new Dictionary<string, IEnumerable<IPersistIfcEntity>>();
        private string _lastVariable = null;
        public string LastVariable { get { return _lastVariable; } }
        public IEnumerable<IPersistIfcEntity> LastEntities
        {
            get 
            {
                if (_lastVariable == null || !IsDefined(_lastVariable)) return new IPersistIfcEntity[] { };
                return this[_lastVariable];
            }
        }
        
        public IEnumerable<IPersistIfcEntity> GetEntities(string variable)
        {
            IEnumerable<IPersistIfcEntity> result = new IPersistIfcEntity[]{};
            _data.TryGetValue(variable, out result);
            return result;
        }

        public IEnumerable<string> GetVariables
        {
            get
            {
                return _data.Select(x => x.Key).AsEnumerable();
            }
        }


        public bool IsDefined(string variable)
        {
            return _data.ContainsKey(variable);
        }

        public void Set(string variable, IPersistIfcEntity entity)
        {
            if (entity == null && IsDefined(variable))
                Clear(variable);
            else
                Set(variable, new IPersistIfcEntity[] { entity });
        }

        public void Set(string variable, IEnumerable<IPersistIfcEntity> entities)
        {
            if (IsDefined(variable))
                _data[variable] = entities.ToList();
            else
                _data.Add(variable, entities.ToList());
            _lastVariable = variable;
        }

        public void AddEntities(string variable, IEnumerable<IPersistIfcEntity> entities)
        {
            if (IsDefined(variable))
            {
                _data[variable] = _data[variable].Union(entities.ToList());
            }
            else
                _data.Add(variable, entities.ToList());

            _lastVariable = variable;
        }

        public void RemoveEntities(string variable, IEnumerable<IPersistIfcEntity> entities)
        {
            if (IsDefined(variable))
            {
                _data[variable] = _data[variable].Except(entities.ToList());
            }
            else
                throw new ArgumentException("Can't remove entities from variable which is not defined.");

            _lastVariable = variable;
        }

        public IEnumerable<IPersistIfcEntity> this[string key]
        {
            get
            {
                return _data[key];
            }
        }

        public void Clear() 
        {
            _data.Clear();
        }

        public void Clear(string identifier)
        {
            if (IsDefined(identifier))
                _data[identifier] = new IPersistIfcEntity[] { };
            else
                throw new ArgumentException(identifier + " is not defined;");
        }


    }
}
