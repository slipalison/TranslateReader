namespace TranslateReader.Models;

/// <summary>
/// Signals that translation cannot proceed on this device: either the platform has no native
/// inference backend (<see cref="NativeBackendPlan.IsManagedBackendSupported"/> is
/// <see langword="false"/>) or the device does not report enough available memory for the
/// selected model (<see cref="ModelInfo.RequiredMemoryBytes"/>). This is the only way
/// <c>TranslationManager.InitializeEngineIfNeededAsync</c> refuses to initialize the engine
/// (D-2026-08-16-llm-mobile-8); the message is generic and actionable, safe to surface directly
/// to the user at the PageModel <c>[RelayCommand]</c> boundary.
/// </summary>
public sealed class TranslationUnavailableException : Exception
{
    public TranslationUnavailableException()
        : base("Translation is not available on this device.")
    {
    }

    public TranslationUnavailableException(string message)
        : base(message)
    {
    }

    public TranslationUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
