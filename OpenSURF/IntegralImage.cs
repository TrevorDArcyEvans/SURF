namespace OpenSURFcs;

using System;
using System.Drawing;

public class IntegralImage
{
  private const float cR = .2989f;
  private const float cG = .5870f;
  private const float cB = .1140f;

  public int Width, Height;

  private readonly float[,] _matrix;

  public float this[int y, int x]
  {
    get => _matrix[y, x];

    set => _matrix[y, x] = value;
  }

  private IntegralImage(int width, int height)
  {
    Width = width;
    Height = height;

    _matrix = new float[height, width];
  }

  public static IntegralImage FromImage(Bitmap image)
  {
    var pic = new IntegralImage(image.Width, image.Height);

    float rowsum = 0;
    for (var x = 0; x < image.Width; x++)
    {
      var c = image.GetPixel(x, 0);
      rowsum += (cR * c.R + cG * c.G + cB * c.B) / 255f;
      pic[0, x] = rowsum;
    }


    for (var y = 1; y < image.Height; y++)
    {
      rowsum = 0;
      for (var x = 0; x < image.Width; x++)
      {
        var c = image.GetPixel(x, y);
        rowsum += (cR * c.R + cG * c.G + cB * c.B) / 255f;

        // integral image is rowsum + value above        
        pic[y, x] = rowsum + pic[y - 1, x];
      }
    }

    return pic;
  }

  public float BoxIntegral(int row, int col, int rows, int cols)
  {
    // The subtraction by one for row/col is because row/col is inclusive.
    var r1 = Math.Min(row, Height) - 1;
    var c1 = Math.Min(col, Width) - 1;
    var r2 = Math.Min(row + rows, Height) - 1;
    var c2 = Math.Min(col + cols, Width) - 1;

    float A = 0, B = 0, C = 0, D = 0;
    if (r1 >= 0 && c1 >= 0)
    {
      A = _matrix[r1, c1];
    }

    if (r1 >= 0 && c2 >= 0)
    {
      B = _matrix[r1, c2];
    }

    if (r2 >= 0 && c1 >= 0)
    {
      C = _matrix[r2, c1];
    }

    if (r2 >= 0 && c2 >= 0)
    {
      D = _matrix[r2, c2];
    }

    return Math.Max(0, A - B - C + D);
  }

  /// <summary>
  /// Get Haar Wavelet X repsonse
  /// </summary>
  /// <param name="row"></param>
  /// <param name="column"></param>
  /// <param name="size"></param>
  /// <returns></returns>
  public float HaarX(int row, int column, int size)
  {
    return BoxIntegral(row - size / 2, column, size, size / 2)
           - 1 * BoxIntegral(row - size / 2, column - size / 2, size, size / 2);
  }

  /// <summary>
  /// Get Haar Wavelet Y repsonse
  /// </summary>
  /// <param name="row"></param>
  /// <param name="column"></param>
  /// <param name="size"></param>
  /// <returns></returns>
  public float HaarY(int row, int column, int size)
  {
    return BoxIntegral(row, column - size / 2, size / 2, size)
           - 1 * BoxIntegral(row - size / 2, column - size / 2, size / 2, size);
  }
}
