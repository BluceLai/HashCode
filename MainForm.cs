using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace HashCode;

public sealed class MainForm : Form
{
    private const string Preset4026 = "4026";
    private const string Preset4024 = "4024";
    private const string PresetCustom = "自訂";

    private readonly AppSettings _settings;
    private readonly HashComparisonService _comparisonService = new();
    private readonly ComparisonLogService _logService = new();
    private string _customTargetDirectory = @"C:\HashCode\UnCheck";
    private string _activeTargetPreset = PresetCustom;
    private bool _loadingSettings;

    private readonly TextBox _goldenDirectoryInput = new();
    private readonly TextBox _goldenFileNameInput = new();
    private readonly ComboBox _targetPresetInput = new();
    private readonly TextBox _targetDirectoryInput = new();
    private readonly TextBox _targetFileNameInput = new();
    private readonly CheckBox _allowDifferentNamesInput = new();
    private readonly CheckedListBox _ignoredEntriesInput = new();
    private readonly Label _entryListWarningLabel = new();
    private readonly TextBox _logDirectoryInput = new();
    private readonly TextBox _logNamePrefixInput = new();
    private readonly DataGridView _resultGrid = new();
    private readonly Label _summaryLabel = new();
    private readonly ToolStripStatusLabel _statusLabel = new();

    public MainForm()
    {
        _settings = AppSettings.Load();

        Text = "HashCode .tpzip MD5 校驗工具";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1120, 720);
        Size = new Size(1280, 800);
        Font = new Font("Microsoft JhengHei UI", 9F);

        BuildLayout();
        LoadSettingsToUi();
        LoadSavedEntryList();
        CheckGoldenEntryListChange();
        SetTargetFileNameState();

        FormClosing += OnFormClosing;
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(14),
            BackColor = Color.FromArgb(246, 248, 250)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        Controls.Add(root);

        root.Controls.Add(BuildSettingsPanel(), 0, 0);
        root.Controls.Add(BuildResultPanel(), 1, 0);

