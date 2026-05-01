# GapCamera 詳細設計書 差分整理（ColorAlignmentSoftware_Nice）

## 位置付け

このフォルダは、docs/02-02.GapCamera 配下の既存文書と、..\\ColorAlignmentSoftware_Nice の実装との差分を、文書単位で整理したものです。

## 全体要約

| 観点 | 差分概要 | 文書影響 |
|------|----------|----------|
| 条件コンパイル | Nice 実装では ForNice, BulkCorrectValueWall, Auto_WriteData, Spec_by_Zdistance などの分岐が増えている | 4章、7章、8章で前提条件の見直しが必要 |
| Controller 連携 | SDCP に加えて ADCP JSON による一括取得・一括設定がある | 2章、7章、8-3 へ追記が必要 |
| 機種対応 | VP15EB/EM, VP23EB/EM, VS25FM を含む機種別 geometry 分岐がある | 1章、4章、5章、8章で機種差分を補強する必要がある |
| 計測条件 | Controller ごとの gamma 値取得、撮影距離に応じた露出補正、moire 判定が追加されている | 要件定義、基本設計、8-2、8-4 の追記が必要 |

## 主な参照実装

- CAS/Functions/GapCamera.cs
- CAS/Functions/TransformImage.cs
- CAS/Functions/EstimateCameraPos.cs
- CAS/SDCPClass.cs
- CameraDataClass/CameraDataClass.cs

## 使い方

- 各ファイルは「要約」「差分一覧」「更新方針」の順で整理している。
- 元文書を改訂する際は、同名ファイルの差分一覧を先に確認する。
- 実装追従の優先度が高いのは 7章と 8章である。

## 収録範囲

- GapCamera_要件定義書.md
- GapCamera_基本設計書.md
- GapCamera_詳細設計書/ 以下の章別差分整理
