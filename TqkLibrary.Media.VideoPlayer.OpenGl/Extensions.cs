using SharpGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SharpGL.OpenGL;

namespace TqkLibrary.Media.VideoPlayer.OpenGl
{
  internal static class Extensions
  {
    internal static Size Div(this Size size,int div)
    {
      return new Size(size.Width / 2, size.Height / 2);
    }

    internal static uint CheckError(this uint err)
    {
      if (err == 0) return err;
      else
      {
        return err;
      }
    }

    internal static void CheckLinkStatus(this OpenGL gl,uint program)
    {
      int[] status = new int[1];
      gl.GetProgram(program, GL_LINK_STATUS, status);
      if (status[0] == GL_FALSE)
      {
        StringBuilder log = new StringBuilder((int)GL_INFO_LOG_LENGTH);
        gl.GetProgramInfoLog(program, (int)GL_INFO_LOG_LENGTH, IntPtr.Zero, log);
        throw new ShaderProgramException(status[0], log.ToString());
      }
    }
    internal static void CheckValidateStatus(this OpenGL gl, uint program)
    {
      int[] status = new int[1];
      gl.GetProgram(program, GL_VALIDATE_STATUS, status);
      if (status[0] == GL_FALSE)
      {
        StringBuilder log = new StringBuilder((int)GL_INFO_LOG_LENGTH);
        gl.GetProgramInfoLog(program, (int)GL_INFO_LOG_LENGTH, IntPtr.Zero, log);
        throw new ShaderProgramException(status[0], log.ToString());
      }
    }
  }
}
