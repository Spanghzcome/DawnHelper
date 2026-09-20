using System.Collections;
using Monocle;

namespace Celeste.Mod.DawnHelper.Settings;

public class AlwaysBoosting
{
    internal static void Load()
    {
        On.Celeste.Player.Update += Player_Update;
        Everest.Events.Player.OnDie += OnDie;
        Everest.Events.Player.OnSpawn += OnSpawn;
    }

    internal static void Unload()
    {
        On.Celeste.Player.Update -= Player_Update;
        Everest.Events.Player.OnDie -= OnDie;
        Everest.Events.Player.OnSpawn -= OnSpawn;
    }
    
    private static void OnSpawn(Player player)
    {
        bool flag = DawnHelperModule.Settings.AlwaysBoosting.Enabled;
        if (flag)
        {
            player.Add(new CoroutineCheck());
            player.Get<CoroutineCheck>().running = false;
        }
    }

    internal class CoroutineCheck : Component
    {
        public bool running;

        public CoroutineCheck() : base(false, false) {}
    }
        
    private static void OnDie(Player player)
    {
        if (DawnHelperModule.Settings.AlwaysBoosting.Enabled && player.Get<CoroutineCheck>() is not null)
            player.Get<CoroutineCheck>().running = false;
    }
           
    private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
    {
        bool flag = DawnHelperModule.Settings.AlwaysBoosting.Enabled;
        orig(self);
        if (flag)
        {
            if (self.Get<CoroutineCheck>() is {running: false} && (!DawnHelperModule.Settings.AlwaysBoosting.RenderParticles || !self.JustRespawned))
            {
                self.Add(new Coroutine(FakeBoosterRoutine(self), removeOnComplete: true));
            }
        }
    }

    private static IEnumerator FakeBoosterRoutine(Player player)
    {
        player.Get<CoroutineCheck>().running = true;
        Booster booster;
        player.Scene.Add(booster = new Booster(player.Center, false));
        yield return null;
        yield return null;
        
        player.StateMachine.State = Player.StDash;
        player.Scene.Remove(booster.outline);
        
        while(player.StateMachine.State == Player.StDash)
            yield return null;
        
        player.Get<CoroutineCheck>().running = false;
        if (DawnHelperModule.Settings.AlwaysBoosting.RenderParticles)
            yield return 0.6f;
        else
            yield return null;
        
        player.Scene.Remove(booster);
    }
}