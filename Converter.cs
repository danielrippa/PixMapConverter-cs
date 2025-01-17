using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Collections;

namespace Pixmap {

  public static class Converter {

    private static string ConvertToPlainText(PixelData pixelData, Func<RGB, string> pixelConverter) {
      var sb = new StringBuilder();
      var lines = new ArrayList();
      var currentLine = new ArrayList();

      for (int i = 0; i < pixelData.Pixels.Length; i++) {
        currentLine.Add(pixelConverter(pixelData.Pixels[i]));
        if ((i + 1) % pixelData.Width == 0) {
          lines.Add(string.Join(" ", (string[])currentLine.ToArray(typeof(string))));
          currentLine.Clear();
        }
      }

      sb.Append(string.Join("\n", (string[])lines.ToArray(typeof(string))));
      return sb.ToString();
    }

    private static string ConvertToHeader(PixelData pixelData, string format, bool includeMaxColor = true) {
      var sb = new StringBuilder();
      sb.AppendLine(format);
      sb.AppendLine($"{pixelData.Width} {pixelData.Height}");
      if (includeMaxColor) {
        sb.AppendLine("255");
      }
      return sb.ToString();
    }

    public static string ToP1(PixelData pixelData) {
      return ConvertToHeader(pixelData, "P1", false) + ConvertToPlainText(pixelData, pixel => (pixel.R == 255 && pixel.G == 255 && pixel.B == 255) ? "1" : "0");
    }

    public static string ToP2(PixelData pixelData) {
      return ConvertToHeader(pixelData, "P2") + ConvertToPlainText(pixelData, pixel => ((pixel.R + pixel.G + pixel.B) / 3).ToString());
    }

    public static string ToP3(PixelData pixelData) {
      return ConvertToHeader(pixelData, "P3") + ConvertToPlainText(pixelData, pixel => $"{pixel.R} {pixel.G} {pixel.B}");
    }

    public static string ToXPM2(PixelData pixelData) {
      var sb = new StringBuilder();
      sb.AppendLine("XPM2");
      sb.AppendLine($"{pixelData.Width} {pixelData.Height}");
      sb.AppendLine("256");
      sb.AppendLine("1");

      var colorMap = new Hashtable();
      var colorList = new ArrayList();
      int colorIndex = 0;

      foreach (var pixel in pixelData.Pixels) {
        if (!colorMap.ContainsKey(pixel)) {
          colorMap[pixel] = colorIndex.ToString("X2");
          colorList.Add(pixel);
          colorIndex++;
        }
      }

      foreach (RGB color in colorList) {
        sb.AppendLine($"{colorMap[color]} c #{color.R:X2}{color.G:X2}{color.B:X2}");
      }

      for (int y = 0; y < pixelData.Height; y++) {
        for (int x = 0; x < pixelData.Width; x++) {
          var pixel = pixelData.Pixels[y * pixelData.Width + x];
          sb.Append(colorMap[pixel]);
        }
        sb.AppendLine();
      }

      return sb.ToString();
    }

    public static string ToSixel(PixelData pixelData) {
      var sb = new StringBuilder();
      var colorMap = new Hashtable();
      var colorList = new ArrayList();
      int colorIndex = 0;

      foreach (var pixel in pixelData.Pixels) {
        if (!colorMap.ContainsKey(pixel)) {
          colorMap[pixel] = colorIndex;
          colorList.Add(pixel);
          colorIndex++;
        }
      }

      foreach (RGB color in colorList) {
        sb.AppendLine($"#{colorMap[color]};{color.R},{color.G},{color.B}");
      }

      for (int y = 0; y < pixelData.Height; y++) {
        for (int x = 0; x < pixelData.Width; x++) {
          var pixel = pixelData.Pixels[y * pixelData.Width + x];
          sb.Append(colorMap[pixel]);
          if (x < pixelData.Width - 1) sb.Append(";");
        }
        sb.AppendLine();
      }

      return sb.ToString();
    }

