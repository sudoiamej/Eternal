using System;
using System.Collections.Generic;

namespace Eternal.Models
{
    public record SnapshotEntry(string Name, string Value, string Category);

    public record SystemSnapshot(string Id, DateTime Timestamp, string Description, List<SnapshotEntry> Entries);

    public record SnapshotDiff(string Name, string OldValue, string NewValue, string Category, DiffType Type);

    public enum DiffType { Added, Removed, Modified, Identical }
}
