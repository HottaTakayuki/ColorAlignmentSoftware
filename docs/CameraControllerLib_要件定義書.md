# CameraController.lib 要件定義書

| 項目 | 内容 |
|------|------|
| プロジェクト名 | CameraController.lib |
| 作成日 | 2026年4月15日 |
| 作成者 | （記入） |
| バージョン | 1.0 |

---

## 1. ビジネス要件

### 1-1. To-Be業務プロセス概要

CameraController.libは、Sony α（アルファ）シリーズデジタルカメラをWindowsアプリケーションからプログラマティックに制御するための静的ライブラリです。Windows Portable Devices (WPD) API を介したUSB/PTP通信を抽象化し、カメラのデバイス検出・接続管理・撮影パラメータ制御・画像取得を統一されたインタフェースで提供します。

主な利用形態は、CameraControllerSharp.dll のビルド時に本ライブラリを静的リンクしてネイティブ制御処理を組み込む構成である。必要に応じて、C スタイル公開APIを介したネイティブアプリケーションからの直接利用も可能とする。

本ライブラリは以下の機能を提供します：
- 接続可能なカメラデバイスの列挙・接続・切断管理
- 静止画撮影およびライブビュー画像の取得
- 撮影パラメータ（F値、シャッター速度、ISO感度、ホワイトバランス）の取得・設定・ステップ変更
- 画像サイズ・圧縮形式の設定
- フォーカスモードの取得・設定、AF/MFホールド操作、ニアファー制御
- シングルオートフォーカスの実行
- フォーカスエリアの取得・設定、AFエリア位置の取得・設定
- C スタイルの公開API（`CameraControllerApi.hpp`）経由での利用

---

### 1-2. 業務内容、業務特性（ルール、制約）

| 業務名 | 業務内容 | ルール・制約 |
|--------|----------|-------------|
| デバイス列挙 | USBに接続されたSonyカメラを検出し、台数とデバイス名を提供 | 接続済みデバイスのみが対象。呼び出し側が列挙後にインデックスまたはデバイス名で指定して接続する |
| カメラ接続管理 | 指定デバイスへの接続確立および切断 | 同時接続は1台のみ。接続前に必ずEnumerateDevicesを呼び出すこと |
| 画像取得 | 撮影指示後にカメラから画像バイナリ（JPEG / RAW）を取得 | GetImageは撮影完了まで待機（is_wait=true時）。バッファ管理は呼び出し側の責務 |
| ライブビュー取得 | プレビュー用低解像度JPEG画像をカメラから取得 | 連続呼び出し可能。撮影とは独立したコマンド |
| 撮影パラメータ制御 | F値・シャッター速度・ISO・ホワイトバランスの取得・設定・ステップ変更 | 設定可能範囲はカメラモデルごとのParameterSetCameraで定義される |
| 圧縮形式設定 | ECO / STD / FINE / XFINE / RAW / RAW+JPG / RAWC / RAWC+JPGの選択 | CameraController::CompressionSetting 列挙体で指定 |
| フォーカス制御 | フォーカスモード変更、AF/MFホールド、ニアファー調整、シングルAF | フォーカスモードはFocusModeTable定義値を使用。ニアファーはステップ指定 |
| フォーカスエリア制御 | フォーカスエリアの種別・位置指定 | AFエリア位置はX/Y座標（UINT16）で指定 |

---

### 1-3. 組織構成、要員、設備

#### 組織構成

省略

#### 要員スキル・規模

- 開発者：C++ および Windows Portable Devices (WPD) API、PTP/MTPプロトコルの知識が必要
- 組込み利用者：C/C++ でのDLL・libリンク、およびCameraControllerApi.hppの理解が必要

#### 必要設備

- Sony α シリーズカメラ（ILCE-7RM2 / ILCE-7R / ILCE-6000 / ILCE-5000 / ILCE-6400）
- USB 2.0 以上のインタフェース
- Windows 開発環境（Windows 7以上）
- Visual Studio（C++ ビルド環境）
- Windows SDK（PortableDeviceApi.lib, PortableDevice.h 等）

