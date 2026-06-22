param(
    [string]$TemplatePath = "C:\Users\BluceL\Documents\HashCode\outputs\manual-20260610-hashcode\presentations\hashcode-manuals\template.pptx",
    [string]$OutputDirectory = "C:\Users\BluceL\Documents\HashCode\manuals"
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$qaRoot = Join-Path $OutputDirectory "qa"
New-Item -ItemType Directory -Force -Path $qaRoot | Out-Null

$msoTextOrientationHorizontal = 1
$msoTrue = -1
$msoFalse = 0
$ppSaveAsOpenXMLPresentation = 24

function New-SlideSpec {
    param(
        [string]$Kind,
        [string]$Title,
        [string[]]$Lines,
        [string]$Tag = ""
    )

    [pscustomobject]@{
        Kind = $Kind
        Title = $Title
        Lines = $Lines
        Tag = $Tag
    }
}

$userManual = @(
    New-SlideSpec "cover" "HashCode .tpzip MD5 校驗工具" @("設定與操作流程手冊", "適用：Golden / UNCHECK 檔案一致性校驗")
    New-SlideSpec "content" "前言" @(
        "本工具用來比對 .tpzip 內部檔案的 MD5，而不是直接比對最外層 .tpzip 檔案。",
        "原因：最外層 .tpzip 可能帶有時間戳，同一份內容在不同時間打包會造成外層 Hash 不一致。",
        "使用者可設定 Golden 檔案、待檢查檔案、忽略項目與 Log 輸出位置。"
    )
    New-SlideSpec "toc" "目錄" @(
        "1. 基本路徑與檔名設定",
        "2. 預讀 Golden 內容與忽略項目",
        "3. 執行檢查與結果判讀",
        "4. Log 檔輸出",
        "5. 設定保存與常見狀況"
    )
    New-SlideSpec "section" "參數設定"
    New-SlideSpec "content" "Golden 檔案設定" @(
        "Golden 路徑：預設為 C:\HashCode\Golden。",
        "Golden 檔名：請選擇或輸入含副檔名的 .tpzip 檔案名稱。",
        "Golden 代表標準檔案，後續所有檢查會以 Golden 內部檔案作為主要比對基準。",
        "按下「預讀 Golden 內容」可載入內部檔案清單。"
    )
    New-SlideSpec "content" "待檢查檔案設定" @(
        "待檢查路徑選項提供：自訂、4026、4024。",
        "4026 路徑：C:\ProgramData\Beckhoff\TwinCAT\3.1\Boot\CurrentConfig。",
        "4024 路徑：C:\TwinCAT\3.1\Boot\CurrentConfig。",
        "自訂路徑會記住上次輸入的路徑，切換到 4024/4026 後再切回仍會恢復。"
    )
    New-SlideSpec "content" "檔名一致或不同" @(
        "預設情況：Golden 與待檢查檔名相同。",
        "若兩邊檔案名稱不同，勾選「Golden 與待檢查檔名可以不同」。",
        "勾選後可獨立設定待檢查檔名。",
        "若未勾選，待檢查檔名會自動跟 Golden 檔名同步。"
    )
    New-SlideSpec "content" "預讀與忽略項目" @(
        "按下「預讀 Golden 內容」後，左側會顯示 Golden .tpzip 內部檔案清單。",
        "清單中打勾的項目代表忽略判斷，該內部檔案不參與 MD5 比對。",
        "重開程式時會載入上次儲存的清單與勾選狀態。",
        "若目前 Golden 內部清單與上次不同，畫面會提醒新增或缺少的數量。"
    )
    New-SlideSpec "section" "檢查與判讀"
    New-SlideSpec "content" "執行檢查流程" @(
        "確認 Golden 路徑、Golden 檔名、待檢查路徑、待檢查檔名都正確。",
        "確認忽略項目是否符合本次檢查需求。",
        "按下「檢查」。",
        "程式會讀取兩個 .tpzip 檔案，逐一計算內部檔案 MD5 並輸出結果。"
    )
    New-SlideSpec "content" "結果判讀" @(
        "OK：Golden 與待檢查檔案的內部項目 MD5 一致。",
        "NG：MD5 不一致、待檢查缺少項目，或 UNCHECK 比 Golden 多出項目。",
        "綠底表示 OK，紅底表示 NG。",
        "UNCHECK 多出的檔案也會列出並判定 NG，不會只比較 Golden 有的項目。"
    )
    New-SlideSpec "content" "Log 輸出" @(
        "Log 路徑預設為 C:\HashCode\logs。",
        "Log 名稱前綴預設為 log，每天產生一個 CSV 檔。",
        "檔名格式：log_yyyyMMdd.csv。",
        "欄位：Index, Date, Time, GoldenHashCode, unCheckGoldenHashCode, Result。",
        "同一天多次檢查會往下追加，不會覆蓋舊紀錄。"
    )
    New-SlideSpec "faq" "常見狀況" @(
        "Q：重開程式後設定是否會消失？",
        "A：不會。程式會從 appsettings.json 載入上次設定。",
        "Q：Golden 清單變動怎麼辦？",
        "A：畫面會提醒新增/缺少數量，請重新確認忽略項目。",
        "Q：UNCHECK 多檔案會怎麼處理？",
        "A：會列出並判定 NG。"
    )
    New-SlideSpec "end" "完成" @("HashCode .tpzip MD5 校驗工具", "設定與操作流程手冊")
)

$engineerManual = @(
    New-SlideSpec "cover" "HashCode 工程師維護手冊" @("程式碼修改與維護指南", "適用：C# WinForms / .NET 9")
    New-SlideSpec "content" "系統定位" @(
        "HashCode 是一支 Windows WinForms 工具，用來檢查 .tpzip 內部檔案一致性。",
        "核心原則：忽略最外層 .tpzip 時間戳影響，改比對內部檔案 MD5。",
        "專案不依賴外部 NuGet 套件，降低部署到工程機或現場電腦的風險。"
    )
    New-SlideSpec "toc" "目錄" @(
        "1. 專案檔案結構",
        "2. UI 與設定保存流程",
        "3. Hash 比對核心邏輯",
        "4. Log 輸出與欄位格式",
        "5. 常見修改位置與驗證方式"
    )
    New-SlideSpec "section" "專案架構"
    New-SlideSpec "content" "主要檔案" @(
        "Program.cs：WinForms 啟動入口，建立 MainForm。",
        "MainForm.cs：所有 UI 控制項、事件流程、設定載入/保存與結果顯示。",
        "AppSettings.cs：appsettings.json 的資料模型與讀寫邏輯。",
        "HashComparisonService.cs：讀取 .tpzip、計算內部檔案 MD5、產生比較結果。",
        "ComparisonLogService.cs：每日 CSV Log 追加寫入與表頭維護。"
    )
    New-SlideSpec "content" "設定保存流程" @(
        "AppSettings.SettingsPath 使用 AppContext.BaseDirectory 下的 appsettings.json。",
        "程式啟動時呼叫 AppSettings.Load()，若檔案不存在則使用預設值。",
        "按「儲存設定」、按「檢查」、或關閉視窗時會將 UI 內容寫回設定檔。",
        "新增設定欄位時，建議提供安全預設值，以相容舊版 appsettings.json。"
    )
    New-SlideSpec "content" "UI 修改重點" @(
        "MainForm.BuildSettingsPanel()：左側設定區控制項。",
        "MainForm.BuildResultPanel()：右側結果表格與摘要區。",
        "LoadSettingsToUi()：設定檔載入到畫面。",
        "ApplyUiToSettings()：畫面值寫回設定模型。",
        "RunComparison()：按下「檢查」後的主要流程。"
    )
    New-SlideSpec "section" "比對核心"
    New-SlideSpec "content" "HashComparisonService.Compare()" @(
        "ReadEntries() 以 ZipFile.OpenRead() 開啟 .tpzip，逐一讀取非資料夾 entry。",
        "每個 entry 使用 MD5.HashData(stream) 計算 HashCode。",
        "Golden 會先排除使用者勾選忽略的項目。",
        "結果清單使用 Golden 與 UNCHECK 的 union，因此 UNCHECK 多出的檔案也會被列出。",
        "Result 為 OK 或 NG，Note 會標示缺少、多出或 MD5 不一致。"
    )
    New-SlideSpec "content" "比對規則" @(
        "Golden 有、UNCHECK 有，且 MD5 相同：OK。",
        "Golden 有、UNCHECK 有，但 MD5 不同：NG。",
        "Golden 有、UNCHECK 沒有：NG，說明為待檢查檔案缺少此項目。",
        "UNCHECK 有、Golden 沒有：NG，說明為只存在於待檢查檔案。",
        "被忽略的 Golden 項目不參與比對。"
    )
    New-SlideSpec "content" "Log 維護" @(
        "ComparisonLogService.GetLogFilePath() 決定每日 CSV 檔名。",
        "EnsureLogHeader() 確保第一列表頭為英文欄位名稱。",
        "GetNextIndex() 以既有資料列數決定下一筆 Index。",
        "Append() 寫入：Index, Date, Time, GoldenHashCode, unCheckGoldenHashCode, Result。",
        "若要新增欄位，需同步調整表頭、line 組合與 README。"
    )
    New-SlideSpec "content" "常見需求修改位置" @(
        "新增路徑預設值：AppSettings.cs 與 MainForm.NormalizeDirectory() 呼叫處。",
        "新增 UI 選項：MainForm 欄位宣告、BuildSettingsPanel()、LoadSettingsToUi()、ApplyUiToSettings()。",
        "調整比對規則：HashComparisonService.Compare()。",
        "調整結果顏色或欄位：MainForm.PopulateResults() 與 BuildResultPanel()。",
        "調整 Log 格式：ComparisonLogService.cs。"
    )
    New-SlideSpec "content" "建置與驗證" @(
        "建置：dotnet build HashCode.sln。",
        "執行：dotnet run。",
        "修改 UI 後需檢查設定是否能保存、重開後是否正確載入。",
        "修改比對邏輯後需準備 Golden/UNCHECK 測試檔，覆蓋 OK、MD5 NG、缺少項目、多出項目。",
        "交付前建議確認 0 warnings / 0 errors。"
    )
    New-SlideSpec "faq" "維護注意事項" @(
        "不要把外層 .tpzip 檔案 Hash 當作一致性依據。",
        "不要只比較 Golden 有的項目；UNCHECK 多出的檔案也代表差異。",
        "不要任意覆蓋使用者已保存的 appsettings.json。",
        "若新增設定欄位，需維持舊設定檔可讀取。",
        "現場機台可能沒有網路，避免加入需下載套件的相依性。"
    )
    New-SlideSpec "end" "完成" @("HashCode 工程師維護手冊", "C# WinForms / .NET 9")
)

function Get-SourceSlideIndex {
    param([string]$Kind)

    switch ($Kind) {
        "cover" { return 1 }
        "toc" { return 3 }
        "section" { return 4 }
        "faq" { return 26 }
        "end" { return 30 }
        default { return 2 }
    }
}

function Clear-SlideContent {
    param($Slide)

    for ($i = $Slide.Shapes.Count; $i -ge 1; $i--) {
        $shape = $Slide.Shapes.Item($i)
        try {
            if ($shape.HasTextFrame -eq $msoTrue) {
                if ($shape.TextFrame.HasText -eq $msoTrue) {
                    $shape.TextFrame.TextRange.Text = ""
                }
            }

            if ($shape.Type -eq 13 -or $shape.Type -eq 11 -or $shape.Type -eq 7) {
                $shape.Delete()
            }
        } catch {
        }
    }
}

function Set-TextStyle {
    param($TextRange, [int]$Size, [bool]$Bold = $false, [int]$Color = 0x202020)

    $TextRange.Font.Name = "Microsoft JhengHei UI"
    $TextRange.Font.Size = $Size
    $TextRange.Font.Bold = $(if ($Bold) { $msoTrue } else { $msoFalse })
    $TextRange.Font.Color.RGB = $Color
}

function Add-TextBox {
    param($Slide, [string]$Text, [single]$Left, [single]$Top, [single]$Width, [single]$Height, [int]$Size, [bool]$Bold = $false, [int]$Color = 0x202020)

    $box = $Slide.Shapes.AddTextbox($msoTextOrientationHorizontal, $Left, $Top, $Width, $Height)
    $box.TextFrame.WordWrap = $msoTrue
    $box.TextFrame.MarginLeft = 8
    $box.TextFrame.MarginRight = 8
    $box.TextFrame.MarginTop = 6
    $box.TextFrame.MarginBottom = 6
    $box.TextFrame.TextRange.Text = $Text
    Set-TextStyle $box.TextFrame.TextRange $Size $Bold $Color
    return $box
}

function Add-BodyLines {
    param($Slide, [string[]]$Lines)

    $text = (($Lines | ForEach-Object {
        if ($_ -match "^[0-9]+\.|^Q：|^A：|^Q:|^A:") { $_ } else { "• $_" }
    }) -join "`r")
    $body = Add-TextBox $Slide $text 82 210 1060 390 20 $false 0x202020
    $body.TextFrame.TextRange.ParagraphFormat.SpaceAfter = 8
}

function Add-SlideNumber {
    param($Slide, [int]$Index, [int]$Total)

    $box = Add-TextBox $Slide "$Index / $Total" 1090 640 100 24 10 $false 0x666666
    $box.TextFrame.TextRange.ParagraphFormat.Alignment = 3
}

function Apply-SlideContent {
    param($Slide, $Spec, [int]$Index, [int]$Total)

    Clear-SlideContent $Slide

    switch ($Spec.Kind) {
        "cover" {
            Add-TextBox $Slide $Spec.Title 70 185 1080 80 34 $true 0x202020 | Out-Null
            Add-TextBox $Slide (($Spec.Lines) -join "`r") 74 285 980 120 22 $false 0x404040 | Out-Null
        }
        "section" {
            Add-TextBox $Slide $Spec.Title 78 285 1060 86 34 $true 0x202020 | Out-Null
        }
        "toc" {
            Add-TextBox $Slide $Spec.Title 72 125 1050 64 30 $true 0x202020 | Out-Null
            Add-BodyLines $Slide $Spec.Lines
        }
        "end" {
            Add-TextBox $Slide $Spec.Title 78 235 1060 70 34 $true 0x202020 | Out-Null
            Add-TextBox $Slide (($Spec.Lines) -join "`r") 82 325 1000 90 20 $false 0x404040 | Out-Null
        }
        default {
            Add-TextBox $Slide $Spec.Title 72 125 1050 64 28 $true 0x202020 | Out-Null
            Add-BodyLines $Slide $Spec.Lines
        }
    }

    Add-SlideNumber $Slide $Index $Total
}

function Export-QAImages {
    param($Presentation, [string]$Name)

    $dir = Join-Path $qaRoot $Name
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    for ($i = 1; $i -le $Presentation.Slides.Count; $i++) {
        $path = Join-Path $dir ("slide-{0:00}.png" -f $i)
        $Presentation.Slides.Item($i).Export($path, "PNG", 1280, 720)
    }
}

function Build-Deck {
    param($PowerPoint, [string]$OutputPath, [array]$Specs, [string]$QaName)

    if (Test-Path $OutputPath) {
        Remove-Item -LiteralPath $OutputPath -Force
    }

    $presentation = $PowerPoint.Presentations.Open($TemplatePath, $msoTrue, $msoFalse, $msoFalse)
    $originalCount = $presentation.Slides.Count

    foreach ($spec in $Specs) {
        $sourceIndex = Get-SourceSlideIndex $spec.Kind
        $presentation.Slides.Item($sourceIndex).Copy()
        $presentation.Slides.Paste($presentation.Slides.Count + 1) | Out-Null
    }

    for ($i = $originalCount; $i -ge 1; $i--) {
        $presentation.Slides.Item($i).Delete()
    }

    for ($i = 1; $i -le $Specs.Count; $i++) {
        Apply-SlideContent $presentation.Slides.Item($i) $Specs[$i - 1] $i $Specs.Count
    }

    Export-QAImages $presentation $QaName
    $presentation.SaveAs($OutputPath, $ppSaveAsOpenXMLPresentation)
    $presentation.Close()
}

$powerPoint = $null
try {
    $powerPoint = New-Object -ComObject PowerPoint.Application
    $powerPoint.Visible = $msoTrue
    $powerPoint.DisplayAlerts = 1

    Build-Deck $powerPoint (Join-Path $OutputDirectory "HashCode_設定與操作流程手冊.pptx") $userManual "user-manual"
    Build-Deck $powerPoint (Join-Path $OutputDirectory "HashCode_工程師維護手冊.pptx") $engineerManual "engineer-manual"
}
finally {
    if ($powerPoint -ne $null) {
        $powerPoint.Quit()
    }
}
