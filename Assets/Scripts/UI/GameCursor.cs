using UnityEngine;

namespace MineArena.Game.UI
{
    public static class GameCursor
    {
        private const string CursorResourcePath = "Cursor";
        private static readonly Vector2 HotSpot = Vector2.zero;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnGameStart()
        {
            var cursorSprite = Resources.Load<Sprite>(CursorResourcePath);

            if (cursorSprite == null)
            {
                Debug.LogWarning($"Cursor sprite was not found in Resources by path '{CursorResourcePath}'.");
                return;
            }

            Set(cursorSprite);
        }

        public static void Set(Sprite cursorSprite)
        {
            if (cursorSprite == null)
            {
                UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                return;
            }

            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.SetCursor(cursorSprite.texture, HotSpot, CursorMode.Auto);
        }
    }
}
