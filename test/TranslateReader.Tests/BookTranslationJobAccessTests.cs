using TranslateReader.Access;
using TranslateReader.Models;

namespace TranslateReader.Tests;

public class BookTranslationJobAccessTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();
    private BookTranslationJobAccess CreateSut() => new(_db.ConnectionString, initializeOnStartup: true);

    public void Dispose() => _db.Dispose();

    private static BookTranslationJob MakeJob(
        int bookId = 1,
        string status = "InProgress",
        int lastCompletedChapterIndex = -1) => new()
        {
            BookId = bookId,
            SourceLanguage = "English",
            TargetLanguage = "Brazilian Portuguese (PT-BR)",
            Status = status,
            LastCompletedChapterIndex = lastCompletedChapterIndex
        };

    [Fact]
    public async Task FetchActiveJobAsync_ReturnsNullWhenNoJobExists()
    {
        var result = await CreateSut().FetchActiveJobAsync(1);
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveJobAsync_AssignsGeneratedIdAndRoundTripsJob()
    {
        var sut = CreateSut();
        var job = MakeJob(bookId: 7, status: "Paused", lastCompletedChapterIndex: 4);

        await sut.SaveJobAsync(job);
        var stored = await sut.FetchActiveJobAsync(7);

        Assert.True(job.Id > 0);
        Assert.NotNull(stored);
        Assert.Equal(job.Id, stored.Id);
        Assert.Equal(7, stored.BookId);
        Assert.Equal("English", stored.SourceLanguage);
        Assert.Equal("Brazilian Portuguese (PT-BR)", stored.TargetLanguage);
        Assert.Equal("Paused", stored.Status);
        Assert.Equal(4, stored.LastCompletedChapterIndex);
    }

    [Fact]
    public async Task SaveJobAsync_StampsCreatedAtEqualToUpdatedAt()
    {
        var sut = CreateSut();
        var before = DateTime.UtcNow;

        await sut.SaveJobAsync(MakeJob());
        var stored = await sut.FetchActiveJobAsync(1);

        Assert.NotNull(stored);
        Assert.Equal(stored.CreatedAt, stored.UpdatedAt);
        Assert.InRange(stored.CreatedAt.ToUniversalTime(), before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("InProgress")]
    [InlineData("Paused")]
    public async Task FetchActiveJobAsync_ReturnsJobWhoseStatusIsResumable(string status)
    {
        var sut = CreateSut();
        await sut.SaveJobAsync(MakeJob(status: status));

        var stored = await sut.FetchActiveJobAsync(1);

        Assert.NotNull(stored);
        Assert.Equal(status, stored.Status);
    }

    [Fact]
    public async Task FetchActiveJobAsync_IgnoresJobInTerminalStatus()
    {
        var sut = CreateSut();
        await sut.SaveJobAsync(MakeJob(status: "Completed"));

        var stored = await sut.FetchActiveJobAsync(1);

        Assert.Null(stored);
    }

    [Fact]
    public async Task FetchActiveJobAsync_WithTerminalJobsAroundActiveOne_ReturnsOnlyTheActiveJob()
    {
        var sut = CreateSut();
        await sut.SaveJobAsync(MakeJob(status: "Completed"));
        var active = MakeJob(status: "Paused", lastCompletedChapterIndex: 2);
        await sut.SaveJobAsync(active);
        await sut.SaveJobAsync(MakeJob(status: "Completed"));

        var stored = await sut.FetchActiveJobAsync(1);

        Assert.NotNull(stored);
        Assert.Equal(active.Id, stored.Id);
        Assert.Equal("Paused", stored.Status);
        Assert.Equal(2, stored.LastCompletedChapterIndex);
    }

    [Fact]
    public async Task FetchActiveJobAsync_IsolatesJobsByBookId()
    {
        var sut = CreateSut();
        await sut.SaveJobAsync(MakeJob(bookId: 1, lastCompletedChapterIndex: 5));

        Assert.NotNull(await sut.FetchActiveJobAsync(1));
        Assert.Null(await sut.FetchActiveJobAsync(2));
    }

    [Fact]
    public async Task UpdateJobProgressAsync_PersistsChapterIndexAndStatus()
    {
        var sut = CreateSut();
        var job = MakeJob(status: "Pending");
        await sut.SaveJobAsync(job);

        await sut.UpdateJobProgressAsync(job.Id, 12, "InProgress");
        var stored = await sut.FetchActiveJobAsync(1);

        Assert.NotNull(stored);
        Assert.Equal(12, stored.LastCompletedChapterIndex);
        Assert.Equal("InProgress", stored.Status);
    }

    [Fact]
    public async Task UpdateJobProgressAsync_ToTerminalStatus_MakesJobInactive()
    {
        var sut = CreateSut();
        var job = MakeJob(status: "InProgress");
        await sut.SaveJobAsync(job);

        await sut.UpdateJobProgressAsync(job.Id, 9, "Completed");

        Assert.Null(await sut.FetchActiveJobAsync(1));
    }

    [Fact]
    public async Task UpdateJobProgressAsync_TouchesOnlyTargetJob()
    {
        var sut = CreateSut();
        var target = MakeJob(bookId: 1);
        var other = MakeJob(bookId: 2, lastCompletedChapterIndex: 3);
        await sut.SaveJobAsync(target);
        await sut.SaveJobAsync(other);

        await sut.UpdateJobProgressAsync(target.Id, 8, "Paused");
        var untouched = await sut.FetchActiveJobAsync(2);

        Assert.NotNull(untouched);
        Assert.Equal(3, untouched.LastCompletedChapterIndex);
        Assert.Equal("InProgress", untouched.Status);
    }

    [Fact]
    public async Task DeleteJobAsync_RemovesJob()
    {
        var sut = CreateSut();
        var job = MakeJob();
        await sut.SaveJobAsync(job);

        await sut.DeleteJobAsync(job.Id);

        Assert.Null(await sut.FetchActiveJobAsync(1));
    }

    [Fact]
    public async Task DeleteJobAsync_WithUnknownId_LeavesExistingJobsIntact()
    {
        var sut = CreateSut();
        var job = MakeJob(lastCompletedChapterIndex: 1);
        await sut.SaveJobAsync(job);

        await sut.DeleteJobAsync(job.Id + 999);
        var stored = await sut.FetchActiveJobAsync(1);

        Assert.NotNull(stored);
        Assert.Equal(job.Id, stored.Id);
    }
}
