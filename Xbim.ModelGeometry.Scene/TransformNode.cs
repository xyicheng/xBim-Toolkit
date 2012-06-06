#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.ModelGeometry.Scene
// Filename:    TransformNode.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Media.Media3D;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.SharedBldgElements;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.ModelGeometry.Scene
{
    [Serializable]
    public class TransformNode
    {
        private Matrix3D _localMatrix;
        private HashSet<TransformNode> _children;
        private long _filePosition = -2;
        private long? _productId;
        private Rect3D _boundingBox;
        TransformGraph _transformGraph;

        //non stored data
        XbimTriangulatedModelStream _triangulatedModel;
        private bool _visible;
        private TransformNode _parent;

        public TransformGraph TransformGraph
        {
            get { return _transformGraph; }
            set { _transformGraph = value; }
        }

         
        public XbimTriangulatedModelStream TriangulatedModel
        {
            get
            {
                if (_triangulatedModel == null)
                {
                    _triangulatedModel = _transformGraph.Scene.Triangulate(this);
                }
                return _triangulatedModel;
            }
            set 
            {
                _triangulatedModel = value; 
            }
        }

        public Rect3D BoundingBox
        {
            get { return _boundingBox; }
            set 
            {
                _boundingBox = value;
                
            }
        }

        public long FilePosition
        {
            get { return _filePosition; }
            set { _filePosition = value; }
        }

        public long ProductId
        {
            get { return _productId ?? 0; }
            set { _productId = value; }
        }

        public bool HasGeometryModel
        {
            get { return _filePosition > 0; }
        }

       
        internal void Write(BinaryWriter binaryWriter)
        {
            if (_productId.HasValue)
                binaryWriter.Write(_productId.Value);
            else
                binaryWriter.Write(0L);
            _localMatrix.Write(binaryWriter);
            _boundingBox.Write(binaryWriter);
            binaryWriter.Write(_filePosition);
            if (_children == null)
                binaryWriter.Write((int) 0);
            else
            {
                binaryWriter.Write(_children.Count);
                foreach (var child in _children)
                    child.Write(binaryWriter);
            }
        }

        /// <summary>
        /// Reads a node and all the children recursively from the binary stream.
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="graph"></param>
        internal void Read(BinaryReader strm, TransformGraph graph)
        {
            _transformGraph = graph;
            _productId = strm.ReadInt64();
            if (_productId == 0) 
                _productId = null;
            else
                graph.ProductNodes.Add(_productId.Value, this);
            _localMatrix = _localMatrix.Read(strm);
            _boundingBox = _boundingBox.Read(strm);
            _filePosition = strm.ReadInt64();
            // Debug.WriteLine(string.Format("Stream beginning at: {0}.", _filePosition));
            int childCount = strm.ReadInt32();
            if (childCount > 0)
            {
                for (int i = 0; i < childCount; i++)
                {
                    TransformNode child = new TransformNode(_transformGraph);
                    child.Read(strm, graph);
                    AddChild(child);
                    child.Parent = this;
                }
            }
        }

        public bool Visible
        {
            get { return _visible; }
            set { _visible = value; }
        }



        public IEnumerable<TransformNode> Children
        {
            get { return _children ?? Enumerable.Empty<TransformNode>(); }
        }

        public int ChildCount
        {
            get { return _children == null ? 0 : _children.Count; }
        }




        public TransformNode( TransformGraph transformGraph)
        {
            _transformGraph = transformGraph;
        }

        public TransformNode(TransformGraph transformGraph, IfcProduct prod)
            : this(transformGraph)
        {
            if (prod != null)
            {
                _productId = prod.EntityLabel;
                
            }
        }

        public TransformNode(TransformGraph transformGraph, Matrix3D localMatrix)
            : this(transformGraph)
        {
            _localMatrix = localMatrix;
        }

        public IfcProduct Product
        {
            get
            {
                if (_productId.HasValue)
                {
                    IPersistIfcEntity ent = _transformGraph.Model.GetInstance(_productId.Value);
                    return ent as IfcProduct;
                }
                else
                    return null;
            }
            set { _productId = value.EntityLabel; }
        }

        public IfcProduct NearestProduct
        {
            get
            {
                if (!_productId.HasValue && _parent != null)
                    return _parent.NearestProduct;
                return Product;
            }
        }


        public TransformNode Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public void AddChild(TransformNode child)
        {
            if (_children == null) _children = new HashSet<TransformNode>();
            _children.Add(child);
            // child.Parent = this;
        }

        public void RemoveChild(TransformNode child)
        {
            if (_children == null)
            {
                _children.Remove(child);
            }
        }

        public Matrix3D WorldMatrix()
        {
            if (_parent != null)
                return _localMatrix*_parent.WorldMatrix();
            else
                return _localMatrix;
        }


        public Matrix3D LocalMatrix
        {
            get { return _localMatrix; }
            set { _localMatrix = value; }
        }


        public bool IsWindow
        {
            get
            {
                IfcProduct product = NearestProduct;
                if (product != null) return product is IfcWindow; else return false;
                    
            }
        }

    }
}