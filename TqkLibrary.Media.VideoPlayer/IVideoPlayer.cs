using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;

namespace TqkLibrary.Media.VideoPlayer
{
  public unsafe interface IVideoPlayer
  {
    bool IsSkipOldFrame { get; set; }
    void PushFrame(AVFrame* frame);
  }
}
