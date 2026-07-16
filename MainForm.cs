using System.Diagnostics;
using System.Globalization;

namespace MetaLinkCompatTool;

public sealed class MainForm : Form
{
    private readonly UiThemePalette _theme = UiThemes.Atelier;
    private Color _bg => _theme.Background;
    private Color _card => _theme.Surface;
    private Color _card2 => _theme.SurfaceAlt;
    private Color _accent => _theme.Accent;
    private Color _accent2 => _theme.AccentSecondary;
    private Color _text => _theme.Text;
    private Color _muted => _theme.Muted;

    private HardwareInfo? _hardware;
    private AppLanguage _language = DetectDefaultLanguage();

    private readonly Dictionary<string, Control> _localizedControls = new();
    private readonly List<Button> _operationButtons = new();

    private readonly Panel _pageHost = new();
    private readonly Button _dashboardNavButton = new RoundedButton();
    private readonly Button _toolsNavButton = new RoundedButton();
    private readonly Panel _scrollHost = new();
    private readonly Panel _toolsScrollHost = new();
    private readonly TableLayoutPanel _content = new();
    private readonly TableLayoutPanel _toolsContent = new();
    private readonly TableLayoutPanel _dashboardBody = new();
    private readonly TableLayoutPanel _dashboardLeft = new();
    private readonly TableLayoutPanel _dashboardRight = new();
    private readonly TableLayoutPanel _toolsBody = new();
    private Control? _linkTuningCard;
    private Control? _debugToolCard;
    private readonly Label _adminLabel = new();
    private readonly Label _cpuLabel = new();
    private readonly Label _gpuDetailLabel = new();
    private readonly Label _runtimeLabel = new();
    private readonly Label _linkSettingsLabel = new();
    private readonly ComboBox _gpuCombo = new();
    private readonly ComboBox _backupCombo = new();
    private readonly ComboBox _languageCombo = new();
    private readonly ComboBox _aswCombo = new();
    private readonly ComboBox _colorSpaceCombo = new();
    private readonly TextBox _compatStatusBox = new();
    private readonly TextBox _logBox = new();
    private readonly TextBox _toolsLogBox = new();
    private readonly CheckBox _lockLocalCheck = new();
    private readonly CheckBox _encoderDefaultsCheck = new();
    private readonly CheckBox _hevcCheck = new();
    private readonly CheckBox _dynamicBitrateCheck = new();
    private readonly NumericUpDown _bitrateBox = new();
    private readonly NumericUpDown _encodeWidthBox = new();
    private readonly NumericUpDown _pixelsPerDisplayPixelBox = new();
    private readonly ToolTip _detailsToolTip = new();
    private readonly List<FontMetric> _fontMetrics = new();
    private readonly List<BoxMetric> _boxMetrics = new();
    private readonly List<RowMetric> _rowMetrics = new();
    private readonly List<ColumnMetric> _columnMetrics = new();
    private List<Font> _scaledFonts = new();
    private float _lastUiScale = -1F;
    private bool? _narrowLayout;

    private sealed record FontMetric(Control Control, string Family, float Size, FontStyle Style);
    private sealed record BoxMetric(Control Control, Padding Margin, Padding BasePadding, Size? FixedSize);
    private sealed record RowMetric(RowStyle Style, float Height);
    private sealed record ColumnMetric(ColumnStyle Style, float Width);

    private sealed record OptionItem(string Value, string Text)
    {
        public override string ToString() => Text;
    }

    public MainForm()
    {
        Text = I18n.T(_language, "AppTitle");
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 560);
        Size = new Size(1220, 820);
        BackColor = _bg;
        ForeColor = _text;
        Font = new Font(_theme.BodyFont, 10F, FontStyle.Regular);
        DoubleBuffered = true;

