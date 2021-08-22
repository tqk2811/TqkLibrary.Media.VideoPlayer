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


    readonly AVFrameQueue frames = new AVFrameQueue();
    readonly OpenGL gl;

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
    }
    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {

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

    private void RenderTextures()
    {

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
