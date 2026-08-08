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
        private const int WIDTH = 250;

        private static readonly string[] _abbreviations = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };

        private readonly Label[] _scoreLabels = new Label[6];
        private readonly Label[] _modifierLabels = new Label[6];

        private Label _classLabel;
        private Label _healthLabel;
        private Label _defenceLabel;

        public DnDCharacterSheetGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            Width = WIDTH;
            Height = 210;

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

            DnDState.Changed += OnDnDStateChanged;

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
        }

        public override void Dispose()
        {
            DnDState.Changed -= OnDnDStateChanged;

            base.Dispose();
        }
    }
}
