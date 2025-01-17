using System;
using System.Drawing;
using System.IO;

public class PixMapConverter {

  public void ConvertFromString(string pixmapData, string outputFilePath = "output.bmp") {
    using (var reader = new StringReader(pixmapData)) Convert(reader, outputFilePath);
  }

  public void ConvertFromFile(string inputFilePath, string outputFilePath = "output.bmp") {
    using (var reader = new StreamReader(inputFilePath)) Convert(reader, outputFilePath);
  }

  private void Convert(TextReader reader, string outputFilePath) {

    string[] data = reader.ReadToEnd().Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

    if (data[0].ToUpper() != "P3") return;

    int width = int.Parse(data[1]);
    int height = int.Parse(data[2]);
    int maxColorValue = int.Parse(data[3]);

    if (maxColorValue != 255) return;

    using (var bitmap = new Bitmap(width, height)) {

      var index = 4;

      for (int y = 0; y < height; y++) {

        for (int x = 0; x < width; x++) {

          int r = int.Parse(data[index++]);
          int g = int.Parse(data[index++]);
          int b = int.Parse(data[index++]);

          bitmap.SetPixel(x, y, Color.FromArgb(r, g, b));
        }
      }

      bitmap.Save(outputFilePath);

    }

  }

}