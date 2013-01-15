using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.Xplorer
{
    static class ErrorCheck
    {
        public static void Check(String Message="")
        {
            ErrorCode ec = GL.GetError();
            if (ec != ErrorCode.NoError)
            {
                throw new Exception(Message+" "+ec.ToString());
            }
        }
    }
}
