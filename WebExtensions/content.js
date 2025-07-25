let pageStartTime = Date.now();
let isPageVisible = !document.hidden;

// 监听页面可见性变化
document.addEventListener('visibilitychange', () => {
  if (document.hidden) {
    // 页面隐藏，发送当前时间数据
    if (isPageVisible) {
      const timeSpent = Date.now() - pageStartTime;
      sendTimeToBackground(timeSpent);
      isPageVisible = false;
    }
  } else {
    // 页面显示，重新开始计时
    pageStartTime = Date.now();
    isPageVisible = true;
  }
});

// 页面卸载时发送数据
window.addEventListener('beforeunload', () => {
  if (isPageVisible) {
    const timeSpent = Date.now() - pageStartTime;
    sendTimeToBackground(timeSpent);
  }
});

function sendTimeToBackground(timeSpent) {
  chrome.runtime.sendMessage({
    type: 'TIME_UPDATE',
    url: window.location.href,
    domain: window.location.hostname,
    timeSpent: timeSpent
  });
}