// SPDX-License-Identifier: BSD-2-Clause

using System.Linq;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps.Login;
using ClassicUO.Assets;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    internal class CharCreationGump : Gump
    {
        private PlayerMobile _character;
        private int _cityIndex;
        private CharCreationStep _currentStep;
        private LoadingGump _loadingGump;
        private readonly LoginScene _loginScene;
        private ProfessionInfo _selectedProfession;

        public CharCreationGump(World world, LoginScene scene) : base(world, 0, 0)
        {
            _loginScene = scene;
            Add(new CreateCharAppearanceGump(world), 1);
            SetStep(CharCreationStep.Appearence);
            CanCloseWithRightClick = false;
        }

        internal static int _skillsCount => Client.Game.UO.Version >= ClientVersion.CV_70160 ? 4 : 3;

        /// <summary>
        /// Appearance is finished, so the character is created immediately.
        /// <para>
        /// Ultima's remaining creation steps - profession, trade skills, starting city - have no
        /// D&amp;D meaning and are gone. A profession is a class, trade skills are the class's own
        /// skill choices, and both are picked on the D&amp;D setup screen the server raises once the
        /// character exists. The city list was already ignored: the server picks the start location
        /// itself.
        /// </para>
        /// <para>
        /// The stats and skills left on the character here are whatever the appearance step
        /// defaulted to. That is fine - the server overwrites the stats and never reads UO skills,
        /// because D&amp;D combat resolves on ability scores instead.
        /// </para>
        /// </summary>
        public void SetCharacter(PlayerMobile character)
        {
            _character = character;

            CreateCharacter(profession: 0);
        }

        // SetAttributes, SetCity, and SetProfession went with the screens that called them.
        // Ultima professions set UO stats and trade skills, neither of which D&D combat reads,
        // and the city index was already ignored - the server picks the start location itself.

        public void CreateCharacter(byte profession)
        {
            _loginScene.CreateCharacter(_character, _cityIndex, profession);
        }

        public void StepBack(int steps = 1)
        {
            if (_currentStep == CharCreationStep.Appearence)
            {
                _loginScene.StepBack();
            }
            else
            {
                SetStep(_currentStep - steps);
            }
        }

        public void ShowMessage(string message)
        {
            int currentPage = ActivePage;

            if (_loadingGump != null)
            {
                Remove(_loadingGump);
            }

            Add(_loadingGump = new LoadingGump(World, message, LoginButtons.OK, a => ChangePage(currentPage)), 4);
            ChangePage(4);
        }

        private void SetStep(CharCreationStep step)
        {
            _currentStep = step;

            // Appearance is the only step left. Profession, trade, and city were removed rather
            // than left unreachable, so nothing can route back into them by accident - a dead
            // branch that still compiles is a dead branch someone eventually calls.
            ChangePage(1);
        }

        private enum CharCreationStep
        {
            Appearence = 0,
        }
    }
}
