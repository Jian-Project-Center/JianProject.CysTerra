# Jian Project（CysTerra）

[CysTerra](https://docs.eew.cryville.world/) 通用版扩展：订阅 Jian Project API 聚合通道，将预警 / 速报 / 海啸等事件接入 CysTerra 的事件摘要、事件详情与语音播报。

对齐共通端 **Alpha 0.0.15**、接口层 **Cryville.EEW 0.8.14**、Features / TTS **0.8.1**。扩展间引用使用 `IsExtension`；接口包使用 `ExcludeAssets=runtime`（与官方开发文档中 0.8.14 起的裁剪约定一致）。

## 安装

扩展仅适用于 CysTerra **通用版**（Android 版不支持扩展）。以下三个包均需安装，版本 **0.1.4.8**：

| 组件 | 导入包 |
| --- | --- |
| 事件源 + 事件摘要 | `JianProject.CysTerra-0.1.4.8.zip` |
| 事件详情 | `JianProject.CysTerra.Features-0.1.4.8.zip` |
| 语音消息 | `JianProject.CysTerra.TTS-0.1.4.8.zip` |

每个 zip 的主 dll 与对应 `*.deps.json` 须位于根目录（从 `bin/ext/Release` 打包），可直接导入；缺少 deps.json 会导致 CysTerra 提示「未在目录中找到扩展」。发布附件见本仓库 [Releases](https://github.com/Jian-Project-Center/JianProject.CysTerra/releases)。

## 配置

| 项 | 说明 |
| --- | --- |
| 认证密钥 | 填写邮件内 Token；扩展会自动置换并保存 |
| 筛选项 | 按来源开关，默认全部开启 |
| WebSocket | 默认 `/all`；设置可选主服务器/备服务器/自动切换（`api.sismotide.top` ↔ `api.jianproject.cloud`） |
| 服务器选择 | 自动切换 / 主服务器 / 备服务器 |

事件摘要、事件详情、TTS 均按数据源单独导出（名称形如「Jian Project（…）」），可分别开关。

## 行为说明

- 连接后不发送列表指令。
- 「自动切换」：`WebSocketWorker` 无内置切服；在 `OnError` 识别 `SourceWorkerNetworkException`，约 60 秒内累计 3 次则切换主/备主机，由下次 `GetUri` 生效。
- 丢弃 `type=all` / `initial` 快照帧；连接后首帧业务数据整帧丢弃，避免 TTS 轰炸。
- 卡片机构名使用正式缩写（EMSC 可带测定机构，如 `EMSC | GFZ`）；地名优先 `placename_zh`。
- 上游没有的震级、深度、坐标不会补写。
- 速报类标题对齐 Fan Studio / Wolfx：多数为「地震信息」，香港天文台为「地震报告」。
- CENC 卡片输出 `Quality:CENC`（正式测定 / 自动测定），判定对齐 Fan（`autoFlag` / `infoTypeName`）与 Wolfx（`type=reviewed|automatic`）；TTS 同步朗读测定类型。
- 日本气象厅紧急地震速报标题为「紧急地震速报（预报/警报）」；假定震源时标注等级法 / PLUM Quality（对齐 Wolfx）。
- 中国地震预警网、CWA、KMA、ShakeAlert 等标题固定为「地震预警」（续报不改标题，对齐 Fan）；ShakeAlert 不展示报数角标。
- 报数 `#N` 仅预警源在上游有 serial/updates/reportNum 时写出；CENC 速报不写 Serial。
- 日本气象厅海啸会生成海啸事件。
- 不处理：Early-est、中国气象局气象预警、自然资源部海啸预警、JMA Atom 火山情报、USGS 火山警戒、NOAA/USGS 海啸预警。烈度速报不在 `/all` 通道内，扩展不提供对应开关。

开发说明见 [CysTerra 扩展文档](https://docs.eew.cryville.world/) 与 [打包 / 版本](https://docs.eew.cryville.world/docs/packaging.html#versioning)。

## 许可证

版权所有 © 2026 纪安 / Jian Project Center。

联系邮箱：jianproject@163.com

本程序以 **GNU LGPL-3.0-or-later** 授权。LGPL 正文见 `LICENSE.txt`，其所引用的 GPL-3.0 正文见 `COPYING.txt`。对应源代码即本仓库；发布的扩展包内同时附带这两份许可证。
