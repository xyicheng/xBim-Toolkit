using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.Kernel;
using Xbim.XbimExtensions;

namespace Xbim.IO.Tree
{
    public class TreeComponentStrategy : TreeQueryStrategy
    {
        public TreeComponentStrategy(IModel model)
        {
            _model = model;
        }

        public override TreeNodes GetTreeStructure()
        {
            return GetComponentStructure();
        }

        /// <summary>
        /// Groups all elements by their Family Type
        /// </summary>
        /// <returns></returns>
        private TreeNodes GetComponentStructure()
        {
            TreeNodes tree = new TreeNodes();

            var types = from t in FamilyTypes orderby t.Name select t;

            foreach (Type type in types)
            {
                FamilyNode family = new FamilyNode();
                family.Name = type.Name;

                LoadFamilyElements(type, family);

                tree.Add(family);
            }

            return tree;
        }
    }
}
