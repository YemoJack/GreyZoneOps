using System;
using UnityEngine;

public class SaveLoaderEasy : ISaveLoader
{
    private const string FallbackFilePath = "game_save.es3";

    private readonly ES3Settings baseSettings;

    public string DefaultFilePath { get; }

    public SaveLoaderEasy()
    {
        DefaultFilePath = ResolveDefaultFilePath();
        baseSettings = BuildBaseSettings(DefaultFilePath);
    }

    public void Save<T>(string key, T value, string filePath = null)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("SaveLoaderEasy.Save: key is null or empty.");
            return;
        }

        try
        {
            ES3.Save(key, value, CreateSettings(filePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveLoaderEasy.Save failed, key={key}, file={ResolvePath(filePath)}, error={e}");
        }
    }

    public T Load<T>(string key, string filePath = null)
    {
        return Load(key, default(T), filePath);
    }

    public T Load<T>(string key, T defaultValue, string filePath = null)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("SaveLoaderEasy.Load: key is null or empty.");
            return defaultValue;
        }

        try
        {
            return ES3.Load(key, defaultValue, CreateSettings(filePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveLoaderEasy.Load failed, key={key}, file={ResolvePath(filePath)}, error={e}");
            return defaultValue;
        }
    }

    public bool TryLoad<T>(string key, out T value, string filePath = null)
    {
        value = default(T);

        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("SaveLoaderEasy.TryLoad: key is null or empty.");
            return false;
        }

        try
        {
            var settings = CreateSettings(filePath);
            if (!ES3.KeyExists(key, settings))
            {
                return false;
            }

            value = ES3.Load<T>(key, settings);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveLoaderEasy.TryLoad failed, key={key}, file={ResolvePath(filePath)}, error={e}");
            return false;
        }
    }

    public bool KeyExists(string key, string filePath = null)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        try
        {
            return ES3.KeyExists(key, CreateSettings(filePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveLoaderEasy.KeyExists failed, key={key}, file={ResolvePath(filePath)}, error={e}");
            return false;
        }
    }

    public bool FileExists(string filePath = null)
    {
        try
        {
            return ES3.FileExists(CreateSettings(filePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveLoaderEasy.FileExists failed, file={ResolvePath(filePath)}, error={e}");
            return false;
        }
    }

    public void DeleteKey(string key, string filePath = null)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        try
        {
            ES3.DeleteKey(key, CreateSettings(filePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveLoaderEasy.DeleteKey failed, key={key}, file={ResolvePath(filePath)}, error={e}");
        }
    }

    public void DeleteFile(string filePath = null)
    {
        try
        {
            ES3.DeleteFile(CreateSettings(filePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveLoaderEasy.DeleteFile failed, file={ResolvePath(filePath)}, error={e}");
        }
    }

    private ES3Settings CreateSettings(string filePath = null)
    {
        return new ES3Settings(ResolvePath(filePath), baseSettings);
    }

    private string ResolvePath(string filePath)
    {
        return string.IsNullOrWhiteSpace(filePath) ? DefaultFilePath : filePath;
    }

    private static string ResolveDefaultFilePath()
    {
        var config = GameSettingManager.Instance?.Config;
        if (config != null && !string.IsNullOrWhiteSpace(config.SaveFileName))
        {
            return config.SaveFileName;
        }

        return FallbackFilePath;
    }

    private static ES3Settings BuildBaseSettings(string defaultFilePath)
    {
        var settings = new ES3Settings(defaultFilePath);
        settings.location = ES3.Location.File;
        settings.directory = ES3.Directory.PersistentDataPath;
        settings.format = ES3.Format.JSON;

        var config = GameSettingManager.Instance?.Config;
        if (config != null)
        {
            settings.compressionType = config.SaveUseCompression
                ? ES3.CompressionType.Gzip
                : ES3.CompressionType.None;

            if (config.SaveUseEncryption && !string.IsNullOrEmpty(config.SaveEncryptionPassword))
            {
                settings.encryptionType = ES3.EncryptionType.AES;
                settings.encryptionPassword = config.SaveEncryptionPassword;
            }
            else
            {
                settings.encryptionType = ES3.EncryptionType.None;
            }
        }
        else
        {
            settings.compressionType = ES3.CompressionType.None;
            settings.encryptionType = ES3.EncryptionType.None;
        }

        return settings;
    }
}
