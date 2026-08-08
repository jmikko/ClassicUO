// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// One place for the D&amp;D windows' shared look, so the setup screen, the sheet, the spellbook
    /// and the level-up prompt read as one product rather than four separately-built panels.
    /// <para>
    /// Everything here is built from ClassicUO's own primitives - the parchment resize-pic, solid
    /// lines and alpha panels. Nothing needs new art, which matters: art would have to ship
    /// alongside the client and be kept in step with it.
    /// </para>
    /// </summary>
    internal static class DnDStyle
    {
        /// <summary>The parchment background every D&amp;D window sits on.</summary>
        public const ushort BackgroundGraphic = 0x0A28;

        // Hues, chosen to carry meaning rather than decoration: a heading is not just larger, and
        // an unaffordable option is not just dimmer.
        public const ushort HueTitle = 0x0481;      // pale gold, for window titles
        public const ushort HueHeading = 0x0035;    // warm amber, for section headings
        public const ushort HueBody = 0x0386;       // dark ink, for ordinary text
        public const ushort HueMuted = 0x0385;      // faded ink, for hints and secondary detail
        public const ushort HueGood = 0x0044;       // green, for success and things you can afford
        public const ushort HueBad = 0x0021;        // red, for refusal and things you cannot
        public const ushort HueSelected = 0x0035;   // amber, for a chosen option

        public const uint RuleColour = 0xFF7A6A4F;      // a drawn line, the colour of old ink
        public const uint PanelColour = 0xFF1A1207;     // inset panels behind grouped content

        public const int Margin = 14;
        public const int RowHeight = 24;
        public const int SectionGap = 10;

        /// <summary>Adds the parchment backing and the window title, returning the y to build from.</summary>
        public static int AddFrame(Gump gump, string title, int width, int height)
        {
            gump.Add(new ResizePic(BackgroundGraphic) { Width = width, Height = height });

            gump.Add(
                new Label(title, true, HueTitle, width - (Margin * 2), 1, FontStyle.BlackBorder)
                {
                    X = Margin,
                    Y = 10
                });

            gump.Add(new Line(Margin, 32, width - (Margin * 2), 1, RuleColour));

            return 40;
        }

        /// <summary>
        /// A section heading with a rule running out to the right of it, which is what stops a tall
        /// window reading as one undifferentiated list.
        /// </summary>
        public static int AddSection(Gump gump, string heading, int y, int width)
        {
            gump.Add(
                new Label(heading, true, HueHeading, width, 1, FontStyle.BlackBorder)
                {
                    X = Margin,
                    Y = y
                });

            int textWidth = (heading.Length * 7) + 6;

            gump.Add(new Line(Margin + textWidth, y + 8, width - textWidth - (Margin * 2), 1, RuleColour));

            return y + 20;
        }

        /// <summary>A darkened panel to group related controls against the parchment.</summary>
        public static void AddPanel(Gump gump, int x, int y, int width, int height)
        {
            gump.Add(new AlphaBlendControl(0.25f) { X = x, Y = y, Width = width, Height = height });
        }

        public static Label AddText(Gump gump, string text, int x, int y, int width, ushort hue)
        {
            var label = new Label(text, true, hue, width, 1, FontStyle.BlackBorder) { X = x, Y = y };

            gump.Add(label);

            return label;
        }

        /// <summary>A hint line - the small print that says what a section expects of you.</summary>
        public static Label AddHint(Gump gump, string text, int x, int y, int width)
        {
            return AddText(gump, text, x, y, width, HueMuted);
        }

        /// <summary>
        /// A selectable option. Grouping is what makes a set behave as a set: NiceButton draws the
        /// selected member differently and clears the others in the same group by itself.
        /// </summary>
        public static NiceButton AddOption(
            Gump gump, int x, int y, int width, string text, int buttonId, int group, bool selected = false)
        {
            var button = new NiceButton(x, y, width, RowHeight - 2, ButtonAction.Activate, text, group)
            {
                ButtonParameter = buttonId
            };

            gump.Add(button);

            if (selected)
            {
                button.IsSelected = true;
            }

            return button;
        }

        /// <summary>A toggle that is not part of an exclusive group - a skill pick, say.</summary>
        public static NiceButton AddToggle(Gump gump, int x, int y, int width, string text, int buttonId)
        {
            var button = new NiceButton(x, y, width, RowHeight - 2, ButtonAction.Activate, text)
            {
                ButtonParameter = buttonId,
                IsSelectable = false
            };

            gump.Add(button);

            return button;
        }

        /// <summary>The confirm control, sitting on its own rule at the foot of the window.</summary>
        public static void AddFooter(Gump gump, int y, int width, int buttonId, string caption)
        {
            gump.Add(new Line(Margin, y, width - (Margin * 2), 1, RuleColour));

            gump.Add(
                new Button(buttonId, 0x0481, 0x0482, 0x0483)
                {
                    X = Margin + 6,
                    Y = y + 10,
                    ButtonAction = ButtonAction.Activate
                });

            AddText(gump, caption, Margin + 34, y + 12, 160, HueHeading);
        }

        /// <summary>Centres a window, which is where a modal prompt belongs.</summary>
        public static void Centre(Gump gump)
        {
            gump.X = (Client.Game.ClientBounds.Width - gump.Width) >> 1;
            gump.Y = (Client.Game.ClientBounds.Height - gump.Height) >> 1;
        }
    }
}
