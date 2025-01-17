using System;
using System.IO;
using System.Linq;
using Pixmap;

class Program {
    static void Main(string[] args) {
        try {
            if (args.Length < 2) {
                Console.WriteLine("Usage: program <input-format> <output-format> [--raw-input] [--raw-output] [input-string-or-filepath]");
                return;
            }

            string inputFormat = args[0].ToLower();
            string outputFormat = args[1].ToLower();
            bool isRawInput = args.Contains("--raw-input");
            bool isRawOutput = args.Contains("--raw-output");

            string input = null;
            if (isRawInput) {
                int startIndex = Array.IndexOf(args, "--raw-input") + 1;
                input = string.Join(" ", args.Skip(startIndex).Where(arg => !arg.StartsWith("--")).ToArray()).Trim('"');
            } else if (args.Length > 2) {
                input = args[2].Trim('"');
            }

            if (isRawInput && input == null) {
                // Read from stdin
                input = Console.In.ReadToEnd();
            }

            if (isRawInput) {
                // Treat input as literal string
                Converter.ConvertData(input, inputFormat, outputFormat, isRawOutput);
            } else if (input != null && File.Exists(input)) {
                // Pass file path directly to converter
                Converter.ConvertDataFromFile(input, inputFormat, outputFormat, isRawOutput);
            } else {
                Console.WriteLine("Error: File does not exist and --raw-input flag is not set.");
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}