// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.DnD;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// Phase 1 D&amp;D 5.5e vertical slice: shown once, right after vanilla character creation
    /// completes, in response to the server's DnDCreationPrompt (0xBF/0x40). Collects a species
    /// pick, a class pick, and six ability scores (typed in directly - no point-buy validation UI
    /// yet, the server re-validates bounds anyway) and submits them via Send_DnDCharacterSetup.
    /// </summary>
    internal class DnDCharacterSetupGump : Gump
    {
        private const int BUTTON_CONFIRM = 1;

        private const int CLASS_BUTTON_GROUP = 1;
        private const int CLASS_BUTTON_BASE_ID = 100; // classIndex N -> buttonID 100+N

        private const int SPECIES_BUTTON_GROUP = 2;
        private const int SPECIES_BUTTON_BASE_ID = 200; // table index N -> buttonID 200+N

        private readonly StbTextBox[] _scoreBoxes = new StbTextBox[6];

        // Order MUST match Scripts/Engines/Classes/ClassSystem.cs's CharacterClass.Register calls
        // exactly - classIndex is a plain array index over the wire, not a name lookup.
        private static readonly string[] _classNames =
        {
            "Fighter", "Barbarian", "Bard", "Cleric", "Druid", "Monk",
            "Paladin", "Ranger", "Rogue", "Sorcerer", "Warlock", "Wizard"
        };

        // (display name, Race.RaceIndex) - the index is the server's actual RaceIndex, NOT a
        // sequential position, since race-registration order across files isn't guaranteed (see
        // DnDCharacterSetupEventArgs.SpeciesIndex server-side for the full reasoning). Must match
        // Scripts/Misc/RaceDefinitions.cs (0/1/2) and Scripts/Engines/Races/RaceSystem.cs (32-37).
        private static readonly (string Name, int RaceIndex)[] _species =
        {
            ("Human", 0), ("Elf", 1), ("Gargoyle", 2),
            ("Dwarf", 32), ("Halfling", 33), ("Gnome", 34),
            ("Half-Orc", 35), ("Tiefling", 36), ("Dragonborn", 37)
        };

        private static readonly string[] _scoreNames = { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };

        private int _selectedClassIndex;
        private int _selectedSpeciesRaceIndex;

        public DnDCharacterSetupGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;

            const int gridColumns = 3;

            int speciesRows = (_species.Length + gridColumns - 1) / gridColumns;
            int classRows = (_classNames.Length + gridColumns - 1) / gridColumns;
            const int gridRowHeight = 26;

            int speciesGridY = 62;
            int speciesGridHeight = speciesRows * gridRowHeight;

            int classLabelY = speciesGridY + speciesGridHeight + 8;
            int classGridY = classLabelY + 22;
            int classGridHeight = classRows * gridRowHeight;

            int scoresY = classGridY + classGridHeight + 12;
            int scoresHeight = _scoreNames.Length * 28;

            int buttonY = scoresY + scoresHeight + 10;

            Width = 300;
            Height = buttonY + 40;

            Add(new ResizePic(0x0A28) { Width = Width, Height = Height });

            Add(new Label("D&D 5.5e Character Setup", true, 0x0035, Width - 20, 0xFF, FontStyle.BlackBorder) { X = 14, Y = 14 });
            Add(new Label("Species:", true, 0x0481, 100, 0xFF, FontStyle.BlackBorder) { X = 14, Y = 40 });

            int gridColWidth = (Width - 28) / gridColumns;

            for (int i = 0; i < _species.Length; ++i)
            {
                int col = i % gridColumns;
                int row = i / gridColumns;

                NiceButton speciesButton = new NiceButton
                (
                    14 + (col * gridColWidth),
                    speciesGridY + (row * gridRowHeight),
                    gridColWidth - 4,
                    22,
                    ButtonAction.Activate,
                    _species[i].Name,
                    SPECIES_BUTTON_GROUP
                )
                {
                    ButtonParameter = SPECIES_BUTTON_BASE_ID + i
                };

                Add(speciesButton);

                if (i == 0)
                {
                    speciesButton.IsSelected = true;
                    _selectedSpeciesRaceIndex = _species[i].RaceIndex;
                }
            }

            Add(new Label("Class:", true, 0x0481, 100, 0xFF, FontStyle.BlackBorder) { X = 14, Y = classLabelY });

            for (int i = 0; i < _classNames.Length; ++i)
            {
                int col = i % gridColumns;
                int row = i / gridColumns;

                NiceButton classButton = new NiceButton
                (
                    14 + (col * gridColWidth),
                    classGridY + (row * gridRowHeight),
                    gridColWidth - 4,
                    22,
                    ButtonAction.Activate,
                    _classNames[i],
                    CLASS_BUTTON_GROUP
                )
                {
                    ButtonParameter = CLASS_BUTTON_BASE_ID + i
                };

                Add(classButton);

                if (i == 0)
                {
                    classButton.IsSelected = true;
                }
            }

            for (int i = 0; i < _scoreNames.Length; ++i)
            {
                int y = scoresY + (i * 28);

                Add
                (
                    new Label(_scoreNames[i], true, 0x0481, 140, 0xFF, FontStyle.BlackBorder)
                    {
                        X = 20,
                        Y = y
                    }
                );

                Add
                (
                    new ResizePic(0x0BB8)
                    {
                        X = 170,
                        Y = y - 3,
                        Width = 50,
                        Height = 22
                    }
                );

                StbTextBox box = new StbTextBox(0xFF, 2, 42, true, FontStyle.BlackBorder)
                {
                    X = 176,
                    Y = y - 1,
                    Width = 40,
                    Height = 20
                };

                box.SetText("10");

                Add(box);

                _scoreBoxes[i] = box;
            }

            Add
            (
                new Button(BUTTON_CONFIRM, 0x0481, 0x0482, 0x0483)
                {
                    X = 20,
                    Y = buttonY,
                    ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                new Label("Confirm", true, 0x0481, 120, 0xFF, FontStyle.BlackBorder)
                {
                    X = 48,
                    Y = buttonY + 2
                }
            );

            X = (Client.Game.ClientBounds.Width - Width) >> 1;
            Y = (Client.Game.ClientBounds.Height - Height) >> 1;
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID >= SPECIES_BUTTON_BASE_ID && buttonID < SPECIES_BUTTON_BASE_ID + _species.Length)
            {
                _selectedSpeciesRaceIndex = _species[buttonID - SPECIES_BUTTON_BASE_ID].RaceIndex;
                return;
            }

            if (buttonID >= CLASS_BUTTON_BASE_ID && buttonID < CLASS_BUTTON_BASE_ID + _classNames.Length)
            {
                _selectedClassIndex = buttonID - CLASS_BUTTON_BASE_ID;
                return;
            }

            if (buttonID != BUTTON_CONFIRM)
            {
                return;
            }

            int[] scores = new int[6];

            for (int i = 0; i < _scoreBoxes.Length; ++i)
            {
                if (!int.TryParse(_scoreBoxes[i].Text, out scores[i]))
                {
                    scores[i] = 10;
                }
            }

            NetClient.Socket.Send_DnDCharacterSetup
            (
                World,
                scores[0], scores[1], scores[2], scores[3], scores[4], scores[5],
                _selectedClassIndex, _selectedSpeciesRaceIndex
            );

            Dispose();
        }
    }
}
