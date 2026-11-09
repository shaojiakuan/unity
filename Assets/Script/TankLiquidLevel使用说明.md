# 直立矩形水箱液位动画

适用：Unity 2021.3、Built-in 渲染管线、矩形等截面内腔。

液体使用独立的 Unity Cube。改变高度的同时将中心抬高高度的一半，使液体底部固定，顶部随液位升降。Cube 自带顶面，水面轮廓由它提供；流动外观由 `Boiler/Tank Flow Water` 材质着色器提供。

## 1. 快速接入水箱

1. 等待 Unity 完成脚本编译，确认 Console 没有编译错误。
2. 在 **Hierarchy** 中选中已经放进场景的新水箱模型或其父对象。
3. 点击菜单 **Tools → Boiler → Liquid Level → Create For Selected Tank**。
4. 菜单创建一个场景根级对象 `TankLiquid`，其中有 `LiquidBody` 和 `TankLiquidLevel` 组件。根对象保持竖直、Scale 为 1，便于避开导入模型的缩放和旋转。
5. 选中 `TankLiquid`，调整位置，使根对象的原点位于水箱**内腔箱底中心**。
6. 调整 **Inner Size**：X 是内腔宽度，Y 是最大可显示液位高度，Z 是内腔深度。
7. 在编辑模式拖动 **Target Level 01**，检查预览；在 Play 模式拖动这个值，观察平滑升降。
8. 校准完成后保存场景。

菜单从水箱外形包围盒估算初始位置和尺寸。这个初值不识别内壁、壁厚、底座或凹腔，需要手动校准。

**尺寸依据水箱实际内腔，不能直接复制 FBX 或场景对象的 Transform Scale。**

## 2. Inspector 参数

| 参数 | 含义 |
| --- | --- |
| Liquid Body | 菜单生成的 LiquidBody；必须是本控制器的直接子级单位 Cube |
| Inner Size X / Y / Z | 内腔宽、最大液位高度、深；根对象 Scale 为 1 时单位按米理解 |
| Initial Level 01 | 进入 Play 或启用组件时的起始液位 |
| Target Level 01 | 目标液位：0 为空，0.5 为半箱高度，1 为满高度 |
| Smooth Time | 平滑过渡时间尺度，建议从 0.6 开始；0 表示立即变化 |
| Animate Demo | 循环进水/出水演示；接入真实数据前关闭 |
| Demo Minimum 01 | 演示最低液位 |
| Demo Maximum 01 | 演示最高液位 |
| Demo Cycle Seconds | 从低到高再回到低的一整轮时间 |
| Tank Shell Root | 水箱外壳模型根对象，按 Q 时切换此对象下的外壳材质 |
| Enable Shell Hotkey | 是否响应外壳透明快捷键 |
| Shell Transparency Key | 默认 Q；与项目已有的 Q 功能同时响应 |
| Shell Transparent Alpha | 开启透明时的外壁透明度，默认 0.2 |

“空箱 / 半箱 / 满箱”按钮立即切换，适合校准，不展示平滑过渡。按钮会退出循环演示。

例如内腔高 2 米：目标 0.25 对应 0.5 米，目标 0.75 对应 1.5 米。

## 3. 循环演示

选中 `TankLiquid`，勾选 **Animate Demo**，将最低值设为 0.2、最高值设为 0.8、周期设为 8 秒，然后进入 Play。

也可以直接使用 **Tools → Boiler → Liquid Level → Create Demo** 创建一个独立演示液体块。该演示对象没有水箱外壳。

## 4. 看见箱内液体

外壳不透明时，内腔液体会被外壳遮挡。

先临时隐藏**水箱外壳模型**，保持 `TankLiquid` 启用，确认液体位置、底部和满液位高度。

播放时按 **Q** 让外壳透明，按 **E** 恢复原材质绑定。先点击 Game 窗口或 WebGL 画布获得键盘焦点。

`SampleScene` 的软化水箱启用了 **Enable Treatment Demo**。在水处理视角透视水箱后，点击“开始演示”或按 **Y**，依次观察进水、箱内处理、静置和出水。面板标注模拟液位。阶段时长、液位及水纹起点均为示例参数。退出演示后恢复正常液位显示。

项目会在脚本编译后，为已有且尚未绑定外壳的液位对象查找新导入的 `Water Tank.fbx` 并绑定。绑定后保存场景，以便构建时保留引用。

如果 Tank Shell Root 为空，在 Hierarchy 中选中实际水箱外壳，使用 **Tools → Boiler → Liquid Level → Bind Q To Selected Tank**，或直接将外壳根对象拖到该字段。

透明模式保留原来的颜色与贴图，使用运行时材质副本，液体本身保持原样。禁用组件或退出播放时恢复原材质绑定。项目其他 Q 键功能继续同时响应。

### WebGL 构建中的透明变体

