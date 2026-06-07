# リリースノート

## v0.8.7 (リファクタ後回帰確認・小修正) — 2026-06-07

v0.8.1〜v0.8.6 の MainViewModel 段階的分割を受けて、**新機能の追加や大規模な設計変更は行わず**、
回帰確認と軽微な不整合の修正のみを実施しました。

### 確認結果

以下を目視・コードレビューで確認し、回帰がないことを検証しました。

- **起動導線**: 通常起動 (スタートダイアログ) / ダブルクリック起動 / コマンドライン引数起動の
  いずれも `StartupCoordinator` / `StartupViewModel` 経由で旧挙動が維持されている
- **保存・自動保存**: `SaveStateViewModel` への移譲後も、手動保存・名前を付けて保存・
  自動保存・保存失敗時の状態遷移・終了時確認が正しく機能している
- **カード操作**: 追加・編集・削除・プレビュー・ピン留め・アーカイブ・前後移動が正常
- **フィルタ・タグパネル**: 検索・タグ・色フィルタ・アーカイブ表示・空状態表示・
  タグパネル開閉・タグ検索が正常
- **並び順・シャッフル**: 各並び順・再シャッフル・ピン留め上部固定が正常
- **エクスポート・コピー**: Markdown / NoteNest 向けの出力形式が変わっていない
- **その他**: チュートリアル画面・バージョン表示・ドキュメント記載が整合している

### 修正内容

- **`MainViewModel.SaveTo` の catch ブロックの不要な通知を削除**
  - 手動保存が失敗した際、`OnPropertyChanged(nameof(SaveStatusText))` を呼んでいたが、
    `SaveState` の状態は変化しておらず `SaveStatusText` は変化しない。コメントには
    「SaveStatusText は手動保存失敗時に変化しない」と明記されており、コードと矛盾していた
  - 当該 `OnPropertyChanged` 呼び出しを削除。MessageBox 表示のみで対応する従来の意図を明確化

- **リリースノート v0.8.4 のテスト件数を修正**
  - "22件" と記載していた `ExportViewModelTests` のテスト数が、
    v0.8.4 レビュー時のスナップショットテスト分割 (1 → 2 件) により実際は 23 件になっていた

### 変更なし

- 保存ファイル形式 (`.ideanest`) / `settings.json` 形式 / XAML / メニュー構成 /
  キーボードショートカット / 自動保存挙動 / エクスポート出力形式 — **すべて変更なし**
- 新しい ViewModel の追加・MainViewModel の大規模分割 — **実施しない**
- 単体テストの追加 — **なし** (全 260 件引き続き Pass)

### 影響範囲

- `src/IdeaNest/ViewModels/MainViewModel.cs`
  (SaveTo catch ブロックの冗長 `OnPropertyChanged` を削除)
- `docs/release-notes.md`
  (v0.8.4 テスト件数を 22 → 23 に訂正)
- `src/IdeaNest/IdeaNest.csproj` の `<Version>` を `0.8.6` → `0.8.7` に更新

---

## v0.8.6 (保存状態分割：SaveStateViewModel 抽出) — 2026-06-07

`MainViewModel` に残っていた保存状態・自動保存まわりの責務を `SaveStateViewModel` に切り出しました。
v0.8.1 〜 v0.8.5 と同様、XAML・既存挙動・保存形式は変更なし。

> 追補 (2026-06-07): `CanScheduleAutoSave` の依存状態 (`CurrentFilePath` / `_isAutoSaving`) が
> 変化する箇所で `PropertyChanged` が発火するように修正。`SaveStateViewModel` は `ViewModelBase`
> を継承し公開状態として設計されているため、将来 UI バインディング/コマンドの `CanExecute` から
> 参照したときに通知漏れを起こさないようにする保険。テストも 7 件追加。

### 変更内容

- **`SaveStateViewModel` を新規追加** (`ViewModels/SaveStateViewModel.cs`)
  - 担当: ファイルパス管理 (`CurrentFilePath`)、未保存変更フラグ (`IsDirty`)、
    自動保存スケジューリング判定 (`CanScheduleAutoSave`)、保存ステータス文言 (`SaveStatusText`)
  - 遷移メソッド: `MarkDirty` / `OnFileLoaded` / `OnManualSaveSuccess` / `Reset` /
    `OnAutoSaveBegin` / `OnAutoSaveSuccess` / `OnAutoSaveFail`
  - `Func<DateTime>` を差し込むことで `DateTime.Now` も置換可能 — テストでの時刻固定に使用
  - WPF 非依存。`IdeaNest.Tests` でクロスプラットフォームに単体テストできる

- **`MainViewModel` を更新**
  - `_currentFilePath` / `_isDirty` / `_isAutoSaving` / `_autoSaveFailed` /
    `_lastAutoSaveTime` の各フィールドを削除し、`SaveState` サブ ViewModel に移動
  - `DispatcherTimer` (WPF 依存) は引き続き `MainViewModel` に保持
  - `CurrentFilePath` / `IsDirty` / `SaveStatusText` はすべて `SaveState` への転送プロパティ
  - `Title` は `SaveState.CurrentFilePath` / `SaveState.IsDirty` から構築
  - `SaveState.PropertyChanged` を購読し、`CurrentFilePath` / `IsDirty` 変化時は
    `Title` も再通知 (既存 XAML バインディングへの影響ゼロ)
  - `NewWorkspace` / `Open` / `SaveTo` / `LoadStartup` は各遷移メソッドを呼び出す形に置換
  - `MarkDirty()` / `ScheduleAutoSave()` / `PerformAutoSave()` は `SaveState` 経由に変更
  - `ResetAutoSaveState()` プライベートメソッドを削除 (`SaveState.Reset()` / `OnFileLoaded()` に吸収)

- **`IdeaNest.Tests.csproj`** に `SaveStateViewModel.cs` の `<Compile Include>` を追加

- **新規テスト 36 件** (`SaveStateViewModelTests.cs`):
  初期状態・`MarkDirty`・`OnFileLoaded`・`OnManualSaveSuccess`・`Reset`・
  自動保存ライフサイクル (`OnAutoSaveBegin` / `OnAutoSaveSuccess` / `OnAutoSaveFail`)・
  終了時確認フラグ (`IsDirty`) の各遷移、および `CanScheduleAutoSave` の
  `PropertyChanged` 発火タイミング (依存状態変化時に通知、無関係な変化では非通知) を網羅

### 影響範囲

- `src/IdeaNest/ViewModels/SaveStateViewModel.cs` (新規)
- `src/IdeaNest/ViewModels/MainViewModel.cs` (内部リファクタリングのみ、公開コマンド・プロパティの挙動不変)
- `tests/IdeaNest.Tests/IdeaNest.Tests.csproj` / `ViewModels/SaveStateViewModelTests.cs` (新規)
- `src/IdeaNest/IdeaNest.csproj` の `<Version>` を `0.8.5` → `0.8.6` に更新
- 保存ファイル形式 / `settings.json` 形式 / XAML / メニュー構成 / 自動保存間隔 /
  保存挙動 / タイトルバー表示 — **変更なし**

---

## v0.8.5 (起動導線分割：RecentFilesService / StartupCoordinator / StartupViewModel 抽出) — 2026-06-07

起動時の責務 (コマンドライン引数の解決・最近使ったファイル一覧・スタートダイアログ) を
ロジックとUIに分離しました。
v0.8.1 〜 v0.8.4 と同様、XAML・既存挙動・保存形式は変更なし。

### 変更内容

- **`RecentFilesService` を新規追加** (`Services/RecentFilesService.cs`)
  - 担当: 最近使ったファイル一覧に対する純粋な追加 (`Add`) / 削除 (`Remove`) /
    存在ファイルのフィルタ (`FilterExisting`)
  - `MaxRecentFiles = 5` を定数として保持
  - 永続化 (`settings.json`) は従来どおり `AppSettingsService` 側、本クラスは
    純粋なリスト操作のみで、`Func<string, bool>` 注入により I/O 不要で単体テスト可能

- **`StartupCoordinator` を新規追加** (`Services/StartupCoordinator.cs`)
  - 担当: 起動引数の解析 → `StartupAction` (`DirectOpen` / `ShowDialog`) の決定
  - 旧 `App.OnStartup` と同一の判定ルールを維持: `args[0]` のみを対象とし、
    存在するファイルであれば拡張子に関係なく `DirectOpen` とする。後続引数は無視
  - 完全に純粋関数で、`Func<string, bool>` を差し込むことで存在チェックも差し替え可能

- **`StartupViewModel` を新規追加** (`ViewModels/StartupViewModel.cs`)
  - 担当: スタートダイアログの状態 (`Items` / `Choice` / `SelectedPath`)、
    `ChooseNew` / `Cancel` / `TryChooseOpen` / `RemoveItem` / `ClearItems`
  - `RecentFileItem` (FullPath / DisplayName) もここに移動
  - 構築時に `RecentFilesService.FilterExisting` を通して存在しない履歴を除外
  - WPF 非依存。`IdeaNest.Tests` でクロスプラットフォームに単体テストできる

- **`AppSettingsService` を更新**
  - `AddRecentFile` / `RemoveRecentFile` の内部実装を `RecentFilesService` に委譲
    (公開 API は不変、他の呼出側に変更なし)
  - `MaxRecentFiles` 定数は `RecentFilesService` に移動

