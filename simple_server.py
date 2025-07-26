import asyncio
import websockets
import json
import logging

# 配置日志
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(message)s')
logger = logging.getLogger(__name__)

async def handle_zenflow_client(websocket, path):
    """处理ZenFlow客户端连接和消息"""
    client_address = websocket.remote_address
    logger.info(f"ZenFlow客户端已连接: {client_address}")
    
    try:
        async for message in websocket:
            # 处理心跳
            if message == "ping":
                await websocket.send("pong")
                logger.info(f"ZenFlow收到ping，回复pong")
                continue
            
            # 处理数据
            try:
                zenflow_data = json.loads(message)
                print("\n" + "="*60)
                print("ZenFlow收到时间追踪数据:")
                print(f"URL: {zenflow_data.get('url', 'N/A')}")
                print(f"标题: {zenflow_data.get('title', 'N/A')}")
                print(f"域名: {zenflow_data.get('domain', 'N/A')}")
                print(f"开始时间: {zenflow_data.get('startTime', 'N/A')}")
                print(f"结束时间: {zenflow_data.get('endTime', 'N/A')}")
                print(f"持续时间: {zenflow_data.get('duration', 0)}秒")
                
                # 处理图标
                icon_url = zenflow_data.get('icon', '')
                if icon_url:
                    if icon_url.startswith('data:image'):
                        # Base64编码的图标
                        icon_type = "Base64图标"
                        icon_size = len(icon_url)
                        print(f"图标: {icon_type} ({icon_size} 字符)")
                    elif icon_url.startswith('http'):
                        # HTTP URL图标
                        print(f"图标: 外部URL - {icon_url}")
                    elif icon_url.startswith('chrome-extension://'):
                        # Chrome扩展图标
                        print(f"图标: Chrome扩展图标 - {icon_url}")
                    else:
                        print(f"图标: 其他类型 - {icon_url}")
                else:
                    print("图标: 无")
                
                print("="*60)
                
            except json.JSONDecodeError:
                logger.warning(f"ZenFlow收到非JSON消息: {message}")
                
    except websockets.exceptions.ConnectionClosed:
        logger.info(f"ZenFlow客户端断开连接: {client_address}")
    except Exception as e:
        logger.error(f"ZenFlow处理客户端时出错: {e}")

async def main_zenflow():
    """启动ZenFlow服务器"""
    host = "localhost"
    port = 9000
    
    logger.info(f"启动ZenFlow WebSocket服务器: ws://{host}:{port}")
    
    async with websockets.serve(handle_zenflow_client, host, port):
        logger.info("ZenFlow服务器已启动，等待连接...")
        await asyncio.Future()  # 保持服务器运行

if __name__ == "__main__":
    try:
        asyncio.run(main_zenflow())
    except KeyboardInterrupt:
        logger.info("ZenFlow服务器已停止")
