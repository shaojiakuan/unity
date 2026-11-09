/* ============================================================
   WebGLBridge.jslib — C# → JS 桥（Unity 调用网页函数）
   配套 C# 脚本：Assets/Script/WebGLBridge.cs
   JS → C# 方向请使用 unityInstance.SendMessage(
       "WebGLBridgeInstance", "OnWebCommand", json)
   ============================================================ */

mergeInto(LibraryManager.library, {

  /**
   * 将 Unity 侧消息推送给网页。
   * 网页侧通过 window.onUnityMessage(json) 接收。
   */
  NotifyWeb: function (jsonPtr) {
    var json = UTF8ToString(jsonPtr);
    try {
      // 运行时探针：保留最后一条桥接消息，供调试脚本直接读取（绕开 console 缓冲）
      if (typeof window.__unityBridgeState === 'undefined') window.__unityBridgeState = [];
      window.__unityBridgeState.push(json);
      if (window.__unityBridgeState.length > 50) window.__unityBridgeState.shift();

      if (typeof window.onUnityMessage === 'function') {
        window.onUnityMessage(json);
      } else {
        console.log('[Unity->Web]', json);
      }
    } catch (e) {
      console.warn('[NotifyWeb] handler error:', e);
    }
  },

  /**
   * 获取网页地址查询参数（可选辅助，供 C# 读取 ?key=value）。
   */
  GetUrlParam: function (namePtr, buffer, bufferSize) {
    var name = UTF8ToString(namePtr);
    var value = '';
    try {
      value = new URLSearchParams(window.location.search).get(name) || '';
    } catch (e) {}
    // 将值写入 C# 提供的缓冲区，返回实际长度
    var bytes = value.length;
    if (buffer && bytes > 0) {
      var capped = Math.min(bytes, bufferSize - 1);
      for (var i = 0; i < capped; i++) {
        HEAPU8[buffer + i] = value.charCodeAt(i);
      }
      HEAPU8[buffer + capped] = 0;
      return capped;
    }
    return bytes;
  }
});
