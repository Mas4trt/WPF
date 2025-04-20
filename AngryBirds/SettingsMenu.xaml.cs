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
    public partial class SettingsMenu : UserControl
    {
        private MusicPlayer musicPlayer = MusicPlayer.Instance;

        public SettingsMenu()
        {
            InitializeComponent();
        }

        private void MusicCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            musicPlayer.Restart();
        }

        private void MusicCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            musicPlayer.Pause();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            musicPlayer.SetVolume((float)e.NewValue);
        }

        private void CloseSettingsMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }

        private void ScreenModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (ScreenModeComboBox.SelectedIndex == 0)
            {
                parentWindow.WindowStyle = WindowStyle.None;
                parentWindow.WindowState = WindowState.Maximized;
            }
            else if (ScreenModeComboBox.SelectedIndex == 1)
            {
                parentWindow.WindowStyle = WindowStyle.SingleBorderWindow;
                parentWindow.WindowState = WindowState.Normal;
            }
        }

    }
}