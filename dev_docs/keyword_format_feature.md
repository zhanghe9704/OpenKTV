# Keyword-Based File Format Support

This document describes the new keyword-based file format feature for the Karaoke application.

## Overview

In addition to the existing directory structure format (`/artist/song`) and simple hyphen format (`artist-song`), the application now supports a flexible keyword-based format that allows users to specify the order and content of filename components.

## Format Specification

The keyword format uses hyphens (`-`) to separate keywords in the filename. Each keyword represents a specific piece of metadata:

- `artist` - The song artist/performer
- `song` - The song title 
- `comment` - Additional information (e.g., version, style, year)
- `language` - Language of the song (e.g., 国语, English, 粤语)
- `genre` - Musical genre (e.g., 流行歌曲, 摇滚, 民谣)

### Examples

| Format | Filename | Result |
|--------|----------|--------|
| `artist-song` | `张学友-吻别.mp3` | Artist: "张学友", Title: "吻别" |
| `artist-song-comment` | `邓丽君-月亮代表我的心-经典版.mp3` | Artist: "邓丽君", Title: "月亮代表我的心 (经典版)" |
| `artist-song-language-genre` | `凤凰传奇-我是一只小小鸟-国语-流行歌曲.mkv` | Artist: "凤凰传奇", Title: "我是一只小小鸟 [国语] (流行歌曲)" |
| `song-artist` | `青花瓷-周杰伦.mp3` | Artist: "周杰伦", Title: "青花瓷" |
| `artist-artist-song` | `张学友-谭咏麟-朋友.mp3` | Artist: "张学友, 谭咏麟", Title: "朋友" |

## Configuration

### Global Configuration

Set a default keyword format in `config/settings.json`:

```json
{
  "Library": {
    "KeywordFormat": "artist-song-comment",
    "Roots": [
      // ... root configurations
    ]
  }
}
```

### Root-Specific Configuration

Override the global format for specific library roots:

```json
{
  "Library": {
    "KeywordFormat": "artist-song",
    "Roots": [
      {
        "Name": "chinese_collection",
        "Path": "D:/Music/Chinese",
        "KeywordFormat": "artist-song-comment"
      },
      {
        "Name": "english_collection", 
        "Path": "D:/Music/English",
        "KeywordFormat": "song-artist"
      }
    ]
  }
}
```

### UI Configuration

Users can configure keyword formats through the Settings dialog:

1. Open the application
2. Press `Ctrl+S` or click the "Settings" button
3. Set the "Global Keyword Format" field
4. For each library root, optionally set a "Keyword Format" to override the global setting
5. Click "Save" to apply changes

## Behavior Rules

### Duplicate Keywords

If a keyword appears multiple times in the format, the values are concatenated with `", "`:

- Format: `artist-artist-song`
- Filename: `张学友-谭咏麟-朋友.mp3`
- Result: Artist = "张学友, 谭咏麟"

### Comment Handling

When a `comment` keyword is present, it's appended to the song title in parentheses:

- Format: `artist-song-comment`
- Filename: `邓丽君-月亮代表我的心-经典版.mp3`
- Result: Title = "月亮代表我的心 (经典版)"

### Validation

- The number of hyphens in the filename must match the format specification
- At minimum, both `artist` and `song` keywords must be present
- If no format is specified (neither global nor root-specific), the parser will not attempt to parse the file

### Priority Order

The parser tries formats in this order:

1. Root-specific `KeywordFormat` (if set)
2. Global `KeywordFormat` (if set)
3. If neither is set, falls back to other parsers (directory structure, simple hyphen)

## Implementation Details

### Parser Registration

The `KeywordFileNameParser` is automatically registered with the dependency injection container and will be tried alongside existing parsers during the scanning process.

### Error Handling

- Files that don't match the expected format are skipped and logged
- Malformed filenames (wrong number of components) are ignored
- Missing required keywords (`artist` or `song`) cause the file to be skipped

## Testing

The feature includes comprehensive unit tests covering:

- Basic format parsing (`artist-song`)
- Complex formats with comments (`artist-song-comment`)
- Duplicate keyword handling
- Global vs. root-specific format precedence
- Error conditions (mismatched parts, missing formats)

Run tests with:
```bash
dotnet test --filter "KeywordFileNameParserTests"
```

## Migration

Existing libraries using directory structure or simple hyphen formats will continue to work unchanged. The new keyword format is additive and doesn't break existing functionality.

To migrate to keyword format:

1. Rename files to follow the desired keyword pattern
2. Configure the format in settings
3. Run a rescan to update the database