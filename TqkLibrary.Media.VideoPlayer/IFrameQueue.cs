using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Text;

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
