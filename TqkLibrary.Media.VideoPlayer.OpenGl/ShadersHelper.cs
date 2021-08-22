using SharpGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SharpGL.OpenGL;
namespace TqkLibrary.Media.VideoPlayer.OpenGl
{
  internal static class ShadersHelper
  {
    internal static uint BuildShader(this OpenGL gl, string shader_source, uint type)
    {
      uint shader = gl.CreateShader(type);
      gl.ShaderSource(shader, shader_source);
      gl.CompileShader(shader);

      int[] status = new int[1];
      gl.GetShader(shader, GL_COMPILE_STATUS, status);
      if (status[0] == GL_FALSE)
      {
        StringBuilder log = new StringBuilder((int)GL_INFO_LOG_LENGTH);
        gl.GetShaderInfoLog(shader, (int)GL_INFO_LOG_LENGTH, IntPtr.Zero, log);
        throw new BuildShaderException(status[0], log.ToString());
      }
      return shader;
    }

    internal static uint BuildProgram(this OpenGL gl, params uint[] shaders)
    {
      uint program = gl.CreateProgram();
      for (int i = 0; i < shaders.Length; i++) gl.AttachShader(program, shaders[i]);
      return program;
    }
  }
}
