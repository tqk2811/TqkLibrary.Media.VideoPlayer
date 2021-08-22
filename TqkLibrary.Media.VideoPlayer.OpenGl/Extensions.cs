using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TqkLibrary.Media.VideoPlayer.OpenGl
{
  internal static class Extensions
  {
    public static Size Div(this Size size,int div)
    {
      return new Size(size.Width / 2, size.Height / 2);
    }

    internal static uint CheckError(this uint err)
    {
      if (err == 0) return err;
      else
      {
        return err;
      }
    }
  }
}
