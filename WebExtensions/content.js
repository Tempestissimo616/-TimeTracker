let pageStartTime = Date.now();
let isPageVisible = !document.hidden;
let totalTimeSpent = 0;

// 监听页面可见性变化
document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
        if (isPageVisible) {
            const timeSpent = Date.now() - pageStartTime;
            totalTimeSpent += timeSpent;
            sendTimeToBackground(timeSpent);
            isPageVisible = false;
        }
    } else {
        pageStartTime = Date.now();
        isPageVisible = true;
    }
});

// 页面卸载时发送数据
window.addEventListener('beforeunload', () => {
    if (isPageVisible) {
        const timeSpent = Date.now() - pageStartTime;
        totalTimeSpent += timeSpent;
        sendTimeToBackground(timeSpent);
    }
});

// 监听标题变化
let lastTitle = document.title;
const titleObserver = new MutationObserver(() => {
    if (document.title !== lastTitle) {
        lastTitle = document.title;
        chrome.runtime.sendMessage({
            type: 'TITLE_UPDATE',
            title: document.title
        });
    }
});

if (document.querySelector('title') || document.head) {
    titleObserver.observe(document.querySelector('title') || document.head, {
        childList: true,
        subtree: true
    });
}

function sendTimeToBackground(timeSpent) {
    chrome.runtime.sendMessage({
        type: 'TIME_UPDATE',
        timeSpent: timeSpent,
        totalTime: totalTimeSpent
    });
}

