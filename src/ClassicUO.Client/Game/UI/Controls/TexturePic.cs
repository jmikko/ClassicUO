// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    /// <summary>
    /// Draws a texture that did not come out of the Ultima art files.
    /// <para>
    /// Every other picture control here takes a gump id and looks the art up in gumpart. That is
    /// the right default, and it is why bespoke art had nowhere to go: a PNG on disk has no gump id
    /// and never will. This draws a <see cref="Texture2D"/> straight, so anything loadable through
    /// <c>Texture2D.FromStream</c> can sit in a gump beside ordinary Ultima art.
    /// </para>
    /// <para>
    /// Rendered through <c>AddGumpNoAtlas</c> rather than the atlas path, because the atlas is
    /// built from the art files at load time and an arbitrary runtime texture is not in it.
    /// </para>
    /// </summary>
    internal class TexturePic : Control
    {
        private readonly Texture2D _texture;

        public TexturePic(Texture2D texture, int width, int height)
        {
            _texture = texture;

            Width = width;
            Height = height;

            CanMove = false;
            WantUpdateSize = false;
            AcceptMouseInput = true;
        }

        /// <summary>Tint, for the greyed-out state a spellbook uses on unaffordable spells.</summary>
        public ushort Hue { get; set; }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            if (_texture == null || _texture.IsDisposed)
            {
                return false;
            }

            float layerDepth = layerDepthRef;
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue);

            renderLists.AddGumpNoAtlas
            (
                (batcher) =>
                {
                    batcher.Draw
                    (
                        _texture,
                        new Rectangle(x, y, Width, Height),
                        hueVector,
                        layerDepth
                    );

                    return true;
                }
            );

            return true;
        }
    }
}
