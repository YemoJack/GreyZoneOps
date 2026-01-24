using UnityEngine;

public class GameSettingManager
{
    private static GameSettingManager _instance;

    public static GameSettingManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameSettingManager();
                if (_instance.config == null)
                {
                    _instance.config = Resources.Load<SOGameConfig>("SOGameConfig");
                    if (_instance.config == null)
                    {
                        Debug.LogWarning("GameSetting: SOGameConfig not found in Resources/SOGameConfig");
                    }
                }

            }
            return _instance;
        }
    }

    private SOGameConfig config;

    public SOGameConfig Config => config;
}
