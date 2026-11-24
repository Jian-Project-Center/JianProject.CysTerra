# Jian Project（CysTerra）

[CysTerra](https://docs.eew.cryville.world/) 通用版扩展：订阅 Jian Project API 聚合通道，将预警 / 速报 / 海啸等事件接入 CysTerra 的事件摘要、事件详情与语音播报。

对齐共通端 **Alpha 0.0.15**、接口层 **Cryville.EEW 0.8.14**、Features / TTS **0.8.1**。扩展间引用使用 `IsExtension`；接口包使用 `ExcludeAssets=runtime`（与官方开发文档中 0.8.14 起的裁剪约定一致）。

## 安装

扩展仅适用于 CysTerra **通用版**（Android 版不支持扩展）。以下三个包均需安装，版本 **0.1.5.0**：

| 组件 | 导入包 |
| --- | --- |
| 事件源 + 事件摘要 | `JianProject.CysTerra-0.1.5.0.zip` |
| 事件详情 | `JianProject.CysTerra.Features-0.1.5.0.zip` |
| 语音消息 | `JianProject.CysTerra.TTS-0.1.5.0.zip` |

压缩包**统一**放在上级目录 `cysterra-Windows拓展/dist/`（与 Pancakes 等并列），勿再写入本仓 `JianProject/dist/`。本地打包：`python tools/pack_extensions.py`。

每个 zip 根目录须含：主 dll、`*.deps.json`、`Messages/`（多语言文案）、`LICENSE.txt` / `COPYING.txt`（从 `bin/ext/Release` 打包）。缺 `Messages` 会导致导入报找不到路径；缺 deps.json 会提示「未在目录中找到扩展」。发布附件见本仓库 [Releases](https://github.com/Jian-Project-Center/JianProject.CysTerra/releases)。

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

开发说明见 [CysTerra 扩展文档](https://docs.eew.cryville.world/) 与 [打包 / 版本](https://docs.eew.cryville.world/docs/packaging.html#versioning)。

## 许可证

版权所有 © 2026 纪安 / Jian Project Center。

联系邮箱：jianproject@163.com

本程序以 **GNU LGPL-3.0-or-later** 授权。LGPL 正文见 `LICENSE.txt`，其所引用的 GPL-3.0 正文见 `COPYING.txt`。对应源代码即本仓库；发布的扩展包内同时附带这两份许可证。
