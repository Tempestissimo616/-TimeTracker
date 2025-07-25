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
            // 更多卡片...
        };
        private void TestCardLibrary()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var card in CardLibrary)
            {
                sb.AppendLine($"ID: {card.Id}, Name: {card.Name}, Path: {card.ImagePath}");
            }

            MessageBox.Show(sb.ToString(), "卡牌库内容测试");
        }
        private void DrawCardButton_Click(object sender, RoutedEventArgs e)
        {
            Random rand = new Random();
            var selected = CardLibrary.OrderBy(x => rand.Next()).Take(2).ToList();

            // TODO：将 selected[0] 和 selected[1] 显示在界面的卡片框中
            Card1Image.Source = new BitmapImage(new Uri("pack://application:,,,/Images/card_cat.png"));

            Card2Image.Source = new BitmapImage(new Uri("pack://application:,,,/Images/card_pikachu.png"));


        }


    }
}
