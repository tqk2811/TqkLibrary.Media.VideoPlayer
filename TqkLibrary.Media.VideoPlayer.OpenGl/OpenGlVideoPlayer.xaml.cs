using SharpGL;
using SharpGL.Enumerations;
using static SharpGL.OpenGL;
using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;

namespace TqkLibrary.Media.VideoPlayer.OpenGl
{
  /// <summary>
  /// Interaction logic for OpenGlVideoPlayer.xaml
  /// </summary>
  public unsafe partial class OpenGlVideoPlayer : UserControl, IVideoPlayer
  {
    #region WPF Binding
    public static readonly DependencyProperty IsSkipOldFrameProperty = DependencyProperty.Register(
      nameof(IsSkipOldFrame),
      typeof(bool),
      typeof(OpenGlVideoPlayer),
      new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool IsSkipOldFrame
    {
      get { return (bool)GetValue(IsSkipOldFrameProperty); }
      set { SetValue(IsSkipOldFrameProperty, value); }
    }
    #endregion

    const uint ATTRIBUTE_VERTEX = 0;
    const uint ATTRIBUTE_TEXTURE = 1;

    readonly AVFrameQueue frames = new AVFrameQueue();
    readonly OpenGL gl;

    uint _yuvFragmentShader = 0;
    uint _vertShader = 0;
    uint _program_shader = 0;
    readonly int[] _textureUniforms = new int[3];
    readonly uint[] _textures = new uint[3];
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

    AVFrame* _currentFrame = null;
    System.Drawing.Size? _currentSize = null;

    public OpenGlVideoPlayer()
    {
      InitializeComponent();
      gl = videoControl.OpenGL;
      //videoControl.MouseDown += VideoControl_MouseDown;
      //videoControl.MouseUp += VideoControl_MouseUp;
      //videoControl.MouseMove += VideoControl_MouseMove;
      //videoControl.MouseWheel += VideoControl_MouseWheel;
      //videoControl.MouseLeave += VideoControl_MouseLeave;
      videoControl.Resized += VideoControl_Resized;
      videoControl.OpenGLDraw += VideoControl_OpenGLDraw;
      videoControl.RenderContextType = RenderContextType.FBO;
      videoControl.OpenGLVersion = SharpGL.Version.OpenGLVersion.OpenGL4_4;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      frames.Enable();
      gl.Enable(GL_DEPTH_TEST);
      gl.ShadeModel(GL_FLAT);
      gl.GenTextures(_textures.Length, _textures);
      InitShader();
    }
    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
      gl.DeleteShader(_yuvFragmentShader);
      gl.DeleteShader(_vertShader);
      gl.DeleteTextures(_textures.Length, _textures);
      gl.DeleteProgram(_program_shader);
      frames.DisableAndFree();
      AVFrameQueue.FreeFrame(_currentFrame);
    }

    private void VideoControl_Resized(object sender, SharpGL.WPF.OpenGLRoutedEventArgs args)
    {
      gl.Viewport(0, 0, (int)videoControl.ActualWidth, (int)videoControl.ActualHeight);
      //if (_currentSize != null) gl.Viewport(0, 0, _currentSize.Value.Width, _currentSize.Value.Height);
    }

    private void VideoControl_OpenGLDraw(object sender, SharpGL.WPF.OpenGLRoutedEventArgs args)
    {
      if (frames.Count == 0) return;
      AVFrame* frame = frames.Dequeue(this.IsSkipOldFrame);
      if (frame == null) return;

      fixed (AVFrame** t = &_currentFrame) av_frame_free(t);
      _currentFrame = frame;

      var size = new System.Drawing.Size(frame->width, frame->height);
      if (size != _currentSize) _currentSize = size;

      RenderTextures();
    }


