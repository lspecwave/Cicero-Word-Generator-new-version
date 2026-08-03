# Cicero New Version README

本文档记录 `Cicero-Word-Generator-new-version` 分支目录中的所有定制改动。

以后所有新的代码改动、构建方式变化、已知问题修复，都需要同步追加到本文档的 Change Log 中。默认工作目录为：

`D:\Share_Athena\lwt\Cicero\Cicero-Word-Generator-master\Cicero-Word-Generator-new-version`

## 使用原则

- 以后默认只修改 `Cicero-Word-Generator-new-version` 目录，不再修改外层旧工程，除非明确要求。
- `.seq` 文件不能因为 compare、load 或 UI 显示修复被自动改写。
- 不能安装 NI 软件包；NI 相关依赖使用工程根目录下已有的四个 DLL。
- 每次 build 后，优先更新 `ReleaseBuilds\Cicero\WordGenerator.exe`。
- 每次新增功能或修 bug 后，必须在本文档追加一条记录，说明改动目的、涉及文件和 build 状态。

## Build

当前 Cicero UI 使用 Visual Studio 2022 Community 的 MSBuild 直接编译：

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' WordGenerator\Cicero.csproj /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU /p:PostBuildEvent= /m /v:minimal
```

编译成功后复制 Release 输出：

```powershell
Copy-Item -Path WordGenerator\bin\Release\* -Destination ReleaseBuilds\Cicero -Force
```

最新确认的 UI exe：

`ReleaseBuilds\Cicero\WordGenerator.exe`

时间戳：`2026-07-31 11:24:40`

## 当前改动汇总

### 1. Sequence Compare

目标：完善 sequence compare，使任意两个 `.seq` 文件可以比较出真实差异，并且不修改 `.seq` 文件。

主要文件：

- `DataStructures\SequenceData\SequenceComparer.cs`
- `WordGenerator\Controls\MainClientForm.cs`
- `WordGenerator\Controls\Dialogs\SequenceDifferencesForm.cs`
- `WordGenerator\Controls\Dialogs\SequenceDifferencesForm.Designer.cs`
- `WordGenerator\Cicero.csproj`

行为：

- 通过文件对话框选择 base sequence 和 compared sequence。
- 使用独立加载路径读取两个 `.seq` 文件，不替换当前 `Storage.sequenceData`。
- 输出差异报告窗口，显示路径、旧值、新值。
- 覆盖 sequence metadata、timesteps、groups、variables、lists、modes、pulses、waveforms 等序列结构差异。

注意：

- compare 只读输入文件。
- compare 不更新 recent file，也不改变当前打开的 sequence。

### 2. Run Sequence 时主窗口可查看但不可编辑

目标：运行 foreground sequence 时，不再用模态窗口完全阻塞 Cicero 主窗口；主窗口允许查看，但不能编辑 sequence 内容。

主要文件：

- `WordGenerator\Controls\MainClientForm.cs`
- `WordGenerator\Controls\Dialogs\RunForm.cs`
- `WordGenerator\Controls\SequencePage\RunControl.cs`
- `WordGenerator\Controls\SequencePage\DigitalGrid.cs`
- `WordGenerator\Controls\SequencePage\TimestepEditor.cs`
- 多个 tab/page editor 控件文件

行为：

- Foreground run 使用非模态 `RunForm.Show()`。
- 运行期间主窗口保持可切换 tab、可滚动、可查看 sequence、override、analog、variables 等页面。
- 运行期间禁用会修改 sequence/settings 的控件和操作。
- 运行结束或窗口关闭后恢复编辑。

### 3. Variables 排序按钮

目标：variables 多时可以一键按变量名排序，便于查找，同时不能影响变量本身的使用。

主要文件：

- `WordGenerator\Controls\VariablesTab\VariablesAndListPage.cs`
- `WordGenerator\Controls\VariablesTab\VariablesAndListPage.Designer.cs`

行为：

- 新增 `Sort Variables` 按钮。
- 按 `VariableName` 字母顺序排序。
- 保留特殊变量位置。
- 不 clone、不重命名、不重新创建变量对象，避免破坏公式、list binding、引用关系。

### 4. Loop timestep group 上方显示顺序修复

目标：存在多个 loop timestep group 时，上方 timestep editor 显示顺序必须和真实 timestep index 顺序一致。之前下方 digital/analog 和运行顺序是正确的，问题只在上方 UI 显示。

主要文件：

- `WordGenerator\Controls\SequencePage\SequencePage.cs`
- `WordGenerator\Controls\SequencePage\SequencePage.Designer.cs`

最终正确实现：

- 上方 `timeStepsFlowPanel` 从 `FlowLayoutPanel` 改为普通 `Panel`。
- 新增 `layoutTimestepEditorPositions()`，按 `Storage.sequenceData.TimeSteps` 的真实顺序手动设置每个 `TimestepEditor.Location.X`。
- timestep label 继续显示真实 index：`Storage.sequenceData.TimeSteps.IndexOf(step) + 1`。
- 不修改 `TimeSteps` 列表、不重新编号、不清理 loaded sequence 中的 timestep、不影响 digital/analog 显示和运行逻辑。

特别注意：

- 不要把 timestep label 改成“当前可见顺序”的连续编号；真实 index 本来是正确的。
- 修的是上方控件摆放位置，不是 sequence 数据顺序。
- Load/Save 路径不能为了显示问题修改 sequence 内容。

### 5. Run 期间 loop copy cleanup

目标：运行时如果创建临时 loop copy，运行结束或中途失败都应清理，避免临时 copy 泄漏到运行中的 UI 状态。

主要文件：

- `WordGenerator\Controls\Dialogs\RunForm.cs`

行为：

- `sequence.createLoopCopies()` 后的运行主流程包在 `try/finally` 中。
- `finally` 中仅在本次 run 使用 loops 时执行 `sequence.cleanupLoopCopies()`。
- 该 cleanup 只用于运行过程，不用于普通 Load/Save。

### 6. 本地 NI DLL 和 Atticus build

目标：不安装 NI 软件，使用本地已有 DLL 解决编译引用。

相关 DLL：

- `NationalInstruments.Common.dll`
- `NationalInstruments.DAQmx.dll`
- `NationalInstruments.NI4882.dll`
- `NationalInstruments.VisaNS.dll`

注意：

- Atticus server 使用本地 HintPath。
- DAQmx DLL 更适合 x86 Atticus build；不要通过安装 NI 来解决依赖。

## Change Log

### 2026-07-31

- 新建 `Cicero-Word-Generator-new-version` 作为以后主要维护目录。
- 完善 Sequence Compare，可以比较任意两个 `.seq` 文件并显示结构化差异，compare 不修改输入文件。
- 修改 RunForm 行为，foreground run 时 Cicero 主窗口可查看但不可编辑。
- 为 Variables 页面增加 `Sort Variables` 按钮。
- 修复多个 loop timestep group 在上方 timestep editor 中显示顺序混乱的问题：
  - 最终方案是普通 `Panel` + 手动按真实 `TimeSteps` 顺序定位。
  - 已撤回错误的“按可见顺序重新编号”方案。
- 保留真实 timestep index 显示，不改变 sequence 顺序。
- 添加运行期间 loop copy 的 `finally` cleanup。
- 更新 `.gitignore`，GitHub 同步时只放行 `ReleaseBuilds\Cicero` 这一套 Cicero UI build；其他 release build 目录继续忽略。
- build 成功，最新 UI exe 位于 `ReleaseBuilds\Cicero\WordGenerator.exe`。

### 2026-08-03

- 改动目的：修复 run 期间主窗口只读锁结束后，点击 `Unlock Lists` 时 list 顶部 checkbox 仍保持灰色禁用的问题。
- 涉及文件：
  - `WordGenerator\Controls\VariablesTab\ListEditorPanel.cs`
  - `WordGenerator\Controls\VariablesTab\VariablesAndListPage.cs`
- 行为变化：`Unlock Lists` 现在会显式恢复每个 `ListEditorPanel` 内部 checkbox、label 和按 list enabled 状态控制的编辑控件，不再被 run-time read-only restore 留下的 `Enabled=false` 状态卡住。
- 是否影响 `.seq` 文件：不影响。只修复 UI 解锁状态同步，不改变 list 数据、list enabled 数据或 list locked 数据。
- Build 结果：Cicero UI build 成功，并已覆盖默认 release 目录。
- 最新 exe 路径：`ReleaseBuilds\Cicero\WordGenerator.exe`。

## Future Change Template

以后追加改动时使用这个格式：

```markdown
### YYYY-MM-DD

- 改动目的：
- 涉及文件：
- 行为变化：
- 是否影响 `.seq` 文件：
- Build 结果：
- 最新 exe 路径：
```

