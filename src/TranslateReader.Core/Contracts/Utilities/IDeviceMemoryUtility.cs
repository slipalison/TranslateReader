namespace TranslateReader.Contracts.Utilities;

/// <summary>
/// Single seam for "how much memory can this process use" (D-2026-08-16-llm-mobile-8). Deliberately
/// platform-agnostic: no <c>ActivityManager</c> on Android, no <c>os_proc_available_memory</c> on
/// iOS -- just the one number every TFM can report, used only to refuse translation gracefully
/// before an out-of-memory kill, never to promise performance.
/// </summary>
public interface IDeviceMemoryUtility
{
    /// <summary>Total memory (in bytes) this process is currently allowed to use.</summary>
    long GetAvailableMemoryBytes();
}