- **`App.xaml.cs` (`App.OnStartup`) を更新**
  - 引数解決を `StartupCoordinator.Resolve` に委譲
  - スタートダイアログ周りを `StartupViewModel` 経由に整理
  - 既存挙動 (引数オープン → 最近使ったファイル追加、不正ファイル時の MessageBox、
    ダイアログのキャンセル → 終了、新規開始 → 空ワークスペース) は維持
  - `StartupCoordinator.Resolve` は旧 `App.OnStartup` と同一の判定ルールを採用:
    `args[0]` のみを対象とし、拡張子フィルタなし

- **`Views/StartupWindow.xaml.cs` を更新**
  - コンストラクタを `StartupWindow(StartupViewModel vm)` に変更
  - 公開プロパティ (`ChoseNew` / `SelectedPath`) を削除し、結果は VM 経由で参照
  - UI/XAML は変更なし

- **`IdeaNest.Tests.csproj`** に `RecentFilesService.cs` / `StartupCoordinator.cs` /
  `StartupViewModel.cs` の `<Compile Include>` を追加

- **新規テスト 35 件**:
  - `RecentFilesServiceTests.cs` (14 件):
    `Add` の重複排除・先頭移動・大文字小文字不区別・5 件上限・空入力スキップ、
    `Remove` の存在エントリ削除・大文字小文字不区別・該当なし時の不変、
    `FilterExisting` の存在チェック委譲と空白/空文字列除外
  - `StartupCoordinatorTests.cs` (9 件):
    引数なし / 空白のみ → `ShowDialog`、存在する先頭引数 → `DirectOpen` (拡張子問わず)、
    存在しない先頭引数 → `ShowDialog`、複数引数時は先頭のみ判定・後続は無視、
    `null` 引数の例外
  - `StartupViewModelTests.cs` (12 件):
    存在ファイルのみ `Items` に積まれる、初期 `Choice` は `Cancel`、
    `ChooseNew` / `Cancel` / `TryChooseOpen` (成功/失敗) / `RemoveItem` / `ClearItems`、
    `null` リストの例外

### 影響範囲

- `src/IdeaNest/Services/RecentFilesService.cs` (新規)
- `src/IdeaNest/Services/StartupCoordinator.cs` (新規)
- `src/IdeaNest/ViewModels/StartupViewModel.cs` (新規、`RecentFileItem` を移動)
- `src/IdeaNest/Services/AppSettingsService.cs` (内部委譲化、公開 API は不変)
- `src/IdeaNest/App.xaml.cs` (起動ロジック整理)
- `src/IdeaNest/Views/StartupWindow.xaml.cs` (VM 受け取りに変更、XAML は不変)
- `tests/IdeaNest.Tests/IdeaNest.Tests.csproj` / `Services/RecentFilesServiceTests.cs`
  / `Services/StartupCoordinatorTests.cs` / `ViewModels/StartupViewModelTests.cs` (新規)
- `src/IdeaNest/IdeaNest.csproj` の `<Version>` を `0.8.4` → `0.8.5` に更新
- 保存ファイル形式 (`.ideanest`) / `settings.json` 形式 / XAML / メニュー構成 /
  既存コマンド / 通常起動時のスタートダイアログ表示 / 最近使ったファイルの保存先 — **変更なし**

---

## v0.8.4 (MainViewModel分割 第4段階：ExportViewModel抽出) — 2026-06-07

`MainViewModel` の外部出力 (Markdown / NoteNest エクスポート、クリップボードコピー) に関する
責務を `ExportViewModel` に切り出しました。
v0.8.1 〜 v0.8.3 と同様、XAML・保存形式・既存メニュー・ショートカット・出力形式は変更なし。

### 変更内容

- **`ExportViewModel` を新規追加** (`ViewModels/ExportViewModel.cs`)
  - 担当: Markdown 風エクスポート (`ExportMarkdown`)、
    NoteNest 向けエクスポート (`ExportNoteNest`)、
    単一カードの Markdown コピー (`CopyCardMarkdown`)、
    表示中カード全件の Markdown コピー (`CopyAllMarkdown`)、
    表示中カード全件の NoteNest 向けコピー (`CopyNoteNest`)
  - 出力対象 0 件のチェック、`MarkdownExportService` / `NoteNestExportService` への委譲、
    クリップボード / ファイル書き込みの例外ハンドリングを集約
  - 出力フォーマット自体は既存サービスをそのまま呼び出すため変化なし

- **`IExportPlatform` インターフェースを新規追加** (`ViewModels/IExportPlatform.cs`)
  - WPF 依存処理 (SaveFileDialog / MessageBox / Clipboard / NoteNest オプションダイアログ) を
    `PromptSaveFilePath` / `PromptNoteNestOptions` / `SetClipboard` /
    `ShowInformation` / `ShowError` の 5 つのメソッドとして抽象化
  - これにより `ExportViewModel` 本体は WPF 非依存となり、
    `IdeaNest.Tests` でクロスプラットフォームに単体テストできる

- **`WpfExportPlatform` を新規追加** (`ViewModels/WpfExportPlatform.cs`)
  - `IExportPlatform` の WPF 実装。`SaveFileDialog` / `NoteNestExportOptionsWindow` /
    `Clipboard` / `MessageBox` を MainViewModel から切り出して内包

- **`MainViewModel` を更新**
  - `ExportMarkdown` / `CopyCardMarkdown` / `CopyAllMarkdown` / `ExportNoteNest` /
    `CopyNoteNest` の各 private メソッドを削除し、`Export.XxxMethod()` への委譲に置換
  - `ExportMarkdownCommand` 等の既存 `ICommand` プロパティは維持
    (XAML バインディングへの影響ゼロ)
  - PreviewIdeaWindow への `onCopyMarkdown` コールバックも `Export.CopyCardMarkdown` に切替
  - `Export` プロパティとしてサブ ViewModel を公開し、
    `getVisibleCards` / `getFilterContext` ラムダ経由で表示中カード・フィルタ状態を遅延取得

- **`IdeaNest.Tests.csproj`** に `ExportViewModel.cs` / `IExportPlatform.cs` の
  `<Compile Include>` を追加
- **新規テスト 23 件** (`ExportViewModelTests.cs`):
  各エクスポート / コピーメソッドについて、
  - 0 件時にインフォメーションが出てクリップボード・ファイル操作が走らないこと
  - 保存パス / オプションダイアログのキャンセル時に書き込みが行われないこと
  - 表示中カードと FilterContext が `MarkdownExportService` /
    `NoteNestExportService` にそのまま渡され、出力が既存サービスの結果と一致すること
  - クリップボード例外時にエラーダイアログが出てステータスメッセージは更新されないこと
  - 既定ファイル名が `ideanest_export_` / `ideanest_notenest_` プレフィックスと
    `.md` 拡張子を持つこと
  - `getVisibleCards` / `getFilterContext` が呼出時点で評価される (遅延読み出し) こと

### 影響範囲

- `src/IdeaNest/ViewModels/ExportViewModel.cs` (新規)
- `src/IdeaNest/ViewModels/IExportPlatform.cs` (新規)
- `src/IdeaNest/ViewModels/WpfExportPlatform.cs` (新規)
- `src/IdeaNest/ViewModels/MainViewModel.cs` (内部リファクタリングのみ、公開コマンドの挙動不変)
- `tests/IdeaNest.Tests/IdeaNest.Tests.csproj` / `ExportViewModelTests.cs` (新規)
- `src/IdeaNest/IdeaNest.csproj` の `<Version>` を `0.8.3` → `0.8.4` に更新
- 保存ファイル形式 (`.ideanest`) / XAML / メニュー構成 / 既存コマンド / 出力形式 — **変更なし**

---

## v0.8.3 (MainViewModel分割 第3段階：TagPanelViewModel抽出) — 2026-06-07

`MainViewModel` のタグパネル管理に関する責務を `TagPanelViewModel` に切り出しました。
v0.8.1 / v0.8.2 と同様、XAML・保存形式・既存コマンドは変更なし。

### 変更内容

- **`TagPanelViewModel` を新規追加** (`ViewModels/TagPanelViewModel.cs`)
  - 担当: タグパネル開閉 (`IsTagPanelOpen`)、ボタンラベル / ツールチップ
    (`TagPanelButtonLabel` / `TagPanelButtonTip`)、
    タグ検索欄 (`TagSearch` / `HasTagSearch` / `ClearTagSearch`)、
    タグ一覧管理 (`AllItems` / `VisibleItems` / `SetAllItems`)、
    タグ選択通知 (`SelectTag`)
  - `IsTagPanelOpen` 変更時のみ `onMarkDirty` を呼び出す
    (Settings に保存すべき状態変化だけを通知)
  - `TagSearch` はローカル表示フィルタ — Settings 非保存・`onMarkDirty` 非呼出。
    `LoadFromSettings()` でワークスペース切替時に自動クリアされる
  - `AllItems` は未フィルタの全件 `ObservableCollection`。タグ管理画面が参照し、
    `TagSearch` の状態に関係なく常に全タグを扱える
  - `VisibleItems` は `TagSearch` 適用済みの `ObservableCollection`。サイドパネルが参照する。
    いずれも安定した参照で、内容だけを Clear + Add で更新する
  - `WorkspaceSettings` との同期: `SyncToSettings` / `LoadFromSettings`
  - WPF 依存なし → `IdeaNest.Tests` でクロスプラットフォームにテスト可能
  - `MainViewModel` はコールバック (`onMarkDirty` / `onTagSelected`) を渡して連携

