// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.DnD;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// The always-visible character sheet, refreshed whenever the server sends DnDStatSync
    /// (0xBF/0x41).
    /// <para>
    /// Shows each ability score with the modifier beside it, because the modifier is the number
    /// that actually reaches the dice - a sheet listing only "Strength 16" makes the player do the
    /// arithmetic every time they want to know what they will roll.
    /// </para>
    /// </summary>
    internal class DnDCharacterSheetGump : Gump
    {
        private const int WIDTH = 288;

        /// <summary>The SRD skill count. Fixed, so the rows can be built once and only relabelled.</summary>
        private const int SkillCount = 18;

        private static readonly string[] _abbreviations = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };

        private readonly Label[] _scoreLabels = new Label[6];
        private readonly Label[] _modifierLabels = new Label[6];

        private readonly Label[] _skillNames = new Label[SkillCount];
        private readonly Label[] _skillModifiers = new Label[SkillCount];

        private Label _classLabel;
        private Label _healthLabel;
        private Label _defenceLabel;

        public DnDCharacterSheetGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            Width = WIDTH;
            Height = 412;

            int y = DnDStyle.AddFrame(this, "Character", Width, Height);

            _classLabel = DnDStyle.AddText(this, string.Empty, DnDStyle.Margin, y, Width - 28, DnDStyle.HueHeading);

            y = DnDStyle.AddSection(this, "Abilities", y + 20, Width);

            DnDStyle.AddPanel(this, DnDStyle.Margin - 4, y - 2, Width - (DnDStyle.Margin * 2) + 8, 56);

            // Two rows of three, so the sheet stays compact enough to leave on screen.
            const int columns = 3;
            int cellWidth = (Width - (DnDStyle.Margin * 2)) / columns;

            for (int i = 0; i < 6; ++i)
            {
                int col = i % columns;
                int row = i / columns;

                int x = DnDStyle.Margin + (col * cellWidth);
                int rowY = y + (row * 26);

                DnDStyle.AddText(this, _abbreviations[i], x + 2, rowY, 34, DnDStyle.HueMuted);

                _scoreLabels[i] = DnDStyle.AddText(this, "10", x + 34, rowY, 26, DnDStyle.HueBody);
                _modifierLabels[i] = DnDStyle.AddText(this, "+0", x + 56, rowY, 30, DnDStyle.HueGood);
            }

            y = DnDStyle.AddSection(this, "In Combat", y + 60, Width);

            _healthLabel = DnDStyle.AddText(this, string.Empty, DnDStyle.Margin, y, Width - 28, DnDStyle.HueBody);
            _defenceLabel = DnDStyle.AddText(this, string.Empty, DnDStyle.Margin, y + 18, Width - 28, DnDStyle.HueBody);

            y = DnDStyle.AddSection(this, "Skills", y + 42, Width);

            DnDStyle.AddHint(this, "Proficient in green, expertise in gold.", DnDStyle.Margin, y, Width - 28);

            y += 18;

            // Two columns of nine. Every skill is listed, not only the proficient ones - an
            // unproficient skill still has an ability modifier, and "is this worth trying?" is a
            // question about the whole list.
            const int skillColumns = 2;
            const int skillRowHeight = 16;

            int skillCellWidth = (Width - (DnDStyle.Margin * 2)) / skillColumns;
            int rowsPerColumn = SkillCount / skillColumns;

            for (int i = 0; i < SkillCount; ++i)
            {
                int col = i / rowsPerColumn;
                int row = i % rowsPerColumn;

                int x = DnDStyle.Margin + (col * skillCellWidth);
                int rowY = y + (row * skillRowHeight);

                _skillNames[i] = DnDStyle.AddText(this, string.Empty, x, rowY, skillCellWidth - 34, DnDStyle.HueMuted);
                _skillModifiers[i] = DnDStyle.AddText(this, string.Empty, x + skillCellWidth - 32, rowY, 30, DnDStyle.HueMuted);
            }

            DnDState.Changed += OnDnDStateChanged;
            DnDSkillState.Changed += OnDnDStateChanged;

            RequestUpdateContents();
        }

        private void OnDnDStateChanged()
        {
            RequestUpdateContents();
        }

        protected override void UpdateContents()
        {
            _classLabel.Text = string.Format("Level {0}", DnDState.Level);

            int[] scores =
            {
                DnDState.Str, DnDState.Dex, DnDState.Con,
                DnDState.Int, DnDState.Wis, DnDState.Cha
            };

            for (int i = 0; i < scores.Length; ++i)
            {
                int modifier = (int)System.Math.Floor((scores[i] - 10) / 2.0);

                _scoreLabels[i].Text = scores[i].ToString();
                _modifierLabels[i].Text = modifier >= 0 ? "+" + modifier : modifier.ToString();
                _modifierLabels[i].Hue = modifier >= 0 ? DnDStyle.HueGood : DnDStyle.HueBad;
            }

            _healthLabel.Text = string.Format("Hit Points  {0} / {1}", DnDState.HitsCurrent, DnDState.HitsMax);

            // Hurt is worth noticing without reading the numbers.
            _healthLabel.Hue = DnDState.HitsMax > 0 && DnDState.HitsCurrent * 4 <= DnDState.HitsMax
                ? DnDStyle.HueBad
                : DnDStyle.HueBody;

            _defenceLabel.Text = string.Format(
                "Armour Class {0}     Proficiency +{1}", DnDState.ArmorClass, DnDState.ProficiencyBonus);

            UpdateSkills();
        }

        /// <summary>
        /// Relabels the skill rows. Proficiency is carried by colour rather than a marker column,
        /// so the list stays scannable at a glance: green is trained, gold is expertise.
        /// </summary>
        private void UpdateSkills()
        {
            DnDSkillEntry[] skills = DnDSkillState.Skills;

            for (int i = 0; i < _skillNames.Length; ++i)
            {
                if (i >= skills.Length)
                {
                    // Nothing reported yet - a sheet opened before the first sync, or a character
                    // that predates the skill packet. Blank beats a row of confident zeroes.
                    _skillNames[i].Text = string.Empty;
                    _skillModifiers[i].Text = string.Empty;

                    continue;
                }

                DnDSkillEntry entry = skills[i];

                ushort hue = entry.Expertise
                    ? DnDStyle.HueTitle
                    : entry.Proficient ? DnDStyle.HueGood : DnDStyle.HueMuted;

                _skillNames[i].Text = DnDSkills.GetDisplayName(entry.Skill);
                _skillNames[i].Hue = hue;

                _skillModifiers[i].Text = entry.Modifier >= 0
                    ? "+" + entry.Modifier
                    : entry.Modifier.ToString();

                _skillModifiers[i].Hue = hue;
            }
        }

        public override void Dispose()
        {
            DnDState.Changed -= OnDnDStateChanged;
            DnDSkillState.Changed -= OnDnDStateChanged;

            base.Dispose();
        }
    }
}
