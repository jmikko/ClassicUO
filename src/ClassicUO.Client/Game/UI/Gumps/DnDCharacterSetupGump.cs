// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.DnD;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// The character sheet you fill in once, shown right after vanilla creation in response to the
    /// server's DnDCreationPrompt (0xBF/0x40). Collects species, class, ability scores and skill
    /// proficiencies, and submits them with Send_DnDCharacterSetup.
    /// <para>
    /// Laid out in two columns because the whole sheet has to be visible at once - a character is
    /// built by weighing its parts against each other, and a player who has to scroll to see their
    /// class while setting Strength is being asked to hold the sheet in their head instead.
    /// </para>
    /// </summary>
    internal class DnDCharacterSetupGump : Gump
    {
        private const int BUTTON_CONFIRM = 1;

        private const int CLASS_BUTTON_GROUP = 1;
        private const int CLASS_BUTTON_BASE_ID = 100; // classIndex N -> buttonID 100+N

        private const int SPECIES_BUTTON_GROUP = 2;
        private const int SPECIES_BUTTON_BASE_ID = 200; // table index N -> buttonID 200+N

        private const int SKILL_BUTTON_BASE_ID = 300; // DnDSkill N -> buttonID 300+N

        private const int WIDTH = 520;

        private readonly StbTextBox[] _scoreBoxes = new StbTextBox[6];
        private readonly Label[] _modifierLabels = new Label[6];

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
        // Scripts/Misc/RaceDefinitions.cs (0/1) and Scripts/Engines/Races/RaceSystem.cs (32-37).
        //
        // Index 2 was Gargoyle, which is a ServUO race with no D&D equivalent and has been removed
        // server-side; the gap in the numbering is deliberate, since these are RaceIndex values
        // rather than positions.
        private static readonly (string Name, int RaceIndex)[] _species =
        {
            ("Human", 0), ("Elf", 1),
            ("Dwarf", 32), ("Halfling", 33), ("Gnome", 34),
            ("Half-Orc", 35), ("Tiefling", 36), ("Dragonborn", 37)
        };

        private static readonly string[] _scoreNames = { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };

        private int _selectedClassIndex;
        private int _selectedSpeciesRaceIndex;

        private readonly List<DnDSkill> _chosenSkills = new List<DnDSkill>();
        private readonly List<NiceButton> _skillButtons = new List<NiceButton>();

        private Label _skillHint;
        private int _skillsY;

        public DnDCharacterSetupGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;

            Width = WIDTH;
            Height = 470;

            int y = DnDStyle.AddFrame(this, "Create Your Character", Width, Height);

            const int columnWidth = (WIDTH / 2) - DnDStyle.Margin - 6;
            int rightX = (WIDTH / 2) + 4;

            BuildSpecies(y, columnWidth);
            int classBottom = BuildClasses(y, columnWidth);

            BuildAbilityScores(y, rightX, columnWidth);
            BuildSkills(classBottom + DnDStyle.SectionGap, columnWidth);

            DnDStyle.AddFooter(this, Height - 46, Width, BUTTON_CONFIRM, "Begin Adventuring");

            DnDStyle.Centre(this);
        }

        private void BuildSpecies(int y, int columnWidth)
        {
            y = DnDStyle.AddSection(this, "Species", y, Width);

            const int columns = 2;
            int cellWidth = columnWidth / columns;

            for (int i = 0; i < _species.Length; ++i)
            {
                int col = i % columns;
                int row = i / columns;

                NiceButton button = DnDStyle.AddOption(
                    this,
                    DnDStyle.Margin + (col * cellWidth),
                    y + (row * DnDStyle.RowHeight),
                    cellWidth - 4,
                    _species[i].Name,
                    SPECIES_BUTTON_BASE_ID + i,
                    SPECIES_BUTTON_GROUP,
                    i == 0);

                if (i == 0)
                {
                    _selectedSpeciesRaceIndex = _species[i].RaceIndex;
                }
            }
        }

        private int BuildClasses(int y, int columnWidth)
        {
            const int columns = 2;
            int rows = (_species.Length + columns - 1) / columns;

            y = DnDStyle.AddSection(
                this, "Class", y + (rows * DnDStyle.RowHeight) + DnDStyle.SectionGap, Width);

            int cellWidth = columnWidth / columns;

            for (int i = 0; i < _classNames.Length; ++i)
            {
                int col = i % columns;
                int row = i / columns;

                DnDStyle.AddOption(
                    this,
                    DnDStyle.Margin + (col * cellWidth),
                    y + (row * DnDStyle.RowHeight),
                    cellWidth - 4,
                    _classNames[i],
                    CLASS_BUTTON_BASE_ID + i,
                    CLASS_BUTTON_GROUP,
                    i == 0);
            }

            return y + (((_classNames.Length + columns - 1) / columns) * DnDStyle.RowHeight);
        }

        /// <summary>
        /// Ability scores, each showing the modifier it produces. The modifier is the number that
        /// actually reaches the dice, so showing only the score asks the player to do the
        /// arithmetic that decides whether their choice was any good.
        /// </summary>
        private void BuildAbilityScores(int y, int x, int columnWidth)
        {
            y = DnDStyle.AddSection(this, "Ability Scores", y, Width);

            DnDStyle.AddPanel(this, x - 4, y - 2, columnWidth + 8, (_scoreNames.Length * 30) + 6);

            for (int i = 0; i < _scoreNames.Length; ++i)
            {
                int rowY = y + (i * 30);

                DnDStyle.AddText(this, _scoreNames[i], x + 4, rowY + 2, 120, DnDStyle.HueBody);

                Add(new ResizePic(0x0BB8) { X = x + 124, Y = rowY - 1, Width = 46, Height = 22 });

                var box = new StbTextBox(0xFF, 2, 38, true, FontStyle.BlackBorder)
                {
                    X = x + 130,
                    Y = rowY + 1,
                    Width = 36,
                    Height = 20
                };

                box.SetText("10");

                Add(box);

                _scoreBoxes[i] = box;
                _modifierLabels[i] = DnDStyle.AddText(this, "+0", x + 178, rowY + 2, 44, DnDStyle.HueGood);
            }

            DnDStyle.AddHint(this, "3 to 20. The modifier is what rolls.", x + 4, y + (_scoreNames.Length * 30) + 4, columnWidth);
        }

        /// <summary>
        /// The skill picker. Which skills are on offer and how many may be taken depend entirely on
        /// the class, so this is rebuilt whenever the class changes rather than shown as a fixed
        /// list with most of it greyed out.
        /// </summary>
        private void BuildSkills(int y, int columnWidth)
        {
            _skillsY = DnDStyle.AddSection(this, "Skill Proficiencies", y, Width);

            _skillHint = DnDStyle.AddHint(this, string.Empty, DnDStyle.Margin, _skillsY, Width - (DnDStyle.Margin * 2));

            RebuildSkillButtons();
        }

        private void RebuildSkillButtons()
        {
            foreach (NiceButton button in _skillButtons)
            {
                button.Dispose();
            }

            _skillButtons.Clear();
            _chosenSkills.Clear();

            DnDSkill[] offered = DnDSkills.GetChoices(_classNames[_selectedClassIndex]);
            int allowed = DnDSkills.GetChoiceCount(_classNames[_selectedClassIndex]);

            _skillHint.Text = string.Format(
                "Choose {0} for {1}. Unpicked slots are filled for you.", allowed, _classNames[_selectedClassIndex]);

            const int columns = 4;
            int cellWidth = (Width - (DnDStyle.Margin * 2)) / columns;
            int top = _skillsY + 18;

            for (int i = 0; i < offered.Length; ++i)
            {
                int col = i % columns;
                int row = i / columns;

                NiceButton button = DnDStyle.AddToggle(
                    this,
                    DnDStyle.Margin + (col * cellWidth),
                    top + (row * DnDStyle.RowHeight),
                    cellWidth - 4,
                    DnDSkills.GetDisplayName(offered[i]),
                    SKILL_BUTTON_BASE_ID + (int)offered[i]);

                _skillButtons.Add(button);
            }
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

                // A new class offers a different set of skills, so anything already picked is no
                // longer necessarily legal - start the section over rather than silently dropping.
                RebuildSkillButtons();
                return;
            }

            if (buttonID >= SKILL_BUTTON_BASE_ID)
            {
                ToggleSkill(buttonID - SKILL_BUTTON_BASE_ID);
                return;
            }

            if (buttonID != BUTTON_CONFIRM)
            {
                return;
            }

            Submit();
        }

        private void ToggleSkill(int skillValue)
        {
            var skill = (DnDSkill)skillValue;
            int allowed = DnDSkills.GetChoiceCount(_classNames[_selectedClassIndex]);

            if (_chosenSkills.Contains(skill))
            {
                _chosenSkills.Remove(skill);
            }
            else if (_chosenSkills.Count < allowed)
            {
                _chosenSkills.Add(skill);
            }
            else
            {
                // At the limit, taking a new skill replaces the oldest - refusing the click would
                // leave the player having to work out which one to drop first.
                _chosenSkills.RemoveAt(0);
                _chosenSkills.Add(skill);
            }

            RefreshSkillSelection();
        }

        private void RefreshSkillSelection()
        {
            foreach (NiceButton button in _skillButtons)
            {
                bool picked = _chosenSkills.Contains((DnDSkill)(button.ButtonParameter - SKILL_BUTTON_BASE_ID));

                button.TextLabel.Hue = picked ? DnDStyle.HueSelected : DnDStyle.HueBody;
            }

            int allowed = DnDSkills.GetChoiceCount(_classNames[_selectedClassIndex]);

            _skillHint.Text = string.Format(
                "Chosen {0} of {1} for {2}.", _chosenSkills.Count, allowed, _classNames[_selectedClassIndex]);
        }

        public override void Update()
        {
            base.Update();

            // Modifiers track what is typed, so the consequence of a score is visible while it is
            // being chosen rather than after the sheet is submitted.
            for (int i = 0; i < _scoreBoxes.Length; ++i)
            {
                int score;

                if (!int.TryParse(_scoreBoxes[i].Text, out score))
                {
                    score = 10;
                }

                int modifier = (int)System.Math.Floor((score - 10) / 2.0);

                _modifierLabels[i].Text = modifier >= 0 ? "+" + modifier : modifier.ToString();
                _modifierLabels[i].Hue = modifier >= 0 ? DnDStyle.HueGood : DnDStyle.HueBad;
            }
        }

        private void Submit()
        {
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
                _selectedClassIndex, _selectedSpeciesRaceIndex,
                _chosenSkills
            );

            Dispose();
        }
    }
}