- **`MainViewModel` を更新**
  - `IsTagPanelOpen` / `TagPanelButtonLabel` / `TagPanelButtonTip` を
    `TagPanelViewModel` へ移譲
  - `TagItems => TagPanel.AllItems` でタグ管理画面へ未フィルタの全件一覧を提供
  - `VisibleTagPanelItems => TagPanel.VisibleItems` でサイドパネルへ検索済み一覧を提供
  - 既存 XAML バインディングへの影響ゼロ:
    薄い転送プロパティを残し、`TagPanel.PropertyChanged` を再発火
  - `RefreshTags` の TagItems 更新を `TagPanel.SetAllItems(tagItems)` に集約
  - `SyncWindowSizeBeforeSave`: 個別代入を `TagPanel.SyncToSettings()` に集約
  - `ReloadFromWorkspace`: 個別フィールド代入を `TagPanel.LoadFromSettings()` に集約
  - タグパネル変更時の Settings 同期: `TagPanel` の `onMarkDirty` には
    `OnTagPanelChanged()` を渡し、`MarkDirty()` の直前に
    `TagPanel.SyncToSettings(_workspace.Settings)` を実行する。
    これにより、`MainViewModel.Settings.TagPanelOpen` 等が
    保存を待たずに即時最新値を反映する

- **`IdeaNest.Tests.csproj`** に `TagItemViewModel.cs` / `TagPanelViewModel.cs` の
  `<Compile Include>` を追加
- **新規テスト 39 件** (`TagPanelViewModelTests.cs`):
  デフォルト値、`IsTagPanelOpen` の変更・同値・コールバック発火、`Toggle`、
  `TagPanelButtonLabel` / `TagPanelButtonTip` の状態反映、
  `TagSearch` の変更・null 代入・空白のみ判定、`ClearTagSearch` の動作と no-op 確認、
  `VisibleItems` のフィルタリング (空検索で全件表示・大文字小文字無視・一致なし)、
  `AllItems` が TagSearch によらず全件を保持すること・安定参照の保証、
  `LoadFromSettings` でのワークスペース切替時 TagSearch クリア動作、
  `SelectTag` のコールバック呼出・null 強制変換、
  `SyncToSettings` / `LoadFromSettings` のラウンドトリップ、
  `onMarkDirty` 発火時には既に最新値が観測可能であるという順序契約

### 影響範囲

- `src/IdeaNest/ViewModels/TagPanelViewModel.cs` (新規)
- `src/IdeaNest/ViewModels/MainViewModel.cs` (内部リファクタリングのみ、公開インターフェース不変)
- `tests/IdeaNest.Tests/IdeaNest.Tests.csproj` / `TagPanelViewModelTests.cs` (新規)
- `src/IdeaNest/IdeaNest.csproj` の `<Version>` を `0.8.2` → `0.8.3` に更新
- 保存ファイル形式 (`.ideanest`) / XAML / メニュー構成 / 既存コマンド — **変更なし**

---

## v0.8.2 (MainViewModel分割 第2段階：FilterViewModel抽出) — 2026-06-07

`MainViewModel` の表示条件管理に関する責務を `FilterViewModel` に切り出しました。
v0.8.1 と同様、XAML・保存形式・既存コマンドは変更なし。

### 変更内容

- **`FilterViewModel` を新規追加** (`ViewModels/FilterViewModel.cs`)
  - 担当: 検索文字列 (`SearchText`)、選択中タグ (`SelectedTag`)、
    選択中色 (`SelectedColor`)、アーカイブ表示 (`ShowArchived`)、
    アクティブフィルタフラグ (`HasActiveFilter`)
  - 一括クリア: `ClearFilter()` (各 setter が変更時のみコールバックを発火するため、
    既にクリア済みの場合は呼び出しが完全な no-op になる)
  - `WorkspaceSettings` との同期: `SyncToSettings` / `LoadFromSettings`
  - WPF 依存なし → `IdeaNest.Tests` でクロスプラットフォームにテスト可能
  - `MainViewModel` はコールバック (`onRefreshVisible` / `onMarkDirty`) を渡して連携

- **`MainViewModel` を更新**
  - `SearchText` / `SelectedTag` / `SelectedColor` / `ShowArchived` / `HasActiveFilter` を
    `FilterViewModel` へ移譲
  - 既存 XAML バインディングへの影響ゼロ:
    薄い転送プロパティを残し、`Filter.PropertyChanged` を再発火
  - `SyncWindowSizeBeforeSave`: 個別代入を `Filter.SyncToSettings()` に集約
  - `ReloadFromWorkspace`: 個別フィールド代入を `Filter.LoadFromSettings()` に集約
  - フィルタ変更時の Settings 同期: `Filter` の `onMarkDirty` には
    `OnFilterChanged()` を渡し、`MarkDirty()` の直前に
    `Filter.SyncToSettings(_workspace.Settings)` を実行する。
    これにより、抽出前と同様に `MainViewModel.Settings.SearchText` 等が
    保存を待たずに即時最新値を反映する (公開プロパティの観測可能な振る舞いを維持)

- **`IdeaNest.Tests.csproj`** に `FilterViewModel.cs` の `<Compile Include>` を追加
- **新規テスト 34 件** (`FilterViewModelTests.cs`):
  デフォルト値、SearchText / SelectedTag / SelectedColor / ShowArchived の
  変更・同値・null 代入・コールバック発火、HasActiveFilter の各ケース
  (空白のみは inactive、ShowArchived は影響しない)、ClearFilter の動作、
  PropertyChanged 通知、SyncToSettings / LoadFromSettings のラウンドトリップ、
  `onMarkDirty` 発火時には既に最新値が観測可能であるという順序契約

### 影響範囲

- `src/IdeaNest/ViewModels/FilterViewModel.cs` (新規)
- `src/IdeaNest/ViewModels/MainViewModel.cs` (内部リファクタリングのみ、公開インターフェース不変)
- `tests/IdeaNest.Tests/IdeaNest.Tests.csproj` / `FilterViewModelTests.cs` (新規)
- `src/IdeaNest/IdeaNest.csproj` の `<Version>` を `0.8.1` → `0.8.2` に更新
- 保存ファイル形式 (`.ideanest`) / XAML / メニュー構成 / 既存コマンド — **変更なし**

---

## v0.8.1 (MainViewModel分割 第1段階：CardDisplayViewModel抽出) — 2026-06-07

`MainViewModel` のカード表示設定に関する責務を `CardDisplayViewModel` に切り出しました。
v0.8.0 で整備した単体テストを安全網として活用し、既存動作を変えずに内部構造を改善しています。

### 変更内容

- **`CardDisplayViewModel` を新規追加** (`ViewModels/CardDisplayViewModel.cs`)
  - 担当: カードサイズ (`CardSize`)、高さモード (`CardHeightMode`)、並び順 (`SortMode`)、
    シャッフル管理 (`_shuffleOrder` / `OrderByShuffle` / `Reshuffle` / `GenerateShuffleOrder`)
  - すべての寸法計算 (`CardWidth` / `CardHeight` / `CardMinHeight` / `CardMaxHeight`)
    とフラグ (`IsCardSize*` / `IsCardHeight*` / `IsShuffleMode`) を所有
  - `WorkspaceSettings` との同期: `SyncToSettings` / `LoadFromSettings`
  - WPF 依存なし → `IdeaNest.Tests` でクロスプラットフォームにテスト可能
  - `MainViewModel` はコールバック (`onRefreshVisible` / `onMarkDirty`) を渡して連携

- **`MainViewModel` を更新**
  - 上記プロパティ・メソッドを `CardDisplayViewModel` へ移譲
  - 既存 XAML バインディングへの影響ゼロ:
    `MainViewModel` は薄い転送プロパティ (forwarding getters/setters) を残し、
    `CardDisplay.PropertyChanged` を受け取って `MainViewModel.PropertyChanged` として
    再発火することで、既存の `{Binding CardWidth}` 等が変更なしで動作する
  - `SetCardSizeCommand` / `SetCardHeightModeCommand` / `ReshuffleCommand` は
    `CardDisplay` の setters / `Reshuffle(nonPinnedIds)` に委譲

- **`IdeaNest.Tests.csproj`** に `CardDisplayViewModel.cs` の `<Compile Include>` を追加
- **新規テスト 35 件** (`CardDisplayViewModelTests.cs`):
  デフォルト値、CardWidth/CardHeight/CardMin/MaxHeight の全サイズ×モード組み合わせ、
  フラグ、入力値バリデーション、コールバック発火、`PropertyChanged` 通知、
  `SyncToSettings` / `LoadFromSettings` のラウンドトリップ、シャッフル動作

### 影響範囲

