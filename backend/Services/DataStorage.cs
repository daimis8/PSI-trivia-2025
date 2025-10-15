using System.Text.Json;

namespace backend.Services;

public class DataStorage<TKey, TValue> where TValue : class where TKey : notnull
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private Dictionary<TKey, TValue> _data;
    private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

    public DataStorage(string fileName)
    {
        _filePath = Path.Combine(Directory.GetCurrentDirectory(), "data", fileName);
        _jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true 
        };
        
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _data = LoadData();
    }

    private Dictionary<TKey, TValue> LoadData()
    {
        if (!File.Exists(_filePath))
        {
            return new Dictionary<TKey, TValue>();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(json, _jsonOptions) 
                   ?? new Dictionary<TKey, TValue>();
        }
        catch
        {
            return new Dictionary<TKey, TValue>();
        }
    }

    private async Task SaveDataAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_data, _jsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    // Get value by key
    public TValue? Get(TKey key)
    {
        return _data.TryGetValue(key, out var value) ? value : null;
    }

    // Get all values
    public IEnumerable<TValue> GetAll()
    {
        return _data.Values;
    }

    // Get all keys
    public IEnumerable<TKey> GetKeys()
    {
        return _data.Keys;
    }

    // Set value (add or update)
    public async Task SetAsync(TKey key, TValue value)
    {
        _data[key] = value;
        await SaveDataAsync();
    }

    // Remove value
    public async Task<bool> RemoveAsync(TKey key)
    {
        if (_data.Remove(key))
        {
            await SaveDataAsync();
            return true;
        }
        return false;
    }

    // Check if key exists
    public bool Contains(TKey key)
    {
        return _data.ContainsKey(key);
    }

    // Clear all data
    public async Task ClearAsync()
    {
        _data.Clear();
        await SaveDataAsync();
    }
}