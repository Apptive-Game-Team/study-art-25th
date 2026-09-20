using System.Collections.Generic;
using UnityEngine;

namespace ArtUnityWorkshop
{
    public static class GameSession
    {
        public static int PlayerHealth = -1;
        public static bool TrophyOwned, RespawnPending, Transitioning;
        public static readonly Dictionary<string, int> Enemies = new Dictionary<string, int>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            PlayerHealth = -1;
            TrophyOwned = RespawnPending = Transitioning = false;
            Enemies.Clear();
        }
        public static int RespawnHealth(int maximum) => Mathf.Max(1, (int)System.Math.Floor(maximum * .1d + .5d));
    }
}
