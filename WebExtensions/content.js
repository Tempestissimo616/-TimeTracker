let zenFlowPageStartTime = Date.now();
let isZenFlowPageVisible = !document.hidden;
let zenFlowTotalTimeSpent = 0;

// 监听页面可见性变化
document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
        if (isZenFlowPageVisible) {
            const timeSpent = Date.now() - zenFlowPageStartTime;
            zenFlowTotalTimeSpent += timeSpent;
            sendZenFlowTimeToBackground(timeSpent);
            isZenFlowPageVisible = false;
        }
    } else {
        zenFlowPageStartTime = Date.now();
        isZenFlowPageVisible = true;
    }
});

// 页面卸载时发送数据
window.addEventListener('beforeunload', () => {
    if (isZenFlowPageVisible) {
        const timeSpent = Date.now() - zenFlowPageStartTime;
        zenFlowTotalTimeSpent += timeSpent;
        sendZenFlowTimeToBackground(timeSpent);
    }
});

// 监听标题变化
let zenFlowLastTitle = document.title;
const zenFlowTitleObserver = new MutationObserver(() => {
    if (document.title !== zenFlowLastTitle) {
        zenFlowLastTitle = document.title;
        chrome.runtime.sendMessage({
            type: 'ZENFLOW_TITLE_UPDATE',
            title: document.title
        });
    }
});

if (document.querySelector('title') || document.head) {
    zenFlowTitleObserver.observe(document.querySelector('title') || document.head, {
        childList: true,
        subtree: true
    });
}

function sendZenFlowTimeToBackground(timeSpent) {
    chrome.runtime.sendMessage({
        type: 'ZENFLOW_TIME_UPDATE',
        timeSpent: timeSpent,
        totalTime: zenFlowTotalTimeSpent
    });
}


