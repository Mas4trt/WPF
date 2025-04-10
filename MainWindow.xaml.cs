using System.Windows;

namespace YaroslavApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            string text = InputBox.Text;
            OutputText.Text = "Вы ввели: " + text;
        }
    }
}
