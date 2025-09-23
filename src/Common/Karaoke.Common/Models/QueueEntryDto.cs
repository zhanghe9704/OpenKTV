using System;

namespace Karaoke.Common.Models;

public sealed record QueueEntryDto(
    Guid EntryId,
    SongDto Song,
    string RequestedBy,
    DateTimeOffset RequestedAt,
    int Position);
