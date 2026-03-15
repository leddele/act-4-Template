# Slay the Spire 2 - Act 4 & Linear Map Template

这是一个为《杀戮尖塔2》(Slay the Spire 2) mod开发者准备的第四幕 Mod 模板。它实现了在原版三幕流程之后注入一个完全自定义的 第四幕 ，并提供了线性地图生成逻辑及存档安全保护。

## 🌟 核心功能 (Features)

- **Act 4 注入**：在打败 Act 3 Boss 后自动进入自定义的第四幕序列。
- **线性地图系统**：建立类似1代的地图布局（篝火 -> 商店 -> 精英 -> 篝火 -> Boss）。
  
- **状态同步修复**：解决了自定义层级中地图点击无响应（Synchronizer）的问题。
- **存档安全保护**：包含防御性反序列化补丁，防止找不到4层而导致的“无法继续游戏”报错。




## 📝 如何自定义 (How to Customize)

- **修改地图长度**：在 `FinalAct.cs` 中修改 `BaseNumberOfRooms`（至少7）。
- **更改 Boss/精英**：在 `Act4Logic.cs` 的 `Postfix_Rooms` 方法中替换 `ModelDb.Encounter<T>` 的类型。
- **自定义视觉**：在 `Act4Logic.cs` 中修改 `Prefix_Rest` 和 `Prefix_Bg` 的路径，指向你自己的 `.tscn` 场景。
