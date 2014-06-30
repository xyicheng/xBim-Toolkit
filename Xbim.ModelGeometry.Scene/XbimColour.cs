using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.Ifc2x3.PresentationResource;

namespace Xbim.ModelGeometry.Scene
{

    /// <summary>
    /// Represents a Colour in the model
    /// </summary>
    [DataContract]
    public class XbimColour
    {
        public static XbimColour LightGrey = new XbimColour("LightGrey", 0.47, 0.53, 0.60, 1);
        /// <summary>
        /// Gets or sets Colour Name, defaults to its parts
        /// </summary>
        [DataMember]
        public String Name
        {
            get { if (string.IsNullOrWhiteSpace(_name)) return ""; else  return _name; }
            set { _name = value; }
        }

        public override bool Equals(object obj)
        {
           
            XbimColour col = obj as XbimColour;
            if (col == null) return false;
            return  col.Red == Red && 
                    col.Green == Green && 
                    col.Blue == Blue && 
                    col.Alpha == Alpha && 
                    col.DiffuseFactor == DiffuseFactor && 
                    col.TransmissionFactor == TransmissionFactor &&
                    col.DiffuseTransmissionFactor == DiffuseTransmissionFactor && 
                    col.ReflectionFactor == ReflectionFactor && 
                    col.SpecularFactor == SpecularFactor;    
        }

        public override int GetHashCode()
        {
            return Red.GetHashCode() ^ Green.GetHashCode() ^ Blue.GetHashCode() ^ Alpha.GetHashCode() ^ DiffuseFactor.GetHashCode() ^
                TransmissionFactor.GetHashCode() ^ DiffuseTransmissionFactor.GetHashCode() ^ ReflectionFactor.GetHashCode() ^ SpecularFactor.GetHashCode();
        }

        /// <summary>
        /// True if the cuolour is not opaque
        /// </summary>
        [IgnoreDataMember]
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
        /// <param name="red">Red component Value (range 0 to 1.0 inclusive)</param>
        /// <param name="green">Green Value (range 0 to 1.0 inclusive)</param>
        /// <param name="blue">Blue Value (range 0 to 1.0 inclusive)</param>
        /// <param name="alpha">Alpha Value (range 0 to 1.0 inclusive)</param>
        public XbimColour(String name, float red, float green, float blue, float alpha = 1.0f)
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
        [DataMember(Name = "Red")]
        public float Red
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Green Value
        /// </summary>
        [DataMember(Name="Green")]
        public float Green
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Blue Value
        /// </summary>
        [DataMember(Name="Blue")]
        public float Blue
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Alpha Value
        /// </summary>
        [DataMember(Name="Alpha")]
        public float Alpha
        {
            get;
            set;
        }
        
        private String _name;
        [DataMember(Name="DF")]
        public float DiffuseFactor;
        [DataMember(Name = "TF")]
        public float TransmissionFactor;
        [DataMember(Name = "DTF")]
        public float DiffuseTransmissionFactor;
        [DataMember(Name = "RF")]
        public float ReflectionFactor;
        [DataMember(Name = "SF")]
        public float SpecularFactor;

        

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return String.Format("R:{0} G:{1} B:{2} A:{3} DF:{4} TF:{5} DTF:{6} RF:{7} SF:{8}", Red, Green, Blue, Alpha,
                DiffuseFactor, TransmissionFactor, DiffuseTransmissionFactor, ReflectionFactor, SpecularFactor);
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
            this.Alpha = 1;
           // this.Name = string.Format("{0},A={1},D={2},S={3},T={4},R={5},RGB={6}", rgbColour.Name, Alpha, DiffuseFactor, SpecularFactor, TransmissionFactor, ReflectionFactor, rgbColour.EntityLabel);
            
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
        
        [IgnoreDataMember]
        public static XbimColour Default 
        {
            get
            {
                return _default;
            }

        }



    }
}

