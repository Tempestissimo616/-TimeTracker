using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;
namespace Wallet_Payment
{

    public class Card
    {
        public int Id { get; set; }              // 卡片编号
        public string Name { get; set; }         // 卡片名称
        public string ImagePath { get; set; }    // 图片路径，例如 "Images/card1.png"
        public bool IsOwned { get; set; } = false; // 是否已拥有，默认是 false（未拥有）
    }

    public partial class DrawCardWindow : Window
    {
        private DispatcherTimer timeTimer;

        public int FocusMinutes { get; set; }
        public string FocusMode { get; set; }
        private int focusMinutes;
        private int restMinutes;
        private int secondsLeft;


        public DrawCardWindow(int focusMinutes, int restMinutes)
        {
            InitializeComponent();
            this.focusMinutes = focusMinutes;
            this.restMinutes = restMinutes;
            this.secondsLeft = focusMinutes * 60; // 默认用专注时间倒计时
            InitializeTimer();
            UpdateFocusInfo();
        }

        //public DrawCardWindow(int focusMinutes, string focusMode) : this()
        //{
        //    FocusMinutes = focusMinutes;
        //    FocusMode = focusMode;
        //    UpdateFocusInfo();
        //}


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



        private void InitializeTimer()
        {
            timeTimer = new DispatcherTimer();
            timeTimer.Interval = TimeSpan.FromSeconds(1);
            timeTimer.Tick += TimeTimer_Tick;
            timeTimer.Start();

            // 设置初始时间
            UpdateTimeDisplay();
        }

        private void TimeTimer_Tick(object sender, EventArgs e)
        {
            UpdateTimeDisplay();
        }

        private void UpdateTimeDisplay()
        {
            if (secondsLeft > 0)
            {
                secondsLeft--;
                TimeTextBlock.Text = $"{secondsLeft / 60:D2}:{secondsLeft % 60:D2}";
            }
            else
            {
                // 倒计时结束，关闭窗口
                timeTimer?.Stop();
                this.Close();
            }
        }

        private void UpdateFocusInfo()
        {
            if (FocusMinutes > 0)
            {
                FocusInfoTextBlock.Text = $"本次专注 {FocusMinutes} 分钟";
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 允许拖拽窗口
            this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            timeTimer?.Stop();
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            timeTimer?.Stop();
            base.OnClosed(e);
        }

        // ✅ 是否已抽卡标志
        private bool hasDrawn = false;

        private async void GifButton_Click(object sender, RoutedEventArgs e)
        {

            if (hasDrawn) return;
            hasDrawn = true;

            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;


            // 假设你的gif文件在 Resources/reward.gif
            var gifUri = new Uri("pack://application:,,,/MainWindowImage/draw.gif", UriKind.Absolute);

            // 随机抽取两张卡
            Random rand = new Random();
            var selected = CardLibrary.OrderBy(x => rand.Next()).Take(2).ToList();
            var card1 = selected[0];
            var card2 = selected[1];

            // 重新设置GIF动画源
            var imageSource = new System.Windows.Media.Imaging.BitmapImage(gifUri);
            ImageBehavior.SetAnimatedSource(GifImage, imageSource);

            // 只播放一次
            ImageBehavior.SetRepeatBehavior(GifImage, new System.Windows.Media.Animation.RepeatBehavior(1));

            // 等待动画播放完毕（假设gif时长1.1秒）
            await Task.Delay(1100);

            // ✅ 展示卡牌
            await ShowCardsWithDelay(card1, card2);

            // 这里写你的后续逻辑，比如展示奖励/卡牌

            hasDrawn = false; // 如果允许多次点击，重置
        }

        private async Task ShowCardsWithDelay(Card card1, Card card2)
        {
            Card1Image.Source = null;
            Card2Image.Source = null;

            await Task.Delay(920);
            // Card1Image.Width = 300;
            //Card1Image.Height = 260;
            //Card2Image.Width = 300;
            //Card2Image.Height = 260;

            Card1Image.Source = new BitmapImage(new Uri($"pack://application:,,,/{card1.ImagePath}"));
            Card1Image.Visibility = Visibility.Visible;

            Card2Image.Source = new BitmapImage(new Uri($"pack://application:,,,/{card2.ImagePath}"));
            Card2Image.Visibility = Visibility.Visible;


            FadeInImage(Card1Image);
            FadeInImage(Card2Image);
            //var popup = new DrawPopupWindow(card1.ImagePath, card2.ImagePath);
            //popup.ShowDialog();
        }

        private void FadeInImage(Image image)
        {
            image.Visibility = Visibility.Visible;

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            image.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

    }
}