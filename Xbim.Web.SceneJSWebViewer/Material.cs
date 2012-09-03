using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Xbim.SceneJSWebViewer
{
    /// <summary>
    /// Represents a Material in the model
    /// </summary>
    public class Material
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public Material()
        {
        }

        /// <summary>
        /// Constructor for Material
        /// </summary>
        /// <param name="name">Material Name</param>
        /// <param name="red">Red Value</param>
        /// <param name="green">Green Value</param>
        /// <param name="blue">Blue Value</param>
        /// <param name="alpha">Alpha Value</param>
        /// <param name="emit">Emit Value</param>
        public Material(String name, Double red, Double green, Double blue, Double alpha, Double emit)
        {
            this.Name = name;
            this.Red = red;
            this.Green = green;
            this.Blue = blue;
            this.Alpha = alpha;
            this.Emit = emit;
        }

        /// <summary>
        /// Gets or sets Red Value
        /// </summary>
        public Double Red
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Green Value
        /// </summary>
        public Double Green
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Blue Value
        /// </summary>
        public Double Blue
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Alpha Value
        /// </summary>
        public Double Alpha
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Emit Value
        /// </summary>
        public Double Emit
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Material Name
        /// </summary>
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return String.Format("R:{0} G:{1} B:{2} A:{3} E:{4}", Red, Green, Blue, Alpha, Emit);
        }
    }
}