水箱的 `07 - End.mat` 使用 Standard Shader 和 `_NORMALMAP`，原始 Rendering Mode 为 Opaque。Q 在运行时启用 `_ALPHABLEND_ON`；若构建时没有材质使用这个关键字组合，Unity 会剔除相应 Shader 变体，出现脚本已设置 Alpha=0.2、编辑器正常，而 WebGL 外壁仍不透明的现象。

项目使用 `Assets/Resources/TankShellFadeVariants/WaterTankFade.mat` 保留该水箱的 Fade + Normal Map 变体。此材质继承外壁的着色器、贴图和参数，预先设置 Fade 模式、透明混合、ZWrite=0 及透明渲染队列；Resources 使它参与构建。原外壁仍使用原不透明材质，Q 继续创建并切换运行时副本。

请将这个材质及对应 `.meta` 一起提交，保留在 Resources 内。以后若为外壁增加发光、细节贴图等关键字组合，也需同步对应的 Fade 材质。修改后需重新构建，并在浏览器中刷新新产物。

其他显示方式也可以采用：

- 外壳材质改成透明：Built-in Standard / Standard (Specular setup) 的 **Rendering Mode → Transparent**，Albedo 颜色 Alpha 可先试 0.25，再按效果调整。
- 局部透明观察窗或剖切视图。

液体使用独立材质 `TankLiquidWater.mat`。水箱外壳和液体使用不同材质。

## 5. 水纹流动与高光

当前液体材质已切换到 **Boiler/Tank Flow Water**，原材质的 GUID 保持不变，已有场景中的引用继续生效。新建液体也使用该着色器。

1. 在 Project 中选中 `Assets/Materials_SJK/TankLiquidWater.mat`。
2. 确认 Shader 为 **Boiler → Tank Flow Water**。
3. 进入 Play，点击 Game 窗口后按 **Q** 让外壁透明，从斜上方观察水面。
4. 在材质 Inspector 调整以下参数，运行中即可观察差异。需要保存的材质修改应在退出 Play 后确认。

| 材质参数 | 用途 | 当前初值 / 建议 |
| --- | --- | --- |
| Body Color | 水体侧面的主色 | 深蓝青色；调整 RGB，Alpha 不控制透明度 |
| Surface Color | 水面的颜色 | 浅蓝青色 |
| Ripple Highlight Color | 水纹亮部颜色 | 浅青白色 |
| Flow Direction (World X Z) | 主流动方向 | X=1、Y=0.35；此向量的 X/Y 分别代表世界 X/Z，Z/W 不使用 |
| Flow Speed (Meters Per Second) | 主层水纹移动速度 | 0.08；建议 0.03～0.15，设为 0 可暂停全部水纹 |
| Ripple Density | 单位场景尺寸内水纹的密度 | 4；建议 2～6，越大水纹越细密 |
| Ripple Strength | 水纹法线起伏与明暗变化强度 | 0.16；建议 0.08～0.22，设为 0 得到平静表面 |
| Smoothness | 高光集中程度 | 0.86；建议 0.75～0.9 |
| Environment Reflection | 环境反射强度 | 0.45；效果受场景天空和反射探针影响 |

着色器把三个不同方向的弯曲波纹叠加，随时间移动，改变顶面的法线、颜色与高光。侧面保持较平稳的水体颜色，便于辨认液位。纹理坐标使用世界 X/Z，所以液位升降不会把水纹拉长。方向为零时自动使用世界 +X。

液位控制和水纹流动分别调节：**Target Level 01** 控制水位高度，**Flow Speed** 控制水纹速度。关闭 **Animate Demo** 后，即使水位固定，水纹也会继续流动；设置 Flow Speed 为 0 后，水位仍可升降。

该材质绘制不透明液体体积，配合 Q 透明外壁观察内部。水纹只改变着色，不移动 Cube 顶点，底部位置和液位读数保持准确。它不模拟波峰的真实几何位移、溅水或碰撞。没有新增运行时脚本、法线贴图、屏幕抓取或额外相机；目标为本项目 Built-in / WebGL 渲染环境。

## 6. 接入数据或 UI

其他脚本拿到 `TankLiquidLevel` 组件后，可使用以下接口：

```csharp
public TankLiquidLevel tankLiquid;

// 输入 0~1 的液位比例。
tankLiquid.SetLevel01(0.75f);

// 输入 0~100 的液位百分比。
tankLiquid.SetLevelPercent(75f);

// 输入从内腔箱底起算的液位高度，单位为米。
tankLiquid.SetLevelMeters(1.5f);
```

上述接口会关闭循环演示，并按 Smooth Time 平滑过渡。超出范围的有限数值会钳制到空或满，NaN / Infinity 会被忽略。

这三行调用应写在方法体中，例如接收传感器数据的回调；示例展示接口，不是完整的类。

UI Slider：将 Min Value / Max Value 设为 0 / 1，在 On Value Changed 中拖入 `TankLiquid`，选择 **Dynamic float → TankLiquidLevel.SetLevel01**。

