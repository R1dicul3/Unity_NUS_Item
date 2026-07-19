# 主菜单系统使用说明

本目录包含一套运行时自动生成的主菜单 UI 脚本，支持主菜单、读档界面与 Credits（制作人名单）三个场景。所有 UI 在场景运行时自动构建，无需手动拖拽摆放。

---

## 目录结构

```
Assets/
├── Scripts/
│   └── MainMenu/
│       ├── MenuUIHelper.cs      # 通用 UI 构建辅助
│       ├── MainMenuUI.cs        # 主菜单
│       ├── LoadGameUI.cs        # 读档/存档槽界面
│       └── CreditsUI.cs         # 制作人名单
└── Editor/
    └── MainMenuSceneSetup.cs    # 编辑器一键创建场景工具
```

---

## 快速开始（推荐）

### 方法一：使用编辑器菜单一键创建（最简单）

1. 将脚本文件放入项目对应文件夹后，**回到 Unity 等待编译完成**。
2. 点击顶部菜单栏 **MainMenu > Create All Menu Scenes**。
3. 工具会自动在 `Assets/Scenes/` 下创建三个场景，并添加到 **File > Build Settings** 中。
4. 双击打开 `Assets/Scenes/MainMenu.unity`，点击 **Play** 即可看到效果。

如需单独创建某个场景，也可使用：
- **MainMenu > Create MainMenu Scene Only**
- **MainMenu > Create LoadGame Scene Only**
- **MainMenu > Create Credits Scene Only**

### 方法二：手动创建场景

1. 新建三个空场景（**File > New Scene**），分别保存为：
   - `Assets/Scenes/MainMenu.unity`
   - `Assets/Scenes/LoadGame.unity`
   - `Assets/Scenes/Credits.unity`
2. 在每个场景中创建一个空 GameObject，分别挂载对应脚本：
   - MainMenu 场景 → `MainMenuUI`
   - LoadGame 场景 → `LoadGameUI`
   - Credits 场景 → `CreditsUI`
3. 打开 **File > Build Settings**，将三个场景按顺序加入 **Scenes In Build** 列表。
4. 点击 Play 运行。

---

## 界面说明

### 主菜单（MainMenu）

运行后自动生成居中垂直排列的界面，从上至下依次为：

| 元素 | 状态 | 行为 |
|------|------|------|
| **Stopover**（Logo） | 纯文字展示 | 无 |
| **New Game** | 灰色禁用 | 暂未实现 |
| **Load Game** | 可用 | 切换到 LoadGame 场景 |
| **Settings** | 灰色禁用 | 暂未实现 |
| **Credits** | 可用 | 切换到 Credits 场景 |
| **Exit** | 可用 | 退出游戏（编辑器内停止 Play） |

在 `MainMenuUI` 组件的 Inspector 中，勾选 **Show Placeholder Buttons As Enabled** 可将未实现的按钮设为可点击（点击后仅在 Console 打印日志，方便调试）。

### 读档界面（LoadGame）

- 默认显示 **3 个存档槽**，自动检测 `Application.persistentDataPath` 下是否存在对应的存档文件。
- 有存档的槽位为**可用状态**（高亮），点击后可在 `OnSlotClicked` 方法中接入实际读档逻辑。
- 无存档的槽位显示为 **(Empty)** 且置灰。
- 底部有 **< Back** 按钮返回主菜单。

可在 Inspector 中调整 **Save Slot Count**（存档槽数量）与 **Save File Extension**（存档后缀名）。

### Credits 界面

- 居中显示标题 **Credits** 与制作人员名单。
- 在 `CreditsUI` 组件的 Inspector 中可直接编辑 **Credits** 数组，自定义职位（Role）与姓名（Name）。
- 底部有 **< Back** 按钮返回主菜单。

---

## 自定义外观

所有 UI 脚本均在 Inspector 中暴露了可调整参数，例如：

- **Background Color**：背景色
- **Button Color**：按钮颜色
- **Logo / Title Color**：标题文字颜色
- **Font Size**：各字号
- **Override Font**：自定义字体（留空则自动使用 Unity 内置默认字体）

修改后重新运行即可生效。

---

## 后续扩展建议

1. **接入真实存档系统**：在 `LoadGameUI.OnSlotClicked()` 中调用你的存档管理器（如 `SaveSystem.Load(slot)`），然后切换到实际游戏场景。
2. **添加过渡动画**：可在切换场景前加入淡入淡出（Fade）或加载画面（Loading Screen）。
3. **替换 Logo**：将 `MainMenuUI` 中的文字 Logo 替换为 Image 组件，加载游戏标题贴图。
4. **启用 New Game / Settings**：实现对应功能后，将 `MainMenuUI` 中 `CreateMenuButton` 的 `isImplemented` 参数改为 `true`，并补充点击回调逻辑。

---

## 注意事项

- 本系统使用 Unity 内置 **UGUI**（`UnityEngine.UI`），无需额外安装 TextMeshPro。
- 运行时若文字显示为方块或空白，请在对应 UI 脚本的 Inspector 中指定一个 **Override Font**。
- 场景名称固定为 `MainMenu`、`LoadGame`、`Credits`，如需改名请同步修改脚本中的 `SceneManager.LoadScene("场景名")`。
