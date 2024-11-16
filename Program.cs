using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetEnv;
using Microsoft.CognitiveServices.Speech;

// run like dotnet run "Atmosphere"

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
    string? subscriptionKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY");
    string? serviceRegion = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION");

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
    await GenerateSpeechAsync(sentence, audioPath, subscriptionKey, serviceRegion);

    // Generate the MP4 file
    GenerateMp4(sentence, audioPath, videoPath);

    Console.WriteLine($"MP4 file saved to {videoPath}");
  }

  static async Task GenerateSpeechAsync(string text, string audioPath, string subscriptionKey, string serviceRegion)
  {
    var config = SpeechConfig.FromSubscription(subscriptionKey, serviceRegion);

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
    // Escape single quotes in the input text for FFmpeg
    string escapedInputText = text.Replace("'", "\\'");
    string title = "How to Pronounce Incorrectly";

    // Temporary path for the intermediate video (without audio)
    string tempVideoPath = "temp_video.mp4";

    // Step 1: Generate the black background video with text overlay
    string videoFilter = $"drawtext=text='{title}':fontcolor=white:fontsize=48:x=(w-text_w)/2:y=(h-text_h)/2-50," +
                         $"drawtext=text='{escapedInputText}':fontcolor=white:fontsize=48:x=(w-text_w)/2:y=(h-text_h)/2+50";

    string generateVideoArgs = $"-y -f lavfi -i color=c=black:s=1920x1080:d=5 -vf \"{videoFilter}\" -c:v libx264 \"{tempVideoPath}\"";

    RunFfmpegCommand(generateVideoArgs);

    // Step 2: Attach the MP3 audio to the video
    string attachAudioArgs = $"-y -i \"{tempVideoPath}\" -i \"{audioPath}\" -c:v libx264 -c:a aac -strict experimental -shortest \"{outputPath}\"";

    RunFfmpegCommand(attachAudioArgs);

    // Clean up temporary video file
    if (File.Exists(tempVideoPath))
    {
      File.Delete(tempVideoPath);
    }

    Console.WriteLine($"MP4 file saved to {outputPath}");
  }

  static void RunFfmpegCommand(string arguments)
  {
    var process = new Process
    {
      StartInfo = new ProcessStartInfo
      {
        FileName = "ffmpeg",
        Arguments = arguments,
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