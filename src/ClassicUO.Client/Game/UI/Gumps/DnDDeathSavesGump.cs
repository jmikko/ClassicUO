// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.DnD;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// Three successes and three failures, drawn as pips, for as long as the character is down.
    /// <para>
    /// The whole tension of the death-save rule is watching the count climb toward three, and the
    /// player cannot watch a number that arrives as a system message and is scrolled away by the
    /// next line of combat spam. So it gets a window of its own, and it appears by itself the
    /// moment the character falls - a display you have to go and open is no use when you are
    /// unconscious and cannot open anything.
    /// </para>
    /// </summary>
    internal class DnDDeathSavesGump : Gump
    {
        private const int WIDTH = 210;
        private const int HEIGHT = 132;

        private const int PipSize = 14;
        private const int PipGap = 6;
        private const int PipColumn = 108;

        // The sockets are drawn once and never change; only the fills are toggled.
        private const uint SocketColour = 0xFF241A0E;

        private readonly Line[] _successPips = new Line[3];
        private readonly Line[] _failurePips = new Line[3];

        private readonly Label _statusLabel;

        public DnDDeathSavesGump(World world) : base(world, 0, 0)
        {
            CanMove = true;

            // Not closeable: an unconscious player who dismissed this by accident has no way to get
            // it back, and no way to do anything else either. It leaves when the server says so.
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;

            Width = WIDTH;
            Height = HEIGHT;

            int y = DnDStyle.AddFrame(this, "Death Saves", Width, Height);

            _statusLabel = DnDStyle.AddText(this, string.Empty, DnDStyle.Margin, y, Width - 28, DnDStyle.HueBad);

            y += 24;

            DnDStyle.AddText(this, "Successes", DnDStyle.Margin, y, 90, DnDStyle.HueMuted);
            BuildPips(_successPips, y - 1, DnDStyle.HueGood);

            y += 26;

            DnDStyle.AddText(this, "Failures", DnDStyle.Margin, y, 90, DnDStyle.HueMuted);
            BuildPips(_failurePips, y - 1, DnDStyle.HueBad);

            DnDDeathState.Changed += OnDeathStateChanged;

            RequestUpdateContents();
        }

        /// <summary>
        /// A row of three sockets with a fill sitting in each. The fill's colour is baked into its
        /// texture at construction, so a pip is lit by showing its fill rather than recolouring it.
        /// </summary>
        private void BuildPips(Line[] pips, int y, ushort hue)
        {
            uint colour = HueToColour(hue);

            for (int i = 0; i < pips.Length; ++i)
            {
                int x = PipColumn + (i * (PipSize + PipGap));

                Add(new Line(x, y, PipSize, PipSize, SocketColour));

                pips[i] = new Line(x + 2, y + 2, PipSize - 4, PipSize - 4, colour);

                Add(pips[i]);
            }
        }

        /// <summary>
        /// The style hues are font hues, which say nothing about what a solid rectangle should be
        /// filled with. Rather than pretend to look them up, the two pip colours are stated here.
        /// </summary>
        private static uint HueToColour(ushort hue)
        {
            return hue == DnDStyle.HueGood ? 0xFF3FBF4Fu : 0xFF3F3FCFu;
        }

        private void OnDeathStateChanged()
        {
            RequestUpdateContents();
        }

        protected override void UpdateContents()
        {
            switch (DnDDeathState.Phase)
            {
                case DnDDyingPhase.Stable:
                    _statusLabel.Text = "Stable, but still unconscious.";
                    _statusLabel.Hue = DnDStyle.HueGood;
                    break;

                case DnDDyingPhase.Dead:
                    _statusLabel.Text = "You have died.";
                    _statusLabel.Hue = DnDStyle.HueBad;
                    break;

                default:
                    _statusLabel.Text = "You are dying. Three of either ends it.";
                    _statusLabel.Hue = DnDStyle.HueBody;
                    break;
            }

            for (int i = 0; i < _successPips.Length; ++i)
            {
                _successPips[i].IsVisible = i < DnDDeathState.Successes;
            }

            for (int i = 0; i < _failurePips.Length; ++i)
            {
                _failurePips[i].IsVisible = i < DnDDeathState.Failures;
            }
        }

        public override void Dispose()
        {
            DnDDeathState.Changed -= OnDeathStateChanged;

            base.Dispose();
        }
    }
}
