# 08-6. EstimateCameraPos連携メンバ

---

## 8-6-1. setCameraPosition

| 項目 | 内容 |
|------|------|
| シグネチャ | `private void setCameraPosition(UnitInfo unit, double pan, double tilt)` |
| 概要 | カメラのパン・チルト角度を設定する連携メソッド |

引数
| No. | 引数名 | 型 | 必須 | 説明 |
|-----|--------|----|------|------|
| 1 | unit | UnitInfo | Y | 対象ユニット |
| 2 | pan | double | Y | パン角度 |
| 3 | tilt | double | Y | チルト角度 |

返り値: なし（void）

処理概要
| 手順No. | 処理内容 | 詳細 |
|---------|----------|------|
| 1 | パラメータ設定 | ユニットにパン・チルト値を設定 |
| 2 | コントローラ連携 | 必要に応じてコントローラへ反映 |
| 3 | 結果通知 | 設定結果をログ・UIへ通知 |

主要呼出し先
| 呼出し先 | 役割 | 同期/非同期 |
|----------|------|--------------|
| updateUnitCameraPos | パラメータ設定 | 同期 |
| sendCameraPosToController | コントローラ連携 | 同期 |
| saveLog | ログ出力 | 同期 |
| ShowMessageWindow | 異常通知 | 同期 |
