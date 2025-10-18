# Codex Agent Guidelines for LifeOn

## 项目基础信息介绍
名称：生生不息（Life On And On）
语言：中文、英文
目标平台：PC、移动端
游戏画风：2D、等距视角、正交相机
玩法简介：2D城市建造游戏、游戏是回合制进行，单元网格建造，模拟一个文明从部落到王朝，游戏中会需要设计城市满足市民需求、抵御外敌、自然灾害，游戏种存在架空幻想元素、游戏中存在龙、魔法等元素，最终社会科技水平为类似大航海时期

### 开发
开发引擎版本：Unity2022.3.60f1c1
项目架构
输入系统：使用新版输入系统
资源加载：addressable
相机管理：cinemachine
持久化：EasySave3
本地化：Unity官方本地化插件
对象池：LeanPool
文本：TMP

## 适用范围
- 仓库根目录下的所有文件，重点关注 `Assets/` 内的 Unity C# 脚本与资源组织。
- Unity 编辑器版本与具体包信息保持不变；若需新增依赖，务必先与仓库维护者沟通。

## 通用约定
- **习惯**：回答提问时使用中文
- **预制体**：关于需要制作预制体的只需要给出大概层级即可，不需要提供预制体源码
- **语言与编码**：脚本、类名、方法名、枚举等源代码标识一律使用英文；注释需要中文写，但要保持含义清晰。文件保存为 UTF-8 无 BOM。
- **命名风格**：
  - 类、结构、枚举使用 `PascalCase`。
  - 方法使用 `PascalCase`。
  - 私有字段使用 `_camelCase`，公共字段与属性使用 `PascalCase`。
  - 常量使用 `PascalCase`，集中放在 `LOConstant` 或相关常量类中。
  - UI面板脚本以`UIPanel_`开头，UI非面板以`UIItem_`开头
- **序列化字段**：Unity 序列化字段使用 `[SerializeField] private` 或使用 `public` 仅当确需外部访问。必要时配合 Odin Inspector 属性，但请避免破坏现有 Inspector 布局。
- **日志与调试**：使用 `Debug.Log*` 时提供明确上下文；提交前删除无用的调试输出。

## 脚本开发
- 新增或修改脚本需保持与现有结构兼容，优先复用已有工具（如 `MonoSingleton<T>`、`PanelBase`、`UIManager` 等）。
- UI 控制逻辑应放在对应的面板类中，通过 `UIManager.ShowPanel<T>` 管理生命周期；业务脚本只负责传参、订阅事件。
- 处理输入及状态流转时，保持状态枚举为英文，并确保 `switch` 分支覆盖 `default`。
- 任何异步方法调用 `async void` 仅限 Unity 事件入口；其余使用 `Task` 并处理异常。
- 需要赋值的脚本字段需要使用` [LabelText("中文")]`进行修饰。


## 资源与地址
- Addressables 名称使用 `PascalCase`，与脚本类名一致（例如 `BuildingConfirmPanel`）。
- 新增 UI 预制体时，同步创建对应的 `PanelBase` 子类，并在 `UIManager` 现有层级体系中合理选择展示层（默认 `UIManager.UILayer.Popup`）。

## 测试与验证
- 无法运行 Unity 时，应在提交说明中明确标注“需在编辑器中验证”。

以上约定若需调整，请先在 `AGENT.md` 中更新并通知团队。未经说明的破坏性更改严禁提交。
