using QFramework;

public interface ISaveLoader : IUtility
{
    string DefaultFilePath { get; }

    void Save<T>(string key, T value, string filePath = null);

    T Load<T>(string key, string filePath = null);

    T Load<T>(string key, T defaultValue, string filePath = null);

    bool TryLoad<T>(string key, out T value, string filePath = null);

    bool KeyExists(string key, string filePath = null);

    bool FileExists(string filePath = null);

    void DeleteKey(string key, string filePath = null);

    void DeleteFile(string filePath = null);
}
