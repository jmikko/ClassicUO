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
    /// The spellbook: two facing pages of spell icons, turned with the arrows at the foot.
    /// <para>
    /// A book rather than a list, because that is what a spellbook is - and because a flat list of
    /// names told a player nothing at a glance. An icon is recognisable after you have cast it
    /// once; a line of text has to be read every time, and a 1st-level wizard already has 27.
    /// </para>
    /// <para>
    /// Nothing here decides whether a cast is legal. Clicking a spell sends a request and the
    /// server answers with DnDCastResult, which is what produces the message the player sees - so
    /// the book can never show a cast succeeding that the rules refused.
    /// </para>
    /// </summary>
    internal class DnDSpellbookGump : Gump
    {
        private const int SPELL_BUTTON_BASE_ID = 100;
        private const int BUTTON_PREVIOUS = 1;
        private const int BUTTON_NEXT = 2;

        private const int WIDTH = 480;
        private const int HEIGHT = 380;

        private const int PAGE_TOP = 62;
        private const int ROWS = 5;
        private const int COLUMNS = 2;

        /// <summary>Two facing pages, each a grid of the same size.</summary>
        private const int PER_PAGE = ROWS * COLUMNS * 2;

        // UO's magery spell icons: 64 consecutive gumps. Spells are spread across them by school so
        // that a school's spells look like a set, without needing art of our own - art would have to
        // ship with the client and be kept in step with it.
        private const ushort ICON_BASE = 0x08C0;
        private const int ICONS_PER_SCHOOL = 8;

        private readonly List<DnDSpellEntry> _rows = new List<DnDSpellEntry>();
        private readonly List<Control> _pageControls = new List<Control>();

        private Label _slotsLabel;
        private Label _pageLabel;
        private int _page;

        public DnDSpellbookGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            Width = WIDTH;
            Height = HEIGHT;

            Add(new ResizePic(DnDStyle.BackgroundGraphic) { Width = Width, Height = Height });

            Add(
                new Label("Spellbook", true, DnDStyle.HueTitle, Width - 20, 1, FontStyle.BlackBorder)
                {
                    X = DnDStyle.Margin,
                    Y = 10
                });

            Add(new Line(DnDStyle.Margin, 32, Width - (DnDStyle.Margin * 2), 1, DnDStyle.RuleColour));

            _slotsLabel = new Label(string.Empty, true, DnDStyle.HueMuted, Width - 20, 1, FontStyle.BlackBorder)
            {
                X = DnDStyle.Margin,
                Y = 38
            };

            Add(_slotsLabel);

            // The spine. Two columns of icons either side of it is what makes this read as an open
            // book rather than as a window with a line down the middle.
            Add(new Line(WIDTH / 2, PAGE_TOP - 6, 1, HEIGHT - PAGE_TOP - 46, DnDStyle.RuleColour));

            BuildFooter();

            DnDSpellState.Changed += OnSpellStateChanged;
            DnDSpellState.CastResolved += OnCastResolved;

            RequestUpdateContents();
        }

        private void BuildFooter()
        {
            int y = HEIGHT - 34;

            Add(new Line(DnDStyle.Margin, y - 8, Width - (DnDStyle.Margin * 2), 1, DnDStyle.RuleColour));

            // UO's own page-turn arrows, which anyone who has opened a book in this client knows.
            Add(
                new Button(BUTTON_PREVIOUS, 0x08BB, 0x08BC)
                {
                    X = DnDStyle.Margin,
                    Y = y,
                    ButtonAction = ButtonAction.Activate
                });

            Add(
                new Button(BUTTON_NEXT, 0x08BE, 0x08BF)
                {
                    X = Width - DnDStyle.Margin - 22,
                    Y = y,
                    ButtonAction = ButtonAction.Activate
                });

            _pageLabel = new Label(string.Empty, true, DnDStyle.HueMuted, 200, 1, FontStyle.BlackBorder)
            {
                X = (WIDTH / 2) - 50,
                Y = y + 2
            };

            Add(_pageLabel);
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
            foreach (Control control in _pageControls)
            {
                control.Dispose();
            }

            _pageControls.Clear();
            _rows.Clear();
            _rows.AddRange(DnDSpellState.Spells);

            // Cantrips first, then by level - the order a caster thinks about them in. The server
            // sends them in registration order, which is not that.
            _rows.Sort((a, b) => a.Level != b.Level ? a.Level.CompareTo(b.Level) : string.CompareOrdinal(a.Name, b.Name));

            _slotsLabel.Text = DescribeSlots();

            int pages = System.Math.Max(1, (_rows.Count + PER_PAGE - 1) / PER_PAGE);

            if (_page >= pages)
            {
                _page = pages - 1;
            }

            _pageLabel.Text = string.Format("Page {0} of {1}", _page + 1, pages);

            int first = _page * PER_PAGE;

            for (int slot = 0; slot < PER_PAGE; ++slot)
            {
                int index = first + slot;

                if (index >= _rows.Count)
                {
                    break;
                }

                AddSpellEntry(_rows[index], index, slot);
            }
        }

        private void AddSpellEntry(DnDSpellEntry spell, int index, int slot)
        {
            // The first half of the slots fill the left page, the rest the right.
            bool rightPage = slot >= ROWS * COLUMNS;
            int pageSlot = rightPage ? slot - (ROWS * COLUMNS) : slot;

            int column = pageSlot % COLUMNS;
            int row = pageSlot / COLUMNS;

            int pageX = rightPage ? (WIDTH / 2) + 14 : DnDStyle.Margin;
            int x = pageX + (column * 108);
            int y = PAGE_TOP + (row * 52);

            // A spell with no slot left to pay for it is greyed rather than hidden - knowing it
            // exists and is spent is the useful thing.
            bool affordable = spell.IsCantrip || DnDSpellState.SlotsAvailable[spell.Level - 1] > 0;

            ushort icon = GetIcon(spell);

            var button = new Button(SPELL_BUTTON_BASE_ID + index, icon, icon)
            {
                X = x,
                Y = y,
                ButtonAction = ButtonAction.Activate
            };

            Add(button);
            _pageControls.Add(button);

            var name = new Label(
                spell.Name, true, affordable ? DnDStyle.HueBody : DnDStyle.HueMuted, 104, 1, FontStyle.BlackBorder)
            {
                X = x,
                Y = y + 40
            };

            Add(name);
            _pageControls.Add(name);
        }

        /// <summary>
        /// Spread across UO's 64 magery icons by school, so a school's spells look like a set.
        /// Spells within a school cycle through eight icons - which repeats on a long list, but
        /// keeps neighbours on a page distinguishable, which is what the icon is for.
        /// </summary>
        private static ushort GetIcon(DnDSpellEntry spell)
        {
            int school = (int)spell.School;

            return (ushort)(ICON_BASE + (school * ICONS_PER_SCHOOL) + (spell.Id % ICONS_PER_SCHOOL));
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
                builder.AppendFormat(
                    " {0}:{1}/{2}", level, DnDSpellState.SlotsAvailable[level - 1], DnDSpellState.SlotsMax[level - 1]);
            }

            return builder.ToString();
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == BUTTON_PREVIOUS)
            {
                if (_page > 0)
                {
                    --_page;
                    RequestUpdateContents();
                }

                return;
            }

            if (buttonID == BUTTON_NEXT)
            {
                if ((_page + 1) * PER_PAGE < _rows.Count)
                {
                    ++_page;
                    RequestUpdateContents();
                }

                return;
            }

            int index = buttonID - SPELL_BUTTON_BASE_ID;

            if (index < 0 || index >= _rows.Count)
            {
                return;
            }

            BeginTargeting(_rows[index]);
        }

        /// <summary>
        /// Puts the cursor into targeting mode. The player picks any mobile - or a spot on the
        /// ground, for spells placed on a point - and which of those a given spell accepts is the
        /// server's call, which it will say if the choice was wrong.
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