        var status = new StatusStrip
        {
            SizingGrip = false,
            BackColor = Color.White
        };
        _statusLabel.Text = "就緒";
        status.Items.Add(_statusLabel);
        root.SetColumnSpan(status, 2);
        root.Controls.Add(status, 0, 1);
    }

    private Control BuildSettingsPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(16)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true
        };
        panel.Controls.Add(layout);

        layout.Controls.Add(BuildSectionTitle("參數設定"));
        layout.Controls.Add(BuildPathPicker("Golden 路徑", _goldenDirectoryInput, ChooseGoldenDirectory));
        layout.Controls.Add(BuildFilePicker("Golden 檔名 (.tpzip)", _goldenFileNameInput, ChooseGoldenFile));

        _targetPresetInput.DropDownStyle = ComboBoxStyle.DropDownList;
        _targetPresetInput.Items.AddRange(new object[] { PresetCustom, Preset4026, Preset4024 });
        _targetPresetInput.SelectedIndexChanged += (_, _) => ApplyTargetPreset();
        layout.Controls.Add(BuildLabeledControl("待檢查路徑選項", _targetPresetInput));
        layout.Controls.Add(BuildPathPicker("待檢查路徑", _targetDirectoryInput, ChooseTargetDirectory));

        _allowDifferentNamesInput.Text = "Golden 與待檢查檔名可以不同";
        _allowDifferentNamesInput.AutoSize = true;
        _allowDifferentNamesInput.CheckedChanged += (_, _) => SetTargetFileNameState();
        layout.Controls.Add(_allowDifferentNamesInput);
        layout.Controls.Add(BuildFilePicker("待檢查檔名 (.tpzip)", _targetFileNameInput, ChooseTargetFile));

        var preloadButton = new Button
        {
            Text = "預讀 Golden 內容",
            Height = 36,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 10, 0, 6)
        };
        preloadButton.Click += (_, _) => LoadGoldenEntries();
        layout.Controls.Add(preloadButton);

        var ignoreLabel = new Label
        {
            Text = "內部檔案清單（打勾=忽略判斷）",
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 4)
        };
        layout.Controls.Add(ignoreLabel);

        _entryListWarningLabel.AutoSize = false;
        _entryListWarningLabel.Height = 42;
        _entryListWarningLabel.Dock = DockStyle.Top;
        _entryListWarningLabel.TextAlign = ContentAlignment.MiddleLeft;
        _entryListWarningLabel.Padding = new Padding(8, 0, 8, 0);
        _entryListWarningLabel.BackColor = Color.FromArgb(255, 247, 205);
        _entryListWarningLabel.ForeColor = Color.FromArgb(113, 63, 18);
        _entryListWarningLabel.Visible = false;
        layout.Controls.Add(_entryListWarningLabel);

        _ignoredEntriesInput.CheckOnClick = true;
        _ignoredEntriesInput.Height = 170;
        _ignoredEntriesInput.Dock = DockStyle.Top;
        layout.Controls.Add(_ignoredEntriesInput);

        layout.Controls.Add(BuildSectionTitle("Log 設定"));
        layout.Controls.Add(BuildPathPicker("Log 路徑", _logDirectoryInput, ChooseLogDirectory));
        layout.Controls.Add(BuildLabeledControl("Log 名稱前綴", _logNamePrefixInput));

        var checkButton = new Button
        {
            Text = "檢查",
            Height = 42,
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(26, 115, 232),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 12, 0, 4)
        };
        checkButton.FlatAppearance.BorderSize = 0;
        checkButton.Click += (_, _) => RunComparison();
        layout.Controls.Add(checkButton);

        var secondaryActions = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 4, 0, 0)
        };

        var saveButton = new Button { Text = "儲存設定", Width = 92, Height = 32 };
        saveButton.Click += (_, _) => SaveSettings();
        secondaryActions.Controls.Add(saveButton);

        var openLogButton = new Button { Text = "開啟 Log", Width = 92, Height = 32 };
        openLogButton.Click += (_, _) => OpenLogDirectory();
        secondaryActions.Controls.Add(openLogButton);

        var clearButton = new Button { Text = "清除結果", Width = 92, Height = 32 };
        clearButton.Click += (_, _) => ClearResults();
        secondaryActions.Controls.Add(clearButton);
        layout.Controls.Add(secondaryActions);

        return panel;
    }

    private Control BuildResultPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(16)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(layout);

        layout.Controls.Add(BuildSectionTitle("校驗結果"));

        _summaryLabel.Text = "尚未檢查";
        _summaryLabel.AutoSize = false;
        _summaryLabel.Height = 42;
        _summaryLabel.Dock = DockStyle.Top;
        _summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
        _summaryLabel.Padding = new Padding(10, 0, 10, 0);
        _summaryLabel.BackColor = Color.FromArgb(232, 240, 254);
        layout.Controls.Add(_summaryLabel);

        _resultGrid.Dock = DockStyle.Fill;
        _resultGrid.AllowUserToAddRows = false;
        _resultGrid.AllowUserToDeleteRows = false;
        _resultGrid.ReadOnly = true;
        _resultGrid.MultiSelect = false;
        _resultGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _resultGrid.RowHeadersVisible = false;
        _resultGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _resultGrid.BackgroundColor = Color.White;
        _resultGrid.BorderStyle = BorderStyle.FixedSingle;
        _resultGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "EntryName",
            HeaderText = "內部檔案",
            Width = 250
        });
        _resultGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "GoldenHash",
            HeaderText = "Golden HashCode",
            Width = 245
        });
        _resultGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TargetHash",
            HeaderText = "待檢查 HashCode",
            Width = 245
        });
        _resultGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Result",
            HeaderText = "結果",
            Width = 70
        });
        _resultGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Note",
            HeaderText = "說明",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        layout.Controls.Add(_resultGrid);

        return panel;
    }

    private static Label BuildSectionTitle(string text) =>
        new()
        {
            Text = text,
            Dock = DockStyle.Top,
            AutoSize = true,
            Font = new Font("Microsoft JhengHei UI", 10F, FontStyle.Bold),
            Margin = new Padding(0, 8, 0, 8)
        };

    private static Control BuildLabeledControl(string labelText, Control input)
    {
        var layout = new TableLayoutPanel
        {
            RowCount = 2,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };

        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 4)
        };

        input.Dock = DockStyle.Top;
        layout.Controls.Add(label, 0, 0);
        layout.Controls.Add(input, 0, 1);
        return layout;
    }

    private static Control BuildPathPicker(string labelText, TextBox input, EventHandler browseHandler)
    {
        var row = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Top,
            AutoSize = true
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74));
        input.Dock = DockStyle.Top;
        row.Controls.Add(input, 0, 0);

        var button = new Button
        {
            Text = "選擇",
            Dock = DockStyle.Top,
            Height = 28
        };
        button.Click += browseHandler;
        row.Controls.Add(button, 1, 0);
        return BuildLabeledControl(labelText, row);
    }

    private static Control BuildFilePicker(string labelText, TextBox input, EventHandler browseHandler)
    {
        var row = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Top,
            AutoSize = true
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74));
        input.Dock = DockStyle.Top;
        row.Controls.Add(input, 0, 0);

        var button = new Button
        {
            Text = "選擇",
            Dock = DockStyle.Top,
            Height = 28
        };
        button.Click += browseHandler;
        row.Controls.Add(button, 1, 0);
        return BuildLabeledControl(labelText, row);
    }

    private void LoadSettingsToUi()
    {
        _loadingSettings = true;
        _goldenDirectoryInput.Text = _settings.GoldenDirectory;
        _goldenFileNameInput.Text = _settings.GoldenFileName;
        _targetFileNameInput.Text = _settings.TargetFileName;
        _allowDifferentNamesInput.Checked = _settings.AllowDifferentFileNames;
        _logDirectoryInput.Text = _settings.LogDirectory;
        _logNamePrefixInput.Text = _settings.LogNamePrefix;
        _customTargetDirectory = NormalizeDirectory(
            string.IsNullOrWhiteSpace(_settings.CustomTargetDirectory)
                ? _settings.TargetDirectory
                : _settings.CustomTargetDirectory,
            @"C:\HashCode\UnCheck");
        var targetPreset = _targetPresetInput.Items.Contains(_settings.TargetPathPreset)
            ? _settings.TargetPathPreset
            : PresetCustom;
        _activeTargetPreset = targetPreset;
        _targetPresetInput.SelectedItem = targetPreset;
        _targetDirectoryInput.Text = targetPreset == PresetCustom
            ? _customTargetDirectory
            : GetPresetTargetDirectory(targetPreset);
        _loadingSettings = false;
    }

    private void ApplyUiToSettings()
    {
        _settings.GoldenDirectory = NormalizeDirectory(_goldenDirectoryInput.Text, @"C:\HashCode\Golden");
        _settings.GoldenFileName = _goldenFileNameInput.Text.Trim();
        var targetPreset = _targetPresetInput.SelectedItem?.ToString() ?? PresetCustom;
        if (targetPreset == PresetCustom)
        {
            _customTargetDirectory = NormalizeDirectory(_targetDirectoryInput.Text, @"C:\HashCode\UnCheck");
        }

        _settings.TargetPathPreset = targetPreset;
        _settings.CustomTargetDirectory = _customTargetDirectory;
        _settings.TargetDirectory = targetPreset == PresetCustom
            ? _customTargetDirectory
            : GetPresetTargetDirectory(targetPreset);
        _settings.AllowDifferentFileNames = _allowDifferentNamesInput.Checked;
        _settings.TargetFileName = _allowDifferentNamesInput.Checked
            ? _targetFileNameInput.Text.Trim()
            : _goldenFileNameInput.Text.Trim();
        _settings.LogDirectory = NormalizeDirectory(_logDirectoryInput.Text, @"C:\HashCode\logs");
        _settings.LogNamePrefix = string.IsNullOrWhiteSpace(_logNamePrefixInput.Text)
            ? "log"
            : _logNamePrefixInput.Text.Trim();
        _settings.IgnoredEntries = _ignoredEntriesInput.CheckedItems
            .Cast<object>()
            .Select(item => item.ToString() ?? string.Empty)
            .Where(item => item.Length > 0)
            .ToList();
        _settings.LastGoldenEntries = GetDisplayedEntryNames();
    }

    private void ApplyTargetPreset()
    {
        if (_loadingSettings)
        {
            return;
        }

        var preset = _targetPresetInput.SelectedItem?.ToString();
        if (_activeTargetPreset == PresetCustom && !string.IsNullOrWhiteSpace(_targetDirectoryInput.Text))
        {
            _customTargetDirectory = _targetDirectoryInput.Text.Trim();
        }

        _targetDirectoryInput.Text = preset == PresetCustom
            ? _customTargetDirectory
            : GetPresetTargetDirectory(preset);
        _activeTargetPreset = preset ?? PresetCustom;
    }

    private void SetTargetFileNameState()
    {
        _targetFileNameInput.Enabled = _allowDifferentNamesInput.Checked;
        if (!_allowDifferentNamesInput.Checked)
        {
            _targetFileNameInput.Text = _goldenFileNameInput.Text;
        }
    }

    private void LoadGoldenEntries()
    {
        try
        {
            ApplyUiToSettings();
            _settings.Save();
            _ignoredEntriesInput.Items.Clear();

            var goldenPath = BuildPackagePath(_settings.GoldenDirectory, _settings.GoldenFileName);
            var entries = _comparisonService.ReadEntries(goldenPath);

            ShowEntryListChangeWarning(_settings.LastGoldenEntries, entries.Select(entry => entry.EntryName));
            PopulateIgnoredEntries(entries.Select(entry => entry.EntryName));
            _settings.LastGoldenEntries = entries.Select(entry => entry.EntryName).ToList();
            _settings.Save();

            _statusLabel.Text = $"已預讀 Golden：{entries.Count} 個內部檔案";
        }
        catch (Exception ex)
        {
            ShowError("預讀 Golden 失敗", ex);
        }
    }

    private void RunComparison()
    {
        try
        {
            ApplyUiToSettings();
            _settings.Save();

            var goldenPath = BuildPackagePath(_settings.GoldenDirectory, _settings.GoldenFileName);
            var targetPath = BuildPackagePath(_settings.TargetDirectory, _settings.TargetFileName);
            var summary = _comparisonService.Compare(
                goldenPath,
                targetPath,
                _settings.IgnoredEntries);
            PopulateResults(summary);

            var logPath = _logService.Append(_settings, summary);
            _statusLabel.Text = $"檢查完成：{(summary.IsSame ? "OK" : "NG")}，Log 已寫入 {logPath}";
        }
        catch (Exception ex)
        {
            ShowError("檢查失敗", ex);
        }
    }

    private void PopulateResults(ComparisonSummary summary)
    {
        _resultGrid.Rows.Clear();

        foreach (var row in summary.Rows)
        {
            var index = _resultGrid.Rows.Add(
                row.EntryName,
                row.GoldenHashCode,
                row.TargetHashCode,
                row.Result,
                row.Note);

            var gridRow = _resultGrid.Rows[index];
            if (row.Result == "OK")
            {
                gridRow.DefaultCellStyle.BackColor = Color.FromArgb(218, 244, 226);
                gridRow.DefaultCellStyle.ForeColor = Color.FromArgb(20, 83, 45);
            }
            else
            {
                gridRow.DefaultCellStyle.BackColor = Color.FromArgb(255, 221, 221);
                gridRow.DefaultCellStyle.ForeColor = Color.FromArgb(126, 34, 34);
            }
        }

        _summaryLabel.Text = summary.IsSame
            ? $"OK：Golden 與待檢查檔案一致。GoldenHashCode={summary.GoldenPackageHashCode}"
            : $"NG：Golden 與待檢查檔案不一致。GoldenHashCode={summary.GoldenPackageHashCode}，unCheckGoldenHashCode={summary.TargetPackageHashCode}";
        _summaryLabel.BackColor = summary.IsSame
            ? Color.FromArgb(218, 244, 226)
            : Color.FromArgb(255, 221, 221);
        _summaryLabel.ForeColor = summary.IsSame
            ? Color.FromArgb(20, 83, 45)
            : Color.FromArgb(126, 34, 34);
    }

    private void LoadSavedEntryList()
    {
        if (_settings.LastGoldenEntries.Count > 0)
        {
            PopulateIgnoredEntries(_settings.LastGoldenEntries);
        }
    }

    private void CheckGoldenEntryListChange()
    {
        try
        {
            var goldenPath = BuildPackagePath(_settings.GoldenDirectory, _settings.GoldenFileName);
            if (string.IsNullOrWhiteSpace(_settings.GoldenFileName) || !File.Exists(goldenPath))
            {
                return;
            }

            var currentEntries = _comparisonService.ReadEntries(goldenPath)
                .Select(entry => entry.EntryName)
                .ToList();
            ShowEntryListChangeWarning(_settings.LastGoldenEntries, currentEntries);

            if (currentEntries.Count > 0)
            {
                PopulateIgnoredEntries(currentEntries);
            }
        }
        catch
        {
            _entryListWarningLabel.Text = "目前無法讀取 Golden 內部清單，已先顯示上次儲存的清單。";
            _entryListWarningLabel.Visible = _settings.LastGoldenEntries.Count > 0;
        }
    }

    private void PopulateIgnoredEntries(IEnumerable<string> entryNames)
    {
        var ignored = new HashSet<string>(_settings.IgnoredEntries, StringComparer.OrdinalIgnoreCase);
        _ignoredEntriesInput.Items.Clear();

        foreach (var entryName in entryNames
                     .Where(entryName => !string.IsNullOrWhiteSpace(entryName))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(entryName => entryName, StringComparer.OrdinalIgnoreCase))
        {
            _ignoredEntriesInput.Items.Add(entryName, ignored.Contains(entryName));
        }
    }

    private void ShowEntryListChangeWarning(IEnumerable<string> previousEntries, IEnumerable<string> currentEntries)
    {
        var previous = previousEntries
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var current = currentEntries
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (previous.Count == 0)
        {
            _entryListWarningLabel.Visible = false;
            return;
        }

        var added = current.Except(previous, StringComparer.OrdinalIgnoreCase).ToList();
        var missing = previous.Except(current, StringComparer.OrdinalIgnoreCase).ToList();

        if (added.Count == 0 && missing.Count == 0)
        {
            _entryListWarningLabel.Visible = false;
            return;
        }

        _entryListWarningLabel.Text = $"內部檔案清單與上次開啟不同：新增 {added.Count} 個、缺少 {missing.Count} 個，請確認忽略項目。";
        _entryListWarningLabel.Visible = true;
    }

    private List<string> GetDisplayedEntryNames() =>
        _ignoredEntriesInput.Items
            .Cast<object>()
            .Select(item => item.ToString() ?? string.Empty)
            .Where(item => item.Length > 0)
            .ToList();

    private void SaveSettings()
    {
        ApplyUiToSettings();
        _settings.Save();
        _statusLabel.Text = "設定已儲存";
    }

    private void ClearResults()
    {
        _resultGrid.Rows.Clear();
        _summaryLabel.Text = "尚未檢查";
        _summaryLabel.BackColor = Color.FromArgb(232, 240, 254);
        _summaryLabel.ForeColor = SystemColors.ControlText;
        _statusLabel.Text = "結果已清除";
    }

    private void ChooseGoldenDirectory(object? sender, EventArgs e) =>
        ChooseDirectory(_goldenDirectoryInput, "選擇 Golden 路徑");

    private void ChooseTargetDirectory(object? sender, EventArgs e)
    {
        _targetPresetInput.SelectedItem = PresetCustom;
        ChooseDirectory(_targetDirectoryInput, "選擇待檢查路徑");
        _customTargetDirectory = NormalizeDirectory(_targetDirectoryInput.Text, @"C:\HashCode\UnCheck");
    }

    private void ChooseLogDirectory(object? sender, EventArgs e) =>
        ChooseDirectory(_logDirectoryInput, "選擇 Log 路徑");

    private void ChooseGoldenFile(object? sender, EventArgs e)
    {
        ChoosePackageFile(_goldenDirectoryInput, _goldenFileNameInput);
        SetTargetFileNameState();
    }

    private void ChooseTargetFile(object? sender, EventArgs e)
    {
        _targetPresetInput.SelectedItem = PresetCustom;
        ChoosePackageFile(_targetDirectoryInput, _targetFileNameInput);
        _customTargetDirectory = NormalizeDirectory(_targetDirectoryInput.Text, @"C:\HashCode\UnCheck");
    }

    private void ChooseDirectory(TextBox input, string description)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = description,
            SelectedPath = Directory.Exists(input.Text) ? input.Text : @"C:\",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            input.Text = dialog.SelectedPath;
        }
    }

    private void ChoosePackageFile(TextBox directoryInput, TextBox fileNameInput)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "TPZIP 檔案 (*.tpzip)|*.tpzip|ZIP 檔案 (*.zip)|*.zip|所有檔案 (*.*)|*.*",
            InitialDirectory = Directory.Exists(directoryInput.Text) ? directoryInput.Text : @"C:\",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            directoryInput.Text = Path.GetDirectoryName(dialog.FileName) ?? directoryInput.Text;
            fileNameInput.Text = Path.GetFileName(dialog.FileName);
        }
    }

    private void OpenLogDirectory()
    {
        try
        {
            ApplyUiToSettings();
            Directory.CreateDirectory(_settings.LogDirectory);

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = _settings.LogDirectory,
                UseShellExecute = true
            };
            process.Start();
        }
        catch (Exception ex)
        {
            ShowError("開啟 Log 路徑失敗", ex);
        }
    }

    private void OnFormClosing(object? sender, CancelEventArgs e)
    {
        ApplyUiToSettings();
        _settings.Save();
    }

    private void ShowError(string title, Exception ex)
    {
        _statusLabel.Text = $"{title}：{ex.Message}";
        MessageBox.Show(this, ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private static string BuildPackagePath(string directory, string fileName)
    {
        if (Path.IsPathRooted(fileName))
        {
            return fileName;
        }

        return Path.Combine(directory, fileName);
    }

    private static string NormalizeDirectory(string value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();

    private static string GetPresetTargetDirectory(string? preset) =>
        preset switch
        {
            Preset4026 => @"C:\ProgramData\Beckhoff\TwinCAT\3.1\Boot\CurrentConfig",
            Preset4024 => @"C:\TwinCAT\3.1\Boot\CurrentConfig",
            _ => @"C:\HashCode\UnCheck"
        };
}
