﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using NAudio.Wave;

namespace CipherLink.Plugins
{
    public class YouTubeDownloaderPlugin : IPlugin
    {
        private const string ConfigFolder = "Config";
        private const string ConfigFileName = "youtubedl.conf";
        private string ConfigFilePath => Path.Combine(ConfigFolder, ConfigFileName);
        private string downloadPath;

        public string Name => "youtubedl";

        public YouTubeDownloaderPlugin()
        {
            if (!Directory.Exists(ConfigFolder))
            {
                Directory.CreateDirectory(ConfigFolder);
            }

            if (File.Exists(ConfigFilePath))
            {
                downloadPath = ReadConfig(ConfigFilePath, "downloadPath");
            }
        }

        public void Execute(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: youtubedl <url> [--audio] [--setpath]");
                return;
            }

            if (args.Contains("--setpath"))
            {
                PromptForDownloadPath();
                return;
            }

            if (string.IsNullOrEmpty(downloadPath))
            {
                Console.WriteLine("Download path not set. Use --setpath to set the download location.");
                return;
            }

            string url = args[0];
            DownloadVideo(url).Wait();
        }

        private async Task DownloadVideo(string url)
        {
            try
            {
                var youtube = new YoutubeClient();
                var video = await youtube.Videos.GetAsync(url);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

                // Get the stream with the highest video quality
                var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

                // Determine the output path
                string extension = streamInfo.Container.Name == "mp4" ? ".mp4" : ".mp3";
                string sanitizedTitle = string.Concat(video.Title.Split(Path.GetInvalidFileNameChars())).Trim();
                string outputPath = Path.Combine(downloadPath, $"{sanitizedTitle}{extension}");

                Console.Write($"Downloading '{video.Title}' to '{outputPath}'...");

                var tempFilePath = Path.GetTempFileName();

                await youtube.Videos.Streams.DownloadAsync(streamInfo, tempFilePath);

                if (streamInfo.Container.Name == "mp4")
                {
                    File.Move(tempFilePath, outputPath, true);
                }
                else if (streamInfo.Container.Name == "mp3")
                {
                    ConvertToMp3(tempFilePath, outputPath);
                }

                Console.WriteLine("\nDownload completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError downloading video: {ex.Message}");
            }
        }

        private void ConvertToMp3(string inputPath, string outputPath)
        {
            try
            {
                using (var reader = new MediaFoundationReader(inputPath))
                {
                    MediaFoundationEncoder.EncodeToMp3(reader, outputPath);
                }

                File.Delete(inputPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError converting to MP3: {ex.Message}");
            }
        }

        private void PromptForDownloadPath()
        {
            Console.WriteLine("Select download location:");

            var thread = new System.Threading.Thread((ThreadStart)(() =>
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    DialogResult result = dialog.ShowDialog();
                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                    {
                        downloadPath = dialog.SelectedPath;
                        WriteConfig(ConfigFilePath, "downloadPath", downloadPath);
                    }
                    else
                    {
                        Console.WriteLine("No folder selected. Exiting.");
                        Environment.Exit(0);
                    }
                }
            }));

            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        private string ReadConfig(string fileName, string key)
        {
            if (!File.Exists(fileName))
            {
                return null;
            }

            var lines = File.ReadAllLines(fileName);
            foreach (var line in lines)
            {
                var parts = line.Split('=');
                if (parts.Length == 2 && parts[0].Trim() == key)
                {
                    return parts[1].Trim();
                }
            }

            return null;
        }

        private void WriteConfig(string fileName, string key, string value)
        {
            var lines = File.Exists(fileName) ? File.ReadAllLines(fileName).ToList() : new List<string>();

            var found = false;
            for (int i = 0; i < lines.Count; i++)
            {
                var parts = lines[i].Split('=');
                if (parts.Length == 2 && parts[0].Trim() == key)
                {
                    lines[i] = $"{key}={value}";
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                lines.Add($"{key}={value}");
            }

            File.WriteAllLines(fileName, lines);
        }
    }
}
