namespace ScreenToGif.Linux.Services;

/// <summary>Serializes destructive confirmation prompts behind an injectable UI callback.</summary>
public sealed class DestructiveActionGuard
{
    public bool IsPromptOpen { get; private set; }

    public async Task<bool> ConfirmAsync(bool confirmationRequired, Func<Task<bool>> showConfirmation)
    {
        if (!confirmationRequired)
            return true;
        if (IsPromptOpen)
            return false;

        IsPromptOpen = true;
        try
        {
            return await showConfirmation();
        }
        finally
        {
            IsPromptOpen = false;
        }
    }
}
