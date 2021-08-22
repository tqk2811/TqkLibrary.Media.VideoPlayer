using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TqkLibrary.Media.VideoPlayer.OpenGl
{
  public class BuildShaderException : Exception
  {
    public BuildShaderException(int status, string message): base(message)
    {
      this.ShaderStatus = status;
    }

    public int ShaderStatus { get; }
  }

  public class ShaderProgramException : Exception
  {
    public ShaderProgramException(int status, string message) : base(message)
    {
      this.ProgramStatus = status;
    }

    public int ProgramStatus { get; }
  }
}
