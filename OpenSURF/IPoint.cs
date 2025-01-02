namespace OpenSURF;

public class IPoint
{
  /// <summary>
  /// Default ctor
  /// </summary>
  public IPoint()
  {
    Orientation = 0;
  }

  /// <summary>
  /// Coordinates of the detected interest point
  /// </summary>
  public float X, Y;

  /// <summary>
  /// Detected scale
  /// </summary>
  public float Scale;

  /// <summary>
  /// Response of the detected feature (strength)
  /// </summary>
  public float Response;

  /// <summary>
  /// Orientation measured anti-clockwise from +ve x-axis
  /// </summary>
  public float Orientation;

  /// <summary>
  /// Sign of Laplacian for fast matching purposes
  /// </summary>
  public int Laplacian;

  /// <summary>
  /// Descriptor vector
  /// </summary>
  public int DescriptorLength;
  public float [] Descriptor = null;
  public void SetDescriptorLength(int size)
  {
    DescriptorLength = size;
    Descriptor = new float[size];
  }
}
