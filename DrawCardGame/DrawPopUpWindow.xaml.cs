using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DrawCardGame
{
    public partial class DrawPopupWindow : Window
    {
        public DrawPopupWindow(string card1Path, string card2Path)
        {
            InitializeComponent();

            Card1Image.Source = new BitmapImage(new Uri($"pack://application:,,,/{card1Path}"));
            Card2Image.Source = new BitmapImage(new Uri($"pack://application:,,,/{card2Path}"));
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

