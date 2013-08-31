using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QUT.Xbim.Gppg;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Query
{
    internal partial class XbimQueryParser
    {
        private XbimModel _model;
        private XbimVariables _variables = new XbimVariables();
        public XbimVariables Variables { get { return _variables; } }

        internal XbimQueryParser(Scanner lex, XbimModel model): base(lex)
        {
            _model = model;
            if (_model == null) throw new ArgumentNullException("Model is NULL");
        }

        private IPersistIfcEntity CreateObject(Type type, string name, string description = null)
        {
            if (_model == null) throw new ArgumentNullException("Model is NULL");
            if (name == null) throw new ArgumentNullException("Name must be defined");

            var entity = _model.Instances.New(type);
            var root = entity as IfcRoot;
            if (root != null)
            {
                root.Name = name;
                root.Description = description;
            }
            return root;
        }


    }
}
