using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;

namespace Xbim.IO.Tree
{
    public class TreeBuilder
    {
        private IModel _model;
        private TreeQueryStrategy _strategy;

        public TreeBuilder(IModel model, TreeQueryStrategy strategy)
        {
            _model = model;
            _strategy = strategy;
        }

        public TreeNodes BuildTreeStructure()
        {
            return _strategy.GetTreeStructure();
        }
    }
}
