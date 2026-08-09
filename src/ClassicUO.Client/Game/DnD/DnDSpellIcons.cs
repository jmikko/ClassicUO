// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.DnD
{
    /// <summary>
    /// Picks the picture for a spell.
    /// <para>
    /// Three sources, in order. A PNG in <c>Data/SpellIcons</c> named after the spell wins outright,
    /// which is how bespoke art gets in without touching any table here. Failing that, a hand-picked
    /// gump for the spells where one particular piece of Ultima art is obviously right. Failing that,
    /// a deterministic pick from the spell's own school pool.
    /// </para>
    /// <para>
    /// The school pool is the part worth explaining. This used to fall back to
    /// <c>0x08C0 + (id - 1) % 64</c> - the 64 Magery icons, indexed by an arbitrary modulo - so
    /// Wish and Cure Wounds could share a picture for no reason at all, and 230 spells were drawn
    /// with 52 images. Ultima ships far more than 64 spell icons; Necromancy, Chivalry, Ninjitsu,
    /// Bushido, Spellweaving, Mysticism and Mastery add another 112, and every id below is taken
    /// from ClassicUO's own spell tables rather than guessed. Splitting them into pools by school
    /// means the modulo still decides *which* icon, but only ever from a thematically related set:
    /// an Abjuration spell gets something protective, a Necromancy spell gets something grim.
    /// </para>
    /// </summary>
    internal static class DnDSpellIcons
    {
        /// <summary>Where bespoke art goes. Relative to the client executable.</summary>
        private const string IconDirectory = "Data/SpellIcons";

        /// <summary>
        /// A gump that certainly exists in every client - the first Magery icon. Used when a mapped
        /// gump turns out to be missing from this installation's art files, so a wrong id shows the
        /// wrong picture rather than an invisible button.
        /// </summary>
        private const ushort FallbackIcon = 0x08C0;

        #region Verified icon pools

        // Every id below appears in ClassicUO's own spell definitions (Game/Data/Spells*.cs), so
        // they are known to be real art rather than plausible-looking numbers.
        //
        //   Magery        0x08C0 - 0x08FF   64
        //   Necromancy    0x5000 - 0x5010   17
        //   Chivalry      0x5100 - 0x5109   10
        //   Ninjitsu      0x5320 - 0x5327    8
        //   Bushido       0x5420 - 0x5425    6
        //   Spellweaving  0x59D8 - 0x59E7   16
        //   Mysticism     0x5DC0 - 0x5DCF   16
        //   Mastery       0x9B8B - 0x9BB1   39

        private static readonly ushort[] EvocationPool = Range(0x08D0, 16)      // Magery attack half
            .Concat(Range(0x9B8B, 20));                                          // Mastery

        private static readonly ushort[] AbjurationPool = Range(0x5100, 10)     // Chivalry: protection
            .Concat(Range(0x5DC0, 8))                                            // Mysticism: wards
            .Concat(Range(0x08C0, 8));                                           // Magery: reactive

        private static readonly ushort[] ConjurationPool = Range(0x59D8, 16)    // Spellweaving: summons
            .Concat(Range(0x08E0, 8));

        private static readonly ushort[] NecromancyPool = Range(0x5000, 17);    // Necromancy outright

        private static readonly ushort[] TransmutationPool = Range(0x5DC8, 8)   // Mysticism: change
            .Concat(Range(0x5420, 6))                                            // Bushido
            .Concat(Range(0x08F0, 10));

        private static readonly ushort[] EnchantmentPool = Range(0x9BA0, 18)    // Mastery: mind
            .Concat(Range(0x08C8, 6));

        private static readonly ushort[] IllusionPool = Range(0x5320, 8)        // Ninjitsu: deception
            .Concat(Range(0x08E8, 6));

        private static readonly ushort[] DivinationPool = Range(0x08F8, 8)      // Magery: reveal
            .Concat(Range(0x5DCC, 4));

        private static ushort[] Range(int start, int count)
        {
            var ids = new ushort[count];

            for (int i = 0; i < count; ++i)
            {
                ids[i] = (ushort)(start + i);
            }

            return ids;
        }

        private static ushort[] Concat(this ushort[] first, ushort[] second)
        {
            var joined = new ushort[first.Length + second.Length];

            Array.Copy(first, joined, first.Length);
            Array.Copy(second, 0, joined, first.Length, second.Length);

            return joined;
        }

        private static ushort[] PoolFor(DnDSpellSchool school)
        {
            switch (school)
            {
                case DnDSpellSchool.Evocation: return EvocationPool;
                case DnDSpellSchool.Abjuration: return AbjurationPool;
                case DnDSpellSchool.Conjuration: return ConjurationPool;
                case DnDSpellSchool.Necromancy: return NecromancyPool;
                case DnDSpellSchool.Transmutation: return TransmutationPool;
                case DnDSpellSchool.Enchantment: return EnchantmentPool;
                case DnDSpellSchool.Illusion: return IllusionPool;
                case DnDSpellSchool.Divination: return DivinationPool;
            }

            return EvocationPool;
        }

        #endregion

        #region Hand-picked icons

        /// <summary>
        /// The spells where one particular piece of Ultima art is clearly right - a fireball should
        /// look like a fireball. Everything not here is drawn from its school's pool, which is a
        /// perfectly good outcome rather than a gap to be filled.
        /// </summary>
        private static readonly Dictionary<string, ushort> Named = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
        {
            // Fire
            { "Fire Bolt", 0x08D5 },
            { "Burning Hands", 0x08DB },
            { "Fireball", 0x08DE },
            { "Wall of Fire", 0x08E0 },
            { "Flame Strike", 0x08F1 },
            { "Delayed Blast Fireball", 0x08EA },
            { "Fire Shield", 0x08DB },
            { "Produce Flame", 0x08D1 },
            { "Meteor Swarm", 0x08F1 },

            // Cold and lightning
            { "Ray of Frost", 0x08E4 },
            { "Cone of Cold", 0x08E4 },
            { "Ice Storm", 0x08E4 },
            { "Shocking Grasp", 0x08DD },
            { "Lightning Bolt", 0x08DD },
            { "Chain Lightning", 0x08F0 },
            { "Call Lightning", 0x08DD },

            // Force and raw magic
            { "Magic Missile", 0x08D4 },
            { "Eldritch Blast", 0x08E9 },
            { "Disintegrate", 0x08F2 },
            { "Wall of Force", 0x08D7 },
            { "Telekinesis", 0x08D4 },
            { "Mage Hand", 0x08D4 },
            { "Mage Armor", 0x08C6 },
            { "Shield", 0x08C6 },
            { "Counterspell", 0x08C0 },
            { "Dispel Magic", 0x08C0 },

            // Healing and life
            { "Cure Wounds", 0x08DC },
            { "Healing Word", 0x08D8 },
            { "Mass Cure Wounds", 0x08D8 },
            { "Heal", 0x08DC },
            { "Mass Heal", 0x08DC },
            { "Lesser Restoration", 0x08DC },
            { "Greater Restoration", 0x08DC },
            { "Spare the Dying", 0x08CA },
            { "Revivify", 0x08FA },
            { "Raise Dead", 0x08FA },
            { "Resurrection", 0x08FA },
            { "True Resurrection", 0x08FA },
            { "Beacon of Hope", 0x5104 },

            // Necromancy
            { "Chill Touch", 0x5000 },
            { "Toll the Dead", 0x5001 },
            { "Inflict Wounds", 0x5002 },
            { "Animate Dead", 0x5003 },
            { "Vampiric Touch", 0x5004 },
            { "Circle of Death", 0x5005 },
            { "Finger of Death", 0x5006 },
            { "Harm", 0x5007 },
            { "Eyebite", 0x5008 },
            { "Contagion", 0x5009 },
            { "Blight", 0x500A },
            { "Ray of Enfeeblement", 0x500B },
            { "False Life", 0x500C },
            { "Speak with Dead", 0x500D },
            { "Create Undead", 0x500E },
            { "Clone", 0x500F },
            { "Power Word Kill", 0x5010 },

            // Poison and acid
            { "Acid Splash", 0x08C8 },
            { "Poison Spray", 0x08D3 },
            { "Ray of Sickness", 0x08D3 },
            { "Cloudkill", 0x08D3 },
            { "Stinking Cloud", 0x08D3 },

            // Radiant and divine
            { "Sacred Flame", 0x5100 },
            { "Guiding Bolt", 0x5101 },
            { "Divine Favor", 0x5102 },
            { "Bless", 0x5103 },
            { "Holy Aura", 0x5105 },
            { "Divine Word", 0x5106 },
            { "Sunbeam", 0x08E3 },
            { "Sunburst", 0x08E3 },
            { "Daylight", 0x08C5 },
            { "Light", 0x08C5 },

            // Mind
            { "Charm Person", 0x9BA0 },
            { "Command", 0x9BA1 },
            { "Hold Person", 0x08E5 },
            { "Hold Monster", 0x08E5 },
            { "Suggestion", 0x9BA2 },
            { "Mass Suggestion", 0x9BA2 },
            { "Dominate Person", 0x08F4 },
            { "Dominate Monster", 0x08F4 },
            { "Confusion", 0x9BA3 },
            { "Fear", 0x9BA4 },
            { "Sleep", 0x9BA5 },
            { "Calm Emotions", 0x9BA6 },
            { "Vicious Mockery", 0x9BA7 },
            { "Feeblemind", 0x9BA8 },
            { "Modify Memory", 0x9BA9 },

            // Illusion and stealth
            { "Minor Illusion", 0x5320 },
            { "Invisibility", 0x5321 },
            { "Greater Invisibility", 0x5321 },
            { "Mirror Image", 0x5322 },
            { "Blur", 0x5323 },
            { "Disguise Self", 0x5324 },
            { "Silent Image", 0x5325 },
            { "Major Image", 0x5325 },
            { "Pass without Trace", 0x5326 },
            { "Phantasmal Force", 0x5327 },

            // Divination
            { "Detect Magic", 0x08F8 },
            { "Identify", 0x08F9 },
            { "See Invisibility", 0x08FB },
            { "True Seeing", 0x08EF },
            { "Scrying", 0x08EF },
            { "Clairvoyance", 0x08FC },
            { "Augury", 0x08FD },
            { "Divination", 0x08FD },
            { "Foresight", 0x08FE },
            { "Guidance", 0x08C9 },
            { "Locate Object", 0x08FF },

            // Conjuration and summoning
            { "Find Familiar", 0x59D8 },
            { "Conjure Animals", 0x59D9 },
            { "Conjure Elemental", 0x59DA },
            { "Conjure Woodland Beings", 0x59DB },
            { "Summon Lesser Demons", 0x59DC },
            { "Planar Ally", 0x59DD },
            { "Gate", 0x59DE },
            { "Plane Shift", 0x59DF },
            { "Misty Step", 0x59E0 },
            { "Dimension Door", 0x59E1 },
            { "Teleport", 0x59E2 },
            { "Web", 0x59E3 },
            { "Entangle", 0x59E4 },
            { "Grease", 0x59E5 },
            { "Fog Cloud", 0x59E6 },
            { "Darkness", 0x59E7 },

            // Transmutation and movement
            { "Fly", 0x5DC8 },
            { "Levitate", 0x5DC9 },
            { "Spider Climb", 0x5DCA },
            { "Water Breathing", 0x5DCB },
            { "Haste", 0x5420 },
            { "Slow", 0x5421 },
            { "Longstrider", 0x5422 },
            { "Freedom of Movement", 0x5423 },
            { "Enlarge/Reduce", 0x5424 },
            { "Polymorph", 0x5425 },
            { "Enhance Ability", 0x08F3 },
            { "Barkskin", 0x08F5 },
            { "Stoneskin", 0x08F5 },
            { "Flesh to Stone", 0x08E5 },
            { "Darkvision", 0x08C5 },
            { "Knock", 0x08CF },
            { "Magic Weapon", 0x08DF },
            { "True Strike", 0x08DF },
            { "Blade Ward", 0x08C6 },
            { "Thorn Whip", 0x08CD },
            { "Shillelagh", 0x08CD },
            { "Mending", 0x08C3 },
            { "Prestidigitation", 0x08C2 },
            { "Druidcraft", 0x08C1 },
            { "Dancing Lights", 0x08EB },
            { "Message", 0x08EC },
            { "Resistance", 0x08CE },
            { "Blade Barrier", 0x08E0 },
            { "Globe of Invulnerability", 0x08E1 },
            { "Animate Objects", 0x08E7 },
            { "Antimagic Field", 0x08E1 },
            { "Time Stop", 0x08F6 },
            { "Wish", 0x08F7 }
        };

        #endregion

        #region Bespoke PNG art

        private static readonly Dictionary<string, Texture2D> m_Loaded =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        private static bool m_Scanned;

        /// <summary>
        /// Bespoke art for a spell, or null if there is none.
        /// <para>
        /// Files are matched by spell name, so <c>Fireball.png</c> replaces Fireball's icon and
        /// nothing else has to change. The directory is scanned once and every file loaded up front
        /// rather than on demand, because a spellbook page opens all at once and a disk read per
        /// icon would show as a stutter.
        /// </para>
        /// </summary>
        public static Texture2D GetCustomIcon(string spellName)
        {
            if (!m_Scanned)
            {
                Scan();

                // Run once, here rather than at startup, because the art files have certainly
                // finished loading by the time a spellbook is being drawn.
                Audit();
            }

            Texture2D texture;

            return m_Loaded.TryGetValue(ToFileName(spellName), out texture) ? texture : null;
        }

        /// <summary>
        /// The filename a spell's art must use.
        /// <para>
        /// Two SRD spells - Blindness/Deafness and Enlarge/Reduce - have a slash in their name, and
        /// no file on Windows can. Matching on the raw name meant those two could never have art
        /// and nothing would ever say why. The slash becomes a hyphen on both sides.
        /// </para>
        /// </summary>
        public static string ToFileName(string spellName)
        {
            if (string.IsNullOrEmpty(spellName))
            {
                return string.Empty;
            }

            return spellName.Replace('/', '-').Replace('\\', '-').Replace(':', '-');
        }

        private static void Scan()
        {
            m_Scanned = true;

            string directory = Path.Combine(AppContext.BaseDirectory, IconDirectory);

            if (!Directory.Exists(directory))
            {
                // Named, not passed over in silence. Having no bespoke art is a perfectly normal
                // state, but "the folder is missing" and "the folder is somewhere else than you
                // think" look identical from the outside - and the server tree has a Data folder
                // too, which is exactly where the first icon anyone added ended up.
                Log.Info($"No bespoke spell icons: {directory} does not exist.");
                return;
            }

            int loaded = 0;

            foreach (string file in Directory.GetFiles(directory, "*.png"))
            {
                try
                {
                    using (var stream = File.OpenRead(file))
                    {
                        Texture2D texture = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);

                        if (texture != null)
                        {
                            m_Loaded[Path.GetFileNameWithoutExtension(file)] = texture;
                            ++loaded;
                        }
                    }
                }
                catch (Exception e)
                {
                    // One malformed file should not cost every other icon.
                    Log.Warn($"Spell icon '{Path.GetFileName(file)}' could not be loaded: {e.Message}");
                }
            }

            // Reported either way, and with the full path. A folder that exists but is empty is the
            // other half of the same confusion.
            if (loaded > 0)
            {
                Log.Info($"Loaded {loaded} bespoke spell icon(s) from {directory}.");
            }
            else
            {
                Log.Info($"No bespoke spell icons found in {directory}.");
            }
        }

        /// <summary>Drops the cache so new art appears without a client restart.</summary>
        public static void Reload()
        {
            foreach (Texture2D texture in m_Loaded.Values)
            {
                texture?.Dispose();
            }

            m_Loaded.Clear();
            m_Scanned = false;
        }

        #endregion

        /// <summary>
        /// The gump for a spell: hand-picked if there is one, otherwise from the school pool.
        /// <para>
        /// The result is checked against the art files actually installed. A mapped id that this
        /// client has no art for would otherwise draw an invisible button - the failure would look
        /// like a missing spell rather than a missing picture.
        /// </para>
        /// </summary>
        public static ushort GetGumpIcon(DnDSpellEntry spell)
        {
            ushort icon;

            if (!Named.TryGetValue(spell.Name ?? string.Empty, out icon))
            {
                ushort[] pool = PoolFor(spell.School);

                // Deterministic, so a spell keeps the same icon between sessions, and confined to
                // its own school's pool rather than picked from all 176.
                icon = pool[Math.Abs(spell.Id) % pool.Length];
            }

            return Exists(icon) ? icon : FallbackIcon;
        }

        private static bool Exists(ushort graphic)
        {
            ref readonly var info = ref Client.Game.UO.Gumps.GetGump(graphic);

            return info.Texture != null;
        }

        /// <summary>
        /// Reports which mapped icons this installation has no art for.
        /// <para>
        /// The tables above are taken from ClassicUO's own spell definitions, so they should all
        /// resolve - but "should" is how the rest of this project keeps finding silent failures,
        /// and an icon that quietly falls back is exactly the kind of thing nobody notices.
        /// </para>
        /// </summary>
        public static void Audit()
        {
            var missing = new List<string>();

            foreach (var pair in Named)
            {
                if (!Exists(pair.Value))
                {
                    missing.Add($"{pair.Key} (0x{pair.Value:X4})");
                }
            }

            int poolMisses = 0;

            foreach (DnDSpellSchool school in Enum.GetValues(typeof(DnDSpellSchool)))
            {
                foreach (ushort graphic in PoolFor(school))
                {
                    if (!Exists(graphic))
                    {
                        ++poolMisses;
                    }
                }
            }

            if (missing.Count == 0 && poolMisses == 0)
            {
                Log.Info($"Spell icons: {Named.Count} named and every school pool resolved.");
                return;
            }

            Log.Warn($"Spell icons: {missing.Count} named icon(s) and {poolMisses} pool entr(ies) have no art.");

            foreach (string name in missing)
            {
                Log.Warn($"  missing: {name}");
            }
        }
    }
}
