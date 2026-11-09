(function () {
  'use strict';

  var LOCATION = {
    name: '天津农学院 · 蓟州校区',
    address: '天津市蓟州区五百户镇青池一村东侧',
    longitude: 117.516216,
    latitude: 40.020679,
    coordinateSystem: 'GCJ-02'
  };
  var DEVICE_API = 'https://yunchunremote.cn:5656/deviceValue/selectValue/88';
  var DRUM_LEVEL_API = 'https://yunchunremote.cn/yunChunApp/deviceValue/getDeviceValueByDeviceId?deviceId=88';
  var DRUM_LEVEL_ID = 32;
  var DRUM_LEVEL_MAX_AGE_MS = 120000;
  var IMAGERY_URL = 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}';
  var IMAGERY_CREDIT = 'Tiles © Esri — Esri, Vantor, Earthstar Geographics, and the GIS User Community';
  var MODEL_ENTRY_ZOOM = 19;
  var MODEL_ENTRY_RADIUS_METERS = 350;
  var MAP_OVERVIEW_ZOOM = 14;
  var METRICS = [
    { key: 'steamPressure', name: '蒸汽压力', unit: 'Pa', notes: '蒸汽压力' },
    { key: 'drumLevel', name: '锅筒水位', unit: 'mm' },
    { key: 'steamFlow', name: '蒸汽流量', unit: 't', notes: '蒸汽流量' },
    { key: 'steamTotal', name: '蒸汽累计流量', unit: 't', notes: '蒸汽累计流量' },
    { key: 'ecoInTemp', name: '省煤器入口烟温', unit: '℃', notes: '省煤器入口烟温' },
    { key: 'dustInTemp', name: '烟尘入口温度', unit: '℃', notes: '烟尘入口温度' },
    { key: 'dustInPress', name: '烟尘入口压力', unit: 'Pa', notes: '烟尘入口压力' },
    { key: 'o2', name: '烟气氧含量', unit: '%', notes: '烟气氧含量' },
    { key: 'pump1', name: '1 号立泵', unit: 'Hz', notes: '1立泵频率反馈', bar: '#20dff2' },
    { key: 'pump2', name: '2 号立泵', unit: 'Hz', notes: '2立泵频率反馈', bar: '#4cdeb3' },
    { key: 'pump3', name: '3 号立泵', unit: 'Hz', notes: '3立泵频率反馈' },
    { key: 'boost1', name: '1 号增压泵', unit: 'Hz', notes: '1增压泵频率反馈' },
    { key: 'boost2', name: '2 号增压泵', unit: 'Hz', notes: '2增压泵频率反馈' },
    { key: 'grate', name: '炉排', unit: 'Hz', notes: '炉排频率反馈', bar: '#ffca6a' },
    { key: 'feeder', name: '控料器', unit: 'Hz', notes: '控料器频率反馈' },
    { key: 'air1', name: '一次鼓风', unit: 'Hz', notes: '一次鼓风频率反馈', bar: '#4eabff' },
    { key: 'air2', name: '二次鼓风', unit: 'Hz', notes: '二次鼓风频率反馈', bar: '#a589f5' },
    { key: 'idFan', name: '引风机', unit: 'Hz', notes: '引风机频率反馈', bar: '#ff7c8b' }
  ];
  var values = {};
  var history = [];
  var trendKey = 'steamPressure';
  var mode = 'map';
  var started = false;
  var requestPending = false;
  var drumRequestPending = false;
  var drumSourceTime = null;
  var drumRequestFailed = false;
  var lastReceived = 0;
  var sourceTimestamp = null;
  var map = null;
  var miniMap = null;
  var mapTiles = null;
  var mapLoadedTiles = 0;
  var mapErrors = 0;
  var transitionTimer = null;
  var modelEntryPending = false;
  var viewpointsReady = false;
  var reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  var byId = function (id) { return document.getElementById(id); };

  // The supplied point uses GCJ-02; the imagery service uses WGS-84/Web Mercator.
  function gcjOffset(longitude, latitude) {
    var x = longitude - 105, y = latitude - 35, pi = Math.PI;
    var lat = -100 + 2 * x + 3 * y + 0.2 * y * y + 0.1 * x * y + 0.2 * Math.sqrt(Math.abs(x));
    lat += (20 * Math.sin(6 * x * pi) + 20 * Math.sin(2 * x * pi)) * 2 / 3;
    lat += (20 * Math.sin(y * pi) + 40 * Math.sin(y * pi / 3)) * 2 / 3;
    lat += (160 * Math.sin(y * pi / 12) + 320 * Math.sin(y * pi / 30)) * 2 / 3;
    var lng = 300 + x + 2 * y + 0.1 * x * x + 0.1 * x * y + 0.1 * Math.sqrt(Math.abs(x));
    lng += (20 * Math.sin(6 * x * pi) + 20 * Math.sin(2 * x * pi)) * 2 / 3;
    lng += (20 * Math.sin(x * pi) + 40 * Math.sin(x * pi / 3)) * 2 / 3;
    lng += (150 * Math.sin(x * pi / 12) + 300 * Math.sin(x * pi / 30)) * 2 / 3;
    var radians = latitude / 180 * pi;
    var magic = 1 - 0.00669342162296594323 * Math.pow(Math.sin(radians), 2);
    var root = Math.sqrt(magic);
    return [lng * 180 / (6378245 / root * Math.cos(radians) * pi), lat * 180 / (6378245 * (1 - 0.00669342162296594323) / (magic * root) * pi)];
  }
  function gcjToWgs(longitude, latitude) {
    var lng = longitude, lat = latitude;
    for (var i = 0; i < 5; i++) {
      var offset = gcjOffset(lng, lat);
      lng -= lng + offset[0] - longitude;
      lat -= lat + offset[1] - latitude;
    }
    return [lat, lng];
  }
  var campusCenter = gcjToWgs(LOCATION.longitude, LOCATION.latitude);

  function numberOrNull(value) {
    if (value === null || value === undefined || String(value).trim() === '') return null;
    if (typeof value !== 'number' && typeof value !== 'string') return null;
    var number = Number(value);
    return isFinite(number) ? number : null;
  }
  function formatNumber(value) {
    if (value === null) return '--';
    return Math.abs(value) >= 100 ? value.toFixed(1) : value.toFixed(2);
  }
  function isAlarm(value) {
    if (value === null || value === undefined) return false;
    return ['', '0', 'false', 'null', 'normal', '正常'].indexOf(String(value).trim().toLowerCase()) === -1;
  }
  function unitLabel(raw, fallback) {
    var unit = raw === null || raw === undefined ? '' : String(raw).trim();
    var aliases = { '赫兹': 'Hz', '帕': 'Pa', '摄氏度': '℃', '毫米': 'mm', '吨': 't', '百分比': '%' };
    return !unit || unit.toLowerCase() === 'null' ? fallback : (aliases[unit] || unit);
  }
  function clockText(time) {
    return new Date(time).toLocaleTimeString('zh-CN', { hour12: false, timeZone: 'Asia/Shanghai' });
  }
  function fullTime(time) {
    return new Date(time).toLocaleString('zh-CN', { hour12: false, timeZone: 'Asia/Shanghai' });
  }
  function updateClock() {
    var now = new Date();
    byId('dashboard-clock').textContent = clockText(now);
    byId('dashboard-date').textContent = now.toLocaleDateString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', weekday: 'short', timeZone: 'Asia/Shanghai' });
    updateDrumCaption();
    if (lastReceived && Date.now() - lastReceived > 15000) {
      byId('dash-update').textContent = '连接延迟 · 保留最近接收值';
      byId('data-status-dot').className = 'status-dot stale';
    }
  }
  function buildTicker() {
    var track = byId('ticker-track');
    track.textContent = '';
    for (var copy = 0; copy < 2; copy++) {
      var group = document.createElement('div');
      group.className = 'ticker-group';
      if (copy) group.setAttribute('aria-hidden', 'true');
      METRICS.forEach(function (metric) {
        var item = document.createElement('div');
        item.className = 'ticker-item';
        item.dataset.metric = metric.key;
        item.innerHTML = '<span>' + metric.name + (metric.unit === 'Hz' ? '频率' : '') + '</span><strong data-value="' + metric.key + '">--</strong><small data-unit="' + metric.key + '">' + metric.unit + '</small>';
        group.appendChild(item);
      });
      track.appendChild(group);
    }
    byId('ticker-pause').onclick = function () {
      var paused = document.querySelector('.data-ticker').classList.toggle('paused');
      this.setAttribute('aria-pressed', String(paused));
      this.setAttribute('aria-label', paused ? '继续数据滚动' : '暂停数据滚动');
      this.textContent = paused ? '▷' : 'Ⅱ';
    };
  }
  function buildFrequencyBars() {
    var container = byId('frequency-bars');
    container.textContent = '';
    METRICS.filter(function (metric) { return metric.bar; }).forEach(function (metric) {
      var row = document.createElement('div');
      row.className = 'frequency-row';
      row.style.setProperty('--bar-color', metric.bar);
      row.dataset.metric = metric.key;
      row.innerHTML = '<div class="frequency-row-head"><span>' + metric.name + '频率</span><strong><span data-value="' + metric.key + '">--</span> <span data-unit="' + metric.key + '">Hz</span></strong></div><div class="frequency-track"><div class="frequency-fill"></div></div>';
      container.appendChild(row);
    });
  }
  function renderTrend() {
    var metric = METRICS.filter(function (item) { return item.key === trendKey; })[0];
    byId('trend-unit').textContent = values[trendKey] ? values[trendKey].unit : metric.unit;
    var points = history.map(function (sample, index) { return { index: index, value: sample[trendKey], time: sample.time }; }).filter(function (sample) { return sample.value !== null; });
    byId('trend-empty').hidden = points.length > 1;
    byId('trend-chart').setAttribute('aria-label', metric.name + '，本次会话最近五分钟采集趋势');
    if (points.length < 2) {
      byId('trend-line').setAttribute('d', '');
      byId('trend-area').setAttribute('d', '');
      byId('trend-y-labels').textContent = '';
      return;
    }
    var min = Math.min.apply(null, points.map(function (sample) { return sample.value; }));
    var max = Math.max.apply(null, points.map(function (sample) { return sample.value; }));
    var margin = Math.max((max - min) * 0.18, Math.abs(max) * 0.025, 0.01);
    min -= margin; max += margin;
    var denominator = Math.max(1, history.length - 1);
    var d = points.map(function (sample, index) {
      var x = 34 + sample.index / denominator * 278;
      var y = 109 - (sample.value - min) / (max - min) * 93;
      return (index && sample.index === points[index - 1].index + 1 ? 'L' : 'M') + x.toFixed(2) + ',' + y.toFixed(2);
    }).join(' ');
    byId('trend-line').setAttribute('d', d);
    // Do not draw an area across gaps in the source readings.
    var continuous = points.length === history.length;
    byId('trend-area').setAttribute('d', continuous ? d + ' L312,109 L34,109 Z' : '');
    var labels = byId('trend-y-labels');
    labels.textContent = '';
    for (var i = 0; i < 4; i++) {
      var text = document.createElementNS('http://www.w3.org/2000/svg', 'text');
      text.setAttribute('x', '29'); text.setAttribute('y', String(19 + i * 31)); text.setAttribute('class', 'chart-y-label');
      text.textContent = formatNumber(max - i / 3 * (max - min));
      labels.appendChild(text);
    }
    byId('trend-start').textContent = clockText(history[0].time);
    byId('trend-end').textContent = clockText(history[history.length - 1].time);
  }
  function renderMetric(metric, item) {
    var value = item ? numberOrNull(item.value) : null;
    var alarm = !!item && isAlarm(item.alarmTag);
    var unit = unitLabel(item && item.unitName, metric.unit);
    values[metric.key] = { value: value, unit: unit, alarm: alarm };
    document.querySelectorAll('[data-value="' + metric.key + '"]').forEach(function (element) {
      element.textContent = formatNumber(value);
      element.classList.toggle('alarm-value', alarm);
    });
    document.querySelectorAll('[data-unit="' + metric.key + '"]').forEach(function (element) { element.textContent = unit; });
    document.querySelectorAll('[data-metric="' + metric.key + '"]').forEach(function (element) { element.classList.toggle('alarm', alarm); });
  }
  function refreshCounts() {
    var hit = 0, alarms = 0;
    METRICS.forEach(function (metric) {
      var reading = values[metric.key];
      if (reading && reading.value !== null) hit++;
      if (reading && reading.alarm) alarms++;
    });
    byId('point-count').textContent = hit;
    byId('alarm-count').textContent = alarms;
    byId('alarm-count').classList.toggle('alarm-value', alarms > 0);
    return hit;
  }
  function applyReadings(items) {
    if (!Array.isArray(items)) throw new Error('设备接口未返回数据数组');
    var byNotes = {};
    items.forEach(function (item) { if (item && item.notes) byNotes[String(item.notes).trim()] = item; });
    var timestamps = [];
    METRICS.forEach(function (metric) {
      if (metric.key === 'drumLevel') return;
      var item = byNotes[metric.notes];
      renderMetric(metric, item);
      var sourceTime = item ? Date.parse(item.updateTime) : NaN;
      if (isFinite(sourceTime)) timestamps.push(sourceTime);
    });
    var hit = refreshCounts();
    var barMetrics = METRICS.filter(function (metric) { return metric.bar; });
    var scale = Math.max(50, Math.ceil(Math.max.apply(null, barMetrics.map(function (metric) { return values[metric.key].value || 0; })) / 10) * 10);
    barMetrics.forEach(function (metric) {
      var fill = document.querySelector('[data-metric="' + metric.key + '"] .frequency-fill');
      fill.style.width = Math.max(0, (values[metric.key].value || 0) / scale * 100) + '%';
    });
    byId('frequency-max').textContent = scale + ' Hz';
    lastReceived = Date.now();
    sourceTimestamp = timestamps.length ? Math.max.apply(null, timestamps) : null;
    var oldSource = sourceTimestamp !== null && lastReceived - sourceTimestamp > 60000;
    byId('dash-update').textContent = hit ? '设备数据已连接 · ' + hit + '/' + METRICS.length + ' 项' : '接口已连接 · 暂无匹配测点';
    byId('data-status-dot').className = 'status-dot ' + (oldSource || !hit ? 'stale' : 'connected');
    byId('source-time').textContent = sourceTimestamp !== null ? '采样 ' + fullTime(sourceTimestamp) + (oldSource ? ' · 较早' : '') : '接收 ' + clockText(lastReceived) + ' · 接口未提供采样时间';
    byId('source-time').classList.toggle('stale', oldSource);
    byId('source-time').title = '页面接收时间：' + fullTime(lastReceived);
    history.push({ time: lastReceived, steamPressure: values.steamPressure.value, steamFlow: values.steamFlow.value });
    history = history.filter(function (sample) { return lastReceived - sample.time <= 300000; });
    renderTrend();
  }
  function fetchDeviceData(url) {
    var controller = new AbortController();
    var timeout = setTimeout(function () { controller.abort(); }, 9000);
    return fetch(url, { cache: 'no-store', signal: controller.signal })
      .then(function (response) { if (!response.ok) throw new Error('HTTP ' + response.status); return response.json(); })
      .finally(function () { clearTimeout(timeout); });
  }
  function loadReadings() {
    if (requestPending) return;
    requestPending = true;
    fetchDeviceData(DEVICE_API)
      .then(applyReadings)
      .catch(function (error) {
        console.warn('[设备数据]', error.message);
        byId('dash-update').textContent = lastReceived ? '连接异常 · 保留最近接收值' : '设备数据连接异常 · 正在重试';
        byId('data-status-dot').className = 'status-dot stale';
      })
      .finally(function () { requestPending = false; });
  }
  function updateDrumCaption() {
    var caption = byId('drum-level-caption');
    if (!caption) return;
    var status = drumRequestFailed ? ' · 数据不可用' : drumSourceTime === null ? ' · 等待数据' :
      Date.now() - drumSourceTime > DRUM_LEVEL_MAX_AGE_MS ? ' · 较早' : '';
    caption.textContent = '锅筒水位' + status;
    caption.title = drumSourceTime === null ? '磁翻板水位，测点 ID 32' : '磁翻板采样：' + fullTime(drumSourceTime);
  }
  function loadDrumLevel() {
    if (drumRequestPending) return;
    drumRequestPending = true;
    fetchDeviceData(DRUM_LEVEL_API).then(function (payload) {
      if (!payload || payload.code !== 200 || !Array.isArray(payload.data)) throw new Error('接口未返回设备数据');
      var rows = payload.data.filter(function (item) { return item && item.id === DRUM_LEVEL_ID; });
      if (rows.length !== 1) throw new Error('磁翻板测点 ID 缺失或重复');
      var item = rows[0];
      var value = numberOrNull(item.value);
      var time = typeof item.updateTime === 'string' && /(?:Z|[+-]\d{2}:\d{2})$/.test(item.updateTime) ? Date.parse(item.updateTime) : NaN;
      if (value === null || unitLabel(item.unitName, 'mm').toLowerCase() !== 'mm' || !isFinite(time) || time > Date.now() + 60000 ||
          (drumSourceTime !== null && time < drumSourceTime)) throw new Error('磁翻板数值、单位或采样时间无效');
      drumSourceTime = time;
      drumRequestFailed = false;
      renderMetric(METRICS.filter(function (metric) { return metric.key === 'drumLevel'; })[0], item);
      refreshCounts();
    }).catch(function (error) {
      drumRequestFailed = true;
      console.warn('[磁翻板水位]', error.message);
    }).finally(function () {
      drumRequestPending = false;
      updateDrumCaption();
    });
  }
  function newImageryLayer() {
    return window.L.tileLayer(IMAGERY_URL, { maxZoom: 20, maxNativeZoom: 19, attribution: IMAGERY_CREDIT, crossOrigin: true, keepBuffer: 2 });
  }
  function initMiniMap() {
    if (miniMap || !window.L) return;
    miniMap = window.L.map('campus-mini-map', { zoomControl: false, attributionControl: false, dragging: false, scrollWheelZoom: false, doubleClickZoom: false, boxZoom: false, touchZoom: false, keyboard: false }).setView(campusCenter, 15);
    newImageryLayer().addTo(miniMap);
  }
  function flyTo(point, zoom) {
    if (!map) return;
    if (reducedMotion) map.setView(point, zoom, { animate: false });
    else map.flyTo(point, zoom, { duration: 0.7 });
  }
  function initMap() {
    if (map) return true;
    if (!window.L) { byId('map-unavailable').hidden = false; return false; }
    map = window.L.map('campus-map', { zoomControl: false, doubleClickZoom: false, minZoom: 5, maxZoom: 20, zoomAnimation: !reducedMotion }).setView(campusCenter, MAP_OVERVIEW_ZOOM);
    mapTiles = newImageryLayer();
    mapTiles.on('tileload', function () { mapLoadedTiles++; byId('map-unavailable').hidden = true; });
    mapTiles.on('tileerror', function () { if (++mapErrors >= 3 && !mapLoadedTiles) byId('map-unavailable').hidden = false; });
    mapTiles.addTo(map);
    window.L.control.scale({ imperial: false, position: 'bottomleft' }).addTo(map);
    var icon = window.L.divIcon({ className: 'map-campus-icon', iconSize: [30, 30], iconAnchor: [15, 15], html: '<span class="campus-pin">⌖<span class="campus-label">锅炉数字孪生<small>天津农学院 · 蓟州校区</small></span></span>' });
    window.L.marker(campusCenter, { icon: icon, title: LOCATION.name + '，点击放大查看', keyboard: true, bubblingMouseEvents: false }).addTo(map).on('click', function () {
      var nextZoom = Math.min(MODEL_ENTRY_ZOOM, map.getZoom() + 1);
      flyTo(campusCenter, nextZoom);
      byId('map-detail').hidden = nextZoom < 17;
    });
    map.on('click', function (event) {
      byId('map-detail').hidden = true;
      flyTo(event.latlng, Math.min(20, map.getZoom() + 1));
    });
    map.on('zoomend', function () {
      updateMapStage();
      if (mode === 'map' && map.getZoom() >= MODEL_ENTRY_ZOOM && map.distance(map.getCenter(), campusCenter) <= MODEL_ENTRY_RADIUS_METERS) {
        enterModelFromMap();
      }
    });
    return true;
  }
  function updateMapStage() {
    if (!map || mode !== 'map') return;
    var zoom = map.getZoom();
    byId('scene-caption-title').textContent = zoom <= 14 ? '区域鸟瞰 · 天津农学院蓟州校区' : (zoom <= 16 ? '校区鸟瞰 · 天津农学院蓟州校区' : '锅炉房卫星定位');
    byId('scene-caption-tip').textContent = zoom <= 14 ? '逐级放大：校区位置 → 锅炉房位置 → 三维模型' : (zoom <= 16 ? '点击或滚轮逐级放大锅炉标记，查看建筑细节' : '继续放大锅炉区域，自动进入 Unity 模型全景');
  }
  function enterModelFromMap() {
    if (transitionTimer !== null) return;
    if (!window.unityInstance || !viewpointsReady) {
      modelEntryPending = true;
      byId('model-entry-note').hidden = false;
      byId('model-entry-note').querySelector('strong').textContent = window.unityInstance ? '正在准备完整模型全景' : '三维模型正在加载，请稍候';
      if (!window.unityInstance) window.dispatchEvent(new CustomEvent('boiler:request-model'));
      return;
    }
    modelEntryPending = false;
    byId('model-entry-note').querySelector('strong').textContent = '正在进入锅炉房完整模型';
    byId('map-detail').hidden = true;
    byId('model-entry-note').hidden = false;
    byId('workspace').classList.add('entering-model');
    byId('unity-canvas').style.visibility = '';
    var overview = document.querySelector('.view-btn[data-name="OverviewPoint"]') || document.querySelector('.view-btn');
    if (overview) overview.click();
    transitionTimer = setTimeout(function () {
      transitionTimer = null;
      setMode('model');
      byId('workspace').classList.remove('entering-model');
      byId('model-entry-note').hidden = true;
      byId('unity-canvas').focus({ preventScroll: true });
    }, reducedMotion ? 0 : 650);
  }
  function setMode(nextMode) {
    if (nextMode === 'map') {
      if (mode !== 'map') window.dispatchEvent(new Event('boiler:show-map'));
      modelEntryPending = false;
      byId('model-entry-note').hidden = true;
    }
    if (transitionTimer !== null) {
      clearTimeout(transitionTimer);
      transitionTimer = null;
      byId('workspace').classList.remove('entering-model');
      byId('model-entry-note').hidden = true;
    }
    mode = nextMode === 'map' ? 'map' : 'model';
    var showMap = mode === 'map';
    byId('workspace').classList.toggle('map-mode', showMap);
    byId('campus-map-layer').hidden = !showMap;
    byId('map-controls').hidden = !showMap;
    byId('unity-canvas').style.visibility = showMap ? 'hidden' : '';
    byId('mode-map').classList.toggle('active', showMap);
    byId('mode-model').classList.toggle('active', !showMap);
    byId('mode-map').setAttribute('aria-pressed', String(showMap));
    byId('mode-model').setAttribute('aria-pressed', String(!showMap));
    byId('scene-index').textContent = showMap ? '02' : '01';
    byId('scene-caption-title').textContent = showMap ? '天津农学院 · 蓟州校区鸟瞰' : '锅炉房完整模型';
    byId('scene-caption-tip').textContent = showMap ? '放大锅炉位置自动进入三维模型 · 滚轮缩放 · 拖动平移' : '右键拖动旋转 · 滚轮缩放 · J / K 切换视角';
    byId('scene-state').textContent = showMap ? 'SATELLITE MAP' : (document.body.classList.contains('ui-preview') ? '布局预览' : 'WEBGL 场景');
    if (showMap && initMap()) {
      if (map.getZoom() >= MODEL_ENTRY_ZOOM && map.distance(map.getCenter(), campusCenter) <= MODEL_ENTRY_RADIUS_METERS) {
        map.setView(campusCenter, MAP_OVERVIEW_ZOOM, { animate: false });
      }
      requestAnimationFrame(function () { map.invalidateSize({ pan: true }); });
      updateMapStage();
    }
    if (!showMap) byId('map-detail').hidden = true;
  }
  function toggleFullscreen() {
    var result;
    if (document.fullscreenElement) result = document.exitFullscreen();
    else if (document.documentElement.requestFullscreen) result = document.documentElement.requestFullscreen();
    else if (window.unityInstance) window.unityInstance.SetFullscreen(1);
    if (result && result.catch) result.catch(function (error) { console.warn('[全屏]', error.message); });
  }
  function start() {
    if (started) return;
    started = true;
    buildTicker(); buildFrequencyBars(); updateClock(); initMiniMap(); loadReadings(); loadDrumLevel();
    setInterval(updateClock, 1000);
    setInterval(loadReadings, 5000);
    setInterval(loadDrumLevel, 10000);
    byId('mode-model').onclick = enterModelFromMap;
    byId('mode-map').onclick = byId('open-campus-map').onclick = function () { setMode('map'); };
    byId('map-zoom-in').onclick = function () { if (map) map.zoomIn(); };
    byId('map-zoom-out').onclick = function () { if (map) map.zoomOut(); };
    byId('map-home').onclick = function () { modelEntryPending = false; byId('model-entry-note').hidden = true; flyTo(campusCenter, MAP_OVERVIEW_ZOOM); byId('map-detail').hidden = true; };
    byId('map-detail-close').onclick = function () { byId('map-detail').hidden = true; };
    byId('map-detail-zoom').onclick = function () { flyTo(campusCenter, 20); byId('map-detail').hidden = true; };
    byId('map-detail-model').onclick = enterModelFromMap;
    byId('map-retry').onclick = function () { mapErrors = 0; mapLoadedTiles = 0; byId('map-unavailable').hidden = true; if (initMap() && mapTiles) mapTiles.redraw(); };
    byId('map-external').href = 'https://uri.amap.com/marker?position=' + LOCATION.longitude + ',' + LOCATION.latitude + '&name=' + encodeURIComponent(LOCATION.name) + '&coordinate=gaode&callnative=0';
    document.querySelectorAll('[data-trend]').forEach(function (button) {
      button.onclick = function () {
        trendKey = button.dataset.trend;
        document.querySelectorAll('[data-trend]').forEach(function (item) { item.classList.toggle('active', item === button); });
        renderTrend();
      };
    });
    byId('unity-fullscreen-button').onclick = toggleFullscreen;
    window.addEventListener('resize', function () { if (map) map.invalidateSize({ pan: true }); if (miniMap) miniMap.invalidateSize({ pan: true }); });
    document.addEventListener('keydown', function (event) { if (event.key === 'Escape') byId('map-detail').hidden = true; });
    if (new URLSearchParams(location.search).get('uiPreview') === '1') {
      document.body.classList.add('ui-preview');
    }
    setMode('map');
  }
  function setModelProgress(percent) {
    byId('model-load-status').hidden = false;
    byId('model-load-status').textContent = '三维模型后台加载中 · ' + percent + '%';
  }
  function onModelReady() {
    byId('model-load-status').hidden = true;
    document.body.classList.remove('ui-preview');
    if (modelEntryPending) setTimeout(enterModelFromMap, 0);
  }
  function onViewpointsReady() {
    viewpointsReady = true;
    // Defer SendMessage until Unity has completed the frame that registered its cameras.
    if (modelEntryPending && window.unityInstance) setTimeout(enterModelFromMap, 0);
  }
  window.BoilerDashboard = { start: start, setMode: setMode, setModelProgress: setModelProgress, onModelReady: onModelReady, onViewpointsReady: onViewpointsReady, toggleFullscreen: toggleFullscreen, getMode: function () { return mode; } };
})();
