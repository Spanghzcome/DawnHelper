using System;
using Celeste.Mod.DawnHelper.Entities;

namespace Celeste.Mod.DawnHelper;

public class DawnHelperModule : EverestModule {
    public static DawnHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(DawnHelperModuleSettings);
    public static DawnHelperModuleSettings Settings => (DawnHelperModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(DawnHelperModuleSession);
    public static DawnHelperModuleSession Session => (DawnHelperModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(DawnHelperModuleSaveData);
    public static DawnHelperModuleSaveData SaveData => (DawnHelperModuleSaveData) Instance._SaveData;

    public DawnHelperModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(DawnHelperModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(DawnHelperModule), LogLevel.Info);
#endif
    }

    public override void Load() {
        SuperDashBumper.Load();
    }

    public override void Unload() {
        SuperDashBumper.Unload();
    }

    public override void LoadContent(bool firstLoad)
    {
        base.LoadContent(firstLoad);
    }
}
