using FFmpeg.AutoGen;
using TqkLibrary.ScrcpyDotNet;
using static FFmpeg.AutoGen.ffmpeg;

namespace TqkLibrary.Media.VideoPlayer
{
  public unsafe interface IVideoPlayer
  {
    IFrameEndQueue AVFrameQueue { get; }
    bool IsSkipOldFrame { get; set; }
  }
}
