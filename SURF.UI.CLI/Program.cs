namespace SURF.UI.CLI;

using CommandLine;
using Newtonsoft.Json;
using OpenSURF;
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
    var redPen = Pens.Solid(Color.Red, 1);
    var bluePen = Pens.Solid(Color.Blue, 1);
    var whitePen = Pens.Solid(Color.White, 1);

    var jsonFmt = new JsonSerializerSettings
    {
      Formatting = Formatting.Indented
    };

    foreach (var inputFile in opt.InputFiles)
    {
      using var img = await Image.LoadAsync<L8>(inputFile);
      using var iimg = Utils.CreateIImage(img);
      var intImg = IntegralImage.FromImage(iimg);
      var intPts = FastHessian.GetIpoints(0.001f, 2, 2, intImg);
      var sd = new SurfDescriptor(intImg);
      sd.DescribeInterestPoints(intPts, false, false);

      var imgFileName = System.IO.Path.GetFileNameWithoutExtension(inputFile);
      var outFileName = $"interest-points-{imgFileName}.jpg";
      using var outImg = img.CloneAs<Rgba32>();
      outImg.Mutate(x =>
      {
        foreach (var ip in intPts)
        {
          var S = 2 * Convert.ToInt32(2.5f * ip.Scale);
          var R = Convert.ToInt32(S / 2f);

          var pt = new Point(Convert.ToInt32(ip.X), Convert.ToInt32(ip.Y));
          var ptR = new Point(Convert.ToInt32(R * Math.Cos(ip.Orientation)), Convert.ToInt32(R * Math.Sin(ip.Orientation)));

          var myPen = ip.Laplacian > 0 ? bluePen : redPen;
          var circle = new EllipsePolygon(pt.X, pt.Y, S, S);

          x.Draw(myPen, circle);
          x.Draw(whitePen,
            new Path(new LinearLineSegment(new PointF(pt.X, pt.Y),
              new PointF(pt.X + ptR.X, pt.Y + ptR.Y))));
        }
      });
      await outImg.SaveAsJpegAsync(outFileName);

      var json = JsonConvert.SerializeObject(intPts, jsonFmt);
      var jsonFileName = System.IO.Path.ChangeExtension(outFileName, "json");
      await File.WriteAllTextAsync(jsonFileName, json);

      Console.WriteLine($"{imgFileName} --> {outFileName} + {jsonFileName}");
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
