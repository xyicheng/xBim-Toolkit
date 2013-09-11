using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Ast;
using Irony.Interpreter.Ast;
using Irony.Parsing;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Querying.Nodes
{
    public class SelectRoot : AstNode
    {
        ParseTreeNode ElementList = null;
        protected override object DoEvaluate(Irony.Interpreter.ScriptThread thread)
        {
            List<IPersistIfcEntity> returnVal = new List<IPersistIfcEntity>();
            thread.CurrentNode = this;  //standard prolog
            XbimModel m = thread.App.Globals["model"] as XbimModel;
            if (m != null)
            {
                // todo: is there a way to make this lazy with yield?
                
                foreach (var child in ElementList.ChildNodes)
                {
                    string tk = child.FindTokenAndGetText();
                    int iEl = -1;
                    if (Int32.TryParse(tk, out iEl))
                    {
                        returnVal.Add(m.Instances[iEl]);
                    }
                    else if (tk == @"*")
                    {
                        returnVal.AddRange(m.Instances);
                    }
                    else
                    {
                        returnVal.AddRange(m.Instances.OfType(tk, false));
                    }
                }
            }
            thread.CurrentNode = Parent; //standard epilog
            return returnVal;
        }

        // this is where data is initially passed to the item
        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();
            if (nodes.Count == 2)
            {
                // todo: consider model 

                if (nodes[1].AstNode != null)
                {
                    ElementList = nodes[1];
                    // AddChild(string.Empty, nodes[1]);
                    // this.AsString += "(child children: " + child.ChildNodes.Count);
                }
            }
        }

        // fairly default behaviour
        public override void SetIsTail()
        {
            base.SetIsTail();
            if (ChildNodes.Count > 0)
                ChildNodes[ChildNodes.Count - 1].SetIsTail();
        }
    }
}
