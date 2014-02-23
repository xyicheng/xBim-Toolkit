using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.ModelGeometry.Scene;

namespace Xbim.WeXplorer.MVC4.Helpers
{
    public class BaseMaterial
    {
        public String MaterialID;
        public String Name;
        public Single Red;
        public Single Green;
        public Single Blue;
        public Single Alpha;
        public Single Diffusion;
        public Single Emit;
        public Single Specular;
        public UInt16 Priority;

        public byte[] GetData() { 
            UTF8Encoding encoder = new UTF8Encoding();
            byte[] btemp = encoder.GetBytes((Name));

            List<byte> temp = new List<Byte>(36 + btemp.Length);
            //temp.AddRange(BitConverter.GetBytes(MaterialID));
            temp.AddRange(BitConverter.GetBytes((UInt32)btemp.Length));
            temp.AddRange(btemp);
            temp.AddRange(BitConverter.GetBytes(Red));
            temp.AddRange(BitConverter.GetBytes(Green));
            temp.AddRange(BitConverter.GetBytes(Blue));
            temp.AddRange(BitConverter.GetBytes(Alpha));
            temp.AddRange(BitConverter.GetBytes(Diffusion));
            temp.AddRange(BitConverter.GetBytes(Emit));
            temp.AddRange(BitConverter.GetBytes(Specular));
            temp.AddRange(BitConverter.GetBytes(Priority));

            return temp.ToArray();
        }
    }
    public class XbimWebMaterial : IXbimRenderMaterial
    {

        #region Properties

        private List<BaseMaterial> Materials { get; set; }
        string _Description;

        public int Count
        {
            get { return Materials.Count; }
        }

        /// <summary>
        /// Return the first material
        /// </summary>
        public BaseMaterial Material
        {
            get
            {
                if (Materials.Count >= 1)
                    return Materials.First();
                else
                    return null;
            }
        }

        public string Description
        {
            get
            {
                return _Description;
            }
        }

        public BaseMaterial SolidMaterial
        {
            get { return Materials.Where(m => m.Diffusion == 0 && m.Emit == 0 && m.Specular == 0).FirstOrDefault(); }
        }

        public BaseMaterial DiffuseMaterial
        {
            get { return Materials.Where(m => m.Diffusion > 0).FirstOrDefault(); }
        }

        public BaseMaterial EmissiveMaterial
        {
            get { return Materials.Where(m => m.Emit > 0).FirstOrDefault(); }
        }

        public BaseMaterial SpecularMaterial
        {
            get { return Materials.Where(m => m.Specular > 0).FirstOrDefault(); }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public XbimWebMaterial()
        {
            Materials = new List<BaseMaterial>();
            _Description = "";
        }

        public void Add(BaseMaterial mat)
        {
            if (mat != null)
            {
                Materials.Add(mat);
            }

        }


        #endregion

        #region IXbimRenderMaterial Implementation

        public void CreateMaterial(XbimTexture texture)
        {
            if (texture.ColourMap.Count > 1)
            {
                _Description = "Texture";

                foreach (var colour in texture.ColourMap)
                {
                    BaseMaterial mat = RGBAExists(colour);
                    _Description += " " + colour.ToString();
                    if (mat == null)
                    {
                        Materials.Add(CreateMaterial(colour));
                    }
                    else
                    {
                        EditMaterial(mat, colour);
                    }
                }
            }
            else if (texture.ColourMap.Count == 1)
            {
                XbimColour colour = texture.ColourMap[0];
                Materials.Add(CreateMaterial(colour));
            }
        }

        /// <summary>
        /// See if existing RGBA colour exists in Materials
        /// </summary>
        /// <param name="colour">XbimColour</param>
        /// <returns>Material</returns>
        private BaseMaterial RGBAExists(XbimColour colour)
        {
            return Materials.Where(c => DoubleFuzzEq(c.Red, colour.Red) && DoubleFuzzEq(c.Blue, colour.Blue) && DoubleFuzzEq(c.Green, colour.Green) && DoubleFuzzEq(c.Alpha, colour.Alpha)).FirstOrDefault();
        }



        /// <summary>
        /// Double check for equal within a tolerance
        /// </summary>
        /// <param name="num1">double</param>
        /// <param name="num2">double</param>
        /// <returns>bool</returns>
        private static bool DoubleFuzzEq(double num1, double num2)
        {
            double tol = 0.000000001;
            double diff = Math.Abs(num1 * tol);
            return (Math.Abs(num1 - num2) <= diff);
        }

        private BaseMaterial CreateMaterial(XbimColour colour)
        {

            string name = colour.Name;
            //we need a name so make from values
            if (string.IsNullOrEmpty(name))
            {
                name = colour.Red.ToString() + "," + colour.Green.ToString() + "," + colour.Blue.ToString() + "," + colour.Alpha.ToString() + ","
                       + colour.SpecularFactor.ToString() + "," + colour.ReflectionFactor.ToString() + "," + colour.DiffuseFactor.ToString();
            }
            _Description = "Colour " + name;

            Int32 id = 0;
            BaseMaterial mat = new BaseMaterial
            {
                Name = name,
                MaterialID = id.ToString(),
                Red = colour.Red,
                Green = colour.Green,
                Blue = colour.Blue,
                Alpha = colour.Alpha
            };
            if (colour.SpecularFactor > 0)
                mat.Specular = colour.SpecularFactor * 100; //not sure on 100, copy from WpfMaterial class so might need adjusting
            if (colour.ReflectionFactor > 0)
                mat.Emit = colour.ReflectionFactor;
            if (colour.DiffuseFactor > 0)
                mat.Diffusion = colour.DiffuseFactor;
            return mat;

        }

        private BaseMaterial EditMaterial(BaseMaterial mat, XbimColour colour)
        {

            if (colour.SpecularFactor > 0)
                mat.Specular = colour.SpecularFactor * 100; //not sure on 100, copy from WpfMaterial class so might need adjusting
            if (colour.ReflectionFactor > 0)
                mat.Emit = colour.ReflectionFactor;
            if (colour.DiffuseFactor > 0)
                mat.Diffusion = colour.DiffuseFactor;
            return mat;
        }
        internal void SetPriority(ushort Priority)
        {
            foreach (BaseMaterial m in this.Materials)
                m.Priority = Priority;
        }
        public bool IsCreated
        {
            get { return Count > 0; }
        }
        #endregion
    }
}
