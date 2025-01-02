namespace SURF.UI.CLI;

using OpenSURF;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class ImageWrapper(Image<L8> image) : IDisposable, IImage
{
  public int Width => image.Width;
  public int Height => image.Height;

  public System.Drawing.Color GetPixel(int x, int y)
  {
    Rgba32 px = default;
    image[x, y].ToRgba32(ref px);
    return System.Drawing.Color.FromArgb(px.R, px.G, px.B);
  }

  public void Dispose()
  {
    image.Dispose();
  }
}
