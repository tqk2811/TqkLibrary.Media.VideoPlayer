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
    internal const string yuvFragmentShaderString =
@"precision mediump float;
varying vec2 textureOut;
uniform sampler2D tex_y;
uniform sampler2D tex_u;
uniform sampler2D tex_v;
void main(void)
{
  vec3 yuv;
  vec3 rgb;
  yuv.x = texture2D(tex_y, textureOut).r;
  yuv.y = texture2D(tex_u, textureOut).r - 0.5;
  yuv.z = texture2D(tex_v, textureOut).r - 0.5;
  rgb = mat3( 1,       1,         1,
              0,       -0.39465,  2.03211,
              1.13983, -0.58060,  0) * yuv;
  gl_FragColor = vec4(rgb, 1);
}";

    internal const string vertexShaderString =
@"attribute vec4 vertexIn;
attribute vec2 textureIn;
varying vec2 textureOut;
void main(void)
{
  gl_Position = vertexIn;
  textureOut = textureIn;
}";
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