---

### 1-4. 業務KPIとその目標値

| KPI | 現状値 | 目標値 | 達成期限 |
|-----|--------|--------|---------|
| カメラ接続時間 | | 2秒以内 | |
| 撮影コマンド応答時間（GetImage） | | 2.5秒以内（DEFAULT_SHUTTER_WAITING_TIME + IMAGE_TRANSFER_WAITING_TIME） | |
| パラメータ設定応答時間 | | 50ms以内 | |
| ライブビュー取得時間 | | 250ms以内 | |

---

### 1-5. 概要業務フロー

```
[呼び出し側アプリケーション開始]
    ↓
[CreateCameraControllerInstance()]
    ↓
[CameraControllerEnumerateDevices() → 接続デバイス数取得]
    ↓
[CameraControllerGetDeviceName() → デバイス名取得（任意）]
    ↓
[CameraControllerConnectDevice() → カメラ接続]
    ↓
[撮影パラメータ設定]
    ├─→ [SetImageSize / SetCompressionSetting]
    ├─→ [SetFNumber / SetShutterSpeed / SetISO / SetWhiteBalance]
    └─→ [SetFocusMode / SetFocusArea / SetAfAreaPosition]
    ↓
[撮影 / ライブビュー取得]
    ├─→ [GetImage() → JPEG/RAW バイナリ取得]
    └─→ [GetLiveImage() → ライブビューJPEGバイナリ取得]
    ↓
[CameraControllerDisconnectDevice()]
    ↓
[ReleaseCameraControllerInstance()]
```

---

### 1-6. システム化の対象となる業務

| 対象業務 | 実現手段 | 備考 |
|----------|----------|------|
| カメラ通信の抽象化 | WPD/PTP APIラッパー（CameraControllerAlphaCore） | ハードウェア差異を外部へ露出しない |
| カメラモデル差異の吸収 | ParameterSetCamera 構造体によるモデル別パラメータ定義 | ILCE-7RM2 / 7R / 6000 / 5000 / 6400 に対応 |
| パラメータ名称変換 | FNumberTable / ShutterSpeedTable / ISOTable / WhiteBalanceTable / FocusModeTable による文字列⇔コマンド値変換 | ヒューマンリーダブルな文字列で操作可能 |
| C++クラス−Cスタイルブリッジ | CameraControllerApi.hpp の WINAPI 関数群 | .NET や Pure-C 環境からの利用を可能にする |

---

### 1-7. ビジネス制約

| 制約種別 | 内容 |
|----------|------|
| スケジュール | |
| コスト | |
| 技術 | WPD API を使用するため Windows 環境限定 |
| プラットフォーム | Windows OS 限定（Linux / macOS 非対応） |
| 対応カメラ | Sony α シリーズ（ILCE系）のみ。Nikon・Canon 等は対象外 |
| 通信方式 | USB 有線接続のみ。Wi-Fi / Bluetooth 非対応 |

---

### 1-8. その他の業務要件

- 画像バイナリのバッファ確保・解放は呼び出し側の責務とする
- コンパイルはVisual Studio でのネイティブC++（x64 / x86）ビルドを前提とする

---

## 2. システム要件（機能要件）

### 2-1. システム全体像

本ライブラリは単体の静的ライブラリとして提供するが、CAS 系システムでは CameraControllerSharp.dll にビルド時組み込みされる利用形態を主とする。