- `src/IdeaNest/ViewModels/CardDisplayViewModel.cs` (新規)
- `src/IdeaNest/ViewModels/MainViewModel.cs` (内部リファクタリングのみ、公開インターフェース不変)
- `tests/IdeaNest.Tests/IdeaNest.Tests.csproj` / `CardDisplayViewModelTests.cs` (新規)
- `src/IdeaNest/IdeaNest.csproj` の `<Version>` を `0.8.0` → `0.8.1` に更新
- 保存ファイル形式 (`.ideanest`) / XAML / メニュー構成 / 既存コマンド — **変更なし**

---

## v0.8.0 (Unit Testプロジェクト追加) — 2026-06-07

`IdeaNest.Tests` プロジェクトを新規追加し、UI 非依存のロジックに対する
単体テスト基盤を導入しました。
今後 `MainViewModel` の分割など内部リファクタリングを進める前の安全網として、
代表的な Service / ViewModel に xUnit ベースのテストを整備しています。

### 変更内容

- **`tests/IdeaNest.Tests` プロジェクトを追加** (`net8.0`, xUnit)
  - `tools/IdeaNest.Smoke` と同じ方針で、テスト対象のソースを `<Compile Include>`
    でリンクし、WPF (`net8.0-windows`) に依存せずクロスプラットフォームで実行できる構成
  - `IdeaNest.sln` にも追加済み
- **テスト対象 (v0.8.0 時点)**
  - `WorkspaceService` — タグ正規化 (`NormalizeTag` / `NormalizeTags`)、`Save` / `Load`
    のラウンドトリップ、`.bak` 生成、旧形式 (settings なし / `cardHeightMode` なし)
    の読み込み互換性
  - `MarkdownExportService` — 色名表示マップ、単一カード書式、`FormatAll` の
    ヘッダ・件数・区切り、フィルタ表示行の有無
  - `NoteNestExportService` — 連番タイトル、メタ情報の出し分け、`[NOTE]` / `[TODO]`
    マーカーの有無
  - `IdeaCardViewModel` — `DisplayTitle` のフォールバック / 切り詰め、`BodyPreview`
    の 4 行 / 200 文字制限、`TagsText`、`BackgroundBrush` のマッピング、
    `Touch` 後の `UpdatedAt` 更新、`Title` 設定時の `PropertyChanged` 発火
- **`dotnet test` で 56 件のテストが成功することを確認** (Linux 環境含む)
- **README にテスト実行方法を追記** (`dotnet test`)

### 影響範囲

- `tests/IdeaNest.Tests/IdeaNest.Tests.csproj`・テストソースを新規追加
- `IdeaNest.sln` にテストプロジェクト参照を追加
- `src/IdeaNest/IdeaNest.csproj` の `<Version>` を `0.7.3` → `0.8.0` に更新
  (タイトルバーが `ver0.8.0` 表記になる)
- 本体コード (`src/IdeaNest/**`) には変更なし。既存機能の挙動・保存ファイル形式・
  メニュー構成は v0.7.3 と同一

### スコープ外 (今後の検討事項)

- `MainViewModel` 自体の単体テスト整備 (`MainViewModel` の責務分割と併せて検討。
  backlog M12 で継続管理)
- `AppSettingsService` のテスト (`%AppData%` 固定パスのためフックポイントが必要。
  別途検討)
- UI / E2E テスト (WPF 自動操作は今回スコープ外)

---

## v0.7.3 (高さ可変カードの行内ストレッチ修正) — 2026-06-06

v0.7.2 で追加した「本文に合わせる」モードで、短い本文のカードが同じ行にある別カードの高さに引っ張られて高く表示される不具合を修正しました。

### 変更内容

- **カード Border に `VerticalAlignment="Top"` を設定**
  - `WrapPanel` 配下では Border は既定 (`VerticalAlignment="Stretch"`) のままだと、同一行内で最も高いカードと同じ高さまで自動で引き伸ばされていた
  - 明示的に `Top` を指定することで、各カードは自身が要求した高さ (DesiredSize) のままレンダリングされるようになる
  - 結果として、短文カードは最小高さ付近、長文カードは最大高さで表示され、行内で高さがバラつくようになる (短文カードの下に余白が生じるのは仕様)
- **固定モードへの影響はなし**
  - 固定モードでは Border に明示的な `Height` (S:148 / M:212 / L:280) を与えているため、`VerticalAlignment="Top"` を追加しても従来どおり全カードが同じ高さで揃う

### 既知の制約

- 行内では引き続き各行のうち最も高いカード分の縦スペースが確保されるため、短文カードの直下に空白が見える。Google Keep のような完全な Masonry レイアウト (短文カードの下に次の行のカードが詰めて表示される) は今回のスコープ外で、必要に応じて backlog にて検討する

### 影響範囲

- `Views/MainWindow.xaml` のカード Border に `VerticalAlignment="Top"` を 1 行追加
- `IdeaNest.csproj` の `<Version>` を `0.7.2` → `0.7.3` に更新 (タイトルバーが `ver0.7.3` 表記になる)
- 設定ファイル (`.ideanest` / `settings.json`)・コマンド・メニュー構成・公開 API には変更なし

---

## v0.7.2 (カード高さモードの追加) — 2026-06-06

カードサイズ (S / M / L) に加えて、カード高さの表示モードを切り替えられるようにしました。
従来の「整然と並べたい」用途と、「本文量を把握したい」用途を両立させるための調整です。

### 変更内容

- **カード高さモード切替を追加** (`表示 → カード高さ`)
  - `固定` (既定): 従来どおり S/M/L ごとの固定高さで一覧表示
  - `本文に合わせる`: 本文量に応じてカード高さを可変にする。S/M/L ごとに最小高さ・最大高さを設定
    - S: 110 〜 200 / M: 140 〜 280 / L: 180 〜 380
  - 最大高さを超える本文は一覧上では省略表示 (`ClipToBounds`)。全文はカード詳細プレビューまたは編集ダイアログで確認できる
- **カード幅は S / M / L の現在仕様を維持**
  - 高さモードを切り替えても幅は変わらない
- **設定の保存・復元**
  - 選択中の高さモードは `.ideanest` の `settings.cardHeightMode` (string: `"fixed"` / `"auto"`) に保存される
  - 既存ファイルに項目がない場合は `"fixed"` を既定として扱う (従来互換)

### 影響範囲

- `WorkspaceSettings.CardHeightMode` を追加 (既定 `"fixed"`)
- `MainViewModel` に `CardHeightMode` / `CardMinHeight` / `CardMaxHeight` / `SetCardHeightModeCommand` を追加。`CardHeight` は `"auto"` 時に `double.NaN` を返す
- `MainWindow.xaml` のカード Border に `MinHeight` / `MaxHeight` バインディングを追加し、`表示` メニュー配下にサブメニュー `カード高さ` を追加
- `IdeaNest.csproj` の `<Version>` を `0.7.1` → `0.7.2` に更新 (タイトルバーが `ver0.7.2` 表記になる)
- `.ideanest` ファイル形式 (`settings.cardHeightMode` を追加)・`AppSettings` (`settings.json`) には変更なし

---

## v0.7.1 (タグパネル・プレビュー画面の実機調整) — 2026-06-04

実機確認で気になった UI 表示・操作感を調整しました。

### 変更内容

- **タグ一覧パネルの初期表示を折りたたみに変更**
  - `WorkspaceSettings.TagPanelOpen` の既定値を `true` → `false` に変更
  - 既存ファイルに保存済みの状態がある場合はそちらを優先 (新規・履歴に保存値がない場合のみ折りたたみが既定)
  - カード一覧を主役にしたい意図
- **カード詳細プレビュー本文欄の背景色をカード色に統一**
  - 本文 Border の背景を `SurfaceBrush` (白) → 対象カードの色 (`BackgroundBrush`) に変更
  - 白い入力欄のような見た目を回避し、編集不可であることが自然に伝わるように
  - 編集ダイアログ側 (`EditIdeaWindow`) の入力欄は影響なし
- **カード詳細プレビュー本文のフォントサイズを拡大**
  - 本文の `FontSize` 15 → 16、`LineHeight` 24 → 26
  - 「読む」ための画面として読みやすさを優先
- **プレビュー前後移動ボタンの表記を短縮**
  - 「← 前へ」「次へ →」→「← 前」「次 →」
  - ホバー時のツールチップで「前のカード (←)」「次のカード (→)」とショートカットを補足
- **上部検索欄にプレースホルダ (サンプルテキスト) を表示**
  - 未入力時に「タイトル・本文・タグを検索」を薄く表示し、検索対象を一目で示す
  - 入力すると自動的に非表示になる
  - 既存の `Ctrl+F` フォーカス・`Esc` クリア・クリアボタン (`✕`) はすべて従来どおり動作する

### 影響範囲

- `WorkspaceSettings.TagPanelOpen` 既定値の変更
- `MainWindow.xaml` の既存検索ボックスにプレースホルダ用 `TextBlock` を重ねた (ロジック追加なし)
- `IdeaNest.csproj` の `<Version>` を `0.7.0` → `0.7.1` に更新 (タイトルバーが `ver0.7.1` 表記になる)
- `.ideanest` ファイル形式 (キーは変更なし)、`AppSettings` (`settings.json`) には変更なし

---

## v0.7.0 (タイトルバーにバージョン表記) — 2026-06-04

### 変更内容

