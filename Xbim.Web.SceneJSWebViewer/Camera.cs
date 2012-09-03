using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Xbim.SceneJSWebViewer
{
    /// <summary>
    /// Represents a Camera looking at the model space
    /// </summary>
    public class Camera
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public Camera()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minx">model bounds</param>
        /// <param name="miny">model bounds</param>
        /// <param name="minz">model bounds</param>
        /// <param name="maxx">model bounds</param>
        /// <param name="maxy">model bounds</param>
        /// <param name="maxz">model bounds</param>
        public Camera(Double minx, Double miny, Double minz, Double maxx, Double maxy, Double maxz)
        {
            this.minX = minx;
            this.minY = miny;
            this.minZ = minz;
            this.maxX = maxx;
            this.maxY = maxy;
            this.maxZ = maxz;
        }

        /// <summary>
        /// Gets or sets model bounds
        /// </summary>
        public Double minX { get; set; }

        /// <summary>
        /// Gets or sets model bounds
        /// </summary>
        public Double minY { get; set; }

        /// <summary>
        /// Gets or sets model bounds
        /// </summary>
        public Double minZ { get; set; }

        /// <summary>
        /// Gets or sets model bounds
        /// </summary>
        public Double maxX { get; set; }

        /// <summary>
        /// Gets or sets model bounds
        /// </summary>
        public Double maxY { get; set; }

        /// <summary>
        /// Gets or sets model bounds
        /// </summary>
        public Double maxZ { get; set; }
    }
}