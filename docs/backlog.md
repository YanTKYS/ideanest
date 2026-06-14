# バックログ

実装着手前に再判断します。優先度はあくまで現時点の目安です。

### 優先度の定義

- **A**：日常操作の体感改善が大きい・既存設計と相性が良い・WPF 標準の範囲内
- **B**：有益だが影響範囲・設計コストが中程度
- **C**：長期的に有望だが現時点では慎重に扱う

### スコープ区分 (v0.9.0 で導入 / v1.0.0 で整理)

- **完了済み**: 実装・リリース済みの項目
- **v1.0.0 以降に検討**: 正式リリース後に再判断する候補
- **当面見送り**: 価値はあるが現時点では工数 / リスクが見合わない項目
- **対象外**: 設計方針として原則実装しない項目

---

## 完了済み

過去のマイルストーンで対応済み。歴史的経緯は `docs/release-notes.md` を参照。

| No | 項目 | 完了バージョン |
| --- | --- | --- |
| M12 | MainViewModel の段階的分割 | v0.8.1〜v0.8.9 (`CardDisplayViewModel` / `FilterViewModel` / `TagPanelViewModel` / `ExportViewModel+IExportPlatform` / `RecentFilesService+StartupCoordinator+StartupViewModel` / `SaveStateViewModel` / `CardOperationsService+TagSyncService` / `TagManagementService` を抽出。WPF 非依存の純粋ロジックは出尽くしたためここで一区切り。詳細方針は `design-decisions.md` 参照) |
| R1 | v1.0.0 リリース準備 | v1.0.0 (csproj `<Version>` 1.0.0 化、README / docs / release-notes を v1.0.0 時点に整理、`docs/test-scenarios.md` に Windows ビルド向けリリース前チェックリスト R1〜R12 を追加。`Assets/tutorial.png` の同梱状況など、本リポジトリの範囲外の確認項目は test-scenarios と release-notes で明示) |
| U1 | タグパネルの視認性改善 | v1.0.1 (`MainWindow.xaml` のタグパネル `Border` に `SurfaceBrush` 背景と 1px の右側区切り線を追加。XAML のみの変更でレイアウトに影響なし) |
| U2 | テキスト貼り付け / `.txt` ファイル D&D で新規カード作成 | v1.0.2 (`CardOperationsService` に `CommitAddFromText` / `CommitAddFromFileContent` を追加し、`MainViewModel` の `PasteAsNewCard` / `CreateCardsFromFiles` から呼び出し。Ctrl+V はテキスト入力中を除く一覧領域フォーカス時のみ。`.txt` を UTF-8 として読み込み、複数ファイル D&D も対応。読込失敗は警告ダイアログでまとめて通知) |
| M14 | `*Service` クラスのフォルダ整理 | v1.0.3 (`CardOperationsService` / `TagSyncService` / `TagManagementService` を `ViewModels/` から `Services/` へ移動し namespace を `IdeaNest.Services` に統一。テストプロジェクトの `<Compile Include>` パスと 3 件のテストファイルの using も更新。対応するテストファイル 3 件も `tests/IdeaNest.Tests/ViewModels/` から `tests/IdeaNest.Tests/Services/` へ移動し namespace を `IdeaNest.Tests.Services` に統一。動作変更なし) |

---

## v1.0.0 以降に検討

### 低難易度

既存 UI・データ構造への影響が小さく、比較的短期間で実装可能。

