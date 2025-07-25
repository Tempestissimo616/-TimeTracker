using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawCardGame
{
    public class Card
    {
        public int Id { get; set; }              // 卡片编号
        public string Name { get; set; }         // 卡片名称
        public string ImagePath { get; set; }    // 图片路径，例如 "Images/card1.png"
        public bool IsOwned { get; set; } = false; // 是否已拥有，默认是 false（未拥有）
    }

}
