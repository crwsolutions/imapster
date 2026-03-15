using Imapster.Models;
using Imapster.Repositories;
using Imapster.Services;


namespace Imapster.ViewModels;

public partial class PromptEditorPopupViewModel : ObservableObject
{
    private readonly ILogger<PromptEditorPopupViewModel> _logger;
    private readonly IPromptRepository _promptRepository;

    [ObservableProperty]
    private string _verwijderRegels;

    [ObservableProperty]
    private string _behoudenRegels;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _staticIntro;

    [ObservableProperty]
    private string _staticOutputFormat;

    public object? Result { get; set; }

    public PromptEditorPopupViewModel(ILogger<PromptEditorPopupViewModel> logger, IPromptRepository promptRepository)
    {
        _logger = logger;
        _promptRepository = promptRepository;
        _verwijderRegels = EmailAiService.DefaultVerwijderRegels;
        _behoudenRegels = EmailAiService.DefaultBehoudenRegels;
        _staticIntro = EmailAiService.StaticIntro;
        _staticOutputFormat = EmailAiService.StaticOutputFormat;
    }

    [RelayCommand]
    private async Task LoadRulesAsync()
    {
        IsBusy = true;
        try
        {
            var verwijderPrompt = await _promptRepository.GetVerwijderRegelsAsync();
            if (verwijderPrompt != null && !string.IsNullOrWhiteSpace(verwijderPrompt.Prompt))
            {
                VerwijderRegels = verwijderPrompt.Prompt;
            }

            var behoudenPrompt = await _promptRepository.GetBehoudenRegelsAsync();
            if (behoudenPrompt != null && !string.IsNullOrWhiteSpace(behoudenPrompt.Prompt))
            {
                BehoudenRegels = behoudenPrompt.Prompt;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading rules");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsBusy = true;
        try
        {
            await _promptRepository.UpsertRulesAsync(VerwijderRegels, BehoudenRegels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving rules");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ResetToDefaultAsync()
    {
        VerwijderRegels = EmailAiService.DefaultVerwijderRegels;
        BehoudenRegels = EmailAiService.DefaultBehoudenRegels;
    }
}