| No | 項目 | 概要 | 優先度 |
| --- | --- | --- | --- |
| L2 | 今日 / 直近 N 日クイックフィルタ | 検索バー付近にクイックフィルタを追加し、「今日更新」「今週更新」のカードだけ即時絞り込める。`RefreshVisible` のフィルタ条件を 1 つ増やすだけで実現できる | A |
| L3 | カードの複製 | 右クリックメニュー + `Ctrl+D` でカードを複製して追加。テンプレート的なカードを量産するユースケースに有効 | B |
| L4 | タグの色付け | タグに対してパステル色を関連付け、左ペインのタグ一覧とカードのタグチップに反映する。タグが増えた際の視認性向上。保存は `WorkspaceSettings.TagColors` (Dictionary) として追加 | B |
| L7 | タイトルバーのフルパス ToolTip | タイトルバーのファイル名にカーソルを当てると、ToolTip でフルパスを表示する。「どのファイルを開いているか」を確認しやすくする | B |
| L8 | 自動保存間隔の設定 | デバウンス時間を 1 / 2 / 5 秒から選択できるようにする。v0.4.0 では 2 秒固定 | B |
| L9 | ハイフン / 全半角タグ正規化 | `UI` と `ＵＩ`、`to-do` と `todo` を同一視する正規化。v0.2.0 の基本正規化 (前後空白・先頭 `#`・重複) を拡張する | C |

### 中難易度

既存 UI・サービス層への変更を伴うが、新たな外部依存を増やさない。

| No | 項目 | 概要 | 優先度 |
| --- | --- | --- | --- |
| M1 | キーボードによるカード選択・操作 | `Tab` / 矢印キーでカードにフォーカスを移動し、`Enter` でプレビュー、`E` で編集、`Del` で削除、`Ctrl+P` でピン留め切替。「選択中カード」という UI 概念を新たに設計する必要がある | A |
| M2 | 検索のあいまい一致 | ひらがな / カタカナ / 全半角を同一視する正規化オプションを追加する。現在は `OrdinalIgnoreCase` で完全一致のみ対応。日本語メモが多い環境で検索ヒット率が上がる | A |
| M3 | タグ入力のオートコンプリート | 編集ダイアログのタグ入力欄で、既存タグの補完候補をポップアップ表示する。WPF 標準 TextBox での候補リスト表示には追加の UI コンポーネントが必要 | B |
| M4 | 複数色の同時選択フィルタ | 色チップを複数選択して OR 条件で絞り込めるようにする。現状は 1 色のみ。`SelectedColor` (string) を `HashSet<string>` に変えることで対応できるが、保存形式とフィルタ UI に影響する | B |
| M5 | 手動並べ替え (ドラッグ) | カードを D&D で並べ替えて `manualOrder` フィールドに保存する。ピン留め・並び順切替との優先順位設計が必要。`design-decisions.md` v0.5.2 の判断を再検討する前提で着手する | B |
| M6 | `.ideanest` スキーマバージョン管理 | ファイルに `schemaVersion` フィールドを追加し、旧バージョンの読み込み時にマイグレーション処理を走らせる。破壊的変更を安全に導入するための基盤 | B |
| M7 | 世代バックアップ | `.bak` を直近 3 世代保持する (`.bak` / `.bak2` / `.bak3`)。現状は 1 世代のみで、短時間に複数回保存すると前世代が即座に上書きされる | B |
| M8 | 出力オプションのプレビュー | NoteNest 向けエクスポート / コピーのオプションダイアログで、選択結果の冒頭数行をプレビュー表示する。v0.3.1 では未実装 | C |
| M9 | 重複アイデア検出 | タイトル・本文の類似度 (n-gram など) で近しいカードを候補として提示する。Duplicate フラグの設計や、どの時点で検出を走らせるかの判断が必要 | C |
| M10 | 出力マーカー文言のカスタマイズ | NoteNest 向けエクスポートの `[NOTE]` / `[TODO]` 文言を自由入力で変更できるようにする。v0.3.1 では固定文字列 | C |
| M13 | カード一覧の Masonry レイアウト | v0.7.3 の `VerticalAlignment="Top"` 修正後も、行内では各行の最も高いカード分のスペースが確保され、短文カードの直下に余白が残る。Google Keep のように次行のカードを詰めて配置するにはカスタム `Panel` (列ごとに最短列に追加するアルゴリズム) の実装が必要。`WrapPanel` を置き換えるため影響範囲が広く、優先度は中 | B |