- **タイトルバーにバージョンを追加**
  - 表示形式: `IdeaNest - [ファイル名][*] - ver0.7.0`
  - 未保存ファイルは `(未保存)` のまま / 未保存変更の `*` はファイル名の直後を維持
  - バージョン文字列はアセンブリバージョン (`<Version>` in `IdeaNest.csproj`) から生成するため、csproj を更新するだけで自動的に反映される

### 影響範囲

- `IdeaNest.csproj` の `<Version>` を `0.5.0` → `0.7.0` に更新
- `MainViewModel.Title` にバージョン文字列を追加
- `.ideanest` ファイル形式・`AppSettings` (`settings.json`) には変更なし

---

## v0.6.4 (最近使ったファイルのクリア・壁打ち) — 2026-06-04

バックログ L5・L6 の対応。

### 変更内容

- **最近使ったファイルのクリア** (L5)
  - スタートダイアログのフッター左端に「履歴をクリア」ボタンを追加
  - 履歴が 0 件のときはグレーアウト。クリア後は「まだ履歴がありません」のヒントに切り替わる
  - `AppSettingsService.ClearRecentFiles()` を追加
- **壁打ち — ランダムに 1 枚プレビュー** (L6)
  - `編集 → 壁打ち — ランダムにプレビュー` (`Ctrl+Shift+R`) を追加
  - フィルタバーの 🎲 ボタンからも起動可能
  - 現在の表示条件 (検索・タグ・色フィルタ・アーカイブ表示) に一致するカードの中からランダムに 1 枚選んでプレビューを開く
  - カードが 0 件のときはコマンドが無効化される
  - 既存の `PreviewIdea` ロジックを再利用しているため前後移動も動作する

### 影響範囲

- `AppSettingsService` に `ClearRecentFiles()` を追加
- `MainViewModel` に `RandomPreviewCommand` を追加
- `.ideanest` ファイル形式・`AppSettings` (`settings.json`) には変更なし

---

## v0.6.3 (チュートリアル画面) — 2026-06-04

姉妹プロダクト NoteNest と同様、メニューバーから基本操作を視覚的に確認できるチュートリアル画面を追加しました。

### 変更内容

- **ヘルプメニューを新設**し、`ヘルプ → チュートリアル` を追加
- **チュートリアル画像** (`Assets/tutorial.png`) をアプリ内リソースとして同梱
- **TutorialWindow** を追加: 親ウィンドウ中央にモーダル表示、`Esc` または「閉じる」で閉じる
  - 画像はウィンドウ幅に合わせて縮小表示 (`StretchDirection="DownOnly"`)
  - 画像がウィンドウより大きい場合はスクロール可能
  - タスクバーには個別表示しない (`ShowInTaskbar="False"`)

### 影響範囲

- `IdeaNest.csproj` に `Resource Include="Assets\tutorial.png"` を追加
- `.ideanest` ファイル形式・`AppSettings` (`settings.json`) には変更なし
- 既存機能 (スタートダイアログ・カード操作・プレビュー・検索・タグ/色フィルタ・シャッフル・カードサイズ切替・自動保存・エクスポート・コピー) はすべて変更なし

---

## v0.6.2 (プレビューの前後カード移動) — 2026-06-03

カード詳細プレビューに、同一フィルタ内のカードを連続して閲覧できるナビゲーション機能を追加しました。

### 変更内容

- **プレビューウィンドウに「← 前へ」「次へ →」ボタンを追加**
  - プレビューを閉じることなく、現在のフィルタ・並び順に基づくカード一覧を順に閲覧できる
  - 先頭カードでは「← 前へ」、末尾カードでは「次へ →」が無効化される
  - プレビューを開いた時点の `VisibleCards` のスナップショットを基準とする
- **← / → キーショートカット** でキーボードだけで前後移動可能
- **移動先のカードに対して編集・コピー・ピン留め・アーカイブ操作が正しく適用される**

### 影響範囲

- `PreviewIdeaWindow` のコンストラクタ引数を変更: 単一の `IdeaCardViewModel` → `IReadOnlyList<IdeaCardViewModel>` + `int initialIndex`
- アクションのシグネチャを `Action` → `Action<IdeaCardViewModel>` に変更し、移動後のカードに対して操作を適用
- `.ideanest` ファイル形式・`AppSettings` (`settings.json`) には変更なし

---

## v0.6.1 (プレビューの操作・余白調整) — 2026-06-03

v0.6.0 のカード詳細プレビューに対する 2 点のフィードバック対応。

### 変更内容

- **カードのダブルクリック → 編集** を撤去。シングルクリック → プレビュー → `✎ 編集` ボタンに導線を統一。
  - ダブルクリック時にプレビューと編集が二重に開く問題を解消
  - シングルクリックでプレビューが開くまでの 280ms の遅延も不要になり、即時表示に変更
  - 編集には引き続き、プレビュー内の `✎ 編集` ボタン、一覧ホバーの ✎ ボタン、右クリック → 編集 からアクセス可能
- **プレビューウィンドウの余白を縮小**
  - 外側 Border: `Margin 24 → 0` / `Padding 28 → 16` / `DropShadowEffect 撤去` (枠と密着するため影が描画されない)
  - 本文 Border: `Padding 18 → 14` / `CornerRadius 8 → 6`
  - カード色がウィンドウ全体に広がり、本文表示領域が広くなる

### 影響範囲

- v0.6.0 で導入したシングルクリック遅延 (`DispatcherTimer` 280ms) は撤去。コードビハインドが簡素化された
- 編集ダイアログを開く既存導線 (ホバー ✎ / 右クリック → 編集 / プレビューの `✎ 編集` / `Ctrl+Shift+N` の新規追加) はいずれも変更なし
- `.ideanest` ファイル形式・`AppSettings` (`settings.json`) には変更なし

## v0.6.0 (カード詳細プレビュー) — 2026-06-03

カード一覧からアイデアを「読み返す」体験を強化するため、カード詳細プレビューを追加しました。

### 変更内容

- **カード詳細プレビュー** を追加 (`PreviewIdeaWindow`)
  - **シングルクリック** でカード詳細プレビューを開く (Google Keep 風)
  - 既存の **ダブルクリック → 編集** はそのまま動作する
  - 詳細プレビューはモーダルダイアログとして開き、カード色を背景に継承
  - 表示内容: タイトル (大)、★ ピン留めバッジ、📥 アーカイブバッジ、タグチップ、本文 (スクロール可・FontSize 15・LineHeight 24)、作成日時 / 更新日時
  - 操作ボタン: `✎ 編集` / `📋 コピー (Markdown)` / `📌 ピン留め / 解除` / `📥 アーカイブ / 解除` / `閉じる`
  - `ESC` または `閉じる` ボタンでクローズ
  - プレビューを閉じただけでは未保存状態 (`*` / 自動保存) にならない
- **コンテキストメニュー** に `プレビュー(_V)` を追加 (一番上に配置)

### 影響範囲

- 一覧上のホバーボタン (✎ 📌 📥 🗑) のクリックでは詳細プレビューを開かない (`IsInsideButton` で内側 Button を検出)
- 既存のダブルクリック編集・コンテキストメニュー編集・✎ ホバーボタン編集はすべて維持
- 右クリックや右ボタン操作はプレビューを開かない (左クリックのみが対象)
- シングルクリック → ダブルクリック判定のため、シングルクリックでプレビューが開くまで約 280ms の遅延がある
- `.ideanest` ファイル形式・`AppSettings` (`settings.json`) には変更なし
- 既存の検索 / タグ / 色フィルタ / 並び順 / シャッフル / カードサイズ / 自動保存 / エクスポート / コピー / ファイル直接起動 / スタートダイアログはすべて変更なし

## v0.5.4 (起動時スタートダイアログ) — 2026-06-03

IdeaNest.exe を通常起動した場合、毎回「ファイルを開く」から手動で読み込まずに済むよう、スタートダイアログを追加しました。

### 変更内容

- **スタートダイアログ** を起動時に表示
  - 「新規プロジェクトを開始する」ボタンと、最近使ったファイル一覧 (直近 5 件) を表示
  - 最近使ったファイル: 表示名 + フルパス、ダブルクリック / 「開く」ボタンで読み込み
  - 起動引数で `.ideanest` が渡された場合 (ダブルクリック起動 / `IdeaNest.exe <path>`) はスタートダイアログを表示せず、そのまま該当ファイルを開く
  - ダイアログを閉じた (「閉じる」/ ✕ ボタン) 場合はアプリを終了する
  - 読み込みに失敗した場合はメッセージを表示してスタートダイアログに戻る
  - 履歴中のファイルが存在しない場合は一覧から除外する
- **最近使ったファイル一覧** の更新タイミング
  - 「ファイル → 開く」で読み込みが成功したとき
  - 「名前を付けて保存」が成功したとき
  - 起動引数 (関連付け / コマンドライン) でファイルを開いたとき
- **保存先**: `%AppData%\IdeaNest\settings.json` (`AppSettings`)
  - `.ideanest` ファイルとは独立して保存される
  - 同じファイルパスは重複しない (最新が先頭)
  - 上限 5 件

### 影響範囲

