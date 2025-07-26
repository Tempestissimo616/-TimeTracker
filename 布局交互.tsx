// 主界面布局、交互

import React from 'react';
import './App.css';

function App() {
  return (
    <div className="main-bg">
      {/* 顶部栏 */}
      <div className="top-bar">
        <div className="top-icon" style={{ marginLeft: 630 }}>&#xe921;</div>
        <div className="top-icon">&#xe922;</div>
        <div className="top-icon">&#xe8bb;</div>
      </div>
      <div className="content-row">
        {/* 侧边栏 */}
        <div className="sidebar">
          <div className="sidebar-icon-box active">
            <img src="/assets/1983c8318208aa1-12cbae30-5d77-4dc3-99f6-4c9d01a03bf9.svg" alt="icon1" className="sidebar-icon" />
          </div>
          <img src="/assets/1983c8318217873-c940c3d1-931a-4327-be24-75d1fab2b7db.svg" alt="icon2" className="sidebar-icon mt-66" />
          <div className="sidebar-label">收集</div>
          <img src="/assets/1983c831821d4bf-ecedced3-237d-4ce4-b770-0322d2d5b2a4.svg" alt="icon3" className="sidebar-icon mt-51" />
          <div className="sidebar-label">统计</div>
          <div className="sidebar-mode-label">模式</div>
        </div>
        {/* 主内容卡片 */}
        <div className="main-card">
          <div className="main-card-header">
            <span className="main-title">喂养专注力</span>
            <img src="/assets/1983c8318227579-4055c151-1889-447b-879c-f619896773e9.svg" alt="leaf" className="main-title-icon" />
          </div>
          <div className="main-card-row">
            <span className="main-card-label">食材</span>
            <span className="main-card-time">30min</span>
            <span className="main-card-label">阅读</span>
            <span className="main-card-exclaim">！</span>
          </div>
          <div className="main-card-btn-row">
            <button className="main-card-btn">开始</button>
          </div>
          <img src="/assets/1983c831822a2d5-50eaf8f4-3080-4946-a3c6-76502955bff8.svg" alt="bg" className="main-card-bg" />
        </div>
      </div>
    </div>
  );
}

export default App;
