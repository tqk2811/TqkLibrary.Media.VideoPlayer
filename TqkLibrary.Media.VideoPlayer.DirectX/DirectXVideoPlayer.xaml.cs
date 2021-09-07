using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FFmpeg.AutoGen;
using TqkLibrary.ScrcpyDotNet;

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

    public IFrameEndQueue AVFrameQueue { get { return frames; } }
    public ScrcpyControl Control { get; set; }

    readonly AVFrameQueue frames = new AVFrameQueue();



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
    }








  }
}
