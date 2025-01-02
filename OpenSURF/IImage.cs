namespace OpenSURF;

using System.Drawing;

public interface IImage
{
  int Width { get; set; }
  int Height { get; set; }
  Color GetPixel(int x, int y);
}
