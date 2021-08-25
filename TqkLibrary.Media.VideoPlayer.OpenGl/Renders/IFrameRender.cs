using FFmpeg.AutoGen;
using SharpGL.SceneGraph.Core;
using SharpGL.SceneGraph.Shaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TqkLibrary.Media.VideoPlayer.OpenGl.Renders
{
  unsafe interface IFrameRender : IHasOpenGLContext
  {
    void Init(AVFrame* frame);
    void Draw(AVFrame* frame);
    void Resize(double width, double height);
  }
}
