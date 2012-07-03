#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.ModelGeometry.Scene
// Filename:    TransformGraph.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media.Media3D;
using Xbim.IO;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.GeometricConstraintResource;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.RepresentationResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.ModelGeometry.Scene
{
    [Serializable]
    public enum TransformGraphStyle
    {
        /// <summary>
        ///   Constructs a transform graph of only triangulated surfaces
        /// </summary>
        Triangulated,

        /// <summary>
        ///   Constructs a transform graph of faces made of a lst of ordered points
        /// </summary>
        PolyList
    }

    [Serializable]
    public class TransformGraph
    {
        private TransformNode _root;
        private Dictionary<long, TransformNode> _productNodes = new Dictionary<long, TransformNode>();
      
        //Primitives
        [NonSerialized] private CoPlanarComparer _coPlanarComparer = new CoPlanarComparer(1);

        private Dictionary<IfcObjectPlacement, TransformNode> _placementNodes =
            new Dictionary<IfcObjectPlacement, TransformNode>();

        private List<TransformNode> _mappedNodes = new List<TransformNode>();
        private IXbimScene _scene;

        internal IXbimScene Scene
        {
            get { return _scene; }
            set { _scene = value;}
        }


        [NonSerialized]
        private IModel _model;

        public IModel Model
        {
            get { return _model; }
          
        }
       
        public Dictionary<long, TransformNode> ProductNodes
        {
            get { return _productNodes; }
            internal set { _productNodes = value; }
        }

        public void Close()
        {
            _model.Close();
        }

        public bool ReOpen()
        {
            
            return _model.ReOpen();
        }


        /// <summary>
        ///   Converts all solid geoemetry to a tesselated model and stores on the stream
        /// </summary>
        /// <param name = "strm"></param>
        /// <returns></returns>
        public void Write(BinaryWriter strm)
        {
            strm.Write((long) -1);
            foreach (var node in _productNodes.Values)
            {
                node.FilePosition = strm.BaseStream.Position;
                node.TriangulatedModel.Write(strm);
                node.TriangulatedModel = null; //delete data to save memory, can always get back fro the stream later
            
            }
            long pos = strm.BaseStream.Position;
            _root.Write(strm);
            strm.BaseStream.Seek(0, SeekOrigin.Begin);
            strm.Write(pos);
        }


        public void Read(BinaryReader strm)
        {
            strm.BaseStream.Seek(0, SeekOrigin.Begin);
            long offset = strm.ReadInt64();
            strm.BaseStream.Seek(offset, SeekOrigin.Begin);
            _root.Read(strm, this);
        }

 


        public Dictionary<IfcObjectPlacement, TransformNode> PlacementNodes
        {
            get { return _placementNodes; }
        }

        public IEnumerable<TransformNode> Nodes
        {
            get { return _placementNodes.Values.Concat(_mappedNodes); }
        }

        [NonSerialized] private Dictionary<IfcRepresentation, TransformNode> _primitives =
            new Dictionary<IfcRepresentation, TransformNode>();
       
       

        public Dictionary<IfcRepresentation, TransformNode> Primitives
        {
            get { return _primitives; }
        }


        public TransformGraph(IModel model)
        {
            _model = model;
            _root = new TransformNode(this);
        }
        
        public TransformGraph(IModel model, IXbimScene engine)
        {
            _model = model;
            _scene = engine;
            _root = new TransformNode(this);
        }

        public TransformNode Root
        {
            get { return _root; }
        }

        public TransformNode this[IfcProduct product]
        {
            get
            {
                IfcObjectPlacement pl = product.ObjectPlacement;
                if (pl != null)
                {
                    TransformNode node;
                    if (_placementNodes.TryGetValue(pl, out node))
                        return node;
                }
                return null;
            }
        }

        public IList<TransformNode> AddProducts<TProduct>(IEnumerable<TProduct> products) where TProduct : IfcProduct
        {
            List<TransformNode> added = new List<TransformNode>();
            foreach (IfcProduct product in products)
            {
                TransformNode node = AddProduct(product);
                if (node != null) added.Add(node);
            }
            return added;
        }


        public TransformNode AddProduct(IfcProduct product)
        {
            Debug.Assert(product != null);
            TransformNode node = AddNode(product.ObjectPlacement, product);
           
            return node;
        }

        private TransformNode AddNode(IfcObjectPlacement placement, IfcProduct product)
        {
            IfcLocalPlacement lp = placement as IfcLocalPlacement;
            IfcGridPlacement gp = placement as IfcGridPlacement;
            if (gp != null) throw new NotImplementedException("GridPlacement is not implemented");
            if (lp != null)
            {
                Debug.Assert(placement != null);
                TransformNode node;
                if (!_placementNodes.TryGetValue(placement, out node))
                {
                    node = new TransformNode(this, product);
                    if (product != null) _productNodes.Add(product.EntityLabel, node);
                    IfcAxis2Placement3D ax3 = lp.RelativePlacement as IfcAxis2Placement3D;
                    if (ax3 != null) node.LocalMatrix = ax3.ToMatrix3D();
                    else
                    {
                        IfcAxis2Placement2D ax2 = lp.RelativePlacement as IfcAxis2Placement2D;
                        if (ax2 != null) node.LocalMatrix = ax2.ToMatrix3D();
                    }

                    _placementNodes.Add(placement, node);
                    if (lp.PlacementRelTo != null)
                    {
                        TransformNode parent;
                        if (_placementNodes.TryGetValue(lp.PlacementRelTo, out parent))
                            //we have already processed parent
                        {
                            parent.AddChild(node);
                            node.Parent = parent;
                        }
                        else //make sure placement tree is created
                        {
                            parent = AddNode(lp.PlacementRelTo, null);
                            parent.AddChild(node);
                            node.Parent = parent;
                        }
                    }
                    else //it is in world coordinate system just add it
                    {
                        _root.AddChild(node);
                        node.Parent = _root;
                    }
                    return node;
                }
                else // we might have created the node as a placement parent but not set the product yet
                {
                    if (product != null && node.Product == null) //don't add a product twice
                    {
                        node.Product = product;
                        _productNodes.Add(product.EntityLabel, node);
                        return node;
                    }
                }
            }
            return null;
        }


      

        private TransformNode AddMappedNode(Matrix3D transform)
        {
            TransformNode node = new TransformNode(this, transform);
            _mappedNodes.Add(node);
            return node;
        }

       

        public Matrix3D ModelTransform { get; set; }

        public Point3D PerspectiveCameraPosition { get; set; }

        public Vector3D PerspectiveCameraLookDirection { get; set; }

        public double PerspectiveCameraFOV { get; set; }

        public Vector3D PerspectiveCameraUpDirection { get; set; }
    }
}