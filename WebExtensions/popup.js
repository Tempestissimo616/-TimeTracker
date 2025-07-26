document.addEventListener('DOMContentLoaded', async () => {
    const statusDiv = document.getElementById('status');
    const currentPageDiv = document.getElementById('currentPage');
    const connectBtn = document.getElementById('connectBtn');
    const disconnectBtn = document.getElementById('disconnectBtn');

    // 获取后台状态
    chrome.runtime.sendMessage({type: 'GET_STATUS'}, (response) => {
        if (response) {
            updateStatus(response);
        }
    });

    // 连接按钮
    connectBtn.addEventListener('click', () => {
        chrome.runtime.sendMessage({type: 'CONNECT'});
    });

    // 断开按钮
    disconnectBtn.addEventListener('click', () => {
        chrome.runtime.sendMessage({type: 'DISCONNECT'});
    });

    function updateStatus(status) {
        if (status.isConnected) {
            statusDiv.innerHTML = '<span class="connected">✅ 已连接到服务器</span>';
        } else {
            statusDiv.innerHTML = '<span class="disconnected">❌ 未连接到服务器</span>';
        }

        if (status.activePage) {
            currentPageDiv.innerHTML = `
                <p><strong>当前页面:</strong></p>
                <p>📄 ${status.activePage.title}</p>
                <p>🌐 ${status.activePage.domain}</p>
                <p>⏱️ ${Math.round(status.activePage.duration / 1000)}秒</p>
            `;
        }
    }
});