- `App.xaml` の `StartupUri` を撤去し、`ShutdownMode="OnExplicitShutdown"` に変更。MainWindow の Closed で `Application.Shutdown()` を呼ぶ。
- `MainWindow` のコンストラクタに `string? initialFilePath` を追加。引数なしのコンストラクタも残しているため XAML プレビュー等の互換性は保持。
- 既存の「ファイルを開く」「名前を付けて保存」「自動保存」「並び順 / シャッフル」「カードサイズ」「検索 / タグ / 色 / エクスポート / コピー」「.ideanest ダブルクリック起動」はすべて変更なし。
- `.ideanest` ファイル形式には変更なし。最近使ったファイル一覧はアプリ側設定ファイル (`settings.json`) に保存される。

## v0.5.3 (.ideanest ダブルクリック起動対応) — 2026-06-03

`.ideanest` ファイルを IdeaNest.exe に関連付けた際、ダブルクリックでファイルが直接開けるようにしました。

### 変更内容

- **起動引数によるファイル読込** を追加
  - `IdeaNest.exe <ファイルパス>` の形式で起動したとき、引数のファイルを自動で読み込む
  - ファイル関連付けでダブルクリック起動した場合も同様に機能する
  - 読込成功時の挙動は「ファイルを開く」操作と同一 (CurrentFilePath・タイトルバー・保存状態・settings 復元)
  - 引数なし / 引数がファイルとして存在しない場合は従来どおり新規状態で起動
  - 読込に失敗した場合はエラーメッセージを表示後、新規状態で起動
- ファイル関連付けの自動登録・レジストリ操作は行わない

### 影響範囲

- `MainViewModel.LoadStartup(string? filePath)` に省略可能引数を追加。引数なし呼び出しは従来と同一の挙動。
- 既存の読込・保存・カード操作・検索・タグ・色フィルタ・並び順・エクスポートはすべて変更なし。

## v0.5.2 (並び順切替・シャッフル表示) — 2026-06-03

アイデアを「更新日時順」以外の視点からも見返せるよう、並び順切替とシャッフル表示を追加しました。

### 変更内容

- **並び順切替** を追加
  - 色フィルタ行の右側 (カードサイズ S/M/L の左) に `並び:` コンボボックスを配置
  - 選択肢: `更新日時順` (既定) / `作成日時順` / `タイトル順` / `シャッフル`
  - ピン留めカードはどの並び順でも上部に固定 (ピン留め内は更新日時降順)
  - 通常カードに対してのみ選択中の並び順を適用
- **シャッフル表示** を追加
  - シャッフル選択時、現在の表示条件に一致する通常カードがランダム順で並ぶ
  - シャッフルの順序は表示更新のたびに自動再生成されない (検索・タグ・色フィルタの変更で勝手に並び替わらない)
- **再シャッフルボタン (🔀)** を追加
  - シャッフル選択中のみ、コンボボックスの右に `🔀` ボタンが表示される
  - 押すと通常カードの順番が新しくシャッフルされる
- **エクスポート・コピーへの反映**
  - Markdown 風エクスポート / Markdown 形式コピー (`Ctrl+Shift+C`) / NoteNest 向けエクスポート / NoteNest 向けコピーは、いずれも現在の表示順 (シャッフル後の順を含む) を反映する
- **シャッフル中に追加したカードの扱い**
  - シャッフル中にカードを追加すると、追加直後のカードは一覧の先頭側に表示される (見失わないよう配慮)
  - 既存カードのシャッフル順序は保たれる

### 永続化

- 選択中の並び順は `.ideanest` の `settings.sortMode` (string) として保存・復元される。
  値: `"UpdatedDesc"` (既定) / `"CreatedDesc"` / `"TitleAsc"` / `"Shuffle"`
- シャッフルの具体的な順序は保存しない。`sortMode` が `"Shuffle"` のまま開き直した場合は、その時点で新しくシャッフルされる。

### 影響範囲

- `.ideanest` ファイル形式に `settings.sortMode` を追加。古いファイルはそのまま開ける (読込時に無ければ既定値 `"UpdatedDesc"`)。
- 既定の並び順は従来と同じ「ピン留め → 更新日時降順」のため、初期状態の見え方は v0.5.1 と変わらない。
- カード追加・編集・削除・ピン留め・アーカイブ・検索・タグ・色フィルタ・タグパネル折りたたみ・カードサイズ切替・自動保存・エクスポート / コピーは挙動に変更なし (順序のみ反映)。

## v0.5.1 (カードサイズ切替) — 2026-06-03

カード一覧を自分好みの密度に調整できるよう、S/M/L のサイズ切替を追加しました。

### 変更内容

- **カードサイズ切替** を追加
  - 色フィルタ行の右端に「サイズ: S / M / L」ボタンを表示
  - S (小): 184×148 px — 多くのカードを一度に見渡したい場合に
  - M (中): 252×212 px — 従来の標準サイズ (デフォルト)
  - L (大): 340×280 px — 本文をより多く読みたい場合に
  - 選択中のサイズボタンは暖色ハイライト表示
  - `表示 → カードサイズ` メニューからも切り替え可能
- 選択中のカードサイズは `.ideanest` の `settings.cardSize` として保存・復元される

### 影響範囲

- `.ideanest` ファイル形式に `settings.cardSize` (string: `"small"` / `"medium"` / `"large"`) を追加。古いファイルはそのまま開ける (読込時に無ければデフォルト `"medium"`)。
- カード本体の表示内容 (タイトル・本文プレビュー・タグ・日時) はサイズに関わらず同じ。大きいサイズほど多くの本文が表示領域に収まる。
- 追加・編集・削除・検索・タグ・色フィルタ・エクスポート・自動保存はすべて変更なし。

## v0.5.0 (画面密度改善・カード中心 UI) — 2026-06-03

カード一覧を主役に据えるため、検索エリア・タグパネル・カード操作ボタンの表示密度を下げました。

### 変更内容

- **検索欄をコンパクト化**
  - 検索欄の高さを 36px → 30px に縮小、フォントサイズ 14 → 13、上下余白を削減
  - 検索バー全体の上下パディングを `14,10` → `8,6` に削減
  - 色フィルタ行の上下パディングも削減
  - 丸みや見た目は維持
- **タグパネルを折りたたみ可能に**
  - 検索バーの左端に「タグ ◀ / タグ ▶」トグルボタンを追加
  - 折りたたみ時はタグ列が幅 0 に縮小し、カード一覧が画面全幅を使える
  - タグ選択状態・フィルタは折りたたみ後も維持される
  - 折りたたみ状態は `.ideanest` の `settings.tagPanelOpen` として保存・復元される
  - `表示 → タグパネル` メニューでも切り替え可能
- **カード操作ボタンをホバー時のみ表示**
  - 通常時、カード下部の 📌 ✎ 📥 🗑 ボタンを透明 (Opacity=0) にしてカード本文を主役にする
  - カードにマウスを乗せると操作ボタンが表示される
  - 右クリックメニューからの操作は従来どおり常時利用可能

### 影響範囲

- `.ideanest` ファイル形式に `settings.tagPanelOpen` (bool) を追加。古いファイルはそのまま開ける (読込時に無ければデフォルト true)。
- カード追加・編集・削除・ピン留め・アーカイブ・検索・タグ管理・色フィルタ・エクスポート・自動保存はすべて変更なし。

## v0.4.0 (自動保存) — 2026-06-02

IdeaNest で入力・編集した内容を保存忘れによって失わないため、自動保存機能を追加しました。

### 変更内容

- **自動保存** を追加
  - 既存の `.ideanest` ファイルを開いている場合、編集後 2 秒で自動保存する (デバウンス)
  - 新規未保存状態 (タイトルバーが `(未保存)`) では自動保存しない (保存先を勝手に決めないため)
  - 自動保存対象: カード追加・編集・削除、ピン留め切替、アーカイブ切替、タグリネーム、タグ削除、色変更、検索文字列、選択中タグ、選択中色、アーカイブ表示切替、ウィンドウサイズ
  - 連続編集中はタイマーがリセットされ、編集が止まってから保存する
- **保存状態表示** を画面下部に追加
  - `保存済み` / `未保存の変更あり` / `自動保存中...` / `自動保存しました HH:mm` / `自動保存に失敗しました` / `新規ファイル` / `未保存 (新規ファイル)`
- **終了時確認の最適化**
  - 自動保存済みで未保存変更がない場合は、終了時確認ダイアログを出さない
  - 自動保存失敗時や新規未保存状態で変更がある場合は、従来どおり確認ダイアログを表示
- **手動保存** (`Ctrl+S` / `Ctrl+Shift+S`) は引き続き利用可能
  - 手動保存が成功すると自動保存タイマーを停止し、保存状態をリセット
- **保存失敗時の挙動**
  - 自動保存失敗時は MessageBox を出さず、画面下部の保存状態に `自動保存に失敗しました` と表示
  - `IsDirty` (`*` 表示) は維持され、次の編集や手動保存で再試行できる
  - 終了時確認は失敗時にも出るため、未保存変更を失わない

### 影響範囲

- `.ideanest` ファイル形式・保存・読込ロジックには変更なし。
- `.bak` 生成仕様も変更なし。自動保存時にも `.bak` が更新される (世代バックアップは将来検討)。
- v0.3.x の Markdown 風エクスポート / NoteNest 向け出力 / オプションダイアログは変更なし。
- v0.2.x のタグ管理 / 色フィルタも変更なし。

