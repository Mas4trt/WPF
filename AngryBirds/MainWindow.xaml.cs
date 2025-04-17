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


namespace AngryBirds
{
    public partial class MainWindow : Window
    {
        private double velocity;
        private double angle;
        private double timeStep;
        private double gravity = 9.8;

        private double time;
        private double startX;
        private double startY;
        private double scale = 1.0;

        private List<Point> trajectoryPoints = new List<Point>();

        private EventHandler renderHandler;
        private bool isFlying = false;

        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;

        public MainWindow()
        {
            InitializeComponent();
            InitAudio();
        }

        private void InitAudio()
        {
            try
            {
                string tempAudioPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MainMenu.wav");

                using (var resourceStream = new MemoryStream())
                {
                    Properties.Resources.MainMenu.CopyTo(resourceStream);
                    File.WriteAllBytes(tempAudioPath, resourceStream.ToArray());
                }

                audioFile = new AudioFileReader(tempAudioPath);
                outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFile);
                outputDevice.Play();

                if (VolumeSlider != null)
                {
                    VolumeSlider.Value = 0.5;
                    audioFile.Volume = 0.5f;
                    VolumeSlider.IsEnabled = true;
                    VolumeSlider.Opacity = 1.0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при воспроизведении музыки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MusicCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (outputDevice != null && audioFile != null)
            {
                audioFile.Position = 0;
                outputDevice.Play();
                if (VolumeSlider != null)
                {
                    VolumeSlider.IsEnabled = true;
                    VolumeSlider.Opacity = 1.0;
                }
            }
        }

        private void MusicCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            outputDevice?.Pause();
            if (VolumeSlider != null)
            {
                VolumeSlider.IsEnabled = false;
                VolumeSlider.Opacity = 0.5;
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (audioFile != null)
            {
                audioFile.Volume = (float)e.NewValue;
            }
        }

        private void ScreenModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ScreenModeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string mode = selectedItem.Content.ToString();

                if (mode == "Fullscreen")
                {
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Maximized;
                }
                else if (mode == "Windowed")
                {
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.WindowState = WindowState.Normal;
                }
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                outputDevice?.Stop();
                audioFile?.Dispose();

                string tempAudioPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MainMenu.wav");
                File.WriteAllBytes(tempAudioPath, ReadStreamToByteArray(Properties.Resources.GameMusic));

                audioFile = new AudioFileReader(tempAudioPath)
                {
                    Volume = VolumeSlider != null ? (float)VolumeSlider.Value : 0.5f
                };

                outputDevice.Init(audioFile);
                outputDevice.Play();

                MainGrid.Background = new ImageBrush(
                    new BitmapImage(new Uri("pack://application:,,,/Resources/GameBackground.jpg", UriKind.Absolute))
                );
                ButtonsPanel.Visibility = Visibility.Collapsed;
                ResultsPanel.Visibility = Visibility.Collapsed;
                Slingshot.Visibility = Visibility.Visible;
                ResultsPanel.Visibility = Visibility.Visible;


                Dispatcher.InvokeAsync(() =>
                {
                    InputPanel.Visibility = Visibility.Visible;
                }, DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при запуске игры: " + ex.Message);
            }

            byte[] ReadStreamToByteArray(Stream stream)
            {
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        private void StartSimulation_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(SpeedTextBox.Text, out velocity) ||
                !double.TryParse(AngleTextBox.Text, out angle))
            {
                MessageBox.Show("Пожалуйста, введите корректные значения.");
                return;
            }

            timeStep = 0.05;
            velocity *= 2.5;
            angle = angle * Math.PI / 180;
            time = 0;
            // начальная позиция
            startX = 100;
            startY = 300;
            trajectoryPoints.Clear();
            StatusText.Text = "Полет...";

            Bird.Visibility = Visibility.Visible;
            Panel.SetZIndex(Bird, 2);
            Panel.SetZIndex(Slingshot, 1);

            // установка координат
            Canvas.SetLeft(Bird, startX);
            Canvas.SetTop(Bird, startY);

            StartBirdFlight();
        }


        private void StartBirdFlight()
        {
            isFlying = true;
            time = 0;

            renderHandler = new EventHandler(OnRenderFrame);
            CompositionTarget.Rendering += renderHandler;
        }

        private void OnRenderFrame(object sender, EventArgs e)
        {
            if (!isFlying) return;

            time += timeStep;

            double x = velocity * Math.Cos(angle) * time;
            double y = velocity * Math.Sin(angle) * time - 0.5 * gravity * time * time;

            double currentX = startX + x * scale;
            double currentY = startY - y * scale;

            Bird.Margin = new Thickness(currentX, currentY, 0, 0);
            trajectoryPoints.Add(new Point(currentX, currentY));

            if (currentY > MainGrid.ActualHeight || currentX > MainGrid.ActualWidth || y < 0)
            {
                isFlying = false;
                CompositionTarget.Rendering -= renderHandler;

                double maxY = trajectoryPoints.Min(p => p.Y);
                MaxHeightText.Text = $"Макс. высота: {Math.Round(startY - maxY, 2)}";
                XCoordsText.Text = $"X: {Math.Round(currentX, 2)}";
                YCoordsText.Text = $"Y: {Math.Round(startY - currentY, 2)}";
                StatusText.Text = "Полёт завершён";

                ResultsPanel.Visibility = Visibility.Visible;

                // 🎯 Вернуть птичку на стартовую позицию
                Canvas.SetLeft(Bird, startX);
                Canvas.SetTop(Bird, startY);
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsMenu.Visibility = Visibility.Visible;
        }

        private void CloseSettingsMenu_Click(object sender, RoutedEventArgs e)
        {
            SettingsMenu.Visibility = Visibility.Collapsed;
        }

        private void Levels_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Здесь будут уровни!");
        }

        protected override void OnClosed(EventArgs e)
        {
            outputDevice?.Dispose();
            audioFile?.Dispose();
            base.OnClosed(e);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