    public static byte[] ToBitmap(PixelData pixelData) {
      using (var bitmap = new Bitmap(pixelData.Width, pixelData.Height)) {
        for (int y = 0; y < pixelData.Height; y++) {
          for (int x = 0; x < pixelData.Width; x++) {
            var pixel = pixelData.Pixels[y * pixelData.Width + x];
            bitmap.SetPixel(x, y, Color.FromArgb(pixel.R, pixel.G, pixel.B));
          }
        }
        using (var ms = new MemoryStream()) {
          bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
          var bitmapBytes = ms.ToArray();
          return bitmapBytes;
        }
      }
    }

    public static string ToBase64(PixelData pixelData) {
      var bitmapBytes = ToBitmap(pixelData);
      return Convert.ToBase64String(bitmapBytes);
    }

    public static PixelData FromBase64(string base64) {
      var bitmapBytes = Convert.FromBase64String(base64);
      return Parser.ParseBitmapData(bitmapBytes);
    }

    public static void SaveToFile(PixelData pixelData, string filePath, string format) {
      switch (format.ToLower()) {
        case "p1":
          File.WriteAllText(filePath, ToP1(pixelData));
          break;
        case "p2":
          File.WriteAllText(filePath, ToP2(pixelData));
          break;
        case "p3":
          File.WriteAllText(filePath, ToP3(pixelData));
          break;
        case "xpm":
          File.WriteAllText(filePath, ToXPM2(pixelData));
          break;
        case "sxl":
          File.WriteAllText(filePath, ToSixel(pixelData));
          break;
        case "bmp":
          File.WriteAllBytes(filePath, ToBitmap(pixelData));
          break;
        case "b64":
          File.WriteAllText(filePath, ToBase64(pixelData));
          break;
        default:
          throw new ArgumentException("Unsupported format");
      }
    }

    public static PixelData LoadFromFile(string filePath, string format) {
      switch (format.ToLower()) {
        case "p1":
        case "p2":
        case "p3":
        case "xpm":
        case "sxl":
          var data = File.ReadAllText(filePath);
          switch (format.ToLower()) {
            case "p1":
              return Parser.ParseP1Data(Parser.ParseString(data));
            case "p2":
              return Parser.ParseP2Data(Parser.ParseString(data));
            case "p3":
              return Parser.ParseP3Data(Parser.ParseString(data));
            case "xpm":
              return Parser.ParseXPixmapData(Parser.ParseString(data));
            case "sxl":
              return Parser.ParseSixelData(data);
            default:
              throw new ArgumentException("Unsupported format");
          }
        case "bmp":
          var bitmapData = File.ReadAllBytes(filePath);
          return Parser.ParseBitmapData(bitmapData);
        case "b64":
          var base64Data = File.ReadAllText(filePath);
          return FromBase64(base64Data);
        default:
          throw new ArgumentException("Unsupported format");
      }
    }

    public static PixelData LoadFromString(string input, string format) {
      var data = Parser.ParseString(input);
      switch (format.ToLower()) {
        case "p1":
          return Parser.ParseP1Data(data);
        case "p2":
          return Parser.ParseP2Data(data);
        case "p3":
          return Parser.ParseP3Data(data);
        case "xpm":
          return Parser.ParseXPixmapData(data);
        case "sxl":
          return Parser.ParseSixelData(input);
        case "b64":
          var imageData = Convert.FromBase64String(input);
          if (imageData.Length == 0) {
            throw new ArgumentException("Decoded image data is empty.");
          }
          if (!IsValidBitmap(imageData)) {
            throw new ArgumentException("Decoded image data does not represent a valid bitmap.");
          }
          return Parser.ParseBitmapData(imageData);
        default:
          throw new ArgumentException($"Unsupported format: {format}");
      }
    }

    public static string ConvertToString(PixelData pixelData, string format) {
      switch (format.ToLower()) {
        case "p1":
          return ToP1(pixelData);
        case "p2":
          return ToP2(pixelData);
        case "p3":
          return ToP3(pixelData);
        case "xpm":
          return ToXPM2(pixelData);
        case "sxl":
          return ToSixel(pixelData);
        case "bmp":
          var bitmapBytes = ToBitmap(pixelData);
          return Convert.ToBase64String(bitmapBytes);
        case "b64":
          return ToBase64(pixelData);
        default:
          throw new ArgumentException($"Unsupported format: {format}");
      }
    }

