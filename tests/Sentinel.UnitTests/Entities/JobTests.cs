using Sentinel.Core.Entities;
using Sentinel.Core.Enums;

namespace Sentinel.UnitTests.Entities;

public sealed class JobTests
{
    [Fact]
    public void Constructor_WithValidName_CreatesPendingJob()
    {
        // Arrange & Act
        var job = new Job("Send Email");

        // Assert
        Assert.NotEqual(Guid.Empty, job.Id);
        Assert.Equal("Send Email", job.Name);
        Assert.Equal(JobStatus.Pending, job.Status);
        Assert.NotEqual(default, job.CreatedAtUtc);
        Assert.Null(job.StartedAtUtc);
        Assert.Null(job.CompletedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidName_ThrowsException(
        string? name)
    {
        Assert.Throws<ArgumentException>(
            () => new Job(name!));
    }

    [Fact]
    public void Constructor_TrimsJobName()
    {
        var job = new Job("  Send Email  ");

        Assert.Equal("Send Email", job.Name);
    }

    [Fact]
    public void Start_WhenPending_ChangesStatusToRunning()
    {
        var job = new Job("Send Email");

        job.Start();

        Assert.Equal(JobStatus.Running, job.Status);
        Assert.NotNull(job.StartedAtUtc);
    }

    [Fact]
    public void Start_WhenAlreadyRunning_ThrowsException()
    {
        var job = new Job("Send Email");
        job.Start();

        Assert.Throws<InvalidOperationException>(
            () => job.Start());
    }

    [Fact]
    public void Complete_WhenRunning_ChangesStatusToCompleted()
    {
        var job = new Job("Send Email");
        job.Start();

        job.Complete();

        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.NotNull(job.CompletedAtUtc);
    }

    [Fact]
    public void Complete_WhenPending_ThrowsException()
    {
        var job = new Job("Send Email");

        Assert.Throws<InvalidOperationException>(
            () => job.Complete());
    }

    [Fact]
    public void Fail_WhenRunning_ChangesStatusToFailed()
    {
        var job = new Job("Send Email");
        job.Start();

        job.Fail();

        Assert.Equal(JobStatus.Failed, job.Status);
        Assert.NotNull(job.CompletedAtUtc);
    }

    [Fact]
    public void Cancel_WhenPending_ChangesStatusToCancelled()
    {
        var job = new Job("Send Email");

        job.Cancel();

        Assert.Equal(JobStatus.Cancelled, job.Status);
        Assert.NotNull(job.CompletedAtUtc);
    }

    [Fact]
    public void Cancel_WhenRunning_ThrowsException()
    {
        var job = new Job("Send Email");
        job.Start();

        Assert.Throws<InvalidOperationException>(
            () => job.Cancel());
    }

    [Fact]
    public void CompletedJob_CannotBeStartedAgain()
    {
        var job = new Job("Send Email");
        job.Start();
        job.Complete();

        Assert.Throws<InvalidOperationException>(
            () => job.Start());
    }
    [Fact]
    public void Constructor_ShouldRejectNameLongerThan200Characters()
    {
        var name = new string('A', 201);

        Assert.Throws<ArgumentException>(() => new Job(name));
    }
}