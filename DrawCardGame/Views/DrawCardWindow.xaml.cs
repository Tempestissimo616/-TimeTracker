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
            // 更多卡片...
        };

        // ✅ 倒计时相关变量
        private DispatcherTimer countdownTimer;
        private int secondsLeft = 30;

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

            Card1Image.Source = new BitmapImage(new Uri($"pack://application:,,,/{selected[0].ImagePath}"));
            Card2Image.Source = new BitmapImage(new Uri($"pack://application:,,,/{selected[1].ImagePath}"));

            hasDrawn = true; // 标记为已抽卡
            countdownTimer?.Stop(); // 如果点击了就不再等倒计时
            SaveCardToDatabase(selected[0].Id);
            SaveCardToDatabase(selected[1].Id);

            var card1 = selected[0];
            var card2 = selected[1];

            await ShowCardsWithDelay(card1, card2);

            //var popup = new DrawPopupWindow(card1.ImagePath, card2.ImagePath);
            //popup.ShowDialog();
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

        private void OpenCardLibrary_Click(object sender, RoutedEventArgs e)
        {
            CardLibraryWindow cardLibraryWindow = new CardLibraryWindow();
            cardLibraryWindow.Show(); // 使用 Show() 表示非模态窗口，ShowDialog() 是模态窗口
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

            await Task.Delay(5000);
            Card1Image.Source = new BitmapImage(new Uri($"pack://application:,,,/{card1.ImagePath}"));

            Card2Image.Source = new BitmapImage(new Uri($"pack://application:,,,/{card2.ImagePath}"));

            //var popup = new DrawPopupWindow(card1.ImagePath, card2.ImagePath);
            //popup.ShowDialog();
        }

    }


  
}