## v0.3.1 (NoteNest向け出力オプション) — 2026-06-02

NoteNest へ貼り付けて使うことを前提に、NoteNest 向け出力のオプション指定と出力調整を行いました。

### 変更内容

- **NoteNest 向け出力オプションダイアログ** を追加
  - エクスポート / クリップボードコピーのいずれを実行しても、実行前にオプションダイアログを表示
  - 選択できるオプション:
    - `[NOTE] IdeaNestから移行したアイデア` を付ける (初期値: ON)
    - `[TODO] 採用判断` を付ける (初期値: OFF)
    - タグ・色・作成日・更新日などのメタ情報を含める (初期値: ON)
  - `[TODO]` の初期値を OFF にすることで、NoteNest 側のマーカー一覧が過剰にならないようにした
- エクスポートとクリップボードコピーで同じオプションダイアログを使うため、出力形式が揃う

### 影響範囲

- `.ideanest` ファイル形式・保存・読込には変更なし。
- v0.3.0 の NoteNest 向けエクスポート / クリップボードコピーはオプションダイアログが追加されるが、デフォルト設定 (`[NOTE]` ON / `[TODO]` OFF / メタ ON) は v0.3.0 相当の出力に近い。
- v0.3.0 の `[TODO]` が常時付与されていた挙動は廃止 (初期値 OFF)。
- v0.2.3 の Markdown 風エクスポート / クリップボードコピーは変更なし。

## v0.3.0 (NoteNest向けエクスポート) — 2026-06-02

IdeaNest に溜めたアイデアを NoteNest のプロジェクトノートへ移しやすくするため、NoteNest 向けテキスト出力機能を追加しました。

IdeaNest は「思いつきを溜める」道具、NoteNest は「採用したアイデアをプロジェクトとして整理・実行する」道具として役割分担します。

### 変更内容

- **NoteNest向けテキストエクスポート** を追加 (メニュー: ファイル → エクスポート → NoteNest向けテキスト...)
  - 現在表示中のカードを NoteNest のノート本文に貼り付けやすい `.md` ファイルとして保存
  - デフォルトファイル名: `ideanest_notenest_yyyyMMdd_HHmm.md`
- **NoteNest向けクリップボードコピー** を追加 (メニュー: 編集 → 表示中カードをNoteNest向けにコピー)
  - コピー後にステータスバーへ「表示中の N 件をNoteNest向け形式でコピーしました。」を表示
- NoteNest向け出力フォーマット
  - 見出し: `# IdeaNestから取り込んだアイデア`
  - カード番号付き: `## 1. タイトル`
  - 日本語キー: タグ / 色 / ピン留め / アーカイブ / 作成日 / 更新日
  - 各カード末尾に NoteNest マーカー行を追加: `[NOTE] IdeaNestから移行したアイデア` / `[TODO] 採用判断`
  - 色はすべて日本語表示名 (黄 / 青 / 緑 など)
- Markdown 風エクスポート・クリップボードコピーと同じく、検索・タグ・色・アーカイブ・表示順が出力対象に反映される
- 0 件時はエクスポート・コピーを行わず、案内メッセージを表示

### `.notenest` 直接生成について

v0.3.0 では `.notenest` ファイルの直接生成・追記は行いません。
NoteNest の内部 JSON 構造に依存しないよう、テキスト出力 → NoteNest への貼り付けという運用を想定しています。
将来的に必要であれば NoteNest 側でインポート機能を検討します。

### 影響範囲

- `.ideanest` ファイル形式・保存・読込には変更なし。
- v0.2.3 の Markdown 風エクスポート / クリップボードコピーは変更なし。
- `NoteNestExportService` を新規追加。`MarkdownExportService.ColorDisplayName` を共用。

## v0.2.3 (クリップボードコピー) — 2026-06-02

IdeaNest に溜めたアイデアを ChatGPT・NoteNest・記事下書き・設計メモなどへ素早く転用できるよう、Markdown 形式でのクリップボードコピー機能を追加しました。

### 変更内容

- **カード右クリックメニュー** を追加
  - `Markdown形式でコピー`: そのカード 1 枚を Markdown テキストとしてクリップボードへコピー
  - `編集 / ピン留め切替 / アーカイブ切替 / 削除` のショートカットも右クリックから操作可能
- **表示中カードの一括コピー** を追加 (メニュー: 編集 → 表示中カードをMarkdown形式でコピー / `Ctrl+Shift+C`)
  - v0.2.2 の Markdown 風エクスポートと同じ対象・順序・フォーマット
  - 検索・タグ・色・アーカイブ表示の現在条件がそのまま反映される
- **コピー完了をステータスバーに表示** (3 秒後に自動消去)
  - 1 枚コピー時: 「カードをコピーしました。」
  - 一括コピー時: 「表示中の N 件をコピーしました。」
- 0 件時はコピーせず、案内メッセージを表示する
- `MarkdownExportService` の整形ロジックを共通化 (`FormatCard` / `FormatAll`) し、ファイル出力とクリップボードコピーで同一フォーマットを保証

### 影響範囲

- Markdown 風エクスポートのファイル出力フォーマットは変更なし。
- `.ideanest` ファイル形式・保存・読込には変更なし。
- v0.2.2 の `Ctrl+Shift+S` (名前を付けて保存) と重ならないよう `Ctrl+Shift+C` を新設。

## v0.2.2 (Markdown風エクスポート) — 2026-06-02

IdeaNest に溜めたアイデアを外部ツールで活用しやすくするため、Markdown 風テキストエクスポート機能を追加しました。

### 変更内容

- **Markdown 風エクスポート** を追加 (メニュー: ファイル → エクスポート → Markdown風テキスト...)
  - 現在の表示条件 (検索・タグフィルタ・色フィルタ・アーカイブ表示) に一致しているカードのみを出力
  - カードの並び順 (ピン留め優先 → 更新日時降順) が出力順に反映される
  - ファイルを保存するダイアログを表示 (デフォルトファイル名: `ideanest_export_yyyyMMdd_HHmm.md`)
  - 0 件時はエクスポートせず、案内メッセージを表示
  - 出力フォーマット: ヘッダー (出力日時・件数・適用中のフィルタ条件) + カードごとのセクション
  - 出力ファイルは BOM なし UTF-8 (Windows メモ帳・VS Code 等で読める)

### 出力フォーマット概要

```
# IdeaNest エクスポート

- 出力日時: 2026/06/02 15:30
- 出力件数: 3
- タグ: #開発
- 色: 青
- アーカイブ表示: しない

---

## タイトル

本文テキスト

Tags: #開発 #UI
Color: 青
Pinned: false
Archived: false
CreatedAt: 2026/06/01 10:00
UpdatedAt: 2026/06/02 09:30
```

### 影響範囲

- `.ideanest` ファイル形式・保存・読込には変更なし。
- `.ideanest` ファイルの再インポートは対象外。
- v0.2.1 の色フィルタ / v0.2.0 のタグ管理機能は変更なし。

## v0.2.1 (色フィルタ追加) — 2026-06-02

タグによる意味的な整理に加えて、カード色による視覚的な絞り込みを追加しました。

### 変更内容

- **色フィルタ** を検索バーの直下に追加
  - 8 色 (白 / 黄 / 緑 / 青 / ピンク / 紫 / オレンジ / グレー) の丸チップを横並びで表示
  - 選択中の色には暖色アクセントのリングが付く
  - 「(すべて)」ボタンで色フィルタを解除
- 色フィルタは **既存の検索・タグ・アーカイブ表示と AND で組み合わせ動作** する。
  例: `#UI` タグ + 色「青」 + 検索語「画面」 を同時に指定すると、その 3 条件すべてに一致するカードのみ表示される。
- 件数バッジ (`5件 / 全12件`) は色フィルタ適用時にも正しく更新される。
- 空状態ガイダンス (「条件に一致するカードがありません」) は色フィルタによる絞り込み時にも表示される。
- 選択中の色フィルタは `.ideanest` の `settings.selectedColor` として保存・復元される。

### 影響範囲

- `.ideanest` ファイル形式 (`version` は `0.1.0` のまま) に項目を 1 つ追加 (`settings.selectedColor`)。
  v0.1.x / v0.2.0 で作成したファイルはそのまま開ける。読込時に欄が無ければ空 (色フィルタ未選択) として扱う。
- カード自体の色 (`color`) フィールドには変更なし。色の追加・編集・ユーザー定義カラーは v0.2.1 では行わない。
- v0.2.0 のタグ管理機能 (リネーム / マージ / 削除 / カード件数表示) は変更なし。

## v0.2.0 (タグ管理改善) — 2026-06-02

タグを単なる入力文字列から、アイデア整理の中心機能として扱えるようにしました。

### 変更内容

- **タグ管理ダイアログ** を追加 (メニュー: 編集 → タグ管理...)
  - 現在使われているタグ一覧とカード件数をリスト表示
  - タグのリネーム: 一括で全カードに反映
  - タグのマージ: リネーム先に既存タグ名を指定すると統合
  - タグの削除: 全カードからタグのみ外す (カード自体は削除されない)
  - 削除前に確認ダイアログを表示
