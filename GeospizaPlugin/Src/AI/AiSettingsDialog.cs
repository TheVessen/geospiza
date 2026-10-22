#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;

namespace GeospizaPlugin.AI;

/// <summary>Chooses and tests the text model (Ollama or an OpenAI-compatible endpoint) and the decision model (Jev).</summary>
public class AiSettingsDialog : Dialog
{
    private static readonly List<(string Tag, string Label)> OllamaChoices = new()
    {
        ("gemma4:e2b", "Gemma 4 E2B — ~7 GB (edge/laptop, fastest)"),
        ("gemma4:e4b", "Gemma 4 E4B — ~10 GB (recommended)"),
        ("gemma4:26b", "Gemma 4 26B — ~18 GB (workstation GPU, best quality)"),
        ("gemma4:31b", "Gemma 4 31B — ~20 GB (workstation GPU, dense model)"),
    };

    private readonly AiConfig _config;

    private readonly DropDown _providerDropDown;
    private readonly Panel _providerPanel;
    private readonly Control _ollamaLayout;
    private readonly Control _openAiLayout;

    private readonly ComboBox _ollamaModel;
    private readonly Button _downloadButton;
    private readonly ProgressBar _progressBar;

    private readonly TextBox _openAiBaseUrl;
    private readonly TextBox _openAiModel;
    private readonly PasswordBox _openAiKey;

    private readonly Label _textStatus;

    private readonly PasswordBox _jevKey;
    private readonly TextBox _jevModel;
    private readonly TextBox _jevBaseUrl;
    private readonly Label _jevStatus;

    private CancellationTokenSource? _cts;

