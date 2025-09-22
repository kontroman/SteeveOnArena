namespace MineArena.Basics
{
    public static class Constants
    {
        public static class FortuneWheel
        {
            public static int MaxNumberTurns = 10;
            public static int MinNumberTurns = 5;
            public static float MinValueTimer = 3.0f;
            public static float MaxValueTimer = 5.0f;
            public static string ItemIcon = "ItemIcon";
            public static string ItemText = "ItemText";
            public static string ItemAmount = "ItemAmount";
            public static float AngelDeviation = 90f;
        }

        public static class AudioNames
        {
            public const string BackgroundMusic = "BackGround";
            public const string DropResource = "DropResourse";
        }

        public static class PlayerSettings
        {
            public static float Speed = 5f;
            public static float RotationSpeed = 10f;
            public static float JumpHeight = 1.5f;
            public static float Gravity = -20f;
            public static float JumpForce = 7f;
            public static float MinHealth = 0f;
        }

        public static class UISettings
        {
            public static float SpeedFillProgressBar = 10f;
        }

        public static class UIKeys
        {
            public static string DefaultTitle = "DefaultTitle";
            public static string PrizeKey = "GotPrize";
        }

        public static class GameTags
        {
            public static string MainCanvas = "MainCanvas";
            public static string Player = "Player";
        }

        public static class SceneNames
        {
            public static string PlayerBaseScene = "SampleScene";
            public static string GameplayScene = "GameplayScene";
        }
    }
}