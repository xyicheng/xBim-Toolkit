using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Xbim.SceneJSWebViewer
{
     /// <summary>
    /// Represents the geometric data of a piece of Geometry
    /// </summary>
    public class GeometryData
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public GeometryData()
        {
            this.ID = 0;
            this.HasData = 0;
            this.NumChildren = 0;
            this.data = new byte[] { };
            this.MatrixTransform = new Double[16];
        }

        /// <summary>
        /// Geometry Data Constructor
        /// </summary>
        /// <param name="id">ID of Geometry Item</param>
        /// <param name="pos">3d Positions Array</param>
        /// <param name="norm">Normal vectors Array</param>
        /// <param name="ind">Indices Array</param>
        public GeometryData(Int32 id, Byte[] binarystream, byte hasData, UInt16 NoOfChildren, Double[] matrix)
        {
            this.ID = id;
            this.HasData = hasData;
            this.NumChildren = NoOfChildren;
            this.data = binarystream;
            this.MatrixTransform = matrix;
        }

        /// <summary>
        /// Gets or sets ID of Geometry Item
        /// </summary>
        public Int32 ID
        {
            get;
            set;
        }

        public UInt16 NumChildren
        {
            get;
            set;
        }
        public Byte HasData
        {
            get;
            set;
        }

        public byte[] data { get; set; }

        /// <summary>
        /// Gets or sets the Transform Matrix
        /// </summary>
        public Double[] MatrixTransform
        {
            get;
            set;
        }
    }
}