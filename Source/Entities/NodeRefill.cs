using System;
using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;

namespace Celeste.Mod.DawnHelper.Entities;

[CustomEntity("DawnHelper/nodeRefill")]
public class NodeRefill : Refill
{
    private Vector2[] nodes;
    private int index;
    private int outlineIndex;
    private float respawnTime;
    private bool restart;
    private bool drawPath;
    private bool drawOutlines;
    private Vector2 startPosition;
    private static Color COLOR_TWODASH;
    private static Color COLOR_ONEDASH;
    private Color LineColor => twoDashes ? COLOR_TWODASH : COLOR_ONEDASH;

    public NodeRefill(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        restart = data.Bool("restart");
        COLOR_ONEDASH = Calc.HexToColorWithAlpha(data.Attr("OneDashLineColor"));
        COLOR_TWODASH = Calc.HexToColorWithAlpha(data.Attr("TwoDashLineColor"));
        respawnTime = data.Float("respawnTime");
        drawPath = data.Bool("drawPath");
        drawOutlines = data.Bool("drawOutlines");
        nodes = data.NodesOffset(offset);
        Get<PlayerCollider>().OnCollide = OnPlayer;
        startPosition = data.Position + offset;
    }

    [MonoModLinkTo("Monocle.Entity", "System.Void Update()")]
    public void base_Update()
    {
        base.Update();
    }

    public override void Update()
    {
        base_Update();
        if (respawnTimer > 0f)
        {
            respawnTimer -= Engine.DeltaTime;
            if (respawnTimer <= 0f)
            {
                foundyouRespawn();
            }
        }
        else if (base.Scene.OnInterval(0.1f))
        {
            if (index == 0)
                level.ParticlesFG.Emit(p_glow, 1, Position, Vector2.One * 5f);
            else
                level.ParticlesFG.Emit(p_glow, 1, nodes[index - 1], Vector2.One * 5f);
        }
        UpdateY();
        FixY();
        light.Alpha = Calc.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
        bloom.Alpha = light.Alpha * 0.8f;
        if (base.Scene.OnInterval(2f) && sprite.Visible)
        {
            flash.Play("flash", restart: true);
            flash.Visible = true;
        }
    }

    private void foundyouRespawn()
    {
        if (!Collidable)
        {
            Collidable = true;
            sprite.Visible = true;
            outline.Visible = false;
            base.Depth = -100;
            wiggler.Start();
            Audio.Play(twoDashes ? "event:/new_content/game/10_farewell/pinkdiamond_return" : "event:/game/general/diamond_return", Position);
            if (index == 0)
                level.ParticlesFG.Emit(p_regen, 16, Position, Vector2.One * 2f);
            else
                level.ParticlesFG.Emit(p_regen, 16, nodes[index - 1], Vector2.One * 2f);
        }
    }

    public void FixY()
    {
        sprite.Position = Collider.Position + Vector2.One * 8;
        flash.Position = Collider.Position + Vector2.One * 8;
        bloom.Position = Collider.Position + Vector2.One * 8;
        light.Position = Collider.Position + Vector2.One * 8;
    }

    public override void Render()
    {
        base.Render();
        if (drawOutlines)
        {
            outline.Texture.DrawCentered(startPosition);
            for (outlineIndex = 0; outlineIndex < nodes.Length; outlineIndex++)
                outline.Texture.DrawCentered(nodes[outlineIndex]);
        }

        if (drawPath)
        {
            Draw.Line(startPosition, nodes[0], LineColor);
            for (outlineIndex = 0; outlineIndex < nodes.Length - 1; outlineIndex++)
                Draw.Line(nodes[outlineIndex], nodes[outlineIndex + 1], LineColor);
        }
    }

    private IEnumerator CustomRefillRoutine(Player player)
    {
        Celeste.Freeze(0.05f);
        yield return null;
        level.Shake();
        sprite.Visible = (flash.Visible = false);
        Depth = 8999;
        yield return 0.05f;
        float num = player.Speed.Angle();
        if (index == 0)
        {
            level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, num - MathF.PI / 2f);
            level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, num + MathF.PI / 2f);
            SlashFx.Burst(Position, num);
        }
        else
        {
            level.ParticlesFG.Emit(p_shatter, 5, nodes[index - 1], Vector2.One * 4f, num - MathF.PI / 2f);
            level.ParticlesFG.Emit(p_shatter, 5, nodes[index - 1], Vector2.One * 4f, num + MathF.PI / 2f);
            SlashFx.Burst(nodes[index - 1], num);
        }
    }

    private new void OnPlayer(Player player)
    {
        if (player.UseRefill(twoDashes))
        {
            Audio.Play(
                twoDashes
                    ? "event:/new_content/game/10_farewell/pinkdiamond_touch"
                    : "event:/game/general/diamond_touch", this.Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            this.Collidable = false;
            base.Add(new Monocle.Coroutine(CustomRefillRoutine(player), true));
            this.respawnTimer = respawnTime;
            if (index < nodes.Length)
            {
                Collider.Position = nodes[index] - Position - Vector2.One * 8;
                ++index;
                level.ParticlesFG.Emit(p_glow, 1, nodes[index - 1], Vector2.One * 5f);
            }
            else if (restart)
            {
                Collider.CenterOrigin();

                index = 0;
            }
            else
                RemoveSelf();
        }
    }
}
