// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;

namespace ClassicUO.Game.DnD
{
    /// <summary>
    /// The 18 SRD skills. The numbering is the wire contract - the setup packet sends these as
    /// integers - so the order must match Server/DnDSkill.cs exactly.
    /// </summary>
    internal enum DnDSkill
    {
        Acrobatics,
        AnimalHandling,
        Arcana,
        Athletics,
        Deception,
        History,
        Insight,
        Intimidation,
        Investigation,
        Medicine,
        Nature,
        Perception,
        Performance,
        Persuasion,
        Religion,
        SleightOfHand,
        Stealth,
        Survival
    }

    /// <summary>
    /// Which skills each class may take, and how many.
    /// <para>
    /// This duplicates the class definitions in Scripts/Engines/Classes/*Class.cs, because the
    /// setup screen has to show the choices before the character exists and so before the server
    /// has anything to tell it. The server re-validates everything sent back and quietly discards
    /// any skill the class does not actually offer, so a copy drifting out of date costs a player
    /// a bad menu rather than an illegal character.
    /// </para>
    /// </summary>
    internal static class DnDSkills
    {
        private static readonly Dictionary<string, (int Count, DnDSkill[] Choices)> _byClass =
            new Dictionary<string, (int, DnDSkill[])>
            {
                ["Barbarian"] = (2, new[]
                {
                    DnDSkill.AnimalHandling, DnDSkill.Athletics, DnDSkill.Intimidation,
                    DnDSkill.Nature, DnDSkill.Perception, DnDSkill.Survival
                }),
                ["Bard"] = (3, new[]
                {
                    DnDSkill.Acrobatics, DnDSkill.Deception, DnDSkill.History, DnDSkill.Insight,
                    DnDSkill.Investigation, DnDSkill.Performance, DnDSkill.Persuasion,
                    DnDSkill.SleightOfHand, DnDSkill.Stealth
                }),
                ["Cleric"] = (2, new[]
                {
                    DnDSkill.History, DnDSkill.Insight, DnDSkill.Medicine,
                    DnDSkill.Persuasion, DnDSkill.Religion
                }),
                ["Druid"] = (2, new[]
                {
                    DnDSkill.Arcana, DnDSkill.AnimalHandling, DnDSkill.Insight, DnDSkill.Medicine,
                    DnDSkill.Nature, DnDSkill.Perception, DnDSkill.Religion, DnDSkill.Survival
                }),
                ["Fighter"] = (2, new[]
                {
                    DnDSkill.Acrobatics, DnDSkill.AnimalHandling, DnDSkill.Athletics, DnDSkill.History,
                    DnDSkill.Insight, DnDSkill.Intimidation, DnDSkill.Perception, DnDSkill.Survival
                }),
                ["Monk"] = (2, new[]
                {
                    DnDSkill.Acrobatics, DnDSkill.Athletics, DnDSkill.History,
                    DnDSkill.Insight, DnDSkill.Religion, DnDSkill.Stealth
                }),
                ["Paladin"] = (2, new[]
                {
                    DnDSkill.Athletics, DnDSkill.Insight, DnDSkill.Intimidation,
                    DnDSkill.Medicine, DnDSkill.Persuasion, DnDSkill.Religion
                }),
                ["Ranger"] = (3, new[]
                {
                    DnDSkill.AnimalHandling, DnDSkill.Athletics, DnDSkill.Insight, DnDSkill.Investigation,
                    DnDSkill.Nature, DnDSkill.Perception, DnDSkill.Stealth, DnDSkill.Survival
                }),
                ["Rogue"] = (4, new[]
                {
                    DnDSkill.Acrobatics, DnDSkill.Athletics, DnDSkill.Deception, DnDSkill.Insight,
                    DnDSkill.Intimidation, DnDSkill.Investigation, DnDSkill.Perception,
                    DnDSkill.Performance, DnDSkill.Persuasion, DnDSkill.SleightOfHand, DnDSkill.Stealth
                }),
                ["Sorcerer"] = (2, new[]
                {
                    DnDSkill.Arcana, DnDSkill.Deception, DnDSkill.Insight,
                    DnDSkill.Intimidation, DnDSkill.Persuasion, DnDSkill.Religion
                }),
                ["Warlock"] = (2, new[]
                {
                    DnDSkill.Arcana, DnDSkill.Deception, DnDSkill.History, DnDSkill.Intimidation,
                    DnDSkill.Investigation, DnDSkill.Nature, DnDSkill.Religion
                }),
                ["Wizard"] = (2, new[]
                {
                    DnDSkill.Arcana, DnDSkill.History, DnDSkill.Insight,
                    DnDSkill.Investigation, DnDSkill.Medicine, DnDSkill.Religion
                })
            };

        public static DnDSkill[] GetChoices(string className)
        {
            return _byClass.TryGetValue(className, out var entry) ? entry.Choices : new DnDSkill[0];
        }

        public static int GetChoiceCount(string className)
        {
            return _byClass.TryGetValue(className, out var entry) ? entry.Count : 2;
        }

        /// <summary>Two skills are spelled as two words; the rest are their enum name.</summary>
        public static string GetDisplayName(DnDSkill skill)
        {
            switch (skill)
            {
                case DnDSkill.AnimalHandling: return "Animal Handling";
                case DnDSkill.SleightOfHand: return "Sleight of Hand";
            }

            return skill.ToString();
        }
    }
}
