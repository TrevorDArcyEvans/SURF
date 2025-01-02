namespace SURF.UI.CLI;

using OpenSURF;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

internal static class Utils
{
  public static List<IPoint>[] GetMatches(List<IPoint> ipts1, List<IPoint> ipts2)
  {
    double dist;
    double d1, d2;
    var match = new IPoint();

    var matches = new List<IPoint>[2];
    matches[0] = new List<IPoint>();
    matches[1] = new List<IPoint>();

    for (var i = 0; i < ipts1.Count; i++)
    {
      d1 = d2 = double.MaxValue;

      for (var j = 0; j < ipts2.Count; j++)
      {
        dist = GetDistance(ipts1[i], ipts2[j]);

        if (dist < d1) // if this feature matches better than current best
        {
          d2 = d1;
          d1 = dist;
          match = ipts2[j];
        }
        else if (dist < d2) // this feature matches better than second best
        {
          d2 = dist;
        }
      }

      // If match has a d1:d2 ratio < 0.65 ipoints are a match
      if (d1 / d2 < 0.77) //ԽСMatch
      {
        matches[0].Add(ipts1[i]);
        matches[1].Add(match);
      }
    }

    return matches;
  }

  private static double GetDistance(IPoint ip1, IPoint ip2)
  {
    var sum = 0.0f;
    for (var i = 0; i < 64; ++i)
    {
      sum += (ip1.Descriptor[i] - ip2.Descriptor[i]) * (ip1.Descriptor[i] - ip2.Descriptor[i]);
    }

    return Math.Sqrt(sum);
  }

  public static IImage CreateIImage(Image<L8> img)
  {
    return new ImageWrapper(img);
  }
}