# NestSuite試験配置 readiness (v1.1.4)

## 目的

NestSuite本体へ接続する前に、IdeaNest内でホスト表示時の挙動と必要な境界を確認するための手順書です。

## 確認方法

IdeaNestを通常起動し、**ヘルプ → WorkspaceViewホスト表示テスト**を開きます。`WorkspaceHostPreviewWindow` は `IdeaNestWorkspaceView` を `ShowMenu="False"`、AppShellコマンド未設定、新規Workspace相当で表示します。

## Preview Windowで確認できること

- MenuがHiddenで、メニュー跡の不要な領域が残らないこと。
- dirty状態、全カード件数、表示中カード件数が上部に反映されること。
- カード追加・編集・削除、ピン留め、アーカイブ、プレビュー。
- 検索、タグ・色フィルタ、タグパネル、タグ管理、並び順。
- テキスト貼り付け、テキストファイルD&D。
- カード編集・プレビュー・タグ管理・NoteNest向け出力のOwnerがPreview Windowになること。

## IdeaNest Workspace側が提供できるもの

- `DisplayName`、`TotalCount`、`VisibleCardCount`、`CurrentFilterContext`。
- `DirtyRequested`、`LoadFromWorkspace()`、`BuildWorkspaceForSave()`。
- `IdeaNestWorkspaceView.FocusWorkspace()` と、必要に応じた `IdeaNestWorkspaceHostCommands`。

## NestSuite試験配置時にホスト側が担当すること

- 保存・名前を付けて保存・自動保存と保存先管理。
- dirty状態の保持、未保存確認、タブタイトル、タブクローズ制御。
- 最近使ったファイルと複数Workspace同時表示時の保存状態管理。
- WorkspaceViewを配置したWindowをダイアログOwnerとして解決できる状態の維持。

## 未対応・注意点

NestSuite本体接続、NestSuiteタブ内での実表示、共通 `IWorkspace` 契約、共通ライブラリ化、保存形式変更、本格統合は未対応です。Preview Windowは検証専用で、ファイル保存・自動保存・最近使ったファイルには対応しません。
