using Microsoft.Data.Sqlite;
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
using System.Windows.Shapes;

namespace DrawCardGame
{
    /// <summary>
    /// Interaction logic for CardLibraryWindow.xaml
    /// </summary>
    public partial class CardLibraryWindow : Window
    {
        public CardLibraryWindow()
        {
            InitializeComponent();
            
        }
        private List<int> LoadOwnedCardIds()
        {
            var owned = new List<int>();
            using (var connection = new SqliteConnection("Data Source=user_cards.db"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT CardId FROM OwnedCards";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        owned.Add(reader.GetInt32(0));
                    }
                }
            }
            return owned;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var ownedIds = LoadOwnedCardIds();

            var cardLibrary = new List<Card>
      {
            new Card { Id = 1, Name = "Cat", ImagePath = "Images/card_cat.png" },
            new Card { Id = 2, Name = "Pikachu", ImagePath = "Images/card_pikachu.png" },
            new Card { Id = 3, Name = "Dog", ImagePath = "Images/card_dog.png" },
            new Card { Id = 4, Name = "Rabbit", ImagePath = "Images/card_rabbit.png" },
                new Card { Id = 5, Name = "Bicycle", ImagePath = "Images/card_bicycle.png" },
            new Card { Id = 6, Name = "BMW", ImagePath = "Images/card_bmw.png" },
            new Card { Id = 7, Name = "Book", ImagePath = "Images/card_book.png" },
            new Card { Id = 8, Name = "Bubble Tea", ImagePath = "Images/card_bubbletea.png" },
            new Card { Id = 9, Name = "F1 Car", ImagePath = "Images/card_f1_car.png" },
            new Card { Id = 10, Name = "French Fries", ImagePath = "Images/card_frenchfries.png" },
            new Card { Id = 11, Name = "Pen", ImagePath = "Images/card_pen.png" },
            new Card { Id = 12, Name = "Pencil", ImagePath = "Images/card_pencil.png" },
            new Card { Id = 13, Name = "Pizza", ImagePath = "Images/card_pizza.png" },
            new Card { Id = 14, Name = "Ruler", ImagePath = "Images/card_ruler.png" },
            new Card { Id = 15, Name = "AE86", ImagePath = "Images/card_AE86.png" },
            new Card { Id = 16, Name = "Japanese Anime Gundam", ImagePath = "Images/card__japanese_anime_gundam.png" },
            new Card { Id = 17, Name = "Honda Civic Type R", ImagePath = "Images/card_honda_civic_type_r.png" },
            new Card { Id = 18, Name = "Ice Cream", ImagePath = "Images/card_icecream.png" },
            new Card { Id = 19, Name = "Knife", ImagePath = "Images/card_knife.png" },
            new Card { Id = 20, Name = "MacBook", ImagePath = "Images/card_macbook.png" },
            new Card { Id = 21, Name = "Monkey D. Luffy", ImagePath = "Images/card_Monkey_D.Luffy.png" },
            new Card { Id = 22, Name = "Naruto", ImagePath = "Images/card_naruto.png" },
            new Card { Id = 23, Name = "Pistol", ImagePath = "Images/card_pistol.png" },
            new Card { Id = 24, Name = "Plane", ImagePath = "Images/card_plane.png" },
            new Card { Id = 25, Name = "Rifle", ImagePath = "Images/card_rifle.png" },
            new Card { Id = 26, Name = "Rononoa Zoro", ImagePath = "Images/card_Roronoa_zoro.png" },
            new Card { Id = 27, Name = "Shotgun", ImagePath = "Images/card_shotgun.png" },
            new Card { Id = 28, Name = "Sniper Rifle", ImagePath = "Images/card_sniper_rifle.png" },
            new Card { Id = 29, Name = "Subway", ImagePath = "Images/card_subway.png" },
            new Card { Id = 30, Name = "Train", ImagePath = "Images/card_train.png" },
            new Card { Id = 31, Name = "Cookie", ImagePath = "Images/card_cookies.png" },
            new Card { Id = 32, Name = "Birthdaycake", ImagePath = "Images/card_birthdaycake.png" }
};

            int totalSlots = 32;

            for (int i = 0; i < totalSlots; i++)
            {
                var cardPanel = new StackPanel { Margin = new Thickness(10), Width = 150 };

                var border = new Border
                {
                    Width = 130,
                    Height = 180,
                    Background = Brushes.Black,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8)
                };

                var image = new Image
                {
                    Width = 130,
                    Height = 180,
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(5)
                };

                var label = new TextBlock
                {
                    Text = "未知卡片",
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                if (i < cardLibrary.Count)
                {
                    var card = cardLibrary[i];
                    if (ownedIds.Contains(card.Id))
                    {
                        image.Source = new BitmapImage(new Uri($"pack://application:,,,/{card.ImagePath}"));
                        label.Text = card.Name;
                        border.Background = Brushes.White;
                    }
                    else
                    {
                        image.Source = new BitmapImage(new Uri("pack://application:,,,/Images/placeholder.png"));
                    }
                }
                else
                {
                    // 超过卡牌库长度，仍保留空槽
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/Images/placeholder.png"));
                }

                border.Child = image;
                cardPanel.Children.Add(border);
                cardPanel.Children.Add(label);
                CardWrapPanel.Children.Add(cardPanel);
            }
        }
        private void ClearCards_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要清空所有卡片记录吗？此操作无法撤销！", "确认操作", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                DatabaseHelper.ClearAllOwnedCards();
                MessageBox.Show("所有卡片记录已清空！");

                // 刷新界面
                CardWrapPanel.Children.Clear();
                Window_Loaded(null, null); // 重新加载
            }
        }



    }
}
