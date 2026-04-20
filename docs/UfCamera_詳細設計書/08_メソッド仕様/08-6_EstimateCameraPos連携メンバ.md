### 8-6. EstimateCameraPos連携メンバ

本節は、GapCamera_詳細設計書の 8-6 と章立て・節建・粒度を合わせるための対応節である。UfCamera では推定処理を主に `GetCameraPosUf` / `CheckCameraPos` 側で実装しているため、以下は連携観点の対応定義を記載する。

#### 8-6-1. CameraParameter.Set

| 項目 | 内容 |
|------|------|
| 概要 | カメラ内部/外部パラメータ設定に相当する連携点。UfCamera では姿勢推定前提として内部で吸収される。 |
| Uf側対応 | `8-5-3-3 GetCameraPosUf` |

#### 8-6-2. ImagePoints

| 項目 | 内容 |
|------|------|
| 概要 | 画像上特徴点集合の連携点。UfCamera では Blob 検出結果を入力として扱う。 |
| Uf側対応 | `8-5-3-3 GetCameraPosUf`（`CvBlob[,] aryBlob`） |

#### 8-6-3. ObjectPoints

| 項目 | 内容 |
|------|------|
| 概要 | 物体座標系の基準点集合連携。UfCamera では Cabinet/Unit 幾何情報から間接的に供給される。 |
| Uf側対応 | `8-3-1 SetCabinetPos`、`8-3-6 SetCamPosTarget` |

#### 8-6-4. Estimate

| 項目 | 内容 |
|------|------|
| 概要 | 姿勢推定実行の連携点。UfCamera では位置推定処理を `GetCameraPosUf` に集約する。 |
| Uf側対応 | `8-5-3-3 GetCameraPosUf` |

#### 8-6-5. Rot

| 項目 | 内容 |
|------|------|
| 概要 | 推定回転成分（Rotation）の受渡し連携点。 |
| Uf側対応 | `8-5-3-3 GetCameraPosUf` の `CameraPosition` 内回転成分 |

#### 8-6-6. Trans

| 項目 | 内容 |
|------|------|
| 概要 | 推定並進成分（Translation）の受渡し連携点。 |
| Uf側対応 | `8-5-3-3 GetCameraPosUf` の `CameraPosition` 内並進成分 |

