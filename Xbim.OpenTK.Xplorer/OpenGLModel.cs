using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using glMatrix;

namespace Xbim.Xplorer
{
    public class OpenGLModel : IDisposable
    {
        ConcurrentDictionary<String, Layer> Layers = new ConcurrentDictionary<string, Layer>();

        public box3 modelbounds = box3.create();

        public void Draw(ulong time, Int32 positionlocation, Int32 normallocation, Int32 uMaterialLocation, Int32 uPickLocation)
        {
            ICollection<Layer> layers = Layers.Values;
            layers.OrderBy(x => x.Priority);
            foreach (Layer l in layers)
            {
                l.Draw(positionlocation, normallocation, uMaterialLocation, uPickLocation);
            }
        }

        public void AddGeometry(GLGeometry geo, String layid)
        {
            Layer l = Layers[layid];
            if (l == null)
            {
                l = new Layer();
                if (!Layers.TryAdd(layid, l)) MessageBox.Show("Failed to Add Geo ID " + geo.EntityLabel);
            }

            l.AddGeometry(geo);
        }

        internal bool HasMaterial(string p)
        {
            Layer l = null;
            return Layers.TryGetValue(p, out l);
        }

        internal void AddMaterial(Color c, String Name, UInt32 Priority)
        {
            Layer l = null;
            if (!Layers.TryGetValue(Name, out l))
            {
                l = new Layer();
                if (!Layers.TryAdd(Name, l)) MessageBox.Show("Failed to Add Material " + Name);
            }
            l.Material = c;
            l.Priority = Priority;
        }

        public void Dispose()
        {
            foreach (Layer l in Layers.Values)
            {
                l.Dispose();
            }
        }
    }
}
