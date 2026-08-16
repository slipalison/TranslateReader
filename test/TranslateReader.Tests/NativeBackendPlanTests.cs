using TranslateReader.Models;

namespace TranslateReader.Tests;

/// <summary>
/// <see cref="NativeBackendPlan.For"/> is a pure function of <see cref="TranslationPlatform"/>, so
/// every platform is testable from this machine even though only Windows can ever run the real
/// LLamaSharp engine here (D-2026-08-16-llm-mobile-3).
/// </summary>
public class NativeBackendPlanTests
{
    [Fact]
    public void NativeBackendPlan_Windows_KeepsCudaAndTheWin64SearchDirectory()
    {
        var plan = NativeBackendPlan.For(TranslationPlatform.Windows);

        Assert.True(plan.IsManagedBackendSupported);
        Assert.True(plan.UseCuda);
        Assert.False(plan.UseVulkan);
        Assert.False(plan.UseAutoFallback);
        Assert.NotNull(plan.SearchDirectory);
        Assert.Contains("win-x64", plan.SearchDirectory, StringComparison.Ordinal);
        Assert.Contains("cuda12", plan.SearchDirectory, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeBackendPlan_Android_DisablesCudaAndDeclaresNoSearchDirectory()
    {
        var plan = NativeBackendPlan.For(TranslationPlatform.Android);

        Assert.True(plan.IsManagedBackendSupported);
        Assert.False(plan.UseCuda);
        Assert.False(plan.UseVulkan);
        Assert.False(plan.UseAutoFallback);
        Assert.Null(plan.SearchDirectory);
    }

    [Fact]
    public void NativeBackendPlan_IOS_ReportsTheManagedBackendAsUnsupported()
    {
        var plan = NativeBackendPlan.For(TranslationPlatform.IOS);

        Assert.False(plan.IsManagedBackendSupported);
        Assert.False(plan.UseCuda);
        Assert.Null(plan.SearchDirectory);
    }

    [Fact]
    public void NativeBackendPlan_MacCatalyst_ReportsTheManagedBackendAsUnsupported()
    {
        var plan = NativeBackendPlan.For(TranslationPlatform.MacCatalyst);

        Assert.False(plan.IsManagedBackendSupported);
        Assert.False(plan.UseCuda);
        Assert.Null(plan.SearchDirectory);
    }

    [Fact]
    public void NativeBackendPlan_UnknownPlatform_ReportsTheManagedBackendAsUnsupported()
    {
        var plan = NativeBackendPlan.For(TranslationPlatform.Other);

        Assert.False(plan.IsManagedBackendSupported);
        Assert.Null(plan.SearchDirectory);
    }
}
