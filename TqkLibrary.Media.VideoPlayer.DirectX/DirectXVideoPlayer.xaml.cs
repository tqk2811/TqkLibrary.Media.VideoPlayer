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
using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;

namespace TqkLibrary.Media.VideoPlayer.DirectX
{
  /// <summary>
  /// Interaction logic for DirectXVideoPlayer.xaml
  /// </summary>
  public unsafe partial class DirectXVideoPlayer : UserControl, IVideoPlayer
  {

    #region WPF Binding
    public static readonly DependencyProperty IsSkipOldFrameProperty = DependencyProperty.Register(
      nameof(IsSkipOldFrame),
      typeof(bool),
      typeof(DirectXVideoPlayer),
      new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool IsSkipOldFrame
    {
      get { return (bool)GetValue(IsSkipOldFrameProperty); }
      set { SetValue(IsSkipOldFrameProperty, value); }
    }
    #endregion


    readonly AVFrameQueue frames = new AVFrameQueue();


    AVFrame* _currentFrame = null;
    System.Drawing.Size? _currentSize = null;


    public DirectXVideoPlayer()
    {
      InitializeComponent();
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














    /// <summary>
    /// Only support YUV420
    /// </summary>
    /// <param name="frame"></param>
    public void PushFrame(AVFrame* frame)
    {
      switch ((AVPixelFormat)frame->format)
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
