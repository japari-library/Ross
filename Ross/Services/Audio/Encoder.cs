using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Ross.Services.Audio
{
    public class Encoder : IDisposable
    {
        string tempPath;

        ~Encoder()
        {
            Dispose();
        }

        public async Task<Stream> Encode(string path)
        {
            string outputPath = await Task.Run(() => EncodeFile(path)).ConfigureAwait(false);
            tempPath = outputPath;
            return (Stream)File.Open(outputPath, FileMode.Open);
        }

        private string EncodeFile(string path)
        {
            Log.Information($"[Encoder] Encoding {path}...");
            var tempPath = "./tmp";
            var tempFilename = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
            var outputPath = Path.Combine(tempPath, $"{tempFilename}.raw");
            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -b:a 96k -bufsize 100k -ar 48000 \"{outputPath}\" ",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            process.WaitForExit();
            Log.Information($"[Encoder] Encoded {path}!");
            return outputPath;
        }

        public void Dispose()
        {
            try
            {
                Log.Information($"[Encoder] Deleting temporary file {Path.GetFileName(tempPath)}...");
                File.Delete(tempPath);
            } catch (Exception e)
            {
                Log.Warning($"[Encoder] Unable to delete temporary file {Path.GetFileName(tempPath)}: {e.ToString()}");
            } finally
            {
                Log.Information($"[Encoder] Deleted temporary file {Path.GetFileName(tempPath)}");
            }
        }
    }
}