```
┌─────────────────────────────────────────────────────────┐
│              呼び出し側アプリケーション                   │
│        (AlphaCameraController / CAS / テストアプリ)       │
└─────────────────────────────────────────────────────────┘
                           ↓
                  CameraControl.dll / CameraControllerSharp.dll
                           ↓
              CameraControllerApi.hpp (C WINAPI) または静的リンク
                           ↓
┌─────────────────────────────────────────────────────────┐
│                  CameraController.lib                   │
│  ┌──────────────────────────────────────────────────┐   │
│  │  CameraControllerAlpha (パラメータ変換レイヤ)     │   │
│  │  ・文字列 ⇔ コマンド値変換                        │   │
│  │  ・モデル別パラメータ範囲チェック                  │   │
│  └──────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────┐   │
│  │  CameraControllerAlphaCore (WPD通信レイヤ)        │   │
│  │  ・デバイス列挙 / 接続 / 切断                    │   │
│  │  ・PTPプロパティ Get/Set                          │   │
│  │  ・画像転送（静止画 / ライブビュー）              │   │
│  └──────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────┐   │
│  │  CameraControllerAlphaEventsCallback             │   │
│  │  ・IPortableDeviceEventCallback 実装              │   │
│  │  ・画像転送完了イベント受信                        │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                           ↓
              Windows Portable Devices (WPD) API
                           ↓
                    USB / カメラドライバ
                           ↓
          ┌────────────────────────────────┐
          │   Sony α シリーズカメラ          │
          │   (ILCE-7RM2 / 7R / 6000 /     │
          │    5000 / 6400)                 │
          └────────────────────────────────┘
```

---

### 2-2. システム化対象領域（適用範囲）と影響範囲

#### 適用範囲

- **デバイス管理**：USBに接続されたSony αカメラの列挙・接続・切断
- **画像取得**：静止画（JPEG / RAW）取得、ライブビュー画像取得
- **露出制御**：F値・シャッター速度・ISO感度・ホワイトバランスの取得・設定・ステップ変更
- **画像設定**：画像サイズ（S/M/L）・圧縮形式（ECO〜RAWC+JPG）の設定
- **フォーカス制御**：フォーカスモード取得・設定、AF/MFホールド、ニアファー調整、シングルAF、フォーカスエリア設定・AFエリア位置設定
- **C API提供**：WINAPI 形式のC言語互換インタフェース（CameraControllerApi.hpp）

#### 影響を受ける周辺システム

| システム名 | 影響内容 |
|-----------|---------|
| 呼び出し側アプリケーション | CameraControllerApi.hpp / CameraControllerAlpha.h のインタフェース変更時に影響 |
| Windows OS / WPD スタック | OSバージョンアップやドライバ更新による通信互換性への影響 |
| Sony カメラファームウェア | ファームウェア更新でのPTPコマンドセット変更により動作影響の可能性 |

---

### 2-3. ソリューション方針

- **多層アーキテクチャ**：公開API層（`CameraControllerApi`）→ パラメータ変換層（`CameraControllerAlpha`）→ WPD通信層（`CameraControllerAlphaCore`）の3層構成により、関心の分離と保守性を確保
- **テーブル駆動のパラメータ変換**：FNumberTable / ShutterSpeedTable 等の変換テーブルを`CameraControllerAlphaParameterTable.h`に集約し、追加・修正を容易にする
- **モデル別パラメータ管理**：`CameraControllerAlphaParameterModelTable.h`の`ParameterSetCamera`構造体でカメラモデルごとの設定可能範囲を管理し、モデル追加時の局所変更を実現
- **イベント駆動の画像転送**：`IPortableDeviceEventCallback`実装（`CameraControllerAlphaEventsCallback`）により撮影完了イベントを受信し、ポーリングを不要とする
- **純粋仮想インタフェース**：`CameraController`基底クラスを純粋仮想インタフェースとして定義し、将来的な別メーカーカメラ対応を可能にする

---

### 2-4. システム機能要件

