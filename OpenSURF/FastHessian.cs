namespace OpenSURFcs;

using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;

public class FastHessian
{
  /// <summary>
  /// Reponse Layer 
  /// </summary>
  private class ResponseLayer
  {
    public readonly int Width;
    public readonly int Height;
    public readonly int Step;
    public readonly int Filter;
    public readonly float[] Responses;
    public readonly byte[] Laplacian;

    public ResponseLayer(int width, int height, int step, int filter)
    {
      Width = width;
      Height = height;
      Step = step;
      Filter = filter;

      Responses = new float[width * height];
      Laplacian = new byte[width * height];
    }

    public byte GetLaplacian(int row, int column)
    {
      return Laplacian[row * Width + column];
    }

    public byte GetLaplacian(int row, int column, ResponseLayer src)
    {
      var scale = Width / src.Width;
      return Laplacian[(scale * row) * Width + (scale * column)];
    }

    public float GetResponse(int row, int column)
    {
      return Responses[row * Width + column];
    }

    public float GetResponse(int row, int column, ResponseLayer src)
    {
      var scale = Width / src.Width;
      return Responses[(scale * row) * Width + (scale * column)];
    }
  }

  /// <summary>
  /// Static one-call do it all method
  /// </summary>
  /// <param name="thresh"></param>
  /// <param name="octaves"></param>
  /// <param name="init_sample"></param>
  /// <param name="img"></param>
  /// <returns></returns>
  public static List<IPoint> GetIpoints(float thresh, int octaves, int init_sample, IntegralImage img)
  {
    var fh = new FastHessian(thresh, octaves, init_sample, img);
    return fh.GetIpoints();
  }

  /// <summary>
  /// Constructor with parameters
  /// </summary>
  /// <param name="thresh"></param>
  /// <param name="octaves"></param>
  /// <param name="init_sample"></param>
  /// <param name="img"></param>
  public FastHessian(float thresh, int octaves, int init_sample, IntegralImage img)
  {
    this._thresh = thresh;
    this._octaves = octaves;
    this._initSample = init_sample;
    this._img = img;
  }

  /// <summary>
  /// These are passed in
  /// </summary>
  private readonly float _thresh;

  private readonly int _octaves;
  private readonly int _initSample;
  private readonly IntegralImage _img;

  /// <summary>
  /// These get built
  /// </summary>
  private List<IPoint> _ipts;

  private List<ResponseLayer> _responseMap;

  /// <summary>
  /// Find the image features and write into vector of features
  /// </summary>
  public List<IPoint> GetIpoints()
  {
    // filter index map
    int[,] filter_map = { { 0, 1, 2, 3 }, { 1, 3, 4, 5 }, { 3, 5, 6, 7 }, { 5, 7, 8, 9 }, { 7, 9, 10, 11 } };

    // Clear the vector of exisiting ipts
    if (_ipts == null) _ipts = new List<IPoint>();
    else _ipts.Clear();

    // Build the response map
    BuildResponseMap();

    // Get the response layers
    ResponseLayer b, m, t;
    for (var o = 0; o < _octaves; ++o)
    for (var i = 0; i <= 1; ++i)
    {
      b = _responseMap[filter_map[o, i]];
      m = _responseMap[filter_map[o, i + 1]];
      t = _responseMap[filter_map[o, i + 2]];

      // loop over middle response layer at density of the most 
      // sparse layer (always top), to find maxima across scale and space
      for (var r = 0; r < t.Height; ++r)
      {
        for (var c = 0; c < t.Width; ++c)
        {
          if (IsExtremum(r, c, t, m, b))
          {
            InterpolateExtremum(r, c, t, m, b);
          }
        }
      }
    }

    return _ipts;
  }