    /// <param name="notice">Why the dialog opened, e.g. the model that still needs downloading.</param>
    /// <param name="showDecisionTab">Open on the decision-model tab instead of the text-model tab.</param>
    public AiSettingsDialog(string? notice = null, bool showDecisionTab = false)
    {
        _config = AiConfig.Load();

        Title = "Geospiza AI Settings";
        Resizable = true;
        MinimumSize = new Size(560, 380);

        // Text model tab
        _providerDropDown = new DropDown { Items = { "Ollama (local)", "OpenAI-compatible endpoint" } };
        _providerDropDown.SelectedIndex = _config.TextProvider == TextProviderKind.OpenAiCompatible ? 1 : 0;

        _ollamaModel = new ComboBox { AutoComplete = true };
        foreach (var (tag, _) in OllamaChoices) _ollamaModel.Items.Add(tag);
        _ollamaModel.Text = _config.OllamaModel;
        _downloadButton = new Button { Text = "Download" };
        _downloadButton.Click += OnDownloadClicked;
        _progressBar = new ProgressBar { MinValue = 0, MaxValue = 100, Visible = false, Indeterminate = true };
        _ollamaLayout = new TableLayout
        {
            Spacing = new Size(8, 8),
            Rows =
            {
                new TableRow(new Label { Text = "Model:" }, new TableCell(_ollamaModel, true), _downloadButton),
                new TableRow(new Label
                {
                    Text = string.Join("\n", OllamaChoices.ConvertAll(c => c.Label)) +
                           "\nAny other installed Ollama model name works too.",
                    TextColor = Colors.Gray
                }) { ScaleHeight = false },
                new TableRow(_progressBar)
            }
        };

        _openAiBaseUrl = new TextBox { Text = _config.OpenAiBaseUrl, PlaceholderText = "http://localhost:1234/v1" };
        _openAiModel = new TextBox { Text = _config.OpenAiModel, PlaceholderText = "model name as the server lists it" };
        _openAiKey = new PasswordBox { Text = _config.OpenAiApiKey ?? string.Empty };
        _openAiLayout = new TableLayout
        {
            Spacing = new Size(8, 8),
            Rows =
            {
                new TableRow(new Label { Text = "Base URL:" }, new TableCell(_openAiBaseUrl, true)),
                new TableRow(new Label { Text = "Model:" }, _openAiModel),
                new TableRow(new Label { Text = "API key:" }, _openAiKey),
                new TableRow(null, new Label
                {
                    Text = "LM Studio, vLLM, llama.cpp server, OpenRouter, OpenAI… Include the API version " +
                           $"in the URL. Leave the key empty for local servers; {AiConfig.LlmApiKeyEnvironmentVariable} " +
                           "is used when it is empty.",
                    Wrap = WrapMode.Word,
                    TextColor = Colors.Gray
                })
            }
        };

        _providerPanel = new Panel();
        _providerDropDown.SelectedIndexChanged += (_, _) => ShowProviderPanel();
        ShowProviderPanel();

        _textStatus = new Label { Wrap = WrapMode.Word };
        var testTextButton = new Button { Text = "Test" };
        testTextButton.Click += async (_, _) => await TestAsync(_textStatus,
            ct => AiProviders.CreateTextModel(ReadForm()).CheckAsync(ct));

        var textTab = new TabPage
        {
            Text = "Text model",
            Padding = new Padding(12),
            Content = new TableLayout
            {
                Spacing = new Size(8, 10),
                Rows =
                {
                    new TableRow(new Label
                    {
                        Text = "Writes the AI Analysis and canvas summaries. Can also drive the Run Advisor " +
                               "(slower and less calibrated than a decision model).",
                        Wrap = WrapMode.Word
                    }),
                    new TableRow(new TableLayout(new TableRow(new Label { Text = "Provider:" },
                        new TableCell(_providerDropDown, true))) { Spacing = new Size(8, 8) }),
                    new TableRow(_providerPanel),
                    new TableRow(new TableLayout(new TableRow(testTextButton, new TableCell(_textStatus, true)))
                        { Spacing = new Size(8, 8) }),
                    null
                }
            }
        };

        // Decision model tab
        _jevKey = new PasswordBox { Text = _config.JevApiKey ?? string.Empty };
        _jevModel = new TextBox { Text = _config.JevModel, PlaceholderText = JevDecisionModel.DefaultModel };
        _jevBaseUrl = new TextBox { Text = _config.JevBaseUrl, PlaceholderText = JevDecisionModel.DefaultBaseUrl };
        _jevStatus = new Label { Wrap = WrapMode.Word };
        var testJevButton = new Button { Text = "Test" };
        testJevButton.Click += async (_, _) => await TestAsync(_jevStatus,
            ct => AiProviders.CreateJev(ReadForm()).CheckAsync(ct));

        var decisionTab = new TabPage
        {
            Text = "Decision model",
            Padding = new Padding(12),
            Content = new TableLayout
            {
                Spacing = new Size(8, 10),
                Rows =
                {
                    new TableRow(new Label
                    {
                        Text = "Jev by TypeSafe answers the Run Advisor's questions with calibrated probabilities, " +
                               "typically in well under a second. Run state is sent to api.typesafe.ai. " +
                               "Get a key at https://console.typesafe.ai",
                        Wrap = WrapMode.Word
                    }),
                    new TableRow(new TableLayout
                    {
                        Spacing = new Size(8, 8),
                        Rows =
                        {
                            new TableRow(new Label { Text = "API key:" }, new TableCell(_jevKey, true)),
                            new TableRow(new Label { Text = "Model:" }, _jevModel),
                            new TableRow(new Label { Text = "Base URL:" }, _jevBaseUrl),
                            new TableRow(null, new Label
                            {
                                Text = $"{JevDecisionModel.ApiKeyEnvironmentVariable} is used when the key is empty.",
                                TextColor = Colors.Gray
                            })
                        }
                    }),
                    new TableRow(new TableLayout(new TableRow(testJevButton, new TableCell(_jevStatus, true)))
                        { Spacing = new Size(8, 8) }),
                    null
                }
            }
        };

        var tabs = new TabControl { Pages = { textTab, decisionTab } };
        tabs.SelectedPage = showDecisionTab ? decisionTab : textTab;

        var saveButton = new Button { Text = "Save" };
        saveButton.Click += (_, _) => Save();
        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Click += (_, _) =>
        {
            _cts?.Cancel();
            Close();
        };
        DefaultButton = saveButton;
        AbortButton = cancelButton;

        var footer = new TableLayout { Spacing = new Size(8, 6) };
        if (!string.IsNullOrEmpty(notice))
            footer.Rows.Add(new TableRow(new Label { Text = notice, Wrap = WrapMode.Word }));
        footer.Rows.Add(new TableRow(new Label
        {
            Text = $"API keys are saved in plain text in {AiConfig.ConfigPath}",
            Wrap = WrapMode.Word,
            TextColor = Colors.Gray
        }));
        footer.Rows.Add(new TableRow(new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalContentAlignment = HorizontalAlignment.Right,
            Items = { null, saveButton, cancelButton }
        }));

        Content = new TableLayout
        {
            Padding = new Padding(12),
            Spacing = new Size(8, 10),
            Rows = { new TableRow(tabs) { ScaleHeight = true }, new TableRow(footer) }
        };