| No. | 機能名 | 機能概要 | 優先度 |
|-----|--------|----------|--------|
| 1 | デバイス列挙 | `EnumerateDevices()` / `CameraControllerEnumerateDevices()` — 接続中のSony αカメラ台数を返す | 高 |
| 2 | デバイス名取得 | `GetDeviceName()` / `CameraControllerGetDeviceName()` — デバイスインデックスからデバイス名(wchar_t)を取得 | 高 |
| 3 | カメラ接続 | `ConnectDevice(index)` / `ConnectDevice(name)` — インデックスまたはデバイス名で接続 | 高 |
| 4 | カメラ切断 | `DisconnectDevice()` — 接続中カメラを切断 | 高 |
| 5 | 静止画取得 | `GetImage()` — 撮影を実行し、JPEG/RAW画像バイナリとサイズを返す | 高 |
| 6 | ライブビュー取得 | `GetLiveImage()` — ライブビューJPEGバイナリとサイズを返す | 高 |
| 7 | 画像サイズ設定 | `SetImageSize(S/M/L)` — 画像サイズをS/M/Lの3段階で設定 | 高 |
| 8 | 画像サイズ取得 | `GetImageSize(width, height)` — 現在の画像サイズ（ピクセル）を取得 | 高 |
| 9 | 圧縮形式設定 | `SetCompressionSetting()` — ECO/STD/FINE/XFINE/RAW/RAW+JPG/RAWC/RAWC+JPGを設定 | 高 |
| 10 | 圧縮形式取得 | `GetCompressionSetting()` — 現在の圧縮形式を取得 | 高 |
| 11 | F値取得 | `GetFNumber()` — 現在のF値を文字列で取得 | 高 |
| 12 | F値設定 | `SetFNumber()` — F値を文字列で設定（FNumberTable定義値） | 高 |
| 13 | F値ステップ変更 | `ChangeFNumber(step)` — 現在値から相対ステップでF値を変更 | 高 |
| 14 | シャッター速度取得 | `GetShutterSpeed()` — 現在のシャッター速度を文字列で取得 | 高 |
| 15 | シャッター速度設定 | `SetShutterSpeed()` — シャッター速度を文字列で設定 | 高 |
| 16 | シャッター速度ステップ変更 | `ChangeShutterSpeed(step)` — ステップ指定でシャッター速度を変更 | 高 |
| 17 | ISO感度取得 | `GetISO()` — 現在のISO感度を文字列で取得 | 高 |
| 18 | ISO感度設定 | `SetISO()` — ISO感度を文字列で設定 | 高 |
| 19 | ISO感度ステップ変更 | `ChangeISO(step)` — ステップ指定でISO感度を変更 | 高 |
| 20 | ホワイトバランス取得 | `GetWhiteBalance()` — 現在のホワイトバランスを文字列で取得 | 高 |
| 21 | ホワイトバランス設定 | `SetWhiteBalance()` — ホワイトバランスを文字列で設定 | 高 |
| 22 | ホワイトバランスステップ変更 | `ChangeWhiteBalance(step)` — ステップ指定でホワイトバランスを変更 | 高 |
| 23 | フォーカスモード取得 | `GetFocusMode()` — 現在のフォーカスモードを文字列で取得 | 高 |
| 24 | フォーカスモード設定 | `SetFocusMode()` — フォーカスモードを文字列で設定 | 高 |
| 25 | AF/MFホールド | `SetAfMfHold(ButtonStatus)` — AF/MFホールドボタンのUp/Down状態を送信 | 高 |
| 26 | ニアファー制御 | `ChangeNearFar(step)` — マニュアルフォーカスのニア/ファー方向をステップ指定で移動 | 中 |
| 27 | シングルAF実行 | `AutoFocusSingle()` — 1ショットオートフォーカスを実行 | 中 |
| 28 | フォーカスエリア取得 | `GetFocusArea()` — 現在のフォーカスエリア種別を文字列で取得 | 中 |
| 29 | フォーカスエリア設定 | `SetFocusArea()` — フォーカスエリア種別を文字列で設定 | 中 |
| 30 | AFエリア位置取得 | `GetAfAreaPosition(x, y)` — AFエリアのX/Y座標を取得 | 中 |
| 31 | AFエリア位置設定 | `SetAfAreaPosition(x, y)` — AFエリアのX/Y座標を設定 | 中 |
| 32 | インスタンス生成 | `CreateCameraControllerInstance()` — CameraControllerAlphaインスタンスをHCAMERAハンドルとして生成 | 高 |
| 33 | インスタンス破棄 | `ReleaseCameraControllerInstance(handle)` — インスタンスを破棄しハンドルをNULLに設定 | 高 |

---

### 2-5. データ要件

