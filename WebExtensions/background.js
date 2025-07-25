let activeTabId = null;
let startTime = null;
let websocket = null;

// 连接WebSocket
function connectWebSocket() {
  try {
    websocket = new WebSocket('ws://localhost:8080'); // 修改为你的WPF后端地址
    
    websocket.onopen = () => {
      console.log('WebSocket connected');
    };
    
    websocket.onerror = (error) => {
      console.error('WebSocket error:', error);
    };
    
    websocket.onclose = () => {
      // 重连机制
      setTimeout(connectWebSocket, 5000);
    };
  } catch (error) {
    console.error('Failed to connect WebSocket:', error);
  }
}

// 发送时间数据
function sendTimeData(url, domain, timeSpent) {
  if (websocket && websocket.readyState === WebSocket.OPEN) {
    const data = {
      url: url,
      domain: domain,
      timeSpent: timeSpent,
      timestamp: new Date().toISOString()
    };
    websocket.send(JSON.stringify(data));
  }
}

// 监听标签页切换
chrome.tabs.onActivated.addListener((activeInfo) => {
  handleTabChange(activeInfo.tabId);
});

// 监听标签页更新
chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
  if (changeInfo.status === 'complete' && tab.active) {
    handleTabChange(tabId);
  }
});

function handleTabChange(tabId) {
  // 记录之前标签页的时间
  if (activeTabId && startTime) {
    chrome.tabs.get(activeTabId, (tab) => {
      if (tab && tab.url) {
        const timeSpent = Date.now() - startTime;
        const domain = new URL(tab.url).hostname;
        sendTimeData(tab.url, domain, timeSpent);
      }
    });
  }
  
  // 开始记录新标签页
  activeTabId = tabId;
  startTime = Date.now();
}

// 初始化
connectWebSocket();