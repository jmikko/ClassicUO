// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;

namespace ClassicUO.Game.DnD
{
    internal enum DnDSpellSchool
    {
        Abjuration,
        Conjuration,
        Divination,
        Enchantment,
        Evocation,
        Illusion,
        Necromancy,
        Transmutation
    }

    /// <summary>
    /// Why a cast succeeded or failed. Mirrors the server's CastResult enum - the numbers are the
    /// wire contract, so the order here must match Scripts/Spells/DnD/DnDCasting.cs exactly.
    /// </summary>
    internal enum DnDCastResult
    {
        Success,
        NotACaster,
        NotOnClassList,
        NoSlotAvailable,
        NoTarget,
        OutOfRange,
        WrongTargetType
    }

    internal readonly struct DnDSpellEntry
    {
        public readonly int Id;
        public readonly int Level;
        public readonly DnDSpellSchool School;
        public readonly string Name;

        public DnDSpellEntry(int id, int level, DnDSpellSchool school, string name)
        {
            Id = id;
            Level = level;
            School = school;
            Name = name;
        }

        public bool IsCantrip => Level == 0;
    }

    /// <summary>
    /// Client-side mirror of what the local player can cast, populated from the server's
    /// DnDSpellList packet (0xBF subcommand 0x42).
    /// <para>
    /// This is display state only. The client never decides whether a cast is allowed - it sends a
    /// request and the server answers, so a stale list here can at worst produce a refusal, never
    /// an illegal cast.
    /// </para>
    /// </summary>
    internal static class DnDSpellState
    {
        public const int MaxSpellLevel = 9;

        public static readonly List<DnDSpellEntry> Spells = new List<DnDSpellEntry>();

        /// <summary>Index 0 is 1st-level slots; cantrips cost nothing and are not tracked.</summary>
        public static readonly int[] SlotsAvailable = new int[MaxSpellLevel];
        public static readonly int[] SlotsMax = new int[MaxSpellLevel];

        public static bool HasAnySpells => Spells.Count > 0;

        public static event Action Changed;

        /// <summary>Raised when the server reports the outcome of a cast we asked for.</summary>
        public static event Action<int, DnDCastResult> CastResolved;

        public static void ApplySpellList(List<DnDSpellEntry> spells, int[] slotsAvailable, int[] slotsMax)
        {
            Spells.Clear();
            Spells.AddRange(spells);

            for (int i = 0; i < MaxSpellLevel; ++i)
            {
                SlotsAvailable[i] = i < slotsAvailable.Length ? slotsAvailable[i] : 0;
                SlotsMax[i] = i < slotsMax.Length ? slotsMax[i] : 0;
            }

            Changed?.Invoke();
        }

        public static void ApplyCastResult(int spellId, DnDCastResult result)
        {
            CastResolved?.Invoke(spellId, result);
        }

        /// <summary>The highest spell level with any slots at all, or 0 for a cantrip-only caster.</summary>
        public static int HighestSlotLevel
        {
            get
            {
                for (int level = MaxSpellLevel; level >= 1; --level)
                {
                    if (SlotsMax[level - 1] > 0)
                    {
                        return level;
                    }
                }

                return 0;
            }
        }

        public static string Describe(DnDCastResult result)
        {
            switch (result)
            {
                case DnDCastResult.Success: return "You cast the spell.";
                case DnDCastResult.NotACaster: return "You cannot cast spells.";
                case DnDCastResult.NotOnClassList: return "That spell is not on your list.";
                case DnDCastResult.NoSlotAvailable: return "No spell slot left.";
                case DnDCastResult.NoTarget: return "You need a living target.";
                case DnDCastResult.OutOfRange: return "That is too far away.";
                case DnDCastResult.WrongTargetType: return "You cannot target yourself with that.";
            }

            return string.Empty;
        }
    }
}
