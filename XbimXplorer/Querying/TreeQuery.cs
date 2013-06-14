using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;

namespace XbimXplorer
{
    static class QueryEngine
    {
        static public List<int> EntititesForType(string type, XbimModel Model)
        {
            List<int> Values = new List<int>();
            var items = Model.Instances.OfType(type, false);
            foreach (var item in items)
            {
                int thisV = Math.Abs(item.EntityLabel);
                if (!Values.Contains(thisV))
                    Values.Add(thisV);
            }
            Values.Sort();
            return Values;
        }

        static public IEnumerable<int> RecursiveQuery(XbimModel Model, string Query, IEnumerable<int> StartList)
        {
            var proparray = Query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            IEnumerable<int> runningList =  StartList;
            foreach (var StringQuery in proparray)
            {
                TreeQueryItem qi = new TreeQueryItem(runningList, StringQuery);
                runningList = qi.Run(Model);   
            }
            TreeQueryItem qi2 = new TreeQueryItem(runningList, "");
            runningList = qi2.Run(Model);
            foreach (var item in runningList)
            {
                yield return item;    
            }
        }
    }

    public class TreeQueryItem
    {
        private IEnumerable<int> _EntityLabelsToParse;
        private String _QueryCommand;

        public TreeQueryItem(IEnumerable<int> labels, string Query)
        {
            _QueryCommand = Query;
            _EntityLabelsToParse = labels;
        }

        public IEnumerable<int> Run(XbimModel Model)
        {
            foreach (var label in _EntityLabelsToParse)
            {
                if (_QueryCommand.Trim() == "")
                    yield return label;
                var entity = Model.Instances[label];
                if (entity != null)
                {
                    IfcType ifcType = IfcMetaData.IfcType(entity);
                    var prop = ifcType.IfcProperties.Where(x => x.Value.PropertyInfo.Name == _QueryCommand).FirstOrDefault().Value;
                    if (prop != null)
                    {
                        object propVal = prop.PropertyInfo.GetValue(entity, null);
                        if (propVal != null)
                        {
                            if (prop.IfcAttribute.IsEnumerable)
                            {
                                IEnumerable<object> propCollection = propVal as IEnumerable<object>;
                                if (propCollection != null)
                                {
                                    foreach (var item in propCollection)
                                    {
                                        IPersistIfcEntity pe = propVal as IPersistIfcEntity;
                                        yield return pe.EntityLabel;
                                    }
                                }
                            }
                            else
                            {
                                IPersistIfcEntity pe = propVal as IPersistIfcEntity;
                                yield return pe.EntityLabel;
                            }
                        }
                    }
                }
            }
        }
    }
}
