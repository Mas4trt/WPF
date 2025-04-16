using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AngryBirdsClone
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void PlayButton_MouseEnter(object sender, MouseEventArgs e)
        {
            PlayButton.Opacity = 0.5;
        }

        private void PlayButton_MouseLeave(object sender, MouseEventArgs e)
        {
            PlayButton.Opacity = 0.8;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButton.Visibility = Visibility.Collapsed;
            InputPanel.Visibility = Visibility.Visible;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(SpeedTextBox.Text, out double v0) &&
                double.TryParse(AngleTextBox.Text, out double angleDeg) &&
                double.TryParse(TimeStepTextBox.Text, out double dt))
            {
                InputPanel.Visibility = Visibility.Collapsed;
                ResultsTable.Visibility = Visibility.Visible;

                DrawTrajectory(v0, angleDeg, dt);
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректные значения.");
            }
        }

        private void DrawTrajectory(double v0, double angleDeg, double dt)
        {
            GameCanvas.Children.Clear();
            List<TrajectoryPoint> results = new List<TrajectoryPoint>();

            double angleRad = angleDeg * Math.PI / 180;
            double vx = v0 * Math.Cos(angleRad);
            double vy = v0 * Math.Sin(angleRad);
            double g = 9.8;
            double t = 0;
            double x, y;

            Polyline trajectoryLine = new Polyline
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2
            };

            do
            {
                x = vx * t;
                y = vy * t - 0.5 * g * t * t;

                if (y < 0) break;

                trajectoryLine.Points.Add(new Point(x, 400 - y)); // 400 – это условный уровень земли

                results.Add(new TrajectoryPoint
                {
                    Time = t,
                    X = Math.Round(x, 2),
                    Y = Math.Round(y, 2)
                });

                t += dt;
            } while (true);

            GameCanvas.Children.Add(trajectoryLine);

            ResultsTable.ItemsSource = results;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public class TrajectoryPoint
        {
            public double Time { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
        }
    }
}

