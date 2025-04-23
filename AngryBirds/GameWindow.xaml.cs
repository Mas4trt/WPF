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
using System.Diagnostics;

namespace AngryBirds
{
    public partial class GameWindow : UserControl
    {
        private double _time;
        private double _startX;
        private double _startY;

        private double _maxHeight;
        private bool _simulationActive = false;

        private double _vx;
        private double _vy;

        private double _posX;
        private double _posY;

        private Polyline trajectoryLine;


        private const double g = 9.8;
        private const double dragCoefficient = 0.1;

        private DateTime _lastFrameTime;
        private EventHandler _renderHandler;

        public GameWindow()
        {
            InitializeComponent();

            MusicPlayer.Instance.PlayFromResource(Properties.Resources.GameMusic);

            _startX = Canvas.GetLeft(Bird);
            _startY = Canvas.GetTop(Bird);

            _renderHandler = new EventHandler(OnRender);
            CompositionTarget.Rendering += _renderHandler;

            Unloaded += GameControl_Unloaded;
        }

        private void GameControl_Unloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= _renderHandler;
            MusicPlayer.Instance.Stop();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsMenuControl.Visibility = Visibility.Visible;
        }

        private void StartSimulation_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(SpeedTextBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double velocity) &&
                double.TryParse(AngleTextBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double angle))
            {
                double angleRadians = angle * Math.PI / 180;
                _vx = velocity * Math.Cos(angleRadians);
                _vy = velocity * Math.Sin(angleRadians);

                _posX = 0;
                _posY = 0;

                _maxHeight = 0;
                _time = 0;
                _simulationActive = true;
                _lastFrameTime = DateTime.Now;

                Canvas.SetLeft(Bird, _startX);
                Canvas.SetTop(Bird, _startY);

                InputPanel.Visibility = Visibility.Collapsed;
                ResultsPanel.Visibility = Visibility.Visible;
                StatusText.Text = "Полет начался!";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                MessageBox.Show("Введите корректные значения скорости и угла.");
            }
        }

        private void OnRender(object sender, EventArgs e)
        {
            if (!_simulationActive) return;

            DateTime now = DateTime.Now;
            double deltaTime = (now - _lastFrameTime).TotalSeconds;
            _lastFrameTime = now;

            // Константы
            const double airDensity = 1.2; // кг/м³
            const double area = 0.01;      // м²
            const double mass = 0.15;      // кг
            const double Cd = 0.47;        // Коэф. сопротивления птички (примерный)

            double v = Math.Sqrt(_vx * _vx + _vy * _vy);
            if (v > 0.01)
            {
                double dragForce = 0.5 * Cd * airDensity * area * v * v;

                double ax = -dragForce * (_vx / v) / mass;
                double ay = (-g - (dragForce * (_vy / v)) / mass);

                _vx += ax * deltaTime;
                _vy += ay * deltaTime;
            }
            else
            {
                _vy -= g * deltaTime;
            }

            _posX += _vx * deltaTime;
            _posY += _vy * deltaTime;

            // Масштаб: 30 м высоты = MainGrid.Height
            double pixelsPerMeter = MainGrid.Height / 30.0;

            double newX = _startX + _posX * pixelsPerMeter;
            double newY = _startY - _posY * pixelsPerMeter;

            // Проверка на выход за границы
            if (newY >= MainGrid.Height || newY + Bird.ActualHeight <= 0 ||
                newX >= MainGrid.Width || newX + Bird.ActualWidth <= 0)
            {
                _simulationActive = false;
                StatusText.Text = "Полет завершён (вылет за экран)";
                StatusText.Foreground = System.Windows.Media.Brushes.DarkBlue;
                return;
            }

            // Обновление координат птички
            Canvas.SetLeft(Bird, newX);
            Canvas.SetTop(Bird, newY);

            if (_posY > _maxHeight)
                _maxHeight = _posY;

            MaxHeightText.Text = $"Макс. высота: {_maxHeight:F2} м";
            XCoordsText.Text = $"X: {_posX:F2} м";
            YCoordsText.Text = $"Y: {_posY:F2} м";

            CheckCollisions();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(SpeedTextBox.Text, out double speed) &&
                double.TryParse(AngleTextBox.Text, out double angle))
            {
                // Блокируем поля
                SpeedTextBox.IsEnabled = false;
                AngleTextBox.IsEnabled = false;

                // Прячем кнопку Далее, показываем Полет и Изменить
                NextButton.Visibility = Visibility.Collapsed;
                FlyButton.Visibility = Visibility.Visible;
                EditButton.Visibility = Visibility.Visible;

                // Показываем траекторию
                var points = CalculateTrajectory(angle, speed);
                DrawTrajectory(points);
            }
            else
            {
                MessageBox.Show("Введите корректные значения скорости и угла.");
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // Разрешаем редактирование
            SpeedTextBox.IsEnabled = true;
            AngleTextBox.IsEnabled = true;

            // Прячем кнопки Полет и Изменить, показываем Далее
            FlyButton.Visibility = Visibility.Collapsed;
            EditButton.Visibility = Visibility.Collapsed;
            NextButton.Visibility = Visibility.Visible;

            // Удаляем траекторию, если нужно
            ClearTrajectory();
        }

        private void FlyButton_Click(object sender, RoutedEventArgs e)
        {
            // Тут вызывается анимация полета
            InputPanel.Visibility = Visibility.Collapsed;
            StartFlight();
        }

        private void StartFlight()
        {
            if (double.TryParse(SpeedTextBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double velocity) &&
                double.TryParse(AngleTextBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double angle))
            {
                double angleRadians = angle * Math.PI / 180;
                _vx = velocity * Math.Cos(angleRadians);
                _vy = velocity * Math.Sin(angleRadians);

                _posX = 0;
                _posY = 0;

                _maxHeight = 0;
                _time = 0;
                _simulationActive = true;
                _lastFrameTime = DateTime.Now;

                Canvas.SetLeft(Bird, _startX);
                Canvas.SetTop(Bird, _startY);

                ResultsPanel.Visibility = Visibility.Visible;
                StatusText.Text = "Полет начался!";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                MessageBox.Show("Введите корректные значения скорости и угла.");
            }
        }

        private List<Point> CalculateTrajectory(double angleDegrees, double speed, double timeStep = 0.05)
        {
            double angleRadians = angleDegrees * Math.PI / 180;
            double vx = speed * Math.Cos(angleRadians);
            double vy = speed * Math.Sin(angleRadians);

            const double g = 9.8;
            const double airDensity = 1.2;
            const double area = 0.01;
            const double mass = 0.15;
            const double Cd = 0.47;

            List<Point> points = new List<Point>();

            double x = 0, y = 0;

            while (y >= 0)
            {
                points.Add(new Point(x, y));

                double v = Math.Sqrt(vx * vx + vy * vy);

                double dragForce = 0.5 * Cd * airDensity * area * v * v;

                double ax = -dragForce * (vx / v) / mass;
                double ay = (-g - (dragForce * (vy / v)) / mass);

                vx += ax * timeStep;
                vy += ay * timeStep;

                x += vx * timeStep;
                y += vy * timeStep;
            }

            return points;
        }

        private void DrawTrajectory(List<Point> points)
        {
            ClearTrajectory();

            trajectoryLine = new Polyline
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 } // пунктир
            };

            Panel.SetZIndex(trajectoryLine, 5);

            double pixelsPerMeter = MainGrid.Height / 30.0;

            double birdX = Canvas.GetLeft(Bird) + Bird.Width / 2;
            double birdY = Canvas.GetTop(Bird) + Bird.Height / 2;

            foreach (var p in points)
            {
                double canvasX = birdX + p.X * pixelsPerMeter;
                double canvasY = birdY - p.Y * pixelsPerMeter;

                trajectoryLine.Points.Add(new Point(canvasX, canvasY));
            }

            GameCanvas.Children.Add(trajectoryLine);
        }

        private void ClearTrajectory()
        {
            if (trajectoryLine != null && GameCanvas.Children.Contains(trajectoryLine))
            {
                GameCanvas.Children.Remove(trajectoryLine);
                trajectoryLine = null;
            }
        }

        private void CheckCollisions()
        {
            Rect birdRect = new Rect(
                Canvas.GetLeft(Bird),
                Canvas.GetTop(Bird),
                Bird.ActualWidth > 0 ? Bird.ActualWidth : Bird.RenderSize.Width,
                Bird.ActualHeight > 0 ? Bird.ActualHeight : Bird.RenderSize.Height);

            List<UIElement> childrenCopy = GameCanvas.Children.OfType<UIElement>().ToList();
            List<UIElement> toRemove = new List<UIElement>();

            foreach (UIElement element in childrenCopy)
            {
                if (element == Bird || element is Image img && (img.Name == "Slingshot" || img.Name.ToLower().Contains("slingshot"))) continue;

                Image image = element as Image;
                if (image == null) continue;

                double x = Canvas.GetLeft(image);
                double y = Canvas.GetTop(image);
                if (double.IsNaN(x) || double.IsNaN(y)) continue;

                double width = image.ActualWidth > 0 ? image.ActualWidth : image.RenderSize.Width;
                double height = image.ActualHeight > 0 ? image.ActualHeight : image.RenderSize.Height;

                Rect objRect = new Rect(x, y, width, height);
                if (birdRect.IntersectsWith(objRect))
                {
                    string name = image.Name.ToLower();

                    if (name.Contains("pig") || name.Contains("_y"))
                    {
                        toRemove.Add(image);
                        CreateHitEffect(x, y);
                        PlayHitSound();
                    }
                    else if (name.Contains("cube") )
                    {
                        toRemove.Add(image);
                        CreateHitEffect(x, y);
                        PlayHitSound();

                    }
                    else if (name.Contains("cube") || name.Contains("platform"))
                    {
                        // Логика отражения
                        Vector centerBird = new Vector(birdRect.Left + birdRect.Width / 2, birdRect.Top + birdRect.Height / 2);
                        Vector centerObj = new Vector(objRect.Left + objRect.Width / 2, objRect.Top + objRect.Height / 2);

                        Vector delta = centerBird - centerObj;

                        if (Math.Abs(delta.X) > Math.Abs(delta.Y))
                            _vx = -_vx * 0.7;
                        else
                            _vy = -_vy * 0.7;

                        CreateHitEffect(x, y);
                        PlayHitSound();
                    }

                }
            }

            foreach (var element in toRemove)
            {
                GameCanvas.Children.Remove(element);
            }
        }

        private void CreateHitEffect(double x, double y)
        {
            // Временный визуальный эффект на месте столкновения
            Ellipse effect = new Ellipse
            {
                Width = 30,
                Height = 30,
                Fill = Brushes.Red,
                Opacity = 0.7
            };

            Canvas.SetLeft(effect, x);
            Canvas.SetTop(effect, y);
            GameCanvas.Children.Add(effect);

            // Анимация исчезновения
            var fadeOut = new DoubleAnimation(0, TimeSpan.FromSeconds(0.5));
            fadeOut.Completed += (s, e) => GameCanvas.Children.Remove(effect);
            effect.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void PlayHitSound()
        {
            // Если используешь NAudio или MediaPlayer
            var player = new MediaPlayer();
            player.Open(new Uri("Sounds/hit.wav", UriKind.Relative));
            player.Volume = 0.5;
            player.Play();
        }


    }
}

