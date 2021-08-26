using SharpGL;
using static SharpGL.OpenGL;
using SharpGL.SceneGraph.Core;
using System;
using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System.Drawing;
using SharpGL.SceneGraph.Shaders;

namespace TqkLibrary.Media.VideoPlayer.OpenGl.Renders
{
  internal unsafe class YUV420Render : IFrameRender
  {
    const string vertexSource =
@"";
    const string fragmentSource =
@"";

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
      gl.DeleteTextures(_Texs.Length, _Texs);
      CurrentOpenGLContext = null;
    }
    #endregion

    readonly ShaderProgram _program = new ShaderProgram();
    readonly VertexShader _vertexShader = new VertexShader();
    readonly FragmentShader _fragmentShader = new FragmentShader();

    OpenGL gl { get { return CurrentOpenGLContext; } }
    readonly uint[] _Texs = new uint[3];
    
   
   
    public void Init(AVFrame* frame)
    {
      if ((AVPixelFormat)frame->format != AVPixelFormat.AV_PIX_FMT_YUV420P) throw new NotSupportedException(((AVPixelFormat)frame->format).ToString());
      if (CurrentOpenGLContext == null) throw new Exception("Call CreateInContext first");
      
    }

    public void Draw(AVFrame* frame)
    {

    }

    public void Resize(double width, double height)
    {

    }






    private void InitTexture(AVFrame* frame)
    {
      gl.GenTextures(_Texs.Length, _Texs);
      for (uint i = 0; i < _Texs.Length; i++)
      {
        gl.BindTexture(GL_TEXTURE_2D, _Texs[i]);
        gl.TexImage2D(GL_TEXTURE_2D, 0, GL_R8,//GL_LUMINANCE
          i == 0 ? frame->width : frame->width / 2,
          i == 0 ? frame->height : frame->height / 2,
          0, GL_RED /*GL_LUMINANCE*/, GL_UNSIGNED_BYTE, IntPtr.Zero);

        gl.TexParameter(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        gl.TexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
      }
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
      _program.Link();
      if (_program.LinkStatus != true)
        throw new Exception(_program.InfoLog);
    }
  }
}
