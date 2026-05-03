using UnityEngine;

namespace MineArena.Windows.Crafting
{
    public sealed class CraftingPixelStyle
    {
        public Sprite Overlay { get; private set; }
        public Sprite Panel { get; private set; }
        public Sprite PanelInset { get; private set; }
        public Sprite Slot { get; private set; }
        public Sprite SlotSelected { get; private set; }
        public Sprite Button { get; private set; }
        public Sprite ButtonSelected { get; private set; }
        public Sprite ButtonDisabled { get; private set; }
        public Sprite CraftButton { get; private set; }
        public Sprite PlaceholderIcon { get; private set; }

        public Color Text => new Color32(245, 238, 218, 255);
        public Color MutedText => new Color32(163, 155, 135, 255);
        public Color WarningText => new Color32(238, 104, 76, 255);
        public Color SuccessText => new Color32(124, 210, 112, 255);
        public Color DarkText => new Color32(39, 31, 26, 255);

        public static CraftingPixelStyle Create()
        {
            return Create(null, null, null, null, null, null, null, null, null, null);
        }

        public static CraftingPixelStyle Create(
            Sprite overlay,
            Sprite panel,
            Sprite panelInset,
            Sprite slot,
            Sprite slotSelected,
            Sprite button,
            Sprite buttonSelected,
            Sprite buttonDisabled,
            Sprite craftButton,
            Sprite placeholderIcon)
        {
            return new CraftingPixelStyle
            {
                Overlay = overlay != null ? overlay : CreateFlatSprite("CraftOverlay", new Color32(0, 0, 0, 170)),
                Panel = panel != null ? panel : CreateFrameSprite("CraftPanel", new Color32(55, 52, 48, 255), new Color32(18, 16, 14, 255), new Color32(116, 108, 94, 255), new Color32(29, 27, 25, 255)),
                PanelInset = panelInset != null ? panelInset : CreateFrameSprite("CraftPanelInset", new Color32(36, 35, 33, 255), new Color32(13, 12, 11, 255), new Color32(79, 74, 66, 255), new Color32(21, 20, 19, 255)),
                Slot = slot != null ? slot : CreateFrameSprite("CraftSlot", new Color32(80, 76, 68, 255), new Color32(20, 18, 16, 255), new Color32(142, 132, 113, 255), new Color32(42, 39, 35, 255)),
                SlotSelected = slotSelected != null ? slotSelected : CreateFrameSprite("CraftSlotSelected", new Color32(98, 90, 72, 255), new Color32(248, 219, 106, 255), new Color32(255, 241, 154, 255), new Color32(122, 91, 32, 255)),
                Button = button != null ? button : CreateFrameSprite("CraftButton", new Color32(96, 74, 51, 255), new Color32(30, 21, 14, 255), new Color32(148, 114, 74, 255), new Color32(55, 39, 28, 255)),
                ButtonSelected = buttonSelected != null ? buttonSelected : CreateFrameSprite("CraftButtonSelected", new Color32(113, 92, 55, 255), new Color32(242, 207, 99, 255), new Color32(255, 230, 132, 255), new Color32(83, 55, 23, 255)),
                ButtonDisabled = buttonDisabled != null ? buttonDisabled : CreateFrameSprite("CraftButtonDisabled", new Color32(61, 59, 57, 255), new Color32(24, 22, 21, 255), new Color32(84, 80, 75, 255), new Color32(38, 36, 34, 255)),
                CraftButton = craftButton != null ? craftButton : CreateFrameSprite("CraftButtonGreen", new Color32(67, 128, 62, 255), new Color32(18, 45, 24, 255), new Color32(110, 181, 96, 255), new Color32(35, 77, 39, 255)),
                PlaceholderIcon = placeholderIcon != null ? placeholderIcon : CreatePlaceholderIcon()
            };
        }

        private static Sprite CreateFlatSprite(string name, Color32 color)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[16];

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 4f);
        }

        private static Sprite CreateFrameSprite(string name, Color32 fill, Color32 border, Color32 highlight, Color32 shadow)
        {
            const int size = 16;
            const int borderSize = 2;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var isBorder = x < borderSize || y < borderSize || x >= size - borderSize || y >= size - borderSize;
                    var color = fill;

                    if (isBorder)
                    {
                        color = border;
                    }

                    if ((x == borderSize || y == size - borderSize - 1) && x >= borderSize && y >= borderSize)
                    {
                        color = shadow;
                    }

                    if ((x == size - borderSize - 1 || y == borderSize) && x >= borderSize && y >= borderSize)
                    {
                        color = highlight;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0u,
                SpriteMeshType.FullRect,
                new Vector4(3f, 3f, 3f, 3f));
        }

        private static Sprite CreatePlaceholderIcon()
        {
            const int size = 16;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "CraftPlaceholderIcon",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var clear = new Color32(0, 0, 0, 0);
            var top = new Color32(116, 107, 88, 255);
            var left = new Color32(84, 78, 66, 255);
            var right = new Color32(52, 49, 43, 255);
            var outline = new Color32(20, 18, 16, 255);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            for (var y = 3; y < 13; y++)
            {
                for (var x = 3; x < 13; x++)
                {
                    var color = y > 9 ? top : x < 8 ? left : right;

                    if (x == 3 || x == 12 || y == 3 || y == 12)
                    {
                        color = outline;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
