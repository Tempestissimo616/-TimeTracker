// 查看当前连接状态
console.log('WebSocket状态:', isConnected);
console.log('Chrome焦点状态:', isChromeFocused);
console.log('睡眠状态:', isSleep);
console.log('当前活跃页面:', activePage);

// 手动触发连接
connect();

// 手动断开连接
if (webSocket) {
    webSocket.close();
}

// 查看失败列表
console.log('发送失败列表:', notifyFailList);

// 手动重试发送失败的数据
renotify();

// 手动计算当前页面时长
calDuration();

// 获取当前标签页信息
getCurrentTab().then(tab => console.log('当前标签页:', tab));

// 检查浏览器焦点状态
isFocused().then(focused => console.log('浏览器是否有焦点:', focused));

// 手动设置睡眠状态
isSleep = true;
console.log('已设置为睡眠状态');

// 手动唤醒
isSleep = false;
console.log('已唤醒');

// 模拟服务器消息
if (webSocket && webSocket.readyState === WebSocket.OPEN) {
    // 这个需要从服务器端发送
    console.log('发送ping测试');
    webSocket.send('ping');
}

// 查看所有全局变量
console.log({
    webSocket,
    isConnected,
    isChromeFocused,
    isSleep,
    activePage,
    reconnectFail,
    notifyFailList
});