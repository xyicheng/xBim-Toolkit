using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Xbim.Xplorer
{
    public class ShaderLoader
    {
        public static String LoadShader(String name, ShaderType type)
        {
            String extension = String.Empty;
            switch(type)
            {
                case ShaderType.FragmentShader:
                    extension = ".frag";
                    break;
                case ShaderType.VertexShader:
                    extension = ".vert";
                    break;
                default:
                    break;
            }

            if (File.Exists(name + extension))
            {
                String s = String.Empty;
                using (StreamReader sr = new StreamReader(name + extension))
                {
                    s = sr.ReadToEnd();
                }
                return s;
            }
            else
            {
                throw new ArgumentException();
            }
        }
        public static Int32 BuildShaders(String vertexShaderSource, String fragmentShaderSource, out string log)
        {
            Int32 fragmentShaderHandle, vertexShaderHandle, shaderProgramHandle;
            CreateShaders(vertexShaderSource, fragmentShaderSource, out vertexShaderHandle, out fragmentShaderHandle);
            CreateProgram(vertexShaderHandle, fragmentShaderHandle, out shaderProgramHandle, out log);
            return shaderProgramHandle;
        }
        public static void CreateShaders(String vertexShaderSource, String fragmentShaderSource, out Int32 vertexShaderHandle, out Int32 fragmentShaderHandle)
        {
            vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
            fragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);

            ErrorCheck.Check();

            GL.ShaderSource(vertexShaderHandle, vertexShaderSource);
            GL.ShaderSource(fragmentShaderHandle, fragmentShaderSource);

            ErrorCheck.Check();

            GL.CompileShader(vertexShaderHandle);
            GL.CompileShader(fragmentShaderHandle);

            ErrorCheck.Check();
        }
        public static void CreateProgram(Int32 vertexShaderHandle, Int32 fragmentShaderHandle, out Int32 shaderProgramHandle, out string log)
        {
            shaderProgramHandle = GL.CreateProgram();

            ErrorCheck.Check();

            GL.AttachShader( shaderProgramHandle, vertexShaderHandle );
            GL.AttachShader( shaderProgramHandle, fragmentShaderHandle );

            ErrorCheck.Check();

            GL.LinkProgram( shaderProgramHandle );

            ErrorCheck.Check();

            GL.GetProgramInfoLog( shaderProgramHandle, out log );
        }
    }
}
