# NoteNest / NestSuite 連携準備

IdeaNest は、将来 NestSuite のタブ内で `IdeaNestWorkspaceView` を表示できるよう、AppShell と作業画面の分離を段階的に進めています。v1.1.3 時点では NoteNest や NestSuite 本体には接続せず、IdeaNest 内の `WorkspaceHostPreviewWindow` でのみホスト表示を検証します。

## v1.1.3 で確認できること

- WorkspaceViewを `ShowMenu="False"` で表示できる。
- AppShellコマンドを渡さない新規Workspace相当でも、カード操作・検索・タグ・並び順を確認できる。
- Workspaceのdirty通知をホストが受け取れる。
- ダイアログOwnerはWorkspaceViewが配置されたホストWindowを基準に解決される。

## 今後ホスト側で設計が必要なこと

- `.ideanest` の保存・自動保存と保存先管理。
- 未保存状態、タブタイトル、タブを閉じる際の確認。
- 最近使ったファイルやアプリ終了など、単体AppShell固有機能の扱い。

保存形式の変更、共通ライブラリ化、汎用 `IWorkspace` 契約、NestSuite本体接続はv1.1.3の対象外です。
