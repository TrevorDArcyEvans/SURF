namespace SURF.UI.CLI;

using CommandLine;

internal sealed class Options
{
  [Value(index: 0, Required = true, HelpText = "Paths to image files to be processed.")]
  public IEnumerable<string> InputFiles { get; set; }

  [Option('o', "octaves", Required = false, Default = 2, HelpText = "Number of octaves (min 1, max 5).")]
  public int Octaves { get; set; }

  [Option('i', "initialsamples", Required = false, Default = 2, HelpText = "Number of initial samples in width & height.")]
  public int InitialSamples { get; set; }

  [Option('t', "threshold", Required = false, Default = 0.001f, HelpText = "Threshold for point of interest.")]
  public float Threshold { get; set; }
}
