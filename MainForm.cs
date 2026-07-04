using System.Diagnostics;
using System.Globalization;

namespace MetaLinkCompatTool;

public sealed class MainForm : Form
{
    private readonly Color _bg = Color.FromArgb(16, 20, 28);
    private readonly Color _card = Color.FromArgb(27, 34, 48);
    private readonly Color _card2 = Color.FromArgb(35, 44, 62);
    private readonly Color _accent = Color.FromArgb(92, 150, 255);
    private readonly Color _accent2 = Color.FromArgb(83, 220, 180);
    private readonly Color _text = Color.FromArgb(238, 243, 250);
    private readonly Color _muted = Color.FromArgb(155, 166, 184);

    private HardwareInfo? _hardware;
    private AppLanguage _language = DetectDefaultLanguage();

    private readonly Dictionary<string, Control> _localizedControls = new();
    private readonly List<Button> _operationButtons = new();

    private readonly Panel _scrollHost = new();
    private readonly TableLayoutPanel _content = new();
    private readonly Label _adminLabel = new();
    private readonly Label _cpuLabel = new();
    private readonly Label _gpuDetailLabel = new();
    private readonly Label _runtimeLabel = new();
    private readonly ComboBox _gpuCombo = new();
    private readonly ComboBox _backupCombo = new();
    private readonly ComboBox _languageCombo = new();
    private readonly TextBox _compatStatusBox = new();
    private readonly TextBox _logBox = new();
    private readonly CheckBox _lockLocalCheck = new();
    private readonly CheckBox _encoderDefaultsCheck = new();

    public MainForm()
    {
        Text = I18n.T(_language, "AppTitle");
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 560);
        Size = new Size(1220, 820);
        BackColor = _bg;
        ForeColor = _text;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        DoubleBuffered = true;