| データ名 | 主要項目 | 関連データ | 備考 |
|----------|----------|-----------|------|
| 画像バイナリ | バイナリデータ(BYTE*), サイズ(ULONG) | GetImage / GetLiveImage | バッファ確保・解放は呼び出し側の責務 |
| デバイス情報 | インデックス(DWORD), フレンドリ名(PWSTR), メーカー(PWSTR) | DeviceInfo 構造体 | EnumerateDevices で内部に保持 |
| F値パラメータ | 文字列名(char*), コマンド値(UINT16), 実数値(float) | FNumberTable | F1.0〜F90 の全段階定義 |
| シャッター速度パラメータ | 文字列名(char*), コマンド値(UINT32), 秒数(float) | ShutterSpeedTable | BULB〜1/64000 の全段階定義 |
| ISO感度パラメータ | 文字列名(char*), コマンド値(UINT32), 数値(int) | ISOTable | ISO25〜ISO819200、AUTO、マルチフレームNR対応 |
| ホワイトバランスパラメータ | 文字列名(char*), コマンド値(UINT16), 数値(int) | WhiteBalanceTable | 色温度（K値）で管理 |
| フォーカスモードパラメータ | 文字列名(char*), コマンド値(UINT16), 数値(int) | FocusModeTable | MF / AF-S / AF-C 等 |
| フォーカスエリアパラメータ | 文字列名(char*), コマンド値(UINT16), 数値(int) | FocusAreaTable | ワイド / ゾーン / スポット 等 |
| モデル別パラメータ | モデル名(char[32]), 画像サイズ(S/M/L), 各種インデックス範囲 | ParameterSetCamera | ILCE-7RM2 / 7R / 6000 / 5000 / 6400 に対応 |

---

### 2-6. 関連システムインタフェース要件

| 連携先システム | インタフェース種別 | データ内容 | 頻度 |
|--------------|-----------------|-----------|------|
| 呼び出し側アプリケーション | 静的リンク（.lib） / C WINAPI 関数 | ハンドル(HCAMERA), 画像バッファポインタ, パラメータ文字列 | アプリ制御に応じて随時 |
| Windows Portable Devices API | COM/WPD API (`IPortableDevice`, `IPortableDeviceValues` 等) | PTPプロパティコード, デバイスプロパティ値, 画像オブジェクト | 操作都度 |
| Sony カメラ（USB PTP） | USB 2.0 / PTP-IP プロトコル | DevicePropertyCode, 画像JPEG/RAWバイナリ | 非同期イベント含む |
| IPortableDeviceEventCallback | COMイベントコールバック | 画像転送完了イベント (TransferFlag) | 撮影完了時 |

---

### 2-7. 要件定義不要機能

| 機能名 | 不要となる理由 |
|--------|--------------|
| ネットワーク（Wi-Fi/Bluetooth）接続 | USB有線接続のみを対象とし、ワイヤレス接続は対象外 |
| 動画録画制御 | 静止画および静止画転用のライブビュー取得のみが対象 |
| 複数カメラの同時制御 | 1インスタンスにつき1台の接続を前提とする |
| カメラ設定の永続保存 | 設定の保存・復元は呼び出し側アプリケーションの責務 |
| 露出プログラムモード（P/A/S/M）切り替え | PTPで取得可能だが本ライブラリの公開APIには含めない |

---

### 2-8. システム構築の制約

| 制約種別 | 内容 |
|----------|------|
| OS制約 | Windows 環境限定（WPD APIの都合）。Linux / macOS は対象外 |
| コンパイラ制約 | Visual Studio（MSVC）によるネイティブC++ビルドを前提とする |
| アーキテクチャ | x64 / x86 の両プラットフォームでビルド可能であること |
| 依存ライブラリ | PortableDeviceApi.lib, Ole32.lib（WPD/COM依存） |
| 対応カメラ | Sony α シリーズ（ILCE系）のみ。ParameterSetCameraへの追加でモデル拡張可能 |
| スレッド安全性 | マルチスレッドからの同時呼び出しは想定しない |

---

## 3. システム要件（非機能要件）

### 3-1. 移行要件

