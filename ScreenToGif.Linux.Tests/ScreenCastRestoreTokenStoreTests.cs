using ScreenToGif.Linux.Services.Capture;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class ScreenCastRestoreTokenStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "screentogif-source-token-" + Guid.NewGuid().ToString("N"));
    private string TokenPath => Path.Combine(_directory, "source.token");

    [Fact]
    public void Tokens_are_private_single_use_and_can_be_rotated_or_forgotten()
    {
        var store = new ScreenCastRestoreTokenStore(TokenPath);
        Assert.Null(store.Take());
        store.Save("first-opaque-grant");
        Assert.True(store.HasToken);
        if (OperatingSystem.IsLinux())
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(TokenPath));
        Assert.Equal("first-opaque-grant", store.Take());
        Assert.Null(store.Take());
        Assert.False(store.HasToken);
        store.Save("replacement-grant");
        Assert.Equal("replacement-grant", store.Take());
        store.Save("forgotten-grant");
        Assert.True(store.Forget());
        Assert.Null(store.Take());
        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public void Token_size_is_bounded_in_encoded_bytes()
    {
        var store = new ScreenCastRestoreTokenStore(TokenPath);
        var token = new string('\u03c0', 2048);
        store.Save(token);
        Assert.Equal(token, store.Take());
        store.Save(new string('\u03c0', 2049));
        Assert.False(store.HasToken);
    }

    [Fact]
    public async Task Concurrent_starts_consume_one_token_only_once()
    {
        new ScreenCastRestoreTokenStore(TokenPath).Save("single-use-grant");
        var tokens = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
            new ScreenCastRestoreTokenStore(TokenPath).Take())));
        Assert.Single(tokens, token => token == "single-use-grant");
        Assert.Equal(15, tokens.Count(token => token is null));
        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public void Malformed_or_unwritable_tokens_fall_back_to_the_source_picker()
    {
        Directory.CreateDirectory(_directory);
        var store = new ScreenCastRestoreTokenStore(TokenPath);
        File.WriteAllText(TokenPath, new string('x', 4097));
        Assert.Null(store.Take());
        File.WriteAllText(TokenPath, "  ");
        Assert.Null(store.Take());
        File.WriteAllText(TokenPath, "not-a-directory");
        var blocked = new ScreenCastRestoreTokenStore(Path.Combine(TokenPath, "token"));
        blocked.Save("grant");
        Assert.False(blocked.HasToken);
        Assert.Null(blocked.Take());
        store.Save(null);
        Assert.Equal("not-a-directory", File.ReadAllText(TokenPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
