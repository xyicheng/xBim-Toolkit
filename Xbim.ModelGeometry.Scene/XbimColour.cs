using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.ModelGeometry.Scene
{

    /// <summary>
    /// Represents a Colour in the model
    /// </summary>
    public class XbimColour
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public XbimColour()
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
        public XbimColour(String name, float red, float green, float blue, float alpha = 1, float emit = 0)
        {
            this.Name = name;
            this.Red = red;
            this.Green = green;
            this.Blue = blue;
            this.Alpha = alpha;
            this.Emit = emit;
        }

        public XbimColour(String name, double red, double green, double blue, double alpha = 1, double emit = 0)
            : this(name, (float)red, (float)green, (float)blue, (float)alpha, (float)emit)
        {
        }

        /// <summary>
        /// Gets or sets Red Value
        /// </summary>
        public float Red
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Green Value
        /// </summary>
        public float Green
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Blue Value
        /// </summary>
        public float Blue
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Alpha Value
        /// </summary>
        public float Alpha
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Emit Value
        /// </summary>
        public float Emit
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

        static XbimColour _default;

        static XbimColour()
        {
            _default = new XbimColour("Default", 1, 1, 1);
        }

        public static XbimColour Default 
        {
            get
            {
                return _default;
            }

        }



    }
}