    private void InitShader()
    {
      _vertShader = gl.BuildShader(ShadersHelper.vertexShaderString, GL_VERTEX_SHADER);
      _yuvFragmentShader = gl.BuildShader(ShadersHelper.yuvFragmentShaderString, GL_FRAGMENT_SHADER);

      _program_shader = gl.CreateProgram();
      gl.AttachShader(_program_shader, _vertShader);
      gl.AttachShader(_program_shader, _yuvFragmentShader);

      gl.BindAttribLocation(_program_shader, ATTRIBUTE_VERTEX, "vertexIn");
      gl.BindAttribLocation(_program_shader, ATTRIBUTE_TEXTURE, "textureIn");//req LinkProgram after bind

      gl.LinkProgram(_program_shader);
      gl.CheckLinkStatus(_program_shader);

      //gl.ValidateProgram(_program_shader);
      //gl.CheckValidateStatus(_program_shader);

      gl.UseProgram(_program_shader);

      _textureUniforms[0] = gl.GetUniformLocation(_program_shader, "tex_y");
      _textureUniforms[1] = gl.GetUniformLocation(_program_shader, "tex_u");
      _textureUniforms[2] = gl.GetUniformLocation(_program_shader, "tex_v");

      for (int i = 0; i < 3; i++) gl.Uniform1(_textureUniforms[i], i);

      fixed (float* v = &vertexVertices[0])
        gl.VertexAttribPointer(ATTRIBUTE_VERTEX, 2, GL_FLOAT, false, 0, (IntPtr)v);
      fixed (float* t = &textureVertices[0])
        gl.VertexAttribPointer(ATTRIBUTE_TEXTURE, 2, GL_FLOAT, false, 0, (IntPtr)t);

      gl.EnableVertexAttribArray(ATTRIBUTE_VERTEX);
      gl.EnableVertexAttribArray(ATTRIBUTE_TEXTURE);
      gl.GetError().CheckError();
    }


    private unsafe void RenderTextures()
    {
      //gl.Viewport(0, 0, (int)videoControl.ActualWidth, (int)videoControl.ActualHeight);
      gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
      gl.LoadIdentity();
      //gl.Ortho(0, videoControl.ActualWidth, videoControl.ActualHeight, 0, -1, 1);
      for (uint i = 0; i < 3; i++)
      {
        gl.ActiveTexture(GL_TEXTURE0 + i);//GL_TEXTURE0 GL_TEXTURE1 GL_TEXTURE2
        gl.BindTexture(GL_TEXTURE_2D, _textures[i]);

        gl.PixelStore(GL_UNPACK_ROW_LENGTH, _currentFrame->linesize[i]);

        gl.TexImage2D(GL_TEXTURE_2D, 0, GL_LUMINANCE,
          i == 0 ? _currentFrame->width : _currentFrame->width / 2,
          i == 0 ? _currentFrame->height : _currentFrame->height / 2,
          0, GL_LUMINANCE, GL_UNSIGNED_BYTE, (IntPtr)_currentFrame->data[i]);

        gl.TexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        gl.TexParameter(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        gl.TexParameter(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        gl.TexParameter(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
      }


      //var textureRect = new Rect(0, 0, temp_frame->width, temp_frame->width);
      //gl.Begin(GL_QUADS);
      //gl.TexCoord(0, 0); gl.Vertex(textureRect.X, textureRect.Y);
      //gl.TexCoord(1, 0); gl.Vertex(textureRect.Width + textureRect.X, textureRect.Y);
      //gl.TexCoord(1, 1); gl.Vertex(textureRect.Width + textureRect.X, textureRect.Height + textureRect.Y);
      //gl.TexCoord(0, 1); gl.Vertex(textureRect.X, textureRect.Height + textureRect.Y);
      //gl.End();
      //gl.UseProgram(_program_shader);
      //gl.BindVertexArray(VAO);
      for (int i = 0; i < 3; i++) gl.Uniform1(_textureUniforms[i], i);
      gl.DrawArrays(GL_TRIANGLE_STRIP, 0, 4);// Use the vertex array way to draw graphics
    }



    /// <summary>
    /// Only support YUV420
    /// </summary>
    /// <param name="frame"></param>
    public void PushFrame(AVFrame* frame)
    {
      switch((AVPixelFormat)frame->format)
      {
        //case AVPixelFormat.AV_PIX_FMT_NV12://VGA decode
        //  break;
        case AVPixelFormat.AV_PIX_FMT_YUV420P://h264 decode
          break;
        default: throw new NotSupportedException(((AVPixelFormat)frame->format).ToString());
      }
      frames.CloneAndEnqueue(frame);
#if DEBUG
      Console.WriteLine($"Pushed {frame->pts}, WxH: {frame->width}x{frame->height} linesize:{frame->linesize[0]},{frame->linesize[1]},{frame->linesize[2]}");
#endif
    }
  }
}
