using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;

class Program
{
  static async Task Main(string[] args)
  {
    if (args.Length == 0)
    {
      Console.WriteLine("Please provide a sentence as input.");
      return;
    }

    // Load environment variables from .env file
    Env.Load();

    // Get Azure keys from environment variables
    string subscriptionKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY");
    string serviceRegion = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION");

    if (string.IsNullOrEmpty(subscriptionKey) || string.IsNullOrEmpty(serviceRegion))
    {
      Console.WriteLine("Azure Speech subscription key or region is missing in .env file.");
      return;
    }

    if (args.Length == 0)
    {
      Console.WriteLine("Please provide a sentence as input.");
      return;
    }

    string sentence = args[0];
    string audioPath = "output.mp3";
    string videoPath = "output.mp4";

    // Generate audio using Azure Text-to-Speech
    await GenerateSpeechAsync(sentence, audioPath);

    // Generate the MP4 file
    GenerateMp4(sentence, audioPath, videoPath);

    Console.WriteLine($"MP4 file saved to {videoPath}");
  }

  static async Task GenerateSpeechAsync(string text, string audioPath)
  {
    var config = SpeechConfig.FromSubscription("YourSubscriptionKey", "YourServiceRegion");

    using var synthesizer = new SpeechSynthesizer(config, null);
    using var result = await synthesizer.SpeakTextAsync(text);

    if (result.Reason == ResultReason.SynthesizingAudioCompleted)
    {
      await File.WriteAllBytesAsync(audioPath, result.AudioData);
      Console.WriteLine($"Audio file saved to {audioPath}");
    }
    else
    {
      throw new Exception($"Error synthesizing speech: {result.Reason}");
    }
  }

  static void GenerateMp4(string text, string audioPath, string outputPath)
  {
    string videoFilter = $"drawtext=text='{text}':fontcolor=white:fontsize=48:x=(w-text_w)/2:y=(h-text_h)/2";
    string ffmpegArgs = $"-y -f lavfi -i color=c=black:s=1920x1080:d=5 -vf \"{videoFilter}\" -i {audioPath} -shortest -c:v libx264 -c:a aac {outputPath}";

    var process = new Process
    {
      StartInfo = new ProcessStartInfo
      {
        FileName = "ffmpeg",
        Arguments = ffmpegArgs,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
      }
    };

    process.Start();
    process.WaitForExit();

    if (process.ExitCode != 0)
    {
      string error = process.StandardError.ReadToEnd();
      throw new Exception($"FFmpeg error: {error}");
    }
  }
}
