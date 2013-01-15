using glMatrix;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Xbim.Ifc.Kernel;
using Xbim.IO;
using Xbim.ModelGeometry;
using Xbim.ModelGeometry.Scene;
using OpenTK.Math;

namespace Xbim.Xplorer
{
    public partial class Xplorer : Form, IDisposable, IMessageFilter
    {
        bool GL_Loaded = false;
        XbimFileModelServer Scene = null;
        Stopwatch clock = new Stopwatch();
        UInt64 FrameTime = 0;
        OpenGLModel GLModel = new OpenGLModel();
        Camera cam = new Camera();
        Int32 ShaderProgramID = 0;
        int uProjectionMatLocation = 0;
        int uViewMatLocation = 0;
        int uNormalMatLocation = 0;
        int uMaterialLocation = 0;
        int uPickingLocation = 0;
        int uPositionLocation = 0;
        int uNormalLocation = 0;
        mat4 normalMatrix, transposedNormalMatrix;

        public Xplorer()
        {
            InitializeComponent();
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;

            normalMatrix = mat4.create();
            transposedNormalMatrix = mat4.create();

            Application.AddMessageFilter(this);
            this.FormClosed += (s, e) => Application.RemoveMessageFilter(this);
        }
        private Keys mLastKey = Keys.None;

