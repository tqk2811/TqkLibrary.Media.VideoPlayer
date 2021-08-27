using SharpGL;
using static SharpGL.OpenGL;
using SharpGL.SceneGraph.Core;
using System;
using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System.Drawing;
using SharpGL.SceneGraph.Shaders;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TqkLibrary.Media.VideoPlayer.OpenGl.Renders
{
  internal unsafe class NV12Render : IFrameRender
  {
    const string vertexSource =
@"attribute vec4 vertexIn;
attribute vec2 textureIn;
varying vec2 textureOut;
void main(void)
{
  gl_Position = vertexIn;
  textureOut = textureIn;
}";
    const string fragmentSource =
@"precision highp float;
varying vec2 textureOut;
uniform sampler2D tex_y;
uniform sampler2D tex_uv;
//uniform float sBrightnessValue;
//uniform float sContrastValue;
void main (void) {
  vec3 yuv;
  vec3 rgb;
  yuv.x = texture2D(tex_y, textureOut).r;
  yuv.y = texture2D(tex_uv, textureOut).r - 0.5;
  yuv.z = texture2D(tex_uv, textureOut).a - 0.5;
  
  rgb = mat3( 1,        1,          1,
              0,        -0.39465,   2.03211,
              1.13983,  -0.58060,   0) * yuv;
  gl_FragColor = vec4(rgb, 1);
}";
    const uint ATTRIBUTE_VERTEX = 0;
    const uint ATTRIBUTE_TEXTURE = 1;
    static readonly float[] vertexVertices = {
      -1.0f, -1.0f,
      1.0f, -1.0f,
      -1.0f, 1.0f,
      1.0f, 1.0f
    };
    static readonly float[] textureVertices = {
      0.0f, 1.0f,
      1.0f, 1.0f,
      0.0f, 0.0f,
      1.0f, 0.0f
    };
    readonly int[] _textureUniforms = new int[2];
    readonly ShaderProgram _program = new ShaderProgram();
    readonly VertexShader _vertexShader = new VertexShader();
    readonly FragmentShader _fragmentShader = new FragmentShader();   
    readonly uint[] _texs = new uint[2];
    readonly List<int[]> _buffers = new List<int[]>();
    Size? _frameSize = null;
    double screenWidth = 0;
    double screenHeight = 0;

    OpenGL gl { get { return CurrentOpenGLContext; } }


    #region IHasOpenGLContext
    public OpenGL CurrentOpenGLContext { get; private set; }
    public void CreateInContext(OpenGL gl)
    {
      if(CurrentOpenGLContext != null) DestroyInContext(CurrentOpenGLContext);
      this.CurrentOpenGLContext = gl;
    }
    public void DestroyInContext(OpenGL gl)
    {
      _program.DestroyInContext(gl);
      _fragmentShader.DestroyInContext(gl);
      _vertexShader.DestroyInContext(gl);
      gl.DeleteTextures(_texs.Length, _texs);
      CurrentOpenGLContext = null;
    }
    #endregion

    public void Init(AVFrame* frame)
    {
      if ((AVPixelFormat)frame->format != AVPixelFormat.AV_PIX_FMT_NV12) throw new NotSupportedException(((AVPixelFormat)frame->format).ToString());
      if (CurrentOpenGLContext == null) throw new Exception("Call CreateInContext first");

      int[] viewport = new int[4];
      gl.GetInteger(GL_VIEWPORT, viewport);
      screenWidth = viewport[2];
      screenHeight = viewport[3];

      gl.GenTextures(_texs.Length, _texs);
      InitTexture(frame);
      InitShader();
    }

    public void Draw(AVFrame* frame)
    {
      DrawTexture(frame);
    }

    public void Resize(double width, double height)
    {
      this.screenWidth = width;
      this.screenHeight = height;

      if (_frameSize != null)
      {
        double ratio_w = width / _frameSize.Value.Width;
        double ratio_h = height / _frameSize.Value.Height;
        double ratio = Math.Min(ratio_w, ratio_h);

        double w = _frameSize.Value.Width * ratio;
        double h = _frameSize.Value.Height * ratio;

        int x = (int)((width - w) / 2);
        int y = (int)((height - h) / 2);

        gl?.Viewport(x, y, (int)w, (int)h);
      }
      else gl?.Viewport(0, 0, (int)width, (int)height);
    }




    private void DrawTexture(AVFrame* frame)
    {
      gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
      for (uint i = 0; i < _texs.Length; i++)
      {
        gl.ActiveTexture(GL_TEXTURE0 + i);
        gl.BindTexture(GL_TEXTURE_2D, _texs[i]);

        int length_in_byte = frame->linesize[i] * (i == 0 ? frame->height : frame->height / 2);
        if ((_buffers[(int)i].Length != length_in_byte / 4) || _frameSize?.Width != frame->width)//rotate -> re-init
        {
          _buffers.Clear();
          InitTexture(frame);
        }
        Marshal.Copy((IntPtr)frame->data[i], _buffers[(int)i], 0, _buffers[(int)i].Length);

        gl.PixelStore(GL_UNPACK_ROW_LENGTH, frame->linesize[i]);
        gl.TexSubImage2D(GL_TEXTURE_2D, 0, 0, 0,
          i == 0 ? frame->width : frame->width / 2,
          i == 0 ? frame->height : frame->height / 2,
          GL_LUMINANCE,//i == 0 ? GL_LUMINANCE : GL_LUMINANCE_ALPHA,
          GL_UNSIGNED_BYTE, _buffers[(int)i]);
      }
      fixed (float* v = &vertexVertices[0])
        gl.VertexAttribPointer(ATTRIBUTE_VERTEX, 2, GL_FLOAT, false, 0, (IntPtr)v);
      fixed (float* t = &textureVertices[0])
        gl.VertexAttribPointer(ATTRIBUTE_TEXTURE, 2, GL_FLOAT, false, 0, (IntPtr)t);

      gl.EnableVertexAttribArray(ATTRIBUTE_VERTEX);
      gl.EnableVertexAttribArray(ATTRIBUTE_TEXTURE);
      gl.DrawArrays(GL_TRIANGLE_STRIP, 0, 4);
    }

    private void InitTexture(AVFrame* frame)
    {
      _frameSize = new Size(frame->width, frame->height);
      gl.GenTextures(_texs.Length, _texs);
      for (uint i = 0; i < _texs.Length; i++)
      {
        gl.ActiveTexture(GL_TEXTURE0 + i);
        gl.BindTexture(GL_TEXTURE_2D, _texs[i]);

        int length_in_byte = frame->linesize[i] * (i == 0 ? frame->height : frame->height / 2);
        if (_buffers.Count == i) _buffers.Add(new int[length_in_byte / 4]);

        gl.PixelStore(GL_UNPACK_ROW_LENGTH, frame->linesize[i]);

        gl.TexImage2D(GL_TEXTURE_2D, 0,
          GL_LUMINANCE,//i == 0 ? GL_LUMINANCE : GL_LUMINANCE_ALPHA,
          i == 0 ? frame->width : frame->width / 2,
          i == 0 ? frame->height : frame->height / 2,
          0,
          GL_LUMINANCE,//i == 0 ? GL_LUMINANCE : GL_LUMINANCE_ALPHA, 
          GL_UNSIGNED_BYTE, IntPtr.Zero);

        gl.TexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        gl.TexParameter(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        gl.TexParameter(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        gl.TexParameter(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
      }
      Resize(screenWidth, screenHeight);
    }

    private void InitShader()
    {
      _vertexShader.CreateInContext(gl);
      _vertexShader.SetSource(vertexSource);
      _vertexShader.Compile();
      if (_vertexShader.CompileStatus != true)
        throw new Exception(_vertexShader.InfoLog);

      _fragmentShader.CreateInContext(gl);
      _fragmentShader.SetSource(fragmentSource);
      _fragmentShader.Compile();
      if (_fragmentShader.CompileStatus != true)
        throw new Exception(_fragmentShader.InfoLog);

      _program.CreateInContext(gl);
      _program.AttachShader(_vertexShader);
      _program.AttachShader(_fragmentShader);

      gl.BindAttribLocation(_program.ProgramObject, ATTRIBUTE_VERTEX, "vertexIn");
      gl.BindAttribLocation(_program.ProgramObject, ATTRIBUTE_TEXTURE, "textureIn");

      _program.Link();
      if (_program.LinkStatus != true)
        throw new Exception(_program.InfoLog);

      gl.UseProgram(_program.ProgramObject);

      _textureUniforms[0] = _program.GetUniformLocation("tex_y");
      _textureUniforms[1] = _program.GetUniformLocation("tex_uv");
      for (int i = 0; i < _texs.Length; i++) gl.Uniform1(_textureUniforms[i], i);

      fixed (float* v = &vertexVertices[0])
        gl.VertexAttribPointer(ATTRIBUTE_VERTEX, 2, GL_FLOAT, false, 0, (IntPtr)v);
      fixed (float* t = &textureVertices[0])
        gl.VertexAttribPointer(ATTRIBUTE_TEXTURE, 2, GL_FLOAT, false, 0, (IntPtr)t);

      gl.EnableVertexAttribArray(ATTRIBUTE_VERTEX);
      gl.EnableVertexAttribArray(ATTRIBUTE_TEXTURE);
    }
  }
}
