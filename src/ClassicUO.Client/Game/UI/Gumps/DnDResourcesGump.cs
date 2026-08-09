using System;
using System.Collections.Generic;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.UI.Gumps
{
    public struct DnDResourceEntry
    {
        public string Name;
        public int Current;
        public int Max;
        public string Description;
    }

    internal class DnDResourcesGump : Gump
    {
        private List<DnDResourceEntry> _resources;
        private AlphaBlendControl _background;
        private DataBox _dataBox;

        public DnDResourcesGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            Width = 300;
            Height = 100;

            _background = new AlphaBlendControl(0.8f) { Width = Width, Height = Height };
            Add(_background);

            var title = new Label("Resources & Features", true, DnDStyle.HueHeading, Width - 20, 1, FontStyle.BlackBorder) { X = 10, Y = 10 };
            Add(title);

            _dataBox = new DataBox(10, 35, 0, 0);
            Add(_dataBox);
        }

        public void Update(List<DnDResourceEntry> resources)
        {
            _resources = resources;
            _dataBox.Clear();

            int currentY = 0;

            foreach (var res in _resources)
            {
                var label = new Label($"{res.Name}: {res.Current} / {res.Max}", true, 0x0481, Width - 20, 1) { X = 0, Y = currentY };
                label.SetTooltip(res.Description);
                _dataBox.Add(label);
                currentY += 20;
            }

            Height = Math.Max(100, currentY + 50);
            _background.Height = Height;
            _dataBox.Height = currentY;
        }
    }

    internal static class DnDResourcesState
    {
        private static List<DnDResourceEntry> _resources = new List<DnDResourceEntry>();
        private static bool _isOpen = false;

        public static void Apply(List<DnDResourceEntry> resources)
        {
            _resources = resources;

            var gump = UIManager.GetGump<DnDResourcesGump>();
            if (gump != null)
            {
                gump.Update(resources);
            }
        }

        public static void ToggleGump(World world)
        {
            var gump = UIManager.GetGump<DnDResourcesGump>();
            if (gump != null)
            {
                gump.Dispose();
                _isOpen = false;
            }
            else
            {
                gump = new DnDResourcesGump(world);
                gump.X = 100;
                gump.Y = 100;
                gump.Update(_resources);
                UIManager.Add(gump);
                _isOpen = true;
            }
        }
    }
}
