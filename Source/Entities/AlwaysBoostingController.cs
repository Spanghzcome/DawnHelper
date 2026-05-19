using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.DawnHelper.Entities;

[CustomEntity("DawnHelper/alwaysBoostingController")]

public class AlwaysBoostingController : Entity
{
    private string flag;
    private bool flagTrue;
    private bool toggled;
    private bool safeRespawn;
    private bool running;
    private bool renderParticles;
    
    public AlwaysBoostingController(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        flag = data.Attr("flag");
        safeRespawn = data.Bool("safeRespawn");
        renderParticles = data.Bool("renderBurstParticles");
        running = false;
        
    }
    
    public override void Update()
    {
        if (Scene?.Tracker.GetEntity<Player>() is not { } player) return;
        Level level = SceneAs<Level>();

        flagTrue = level.Session.GetFlag(flag);
        
        if (!running && (flagTrue || string.IsNullOrEmpty(flag)) && (!safeRespawn || !player.JustRespawned))
        { 
            player.Add(new Coroutine(FakeBoosterRoutine(player), removeOnComplete: true));
        }
    }

    private IEnumerator FakeBoosterRoutine(Player player)
    {
        running = true;
        Booster booster;
        player.Scene.Add(booster = new Booster(player.Center - Vector2.UnitY * 2, false));

        yield return null;
        yield return null;
            
        player.StateMachine.State = Player.StDash;
        player.Scene.Remove(booster.outline);
        while (player.StateMachine.State == Player.StDash)
            yield return null;

        running = false;
        if (renderParticles)
            yield return 0.6f;
        else
            yield return null;
            
        player.Scene.Remove(booster);
    }
} 