        BuildUi();
        ApplyLanguage();
        Shown += (_, _) => RefreshAll();
        Resize += (_, _) => ResizeScrollContent();
    }

    private static AppLanguage DetectDefaultLanguage()
    {
        var name = CultureInfo.CurrentUICulture.Name;
        if (name.StartsWith("ja", StringComparison.OrdinalIgnoreCase)) return AppLanguage.JaJp;
        if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return AppLanguage.ZhCn;
        return AppLanguage.EnUs;
    }

    private string T(string key) => I18n.T(_language, key);

    private void BuildUi()
    {
        _scrollHost.Dock = DockStyle.Fill;
        _scrollHost.AutoScroll = true;
        _scrollHost.BackColor = _bg;
        Controls.Add(_scrollHost);

        _content.ColumnCount = 1;
        _content.RowCount = 3;
        _content.AutoSize = false;
        _content.Padding = new Padding(18);
        _content.BackColor = _bg;
        _content.RowStyles.Add(new RowStyle(SizeType.Absolute, 136));
        _content.RowStyles.Add(new RowStyle(SizeType.Absolute, 620));
        _content.RowStyles.Add(new RowStyle(SizeType.Absolute, 300));
        _scrollHost.Controls.Add(_content);

        _content.Controls.Add(BuildHeader(), 0, 0);
        _content.Controls.Add(BuildBody(), 0, 1);
        _content.Controls.Add(BuildLogCard(), 0, 2);
        ResizeScrollContent();
    }

    private void ResizeScrollContent()
    {
        var scrollbarAllowance = SystemInformation.VerticalScrollBarWidth + 8;
        _content.Size = new Size(
            Math.Max(980, _scrollHost.ClientSize.Width - scrollbarAllowance),
            1092);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = _bg,
            Margin = new Padding(0, 0, 0, 10)
        };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = _text,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        Bind(title, "AppTitle");
        header.Controls.Add(title, 0, 0);

        var subtitle = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = _muted,
            Font = new Font("Segoe UI", 9.6F),
            TextAlign = ContentAlignment.TopLeft,
            AutoEllipsis = true
        };
        Bind(subtitle, "Subtitle");
        header.Controls.Add(subtitle, 0, 1);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _bg,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 6, 0, 0)
        };

        _adminLabel.AutoSize = false;
        _adminLabel.Size = new Size(150, 32);
        _adminLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        _adminLabel.ForeColor = Elevation.IsAdministrator() ? _accent2 : Color.FromArgb(255, 206, 110);
        _adminLabel.TextAlign = ContentAlignment.MiddleLeft;
        toolbar.Controls.Add(_adminLabel);

        var languageLabel = CreateMutedLabel("");
        languageLabel.AutoSize = false;
        languageLabel.Size = new Size(52, 32);
        languageLabel.TextAlign = ContentAlignment.MiddleLeft;
        Bind(languageLabel, "Language");
        toolbar.Controls.Add(languageLabel);

        StyleCombo(_languageCombo);
        _languageCombo.Size = new Size(120, 32);
        _languageCombo.Dock = DockStyle.None;
        _languageCombo.DataSource = I18n.Options.ToList();
        _languageCombo.SelectedItem = I18n.Options.First(o => o.Language == _language);
        _languageCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_languageCombo.SelectedItem is LanguageOption option && option.Language != _language)
            {
                _language = option.Language;
                ApplyLanguage();
                RefreshStatus();
            }
        };
        toolbar.Controls.Add(_languageCombo);

        var refresh = CreateButton("", _accent);
        Bind(refresh, "Refresh");
        refresh.Size = new Size(112, 34);
        refresh.Dock = DockStyle.None;
        refresh.Margin = new Padding(16, 0, 0, 0);
        refresh.Click += (_, _) => RefreshAll();
        toolbar.Controls.Add(refresh);

        header.Controls.Add(toolbar, 0, 2);

        return header;
    }

    private Control BuildBody()
    {
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = _bg
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = _bg };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 250));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 370));
        left.Controls.Add(BuildDeviceCard(), 0, 0);
        left.Controls.Add(BuildCompatibilityCard(), 0, 1);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = _bg };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 430));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        right.Controls.Add(BuildActionsCard(), 0, 0);
        right.Controls.Add(BuildBackupCard(), 0, 1);

        body.Controls.Add(left, 0, 0);
        body.Controls.Add(right, 1, 0);
        return body;
    }

    private Control BuildDeviceCard()
    {
        var card = CreateCard("DetectedHardware", out var body);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, BackColor = _card };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var cpuLabel = CreateMutedLabel("");
        Bind(cpuLabel, "Cpu");
        layout.Controls.Add(cpuLabel, 0, 0);
        _cpuLabel.Text = T("NotLoaded");
        _cpuLabel.ForeColor = _text;
        _cpuLabel.Dock = DockStyle.Fill;
        layout.Controls.Add(_cpuLabel, 0, 1);

        var gpuSelectLabel = CreateMutedLabel("");
        Bind(gpuSelectLabel, "GpuSelect");
        layout.Controls.Add(gpuSelectLabel, 0, 2);
        StyleCombo(_gpuCombo);
        _gpuCombo.SelectedIndexChanged += (_, _) => UpdateGpuDetails();
        layout.Controls.Add(_gpuCombo, 0, 3);

        _gpuDetailLabel.Text = "";
        _gpuDetailLabel.ForeColor = _muted;
        _gpuDetailLabel.Dock = DockStyle.Fill;
        _gpuDetailLabel.AutoEllipsis = true;
        layout.Controls.Add(_gpuDetailLabel, 0, 4);

        body.Controls.Add(layout);
        return card;
    }

    private Control BuildCompatibilityCard()
    {
        var card = CreateCard("CompatibilityStatus", out var body);
        _compatStatusBox.Dock = DockStyle.Fill;
        _compatStatusBox.Multiline = true;
        _compatStatusBox.ReadOnly = true;
        _compatStatusBox.ScrollBars = ScrollBars.Both;
        _compatStatusBox.WordWrap = false;
        _compatStatusBox.BorderStyle = BorderStyle.None;
        _compatStatusBox.BackColor = _card;
        _compatStatusBox.ForeColor = _text;
        _compatStatusBox.Font = new Font("Consolas", 9.5F);
        body.Controls.Add(_compatStatusBox);
        return card;
    }

    private Control BuildActionsCard()
    {
        var card = CreateCard("Actions", out var body);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            BackColor = _card
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        for (var i = 0; i < 5; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        }
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        AddActionButton(layout, "ApplyPatch", _accent, 0, 0, ApplyPatchClicked);
        AddActionButton(layout, "Rollback", Color.FromArgb(255, 160, 105), 1, 0, RollbackClicked);
        AddActionButton(layout, "KillMeta", Color.FromArgb(255, 110, 120), 0, 1, KillMetaClicked);
        AddActionButton(layout, "RestartMeta", _accent2, 1, 1, RestartMetaClicked);
        AddActionButton(layout, "StartHighwind", Color.FromArgb(130, 185, 255), 0, 2, StartHighwindClicked);
        AddActionButton(layout, "LaunchClient", Color.FromArgb(130, 185, 255), 1, 2, LaunchClientClicked);
        AddActionButton(layout, "InstallAutostart", Color.FromArgb(148, 210, 130), 0, 3, InstallAutostartClicked);
        AddActionButton(layout, "RemoveAutostart", Color.FromArgb(185, 145, 255), 1, 3, RemoveAutostartClicked);
        AddActionButton(layout, "SetEncoder", Color.FromArgb(120, 205, 210), 0, 4, SetEncoderDefaultsClicked);
        AddActionButton(layout, "OpenFeedback", Color.FromArgb(120, 205, 210), 1, 4, OpenFeedbackClicked);

        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = _card,
            Padding = new Padding(2, 4, 2, 2),
            AutoScroll = true
        };
        _lockLocalCheck.Checked = true;
        StyleCheck(_lockLocalCheck);
        Bind(_lockLocalCheck, "LockLocal");
        _encoderDefaultsCheck.Checked = true;
        StyleCheck(_encoderDefaultsCheck);
        Bind(_encoderDefaultsCheck, "EncoderDefaults");
        options.Controls.Add(_lockLocalCheck);
        options.Controls.Add(_encoderDefaultsCheck);
        layout.Controls.Add(options, 0, 5);
        layout.SetColumnSpan(options, 2);

        _runtimeLabel.Dock = DockStyle.Fill;
        _runtimeLabel.ForeColor = _muted;
        _runtimeLabel.Text = "";
        _runtimeLabel.AutoEllipsis = true;
        layout.Controls.Add(_runtimeLabel, 0, 6);
        layout.SetColumnSpan(_runtimeLabel, 2);

        body.Controls.Add(layout);
        return card;
    }

    private Control BuildBackupCard()
    {
        var card = CreateCard("Backups", out var body);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = _card };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        StyleCombo(_backupCombo);
        layout.Controls.Add(_backupCombo, 0, 0);

        var hint = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = _muted,
            AutoEllipsis = true
        };
        Bind(hint, "BackupHint");
        layout.Controls.Add(hint, 0, 1);

        var open = CreateButton("", _card2);
        Bind(open, "OpenBackups");
        open.Click += (_, _) => OpenFolder(MetaPaths.BackupRoot);
        layout.Controls.Add(open, 0, 2);
        body.Controls.Add(layout);
        return card;
    }

    private Control BuildLogCard()
    {
        var card = CreateCard("Log", out var body);
        _logBox.Dock = DockStyle.Fill;
        _logBox.Multiline = true;
        _logBox.ReadOnly = true;
        _logBox.ScrollBars = ScrollBars.Both;
        _logBox.WordWrap = false;
        _logBox.BorderStyle = BorderStyle.None;
        _logBox.BackColor = _card;
        _logBox.ForeColor = _text;
        _logBox.Font = new Font("Consolas", 9.5F);
        body.Controls.Add(_logBox);
        return card;
    }

    private Panel CreateCard(string titleKey, out Panel body)
    {
        var outer = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8),
            Padding = new Padding(18),
            BackColor = _card
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = _card
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outer.Controls.Add(layout);

        var title = new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = _text,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        Bind(title, titleKey);
        layout.Controls.Add(title, 0, 0);

        body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _card
        };
        layout.Controls.Add(body, 0, 1);
        return outer;
    }

    private Button CreateButton(string text, Color color)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = color,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(6)
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private void AddActionButton(TableLayoutPanel layout, string key, Color color, int col, int row, EventHandler handler)
    {
        var button = CreateButton("", color);
        Bind(button, key);
        button.Click += handler;
        _operationButtons.Add(button);
        layout.Controls.Add(button, col, row);
    }

    private Label CreateMutedLabel(string text)
    {
        return new Label
        {
            Text = text,
            ForeColor = _muted,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
        };
    }

    private void StyleCombo(ComboBox combo)
    {
        combo.Dock = DockStyle.Fill;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.BackColor = _card2;
        combo.ForeColor = _text;
        combo.FlatStyle = FlatStyle.Flat;
        combo.Font = new Font("Segoe UI", 9.5F);
        combo.Margin = new Padding(2, 4, 2, 6);
    }

    private void StyleCheck(CheckBox check)
    {
        check.AutoSize = true;
        check.ForeColor = _muted;
        check.Margin = new Padding(4, 4, 18, 4);
    }

    private void Bind(Control control, string key)
    {
        control.Tag = key;
        _localizedControls[key + "#" + _localizedControls.Count] = control;
    }

    private void ApplyLanguage()
    {
        Text = T("AppTitle");
        foreach (var control in _localizedControls.Values)
        {
            if (control.Tag is string key)
            {
                control.Text = T(key);
            }
        }

        _adminLabel.Text = Elevation.IsAdministrator() ? T("Admin") : T("Standard");
        _adminLabel.ForeColor = Elevation.IsAdministrator() ? _accent2 : Color.FromArgb(255, 206, 110);
        if (_hardware is null)
        {
            _cpuLabel.Text = T("NotLoaded");
        }
        UpdateGpuDetails();
    }

    private void RefreshAll()
    {
        try
        {
            _hardware = DeviceInfoService.GetHardwareInfo();
            _cpuLabel.Text = _hardware.Cpu.Name;
            _gpuCombo.DataSource = _hardware.Gpus.ToList();

            var preferred = _hardware.Gpus
                .Select((gpu, index) => new { gpu, index, score = GetGpuPreferenceScore(gpu) })
                .OrderByDescending(x => x.score)
                .FirstOrDefault();
            if (preferred is not null)
            {
                _gpuCombo.SelectedIndex = preferred.index;
            }

            UpdateGpuDetails();
            RefreshStatus();
            RefreshBackups();
            Log(T("StatusRefreshed"));
        }
        catch (Exception ex)
        {
            ShowError(T("RefreshFailed"), ex);
        }
    }

    private static int GetGpuPreferenceScore(GpuInfo gpu)
    {
        var name = gpu.Name;
        if (gpu.Vendor.Equals("NVIDIA", StringComparison.OrdinalIgnoreCase) && name.Contains("RTX", StringComparison.OrdinalIgnoreCase)) return 100;
        if (gpu.Vendor.Equals("NVIDIA", StringComparison.OrdinalIgnoreCase)) return 90;
        if (gpu.Vendor.Equals("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase)) return 80;
        if (gpu.Vendor.Equals("Intel", StringComparison.OrdinalIgnoreCase)) return 20;
        if (name.Contains("Virtual", StringComparison.OrdinalIgnoreCase) || name.Contains("Display Adapter", StringComparison.OrdinalIgnoreCase)) return 0;
        return 10;
    }

    private void RefreshStatus()
    {
        if (_hardware is null || _gpuCombo.SelectedItem is not GpuInfo gpu)
        {
            return;
        }

        var lines = new List<string>
        {
            $"{T("CompatFiles")}:"
        };

        foreach (var status in CompatibilityService.GetStatus(_hardware.Cpu, gpu))
        {
            lines.Add($"- {status.Path}");
            lines.Add($"  {T("Exists")}={status.Exists}  {T("ReadOnly")}={status.ReadOnly}  {T("LastWrite")}={status.LastWriteTime}");
            lines.Add($"  GPU: {T("Whitelist")}={status.HasGpuWhiteList}  {T("MinSpec")}={status.HasGpuMinSpec}");
            lines.Add($"  CPU: {T("Whitelist")}={status.HasCpuWhiteList}  {T("MinSpec")}={status.HasCpuMinSpec}");
        }

        lines.Add("");
        lines.Add($"{T("Encoder")}:");
        lines.Add(CompatibilityService.GetEncoderStatus());
        _compatStatusBox.Text = string.Join(Environment.NewLine, lines);
        _runtimeLabel.Text = MetaRuntimeService.GetRuntimeStatus();
    }

    private void RefreshBackups()
    {
        var backups = BackupStore.Load().ToList();
        _backupCombo.DataSource = backups;
        _backupCombo.DisplayMember = nameof(BackupSet.DisplayName);
    }

    private void UpdateGpuDetails()
    {
        if (_gpuCombo.SelectedItem is not GpuInfo gpu)
        {
            _gpuDetailLabel.Text = T("NoGpu");
            return;
        }

        var vram = gpu.AdapterRamBytes > 0 ? $"{gpu.AdapterRamBytes / 1024 / 1024} MB" : T("Unknown");
        _gpuDetailLabel.Text =
            $"{T("Vendor")}: {gpu.Vendor}{Environment.NewLine}" +
            $"{T("Pid")}: {gpu.Pid}{Environment.NewLine}" +
            $"{T("Driver")}: {gpu.DriverVersion}{Environment.NewLine}" +
            $"{T("Vram")}: {vram}{Environment.NewLine}" +
            gpu.PnpDeviceId;
        if (_hardware is not null)
        {
            RefreshStatus();
        }
    }

    private void ApplyPatchClicked(object? sender, EventArgs e)
    {
        if (!RequireAdministrator(T("NeedAdminWrite")))
        {
            return;
        }

        if (_hardware is null || _gpuCombo.SelectedItem is not GpuInfo gpu)
        {
            MessageBox.Show(this, T("RefreshHardwareFirst"), T("NoHardware"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        RunOperation(T("ApplyStarted"), T("ApplyDone"), () =>
        {
            var result = CompatibilityService.Apply(_hardware.Cpu, gpu, _lockLocalCheck.Checked, _encoderDefaultsCheck.Checked);
            MetaRuntimeService.StartHighwind();
            return result.Messages.Concat([$"{T("BackupCreated")}: {result.Backup.Id}", T("HighwindChecked")]);
        });
    }

    private void RollbackClicked(object? sender, EventArgs e)
    {
        if (!RequireAdministrator(T("NeedAdminRollback")))
        {
            return;
        }

        if (_backupCombo.SelectedItem is not BackupSet backup)
        {
            MessageBox.Show(this, T("NoBackup"), T("Rollback"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        RunOperation(T("RollbackStarted"), T("RollbackDone"), () =>
        {
            BackupStore.Restore(backup);
            return [$"{T("RestoredBackup")}: {backup.Id}"];
        });
    }

    private void KillMetaClicked(object? sender, EventArgs e)
    {
        if (!RequireAdministrator(T("NeedAdminStop")))
        {
            return;
        }

        RunOperation(T("KillStarted"), T("KillDone"), () =>
        {
            MetaRuntimeService.KillMeta();
            return [T("KillDone")];
        });
    }

    private void RestartMetaClicked(object? sender, EventArgs e)
    {
        if (!RequireAdministrator(T("NeedAdminRestart")))
        {
            return;
        }

        RunOperation(T("RestartStarted"), T("RestartDone"), () =>
        {
            MetaRuntimeService.RestartMeta();
            return [T("RestartDone")];
        });
    }

    private void StartHighwindClicked(object? sender, EventArgs e)
    {
        RunOperation(T("StartHighwind"), T("HighwindStarted"), () =>
        {
            MetaRuntimeService.StartHighwind();
            return [T("HighwindStarted")];
        });
    }

    private void LaunchClientClicked(object? sender, EventArgs e)
    {
        RunOperation(T("LaunchClient"), T("ClientLaunch"), () =>
        {
            MetaRuntimeService.LaunchMetaClient();
            return [T("ClientLaunch")];
        });
    }

    private void InstallAutostartClicked(object? sender, EventArgs e)
    {
        RunOperation(T("InstallAutostart"), T("AutostartInstalled"), () =>
        {
            MetaRuntimeService.InstallHighwindAutostart();
            return [T("AutostartInstalled")];
        });
    }

    private void RemoveAutostartClicked(object? sender, EventArgs e)
    {
        RunOperation(T("RemoveAutostart"), T("AutostartRemoved"), () =>
        {
            MetaRuntimeService.RemoveHighwindAutostart();
            return [T("AutostartRemoved")];
        });
    }

    private void SetEncoderDefaultsClicked(object? sender, EventArgs e)
    {
        RunOperation(T("SetEncoder"), T("EncoderSet"), () =>
        {
            CompatibilityService.SetEncoderDefaults();
            return [T("EncoderSet")];
        });
    }

    private void OpenFeedbackClicked(object? sender, EventArgs e)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "META_FEEDBACK_SUGGESTIONS.txt");
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "META_FEEDBACK_SUGGESTIONS.txt");
        }
        OpenPath(path);
    }

    private void RunOperation(string started, string completed, Func<IEnumerable<string>> work)
    {
        SetButtonsEnabled(false);
        Log(started);
        Task.Run(() =>
        {
            try
            {
                var messages = work().ToArray();
                BeginInvoke(() =>
                {
                    foreach (var message in messages)
                    {
                        Log(message);
                    }
                    RefreshStatus();
                    RefreshBackups();
                    Log(completed);
                });
            }
            catch (Exception ex)
            {
                BeginInvoke(() => ShowError(started, ex));
            }
            finally
            {
                BeginInvoke(() => SetButtonsEnabled(true));
            }
        });
    }

    private bool RequireAdministrator(string reason)
    {
        if (Elevation.IsAdministrator())
        {
            return true;
        }

        var result = MessageBox.Show(
            this,
            $"{reason}{Environment.NewLine}{Environment.NewLine}{T("RelaunchAsAdmin")}",
            T("AdminRequired"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            try
            {
                Elevation.RelaunchElevated();
                Close();
            }
            catch (Exception ex)
            {
                ShowError(T("AdminRequired"), ex);
            }
        }

        return false;
    }

    private void SetButtonsEnabled(bool enabled)
    {
        foreach (var button in _operationButtons)
        {
            button.Enabled = enabled;
        }
    }

    private void Log(string message)
    {
        _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void ShowError(string title, Exception ex)
    {
        Log($"{title}: {ex.Message}");
        MessageBox.Show(this, ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        OpenPath(path);
    }

    private void OpenPath(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            MessageBox.Show($"{T("PathNotFound")}: {path}", T("OpenPath"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
