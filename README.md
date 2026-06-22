# HashCode .tpzip MD5 校驗工具

這是一個 C# WinForms 桌面程式，用來比對 `.tpzip` 檔案內部檔案的 MD5，避免外層 `.tpzip` 時間戳造成整包 Hash 不一致。

## 功能

- 設定 Golden 路徑，預設 `C:\HashCode\Golden`
- 設定待檢查路徑，內建 `4026`、`4024`、自訂路徑
- Golden 與待檢查檔名可設定為相同或不同
- 預讀 Golden 內部檔案，勾選後可忽略特定檔案
- 重開程式會載入上次設定與內部檔案清單
- Golden 內部檔案清單與上次開啟不同時會顯示提醒
- 按下「檢查」後比對內部檔案 MD5
- UI 結果以綠底顯示 OK、紅底顯示 NG
- 每天一個 CSV log 檔，預設輸出到 `C:\HashCode\logs`

## Log 格式

```csv
Index,Date,Time,GoldenHashCode,unCheckGoldenHashCode,Result
1,20260610,10:10:10,ABCDEFG,ABCDEFG,OK
2,20260610,10:10:20,ABCDEFG,ABCDEFF,NG
```

## 執行

```powershell
dotnet run
```

## 建置

```powershell
dotnet build HashCode.sln
```
