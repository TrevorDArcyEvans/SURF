using OpenSURF;

namespace SURF.UI.CLI;

using CommandLine;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Path = SixLabors.ImageSharp.Drawing.Path;

internal static class Program
{
  public static async Task Main(string[] args)
  {
    var result = await Parser.Default.ParseArguments<Options>(args)
      .WithParsedAsync(Run);
    await result.WithNotParsedAsync(HandleParseError);
  }

  private static async Task Run(Options opt)
  {
    foreach (var inputFile in opt.InputFiles)
    {
      using var img = await Image.LoadAsync<L8>(inputFile);
      using var iimg = Utils.CreateIImage(img);
      var intImg = IntegralImage.FromImage(iimg);
      var intPts = FastHessian.GetIpoints(0.0001f, 5, 2, intImg);
      var sd = new SurfDescriptor(intImg);
      sd.DescribeInterestPoints(intPts, false, false);

      var redPen = Pens.Solid(Color.Red, 1);
      var bluePen = Pens.Solid(Color.Blue, 1);
      var whitePen = Pens.Solid(Color.White, 1);
      img.Mutate(x =>
      {
        foreach (var ip in intPts)
        {
          var S = 2 * Convert.ToInt32(2.5f * ip.Scale);
          var R = Convert.ToInt32(S / 2f);

          var pt = new Point(Convert.ToInt32(ip.X), Convert.ToInt32(ip.Y));
          var ptR = new Point(Convert.ToInt32(R * Math.Cos(ip.Orientation)), Convert.ToInt32(R * Math.Sin(ip.Orientation)));

          var myPen = ip.Laplacian > 0 ? bluePen : redPen;

          // x.DrawEllipse(myPen, pt.X - R, pt.Y - R, S, S);
          // x.DrawLine(new Pen(Color.FromArgb(0, 255, 0)), new Point(pt.X, pt.Y), new Point(pt.X + ptR.X, pt.Y + ptR.Y));
        }
      });

      var imgFileName = System.IO.Path.GetFileNameWithoutExtension(inputFile);
      var outFileName = $"interest-points-{imgFileName}.jpg";
      //await sift.Image.SaveAsJpegAsync(outFileName);

      Console.WriteLine($"{imgFileName} --> {outFileName}");
    }
  }

  private static Task HandleParseError(IEnumerable<Error> errs)
  {
    if (errs.IsVersion())
    {
      Console.WriteLine("Version Request");
      return Task.CompletedTask;
    }

    if (errs.IsHelp())
    {
      Console.WriteLine("Help Request");
      return Task.CompletedTask;
      ;
    }

    Console.WriteLine("Parser Fail");
    return Task.CompletedTask;
  }
}
