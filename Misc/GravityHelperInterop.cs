using Microsoft.Xna.Framework;
using MonoMod.ModInterop;
using System;

namespace Celeste.Mod.DawnHelper.Misc;

internal static class GravityHelperInterop
{
    [ModImportName("GravityHelper")]
    internal static class GelperImports
    {
        public static Func<bool> IsPlayerInverted;
        public static Func<Actor, bool> IsActorInverted;
    }
    
    public static bool IsPlayerInverted() => GelperImports.IsPlayerInverted?.Invoke() ?? false;
    public static bool IsActorInverted(Actor actor) => GelperImports.IsActorInverted?.Invoke(actor) ?? false;
}