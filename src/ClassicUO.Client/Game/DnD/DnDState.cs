// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO.Game.DnD
{
    /// <summary>
    /// Client-side mirror of a PlayerMobile's D&amp;D 5.5e character sheet, populated from the
    /// server's DnDStatSync packet (0xBF subcommand 0x41). Phase 1 vertical slice: a simple static
    /// holder rather than something attached to the World/PlayerMobile object model, since it only
    /// needs to track the local player.
    /// </summary>
    internal static class DnDState
    {
        public static bool Initialized;
        public static int Str, Dex, Con, Int, Wis, Cha;
        public static int ClassId;
        public static int Level;
        public static int ProficiencyBonus;
        public static int ArmorClass;
        public static int HitsCurrent;
        public static int HitsMax;

        public static event Action Changed;

        public static void ApplySync
        (
            int str, int dex, int con, int intl, int wis, int cha,
            int classId, int level, int proficiencyBonus, int armorClass,
            int hitsCurrent, int hitsMax
        )
        {
            Initialized = true;
            Str = str;
            Dex = dex;
            Con = con;
            Int = intl;
            Wis = wis;
            Cha = cha;
            ClassId = classId;
            Level = level;
            ProficiencyBonus = proficiencyBonus;
            ArmorClass = armorClass;
            HitsCurrent = hitsCurrent;
            HitsMax = hitsMax;

            Changed?.Invoke();
        }
    }
}
