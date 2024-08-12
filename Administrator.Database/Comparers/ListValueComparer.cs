using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Administrator.Database;

public class ListValueComparer<T>() : ValueComparer<List<T>>((l, r) => l!.SequenceEqual(r!), x => x.GetHashCode(), x => x.ToList());