using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Xbim.SceneJSWebViewer
{
    /// <summary>
    /// Represents a Header for a type of Geometry
    /// </summary>
    public class GeometryHeader
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public GeometryHeader()
        {
            this.Geometries = new List<String>();
        }

        /// <summary>
        /// Constructs a Geometry Label
        /// </summary>
        /// <param name="type">Name of the Type</param>
        /// <param name="mat">Material Name</param>
        /// <param name="geom">Geometry IDs which are of this type and material</param>
        public GeometryHeader(String type, String mat, Int16 layerPriority, List<String> geom)
        {
            this.Type = type;
            this.Material = mat;
            this.LayerPriority = layerPriority;
            this.Geometries = geom;
        }

        /// <summary>
        /// Constructs a Geometry Label
        /// </summary>
        /// <param name="type">Name of the Type</param>
        /// <param name="mat">Material Name</param>
        /// <param name="geom">Geometry IDs which are of this type and material (as an Array)</param>
        public GeometryHeader(String type, String mat, Int16 layerPriority, String[] geom)
        {
            this.Type = type;
            this.Material = mat;
            this.LayerPriority = layerPriority;
            this.Geometries = new List<String>(geom);
        }

        /// <summary>
        /// Gets or sets Name of the Type
        /// </summary>
        public String Type
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Name of the Type
        /// </summary>
        public Int16 LayerPriority
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Material Name
        /// </summary>
        public String Material
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets  Geometry IDs which are of this type and material
        /// </summary>
        public List<String> Geometries
        {
            get;
            set;
        }
    }
}