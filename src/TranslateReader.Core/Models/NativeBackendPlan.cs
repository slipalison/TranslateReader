namespace TranslateReader.Models;

/// <summary>Platforms the managed translation engine (<c>TranslationEngine</c>, LLamaSharp-backed)
/// is asked to run on. Kept separate from any OS-detection call site so the mapping stays testable
/// as plain data.</summary>
public enum TranslationPlatform
{
    Windows,
    Android,
    IOS,
    MacCatalyst,
    Other
}

/// <summary>
/// Native backend configuration for the managed translation engine, as pure data keyed by
/// <see cref="TranslationPlatform"/>. Every literal that used to be hardcoded inside
/// <c>TranslationEngine.ConfigureNativeLibrary</c> (search directory, CUDA/Vulkan toggles) lives
/// here instead, so the four platform behaviors are unit-testable on a single machine without
/// touching LLamaSharp itself (D-2026-08-16-llm-mobile-3).
/// </summary>
/// <param name="IsManagedBackendSupported">
/// Whether the LLamaSharp-backed engine can run at all on this platform. <see langword="false"/>
/// for iOS/MacCatalyst: LLamaSharp's native loader throws <see cref="PlatformNotSupportedException"/>
/// from its own static constructor on those platforms (D-2026-08-16-llm-mobile-5), so callers must
/// check this before ever touching the engine.
/// </param>
/// <param name="UseCuda">Whether CUDA acceleration should be requested.</param>
/// <param name="UseVulkan">Whether Vulkan acceleration should be requested.</param>
/// <param name="UseAutoFallback">Whether LLamaSharp may silently fall back to a different backend.</param>
/// <param name="SearchDirectory">
/// Extra native-library search path, relative to the app base directory, or <see langword="null"/>
/// when the platform's default resolution (e.g. the Android APK's native library path) is enough.
/// </param>
public sealed record NativeBackendPlan(
    bool IsManagedBackendSupported,
    bool UseCuda,
    bool UseVulkan,
    bool UseAutoFallback,
    string? SearchDirectory)
{
    private const string RuntimesDirectoryName = "runtimes";
    private const string WindowsRuntimeIdentifier = "win-x64";
    private const string Cuda12DirectoryName = "cuda12";
    private const string NativeDirectoryName = "native";

    private static readonly string WindowsCudaSearchDirectory = Path.Combine(
        RuntimesDirectoryName, WindowsRuntimeIdentifier, NativeDirectoryName, Cuda12DirectoryName);

    private static readonly NativeBackendPlan Unsupported = new(
        IsManagedBackendSupported: false,
        UseCuda: false,
        UseVulkan: false,
        UseAutoFallback: false,
        SearchDirectory: null);

    /// <summary>Computes the backend plan for <paramref name="platform"/>. Pure function of its
    /// input: the same platform always yields the same plan, which is what makes every platform
    /// testable from a single machine (D-2026-08-16-llm-mobile-3).</summary>
    public static NativeBackendPlan For(TranslationPlatform platform) => platform switch
    {
        TranslationPlatform.Windows => new NativeBackendPlan(
            IsManagedBackendSupported: true,
            UseCuda: true,
            UseVulkan: false,
            UseAutoFallback: false,
            SearchDirectory: WindowsCudaSearchDirectory),

        TranslationPlatform.Android => new NativeBackendPlan(
            IsManagedBackendSupported: true,
            UseCuda: false,
            UseVulkan: false,
            UseAutoFallback: false,
            SearchDirectory: null),

        TranslationPlatform.IOS => Unsupported,

        TranslationPlatform.MacCatalyst => Unsupported,

        _ => Unsupported
    };
}
