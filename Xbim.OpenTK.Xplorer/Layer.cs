using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Xbim.Xplorer
{
    public class Layer : IDisposable
    {
        private static Int32 count = 1;

        public UInt32 Priority { get; set; }
        public Color Material { get; set; }
        private List<GLGeometry> Children = new List<GLGeometry>();

        private System.Boolean pauseRender = false;
        private UInt32[] VBOid = new UInt32[2];
        private bool dirty = false;
        private bool HaveBuffers = false;
        private Int32 id, DrawCount;

        public Layer()
        {
            id = count++;
        }

        /// <summary>
        /// Adds a single piece of geometry and resets the buffers to account for new piece
        /// </summary>
        /// <param name="geo">the piece of geometry to add</param>
        internal void AddGeometry(GLGeometry geo)
        {
            pauseRender = true;
            lock (Children)
            {
                Children.Add(geo);
            }
            dirty = true;
            pauseRender = false;
        }

        /// <summary>
        /// Adds a batch of geometry and resets the buffers to account for new pieces
        /// </summary>
        /// <param name="geos">the list of geometries to add</param>
        internal void AddGeometry(List<GLGeometry> geos)
        {
            pauseRender = true;
            lock (Children)
            {
                Children.AddRange(geos);
            }
            dirty = true;
            pauseRender = false;
        }

        /// <summary>
        /// Rebuilds the VBOs with the current position/normal data for the Layer
        /// </summary>
        void Resetbuffers()
        {
            if (!HaveBuffers)
            {
                try
                {
                    GL.GenBuffers(2, VBOid);
                    HaveBuffers = true;
                }
                catch (Exception ex) { MessageBox.Show(ex.ToString()); }

                ErrorCheck.Check();

            }
            //create the list of data to buffer
            List<Single> positions = new List<float>();
            List<Single> normals = new List<float>();

            Int32 PositionIndex = 0;
            lock (Children)
            {
                foreach (GLGeometry g in Children)
                {
                    if (g != null)
                    {
                        g.StartIndex = PositionIndex;
                        positions.AddRange(g.Positions);
                        normals.AddRange(g.Normals);
                        PositionIndex += g.Positions.Count;
                    }
                }
            }

            SetVBOData(VBOid[0], positions.ToArray());
            SetVBOData(VBOid[1], normals.ToArray());
            DrawCount = positions.Count / 3;

            dirty = false;
        }

        /// <summary>
        /// Creates a VBO and fills it with the specified data
        /// </summary>
        /// <param name="data">The data to fill the VBO with</param>
        /// <returns>The ID of the buffer location</returns>
        uint SetVBOData(UInt32 id, Single[] data)
        {
            Int32 length = (data.Length * sizeof(Single));

            ErrorCheck.Check();

            GL.BindBuffer(BufferTarget.ArrayBuffer, id);

            ErrorCheck.Check();

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(length), data, BufferUsageHint.DynamicDraw);

            ErrorCheck.Check();

            //check the buffer to make sure it matches expectations
            Int32 bufferSize;
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out bufferSize);
            if (length != bufferSize)
            {
                MessageBox.Show("Vertex array not uploaded correctly");
                return 0;
            }

            ErrorCheck.Check();

            //reset binding so we can do work elsewhere
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            ErrorCheck.Check();
            return id;
        }

        /// <summary>
        /// Binds the current context to our VBOs for this Layer
        /// </summary>
        /// <param name="pointid">location in the shader for our points</param>
        /// <param name="normalid">location in the shader for our normals</param>
        void BindBuffers(Int32 pointid, Int32 normalid)
        {

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.NormalArray);

            ErrorCheck.Check();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOid[0]);
            ErrorCheck.Check();
            GL.VertexPointer(3, VertexPointerType.Float, 0, IntPtr.Zero);
            ErrorCheck.Check();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOid[1]);
            ErrorCheck.Check();
            GL.NormalPointer(NormalPointerType.Float, 0, IntPtr.Zero);
            ErrorCheck.Check();

            if (Material.A < 255)
            {
                //GL.Disable(EnableCap.DepthTest);
                GL.DepthMask(false);
                GL.Enable(EnableCap.Blend);
            } 
            else 
            {
                //GL.Enable(EnableCap.DepthTest);
                GL.DepthMask(true);
                GL.Disable(EnableCap.Blend);
            }
        }

        void UnbindBuffers(Int32 pointid, Int32 normalid)
        {
            ErrorCheck.Check();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOid[0]);
            GL.DisableVertexAttribArray(pointid);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOid[1]);
            GL.DisableVertexAttribArray(normalid);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.NormalArray);
        }
        /// <summary>
        /// Draw the layer. Called in the render loop
        /// </summary>
        /// <param name="pointid">location in the shader for our points</param>
        /// <param name="normalid">location in the shader for our normals</param>
        /// <param name="materialid">location in the shader for our RGBA Material</param>
        public void Draw(Int32 pointid, Int32 normalid, Int32 materialid, Int32 pickingid)
        {
            if (dirty)
            {
                Resetbuffers();
            }

            BindBuffers(pointid, normalid);

            ErrorCheck.Check();
            GL.Uniform4(materialid, this.Material);
            
            ErrorCheck.Check();

            if (DrawCount > 0)
            {
                ErrorCheck.Check();
                if (this.Picked || this.Highlighted)
                {
                    foreach (GLGeometry g in this.Children)
                    {
                        if (g.Picked)
                        {
                            GL.Uniform1(pickingid, 1);
                        }
                        else if (g.Highlighted)
                        {
                            GL.Uniform1(pickingid, 2);
                        }
                        else {
                            GL.Uniform1(pickingid, 0);
                        }

                        GL.DrawArrays(BeginMode.Triangles, g.StartIndex, g.Positions.Count);
                    }
                } else {
                    GL.Uniform1(pickingid, 0);
                    GL.DrawArrays(BeginMode.Triangles, 0, DrawCount);
                }
                ErrorCheck.Check();
            }

            UnbindBuffers(pointid, normalid);
        }

        public void Dispose()
        {
            ErrorCheck.Check();
            GL.DeleteBuffers(2, VBOid);
            ErrorCheck.Check();
        }

        public bool Picked { get; set; }

        public bool Highlighted { get; set; }
    }
}
