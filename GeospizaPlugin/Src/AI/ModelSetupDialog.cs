using System;
using System.Collections.Generic;
using System.Threading;
using Eto.Drawing;
using Eto.Forms;

namespace GeospizaPlugin.AI;

public class ModelSetupDialog : Dialog
{
    private readonly DropDown _modelDropDown;
    private readonly Label _statusLabel;
    private readonly ProgressBar _progressBar;
    private readonly Button _downloadButton;
    private readonly Button _cancelButton;
    private CancellationTokenSource? _cts;

    private static readonly List<(string Tag, string Label)> ModelChoices = new()
    {
        ("gemma4:e2b", "Gemma 4 E2B — ~7 GB (edge/laptop, fastest)"),
        ("gemma4:e4b", "Gemma 4 E4B — ~10 GB (recommended)"),
        ("gemma4:26b", "Gemma 4 26B — ~18 GB (workstation GPU, best quality)"),
        ("gemma4:31b", "Gemma 4 31B — ~20 GB (workstation GPU, dense model)"),
    };

    public string? SelectedModel { get; private set; }

    public ModelSetupDialog()
    {
        Title = "Geospiza AI Setup — Select Gemma 4 Model";
        Resizable = false;
        MinimumSize = new Size(480, 230);

        var currentModel = AiConfig.Load().SelectedModel;
        var currentIndex = ModelChoices.FindIndex(m => m.Tag == currentModel);

        _modelDropDown = new DropDown();
        foreach (var (_, label) in ModelChoices)
            _modelDropDown.Items.Add(label);
        _modelDropDown.SelectedIndex = currentIndex >= 0 ? currentIndex : 1;
        _modelDropDown.SelectedIndexChanged += (_, _) => UpdateButtonLabel();

        _statusLabel = new Label
        {
            Text = currentIndex >= 0
                ? $"Current model: {currentModel}. Select a model and click Download & Use."
                : "Select a model and click Download & Use. Ollama must be running.",
            Wrap = WrapMode.Word
        };

        _progressBar = new ProgressBar { MinValue = 0, MaxValue = 100, Value = 0, Visible = false, Indeterminate = true };

        _downloadButton = new Button { Text = "Download & Use" };
        _downloadButton.Click += OnDownloadClicked;

        _cancelButton = new Button { Text = "Cancel" };
        _cancelButton.Click += (_, _) =>
        {
            _cts?.Cancel();
            Close();
        };

        DefaultButton = _downloadButton;
        AbortButton = _cancelButton;
        LoadComplete += (_, _) => UpdateButtonLabel();

        Content = new TableLayout
        {
            Padding = new Padding(16),
            Spacing = new Size(8, 10),
            Rows =
            {
                new TableRow(new Label { Text = "Model:", VerticalAlignment = VerticalAlignment.Center }, _modelDropDown),
                new TableRow(_statusLabel) { ScaleHeight = false },
                new TableRow(_progressBar),
                new TableRow(
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        Items = { _downloadButton, _cancelButton }
                    })
            }
        };
    }

    private async void UpdateButtonLabel()
    {
        var idx = _modelDropDown.SelectedIndex;
        if (idx < 0 || idx >= ModelChoices.Count) return;
        var tag = ModelChoices[idx].Tag;
        var already = await OllamaService.HasModelAsync(tag);
        _downloadButton.Text = already ? "Use" : "Download & Use";
    }

    private async void OnDownloadClicked(object? sender, EventArgs e)
    {
        var idx = _modelDropDown.SelectedIndex;
        if (idx < 0 || idx >= ModelChoices.Count) return;

        var tag = ModelChoices[idx].Tag;

        _downloadButton.Enabled = false;
        _modelDropDown.Enabled = false;
        _progressBar.Visible = true;
        _statusLabel.Text = "Checking Ollama...";

        _cts = new CancellationTokenSource();

        try
        {
            if (!await OllamaService.IsAvailableAsync(_cts.Token))
            {
                _statusLabel.Text = "Ollama is not running. Start Ollama and try again. Download at https://ollama.com";
                return;
            }

            if (await OllamaService.HasModelAsync(tag, _cts.Token))
            {
                _statusLabel.Text = $"Model '{tag}' is already available.";
                SaveAndClose(tag);
                return;
            }

            _statusLabel.Text = $"Downloading {tag}... this may take a while.";

            var progress = new Progress<(string status, int percent)>(report =>
            {
                Application.Instance.Invoke(() =>
                {
                    _statusLabel.Text = report.status;
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

            _progressBar.Value = 100;
            _statusLabel.Text = "Download complete!";
            SaveAndClose(tag);
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "Cancelled.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error: {ex.Message}";
        }
        finally
        {
            _downloadButton.Enabled = true;
            _modelDropDown.Enabled = true;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void SaveAndClose(string tag)
    {
        SelectedModel = tag;
        var config = AiConfig.Load();
        config.SelectedModel = tag;
        config.SetupComplete = true;
        config.Save();
        Close();
    }
}