        var executablePath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            Icon = Icon.ExtractAssociatedIcon(executablePath) ?? Icon;
        }

        _detailsToolTip.AutoPopDelay = 30000;
        _detailsToolTip.InitialDelay = 300;
        _detailsToolTip.ReshowDelay = 100;
        _detailsToolTip.ShowAlways = true;

        BuildUi();
        ApplyLanguage();
        CaptureResponsiveMetrics(this);
        ResizeScrollContent();
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
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = _bg,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(shell);

        shell.Controls.Add(BuildNavigation(), 0, 0);

        _pageHost.Dock = DockStyle.Fill;
        _pageHost.BackColor = _bg;
        shell.Controls.Add(_pageHost, 0, 1);

        _scrollHost.Dock = DockStyle.Fill;
        _scrollHost.AutoScroll = true;
        _scrollHost.BackColor = _bg;
        _pageHost.Controls.Add(_scrollHost);

        _content.ColumnCount = 1;
        _content.RowCount = 3;
        _content.AutoSize = false;
        _content.Padding = new Padding(22);
        _content.BackColor = _bg;
        _content.RowStyles.Add(new RowStyle(SizeType.Absolute, 158));
        _content.RowStyles.Add(new RowStyle(SizeType.Absolute, 830));
        _content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _scrollHost.Controls.Add(_content);

        _content.Controls.Add(BuildHeader(), 0, 0);
        _content.Controls.Add(BuildBody(), 0, 1);
        _content.Controls.Add(BuildLogCard(), 0, 2);

        _toolsScrollHost.Dock = DockStyle.Fill;
        _toolsScrollHost.AutoScroll = true;
        _toolsScrollHost.BackColor = _bg;
        _pageHost.Controls.Add(_toolsScrollHost);

        _toolsContent.ColumnCount = 1;
        _toolsContent.RowCount = 4;
        _toolsContent.AutoSize = false;
        _toolsContent.Padding = new Padding(22);
        _toolsContent.BackColor = _bg;
        _toolsContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        _toolsContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 400));
        _toolsContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));
        _toolsContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _toolsScrollHost.Controls.Add(_toolsContent);
        _toolsContent.Controls.Add(BuildToolIntroCard(), 0, 0);
        _toolsContent.Controls.Add(BuildToolBody(), 0, 1);
        _toolsContent.Controls.Add(BuildFeedbackCard(), 0, 2);
        _toolsContent.Controls.Add(BuildToolLogCard(), 0, 3);

        LoadToolChoices();
        ShowPage(showTools: false);
        ResizeScrollContent();
    }

    private Control BuildNavigation()
    {
        var navigation = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _bg,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(22, 8, 22, 6),
            Margin = Padding.Empty
        };
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 178));
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 296));
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var brand = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _bg,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = new Padding(0, 7, 0, 0)
        };
        brand.Controls.Add(new Label
        {
            Text = "META LINK LAB",
            AutoSize = false,
            Size = new Size(166, 30),
            ForeColor = _text,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        });
        navigation.Controls.Add(brand, 0, 0);

        var navigationItems = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _bg,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = new Padding(0, 3, 0, 0)
        };

        ConfigureNavigationButton(_dashboardNavButton, "TabDashboard");
        _dashboardNavButton.Click += (_, _) => ShowPage(showTools: false);
        navigationItems.Controls.Add(_dashboardNavButton);

        ConfigureNavigationButton(_toolsNavButton, "TabTools");
        _toolsNavButton.Click += (_, _) => ShowPage(showTools: true);
        navigationItems.Controls.Add(_toolsNavButton);
        navigation.Controls.Add(navigationItems, 1, 0);

        var homepage = new LinkLabel
        {
            Text = "小蓝个人主页",
            AutoSize = false,
            Dock = DockStyle.Fill,
            LinkColor = _accent,
            ActiveLinkColor = _accent2,
            VisitedLinkColor = _accent,
            ForeColor = _accent,
            Font = new Font(_theme.BodyFont, 9.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.TopRight,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Cursor = Cursors.Hand,
            Margin = Padding.Empty,
            Padding = new Padding(0, 10, 0, 0)
        };
        homepage.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo("https://xiaolan9999.net")
        {
            UseShellExecute = true
        });
        navigation.Controls.Add(homepage, 2, 0);

        return navigation;
    }

    private void ConfigureNavigationButton(Button button, string languageKey)
    {
        button.Size = new Size(138, 42);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = _card2;
        button.FlatAppearance.MouseDownBackColor = _accent;
        button.Font = new Font(_theme.BodyFont, 10F, FontStyle.Bold);
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(0, 0, 10, 0);
        if (button is RoundedButton roundedButton)
        {
            roundedButton.CornerRadius = _theme.ButtonRadius;
        }
        Bind(button, languageKey);
    }

    private void ShowPage(bool showTools)
    {
        _scrollHost.Visible = !showTools;
        _toolsScrollHost.Visible = showTools;

        if (showTools)
        {
            _toolsScrollHost.BringToFront();
        }
        else
        {
            _scrollHost.BringToFront();
        }

        StyleNavigationButton(_dashboardNavButton, !showTools);
        StyleNavigationButton(_toolsNavButton, showTools);
        ResizeScrollContent();
    }

    private void StyleNavigationButton(Button button, bool active)
    {
        button.BackColor = active ? _theme.NavigationActiveBackground : _bg;
        button.ForeColor = active ? _theme.NavigationActiveText : _muted;
    }

    private void ResizeScrollContent()
    {
        var hostWidth = _pageHost.ClientSize.Width;
        var hostHeight = _pageHost.ClientSize.Height;
        if (hostWidth <= 0 || hostHeight <= 0)
        {
            return;
        }

        var narrow = hostWidth < 1280;
        ApplyResponsiveLayout(narrow);

        var scale = CalculateUiScale();
        ApplyUiScale(scale);

        var dashboardBodyHeight = (narrow ? 1660F : 830F) * scale;
        var toolsBodyHeight = (narrow ? 800F : 400F) * scale;
        _content.RowStyles[1].Height = dashboardBodyHeight;
        _toolsContent.RowStyles[1].Height = toolsBodyHeight;

        var scrollbarAllowance = SystemInformation.VerticalScrollBarWidth + 8;
        var contentWidth = Math.Max(0, hostWidth - scrollbarAllowance);
        var minimumWidth = (int)Math.Ceiling((narrow ? 720F : 1080F) * scale);
        var dashboardMinimumHeight = (int)Math.Ceiling((158F + (narrow ? 1660F : 830F) + 300F + 44F) * scale);
        var toolsMinimumHeight = (int)Math.Ceiling((180F + (narrow ? 800F : 400F) + 170F + 260F + 44F) * scale);

        _content.Size = new Size(
            Math.Max(minimumWidth, contentWidth),
            Math.Max(dashboardMinimumHeight, hostHeight));
        _toolsContent.Size = new Size(
            Math.Max(minimumWidth, contentWidth),
            Math.Max(toolsMinimumHeight, hostHeight));
    }

    private float CalculateUiScale()
    {
        var widthScale = ClientSize.Width / 1220F;
        var heightScale = ClientSize.Height / 820F;
        var scale = Math.Clamp(Math.Min(widthScale, heightScale), 0.84F, 1.28F);
        return MathF.Round(scale * 20F) / 20F;
    }

    private void ApplyResponsiveLayout(bool narrow)
    {
        if (_narrowLayout == narrow)
        {
            return;
        }

        ConfigureSplitLayout(_dashboardBody, _dashboardLeft, _dashboardRight, narrow, 52F, 48F);
        if (_linkTuningCard is not null && _debugToolCard is not null)
        {
            ConfigureSplitLayout(_toolsBody, _linkTuningCard, _debugToolCard, narrow, 50F, 50F);
        }

        _narrowLayout = narrow;
    }

    private static void ConfigureSplitLayout(
        TableLayoutPanel layout,
        Control first,
        Control second,
        bool narrow,
        float firstColumnPercent,
        float secondColumnPercent)
    {
        layout.SuspendLayout();
        layout.Controls.Remove(first);
        layout.Controls.Remove(second);
        layout.ColumnStyles.Clear();
        layout.RowStyles.Clear();

        if (narrow)
        {
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            layout.Controls.Add(first, 0, 0);
            layout.Controls.Add(second, 0, 1);
        }
        else
        {
            layout.ColumnCount = 2;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, firstColumnPercent));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, secondColumnPercent));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(first, 0, 0);
            layout.Controls.Add(second, 1, 0);
        }

        layout.ResumeLayout(performLayout: true);
    }

    private void CaptureResponsiveMetrics(Control root)
    {
        foreach (Control control in root.Controls)
        {
            _fontMetrics.Add(new FontMetric(control, control.Font.FontFamily.Name, control.Font.Size, control.Font.Style));
            Size? fixedSize = control.Dock == DockStyle.None && !control.AutoSize ? control.Size : null;
            _boxMetrics.Add(new BoxMetric(control, control.Margin, control.Padding, fixedSize));

            if (control is TableLayoutPanel table)
            {
                foreach (RowStyle rowStyle in table.RowStyles)
                {
                    if (rowStyle.SizeType == SizeType.Absolute)
                    {
                        _rowMetrics.Add(new RowMetric(rowStyle, rowStyle.Height));
                    }
                }

                foreach (ColumnStyle columnStyle in table.ColumnStyles)
                {
                    if (columnStyle.SizeType == SizeType.Absolute)
                    {
                        _columnMetrics.Add(new ColumnMetric(columnStyle, columnStyle.Width));
                    }
                }
            }

            CaptureResponsiveMetrics(control);
        }
    }

    private void ApplyUiScale(float scale)
    {
        if (_fontMetrics.Count == 0 || Math.Abs(scale - _lastUiScale) < 0.001F)
        {
            return;
        }

        SuspendLayout();
        var fontCache = new Dictionary<(string Family, float Size, FontStyle Style), Font>();
        foreach (var metric in _fontMetrics)
        {
            if (metric.Control.IsDisposed)
            {
                continue;
            }

            var size = MathF.Round(Math.Max(7F, metric.Size * scale) * 2F) / 2F;
            var key = (metric.Family, size, metric.Style);
            if (!fontCache.TryGetValue(key, out var font))
            {
                font = new Font(metric.Family, size, metric.Style, GraphicsUnit.Point);
                fontCache[key] = font;
            }
            metric.Control.Font = font;

            if (metric.Control is RoundedPanel panel)
            {
                panel.CornerRadius = Math.Max(8, (int)Math.Round(_theme.CardRadius * scale));
            }
            else if (metric.Control is RoundedButton button)
            {
                button.CornerRadius = Math.Max(5, (int)Math.Round(_theme.ButtonRadius * scale));
            }
        }

        foreach (var metric in _boxMetrics)
        {
            if (metric.Control.IsDisposed)
            {
                continue;
            }

            metric.Control.Margin = ScalePadding(metric.Margin, scale);
            metric.Control.Padding = ScalePadding(metric.BasePadding, scale);
            if (metric.FixedSize is Size size)
            {
                metric.Control.Size = new Size(
                    Math.Max(1, (int)Math.Round(size.Width * scale)),
                    Math.Max(1, (int)Math.Round(size.Height * scale)));
            }
        }

        foreach (var metric in _rowMetrics)
        {
            metric.Style.Height = metric.Height * scale;
        }
        foreach (var metric in _columnMetrics)
        {
            metric.Style.Width = metric.Width * scale;
        }

        foreach (var font in _scaledFonts)
        {
            font.Dispose();
        }
        _scaledFonts = fontCache.Values.ToList();
        _lastUiScale = scale;
        ResumeLayout(performLayout: true);
    }

    private static Padding ScalePadding(Padding padding, float scale)
    {
        return new Padding(
            Math.Max(0, (int)Math.Round(padding.Left * scale)),
            Math.Max(0, (int)Math.Round(padding.Top * scale)),
            Math.Max(0, (int)Math.Round(padding.Right * scale)),
            Math.Max(0, (int)Math.Round(padding.Bottom * scale)));
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = _bg,
            Margin = new Padding(0, 0, 0, 10)
        };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        header.Controls.Add(new Label
        {
            Text = "META LINK / LOCAL DESKTOP UTILITY",
            Dock = DockStyle.Fill,
            ForeColor = _accent,
            Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var title = new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = _text,
            Font = new Font(_theme.DisplayFont, 21F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        Bind(title, "AppTitle");
        header.Controls.Add(title, 0, 1);

        var subtitle = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = _muted,
            Font = new Font(_theme.BodyFont, 9.6F),
            TextAlign = ContentAlignment.TopLeft,
            AutoEllipsis = true
        };
        Bind(subtitle, "Subtitle");
        header.Controls.Add(subtitle, 0, 2);

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
        _adminLabel.ForeColor = Elevation.IsAdministrator() ? _accent2 : _theme.Warning;
        _adminLabel.TextAlign = ContentAlignment.MiddleLeft;
        toolbar.Controls.Add(_adminLabel);

        var languageLabel = CreateMutedLabel("");
        languageLabel.AutoSize = false;
        languageLabel.Size = new Size(52, 32);
        languageLabel.Dock = DockStyle.None;
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

        header.Controls.Add(toolbar, 0, 3);

        return header;
    }

    private Control BuildBody()
    {
        _dashboardBody.Dock = DockStyle.Fill;
        _dashboardBody.BackColor = _bg;

        _dashboardLeft.Dock = DockStyle.Fill;
        _dashboardLeft.RowCount = 2;
        _dashboardLeft.ColumnCount = 1;
        _dashboardLeft.BackColor = _bg;
        _dashboardLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 380));
        _dashboardLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _dashboardLeft.Controls.Add(BuildDeviceCard(), 0, 0);
        _dashboardLeft.Controls.Add(BuildCompatibilityCard(), 0, 1);

        _dashboardRight.Dock = DockStyle.Fill;
        _dashboardRight.RowCount = 2;
        _dashboardRight.ColumnCount = 1;
        _dashboardRight.BackColor = _bg;
        _dashboardRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 430));
        _dashboardRight.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _dashboardRight.Controls.Add(BuildActionsCard(), 0, 0);
        _dashboardRight.Controls.Add(BuildBackupCard(), 0, 1);

        ConfigureSplitLayout(_dashboardBody, _dashboardLeft, _dashboardRight, narrow: false, 52F, 48F);
        return _dashboardBody;
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
        _gpuDetailLabel.Cursor = Cursors.Help;
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
        AddActionButton(layout, "Rollback", _theme.Warning, 1, 0, RollbackClicked);
        AddActionButton(layout, "KillMeta", _theme.Danger, 0, 1, KillMetaClicked);
        AddActionButton(layout, "RestartMeta", _accent2, 1, 1, RestartMetaClicked);
        AddActionButton(layout, "StartHighwind", _theme.Info, 0, 2, StartHighwindClicked);
        AddActionButton(layout, "LaunchClient", _theme.Info, 1, 2, LaunchClientClicked);
        AddActionButton(layout, "InstallAutostart", _theme.Success, 0, 3, InstallAutostartClicked);
        AddActionButton(layout, "RemoveAutostart", _theme.Lavender, 1, 3, RemoveAutostartClicked);
        AddActionButton(layout, "SetEncoder", _theme.Aqua, 0, 4, SetEncoderDefaultsClicked);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 4)!, 2);

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

    private Control BuildToolIntroCard()
    {
        var card = CreateCard("DebugTools", out var body);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = _card
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var intro = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = _muted,
            Font = new Font(_theme.BodyFont, 9.5F),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = false
        };
        Bind(intro, "DebugToolsIntro");
        layout.Controls.Add(intro, 0, 0);

        var note = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = _theme.Warning,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.TopLeft,
            AutoEllipsis = false
        };
        Bind(note, "RefreshRateNote");
        layout.Controls.Add(note, 0, 1);
        body.Controls.Add(layout);
        return card;
    }

    private Control BuildToolBody()
    {
        _toolsBody.Dock = DockStyle.Fill;
        _toolsBody.BackColor = _bg;
        _linkTuningCard = BuildLinkTuningCard();
        _debugToolCard = BuildDebugToolCard();
        ConfigureSplitLayout(_toolsBody, _linkTuningCard, _debugToolCard, narrow: false, 50F, 50F);
        return _toolsBody;
    }

    private Control BuildLinkTuningCard()
    {
        var card = CreateCard("LinkTuning", out var body);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = _card
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));

        _linkSettingsLabel.Dock = DockStyle.Fill;
        _linkSettingsLabel.ForeColor = _muted;
        _linkSettingsLabel.TextAlign = ContentAlignment.MiddleLeft;
        _linkSettingsLabel.AutoEllipsis = true;
        layout.Controls.Add(_linkSettingsLabel, 0, 0);

        var settings = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            BackColor = _card
        };
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        for (var i = 0; i < 4; i++)
        {
            settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        }

        StyleCheck(_hevcCheck);
        AddSettingRow(settings, 0, "HevcCodec", _hevcCheck);
        ConfigureNumeric(_bitrateBox, 0, 960, 25, 0);
        AddSettingRow(settings, 1, "BitrateMbps", _bitrateBox);
        ConfigureNumeric(_encodeWidthBox, 0, 5000, 16, 0);
        AddSettingRow(settings, 2, "EncodeWidth", _encodeWidthBox);
        StyleCheck(_dynamicBitrateCheck);
        AddSettingRow(settings, 3, "DynamicBitrate", _dynamicBitrateCheck);
        layout.Controls.Add(settings, 0, 1);

        var buttons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = _card
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        AddActionButton(buttons, "ApplyLinkSettings", _accent, 0, 0, ApplyLinkSettingsClicked);
        AddActionButton(buttons, "Apply120Preset", _theme.Success, 1, 0, Apply120PresetClicked);
        AddActionButton(buttons, "ResetLinkDefaults", _theme.Lavender, 0, 1, ResetLinkDefaultsClicked);
        AddActionButton(buttons, "RefreshToolSettings", _card2, 1, 1, RefreshToolSettingsClicked);
        layout.Controls.Add(buttons, 0, 2);

        body.Controls.Add(layout);
        return card;
    }

    private Control BuildDebugToolCard()
    {
        var card = CreateCard("OdtRuntime", out var body);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = _card
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));

        var settings = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = _card
        };
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        for (var i = 0; i < 3; i++)
        {
            settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        }

        ConfigureNumeric(_pixelsPerDisplayPixelBox, 0, 2, 0.05M, 2);
        AddSettingRow(settings, 0, "PixelsOverride", _pixelsPerDisplayPixelBox);
        StyleCombo(_aswCombo);
        AddSettingRow(settings, 1, "AswMode", _aswCombo);
        StyleCombo(_colorSpaceCombo);
        AddSettingRow(settings, 2, "OutputColorSpace", _colorSpaceCombo);
        layout.Controls.Add(settings, 0, 0);

        var buttons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = _card
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        AddActionButton(buttons, "LaunchDebugTool", _theme.Info, 0, 0, LaunchDebugToolClicked);
        AddActionButton(buttons, "ApplyOdtRuntime", _accent2, 1, 0, ApplyOdtRuntimeClicked);
        AddActionButton(buttons, "KillMeta", _theme.Danger, 0, 1, KillMetaClicked);
        AddActionButton(buttons, "RestartMeta", _accent, 1, 1, RestartMetaClicked);
        layout.Controls.Add(buttons, 0, 1);

        body.Controls.Add(layout);
        return card;
    }

    private Control BuildFeedbackCard()
    {
        var card = CreateCard("FeedbackCard", out var body);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = _card
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

        var text = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = _muted,
            Font = new Font(_theme.BodyFont, 9.5F),
            TextAlign = ContentAlignment.MiddleLeft
        };
        Bind(text, "FeedbackHint");
        layout.Controls.Add(text, 0, 0);

        var open = CreateButton("", _theme.Aqua);
        Bind(open, "OpenFeedback");
        open.Click += OpenFeedbackClicked;
        layout.Controls.Add(open, 1, 0);

        body.Controls.Add(layout);
        return card;
    }

    private Control BuildToolLogCard()
    {
        var card = CreateCard("ToolLog", out var body);
        _toolsLogBox.Dock = DockStyle.Fill;
        _toolsLogBox.Multiline = true;
        _toolsLogBox.ReadOnly = true;
        _toolsLogBox.ScrollBars = ScrollBars.Both;
        _toolsLogBox.WordWrap = false;
        _toolsLogBox.BorderStyle = BorderStyle.None;
        _toolsLogBox.BackColor = _card;
        _toolsLogBox.ForeColor = _text;
        _toolsLogBox.Font = new Font("Consolas", 9.5F);
        body.Controls.Add(_toolsLogBox);
        return card;
    }

    private Panel CreateCard(string titleKey, out Panel body)
    {
        var outer = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(10),
            Padding = new Padding(20),
            BackColor = _card,
            BorderColor = _theme.Border,
            BorderThickness = 1,
            CornerRadius = _theme.CardRadius
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
            Font = new Font(_theme.DisplayFont, 12.5F, FontStyle.Bold),
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
        var button = new RoundedButton
        {
            Text = text,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = color,
            ForeColor = GetButtonTextColor(color),
            Font = new Font(_theme.BodyFont, 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(6),
            CornerRadius = _theme.ButtonRadius
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private Color GetButtonTextColor(Color background)
    {
        var brightness = (background.R * 299 + background.G * 587 + background.B * 114) / 1000;
        return brightness >= 170 ? _text : Color.White;
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
            Font = new Font(_theme.BodyFont, 9F, FontStyle.Bold)
        };
    }

    private void StyleCombo(ComboBox combo)
    {
        combo.Dock = DockStyle.Fill;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.BackColor = _card2;
        combo.ForeColor = _text;
        combo.FlatStyle = FlatStyle.Flat;
        combo.Font = new Font(_theme.BodyFont, 9.5F);
        combo.Margin = new Padding(2, 4, 2, 6);
    }

    private void StyleCheck(CheckBox check)
    {
        check.AutoSize = true;
        check.ForeColor = _muted;
        check.Margin = new Padding(4, 4, 18, 4);
    }

    private void ConfigureNumeric(NumericUpDown numeric, decimal min, decimal max, decimal increment, int decimalPlaces)
    {
        numeric.Dock = DockStyle.Fill;
        numeric.Minimum = min;
        numeric.Maximum = max;
        numeric.Increment = increment;
        numeric.DecimalPlaces = decimalPlaces;
        numeric.BackColor = _card2;
        numeric.ForeColor = _text;
        numeric.BorderStyle = BorderStyle.FixedSingle;
        numeric.Font = new Font(_theme.BodyFont, 9.5F);
        numeric.Margin = new Padding(2, 4, 2, 6);
    }

    private void AddSettingRow(TableLayoutPanel layout, int row, string labelKey, Control input)
    {
        var label = CreateMutedLabel("");
        label.TextAlign = ContentAlignment.MiddleLeft;
        Bind(label, labelKey);
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(input, 1, row);
    }

    private void LoadToolChoices()
    {
        var selectedAsw = (_aswCombo.SelectedItem as OptionItem)?.Value ?? "nochange";
        var selectedColor = (_colorSpaceCombo.SelectedItem as OptionItem)?.Value ?? "-1";

        _aswCombo.DataSource = new List<OptionItem>
        {
            new("nochange", T("ChoiceNoChange")),
            new("default", T("AswDefault")),
            new("off", T("AswOff")),
            new("auto", T("AswAuto")),
            new("force", T("AswForce"))
        };
        SelectOption(_aswCombo, selectedAsw);

        _colorSpaceCombo.DataSource = new List<OptionItem>
        {
            new("-1", T("ChoiceNoChange")),
            new("0", T("ColorNone")),
            new("1", T("ColorDciP3")),
            new("2", T("ColorSrgb"))
        };
        SelectOption(_colorSpaceCombo, selectedColor);
    }

    private static void SelectOption(ComboBox combo, string value)
    {
        foreach (var item in combo.Items)
        {
            if (item is OptionItem option && string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }

        if (combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }
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

        LoadToolChoices();
        _adminLabel.Text = Elevation.IsAdministrator() ? T("Admin") : T("Standard");
        _adminLabel.ForeColor = Elevation.IsAdministrator() ? _accent2 : _theme.Warning;
        if (_hardware is null)
        {
            _cpuLabel.Text = T("NotLoaded");
        }
        _detailsToolTip.SetToolTip(_cpuLabel, _cpuLabel.Text);
        UpdateGpuDetails();
    }

    private void RefreshAll()
    {
        try
        {
            _hardware = DeviceInfoService.GetHardwareInfo();
            _cpuLabel.Text = _hardware.Cpu.Name;
            _detailsToolTip.SetToolTip(_cpuLabel, _hardware.Cpu.Name);
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
            RefreshToolSettings();
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

    private void RefreshToolSettings()
    {
        try
        {
            var settings = DebugToolService.ReadLinkSettings();
            _hevcCheck.Checked = settings.Hevc;
            _dynamicBitrateCheck.Checked = settings.DynamicBitrate;
            _bitrateBox.Value = Math.Min(_bitrateBox.Maximum, Math.Max(_bitrateBox.Minimum, settings.BitrateMbps));
            _encodeWidthBox.Value = Math.Min(_encodeWidthBox.Maximum, Math.Max(_encodeWidthBox.Minimum, settings.EncodeWidth));
            _linkSettingsLabel.Text = DebugToolService.GetLinkSettingsStatus();
        }
        catch (Exception ex)
        {
            _linkSettingsLabel.Text = $"{T("RefreshFailed")}: {ex.Message}";
        }
    }

    private void UpdateGpuDetails()
    {
        if (_gpuCombo.SelectedItem is not GpuInfo gpu)
        {
            _gpuDetailLabel.Text = T("NoGpu");
            _detailsToolTip.SetToolTip(_gpuDetailLabel, _gpuDetailLabel.Text);
            _detailsToolTip.SetToolTip(_gpuCombo, "");
            return;
        }

        var vram = gpu.AdapterRamBytes > 0 ? $"{gpu.AdapterRamBytes / 1024 / 1024} MB" : T("Unknown");
        var details =
            $"{T("Vendor")}: {gpu.Vendor}{Environment.NewLine}" +
            $"{T("Pid")}: {gpu.Pid}{Environment.NewLine}" +
            $"{T("Driver")}: {gpu.DriverVersion}{Environment.NewLine}" +
            $"{T("Vram")}: {vram}{Environment.NewLine}" +
            gpu.PnpDeviceId;
        _gpuDetailLabel.Text = details;
        _detailsToolTip.SetToolTip(_gpuDetailLabel, details);
        _detailsToolTip.SetToolTip(_gpuCombo, gpu.ToString());
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

    private void ApplyLinkSettingsClicked(object? sender, EventArgs e)
    {
        var settings = ReadLinkSettingsFromUi();
        RunOperation(T("ApplyLinkSettings"), T("LinkSettingsApplied"), () =>
        {
            var backup = DebugToolService.ApplyLinkSettings(settings);
            return BuildLinkSettingMessages(settings, backup);
        });
    }

    private void Apply120PresetClicked(object? sender, EventArgs e)
    {
        var settings = DebugToolService.HighRefreshPreset;
        RunOperation(T("Apply120Preset"), T("PresetApplied"), () =>
        {
            var backup = DebugToolService.ApplyLinkSettings(settings);
            return BuildLinkSettingMessages(settings, backup).Concat([T("RefreshRateNote")]);
        });
    }

    private void ResetLinkDefaultsClicked(object? sender, EventArgs e)
    {
        var settings = DebugToolService.SafeDefaults;
        RunOperation(T("ResetLinkDefaults"), T("LinkDefaultsReset"), () =>
        {
            var backup = DebugToolService.ApplyLinkSettings(settings);
            return BuildLinkSettingMessages(settings, backup);
        });
    }

    private void RefreshToolSettingsClicked(object? sender, EventArgs e)
    {
        RefreshToolSettings();
        Log(T("ToolSettingsRefreshed"));
    }

    private void LaunchDebugToolClicked(object? sender, EventArgs e)
    {
        RunOperation(T("LaunchDebugTool"), T("DebugToolLaunch"), () =>
        {
            DebugToolService.LaunchOculusDebugTool();
            return [T("DebugToolLaunch")];
        });
    }

    private void ApplyOdtRuntimeClicked(object? sender, EventArgs e)
    {
        var pixels = _pixelsPerDisplayPixelBox.Value;
        var asw = (_aswCombo.SelectedItem as OptionItem)?.Value ?? "nochange";
        var colorText = (_colorSpaceCombo.SelectedItem as OptionItem)?.Value ?? "-1";
        var color = int.TryParse(colorText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedColor)
            ? parsedColor
            : -1;

        RunOperation(T("ApplyOdtRuntime"), T("OdtRuntimeApplied"), () =>
        {
            var output = DebugToolService.RunRuntimeCommands(pixels, asw, color);
            return [T("OdtRuntimeApplied"), output];
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

    private LinkSettings ReadLinkSettingsFromUi()
    {
        return new LinkSettings(
            Hevc: _hevcCheck.Checked,
            BitrateMbps: (int)_bitrateBox.Value,
            EncodeWidth: (int)_encodeWidthBox.Value,
            DynamicBitrate: _dynamicBitrateCheck.Checked);
    }

    private IEnumerable<string> BuildLinkSettingMessages(LinkSettings settings, string backup)
    {
        yield return $"HEVC={(settings.Hevc ? 1 : 0)}  BitrateMbps={settings.BitrateMbps}  EncodeWidth={settings.EncodeWidth}  DBR={(settings.DynamicBitrate ? 1 : 0)}";
        if (!string.IsNullOrWhiteSpace(backup))
        {
            yield return $"{T("RegistryBackup")}: {backup}";
        }
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
                    RefreshToolSettings();
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
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
        _logBox.AppendText(line);
        _toolsLogBox.AppendText(line);
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
