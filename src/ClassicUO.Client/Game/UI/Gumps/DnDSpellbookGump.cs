// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Data;
using ClassicUO.Game.DnD;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// The D&amp;D spellbook: the spells the character can cast, their remaining slots, and a button
    /// per spell that puts the cursor into targeting mode.
    /// <para>
    /// Nothing here decides whether a cast is legal. Clicking a spell sends a request and the server
    /// answers with DnDCastResult, which is what produces the message the player sees - so the
    /// window can never show a cast succeeding that the rules refused.
    /// </para>
    /// </summary>
    internal class DnDSpellbookGump : Gump
    {
        private const int SPELL_BUTTON_BASE_ID = 100;
        private const int ROW_HEIGHT = 22;
        private const int HEADER_HEIGHT = 52;
        private const int FOOTER_HEIGHT = 14;

        private readonly List<DnDSpellEntry> _rows = new List<DnDSpellEntry>();

        private Label _slotsLabel;
        private ResizePic _background;
        private readonly List<Control> _spellControls = new List<Control>();

        public DnDSpellbookGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            Width = 240;
            Height = HEADER_HEIGHT + FOOTER_HEIGHT;

            _background = new ResizePic(0x0A28) { Width = Width, Height = Height };
            Add(_background);

            Add(new Label("Spellbook", true, 0x0035, Width - 20, 0xFF, FontStyle.BlackBorder) { X = 12, Y = 10 });

            _slotsLabel = new Label(string.Empty, true, 0x0481, Width - 20, 0xFF, FontStyle.BlackBorder)
            {
                X = 12,
                Y = 30
            };

            Add(_slotsLabel);

            DnDSpellState.Changed += OnSpellStateChanged;
            DnDSpellState.CastResolved += OnCastResolved;

            RequestUpdateContents();
        }

        private void OnSpellStateChanged()
        {
            RequestUpdateContents();
        }

        /// <summary>
        /// The server is the only thing that knows why a cast went the way it did, so its answer is
        /// what the player is told - the client never guesses ahead of it.
        /// </summary>
        private void OnCastResolved(int spellId, DnDCastResult result)
        {
            string message = DnDSpellState.Describe(result);

            if (message.Length == 0)
            {
                return;
            }

            // Green for a cast that worked, red for one the rules refused.
            World.MessageManager.HandleMessage
            (
                null,
                message,
                string.Empty,
                result == DnDCastResult.Success ? (ushort)0x0044 : (ushort)0x0021,
                MessageType.Regular,
                3,
                TextType.CLIENT
            );
        }

        protected override void UpdateContents()
        {
            foreach (Control control in _spellControls)
            {
                control.Dispose();
            }

            _spellControls.Clear();
            _rows.Clear();
            _rows.AddRange(DnDSpellState.Spells);

            _slotsLabel.Text = DescribeSlots();

            int y = HEADER_HEIGHT;

            for (int i = 0; i < _rows.Count; ++i)
            {
                DnDSpellEntry spell = _rows[i];

                var button = new NiceButton
                (
                    12,
                    y,
                    Width - 24,
                    ROW_HEIGHT - 2,
                    ButtonAction.Activate,
                    FormatSpell(spell)
                )
                {
                    ButtonParameter = SPELL_BUTTON_BASE_ID + i,
                    IsSelectable = false
                };

                Add(button);
                _spellControls.Add(button);

                y += ROW_HEIGHT;
            }

            Height = y + FOOTER_HEIGHT;
            _background.Height = Height;
            WantUpdateSize = true;
        }

        private static string FormatSpell(DnDSpellEntry spell)
        {
            return spell.IsCantrip
                ? string.Format("{0}  (cantrip)", spell.Name)
                : string.Format("{0}  (lvl {1})", spell.Name, spell.Level);
        }

        private static string DescribeSlots()
        {
            int highest = DnDSpellState.HighestSlotLevel;

            if (highest == 0)
            {
                return "Cantrips only";
            }

            var builder = new StringBuilder("Slots:");

            for (int level = 1; level <= highest; ++level)
            {
                builder.AppendFormat(" {0}:{1}/{2}", level, DnDSpellState.SlotsAvailable[level - 1], DnDSpellState.SlotsMax[level - 1]);
            }

            return builder.ToString();
        }

        public override void OnButtonClick(int buttonID)
        {
            int index = buttonID - SPELL_BUTTON_BASE_ID;

            if (index < 0 || index >= _rows.Count)
            {
                return;
            }

            BeginTargeting(_rows[index]);
        }

        /// <summary>
        /// Puts the cursor into targeting mode. The player picks any mobile, including themselves -
        /// which target types a given spell actually accepts is the server's call, and it will say
        /// so if the choice was wrong.
        /// </summary>
        private void BeginTargeting(DnDSpellEntry spell)
        {
            int spellId = spell.Id;

            World.TargetManager.SetTargeting
            (
                target =>
                {
                    if (target == null)
                    {
                        return;
                    }

                    uint serial = 0;
                    if (target is Entity ent)
                    {
                        serial = ent.Serial;
                    }

                    NetClient.Socket.Send_DnDCastRequest(World, spellId, serial, target.X, target.Y, target.Z);
                },
                0,
                TargetType.Neutral
            );
        }

        public override void Dispose()
        {
            DnDSpellState.Changed -= OnSpellStateChanged;
            DnDSpellState.CastResolved -= OnCastResolved;

            base.Dispose();
        }
    }
}
