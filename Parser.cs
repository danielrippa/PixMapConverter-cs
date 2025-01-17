using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections;

namespace Pixmap {

  public class Parser {

    public static string[] ParseString(string input) {
      return input.Split(new[] { "\r\n", "\n", " " }, StringSplitOptions.RemoveEmptyEntries);
    }

    public static string[] ParseString(TextReader reader) {
      return ParseString(reader.ReadToEnd());
    }

    private static void ParseHeader(string[] data, string expectedFormat, out int width, out int height, out int maxColor, out int headerSize) {
      string actualFormat = data[0].Trim().ToUpper();
      string expectedFormatUpper = expectedFormat.ToUpper();
      if (actualFormat != expectedFormatUpper) {
        throw new InvalidDataException($"Invalid format: '{actualFormat}' (expected '{expectedFormatUpper}')");
      }

      width = int.Parse(data[1]);
      height = int.Parse(data[2]);
      if (expectedFormatUpper == "P1") {
        maxColor = 1;
        headerSize = 3;
      } else {
        maxColor = int.Parse(data[3]);
        headerSize = 4;
      }
    }

    private static PixelData ParseCommonData(string[] data, string expectedFormat, int expectedMaxColor, Func<string[], RGB[]> pixelParser) {
      int width, height, maxColor, headerSize;
      ParseHeader(data, expectedFormat, out width, out height, out maxColor, out headerSize);
      if (expectedFormat.ToUpper() != "P1" && maxColor != expectedMaxColor) {
        throw new InvalidDataException($"Invalid max color value, expected {expectedMaxColor}");
      }

      RGB[] pixels = pixelParser(data.Skip(headerSize).ToArray());

      return new PixelData(height, width, pixels);
    }

    public static PixelData ParseP3Data(string[] data) {
      return ParseCommonData(data, "P3", 255, d => {
        var pixels = new ArrayList();
        try {
          for (int i = 0; i < d.Length; i += 3) {
            pixels.Add(new RGB(int.Parse(d[i]), int.Parse(d[i + 1]), int.Parse(d[i + 2])));
          }
        } catch (Exception ex) {
          throw new InvalidDataException("Error parsing P3 data: " + ex.Message, ex);
        }
        return (RGB[])pixels.ToArray(typeof(RGB));
      });
    }

    public static PixelData ParseP1Data(string[] data) {
      return ParseCommonData(data, "P1", 1, d => {
        var pixels = new ArrayList();
        try {
          foreach (var line in d) {
            foreach (var c in line) {
              pixels.Add(c == '1' ? new RGB(255, 255, 255) : new RGB(0, 0, 0));
            }
          }
        } catch (Exception ex) {
          throw new InvalidDataException("Error parsing P1 data: " + ex.Message, ex);
        }
        return (RGB[])pixels.ToArray(typeof(RGB));
      });
    }

    public static PixelData ParseP2Data(string[] data) {
      return ParseCommonData(data, "P2", 255, d => {
        var pixels = new ArrayList();
        try {
          foreach (var gray in d) {
            int value = int.Parse(gray);
            pixels.Add(new RGB(value, value, value));
          }
        } catch (Exception ex) {
          throw new InvalidDataException("Error parsing P2 data: " + ex.Message, ex);
        }
        return (RGB[])pixels.ToArray(typeof(RGB));
      });
    }

    public static PixelData ParseXPixmapData(string[] data) {
      int width, height, _, headerSize;
      ParseHeader(data, "XPM2", out width, out height, out _, out headerSize);

      int numColors = int.Parse(data[headerSize]);
      int charsPerPixel = int.Parse(data[headerSize + 1]);

      var colorMap = new Hashtable();
      try {
        for (int i = headerSize + 2; i < headerSize + 2 + numColors; i++) {
          var line = data[i];
          var key = line.Substring(0, charsPerPixel);
          var colorValues = line.Substring(charsPerPixel + 1).Split(' ').Select(int.Parse).ToArray();
          colorMap[key] = new RGB(colorValues[0], colorValues[1], colorValues[2]);
        }
      } catch (Exception ex) {
        throw new InvalidDataException("Error parsing XPM2 color map: " + ex.Message, ex);
      }

      RGB[] pixels;
      try {
        pixels = data.Skip(headerSize + 2 + numColors)
                     .SelectMany(line => Enumerable.Range(0, width)
                                                   .Select(i => (RGB)colorMap[line.Substring(i * charsPerPixel, charsPerPixel)]))
                     .ToArray();
      } catch (Exception ex) {
        throw new InvalidDataException("Error parsing XPM2 pixel data: " + ex.Message, ex);
      }

      return new PixelData(height, width, pixels);
    }

    public static PixelData ParseSixelData(string data) {
      var lines = data.Split(new[] { '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries);
      var colorMap = lines.Where(line => line.StartsWith("#"))
                          .Select(line => line.Substring(1).Split(';'))
                          .ToDictionary(
                              parts => int.Parse(parts[0]),
                              parts => {
                                var rgbValues = parts[1].Split(',').Select(int.Parse).ToArray();
                                return new RGB(rgbValues[0], rgbValues[1], rgbValues[2]);
                              }
                          );

      var pixelLines = lines.Where(line => !line.StartsWith("#")).ToArray();
      int width = pixelLines.Max(line => line.Split(';').Length);
      int height = pixelLines.Length;

      RGB[] pixels = new RGB[width * height];
      try {
        for (int y = 0; y < height; y++) {
          var pixelIndices = pixelLines[y].Split(';').Select(int.Parse).ToArray();
          for (int x = 0; x < pixelIndices.Length; x++) {
            pixels[y * width + x] = colorMap[pixelIndices[x]];
          }
        }
      } catch (Exception ex) {
        throw new InvalidDataException("Error parsing Sixel pixel data: " + ex.Message, ex);
      }

      return new PixelData(height, width, pixels);
    }

    public static PixelData ParseBitmapData(byte[] data) {
      if (data == null || data.Length == 0) {
        throw new InvalidDataException("Bitmap data is null or empty.");
      }

      try {
        using (var ms = new MemoryStream(data))
        using (var bitmap = new Bitmap(ms)) {
          if (bitmap.Width == 0 || bitmap.Height == 0) {
            throw new InvalidDataException("Bitmap has invalid dimensions.");
          }

          int width = bitmap.Width;
          int height = bitmap.Height;
          RGB[] pixels = new RGB[width * height];
          for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
              var color = bitmap.GetPixel(x, y);
              pixels[y * width + x] = new RGB(color.R, color.G, color.B);
            }
          }
          return new PixelData(height, width, pixels);
        }
      } catch (ArgumentException ex) {
        throw new InvalidDataException("Error parsing bitmap data: The provided data does not represent a valid bitmap.", ex);
      } catch (Exception ex) {
        throw new InvalidDataException("Error parsing bitmap data: " + ex.Message, ex);
      }
    }

  }

}