        //filter for key repeats
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x100 || m.Msg == 0x104)
            {
                // Detect WM_KEYDOWN, WM_SYSKEYDOWN
                Keys key = (Keys)m.WParam.ToInt32();
                if (key != Keys.Control && key != Keys.Shift && key != Keys.Alt)
                {
                    if (key == mLastKey) return true;
                    mLastKey = key;
                }
            }
            else if (m.Msg == 0x101 || m.Msg == 0x105)
            {
                // Detect WM_UP, WM_SYSKEYUP
                Keys key = (Keys)m.WParam.ToInt32();
                if (key == mLastKey) mLastKey = Keys.None;
            }
            return false;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Ifc Files, xBim Files|*.ifc;*.xbim";
            DialogResult dr = ofd.ShowDialog();
            ofd.CheckFileExists = true;
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                OpenFile(ofd.FileName);
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "xBim File|.xbim";
            DialogResult dr = sfd.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                SaveFile(sfd.FileName);
            }
        }

        private void OpenFile(string p)
        {
            ClosePreviousModel();

            p = p.ToLowerInvariant();

            String ext = p.Split(new char[] { '.' }).Last();

            Scene = new XbimFileModelServer();
            switch (ext)
            {
                case "xbim":
                    Scene.Open(p);
                    break;
                case "ifc":
                    Scene = ParseModelFile(p, p.Replace(".ifc", ".xbim"), p.Replace(".ifc", ".xbimgc"));
                    break;
                default:
                    MessageBox.Show("Sorry, I don't know how to open that file type");
                    return;
            }

            SetupModelForViewingAsync(p);
        }

        private async void SetupModelForViewingAsync(string p)
        {
            if (Scene == null) return;
            var _model = new XbimSceneStream(Scene, p.Replace(".ifc", ".xbimgc"));


            Model_Loaded = true;
            //if model has a bounding box, we can use it here.

            await Task.Run(delegate() { ModelProcessor.ProcessModelAsync(_model, GLModel); });

            //ModelProcessor.ProcessModelAsync(_model, GLModel);

            box3 b = GLModel.modelbounds;
            SetCamera(b.Min.X, b.Max.X, b.Min.Y, b.Max.Y, b.Min.Z, b.Max.Z);


            MessageBox.Show("Model Loaded");
        }

        private XbimFileModelServer ParseModelFile(string IfcFileName, string xbimFileName, string xbimgcfilename)
        {
            XbimFileModelServer model = new XbimFileModelServer();
            switch (Path.GetExtension(IfcFileName).ToLowerInvariant())
            {
                case ".ifc":
                    model.ImportIfc(IfcFileName, xbimFileName, delegate(int percentProgress, object userState)
                    {
                        DisplayProgress(percentProgress);
                    });
                    GenerateGeometry(xbimgcfilename, model);
                    break;
                case ".xbim":
                    model.Open(xbimFileName);
                    break;

                case ".ifcxml":
                    model.ImportXml(IfcFileName, xbimFileName);
                    break;
                case ".ifczip":
                    model.ImportIfc(IfcFileName, xbimFileName, delegate(int percentProgress, object userState)
                    {
                        DisplayProgress(percentProgress);
                    });
                    break;
                default:
                    throw new NotImplementedException(String.Format("XbimConvert does not support {0} file formats currently", Path.GetExtension(IfcFileName)));
            }

            return model;
        }

        private void GenerateGeometry(string xbimGeometryFileName, XbimFileModelServer model)
        {
            //now convert the geometry

            IEnumerable<IfcProduct> toDraw = model.IfcProducts.Items;

            XbimScene scene = new XbimScene(model, toDraw, true);

            using (FileStream sceneStream = new FileStream(xbimGeometryFileName, FileMode.Create, FileAccess.ReadWrite))
            {
                BinaryWriter bw = new BinaryWriter(sceneStream);
                scene.Graph.Write(bw, delegate(int percentProgress, object userState)
                {
                    DisplayProgress(percentProgress);
                });
                bw.Flush();
            }
        }

        public delegate void DisplayProgressDelegate(Int32 val);

        private void DisplayProgress(int percentProgress)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.BeginInvoke(new DisplayProgressDelegate(DisplayProgress), new object[] { percentProgress });
            }
            else
            {
                progressBar1.Value = percentProgress;
            }
        }

        private void ClosePreviousModel()
        {
            if (Scene != null)
            {
                Scene.Close();
                Scene.Dispose();
            }
        }

        private void SaveFile(string p)
        {
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GL_Loaded = true;
            clock.Start();
            InitGL();
            Application.Idle += Application_Idle;

        }

        private void InitGL()
        {
            if (!GL_Loaded) return;

            //Create our Projection Matrix and setup the viewport
            projectionMat = mat4.identity();

            GL.ClearColor(Color.White);
            GL.ClearDepth(1.0f);

            ErrorCheck.Check();

            this.frustummin = 1.0f;
            this.frustummax = 4096f;

            SetupViewport();

            //Compile our shaders
            String vSource = ShaderLoader.LoadShader("vertexshader", ShaderType.VertexShader);
            String fSource = ShaderLoader.LoadShader("fragmentshader", ShaderType.FragmentShader);
            String log = String.Empty;
            ShaderProgramID = ShaderLoader.BuildShaders(vSource, fSource, out log);

            ErrorCheck.Check();

            //Setup the various uniform locations
            uProjectionMatLocation = GL.GetUniformLocation(ShaderProgramID, "projectionMat");
            uViewMatLocation = GL.GetUniformLocation(ShaderProgramID, "viewMat");
            uNormalMatLocation = GL.GetUniformLocation(ShaderProgramID, "normalMat");
            uMaterialLocation = GL.GetUniformLocation(ShaderProgramID, "material");
            uPickingLocation = GL.GetUniformLocation(ShaderProgramID, "picking");
            uPositionLocation = GL.GetAttribLocation(ShaderProgramID, "position");
            uNormalLocation = GL.GetAttribLocation(ShaderProgramID, "normal");

            ErrorCheck.Check();

            string infoString = string.Empty;
            int statusCode = 1;

            GL.GetProgram(ShaderProgramID, ProgramParameter.ValidateStatus, out statusCode);
            GL.GetProgramInfoLog(ShaderProgramID, out infoString);

            if (statusCode != 1)
            {
                MessageBox.Show("Error validating the shader: " + infoString);
            }

            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GLPanel.Invalidate();
        }

        private void SetupViewport()
        {
            int w = GLPanel.Width;
            int h = GLPanel.Height;
            GL.Viewport(0, 0, w, h); // Use all of the glControl painting area
            mat4.perspective(fov, ((float)w / (float)h), frustummin, frustummax, projectionMat);
            ErrorCheck.Check();
        }

        private void Accumulate(double milliseconds)
        {
            idleCounter++;
            accumulator += milliseconds;
            if (accumulator > 1000)
            {
                label1.Text = idleCounter.ToString() + " FPS";
                accumulator -= 1000;
                idleCounter = 0; // don't forget to reset the counter!
            }
        }

        void Application_Idle(object sender, EventArgs e)
        {
            Render();
        }

        private void Render()
        {
            if (!GL_Loaded)
                return;

            //get frame time
            clock.Stop();
            var time = (UInt64)clock.Elapsed.TotalMilliseconds;
            clock.Restart();
            FrameTime += time;
            Accumulate(time);

            cam.update(time);

            mat4 viewMat = cam.getViewMat();
            mat4 projMat = this.projectionMat;
            mat4.inverse(viewMat, this.normalMatrix);
            mat4.transpose(this.normalMatrix, this.transposedNormalMatrix);

            GL.UseProgram(ShaderProgramID);

            ErrorCheck.Check();

            GL.UniformMatrix4(this.uViewMatLocation, 1, false, (Single[])viewMat);
            ErrorCheck.Check();
            GL.UniformMatrix4(this.uProjectionMatLocation, 1, false, (Single[])projMat);
            ErrorCheck.Check();
            GL.UniformMatrix4(this.uNormalMatLocation, 1, false, (Single[])this.transposedNormalMatrix);

            ErrorCheck.Check();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            ErrorCheck.Check();

            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);

            if (Model_Loaded)
                GLModel.Draw(time, this.uPositionLocation, this.uNormalLocation, this.uMaterialLocation, this.uPickingLocation);

            GLPanel.SwapBuffers();
        }

        private void Render(object sender, PaintEventArgs e)
        {
            Render();
        }

        private void GLPanel_Resize(object sender, EventArgs e)
        {
            SetupViewport();
            GLPanel.Invalidate();
        }

        public int idleCounter { get; set; }

        public double accumulator { get; set; }

        private void GLPanel_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            cam.KeyDown(e.KeyCode);
        }

        private void GLPanel_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            cam.KeyUp(e.KeyCode);
        }

        private void GLPanel_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            switch (e.Button)
            {
                case System.Windows.Forms.MouseButtons.Left:
                    cam.MouseDown(e.X, e.Y, MouseButton.Left);
                    break;
                case System.Windows.Forms.MouseButtons.Middle:
                    cam.MouseDown(e.X, e.Y, MouseButton.Middle);
                    break;
                case System.Windows.Forms.MouseButtons.Right:
                    cam.MouseDown(e.X, e.Y, MouseButton.Right);
                    break;
            }
        }

        private void GLPanel_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            cam.MouseMove(e.X, e.Y);
        }

        private void GLPanel_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            switch (e.Button)
            {
                case System.Windows.Forms.MouseButtons.Left:
                    cam.MouseUp(e.X, e.Y, MouseButton.Left);
                    break;
                case System.Windows.Forms.MouseButtons.Middle:
                    cam.MouseUp(e.X, e.Y, MouseButton.Middle);
                    break;
                case System.Windows.Forms.MouseButtons.Right:
                    cam.MouseUp(e.X, e.Y, MouseButton.Right);
                    break;
            }
        }

        private void SetCamera(Double minX, Double maxX, Double minY, Double maxY, Double minZ, Double maxZ)
        {
            var eyez = Math.Max(maxX - minX, Math.Max(maxY - minY, maxZ - minZ));

            //setup a sensible modifier for how much we move
            Single moveMod = (Single)(eyez / 650);

            var aspect = GLPanel.Width / GLPanel.Height;
            var fovy = fov;

            var centerX = minX + ((maxX - minX) / 2);
            var centerY = minY + ((maxY - minY) / 2);
            var centerZ = minZ + ((maxZ - minZ) / 2);

            //calc set zoom
            var theta = (fovy / 2);
            var opposite = eyez / 2;
            Single setZoom = (Single)(opposite / (Math.Tan(theta * cam.DEG2RAD)));
            //setZoom is now the distance from camera to edge of model, but we need to take from center of model, so we add max-center
            setZoom += (Single)(maxY - centerY);

            //setup camera to account for frustrums, and reset the aspect ratio
            frustummin = setZoom / 1000;

            frustummax = setZoom * 10;
            cam.moveMod = moveMod;

            SetupViewport();

            //setup camera so it is looking at the central point
            cam.SetZoomExtents(centerX, centerY, centerZ, setZoom);

            cam.ZoomExtents();
        }

        public mat4 projectionMat { get; set; }

        public float frustummin { get; set; }

        public float frustummax { get; set; }

        public int fov { get { return 65; } }

        void IDisposable.Dispose()
        {
            GLModel.Dispose();
        }

        public bool Model_Loaded = false;
    }
}
