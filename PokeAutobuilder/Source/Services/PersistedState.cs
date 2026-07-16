namespace PokeAutobuilder.Source.Services
{
    // Wraps a value mirrored to browser storage, replacing the copy-pasted
    // "set field, raise change event, fire-and-forget save" triplet that used to be
    // hand-rolled per property in ProfileService/SessionService.
    //
    // Set() is fire-and-forget (for property setters bound from markup, where callers
    // don't await). SetAsync() awaits the actual storage write completing, for call
    // sites that need the write to be durable before they proceed (e.g. before navigating away).
    public class PersistedState<T>
    {
        private readonly string _key;
        private readonly Func<string, Task<T?>> _load;
        private readonly Func<string, T, Task> _save;

        public event Action? OnChanged;

        public T Value { get; private set; }

        public PersistedState(
            string key,
            T defaultValue,
            Func<string, Task<T?>> load,
            Func<string, T, Task> save
        )
        {
            _key = key;
            Value = defaultValue;
            _load = load;
            _save = save;
        }

        public void Set(T value)
        {
            Value = value;
            OnChanged?.Invoke();
            _ = _save(_key, value);
        }

        public async Task SetAsync(T value)
        {
            Value = value;
            OnChanged?.Invoke();
            await _save(_key, value);
        }

        // Loads the persisted value from storage, falling back to the existing (default)
        // value if nothing is stored yet. Does not raise OnChanged or re-save.
        public async Task LoadAsync()
        {
            Value = await _load(_key) is { } stored ? stored : Value;
        }
    }
}
