using SharpGL;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TqkLibrary.Media.VideoPlayer.OpenGl.Renders;
using TqkLibrary.ScrcpyDotNet;
using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;

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



    public IFrameEndQueue AVFrameQueue
    {
      get { return frames; }
    }
    readonly OpenGL gl;
    readonly AVFrameQueue frames = new AVFrameQueue();

    IFrameRender render = null;

    public OpenGlVideoPlayer()
    {
      InitializeComponent();
      this.Focusable = true;
      gl = videoControl.OpenGL;

      videoControl.MouseDown += VideoControl_MouseDown;
      videoControl.MouseUp += VideoControl_MouseUp;
      videoControl.MouseMove += VideoControl_MouseMove;
      videoControl.MouseWheel += VideoControl_MouseWheel;
      videoControl.MouseLeave += VideoControl_MouseLeave;
      this.KeyDown += VideoControl_KeyDown;
      this.KeyUp += VideoControl_KeyUp;

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
      if (frames.Count == 0) return;
      AVFrame* frame = frames.Dequeue(this.IsSkipOldFrame);
      if (frame == null) return;

      switch ((AVPixelFormat)frame->format)
      {
        case AVPixelFormat.AV_PIX_FMT_NV12://VGA decode
          if (render == null)
          {
            render = new NV12Render(); 
            render.CreateInContext(gl);
            render.Init(frame);
          }
          break;
        case AVPixelFormat.AV_PIX_FMT_YUV420P://h264 decode
          if (render == null)
          {
            render = new YUV420Render();
            render.CreateInContext(gl);
            render.Init(frame);
          }
          break;
        default: throw new NotSupportedException(((AVPixelFormat)frame->format).ToString());
      }

      try
      {
        render.Draw(frame);
      }
      finally
      {
        av_frame_unref(frame);
        av_frame_free(&frame);
      }
    }




    #region Control
    public ScrcpyControl Control { get; set; }

    bool _isMouseDown = false;
    const long _touchPointer = 32326;

    //Key: num 34->33, char 34->69
    //AndroidKeyCode: num: 7->16, char 29->52
    AndroidKeyCode FixKey(Key key)
    {
      if (Key.D0 <= key && key <= Key.D9)
      {
        return (AndroidKeyCode)((int)key + ((int)AndroidKeyCode.KEYCODE_0 - (int)Key.D0));
      }
      else if (Key.D0 <= key && key <= Key.D9)
      {
        return (AndroidKeyCode)((int)key + ((int)AndroidKeyCode.KEYCODE_A - (int)Key.A));
      }

      return AndroidKeyCode.KEYCODE_UNKNOWN;
    }


    private void VideoControl_KeyUp(object sender, KeyEventArgs e)
    {
      if(e.SystemKey != Key.None)
      {
        AndroidKeyCode keyCode = FixKey(e.Key);
        if(keyCode != AndroidKeyCode.KEYCODE_UNKNOWN)
        {
          Control?.InjectKeycode(AndroidKeyEventAction.ACTION_UP, keyCode);
#if DEBUG
          Console.WriteLine($"ACTION_UP: {keyCode}");
#endif
        }
      }
      else
      {

      }
    }

    private void VideoControl_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.SystemKey != Key.None)
      {
        AndroidKeyCode keyCode = FixKey(e.Key);
        if (keyCode != AndroidKeyCode.KEYCODE_UNKNOWN)
        {
          Control?.InjectKeycode(AndroidKeyEventAction.ACTION_DOWN, keyCode);
#if DEBUG
          Console.WriteLine($"ACTION_DOWN: {keyCode}");
#endif
        }
      }
      else
      {

      }
    }


    private void VideoControl_MouseLeave(object sender, MouseEventArgs e)
    {
      if(_isMouseDown)
      {
        System.Drawing.Point point = new System.Drawing.Point(0, 0);
        _isMouseDown = !(Control?.InjectTouchEvent(AndroidMotionEventAction.ACTION_UP, _touchPointer, point, 1.0f, AndroidMotionEventButton.BUTTON_PRIMARY) == true);
#if DEBUG
        Console.WriteLine($"ACTION_UP: {point}");
#endif
      }
    }

    private void VideoControl_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      System.Drawing.Point? point = GetRealPointer(e.GetPosition(sender as IInputElement));
      if (point != null)
      {
        Control?.InjectScrollEvent(point.Value, e.Delta / 120);
      }
    }

    private void VideoControl_MouseMove(object sender, MouseEventArgs e)
    {
      if (_isMouseDown)
      {
        System.Drawing.Point? point = GetRealPointer(e.GetPosition(sender as IInputElement));
        if (point != null)
        {
          Control?.InjectTouchEvent(AndroidMotionEventAction.ACTION_MOVE, _touchPointer, point.Value, 1.0f, AndroidMotionEventButton.BUTTON_PRIMARY);
#if DEBUG
          Console.WriteLine($"ACTION_MOVE: {point.Value}");
#endif
        }
      }
    }

    private void VideoControl_MouseUp(object sender, MouseButtonEventArgs e)
    {
      if(_isMouseDown)
      {
        System.Drawing.Point? point = GetRealPointer(e.GetPosition(sender as IInputElement));
        if (point != null)
        {
          _isMouseDown = !(Control?.InjectTouchEvent(AndroidMotionEventAction.ACTION_UP, _touchPointer, point.Value, 1.0f, AndroidMotionEventButton.BUTTON_PRIMARY) == true);
#if DEBUG
          Console.WriteLine($"ACTION_UP: {point.Value}");
#endif
        }
      }
    }

    private void VideoControl_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (!_isMouseDown)
      {
        System.Drawing.Point? point = GetRealPointer(e.GetPosition(sender as IInputElement));
        if (point != null)
        {
          _isMouseDown = Control?.InjectTouchEvent(AndroidMotionEventAction.ACTION_DOWN, _touchPointer, point.Value, 1.0f, AndroidMotionEventButton.BUTTON_PRIMARY) == true;
#if DEBUG
          Console.WriteLine($"ACTION_DOWN: {point.Value}");
#endif
        }
      }
    }

    System.Drawing.Point? GetRealPointer(System.Windows.Point point)
    {
      if (render != null && render.FrameSize != null && render.ViewPort != null && render.ScaleRatio != null)
      {
        System.Windows.Point point_with_texture = new System.Windows.Point(point.X - render.ViewPort.Value.X, point.Y - render.ViewPort.Value.Y);
        double real_x = point_with_texture.X / render.ScaleRatio.Value;
        double real_y = point_with_texture.Y / render.ScaleRatio.Value;
        return new System.Drawing.Point((int)real_x, (int)real_y);
      }
      else return null;
    }
    #endregion
  }
}
