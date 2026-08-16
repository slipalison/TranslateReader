using System.Reflection;
using System.Runtime.CompilerServices;
using LLama;
using TranslateReader.Business.Engines;

namespace TranslateReader.Tests;

/// <summary>
/// <see cref="TranslationEngine"/> talks to the real LLamaSharp SDK: <c>LLamaWeights.LoadFromFile</c>
/// is a static call with no seam, so -- unlike <see cref="LlamaCppTranslationEngineTests"/>, which
/// mocks <c>ILlamaNativeAccess</c> -- these tests can never let a call actually reach it (this test
/// project has no native backend package deployed, and a real load needs a GGUF fixture; see the two
/// [Skip] integration tests below). The concurrency-guard tests use reflection to reach the private
/// <c>_initLock</c>/<c>_weights</c> fields (an established pattern in this suite, see
/// HybridWebViewContractTests/ParsingEngineEdgeCaseTests) so the guard itself -- introduced to close
/// the same csharp.md S3 gap as <see cref="LlamaCppTranslationEngine"/> -- is proven without ever
/// invoking the native loader.
/// </summary>
public class TranslationEngineTests
{
    [Fact]
    public void IsReady_ReturnsFalse_BeforeInitialization()
    {
        using var sut = new TranslationEngine();

        Assert.False(sut.IsReady);
    }

    [Fact]
    public void Dispose_SetsIsReadyToFalse()
    {
        var sut = new TranslationEngine();

        sut.Dispose();

        Assert.False(sut.IsReady);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var sut = new TranslationEngine();

        sut.Dispose();
        sut.Dispose();

        Assert.False(sut.IsReady);
    }

    [Fact]
    public async Task InitializeAsync_ThrowsObjectDisposedException_AfterDispose()
    {
        var sut = new TranslationEngine();
        sut.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            sut.InitializeAsync("fake.gguf", CancellationToken.None));
    }

    [Fact]
    public async Task GenerateAsync_ThrowsInvalidOperation_BeforeInitialization()
    {
        using var sut = new TranslationEngine();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GenerateAsync("system", "test", 0.1f, 50, CancellationToken.None));
    }

    // Bounds every wait in the two concurrency tests below: a correct guard resolves them almost
    // instantly, so this only ever fires if a future regression turns a wait into a hang -- and
    // critically, it is what stands between a regressed guard and an unbounded wait on the pending
    // call, which -- unlike LlamaCppTranslationEngineTests -- would otherwise risk actually reaching
    // the real, unmockable LLamaWeights.LoadFromFile("fake.gguf") native call.
    private static readonly TimeSpan LockGuardTimeout = TimeSpan.FromSeconds(5);

    // csharp.md S3: proves InitializeAsync genuinely waits for the SemaphoreSlim guard instead of
    // racing ahead -- and that once the wait ends because another caller already finished
    // initializing (simulated below, since the real load has no seam here), the re-check inside the
    // lock skips reinitialization instead of touching LLamaWeights.LoadFromFile a second time.
    [Fact]
    public async Task InitializeAsync_WhenAnotherCallIsHoldingTheLock_WaitsAndSkipsReinitializationOnceItSeesTheOtherCallFinished()
    {
        var sut = new TranslationEngine();
        var initLock = GetInitLock(sut);

        await initLock.WaitAsync();
        var pending = sut.InitializeAsync("fake.gguf", CancellationToken.None);

        // Still queued on the held lock -- an unguarded InitializeAsync would have raced ahead and
        // already completed (or faulted trying to load "fake.gguf") by this point.
        Assert.False(pending.IsCompleted);

        // Stand-in for "the call that was holding the lock finished loading": bypasses the ctor so
        // no native call happens, only the reference-type non-null check IsReady depends on.
        SetWeights(sut, (LLamaWeights)RuntimeHelpers.GetUninitializedObject(typeof(LLamaWeights)));
        initLock.Release();

        await pending.WaitAsync(LockGuardTimeout);

        Assert.True(sut.IsReady);
    }

    [Fact]
    public async Task InitializeAsync_WhenCancelledWhileWaitingForTheLock_ThrowsOperationCanceledWithoutLoading()
    {
        var sut = new TranslationEngine();
        var initLock = GetInitLock(sut);

        await initLock.WaitAsync();
        using var cts = new CancellationTokenSource();
        var pending = sut.InitializeAsync("fake.gguf", cts.Token);
        Assert.False(pending.IsCompleted);

        cts.Cancel();

        // Never swallowed, never turned into an error state (csharp.md S1).
        await Assert.ThrowsAsync<OperationCanceledException>(() => pending.WaitAsync(LockGuardTimeout));

        initLock.Release();
        Assert.False(sut.IsReady);
    }

    private static SemaphoreSlim GetInitLock(TranslationEngine engine)
    {
        var field = typeof(TranslationEngine).GetField("_initLock", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("TranslationEngine._initLock does not exist.");
        return (SemaphoreSlim)field.GetValue(engine)!;
    }

    private static void SetWeights(TranslationEngine engine, LLamaWeights weights)
    {
        var field = typeof(TranslationEngine).GetField("_weights", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("TranslationEngine._weights does not exist.");
        field.SetValue(engine, weights);
    }

    // xUnit1004: the two integration tests below need a real .gguf fixture and are opt-in via
    // LLAMASHARP_TEST_MODEL. Waiver per D-2026-07-30-sonar-zero-issues-3 mechanism (c), on the
    // deliberate skip locked by D-2026-07-30-regression-suite-5(2): unskipping breaks CI, which
    // has no model fixture.
#pragma warning disable xUnit1004
    [Trait("Category", "Integration")]
    [Fact(Skip = "Requires GGUF model file for local development")]
    public async Task InitializeAsync_LoadsModel_WithValidPath()
    {
        using var sut = new TranslationEngine();
        var modelPath = Environment.GetEnvironmentVariable("LLAMASHARP_TEST_MODEL")
            ?? "path/to/test-model.gguf";

        await sut.InitializeAsync(modelPath, CancellationToken.None);

        Assert.True(sut.IsReady);
    }

    [Trait("Category", "Integration")]
    [Fact(Skip = "Requires GGUF model file for local development")]
    public async Task GenerateAsync_ProducesOutput_WithValidModel()
    {
        using var sut = new TranslationEngine();
        var modelPath = Environment.GetEnvironmentVariable("LLAMASHARP_TEST_MODEL")
            ?? "path/to/test-model.gguf";

        await sut.InitializeAsync(modelPath, CancellationToken.None);
        var result = await sut.GenerateAsync("Translate to Portuguese", "Hello", 0.1f, 50, CancellationToken.None);

        Assert.NotEmpty(result);
    }
#pragma warning restore xUnit1004
}
