// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.DnD;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// Phase 1 D&amp;D 5.5e vertical slice: a small always-visible character sheet panel, refreshed
    /// whenever the server sends a DnDStatSync packet (0xBF/0x41).
    /// </summary>
    internal class DnDCharacterSheetGump : Gump
    {
        private Label _statsLabel;
        private Label _combatLabel;

        public DnDCharacterSheetGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            Width = 230;
            Height = 100;

            Add(new ResizePic(0x0A28) { Width = Width, Height = Height });

            Add(new Label("Character Sheet", true, 0x0035, Width - 20, 0xFF, FontStyle.BlackBorder) { X = 12, Y = 10 });

            _statsLabel = new Label(string.Empty, true, 0x0481, Width - 20, 0xFF, FontStyle.BlackBorder) { X = 12, Y = 34 };
            Add(_statsLabel);

            _combatLabel = new Label(string.Empty, true, 0x0481, Width - 20, 0xFF, FontStyle.BlackBorder) { X = 12, Y = 58 };
            Add(_combatLabel);

            DnDState.Changed += OnDnDStateChanged;

            RequestUpdateContents();
        }

        private void OnDnDStateChanged()
        {
            RequestUpdateContents();
        }

        protected override void UpdateContents()
        {
            _statsLabel.Text = string.Format
            (
                "Str {0} Dex {1} Con {2} Int {3} Wis {4} Cha {5}",
                DnDState.Str, DnDState.Dex, DnDState.Con, DnDState.Int, DnDState.Wis, DnDState.Cha
            );

            _combatLabel.Text = string.Format
            (
                "Lvl {0}  Prof +{1}  AC {2}  HP {3}/{4}",
                DnDState.Level, DnDState.ProficiencyBonus, DnDState.ArmorClass, DnDState.HitsCurrent, DnDState.HitsMax
            );
        }

        public override void Dispose()
        {
            DnDState.Changed -= OnDnDStateChanged;

            base.Dispose();
        }
    }
}
