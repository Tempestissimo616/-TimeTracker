// 配置常量
const TEN_SECONDS_MS = 10 * 1000;
const FOCUSED_CHECK_MS = 1000;
const RENOTIFY_MS = 10 * 1000;
const RECONNECTFAIL_SLEEP = 5;
const WSURL = 'ws://localhost:9000';

let webSocket = null;
let isConnected = false;
let isChromeFocused = true;
let autoReConnectIntervalId = null;
let isSleep = false;
let reconnectFail = 0;
let notifyFailList = [];

// 焦点网页数据
let activePage = {
    url: '',
    title: '',
    icon: '',
    domain: '',
    startTime: '',
    endTime: '',
    duration: 0
};

init();

function init() {
    connect();
    startWatchFocus();
    startRenotify();
}

function connect() {
    webSocket = new WebSocket(WSURL);

    webSocket.onopen = (event) => {
        isConnected = true;
        isSleep = false;
        reconnectFail = 0;
        clearInterval(autoReConnectIntervalId);
        keepAlive();
        console.log("WebSocket connected!");
    };

    webSocket.onmessage = (event) => {
        console.log(event.data);
        if (event.data === 'sleep') {
            isSleep = true;
            calDuration();
            console.log("Sleep mode");
        } else if (event.data === 'wake') {
            isSleep = false;
            console.log("Wake up");
        }
    };

    webSocket.onclose = (event) => {
        isConnected = false;
        console.warn('WebSocket disconnected...');
        webSocket = null;
        startAutoReConnect();
    };
}

function startAutoReConnect() {
    clearInterval(autoReConnectIntervalId);
    autoReConnectIntervalId = setInterval(() => {
        if (!isConnected) {
            console.log("Attempting to reconnect...");
            connect();
            reconnectFail++;
            if (reconnectFail >= RECONNECTFAIL_SLEEP && !isSleep) {
                isSleep = true;
            }
        }
    }, TEN_SECONDS_MS);
}

function keepAlive() {
    const keepAliveIntervalId = setInterval(() => {
        if (isConnected && webSocket) {
            console.log('ping');
            webSocket.send('ping');
        } else {
            clearInterval(keepAliveIntervalId);
        }
    }, TEN_SECONDS_MS);
}

function notifyServer(data) {
    console.log("notify", data);
    if (isConnected && webSocket) {
        webSocket.send(JSON.stringify(data));
    } else {
        notifyFailList.push(data);
        console.log("failList", notifyFailList);
    }
}

function renotify() {
    if (isConnected && webSocket && notifyFailList.length > 0) {
        const item = notifyFailList[0];
        notifyFailList.splice(0, 1);
        notifyServer(item);
    }
}

function startRenotify() {
    setInterval(() => {
        renotify();
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
    if (message.type === 'TIME_UPDATE') {
        if (activePage && activePage.url) {
            // 将毫秒转换为秒
            // 将毫秒转换为秒Math.round( / 1000)
            activePage.duration += Math.round(message.timeSpent / 1000);
        }
    } else if (message.type === 'TITLE_UPDATE') {
        if (activePage && activePage.url) {
            activePage.title = message.title;
        }
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
    if (isSleep) return;

    if (activePage && activePage.url) {
        if (activePage.url !== tab.url) {
            calDuration();
            setActive(tab);
        } else {
            // 同一个页面，更新页面开始时间（用于焦点切换）
            activePage.pageStartTime = Date.now();
        }
    } else {
        setActive(tab);
    }
}

function setActive(tab) {
    const { url, title, favIconUrl: icon } = tab;
    if (url && !url.startsWith('chrome://')) {
        activePage = {
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
        activePage = null;
    }
}

function calDuration() {
    if (activePage && activePage.url) {
        const endTime = new Date().toISOString();
        
        // 计算当前会话的时长（秒）
        const currentSessionDuration = activePage.pageStartTime ? 
            Math.round((Date.now() - activePage.pageStartTime) / 1000) : 0;
        
        // 总时长 = 之前累积的时长 + 当前会话时长
        const totalDuration = activePage.duration + currentSessionDuration;
        
        const data = {
            url: activePage.url,
            title: activePage.title,
            icon: activePage.icon,
            domain: activePage.domain,
            startTime: activePage.startTime,
            endTime: endTime,
            duration: totalDuration // 现在是秒为单位
        };
        
        console.log(`页面 ${activePage.domain} 总时长: ${totalDuration}秒`);
        activePage = null;
        notifyServer(data);
    }
}

function startWatchFocus() {
    console.log("开始监听焦点");
    setInterval(async () => {
        const focused = await isFocused();
        if (focused) {
            if (!isChromeFocused) {
                isChromeFocused = true;
                const tab = await getCurrentTab();
                onActivePage(tab);
                console.warn("重置统计");
            }
        } else {
            if (isChromeFocused) {
                isChromeFocused = false;
                calDuration();
                console.warn("更新时间");
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






