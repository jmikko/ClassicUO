// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO.Game.DnD
{
    /// <summary>What a character adds to one skill, and why.</summary>
    internal struct DnDSkillEntry
    {
        public DnDSkill Skill;
        public int Modifier;
        public bool Proficient;
        public bool Expertise;
    }

    /// <summary>
    /// The eighteen skills as the server last reported them (0xBF subcommand 0x46).
    /// <para>
    /// Every skill is held, not only the proficient ones: an unproficient skill still has an
    /// ability modifier behind it, and the question a player is actually asking - "is it worth me
    /// trying this?" - needs the whole list to answer.
    /// </para>
    /// </summary>
    internal static class DnDSkillState
    {
        public static DnDSkillEntry[] Skills = new DnDSkillEntry[0];

        public static bool HasAny => Skills.Length > 0;

        public static event Action Changed;

        public static void Apply(DnDSkillEntry[] skills)
        {
            Skills = skills ?? new DnDSkillEntry[0];

            Changed?.Invoke();
        }
    }
}