  /// <summary>
  /// Build map of DoH responses
  /// </summary>
  private void BuildResponseMap()
  {
    // Calculate responses for the first 4 octaves:
    // Oct1: 9,  15, 21, 27
    // Oct2: 15, 27, 39, 51
    // Oct3: 27, 51, 75, 99
    // Oct4: 51, 99, 147,195
    // Oct5: 99, 195,291,387

    // Deallocate memory and clear any existing response layers
    if (_responseMap == null) _responseMap = new List<ResponseLayer>();
    else _responseMap.Clear();

    // Get image attributes
    var w = (_img.Width / _initSample);
    var h = (_img.Height / _initSample);
    var s = (_initSample);

    // Calculate approximated determinant of hessian values
    if (_octaves >= 1)
    {
      _responseMap.Add(new ResponseLayer(w, h, s, 9));
      _responseMap.Add(new ResponseLayer(w, h, s, 15));
      _responseMap.Add(new ResponseLayer(w, h, s, 21));
      _responseMap.Add(new ResponseLayer(w, h, s, 27));
    }

    if (_octaves >= 2)
    {
      _responseMap.Add(new ResponseLayer(w / 2, h / 2, s * 2, 39));
      _responseMap.Add(new ResponseLayer(w / 2, h / 2, s * 2, 51));
    }

    if (_octaves >= 3)
    {
      _responseMap.Add(new ResponseLayer(w / 4, h / 4, s * 4, 75));
      _responseMap.Add(new ResponseLayer(w / 4, h / 4, s * 4, 99));
    }

    if (_octaves >= 4)
    {
      _responseMap.Add(new ResponseLayer(w / 8, h / 8, s * 8, 147));
      _responseMap.Add(new ResponseLayer(w / 8, h / 8, s * 8, 195));
    }

    if (_octaves >= 5)
    {
      _responseMap.Add(new ResponseLayer(w / 16, h / 16, s * 16, 291));
      _responseMap.Add(new ResponseLayer(w / 16, h / 16, s * 16, 387));
    }

    // Extract responses from the image
    for (var i = 0; i < _responseMap.Count; ++i)
    {
      BuildResponseLayer(_responseMap[i]);
    }
  }

  /// <summary>
  /// Build Responses for a given ResponseLayer
  /// </summary>
  /// <param name="rl"></param>
  private void BuildResponseLayer(ResponseLayer rl)
  {
    var step = rl.Step; // step size for this filter
    var b = (rl.Filter - 1) / 2; // border for this filter
    var l = rl.Filter / 3; // lobe for this filter (filter size / 3)
    var w = rl.Filter; // filter size
    var inverse_area = 1f / (w * w); // normalisation factor
    float Dxx, Dyy, Dxy;

    for (int r, c, ar = 0, index = 0; ar < rl.Height; ++ar)
    {
      for (var ac = 0; ac < rl.Width; ++ac, index++)
      {
        // get the image coordinates
        r = ar * step;
        c = ac * step;

        // Compute response components
        Dxx = _img.BoxIntegral(r - l + 1, c - b, 2 * l - 1, w)
              - _img.BoxIntegral(r - l + 1, c - l / 2, 2 * l - 1, l) * 3;
        Dyy = _img.BoxIntegral(r - b, c - l + 1, w, 2 * l - 1)
              - _img.BoxIntegral(r - l / 2, c - l + 1, l, 2 * l - 1) * 3;
        Dxy = +_img.BoxIntegral(r - l, c + 1, l, l)
              + _img.BoxIntegral(r + 1, c - l, l, l)
              - _img.BoxIntegral(r - l, c - l, l, l)
              - _img.BoxIntegral(r + 1, c + 1, l, l);

        // Normalise the filter responses with respect to their size
        Dxx *= inverse_area;
        Dyy *= inverse_area;
        Dxy *= inverse_area;

        // Get the determinant of hessian response & laplacian sign
        rl.Responses[index] = (Dxx * Dyy - 0.81f * Dxy * Dxy);
        rl.Laplacian[index] = (byte)(Dxx + Dyy >= 0 ? 1 : 0);
      }
    }
  }

  /// <summary>
  /// Test whether the point r,c in the middle layer is extremum in 3x3x3 neighbourhood
  /// </summary>
  /// <param name="r">Row to be tested</param>
  /// <param name="c">Column to be tested</param>
  /// <param name="t">Top ReponseLayer</param>
  /// <param name="m">Middle ReponseLayer</param>
  /// <param name="b">Bottome ReponseLayer</param>
  /// <returns></returns>
  private bool IsExtremum(int r, int c, ResponseLayer t, ResponseLayer m, ResponseLayer b)
  {
    // bounds check
    var layerBorder = (t.Filter + 1) / (2 * t.Step);
    if (r <= layerBorder || r >= t.Height - layerBorder || c <= layerBorder || c >= t.Width - layerBorder)
      return false;

    // check the candidate point in the middle layer is above thresh 
    var candidate = m.GetResponse(r, c, t);
    if (candidate < _thresh)
      return false;

    for (var rr = -1; rr <= 1; ++rr)
    {
      for (var cc = -1; cc <= 1; ++cc)
      {
        // if any response in 3x3x3 is greater candidate not maximum
        if (t.GetResponse(r + rr, c + cc) >= candidate ||
            ((rr != 0 || cc != 0) && m.GetResponse(r + rr, c + cc, t) >= candidate) ||
            b.GetResponse(r + rr, c + cc, t) >= candidate)
        {
          return false;
        }
      }
    }

    return true;
  }

