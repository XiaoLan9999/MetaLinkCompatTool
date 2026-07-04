using System.Diagnostics;

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
    private readonly Label _adminLabel = new();
    private readonly Label _cpuLabel = new();
    private readonly Label _gpuDetailLabel = new();
    private readonly Label _encoderLabel = new();
    private readonly Label _runtimeLabel = new();
    private readonly ComboBox _gpuCombo = new();
    private readonly ComboBox _backupCombo = new();
    private readonly TextBox _compatStatusBox = new();
    private readonly TextBox _logBox = new();
    private readonly CheckBox _lockLocalCheck = new();
    private readonly CheckBox _encoderDefaultsCheck = new();
    private readonly List<Button> _operationButtons = new();

    public MainForm()
    {
        Text = "Meta Link Compatibility Tool";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1060, 720);
        Size = new Size(1220, 800);
        BackColor = _bg;
        ForeColor = _text;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        DoubleBuffered = true;

        BuildUi();
        Shown += (_, _) => RefreshAll();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = _bg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 36));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);

        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = _bg
        };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        root.Controls.Add(main, 0, 1);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = _bg };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 46));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 54));
        left.Controls.Add(BuildDeviceCard(), 0, 0);
        left.Controls.Add(BuildCompatibilityCard(), 0, 1);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = _bg };
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        right.Controls.Add(BuildActionsCard(), 0, 0);
        right.Controls.Add(BuildBackupCard(), 0, 1);

        main.Controls.Add(left, 0, 0);
        main.Controls.Add(right, 1, 0);

        root.Controls.Add(BuildLogCard(), 0, 2);
    }

    private Control BuildHeader()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _bg,
            Padding = new Padding(0, 0, 0, 8)
        };

        var title = new Label
        {
            Text = "Meta Link Compatibility Tool",
            AutoSize = true,
            ForeColor = _text,
            Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
            Location = new Point(0, 6)
        };
        panel.Controls.Add(title);

        var subtitle = new Label
        {
            Text = "Patch Meta Horizon Link local compatibility lists, manage highwind, and recover with backups.",
            AutoSize = true,
            ForeColor = _muted,
            Location = new Point(3, 52)
        };
        panel.Controls.Add(subtitle);

        _adminLabel.AutoSize = true;
        _adminLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        _adminLabel.Location = new Point(760, 18);
        _adminLabel.ForeColor = Elevation.IsAdministrator() ? _accent2 : Color.FromArgb(255, 206, 110);
        _adminLabel.Text = Elevation.IsAdministrator() ? "Administrator mode" : "Standard user mode";
        panel.Controls.Add(_adminLabel);

        var refresh = CreateButton("Refresh", _accent);
        refresh.Size = new Size(128, 38);
        refresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        refresh.Location = new Point(1038, 15);
        refresh.Click += (_, _) => RefreshAll();
        panel.Controls.Add(refresh);

        panel.Resize += (_, _) =>
        {
            refresh.Location = new Point(panel.Width - refresh.Width - 4, 15);
            _adminLabel.Location = new Point(Math.Max(520, panel.Width - refresh.Width - 250), 22);
        };

        return panel;
    }

    private Control BuildDeviceCard()
    {
        var card = CreateCard("Detected hardware");

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1, BackColor = _card };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(CreateMutedLabel("CPU"), 0, 0);
        _cpuLabel.Text = "Not loaded";
        _cpuLabel.ForeColor = _text;
        _cpuLabel.Dock = DockStyle.Fill;
        layout.Controls.Add(_cpuLabel, 0, 1);

        layout.Controls.Add(CreateMutedLabel("GPU to add to Meta compatibility list"), 0, 2);
        StyleCombo(_gpuCombo);
        _gpuCombo.SelectedIndexChanged += (_, _) => UpdateGpuDetails();
        layout.Controls.Add(_gpuCombo, 0, 3);

        _gpuDetailLabel.Text = "";
        _gpuDetailLabel.ForeColor = _muted;
        _gpuDetailLabel.Dock = DockStyle.Fill;
        layout.Controls.Add(_gpuDetailLabel, 0, 4);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildCompatibilityCard()
    {
        var card = CreateCard("Compatibility and encoder status");
        _compatStatusBox.Dock = DockStyle.Fill;
        _compatStatusBox.Multiline = true;
        _compatStatusBox.ReadOnly = true;
        _compatStatusBox.BorderStyle = BorderStyle.None;
        _compatStatusBox.BackColor = _card;
        _compatStatusBox.ForeColor = _text;
        _compatStatusBox.Font = new Font("Consolas", 10F);
        card.Controls.Add(_compatStatusBox);
        return card;
    }

    private Control BuildActionsCard()
    {
        var card = CreateCard("Actions");
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
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        }
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        AddActionButton(layout, "Apply patch", _accent, 0, 0, ApplyPatchClicked);
        AddActionButton(layout, "Rollback selected", Color.FromArgb(255, 160, 105), 1, 0, RollbackClicked);
        AddActionButton(layout, "Kill Meta services", Color.FromArgb(255, 110, 120), 0, 1, KillMetaClicked);
        AddActionButton(layout, "Restart Meta services", _accent2, 1, 1, RestartMetaClicked);
        AddActionButton(layout, "Start highwind", Color.FromArgb(130, 185, 255), 0, 2, StartHighwindClicked);
        AddActionButton(layout, "Launch Meta client", Color.FromArgb(130, 185, 255), 1, 2, LaunchClientClicked);
        AddActionButton(layout, "Install autostart", Color.FromArgb(148, 210, 130), 0, 3, InstallAutostartClicked);
        AddActionButton(layout, "Remove autostart", Color.FromArgb(185, 145, 255), 1, 3, RemoveAutostartClicked);
        AddActionButton(layout, "Set HEVC defaults", Color.FromArgb(120, 205, 210), 0, 4, SetEncoderDefaultsClicked);
        AddActionButton(layout, "Open feedback TXT", Color.FromArgb(120, 205, 210), 1, 4, OpenFeedbackClicked);

        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = _card,
            Padding = new Padding(2, 4, 2, 2)
        };
        _lockLocalCheck.Text = "Mark local Compatibility.json read-only";
        _lockLocalCheck.Checked = true;
        StyleCheck(_lockLocalCheck);
        _encoderDefaultsCheck.Text = "Also set HEVC encoder defaults";
        _encoderDefaultsCheck.Checked = true;
        StyleCheck(_encoderDefaultsCheck);
        options.Controls.Add(_lockLocalCheck);
        options.Controls.Add(_encoderDefaultsCheck);
        layout.Controls.Add(options, 0, 5);
        layout.SetColumnSpan(options, 2);

        _runtimeLabel.Dock = DockStyle.Fill;
        _runtimeLabel.ForeColor = _muted;
        _runtimeLabel.Text = "";
        layout.Controls.Add(_runtimeLabel, 0, 6);
        layout.SetColumnSpan(_runtimeLabel, 2);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildBackupCard()
    {
        var card = CreateCard("Backups");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = _card };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        StyleCombo(_backupCombo);
        layout.Controls.Add(_backupCombo, 0, 0);

        var hint = new Label
        {
            Text = "Every Apply operation snapshots both Compatibility.json files and HKCU Oculus RemoteHeadset settings before writing.",
            Dock = DockStyle.Fill,
            ForeColor = _muted
        };
        layout.Controls.Add(hint, 0, 1);

        var open = CreateButton("Open backups folder", _card2);
        open.Click += (_, _) => OpenFolder(MetaPaths.BackupRoot);
        layout.Controls.Add(open, 0, 2);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildLogCard()
    {
        var card = CreateCard("Log");
        _logBox.Dock = DockStyle.Fill;
        _logBox.Multiline = true;
        _logBox.ReadOnly = true;
        _logBox.ScrollBars = ScrollBars.Vertical;
        _logBox.BorderStyle = BorderStyle.None;
        _logBox.BackColor = _card;
        _logBox.ForeColor = _text;
        _logBox.Font = new Font("Consolas", 10F);
        card.Controls.Add(_logBox);
        return card;
    }

    private Panel CreateCard(string title)
    {
        var outer = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8),
            Padding = new Padding(18, 52, 18, 18),
            BackColor = _card
        };

        var titleLabel = new Label
        {
            Text = title,
            AutoSize = true,
            ForeColor = _text,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
            Location = new Point(18, 16)
        };
        outer.Controls.Add(titleLabel);
        titleLabel.BringToFront();
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
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(6)
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private void AddActionButton(TableLayoutPanel layout, string text, Color color, int col, int row, EventHandler handler)
    {
        var button = CreateButton(text, color);
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
        combo.Font = new Font("Segoe UI", 10F);
        combo.Margin = new Padding(2, 4, 2, 8);
    }

    private void StyleCheck(CheckBox check)
    {
        check.AutoSize = true;
        check.ForeColor = _muted;
        check.Margin = new Padding(4, 4, 18, 4);
    }

    private void RefreshAll()
    {
        try
        {
            _hardware = DeviceInfoService.GetHardwareInfo();
            _cpuLabel.Text = _hardware.Cpu.Name;
            _gpuCombo.DataSource = _hardware.Gpus.ToList();

            var preferred = _hardware.Gpus
                .Select((gpu, index) => new { gpu, index })
                .FirstOrDefault(x => !string.Equals(x.gpu.Vendor, "Intel", StringComparison.OrdinalIgnoreCase));
            if (preferred is not null)
            {
                _gpuCombo.SelectedIndex = preferred.index;
            }

            UpdateGpuDetails();
            RefreshStatus();
            RefreshBackups();
            Log("Status refreshed.");
        }
        catch (Exception ex)
        {
            ShowError("Refresh failed", ex);
        }
    }

    private void RefreshStatus()
    {
        if (_hardware is null || _gpuCombo.SelectedItem is not GpuInfo gpu)
        {
            return;
        }

        var lines = new List<string>
        {
            "Compatibility files:"
        };

        foreach (var status in CompatibilityService.GetStatus(_hardware.Cpu, gpu))
        {
            lines.Add($"- {status.Path}");
            lines.Add($"  Exists={status.Exists}  ReadOnly={status.ReadOnly}  LastWrite={status.LastWriteTime}");
            lines.Add($"  GPU: WhiteList={status.HasGpuWhiteList}  MinSpec={status.HasGpuMinSpec}");
            lines.Add($"  CPU: WhiteList={status.HasCpuWhiteList}  MinSpec={status.HasCpuMinSpec}");
        }

        lines.Add("");
        lines.Add("Encoder:");
        lines.Add(CompatibilityService.GetEncoderStatus());
        _compatStatusBox.Text = string.Join(Environment.NewLine, lines);
        _encoderLabel.Text = CompatibilityService.GetEncoderStatus();
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
            _gpuDetailLabel.Text = "No GPU selected.";
            return;
        }

        var vram = gpu.AdapterRamBytes > 0 ? $"{gpu.AdapterRamBytes / 1024 / 1024} MB" : "unknown";
        _gpuDetailLabel.Text = $"Vendor: {gpu.Vendor}{Environment.NewLine}PID: {gpu.Pid}{Environment.NewLine}Driver: {gpu.DriverVersion}{Environment.NewLine}VRAM reported by WMI: {vram}{Environment.NewLine}{gpu.PnpDeviceId}";
        if (_hardware is not null)
        {
            RefreshStatus();
        }
    }

    private void ApplyPatchClicked(object? sender, EventArgs e)
    {
        if (!RequireAdministrator("Writing Program Files compatibility data usually requires administrator rights."))
        {
            return;
        }

        if (_hardware is null || _gpuCombo.SelectedItem is not GpuInfo gpu)
        {
            MessageBox.Show(this, "Refresh hardware first.", "No hardware", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        RunOperation("Apply patch", () =>
        {
            var result = CompatibilityService.Apply(_hardware.Cpu, gpu, _lockLocalCheck.Checked, _encoderDefaultsCheck.Checked);
            MetaRuntimeService.StartHighwind();
            return result.Messages.Concat(new[] { $"Backup created: {result.Backup.Id}", "highwind_service checked." });
        });
    }

    private void RollbackClicked(object? sender, EventArgs e)
    {
        if (!RequireAdministrator("Rollback writes compatibility files back to Meta folders."))
        {
            return;
        }

        if (_backupCombo.SelectedItem is not BackupSet backup)
        {
            MessageBox.Show(this, "No backup selected.", "Rollback", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        RunOperation("Rollback", () =>
        {
            BackupStore.Restore(backup);
            return new[] { $"Restored backup: {backup.Id}" };
        });
    }

    private void KillMetaClicked(object? sender, EventArgs e)
    {
        if (!RequireAdministrator("Stopping OVRService requires administrator rights."))
        {
            return;
        }

        RunOperation("Kill Meta services", () =>
        {
            MetaRuntimeService.KillMeta();
            return new[] { "Meta services and related processes stopped where possible." };
        });
    }

    private void RestartMetaClicked(object? sender, EventArgs e)
    {
        if (!RequireAdministrator("Restarting OVRService requires administrator rights."))
        {
            return;
        }

        RunOperation("Restart Meta services", () =>
        {
            MetaRuntimeService.RestartMeta();
            return new[] { "Meta runtime restarted and highwind_service checked." };
        });
    }

    private void StartHighwindClicked(object? sender, EventArgs e)
    {
        RunOperation("Start highwind", () =>
        {
            MetaRuntimeService.StartHighwind();
            return new[] { "highwind_service started or already running." };
        });
    }

    private void LaunchClientClicked(object? sender, EventArgs e)
    {
        RunOperation("Launch Meta client", () =>
        {
            MetaRuntimeService.LaunchMetaClient();
            return new[] { "Meta client launch requested." };
        });
    }

    private void InstallAutostartClicked(object? sender, EventArgs e)
    {
        RunOperation("Install highwind autostart", () =>
        {
            MetaRuntimeService.InstallHighwindAutostart();
            return new[] { "Installed current-user highwind autostart." };
        });
    }

    private void RemoveAutostartClicked(object? sender, EventArgs e)
    {
        RunOperation("Remove highwind autostart", () =>
        {
            MetaRuntimeService.RemoveHighwindAutostart();
            return new[] { "Removed current-user highwind autostart." };
        });
    }

    private void SetEncoderDefaultsClicked(object? sender, EventArgs e)
    {
        RunOperation("Set HEVC encoder defaults", () =>
        {
            CompatibilityService.SetEncoderDefaults();
            return new[] { "Set HEVC=1, BitrateMbps=0, EncodeWidth=0, DBR=0." };
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

    private void RunOperation(string name, Func<IEnumerable<string>> work)
    {
        SetButtonsEnabled(false);
        Log($"{name} started.");
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
                    Log($"{name} completed.");
                });
            }
            catch (Exception ex)
            {
                BeginInvoke(() => ShowError($"{name} failed", ex));
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
            $"{reason}{Environment.NewLine}{Environment.NewLine}Relaunch this tool as administrator now?",
            "Administrator required",
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
                ShowError("Could not relaunch elevated", ex);
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

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        OpenPath(path);
    }

    private static void OpenPath(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            MessageBox.Show($"Path not found: {path}", "Open path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
