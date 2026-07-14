using Xunit;

// DataModelCache.LoadedTypes is a single mutable static list shared by every test class (see
// TestFixtures.EnsureRealTypesLoadedAsync). Disabling parallelization avoids concurrent mutation
// of that list across test classes.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
