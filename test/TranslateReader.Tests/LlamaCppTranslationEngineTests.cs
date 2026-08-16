using NSubstitute;
using TranslateReader.Business.Engines;
using TranslateReader.Contracts.Access;

namespace TranslateReader.Tests;

/// <summary>
/// Proves the generation LOOP (tokenize -> decode -> sample -> detokenize, streaming, cancellation,
/// dispose) that <see cref="LlamaCppTranslationEngine"/> owns, entirely through NSubstitute over
/// <see cref="ILlamaNativeAccess"/> -- no device, no GGUF file, nothing platform-specific
/// (D-2026-08-16-llm-mobile-5). This does NOT prove real inference: that is Deferred to PR review,
/// since no machine in this phase can compile or run the iOS binding.
/// </summary>
public class LlamaCppTranslationEngineTests
{
    private readonly ILlamaNativeAccess _nativeAccess = Substitute.For<ILlamaNativeAccess>();
    private readonly LlamaCppTranslationEngine _sut;

    public LlamaCppTranslationEngineTests()
    {
        _sut = new LlamaCppTranslationEngine(_nativeAccess);
    }

    [Fact]
    public async Task InitializeAsync_LoadsTheModelAndCreatesAContext()
    {
        await _sut.InitializeAsync("/models/model.gguf", CancellationToken.None);

        _nativeAccess.Received(1).LoadModel("/models/model.gguf");
        _nativeAccess.Received(1).CreateContext(Arg.Any<int>());
        Assert.True(_sut.IsReady);
    }

    [Fact]
    public async Task InitializeAsync_WhenAlreadyReady_SkipsReinitialization()
    {
        await _sut.InitializeAsync("/models/model.gguf", CancellationToken.None);
        _nativeAccess.ClearReceivedCalls();

        await _sut.InitializeAsync("/models/model.gguf", CancellationToken.None);

        _nativeAccess.DidNotReceive().LoadModel(Arg.Any<string>());
        _nativeAccess.DidNotReceive().CreateContext(Arg.Any<int>());
    }

