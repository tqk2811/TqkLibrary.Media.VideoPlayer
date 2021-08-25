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
using SharpGL.SceneGraph.Shaders;
using TqkLibrary.Media.VideoPlayer.OpenGl.Renders;

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


    readonly OpenGL gl;
    readonly AVFrameQueue frames = new AVFrameQueue();   


    IFrameRender render = null;
    bool _IsSetup = false;

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
      videoControl.OpenGLInitialized += VideoControl_OpenGLInitialized;
      videoControl.Unloaded += VideoControl_Unloaded;
      videoControl.OpenGLDraw += VideoControl_OpenGLDraw;
      videoControl.RenderContextType = RenderContextType.FBO;
      //videoControl.OpenGLVersion = SharpGL.Version.OpenGLVersion.OpenGL4_4;
    }

    private void VideoControl_Unloaded(object sender, RoutedEventArgs e)
    {
      frames.DisableAndFree();
      _IsSetup = false;
    }

    private void VideoControl_OpenGLInitialized(object sender, SharpGL.WPF.OpenGLRoutedEventArgs args)
    {
      frames.Enable();
    }

    private void VideoControl_Resized(object sender, SharpGL.WPF.OpenGLRoutedEventArgs args)
    {
      render?.Resize(videoControl.ActualWidth, videoControl.ActualHeight);
    }

    private void VideoControl_OpenGLDraw(object sender, SharpGL.WPF.OpenGLRoutedEventArgs args)
    {
      if (frames.Count == 0 || render == null) return;
      AVFrame* frame = frames.Dequeue(this.IsSkipOldFrame);
      if (frame == null) return;

      if(!_IsSetup)
      {
        render.CreateInContext(gl);
        render.Init(frame);
        _IsSetup = true;
        //return;
      }

      render.Draw(frame);
    }

    /// <summary>
    /// Only support YUV420
    /// </summary>
    /// <param name="frame"></param>
    public void PushFrame(AVFrame* frame)
    {
      switch ((AVPixelFormat)frame->format)
      {
        case AVPixelFormat.AV_PIX_FMT_NV12://VGA decode
          if (render == null) render = new NV12Render();
          break;
        case AVPixelFormat.AV_PIX_FMT_YUV420P://h264 decode
          if (render == null) render = new YUV420Render();
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
