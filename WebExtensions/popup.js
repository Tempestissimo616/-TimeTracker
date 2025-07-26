document.addEventListener('DOMContentLoaded', async () => {
    const statusDiv = document.getElementById('status');
    const currentPageDiv = document.getElementById('currentPage');
    const connectBtn = document.getElementById('connectBtn');

    // 获取ZenFlow后台状态
    function updateZenFlowStatus() {
        chrome.runtime.sendMessage({type: 'GET_ZENFLOW_STATUS'}, (response) => {
            if (response) {
                displayZenFlowStatus(response);
            }
        });
    }

    // 连接按钮 - 用于重连
    connectBtn.addEventListener('click', () => {
        console.log('点击ZenFlow重新连接按钮');
        chrome.runtime.sendMessage({type: 'ZENFLOW_CONNECT'}, (response) => {
            console.log('ZenFlow连接响应:', response);
            setTimeout(updateZenFlowStatus, 1000); // 1秒后更新状态
        });
    });

    function displayZenFlowStatus(status) {
        if (status.isConnected) {
            statusDiv.innerHTML = '<span class="connected">✅ 已连接到ZenFlow服务器</span>';
            connectBtn.textContent = '重新连接';
            connectBtn.className = 'reconnect';
        } else {
            statusDiv.innerHTML = '<span class="disconnected">❌ 未连接到ZenFlow服务器</span>';
            connectBtn.textContent = '连接服务器';
            connectBtn.className = 'primary';
        }

        if (status.activePage && status.activePage.url) {
            const duration = status.activePage.duration || 0;
            currentPageDiv.innerHTML = `
                <p><strong>当前页面:</strong></p>
                <p>📄 ${status.activePage.title || '无标题'}</p>
                <p>🌐 ${status.activePage.domain}</p>
                <p>⏱️ ${duration}秒</p>
            `;
        } else {
            currentPageDiv.innerHTML = '<p>暂无活跃页面</p>';
        }
    }

    // 初始状态更新
    updateZenFlowStatus();

    // 定期更新状态
    setInterval(updateZenFlowStatus, 2000);
});



