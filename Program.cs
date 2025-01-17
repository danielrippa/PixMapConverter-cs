using System;
using System.Drawing;
using System.IO;

class Program {

  static void Main(string[] args) {

    if (args.Length < 1 || args.Length > 2) {
      Console.WriteLine("Usage:");
      return;
    }

    string inputFilePath = args[0];
    string outputFilePath = args.Length == 2 ? args[1] : "output.bmp";

    try {

      var converter = new PixMapConverter();
      converter.ConvertFromFile(inputFilePath, outputFilePath);

    } catch (Exception ex) {
      Console.WriteLine($"Error: {ex.Message}");
    }

  }

}