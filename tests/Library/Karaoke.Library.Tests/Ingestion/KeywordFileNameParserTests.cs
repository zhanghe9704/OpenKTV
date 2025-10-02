using System.Collections.Generic;
using Xunit;
using Karaoke.Library.Ingestion;
using Karaoke.Library.Configuration;

namespace Karaoke.Library.Tests.Ingestion;

public class KeywordFileNameParserTests
{
    [Fact]
    public void TryParse_WithBasicArtistSongFormat_ShouldParseCorrectly()
    {
        // Arrange
        var parser = new KeywordFileNameParser();
        var rootOptions = new LibraryRootOptions { KeywordFormat = "artist-song" };
        var globalOptions = new LibraryOptions();
        var context = new MediaFileContext(
            "TestRoot", 
            "/test/root", 
            rootOptions, 
            globalOptions, 
            "/test/root/张学友-吻别.mp3");

        // Act
        var result = parser.TryParse(context, out var metadata);

        // Assert
        Assert.True(result);
        Assert.Equal("吻别", metadata.Title);
        Assert.Equal("张学友", metadata.Artist);
    }

    [Fact]
    public void TryParse_WithArtistSongCommentFormat_ShouldParseCorrectly()
    {
        // Arrange
        var parser = new KeywordFileNameParser();
        var rootOptions = new LibraryRootOptions { KeywordFormat = "artist-song-comment" };
        var globalOptions = new LibraryOptions();
        var context = new MediaFileContext(
            "TestRoot", 
            "/test/root", 
            rootOptions, 
            globalOptions, 
            "/test/root/邓丽君-月亮代表我的心-经典版.mp3");

        // Act
        var result = parser.TryParse(context, out var metadata);

        // Assert
        Assert.True(result);
        Assert.Equal("月亮代表我的心 (经典版)", metadata.Title);
        Assert.Equal("邓丽君", metadata.Artist);
    }

    [Fact]
    public void TryParse_WithDuplicateKeywords_ShouldConcatenateValues()
    {
        // Arrange
        var parser = new KeywordFileNameParser();
        var rootOptions = new LibraryRootOptions { KeywordFormat = "artist-artist-song" };
        var globalOptions = new LibraryOptions();
        var context = new MediaFileContext(
            "TestRoot", 
            "/test/root", 
            rootOptions, 
            globalOptions, 
            "/test/root/张学友-谭咏麟-朋友.mp3");

        // Act
        var result = parser.TryParse(context, out var metadata);

        // Assert
        Assert.True(result);
        Assert.Equal("朋友", metadata.Title);
        Assert.Equal("张学友, 谭咏麟", metadata.Artist);
    }

    [Fact]
    public void TryParse_WithNoFormat_ShouldReturnFalse()
    {
        // Arrange
        var parser = new KeywordFileNameParser();
        var rootOptions = new LibraryRootOptions(); // No format
        var globalOptions = new LibraryOptions(); // No format
        var context = new MediaFileContext(
            "TestRoot", 
            "/test/root", 
            rootOptions, 
            globalOptions, 
            "/test/root/张学友-吻别.mp3");

        // Act
        var result = parser.TryParse(context, out var metadata);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryParse_WithMismatchedParts_ShouldReturnFalse()
    {
        // Arrange
        var parser = new KeywordFileNameParser();
        var rootOptions = new LibraryRootOptions { KeywordFormat = "artist-song-comment" };
        var globalOptions = new LibraryOptions();
        var context = new MediaFileContext(
            "TestRoot", 
            "/test/root", 
            rootOptions, 
            globalOptions, 
            "/test/root/张学友-吻别.mp3"); // Only 2 parts but format expects 3

        // Act
        var result = parser.TryParse(context, out var metadata);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryParse_WithGlobalFormat_ShouldUseGlobalWhenRootNotSet()
    {
        // Arrange
        var parser = new KeywordFileNameParser();
        var rootOptions = new LibraryRootOptions(); // No format
        var globalOptions = new LibraryOptions { KeywordFormat = "artist-song" };
        var context = new MediaFileContext(
            "TestRoot", 
            "/test/root", 
            rootOptions, 
            globalOptions, 
            "/test/root/周杰伦-青花瓷.mp3");

        // Act
        var result = parser.TryParse(context, out var metadata);

        // Assert
        Assert.True(result);
        Assert.Equal("青花瓷", metadata.Title);
        Assert.Equal("周杰伦", metadata.Artist);
    }

    [Fact]
    public void TryParse_WithLanguageAndGenre_ShouldFormatCorrectly()
    {
        // Arrange
        var parser = new KeywordFileNameParser();
        var rootOptions = new LibraryRootOptions { KeywordFormat = "artist-song-language-genre" };
        var globalOptions = new LibraryOptions();
        var context = new MediaFileContext(
            "TestRoot", 
            "/test/root", 
            rootOptions, 
            globalOptions, 
            "/test/root/凤凰传奇-我是一只小小鸟-国语-流行歌曲.mkv");

        // Act
        var result = parser.TryParse(context, out var metadata);

        // Assert
        Assert.True(result);
        Assert.Equal("我是一只小小鸟 [国语] (流行歌曲)", metadata.Title);
        Assert.Equal("凤凰传奇", metadata.Artist);
    }
}