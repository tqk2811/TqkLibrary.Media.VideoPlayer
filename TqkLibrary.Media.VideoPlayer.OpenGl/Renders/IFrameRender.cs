using FFmpeg.AutoGen;
using SharpGL.SceneGraph.Core;
using System.Drawing;

namespace TqkLibrary.Media.VideoPlayer.OpenGl.Renders
{
  unsafe interface IFrameRender : IHasOpenGLContext
  {
    Size? FrameSize { get; }
    Rectangle? ViewPort { get; }
    double? ScaleRatio { get; }
    void Init(AVFrame* frame);
    void Draw(AVFrame* frame);
    void Resize(double width, double height);
  }
}