    private static bool IsValidBitmap(byte[] data) {
      try {
        using (var ms = new MemoryStream(data)) {
          using (var bitmap = new Bitmap(ms)) {
            bool isValid = bitmap.Width > 0 && bitmap.Height > 0;
            return isValid;
          }
        }
      } catch (Exception) {
        return false;
      }
    }

    private static void SaveBitmapToFile(byte[] data, string filePath) {
      File.WriteAllBytes(filePath, data);
    }

    private static void WriteOutputToFile(string inputFormat, string outputFormat, string output) {
      string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
      string filePath = $"{inputFormat}-{outputFormat}-{timestamp}.{outputFormat}";
      File.WriteAllText(filePath, output);
      Console.WriteLine(filePath);
    }

    private static void WriteBinaryOutputToFile(string inputFormat, string outputFormat, byte[] output) {
      string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
      string filePath = $"{inputFormat}-{outputFormat}-{timestamp}.{outputFormat}";
      File.WriteAllBytes(filePath, output);
      Console.WriteLine(filePath);
    }

    public static void ConvertDirectly(string input, string inputFormat, string outputFormat) {
      string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
      string filePath = $"{inputFormat}-{outputFormat}-{timestamp}.{outputFormat}";

      if (inputFormat == "b64" && outputFormat == "bmp") {
        var imageData = Convert.FromBase64String(input);
        WriteBinaryOutputToFile(inputFormat, outputFormat, imageData);
      } else if (inputFormat == "bmp" && outputFormat == "b64") {
        var imageData = File.ReadAllBytes(input);
        string base64String = Convert.ToBase64String(imageData);
        WriteOutputToFile(inputFormat, outputFormat, base64String);
      } else {
        throw new ArgumentException($"Unsupported conversion from {inputFormat} to {outputFormat}");
      }
    }

    public static void ConvertData(string input, string inputFormat, string outputFormat, bool isRawOutput) {
      if (CanConvertDirectly(inputFormat, outputFormat)) {
        ConvertDirectly(input, inputFormat, outputFormat);
        return;
      }

      if (inputFormat == "sxl") {
        input = input.Replace("\n", "").Replace("\r", "");
      }

      PixelData pixelData = LoadFromString(input, inputFormat);
      if (outputFormat == "bmp") {
        var bitmapBytes = ToBitmap(pixelData);
        WriteBinaryOutputToFile(inputFormat, outputFormat, bitmapBytes);
      } else {
        string output = ConvertToString(pixelData, outputFormat);
        if (isRawOutput) {
          Console.WriteLine(output);
        } else {
          WriteOutputToFile(inputFormat, outputFormat, output);
        }
      }
    }

    public static void ConvertDataFromFile(string filePath, string inputFormat, string outputFormat, bool isRawOutput) {
      if (CanConvertDirectly(inputFormat, outputFormat)) {
        ConvertDirectly(File.ReadAllText(filePath), inputFormat, outputFormat);
        return;
      }
      PixelData pixelData = LoadFromFile(filePath, inputFormat);
      if (outputFormat == "bmp") {
        var bitmapBytes = ToBitmap(pixelData);
        WriteBinaryOutputToFile(inputFormat, outputFormat, bitmapBytes);
      } else {
        string output = ConvertToString(pixelData, outputFormat);
        if (isRawOutput) {
          Console.WriteLine(output);
        } else {
          WriteOutputToFile(inputFormat, outputFormat, output);
        }
      }
    }

    private static bool CanConvertDirectly(string inputFormat, string outputFormat) {
      return (inputFormat.ToLower() == "b64" && outputFormat.ToLower() == "bmp") ||
             (inputFormat.ToLower() == "bmp" && outputFormat.ToLower() == "b64");
    }

  }

}