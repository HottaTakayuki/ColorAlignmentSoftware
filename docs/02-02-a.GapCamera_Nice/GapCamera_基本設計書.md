# GapCamera 基本設計書 差分整理（ColorAlignmentSoftware_Nice）

## 要約

基本設計書は GapCamera の主構成を押さえていますが、Nice 実装で追加された ADCP bulk 経路、gamma 管理、撮影距離依存の露出補正までは表現できていません。構成図と外部要素一覧をそのまま使うと、実装依存の重要な経路が抜けます。

## 差分一覧

| 観点 | 既存文書 | Nice 実装 | 影響 |
|------|----------|-----------|------|
| Controller 連携 | SDCP 中心 | ADCPClass + Newtonsoft.Json による bulk 取得・反映を追加 | 構成図、連携表、ソリューション方針の補強が必要 |
| ShootCondition 生成 | Settings をそのまま参照 | ForNice 条件で new ShootCondition(...) を生成 | 撮影条件設定の説明見直しが必要 |
| 計測前提値 | 固定条件中心 | Controller ごとの gamma 値を取得して保持 | 計測処理の前提条件を追記する必要がある |
| 露出補正 | 固定的な撮影条件 | 撮影距離に応じて F18/F22, 1/3, 0.5 へ補正 | 計測設計の条件分岐追記が必要 |

## 更新方針

- 構成図に ADCP JSON 経路を追加する。
- 外部要素一覧に ADCPClass、Newtonsoft.Json、configurationOfLedModelList を追加する。
- ソリューション方針に Nice 条件付き分岐、gamma 取得、撮影距離依存の露出補正を追加する。
