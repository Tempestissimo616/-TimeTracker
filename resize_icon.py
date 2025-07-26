from PIL import Image
import os
import sys

def resize_icon(input_path, output_dir="icons"):
    """
    将输入的PNG图标转换成Chrome扩展需要的不同尺寸
    
    Args:
        input_path: 输入图标文件路径
        output_dir: 输出目录
    """
    
    # Chrome扩展需要的尺寸
    sizes = [16, 48, 128]
    
    try:
        # 打开原始图像
        with Image.open(input_path) as img:
            print(f"原始图像尺寸: {img.size}")
            print(f"原始图像模式: {img.mode}")
            
            # 确保是RGBA模式（支持透明度）
            if img.mode != 'RGBA':
                img = img.convert('RGBA')
            
            # 创建输出目录
            if not os.path.exists(output_dir):
                os.makedirs(output_dir)
                print(f"创建目录: {output_dir}")
            
            # 生成不同尺寸的图标
            for size in sizes:
                # 使用高质量重采样
                resized_img = img.resize((size, size), Image.Resampling.LANCZOS)
                
                # 输出文件名
                output_path = os.path.join(output_dir, f"icon-{size}.png")
                
                # 保存图像
                resized_img.save(output_path, "PNG", optimize=True)
                print(f"✅ 生成 {size}x{size}: {output_path}")
            
            print(f"\n🎉 所有图标已生成到 {output_dir} 目录")
            
    except FileNotFoundError:
        print(f"❌ 错误: 找不到文件 {input_path}")
    except Exception as e:
        print(f"❌ 错误: {e}")

def main():
    if len(sys.argv) != 2:
        print("使用方法: python resize_icon.py <图标文件路径>")
        print("例如: python resize_icon.py my_icon.png")
        return
    
    input_file = sys.argv[1]
    
    if not os.path.exists(input_file):
        print(f"❌ 文件不存在: {input_file}")
        return
    
    if not input_file.lower().endswith('.png'):
        print("❌ 请提供PNG格式的图标文件")
        return
    
    resize_icon(input_file)

if __name__ == "__main__":
    main()