### 高難易度

既存設計に大きく影響するため、長期的な検討が必要。

| No | 項目 | 概要 | 優先度 |
| --- | --- | --- | --- |
| H1 | ダークモード | `App.xaml` の ResourceDictionary をテーマ別 (ライト / ダーク) に切り替える仕組みが必要。すべてのブラシ・スタイルの二重化を伴う | B |
| H2 | ユーザー定義カラーパレット | パステル 8 色を超えるカラースキーマ拡張。`settings.json` と `WorkspaceSettings` の両方に影響し、色フィルタ UI・エクスポートの色名処理も再設計が必要 | C |
| H3 | 新規未保存状態の一時ファイル自動保存 | `%TEMP%` に自動保存し、起動時に未保存セッションを復元する。「どこに保存するか」をアプリが決める方針転換が必要。v0.4.0 では意図的に対象外とした | C |
| H4 | NoteNest 側インポート機能 | IdeaNest → NoteNest への移行を NoteNest 側のインポートコマンドとして実装する。NoteNest リポジトリの改修が必要で、IdeaNest 単体では対応できない | C |
| H5 | DIコンテナ導入 | `Microsoft.Extensions.DependencyInjection` を導入し、Service / ViewModel の生成と依存関係を整理する。現時点では過剰化を避けるため長期検討 | C |

---

## 対象外・当面見送り

IdeaNest の設計方針から意図的に除外しているもの。要望があっても原則実装しない。

| 機能 | 理由 |
| --- | --- |
| 右ペイン常設プレビュー | v0.6.0 でモーダル方式を採用。「一覧を主役にする」コンセプトと常設右ペインは矛盾し、カードサイズ切替・タグパネルとの横幅競合も大きい |
| Markdown / リッチテキスト表示 | WPF 標準 TextBox の範囲を超える。外部ライブラリを引き込まないという設計方針と相容れない |
| 画像貼り付け | 単一 JSON ファイル設計と根本的に矛盾する。Base64 埋め込みはファイルサイズを爆発させ、外部フォルダ参照は「1 ファイルで持ち運べる」を崩す |
| 添付ファイル | 画像貼り付けと同様の理由 |
| 色表示名のカスタマイズ | 英語キー (`yellow` など) と表示名の対応を永続化すると、エクスポート・フィルタ復元の複雑度が増す割にメリットが小さい |
| クラウド同期 | ローカル利用前提の方針。OneDrive 等のフォルダへ手動配置することで代替可能 |
| 共有編集 | ローカル単一ファイル管理の方針と根本的に合わない。排他制御・マージ処理が複雑 |
| コメント機能 | カードに「会話」を持ち込むと NoteNest と役割が重複する。IdeaNest は「個人の思考在庫」に留まる |

## AppShell / WorkspaceView 分離ロードマップ

- **v1.1.x 系**: AppShell / WorkspaceView の段階的分離を継続し、外枠制御と作業状態の境界を明確化する。
- **将来**: NestSuite タブ内での `IdeaNestWorkspaceView` 表示を検証する。
- **将来**: 複数 Nest アプリで必要性が確認できた時点で共通 Workspace 契約を検討する。
- **当面見送り**: NestSuite 本体接続、共通ライブラリ化、`.ideanest` 保存形式変更。


### v1.1.x 境界整理の次候補

- **次候補**: AppShell / WorkspaceView 境界のWindows実機回帰確認。
- **将来**: NestSuiteタブ内表示検証、共通Workspace契約の検討。
- **当面見送り**: NestSuite本体接続、共通ライブラリ化、保存形式変更。


### v1.1.x ホスト再利用準備の次候補

- **v1.1.x 次候補**: NestSuite試験配置の前提整理。
- **将来**: NestSuiteタブ内での `IdeaNestWorkspaceView` 表示検証、共通Workspace契約の検討。
- **当面見送り**: NestSuite本体接続、共通ライブラリ化、保存形式変更。
