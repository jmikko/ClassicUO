// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO.Game.DnD
{
    /// <summary>Where a dying character currently stands, as the server sees it.</summary>
    internal enum DnDDyingPhase : byte
    {
        Alive = 0,
        Dying = 1,
        Stable = 2,
        Dead = 3
    }

    /// <summary>
    /// The running death-save count, from the server's DnDDeathSaves packet (0xBF subcommand 0x45).
    /// <para>
    /// Held separately from <see cref="DnDState"/> because it has a different lifetime: the sheet
    /// is true for as long as the character exists, while this is true only for the half-minute
    /// between falling and either getting up or not. Mixing them would leave the sheet carrying
    /// stale failure counts long after the fight.
    /// </para>
    /// </summary>
    internal static class DnDDeathState
    {
        public static DnDDyingPhase Phase;
        public static int Successes;
        public static int Failures;

        /// <summary>True while there is anything worth showing the player.</summary>
        public static bool IsShowing => Phase != DnDDyingPhase.Alive;

        public static event Action Changed;

        public static void Apply(DnDDyingPhase phase, int successes, int failures)
        {
            Phase = phase;
            Successes = successes;
            Failures = failures;

            Changed?.Invoke();
        }
    }
}