  /// <summary>
  /// Interpolate scale-space extrema to subpixel accuracy to form an image feature
  /// </summary>
  /// <param name="r"></param>
  /// <param name="c"></param>
  /// <param name="t"></param>
  /// <param name="m"></param>
  /// <param name="b"></param>
  private void InterpolateExtremum(int r, int c, ResponseLayer t, ResponseLayer m, ResponseLayer b)
  {
    var D = Matrix.Create(BuildDerivative(r, c, t, m, b));
    var H = Matrix.Create(BuildHessian(r, c, t, m, b));
    var Hi = H.Inverse();
    var Of = -1 * Hi * D;

    // get the offsets from the interpolation
    double[] O = { Of[0, 0], Of[1, 0], Of[2, 0] };

    // get the step distance between filters
    var filterStep = (m.Filter - b.Filter);

    // If point is sufficiently close to the actual extremum
    if (Math.Abs(O[0]) < 0.5f && Math.Abs(O[1]) < 0.5f && Math.Abs(O[2]) < 0.5f)
    {
      var ipt = new IPoint();
      ipt.X = (float)((c + O[0]) * t.Step);
      ipt.Y = (float)((r + O[1]) * t.Step);
      ipt.Scale = (float)((0.1333f) * (m.Filter + O[2] * filterStep));
      ipt.Laplacian = (int)(m.GetLaplacian(r, c, t));
      _ipts.Add(ipt);
    }
  }

  /// <summary>
  /// Build Matrix of First Order Scale-Space derivatives
  /// </summary>
  /// <param name="octave"></param>
  /// <param name="interval"></param>
  /// <param name="row"></param>
  /// <param name="column"></param>
  /// <returns>3x1 Matrix of Derivatives</returns>
  private double[,] BuildDerivative(int r, int c, ResponseLayer t, ResponseLayer m, ResponseLayer b)
  {
    double dx, dy, ds;

    dx = (m.GetResponse(r, c + 1, t) - m.GetResponse(r, c - 1, t)) / 2f;
    dy = (m.GetResponse(r + 1, c, t) - m.GetResponse(r - 1, c, t)) / 2f;
    ds = (t.GetResponse(r, c) - b.GetResponse(r, c, t)) / 2f;

    double[,] D = { { dx }, { dy }, { ds } };
    return D;
  }

  /// <summary>
  /// Build Hessian Matrix 
  /// </summary>
  /// <param name="octave"></param>
  /// <param name="interval"></param>
  /// <param name="row"></param>
  /// <param name="column"></param>
  /// <returns>3x3 Matrix of Second Order Derivatives</returns>
  private double[,] BuildHessian(int r, int c, ResponseLayer t, ResponseLayer m, ResponseLayer b)
  {
    double v, dxx, dyy, dss, dxy, dxs, dys;

    v = m.GetResponse(r, c, t);
    dxx = m.GetResponse(r, c + 1, t) + m.GetResponse(r, c - 1, t) - 2 * v;
    dyy = m.GetResponse(r + 1, c, t) + m.GetResponse(r - 1, c, t) - 2 * v;
    dss = t.GetResponse(r, c) + b.GetResponse(r, c, t) - 2 * v;
    dxy = (m.GetResponse(r + 1, c + 1, t) - m.GetResponse(r + 1, c - 1, t) -
      m.GetResponse(r - 1, c + 1, t) + m.GetResponse(r - 1, c - 1, t)) / 4f;
    dxs = (t.GetResponse(r, c + 1) - t.GetResponse(r, c - 1) -
      b.GetResponse(r, c + 1, t) + b.GetResponse(r, c - 1, t)) / 4f;
    dys = (t.GetResponse(r + 1, c) - t.GetResponse(r - 1, c) -
      b.GetResponse(r + 1, c, t) + b.GetResponse(r - 1, c, t)) / 4f;

    var H = new double[3, 3];
    H[0, 0] = dxx;
    H[0, 1] = dxy;
    H[0, 2] = dxs;
    H[1, 0] = dxy;
    H[1, 1] = dyy;
    H[1, 2] = dys;
    H[2, 0] = dxs;
    H[2, 1] = dys;
    H[2, 2] = dss;
    return H;
  }
}
