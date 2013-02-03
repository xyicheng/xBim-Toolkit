using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.Ifc2x3.PresentationResource;

namespace Xbim.ModelGeometry.Scene
{

    /// <summary>
    /// Represents a Colour in the model
    /// </summary>
    public class XbimColour
    {
        /// <summary>
        /// True if the cuolour is not opaque
        /// </summary>
        public bool IsTransparent
        {
            get
            {
                return Alpha < 1;
            }
        }
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
        public XbimColour(String name, float red, float green, float blue, float alpha = 1)
        {
            this.Name = name;
            this.Red = red;
            this.Green = green;
            this.Blue = blue;
            this.Alpha = alpha;
           
        }

        public XbimColour(String name, double red, double green, double blue, double alpha = 1.0)
            : this(name, (float)red, (float)green, (float)blue, (float)alpha)
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

        public float DiffuseFactor;
        public float TransmissionFactor;
        public float DiffuseTransmissionFactor;
        public float ReflectionFactor;
        public float SpecularFactor;

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
            return String.Format("R:{0} G:{1} B:{2} A:{3}", Red, Green, Blue, Alpha);
        }

        static XbimColour _default;
       
        
        static XbimColour()
        {
            _default = new XbimColour("Default", 1, 1, 1);
        }

        public XbimColour(IfcSurfaceStyle style)
        {
           
        }
        internal XbimColour(IfcColourRgb rgbColour)
        {
           
            this.Red = (float)(double)rgbColour.Red;
            this.Green = (float)(double)rgbColour.Green;
            this.Blue = (float)(double)rgbColour.Blue;
            this.Name=rgbColour.Name;
            
        }

        public XbimColour(IfcColourRgb ifcColourRgb, double opacity = 1.0, double diffuseFactor = 1.0 , double specularFactor = 0.0, double transmissionFactor = 1.0, double reflectanceFactor = 0.0)
            :this(ifcColourRgb)
        {
            this.Alpha = (float)opacity;
            this.DiffuseFactor = (float)diffuseFactor;
            this.SpecularFactor = (float)specularFactor;
            this.TransmissionFactor = (float)transmissionFactor;
            this.ReflectionFactor = (float)reflectanceFactor;
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

