using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Media;
using NAudio.Wave;
using System.IO;
using System.Drawing.Printing;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Xml.Linq;
using System.Windows.Threading;
using System.Runtime.Remoting.Messaging;
using AngryBirds;


namespace AngryBirds
{
    public partial class MainMenuWindow : Window
    {
        private MusicPlayer musicPlayer = MusicPlayer.Instance;

        public MainMenuWindow()
        {
            InitializeComponent();
            musicPlayer.PlayFromResource(Properties.Resources.MainMenu, 0.5f);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            musicPlayer.Stop();
            var gameWindow = new GameWindow();
            gameWindow.Show();
            this.Close();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsMenuControl.Visibility = Visibility.Visible;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            musicPlayer.Stop();
            Close();
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            musicPlayer.Stop();
            var gameWindow = new GameWindow();
            gameWindow.Show();
            this.Close();
        }
    }
    public class MusicPlayer
    {
            private AudioFileReader audioFileReader;
            private WaveOutEvent outputDevice;

            private static readonly MusicPlayer instance = new MusicPlayer();
            public static MusicPlayer Instance => instance;

            private MusicPlayer() { }

            public void PlayFromResource(Stream resourceStream, float volume = 0.5f)
            {
                Stop();

                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GameMusic.wav");
                using (var fileStream = File.Create(tempPath))
                {
                    resourceStream.CopyTo(fileStream);
                }

                audioFileReader = new AudioFileReader(tempPath) { Volume = volume };
                outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFileReader);
                outputDevice.Play();
            }

            public void Stop()
            {
                outputDevice?.Stop();
                outputDevice?.Dispose();
                audioFileReader?.Dispose();
                outputDevice = null;
                audioFileReader = null;
            }

            public void Pause()
            {
                outputDevice?.Pause();
            }

            public void Restart()
            {
                if (audioFileReader != null && outputDevice != null)
                {
                    outputDevice.Stop();
                    audioFileReader.Position = 0;
                    outputDevice.Play();
                }
            }

            public void SetVolume(float volume)
            {
                if (audioFileReader != null)
                    audioFileReader.Volume = volume;
            }
    }
}
