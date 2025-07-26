using Microsoft.Data.Sqlite;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DrawCardGame
{
    public partial class DrawCardWindow : Window
    {
        public DrawCardWindow()
        {
            InitializeComponent();
        }

        // ✅ 这里添加 CardLibrary：
        private List<Card> CardLibrary = new List<Card>
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



        // ✅ 倒计时相关变量
        private DispatcherTimer countdownTimer;
        private int secondsLeft = 3000;

        // ✅ 是否已抽卡标志
        private bool hasDrawn = false;

        private void TestCardLibrary()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var card in CardLibrary)
            {
                sb.AppendLine($"ID: {card.Id}, Name: {card.Name}, Path: {card.ImagePath}");
            }

            MessageBox.Show(sb.ToString(), "卡牌库内容测试");
        } // ✅ ← 这里加上这个括号


        private async void DrawCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (hasDrawn) return;

            Random rand = new Random();
            var selected = CardLibrary.OrderBy(x => rand.Next()).Take(2).ToList();

            hasDrawn = true;
            countdownTimer?.Stop();

            var card1 = selected[0];
            var card2 = selected[1];

           //OpenBoxAnimation.Stop();

            //PlayOpenBoxAnimation();

            await ShowCardsWithDelay(card1, card2);

            SaveCardToDatabase(card1.Id);
            SaveCardToDatabase(card2.Id);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            countdownTimer = new DispatcherTimer();
            countdownTimer.Interval = TimeSpan.FromSeconds(1);
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            secondsLeft--;

            CountdownText.Text = $"请在 {secondsLeft} 秒内抽卡";

            if (secondsLeft == 1 && !hasDrawn)
            {
                AutoDrawCards(); // 自动抽卡
            }

            if (secondsLeft <= 0)
            {
                countdownTimer.Stop();

                // 弹出对话框，用户确认后关闭窗口
                MessageBoxResult result = MessageBox.Show("本次抽卡已结束。点击确定关闭窗口。", "提示", MessageBoxButton.OK);

                if (result == MessageBoxResult.OK)
                {
                    this.Close(); // 关闭窗口
                }
            }
        }


        // ✅ 自动抽卡逻辑
        private async void AutoDrawCards()
        {
            Random rand = new Random();
            var selected = CardLibrary.OrderBy(x => rand.Next()).Take(2).ToList();

            Card1Image.Source = new BitmapImage(new Uri($"pack://application:,,,/{selected[0].ImagePath}"));
            Card2Image.Source = new BitmapImage(new Uri($"pack://application:,,,/{selected[1].ImagePath}"));

            hasDrawn = true;
            SaveCardToDatabase(selected[0].Id);
            SaveCardToDatabase(selected[1].Id);

            var card1 = selected[0];
            var card2 = selected[1];

            await ShowCardsWithDelay(card1, card2);

            //var popup = new DrawPopupWindow(card1.ImagePath, card2.ImagePath);
            //popup.ShowDialog();
        }


        private void SaveCardToDatabase(int cardId)
        {
            using (var connection = new SqliteConnection("Data Source=user_cards.db"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
            INSERT OR IGNORE INTO OwnedCards (CardId) 
            VALUES ($id);";
                command.Parameters.AddWithValue("$id", cardId);

                command.ExecuteNonQuery();
            }
        }
        private async Task ShowCardsWithDelay(Card card1, Card card2)
        {
            Card1Image.Source = null;
            Card2Image.Source = null;

            await Task.Delay(1000);
           // Card1Image.Width = 300;
            //Card1Image.Height = 260;
            //Card2Image.Width = 300;
            //Card2Image.Height = 260;

            Card1Image.Source = new BitmapImage(new Uri($"pack://application:,,,/{card1.ImagePath}"));
            Card1Image.Visibility = Visibility.Visible;

            Card2Image.Source = new BitmapImage(new Uri($"pack://application:,,,/{card2.ImagePath}"));
            Card2Image.Visibility = Visibility.Visible;


            //var popup = new DrawPopupWindow(card1.ImagePath, card2.ImagePath);
            //popup.ShowDialog();
        }

    }


  
}