    [Fact]
    public async Task InitializeAsync_WhenAlreadyCancelled_ThrowsOperationCanceledWithoutLoadingAnything()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.InitializeAsync("/models/model.gguf", cts.Token));

        _nativeAccess.DidNotReceive().LoadModel(Arg.Any<string>());
    }

    // Bounds every wait in the two concurrency tests below: a correct guard resolves them almost
    // instantly, so this only ever fires if a future regression turns a wait into a hang -- turning
    // that into a fast, readable test failure instead of an indefinitely stuck test host process.
    private static readonly TimeSpan LockGuardTimeout = TimeSpan.FromSeconds(5);

    // csharp.md S3: proves the SemaphoreSlim guard actually serializes the one-time model load.
    // LoadModel is made to block (as the real native call would take real time) until both calls
    // are in flight, so an unguarded implementation would let the second one race in and call
    // LoadModel a second time -- this is the scenario W-4 flagged as merely "mitigated by the
    // absence of an await", which does not hold once InitializeAsync is genuinely called from two
    // concurrent background flows (e.g. visible-paragraph translation racing a book-translation job).
    [Fact]
    public async Task InitializeAsync_WhenCalledConcurrently_LoadsTheModelOnlyOnce()
    {
        var loadEntered = new SemaphoreSlim(0, 100);
        var releaseLoad = new SemaphoreSlim(0, 100);
        var loadCallCount = 0;

        _nativeAccess.When(x => x.LoadModel(Arg.Any<string>())).Do(_ =>
        {
            Interlocked.Increment(ref loadCallCount);
            loadEntered.Release();
            releaseLoad.Wait();
        });

        var first = Task.Run(() => _sut.InitializeAsync("/models/model.gguf", CancellationToken.None));
        await loadEntered.WaitAsync().WaitAsync(LockGuardTimeout);

        // Started only after `first` is confirmed to be inside LoadModel (blocked), so this is a
        // genuine overlap, not two sequential calls.
        var second = Task.Run(() => _sut.InitializeAsync("/models/model.gguf", CancellationToken.None));

        // Generous release count: if the guard regressed and a second LoadModel call is blocked
        // too, this must fail the loadCallCount assertion below instead of hanging forever.
        releaseLoad.Release(10);
        await Task.WhenAll(first, second).WaitAsync(LockGuardTimeout);

        Assert.Equal(1, loadCallCount);
        _nativeAccess.Received(1).LoadModel(Arg.Any<string>());
        _nativeAccess.Received(1).CreateContext(Arg.Any<int>());
        Assert.True(_sut.IsReady);
    }

    [Fact]
    public async Task InitializeAsync_WhenCancelledWhileAnotherInitializationHoldsTheLock_ThrowsOperationCanceledWithoutLoadingAgain()
    {
        var loadEntered = new SemaphoreSlim(0, 100);
        var releaseLoad = new SemaphoreSlim(0, 100);

        _nativeAccess.When(x => x.LoadModel(Arg.Any<string>())).Do(_ =>
        {
            loadEntered.Release();
            releaseLoad.Wait();
        });

        var first = Task.Run(() => _sut.InitializeAsync("/models/model.gguf", CancellationToken.None));
        await loadEntered.WaitAsync().WaitAsync(LockGuardTimeout);

        // Task.Run (not a direct call) so this thread is never at the mercy of InitializeAsync's
        // own synchronization behavior: it must stay free to call cts.Cancel() right away.
        using var cts = new CancellationTokenSource();
        var second = Task.Run(() => _sut.InitializeAsync("/models/model.gguf", cts.Token));
        cts.Cancel();

        // Never swallowed, never turned into an error state (csharp.md S1): OperationCanceledException
        // must flow out of the pending WaitAsync exactly as it would from any other await.
        await Assert.ThrowsAsync<OperationCanceledException>(() => second.WaitAsync(LockGuardTimeout));
        _nativeAccess.Received(1).LoadModel(Arg.Any<string>());

        releaseLoad.Release(10);
        await first.WaitAsync(LockGuardTimeout);
        Assert.True(_sut.IsReady);
    }

    [Fact]
    public async Task GenerateAsync_WhenNotInitialized_ThrowsInvalidOperation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GenerateAsync("system", "user", 0.1f, 10, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateStreamingAsync_DecodesThePromptBeforeSamplingTheFirstToken()
    {
        await _sut.InitializeAsync("/models/model.gguf", CancellationToken.None);
        _nativeAccess.Tokenize(Arg.Any<string>()).Returns([10, 11, 12]);
        _nativeAccess.IsEndOfGeneration(Arg.Any<int>()).Returns(true);

        await foreach (var _ in _sut.GenerateStreamingAsync("system", "user", 0.1f, 10, CancellationToken.None))
        {
        }

        int[] expectedPromptTokens = [10, 11, 12];
        _nativeAccess.Received(1).ResetContext();
        _nativeAccess.Received(1).Tokenize(Arg.Is<string>(t => t.Contains("system", StringComparison.Ordinal) && t.Contains("user", StringComparison.Ordinal)));
        _nativeAccess.Received(1).Decode(Arg.Is<int[]>(t => t.SequenceEqual(expectedPromptTokens)));
    }

    [Fact]
    public async Task GenerateStreamingAsync_YieldsTokensUntilEndOfGeneration()
    {
        await _sut.InitializeAsync("/models/model.gguf", CancellationToken.None);
        _nativeAccess.Tokenize(Arg.Any<string>()).Returns([1]);
        _nativeAccess.SampleNextToken(Arg.Any<float>()).Returns(101, 102, 103);
        _nativeAccess.IsEndOfGeneration(101).Returns(false);
        _nativeAccess.IsEndOfGeneration(102).Returns(false);
        _nativeAccess.IsEndOfGeneration(103).Returns(true);
        _nativeAccess.TokenToText(101).Returns("Ol");
        _nativeAccess.TokenToText(102).Returns("á, ");

        var pieces = new List<string>();
        await foreach (var piece in _sut.GenerateStreamingAsync("system", "user", 0.1f, 10, CancellationToken.None))
        {
            pieces.Add(piece);
        }

        Assert.Equal(["Ol", "á, "], pieces);
        _nativeAccess.DidNotReceive().TokenToText(103);
    }

    [Fact]
    public async Task GenerateStreamingAsync_StopsAtMaxTokensEvenWithoutEndOfGeneration()
    {
        await _sut.InitializeAsync("/models/model.gguf", CancellationToken.None);
        _nativeAccess.Tokenize(Arg.Any<string>()).Returns([1]);
        _nativeAccess.SampleNextToken(Arg.Any<float>()).Returns(7);
        _nativeAccess.IsEndOfGeneration(Arg.Any<int>()).Returns(false);
        _nativeAccess.TokenToText(Arg.Any<int>()).Returns("x");

        var pieces = new List<string>();
        await foreach (var piece in _sut.GenerateStreamingAsync("system", "user", 0.1f, 3, CancellationToken.None))
        {
            pieces.Add(piece);
        }

        Assert.Equal(3, pieces.Count);
    }

    [Fact]
    public async Task GenerateStreamingAsync_WhenAlreadyCancelled_ThrowsOperationCanceledBeforeSamplingAnyToken()
    {
        await _sut.InitializeAsync("/models/model.gguf", CancellationToken.None);
        _nativeAccess.Tokenize(Arg.Any<string>()).Returns([1]);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        async Task ConsumeAsync()
        {
            await foreach (var _ in _sut.GenerateStreamingAsync("system", "user", 0.1f, 10, cts.Token))
            {
            }
        }

        await Assert.ThrowsAsync<OperationCanceledException>(ConsumeAsync);
        _nativeAccess.DidNotReceive().SampleNextToken(Arg.Any<float>());
    }

    [Fact]
    public async Task GenerateStreamingAsync_WhenCancelledMidStream_StopsYieldingAndThrowsOperationCanceled()
    {
        await _sut.InitializeAsync("/models/model.gguf", CancellationToken.None);
        _nativeAccess.Tokenize(Arg.Any<string>()).Returns([1]);
        _nativeAccess.SampleNextToken(Arg.Any<float>()).Returns(7);
        _nativeAccess.IsEndOfGeneration(Arg.Any<int>()).Returns(false);
        _nativeAccess.TokenToText(Arg.Any<int>()).Returns("x");

        using var cts = new CancellationTokenSource();
        var received = new List<string>();

        async Task ConsumeAsync()
        {
            await foreach (var piece in _sut.GenerateStreamingAsync("system", "user", 0.1f, 10, cts.Token))
            {
                received.Add(piece);
                cts.Cancel();
            }
        }

        await Assert.ThrowsAsync<OperationCanceledException>(ConsumeAsync);
        Assert.Single(received);
    }

    [Fact]
    public async Task GenerateAsync_AggregatesStreamedPiecesIntoOneString()
    {
        await _sut.InitializeAsync("/models/model.gguf", CancellationToken.None);
        _nativeAccess.Tokenize(Arg.Any<string>()).Returns([1]);
        _nativeAccess.SampleNextToken(Arg.Any<float>()).Returns(1, 2, 3);
        _nativeAccess.IsEndOfGeneration(3).Returns(true);
        _nativeAccess.TokenToText(1).Returns("Ol");
        _nativeAccess.TokenToText(2).Returns("á");

        var result = await _sut.GenerateAsync("system", "user", 0.1f, 10, CancellationToken.None);

        Assert.Equal("Olá", result);
    }

    [Fact]
    public async Task Dispose_ReleasesTheContextAndTheModel()
    {
        await _sut.InitializeAsync("/models/model.gguf", CancellationToken.None);

        _sut.Dispose();

        _nativeAccess.Received(1).FreeContext();
        _nativeAccess.Received(1).FreeModel();
        Assert.False(_sut.IsReady);
    }

    [Fact]
    public void Dispose_WhenNeverInitialized_DoesNotCallNativeFree()
    {
        _sut.Dispose();

        _nativeAccess.DidNotReceive().FreeContext();
        _nativeAccess.DidNotReceive().FreeModel();
    }

    [Fact]
    public async Task Dispose_IsIdempotent()
    {
        await _sut.InitializeAsync("/models/model.gguf", CancellationToken.None);

        _sut.Dispose();
        _sut.Dispose();

        _nativeAccess.Received(1).FreeContext();
        _nativeAccess.Received(1).FreeModel();
    }

    [Fact]
    public async Task InitializeAsync_WhenDisposed_ThrowsObjectDisposed()
    {
        _sut.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _sut.InitializeAsync("/models/model.gguf", CancellationToken.None));
    }

    [Fact]
    public async Task GenerateStreamingAsync_WhenDisposed_ThrowsObjectDisposed()
    {
        await _sut.InitializeAsync("/models/model.gguf", CancellationToken.None);
        _sut.Dispose();

        async Task ConsumeAsync()
        {
            await foreach (var _ in _sut.GenerateStreamingAsync("system", "user", 0.1f, 10, CancellationToken.None))
            {
            }
        }

        await Assert.ThrowsAsync<ObjectDisposedException>(ConsumeAsync);
    }
}
