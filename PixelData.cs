using System;

namespace Pixmap {

  public struct RGB {
    public int R { get; }
    public int G { get; }
    public int B { get; }

    public RGB(int r, int g, int b) {
      R = r;
      G = g;
      B = b;
    }
  }

  public class PixelData {

    public int Height { get; }
    public int Width { get; }

    public RGB[] Pixels { get; }

    public PixelData(int height, int width, RGB[] pixels) {
      Height = height;
      Width = width;
      Pixels = pixels;
    }

  }

}