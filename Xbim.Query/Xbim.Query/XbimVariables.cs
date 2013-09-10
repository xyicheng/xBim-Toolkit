using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Interfaces;
using System.Text.RegularExpressions;

namespace Xbim.Query
{
    public class XbimVariables : Dictionary<string, IEnumerable<IPersistIfcEntity>>
    {
        public IEnumerable<IPersistIfcEntity> GetEntities(string variable)
        {
            IEnumerable<IPersistIfcEntity> result = null;
            TryGetValue(variable, out result);
            return result;
        }

        public bool IsDefined(string variable)
        {
            return ContainsKey(variable);
        }

        public void Set(string variable, IPersistIfcEntity entity)
        {
            if (entity == null) return;
            Set(variable, new IPersistIfcEntity[] { entity });
        }

        public void Set(string variable, IEnumerable<IPersistIfcEntity> entities)
        {
            if (IsDefined(variable))
                this[variable] = entities;
            else
                Add(variable, entities);
        }

        public void AddEntities(string variable, IEnumerable<IPersistIfcEntity> entities)
        {
            if (IsDefined(variable))
            {
                this[variable] = this[variable].Union(entities);
            }
            else
                Add(variable, entities);

        }

        public void RemoveEntities(string variable, IEnumerable<IPersistIfcEntity> entities)
        {
            if (IsDefined(variable))
            {
                this[variable] = this[variable].Except(entities);
            }
            else
                throw new ArgumentException("Can't remove entities from variable which is not defined.");

        }

    }
}