需要初始化时直接显示液位，可调用：

```csharp
tankLiquid.SetLevelInstant01(0.5f);
```

当前读数可通过 `CurrentLevel01` 和 `CurrentHeightMeters` 获取。

## 7. 检查步骤

1. 空箱：0 时液体隐藏。
2. 半箱：0.5 时顶面位于内腔高度的一半。
3. 满箱：1 时顶面达到最大液位，留出实际箱顶所需余量。
4. 改变目标液位时，底部固定，顶部平滑升降。
5. 暂时隐藏外壳，检查液体是否穿出内壁。
6. 重新显示外壳，验证透明或剖切效果。
7. 进入 Play，确认水位固定时水纹仍然流动；材质 Flow Speed=0 时水纹暂停。
8. 构建 WebGL，并在浏览器中检查尺寸、材质和动画。

运行时 `LiquidBody` 的位置、旋转和缩放由组件统一控制；通过根对象位置与 Inner Size 校准。

## 8. 适用边界

- 水箱需要竖直，液体根对象可以绕 Y 轴旋转对齐，不能沿 X/Z 轴倾斜。
- 支持矩形等截面内腔。外形看起来是立方体，但凹腔侵入内腔时，必须校核液体方块是否穿壁；复杂内腔仍需要独立的内腔模型与水面轮廓方案。
- 此组件用于液位显示和数据驱动，不提供溅水、倾倒或流体碰撞模拟。

## 9. 文件与版本控制

运行脚本：`Assets/Script/TankLiquidLevel.cs`。

创建菜单和 Inspector：`Assets/Editor/TankLiquidLevelEditor.cs`；只参与编辑器，不进入 WebGL 运行代码。

外壳透明辅助代码：`Assets/Script/TankShellTransparency.cs`，由液位组件直接使用，不需要单独添加到对象上。

水纹着色器：`Assets/Shaders/TankFlowWater.shader`。

液体材质：`Assets/Materials_SJK/TankLiquidWater.mat`。场景接入后，应一起提交脚本、Shader、材质、场景以及对应 `.meta` 文件。

水箱 WebGL 透明变体：`Assets/Resources/TankShellFadeVariants/WaterTankFade.mat`。

## 10. 验证记录与参考

2026-10-14：在独立的 Unity 2021.3.19f1c1 验证项目中完成编译、空/满液位、底部固定、平滑升降、百分比与米制输入、循环演示、创建撤销/重做，以及大幅负缩放来源模型的创建检查，均通过。

同日增加 Q 外壁透明切换，完成反复切换、材质颜色与贴图保留、共享材质隔离、液体排除、禁用恢复、多材质槽和实际 Water Tank FBX 自动绑定验证，均通过。当前 SampleScene 已写入外壳引用、Q 快捷键和 0.2 透明度。

2026-10-15：水体材质增加流动水纹。在独立 Unity 验证项目中使用 Direct3D11 实际渲染，检查不同时间的水面像素发生变化、速度为 0 时完全静止、零方向参数可正常渲染、液体几何边界不变，以及实际水箱透明后可以显示新水面；均通过。原有液位和外壳透明的 464 项断言也通过。

同日将水体材质与 Shader 按 WebGL / OpenGLES3 目标构建 AssetBundle，完成平台着色器编译验证。此检查不等同于整个项目的 WebGL 构建或浏览器实测。

同日完成本项目完整 WebGL 构建，Unity 返回 Succeeded。随后针对浏览器中的外壁不透明问题，用同一个 `07 - End.mat` 创建独立 WebGL 复现：原构建按 Q 后脚本状态为透明、Alpha=0.2，但水体区域的渲染像素完全不变；包含 Fade + Normal Map 材质后可看到后方蓝色水体，再按 Q 恢复时像素与初始外观一致，两次浏览器测试均无 JavaScript 异常。

已将相同着色器、关键字、贴图引用和渲染状态的 `WaterTankFade.mat` 加入项目 Resources。该检查验证的是同材质的 WebGL 像素效果；完整场景中的摆放、内腔尺寸和实际数据仍按项目现场配置核对。

参考：[Mathf.SmoothDamp](https://docs.unity3d.com/2021.3/Documentation/ScriptReference/Mathf.SmoothDamp.html)、[ExecuteAlways](https://docs.unity3d.com/2021.3/Documentation/ScriptReference/ExecuteAlways.html)、[内置着色器变量（含 _Time）](https://docs.unity3d.com/2021.3/Documentation/Manual/SL-UnityShaderVariables.html)。

透明构建参考：[Shader 关键字与构建时变体保留](https://docs.unity3d.com/2021.3/Documentation/Manual/shader-keywords.html)、[Shader 变体剔除](https://docs.unity3d.com/2021.3/Documentation/Manual/shader-variant-stripping.html)。
