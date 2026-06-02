# IdeaNest

IdeaNest は、思いついたアイデアを素早く保存し、あとから検索・整理できる **ローカル利用前提のカード型メモツール** です。

> **このリポジトリは v0.1.0 のプロトタイプです。** 一気通貫の動作確認を最優先しており、UI の作り込みや細かな例外処理は最低限です。

## IdeaNest とは

- アイデア 1 件 = カード 1 枚
- 思いついた順に素早く追加できる
- タグで分類できる
- ピン留め / アーカイブができる
- すべてローカルの単一 JSON ファイル (`.ideanest`) に保存する

「整理する前に、まず溜める」ためのツールです。

## NoteNest との違い

| 観点 | NoteNest | IdeaNest |
| --- | --- | --- |
| 主目的 | プロジェクト管理・ノート管理 | アイデアの即時メモと検索 |
| 主単位 | プロジェクト / ノート | カード |
| 画面構成 | プロジェクト×ツリー×エディタ | カードのフラット一覧 |
| 重視点 | 構造化と進捗管理 | スピードと一覧性 |
| 想定利用 | じっくり書く | パッと書いて後で探す |

IdeaNest は NoteNest の縮小版ではなく、**「とにかく雑に放り込めるアイデアの巣 (nest)」** という別の役割を担います。

## 主な機能 (v0.1.0)

- アイデアカードの追加・編集・削除
- カード型の一覧表示 (ピン留めが上に来る / 更新日時の新しい順)
- 簡易入力欄からの即時追加 (本文だけでも可)
- 編集ダイアログでタイトル・本文・タグ・色・ピン・アーカイブを変更
- キーワード検索 (タイトル / 本文 / タグ)
- タグによる絞り込み
- アーカイブ表示の切替
- `.ideanest` ファイルへの保存・読込
- 名前を付けて保存
- 未保存変更時の確認ダイアログ / タイトルバーの `*` 表示
- 保存時に `.bak` を自動生成
- ウィンドウサイズ・検索文字列・選択中タグ・アーカイブ表示状態をファイルに記録

## 起動方法

### 必要環境

- Windows 10 / 11
- .NET 8 SDK (`dotnet --version` が `8.0.x` を返すこと)

### ビルドと起動

```powershell
git clone <this-repo>
cd ideanest
dotnet build
dotnet run --project src/IdeaNest/IdeaNest.csproj
```

> **注:** WPF は Windows 専用です。Linux / macOS では `dotnet build` できません。
> ただし、Models/Services を切り出した `tools/IdeaNest.Smoke` プロジェクトは
> クロスプラットフォームで動作し、JSON 保存・読込のラウンドトリップを検証できます。
>
> ```bash
> dotnet run --project tools/IdeaNest.Smoke
> ```

## `.ideanest` ファイルについて

- UTF-8 の JSON ファイル
- アプリ起動中に「保存」「名前を付けて保存」で生成される
- 拡張子は `.ideanest`
- 保存時、同フォルダに `*.ideanest.bak` を生成 (直前の保存内容)
- 形式は以下のとおり

```json
{
  "version": "0.1.0",
  "workspaceName": "IdeaNest",
  "ideas": [
    {
      "id": "uuid",
      "title": "アイデアのタイトル",
      "body": "アイデア本文",
      "tags": ["UI", "開発"],
      "color": "yellow",
      "isPinned": false,
      "isArchived": false,
      "createdAt": "2026-06-02T10:00:00",
      "updatedAt": "2026-06-02T10:00:00"
    }
  ],
  "settings": {
    "searchText": "",
    "selectedTag": "",
    "showArchived": false,
    "windowWidth": 1100,
    "windowHeight": 700
  }
}
```

## v0.1.0 時点の制限

- **プロトタイプ段階です。** UI の細部・キー操作・例外の見せ方は最低限です。
- 画面設定 (ウィンドウサイズや検索条件など) はファイルに保存されますが、起動時の復元は読み込んだ `.ideanest` 経由のみです。
- 並べ替えは「ピン → 更新日時降順」固定で、ユーザーによる並べ替えはできません。
- カードの色は内蔵の固定パレット (8 色) のみです。
- カードのドラッグ操作はできません。

## 対象外機能 (v0.1.0)

将来検討としており、本バージョンには含めていません。

- 画像貼り付け
- 添付ファイル
- リマインダー
- チェックリスト
- タスク管理
- カレンダー連携
- クラウド同期
- 共同編集
- Markdown プレビュー
- NoteNest との直接連携
- AI 要約
- 自動分類

## ドキュメント

- [docs/design-decisions.md](docs/design-decisions.md) - 設計判断
- [docs/backlog.md](docs/backlog.md) - v0.2.0 以降の候補
- [docs/test-scenarios.md](docs/test-scenarios.md) - 手動テストシナリオ
- [docs/operation-note.md](docs/operation-note.md) - 運用メモ
- [docs/release-notes.md](docs/release-notes.md) - リリースノート
