using NReco.VideoConverter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoutubeExplode;
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;

namespace YouTubeApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            // Initialise and create the save directory if it doesn't exist
            // TODO: Create a settings page so this can be set to a custom folder
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YouTube Downloads");
            Directory.CreateDirectory(folderPath);
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            // Disable the inputs
            InputToggle(false);

            // Get the string from the textbox
            // TODO: Implement a method to get the ID from a URL
            // TODO: Implement a method to insert a comma separated list of Ids
            string youTubeId = txtId.Text;

            // Check to see if there is an ID supplied
            if (youTubeId == "")
            {
                MessageBox.Show("Please enter a YouTube ID.", "Enter a value", MessageBoxButtons.OK, MessageBoxIcon.Information);
                InputToggle(true);
                return;
            }

            // Create a progress variable to help manage the progress bar
            Progress<double> progress = new Progress<double>(p => UpdateProgressBar(p));

            // Create a task for the downloader and the converter and run it asynchronous
            Task downloaderTask = RunDownloader(youTubeId, rbAudio.Checked, progress);

            // Wait for the task to finish before continuing - helps keep the UI thread in line with the task
            downloaderTask.Wait();

            // Show a success message and re-enable the inputs  
            MessageBox.Show("The download has been completed.");
            InputToggle(true);
        }

        private static async Task RunDownloader(string youTubeId, bool justAudio, Progress<double> progress)
        {
            // Create YouTube Client
            YoutubeClient client = new YoutubeClient();

            // Get the video info
            VideoInfo videoInfo = await client.GetVideoInfoAsync(youTubeId);

            // Get the stream info for audio and video
            AudioStreamInfo audioStreamInfo = videoInfo.AudioStreams.OrderBy(s => s.Bitrate).Last();
            MixedStreamInfo mixedStreamInfo = videoInfo.MixedStreams.OrderBy(s => s.VideoQuality).Last();

            // Get the file extension based on if we just need the audio
            string fileExtension = justAudio ? audioStreamInfo.Container.GetFileExtension() : mixedStreamInfo.Container.GetFileExtension();

            // Create the file name from the video title and extension
            string fileName = $"{videoInfo.Title}.{fileExtension}";

            // Check the file name is valid and doesn't include bad characters
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                // Create a list of invalid characters
                string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

                // If it does, then remove them character by character
                foreach (char c in invalid) { fileName = fileName.Replace(c.ToString(), ""); }
            }

            // Create the full file path
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YouTube Downloads", fileName);

            // Download the correct track for the selected media
            if (justAudio)
            {
                await client.DownloadMediaStreamAsync(audioStreamInfo, filePath, progress);

                // Create the FFMpeg Converter
                var ffMpeg = new FFMpegConverter();

                // Create the MP3 Path
                string mp3FilePath = filePath.Replace("webm", "mp3");

                // Convert the track to MP3
                ffMpeg.ConvertMedia(filePath, mp3FilePath, "mp3");

                // Delete the webm file 
                if (File.Exists(mp3FilePath)) { File.Delete(filePath); }
            }
            else { await client.DownloadMediaStreamAsync(mixedStreamInfo, filePath, progress); }
        }

        private void UpdateProgressBar(double p)
        {
            // Create a progress integer
            int progress = Convert.ToInt32(p * 100);

            // Update the progress bar
            progBarDownload.Value = progress;
            progBarDownload.Update();

            if (progress >= 100)
            {
                progBarDownload.Value = 0;
                progBarDownload.Update();
            }
        }

        /// <summary>
        /// This will toggle the inputs to be enabled or disabled depending on the passed value.
        /// </summary>
        /// <param name="toggle">If true, controls are enabled. If false, controls are disabled.</param>
        private void InputToggle(bool toggle)
        {
            btnDownload.Enabled = toggle;
            txtId.Enabled = toggle;
            rbAudio.Enabled = toggle;
            rbVideo.Enabled = toggle;
        }

    }
}