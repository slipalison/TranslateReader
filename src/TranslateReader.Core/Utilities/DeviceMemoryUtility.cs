using TranslateReader.Contracts.Utilities;

namespace TranslateReader.Utilities;

/// <summary>
/// Reads the process memory ceiling from the runtime GC info -- the same number that governs
/// when this process gets OOM-killed on mobile, and the only platform-agnostic signal available
/// without per-TFM code (D-2026-08-16-llm-mobile-8).
/// </summary>
public sealed class DeviceMemoryUtility : IDeviceMemoryUtility
{
    public long GetAvailableMemoryBytes() => GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
}
