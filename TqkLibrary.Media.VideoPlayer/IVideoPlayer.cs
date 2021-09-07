using TqkLibrary.ScrcpyDotNet;
namespace TqkLibrary.Media.VideoPlayer
{
  public unsafe interface IVideoPlayer
  {
    ScrcpyControl Control { get; set; }
    IFrameEndQueue AVFrameQueue { get; }
    bool IsSkipOldFrame { get; set; }
  }
}
