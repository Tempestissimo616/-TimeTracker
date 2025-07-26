import asyncio
import websockets
import json
import logging

# 配置日志
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(message)s')
logger = logging.getLogger(__name__)

async def handle_client(websocket, path):
    """处理客户端连接和消息"""
    client_address = websocket.remote_address
    logger.info(f"客户端已连接: {client_address}")
    
    try:
        async for message in websocket:
            # 处理心跳
            if message == "ping":
                await websocket.send("pong")
                logger.info(f"收到ping，回复pong")
                continue
            
            # 处理数据
            try:
                data = json.loads(message)
                print("\n" + "="*50)
                print("收到时间追踪数据:")
                print(f"URL: {data.get('url', 'N/A')}")
                print(f"标题: {data.get('title', 'N/A')}")
                print(f"域名: {data.get('domain', 'N/A')}")
                print(f"开始时间: {data.get('startTime', 'N/A')}")
                print(f"结束时间: {data.get('endTime', 'N/A')}")
                print(f"持续时间: {data.get('duration', 0)}ms")
                print("="*50)
                
            except json.JSONDecodeError:
                logger.warning(f"收到非JSON消息: {message}")
                
    except websockets.exceptions.ConnectionClosed:
        logger.info(f"客户端断开连接: {client_address}")
    except Exception as e:
        logger.error(f"处理客户端时出错: {e}")

async def main():
    """启动服务器"""
    host = "localhost"
    port = 9000
    
    logger.info(f"启动WebSocket服务器: ws://{host}:{port}")
    
    async with websockets.serve(handle_client, host, port):
        logger.info("服务器已启动，等待连接...")
        await asyncio.Future()  # 保持服务器运行

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        logger.info("服务器已停止")