- **左ペインのタグ一覧** にカード件数を追加表示 (`#UI  5` のように)
- **タグ正規化** を強化
  - 入力時・ロード時に前後の空白を除去
  - 先頭の `#` を自動的に取り除く (例: `#UI` → `UI` として保存)
  - 同一カード内の重複タグを排除
  - 空文字・空白のみのタグは無視
- **タグ一覧はカードから算出** するため、使われていないタグは自然に消える。
  別途クリーンアップ操作は不要。
- `PromptWindow` (テキスト入力ダイアログ) を共通コンポーネントとして追加

### タグ統合の動作

タグ管理ダイアログで「リネーム」を選び、既に存在するタグ名を入力した場合は、
その 2 タグが統合されます。例: `#画面` を `#UI` にリネームすると、
`#画面` を持っていたすべてのカードに `#UI` が追加され (重複は排除)、
`#画面` は各カードから外れます。

### 影響範囲

- `.ideanest` ファイル形式に変更はありません。v0.1.x で作成したファイルはそのまま開けます。
- ロード時に `#` 付きで保存されていたタグは自動的に正規化されます。

## v0.1.3 (基本操作の整理) — 2026-06-02

「一通り動くだけ」から「日常的に少し使ってみられる」状態に近づけるため、
基本操作の粗を整える小さな改善をまとめた。機能追加や保存形式の変更はない。

### 変更内容

- カード一覧が 0 件のときに、状況に応じた **空状態ガイダンス** を表示するようにした。
  - 全 0 件: 「まだアイデアがありません」+ 「右下の『＋』ボタンから最初のアイデアを追加できます」
  - 検索 / タグで 0 件: 「条件に一致するカードがありません」+ 「検索語やタグを変更してください」
  - 非アーカイブ表示で全カードがアーカイブ済み: 「表示できるカードがありません」+
    「アーカイブを表示」案内
- 検索バー横に **表示件数バッジ** を表示。
  実際に見えている件数と全件数が一致しないとき (検索 / タグ絞り込み、または
  アーカイブが隠れているとき) は `5件 / 全12件` の形式、一致するときは `12件` の形式。
- 検索欄内側に **クリアボタン (✕)** を追加。検索文字列があるときだけ表示される。
- `Ctrl+F` で検索欄にフォーカスし、既存の検索文字列を全選択する。
- 検索欄で `Esc` を押すと検索文字列をクリア、空ならフォーカスを外す。
- 削除確認ダイアログの文言を見直し、誤削除を防ぐためにアーカイブ案内を追加。
- メニューの「編集」に「検索(_F)」項目 (`Ctrl+F`) を追加した。

### `Del` / `Ctrl+P` について

「選択中カード」という UI 概念が現状の実装に無いため、`Del` (削除) と
`Ctrl+P` (ピン留め切替) は v0.1.3 では実装せず、`docs/backlog.md` に
カード選択モデル導入と合わせた将来検討として残した。

### 影響範囲

- 機能・コマンド・保存形式に変更はありません。
- `.ideanest` ファイル形式 (`version` は `0.1.0` のまま) は v0.1.0 以降と互換です。

## v0.1.2 (見た目の整理) — 2026-06-02

カード型アイデアスクラップツールにふさわしい軽い見た目に近づけるため、
画面全体の野暮ったさを取り除く UI 調整を行いました。
機能は v0.1.1 から変更ありません。

### 変更内容

- アプリ全体の背景を真っ白から薄いグレー (`#F6F7F9`) に変更し、
  カードが浮き上がって見えるようにした。
- カードを白基調 + 角丸 (12px) + 薄い影の見た目に統一し、
  枠線を排除して余白を増やした。
- カード色を濃いパステルから淡いパステル (`#FFF7CC` 等) に差し替え、
  文字の可読性を優先するようにした。
- タグ表示を `#UI #開発` の素の文字列から、
  小さなチップ風 (淡いグレー背景 + 角丸) に変更した。
- 右下の「＋」フローティングボタンを暖色アクセント (`#E8773A`) に変更し、
  サイズも 56px → 60px に拡大して主要導線として強調した。
- 検索欄を角丸の `SearchTextBoxStyle` に置き換え、
  上部操作バーの余白を拡げた。
- 編集ダイアログを白カードの上に乗せたシンプルなフォーム形式に変更し、
  `OK` を主ボタン (アクセント色)、`キャンセル` をアウトライン (副ボタン) に差し替えた。
- 編集ダイアログの色選択を、文字列 (`yellow` 等) を表示する標準 `ComboBox` から、
  淡いパステルの丸スウォッチ並びに変更。選択中は暖色アクセントのリングが付く。
- 未保存変更時の確認・カード削除確認のダイアログを、標準 `MessageBox` から、
  メイン画面と同じカード風の独自ダイアログ (`ConfirmWindow`) に置き換えた。
  ボタンには主/副スタイルが適用され、操作の選択肢が見た目で区別できる。
- 共通スタイル (`CardContainerStyle` / `PrimaryButtonStyle` /
  `SecondaryButtonStyle` / `IconButtonStyle` / `SearchTextBoxStyle` /
  `TagChipStyle` / `FloatingAddButtonStyle`) を `App.xaml` に定義した。
- 標準 WPF のボタン立体感を抑え、ホバー時にだけ淡い背景色が乗る形に統一した。

### 影響範囲

- 機能・コマンド・キーボードショートカット・保存形式に変更はありません。
- `.ideanest` ファイル形式 (`version` は `0.1.0` のまま) は v0.1.0 と互換です。
- v0.1.0 / v0.1.1 で作成したファイルはそのまま読み込めます。

## v0.1.1 (UX 改善) — 2026-06-02

新規カード作成導線を「画面上部の入力欄」から「画面右下の "＋" ボタンによる
ダイアログ呼び出し」へ変更しました。Google Keep ライクな
「カード一覧中心 + 追加はモーダル」というメンタルモデルに揃え、
メイン画面はアイデアの俯瞰と検索に集中できる構成になります。

### 変更内容

- 画面上部の新規カード入力欄 (タイトル / 本文 / タグ / 追加ボタン) を撤去。
- 画面右下に丸い青色のフローティング「＋」ボタンを追加。クリックで
  新規カード作成ダイアログを表示。
- 新規作成も既存編集と同じ `EditIdeaWindow` を再利用 (UI と保守を一本化)。
  ダイアログタイトルだけ「新規アイデア」「アイデア編集」で切り替える。
- 新規作成時もタイトル未入力なら本文先頭から自動生成 (v0.1.0 と同じ規則)。
- タイトル・本文ともに空のまま `OK` した場合はカードを作成しない (静かに破棄)。
- キーボードショートカットを整備:
  - `Ctrl+Shift+N`: 新規カード作成ダイアログ
  - `Ctrl+N` / `Ctrl+O` / `Ctrl+S` / `Ctrl+Shift+S`: ファイル操作
- 「編集」メニューの「新規カード追加」項目に `Ctrl+Shift+N` を併記。

### 影響範囲

- 既存カードの編集 / 検索 / タグフィルタ / アーカイブ表示 / 保存 / 読込 /
  `.bak` 生成 / 未保存確認は変更なし。
- `.ideanest` 保存形式は v0.1.0 と互換 (`version` は `0.1.0` のまま据え置き)。
  v0.1.0 で作成したファイルはそのまま開けます。

## v0.1.0 (プロトタイプ初版) — 2026-06-02

NoteNest の姉妹品として、カード型のアイデアスクラップツール **IdeaNest** の最初の動作版です。
「起動 → 追加 → 検索 → 保存 → 読込」の一気通貫を最優先に実装しました。

### 追加した機能

- アイデアカードの追加 / 編集 / 削除
- カード型一覧 (ピン留め最上位 / 更新日時降順)
- 簡易入力欄からのワンアクション追加 (本文だけでも可、タイトルは自動生成)
- 編集ダイアログ (タイトル / 本文 / タグ / 色 / ピン / アーカイブ)
- キーワード検索 (タイトル / 本文 / タグ)
- タグによる絞り込み (左ペイン)
- アーカイブ表示の切替
- `.ideanest` (UTF-8 JSON) への保存 / 読込 / 名前を付けて保存 / 新規作成
- 未保存変更時の確認ダイアログ / タイトルバーの `*` 表示
- 保存時の `.bak` 自動生成
- ウィンドウサイズ / 検索文字列 / 選択中タグ / アーカイブ表示状態の永続化

### 既知の制限

- 画面設定は `.ideanest` 経由でのみ復元される (アプリ単体のグローバル設定保存はない)。
- 並べ替えは「ピン → 更新日時降順」固定。
- 色は固定パレット (8 色) のみ。
- ショートカットキーは未実装。
- 同じ `.ideanest` を複数プロセスで開くことは想定していない。

### 対象外 (将来検討)

画像貼り付け / 添付ファイル / リマインダー / チェックリスト / タスク管理 /
カレンダー連携 / クラウド同期 / 共同編集 / Markdown プレビュー /
NoteNest との直接連携 / AI 要約 / 自動分類。

詳細は [`docs/backlog.md`](backlog.md) を参照。

### ビルド / 動作確認

- Windows 上で `dotnet build` および `dotnet run --project src/IdeaNest/IdeaNest.csproj` を確認。
- 保存形式の健全性は `tools/IdeaNest.Smoke` のスモークテストで確認可能 (クロスプラットフォーム)。
