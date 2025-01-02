namespace OpenSURF;

using System;
using System.Drawing;

public interface IImage : IDisposable
{
  int Width { get; }
  int Height { get; }
  Color GetPixel(int x, int y);
}
