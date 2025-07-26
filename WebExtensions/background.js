// ZenFlow 配置常量
const TEN_SECONDS_MS = 10 * 1000;
const FOCUSED_CHECK_MS = 1000;
const RENOTIFY_MS = 10 * 1000;
const RECONNECTFAIL_SLEEP = 5;
const ZENFLOW_WSURL = 'ws://localhost:9000';

let zenFlowWebSocket = null;
let isZenFlowConnected = false;
let isChromeFocused = true;
let autoReConnectIntervalId = null;
let isZenFlowSleep = false;
let reconnectFail = 0;
let zenFlowNotifyFailList = [];

// 焦点网页数据
let zenFlowActivePage = {
    url: '',
    title: '',
    icon: '',
    domain: '',
    startTime: '',
    endTime: '',
    duration: 0
};

initZenFlow();

function initZenFlow() {
    connectZenFlow();
    startWatchFocus();
    startRenotify();
}

function connectZenFlow() {
    zenFlowWebSocket = new WebSocket(ZENFLOW_WSURL);

    zenFlowWebSocket.onopen = (event) => {
        isZenFlowConnected = true;
        isZenFlowSleep = false;
        reconnectFail = 0;
        clearInterval(autoReConnectIntervalId);
        keepZenFlowAlive();
        console.log("ZenFlow WebSocket connected!");
    };

    zenFlowWebSocket.onmessage = (event) => {
        console.log(event.data);
        if (event.data === 'sleep') {
            isZenFlowSleep = true;
            calDuration();
            console.log("ZenFlow Sleep mode");
        } else if (event.data === 'wake') {
            isZenFlowSleep = false;
            console.log("ZenFlow Wake up");
        }
    };

    zenFlowWebSocket.onclose = (event) => {
        isZenFlowConnected = false;
        console.warn('ZenFlow WebSocket disconnected...');
        zenFlowWebSocket = null;
        startAutoReConnect();
    };
}

function startAutoReConnect() {
    clearInterval(autoReConnectIntervalId);
    autoReConnectIntervalId = setInterval(() => {
        if (!isZenFlowConnected) {
            console.log("ZenFlow attempting to reconnect...");
            connectZenFlow();
            reconnectFail++;
            if (reconnectFail >= RECONNECTFAIL_SLEEP && !isZenFlowSleep) {
                isZenFlowSleep = true;
            }
        }
    }, TEN_SECONDS_MS);
}

function keepZenFlowAlive() {
    const keepAliveIntervalId = setInterval(() => {
        if (isZenFlowConnected && zenFlowWebSocket) {
            console.log('ZenFlow ping');
            zenFlowWebSocket.send('ping');
        } else {
            clearInterval(keepAliveIntervalId);
        }
    }, TEN_SECONDS_MS);
}

function notifyZenFlowServer(data) {
    console.log("ZenFlow notify", data);
    if (isZenFlowConnected && zenFlowWebSocket) {
        zenFlowWebSocket.send(JSON.stringify(data));
    } else {
        zenFlowNotifyFailList.push(data);
        console.log("ZenFlow failList", zenFlowNotifyFailList);
    }
}

function renotifyZenFlow() {
    if (isZenFlowConnected && zenFlowWebSocket && zenFlowNotifyFailList.length > 0) {
        const item = zenFlowNotifyFailList[0];
        zenFlowNotifyFailList.splice(0, 1);
        notifyZenFlowServer(item);
    }
}

function startRenotify() {
    setInterval(() => {
        renotifyZenFlow();
    }, RENOTIFY_MS);
}

// Chrome事件监听
chrome.tabs.onActivated.addListener(async (activeInfo) => {
    const tab = await getTab(activeInfo.tabId);
    onActivePage(tab);
});

chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
    if (changeInfo.status === 'complete' && tab.active) {
        onActivePage(tab);
    }
});

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.type === 'ZENFLOW_TIME_UPDATE') {
        if (zenFlowActivePage && zenFlowActivePage.url) {
            // 将毫秒转换为秒
            zenFlowActivePage.duration += Math.round(message.timeSpent / 1000);
        }
    } else if (message.type === 'ZENFLOW_TITLE_UPDATE') {
        if (zenFlowActivePage && zenFlowActivePage.url) {
            zenFlowActivePage.title = message.title;
        }
    } else if (message.type === 'GET_ZENFLOW_STATUS') {
        // 返回当前状态给弹出页面
        sendResponse({
            isConnected: isZenFlowConnected,
            isChromeFocused: isChromeFocused,
            isSleep: isZenFlowSleep,
            activePage: zenFlowActivePage,
            reconnectFail: reconnectFail
        });
        return true; // 保持消息通道开放
    } else if (message.type === 'ZENFLOW_CONNECT') {
        console.log('收到ZenFlow连接请求');
        connectZenFlow();
        sendResponse({success: true});
        return true;
    }
});

function getTab(tabId) {
    return new Promise((resolve) => {
        chrome.tabs.get(tabId, (tab) => {
            resolve(tab);
        });
    });
}

function isFocused() {
    return new Promise((resolve) => {
        chrome.windows.getCurrent((w) => {
            resolve(w.focused);
        });
    });
}

function getCurrentTab() {
    return new Promise((resolve, reject) => {
        chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
            if (tabs && tabs.length > 0) {
                resolve(tabs[0]);
            } else {
                reject(tabs);
            }
        });
    });
}

function onActivePage(tab) {
    if (isZenFlowSleep) return;

    if (zenFlowActivePage && zenFlowActivePage.url) {
        if (zenFlowActivePage.url !== tab.url) {
            calDuration();
            setActive(tab);
        } else {
            // 同一个页面，更新页面开始时间（用于焦点切换）
            zenFlowActivePage.pageStartTime = Date.now();
        }
    } else {
        setActive(tab);
    }
}

function setActive(tab) {
    const { url, title, favIconUrl: icon } = tab;
    if (url && !url.startsWith('chrome://')) {
        zenFlowActivePage = {
            url,
            title: title || '',
            icon: icon || '',
            domain: new URL(url).hostname,
            startTime: new Date().toISOString(),
            endTime: null,
            duration: 0,
            pageStartTime: Date.now() // 添加页面开始时间戳
        };
    } else {
        zenFlowActivePage = null;
    }
}

function calDuration() {
    if (zenFlowActivePage && zenFlowActivePage.url) {
        const endTime = new Date().toISOString();
        
        // 计算当前会话的时长（秒）
        const currentSessionDuration = zenFlowActivePage.pageStartTime ? 
            Math.round((Date.now() - zenFlowActivePage.pageStartTime) / 1000) : 0;
        
        // 总时长 = 之前累积的时长 + 当前会话时长
        const totalDuration = zenFlowActivePage.duration + currentSessionDuration;
        
        const data = {
            url: zenFlowActivePage.url,
            title: zenFlowActivePage.title,
            icon: zenFlowActivePage.icon,
            domain: zenFlowActivePage.domain,
            startTime: zenFlowActivePage.startTime,
            endTime: endTime,
            duration: totalDuration // 现在是秒为单位
        };
        
        console.log(`ZenFlow页面 ${zenFlowActivePage.domain} 总时长: ${totalDuration}秒`);
        zenFlowActivePage = null;
        notifyZenFlowServer(data);
    }
}

function startWatchFocus() {
    console.log("ZenFlow开始监听焦点");
    setInterval(async () => {
        const focused = await isFocused();
        if (focused) {
            if (!isChromeFocused) {
                isChromeFocused = true;
                const tab = await getCurrentTab();
                onActivePage(tab);
                console.warn("ZenFlow重置统计");
            }
        } else {
            if (isChromeFocused) {
                isChromeFocused = false;
                calDuration();
                console.warn("ZenFlow更新时间");
            }
        }
    }, FOCUSED_CHECK_MS);
}

// 初始化当前活跃标签页
chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
    if (tabs[0]) {
        onActivePage(tabs[0]);
    }
});

