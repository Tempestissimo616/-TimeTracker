import json
import requests
from PIL import Image, ImageDraw
from io import BytesIO

# ✅ 添加圆角函数
def add_rounded_corners(im: Image.Image, radius: int) -> Image.Image:
    circle = Image.new('L', (radius * 2, radius * 2), 0)
    draw = ImageDraw.Draw(circle)
    draw.ellipse((0, 0, radius * 2, radius * 2), fill=255)

    alpha = Image.new('L', im.size, 255)
    w, h = im.size

    # 四角贴遮罩
    alpha.paste(circle.crop((0, 0, radius, radius)), (0, 0))
    alpha.paste(circle.crop((radius, 0, radius * 2, radius)), (w - radius, 0))
    alpha.paste(circle.crop((0, radius, radius, radius * 2)), (0, h - radius))
    alpha.paste(circle.crop((radius, radius, radius * 2, radius * 2)), (w - radius, h - radius))

    im.putalpha(alpha)
    return im

# ✅ ModelScope Toke
API_TOKEN = "ms-e6074399-4a04-4b3e-942a-0526bb918881"

# ✅ API 地址和请求头
url = "https://api-inference.modelscope.cn/v1/images/generations"
headers = {
    "Authorization": f"Bearer {API_TOKEN}",
    "Content-Type": "application/json"
}

# ✅ 输出尺寸
output_size = (180, 240)


items = ["this is a placeholder"]


# ✅ Prompt 模板
style_prompt_template = (
    "A pixel art of a {item}, Whitebackground, 32x32 resolution, "
    "top-down view, 8-bit retro style, flat colors, no shadow, centered"
)

# ✅ 加载卡牌边框
card_frame = Image.open("card_frame.png").convert("RGBA")

# ✅ 批量生成图像并合成
for idx, item in enumerate(items):
    prompt = style_prompt_template.format(item=item)
    payload = {
        "model": "black-forest-labs/FLUX.1-Kontext-dev",
        "prompt": prompt,
        "num_inference_steps": 30
    }

    print(f"🎨 正在生成图像：{prompt} ...")

    response = requests.post(url, headers=headers, data=json.dumps(payload))

    if response.status_code == 200:
        image_url = response.json()["images"][0]["url"]
        image_data = requests.get(image_url).content
        image = Image.open(BytesIO(image_data)).convert("RGBA")

        # ✅ 替换透明背景为白色
        bg = Image.new("RGBA", image.size, (255, 255, 255, 255))
        image = Image.alpha_composite(bg, image)
        image = image.resize((90, 180), Image.NEAREST)
        image = add_rounded_corners(image, radius=18)

        # ✅ 创建卡牌边框副本
        card_frame_resized = card_frame.resize(output_size).copy()

        # ✅ 计算粘贴位置
        offset_x = 3  
        offset_y = -2  
        pos_x = (output_size[0] - image.width) // 2 + offset_x
        pos_y = (output_size[1] - image.height) // 2 + offset_y
        position = (pos_x, pos_y)
        print(position)
        # ✅ 粘贴带圆角图像
        card_frame_resized.paste(image, position, image)

        # ✅ 保存
        filename = f"card_{item.replace(' ', '_')}.png"
        card_frame_resized.save(filename)
        print(f"🃏 已保存卡牌图像 {filename}")

    else:
        print(f"❌ 请求失败: {response.status_code}")
        print(response.text)