        Closed += (_, _) => _cts?.Cancel();
    }

    /// <summary>True when the user saved; false when the dialog was cancelled or closed.</summary>
    public bool Saved { get; private set; }

    /// <summary>
    ///     Shows the dialog on Rhino's UI thread from any thread and completes when it closes.
    ///     InvokeOnUiThread only queues the call, so callers must await this rather than assume it ran.
    /// </summary>
    public static Task<bool> ShowOnUiThreadAsync(string? notice = null, bool showDecisionTab = false)
    {
        var closed = new TaskCompletionSource<bool>();
        Rhino.RhinoApp.InvokeOnUiThread(() =>
        {
            try
            {
                var dialog = new AiSettingsDialog(notice, showDecisionTab);
                dialog.ShowModal();
                closed.TrySetResult(dialog.Saved);
            }
            catch (Exception ex)
            {
                closed.TrySetException(ex);
            }
        });
        return closed.Task;
    }

    private void ShowProviderPanel()
    {
        _providerPanel.Content = _providerDropDown.SelectedIndex == 1 ? _openAiLayout : _ollamaLayout;
    }

    private AiConfig ReadForm()
    {
        _config.TextProvider = _providerDropDown.SelectedIndex == 1
            ? TextProviderKind.OpenAiCompatible
            : TextProviderKind.Ollama;
        _config.OllamaModel = string.IsNullOrWhiteSpace(_ollamaModel.Text)
            ? AiConfig.DefaultOllamaModel
            : _ollamaModel.Text.Trim();
        _config.OpenAiBaseUrl = _openAiBaseUrl.Text.Trim();
        _config.OpenAiModel = _openAiModel.Text.Trim();
        _config.OpenAiApiKey = NullIfEmpty(_openAiKey.Text);
        _config.JevApiKey = NullIfEmpty(_jevKey.Text);
        _config.JevModel = string.IsNullOrWhiteSpace(_jevModel.Text) ? JevDecisionModel.DefaultModel : _jevModel.Text.Trim();
        _config.JevBaseUrl = string.IsNullOrWhiteSpace(_jevBaseUrl.Text)
            ? JevDecisionModel.DefaultBaseUrl
            : _jevBaseUrl.Text.Trim();
        return _config;
    }

    private void Save()
    {
        try
        {
            ReadForm().Save();
            Saved = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not save settings: {ex.Message}", "Geospiza AI", MessageBoxType.Error);
        }
    }

    private async Task TestAsync(Label status, Func<CancellationToken, Task<ProviderStatus>> check)
    {
        status.Text = "Testing...";
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var result = await check(cts.Token);
            status.Text = result.IsReady ? "Connected." : result.Message;
        }
        catch (Exception ex)
        {
            status.Text = $"Failed: {ex.GetBaseException().Message}";
        }
    }

    private async void OnDownloadClicked(object? sender, EventArgs e)
    {
        var tag = string.IsNullOrWhiteSpace(_ollamaModel.Text) ? AiConfig.DefaultOllamaModel : _ollamaModel.Text.Trim();

        _downloadButton.Enabled = false;
        _ollamaModel.Enabled = false;
        _progressBar.Visible = true;
        _progressBar.Indeterminate = true;
        _textStatus.Text = "Checking Ollama...";
        _cts = new CancellationTokenSource();

        try
        {
            if (!await OllamaService.IsAvailableAsync(_cts.Token))
            {
                _textStatus.Text = "Ollama is not running. Start Ollama and try again. Download at https://ollama.com";
                return;
            }

            if (await OllamaService.HasModelAsync(tag, _cts.Token))
            {
                _textStatus.Text = $"'{tag}' is already downloaded.";
                return;
            }

            _textStatus.Text = $"Downloading {tag}... this may take a while.";
            var progress = new Progress<(string status, int percent)>(report =>
            {
                Application.Instance.Invoke(() =>
                {
                    _textStatus.Text = report.status;
                    if (report.percent >= 0)
                    {
                        _progressBar.Indeterminate = false;
                        _progressBar.Value = Math.Min(100, report.percent);
                    }
                    else
                    {
                        _progressBar.Indeterminate = true;
                    }
                });
            });

            await OllamaService.PullModelAsync(tag, progress, _cts.Token);
            _progressBar.Indeterminate = false;
            _progressBar.Value = 100;
            _textStatus.Text = $"Downloaded {tag}. Click Save to use it.";
        }
        catch (OperationCanceledException)
        {
            _textStatus.Text = "Download cancelled.";
        }
        catch (Exception ex)
        {
            _textStatus.Text = $"Download failed: {ex.Message}";
        }
        finally
        {
            _progressBar.Visible = false;
            _downloadButton.Enabled = true;
            _ollamaModel.Enabled = true;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private static string? NullIfEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text!.Trim();
}