新規開発のため該当せず。ただし新カメラモデル追加時は `CameraControllerAlphaParameterModelTable.h` への `ParameterSetCamera` 定義の追加、および`CameraControllerAlpha.cpp` での分岐拡張が必要。

---

### 3-2. 品質要件

| 品質特性 | 要件内容 | 指標・目標値 |
|----------|----------|------------|
| 信頼性 | カメラ未接続・WPD APIエラー時にfalse/0を返し、例外を外部へ漏らさない | APIが例外なく戻り値でエラーを通知できること |
| 保守性 | パラメータテーブルと変換ロジックの分離、モデル差異の局所化 | 新カメラモデル追加时の修正ファイル数 2以下 |
| 正確性 | PTPコマンド値と文字列名の対応が正確であること | FNumberTable / ShutterSpeedTable 等の変換誤りゼロ |
| 機能性 | 2-4 で定義した全33機能が実装されていること | API仕様に基づくテスト全項目合格 |
| 生産性 | シンプルなCスタイルAPIにより、呼び出し側の実装コストを低減 | HCAMERA ハンドルを使った10行以内のコードで撮影可能 |
| 操作性 | 文字列ベースのパラメータ指定により、コマンド値を意識せずに操作可能 | パラメータ設定にコマンド値の知識が不要であること |
| 経済性 | 純粋仮想インタフェース（CameraController基底クラス）によるモデル拡張コストの最小化 | 新モデル対応に伴う既存コード変更なし |

---

### 3-3. 性能要件

| 項目 | 要件内容 | 目標値 |
|------|----------|--------|
| カメラ接続時間 | ConnectDevice呼び出しから接続確立まで | 2秒以内 |
| 静止画取得時間 | GetImage（is_wait=true）の最大待機時間 | 4秒以内（シャッター待機2秒 + 転送待機2秒） |
| ライブビュー取得時間 | GetLiveImage の応答時間 | 250ms以内 |
| パラメータ設定応答時間 | SetFNumber 等のWPDコマンド発行から完了まで | 50ms以内 |
| デバイス列挙時間 | EnumerateDevices の完了時間 | 1秒以内 |

---

### 3-4. システムマネジメント要件

| 項目 | 要件内容 |
|------|----------|
| 監視 | ライブラリ単体に監視機能は持たない。エラーは戻り値（bool / int）で呼び出し側へ通知 |
| バックアップ・リストア | 対象外（ライブラリに状態保存機能なし） |
| 障害対応 | WPD APIエラー時はfalseを返して呼び出し側に処理を委ねる。カメラ抜去時はDisconnectDevice呼び出しで状態リセット |
| 自動化（RBA） | 対象外 |

---

### 3-5. インフラストラクチャー要件

| 項目 | 要件内容 |
|------|----------|
| サーバ | 不要（ローカルPCで動作する静的ライブラリ） |
| ネットワーク | 不要（USB接続のみ） |
| ストレージ | ライブラリ本体（.lib）の配置先のみ必要 |
| セキュリティ | USB接続デバイスへのアクセスはOS標準のデバイス許可に準拠 |
| カメラ接続 | USB 2.0 以上での有線接続 |
| 可用性・冗長化 | 単一デバイス1接続構成。冗長化は対象外 |

---

## 4. 次工程以降への申し送り事項

| No. | 申し送り内容 | 担当者 | 期限 | 備考 |
|-----|------------|--------|------|------|
| 1 | 新モデル（ILCE-7RM4 等）追加時のParameterSetCamera定義確認・追加 | | | CameraControllerAlphaParameterModelTable.h を修正 |
| 2 | CameraControllerApi.hpp に未公開の内部機能（GetImageAspectRatio / ChangeExposureBiasCompensation 等）の公開要否判断 | | | CameraControllerAlphaCore.h に定義済み |
| 3 | マルチスレッド環境での利用ニーズが発生した場合のスレッドセーフ化対応 | | | 現状は非対応 |

---

## 変更履歴

| バージョン | 変更日 | 変更者 | 変更内容 |
|-----------|--------|--------|----------|
| 1.0 | 2026年4月15日 | システム分析チーム | 初版作成 |
