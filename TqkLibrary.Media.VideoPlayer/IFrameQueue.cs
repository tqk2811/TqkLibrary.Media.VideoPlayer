using FFmpeg.AutoGen;
namespace TqkLibrary.Media.VideoPlayer
{
  unsafe interface IFrameQueue
  {
    int Count { get; }
    AVFrame* Dequeue(bool IsSkipOldFrame = false);
    void Enable();
    void DisableAndFree();
  }
}
