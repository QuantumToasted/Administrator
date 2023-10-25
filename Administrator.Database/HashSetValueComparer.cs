using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Administrator.Database;

public sealed class HashSetValueComparer<T>() : ValueComparer<HashSet<T>>((l, r) => l!.SequenceEqual(r!), x => x.GetHashCode(), x => x.ToHashSet());