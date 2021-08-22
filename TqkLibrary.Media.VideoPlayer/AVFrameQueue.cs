using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;

namespace TqkLibrary.Media.VideoPlayer
{
  unsafe class AVFrameQueue
  {
    readonly Queue<IntPtr> queues = new Queue<IntPtr>();

    bool IsEnable = false;
    public int Count { get { return queues.Count; } }

    public void CloneAndEnqueue(AVFrame* frame)
    {
      lock(queues)
      {
        if (IsEnable)
        {
          AVFrame* clone = av_frame_clone(frame);
          queues.Enqueue((IntPtr)clone);
        }
      }
    }

    public AVFrame* Dequeue(bool IsSkipOldFrame = false)
    {
      lock (queues)
      {
        AVFrame* frame = null;
        do
        {
          if(queues.Count > 0) frame = (AVFrame*)queues.Dequeue();
        } 
        while (queues.Count > 0 && IsSkipOldFrame && FreeFrame(frame));
        return frame;
      }
    }

    public void Enable()
    {
      lock (queues) IsEnable = true;
    }

    public void DisableAndFree()
    {
      lock (queues)
      {
        IsEnable = false;
        foreach(var item in queues) FreeFrame((AVFrame*)item);
        queues.Clear();
      }
    }

    internal static bool FreeFrame(AVFrame* frame)
    {
      av_frame_unref(frame);
      av_frame_free(&frame);
      return true;
    }
  }
}
