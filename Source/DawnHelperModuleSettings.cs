namespace Celeste.Mod.DawnHelper;

public class DawnHelperModuleSettings : EverestModuleSettings {
    [SettingSubMenu]
    public class AlwaysBoost
    {
        public bool Enabled { get; set; }

        public bool SafeRespawn { get; set; }

        public bool RenderParticles { get; set; }
    }

    public AlwaysBoost AlwaysBoosting { get; set; } = new AlwaysBoost();
}