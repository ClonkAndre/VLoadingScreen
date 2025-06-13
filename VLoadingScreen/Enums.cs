namespace VLoadingScreen
{

    internal enum TargetPosition
    {
        Center,
        Left,
        Right
    }
    internal enum StartingPosition
    {
        Left,
        Right
    }

    internal enum TextureOriginPosition
    {
        TopLeft,
        Center,
        BottomCenter
    }
    internal enum TextureType
    {
        Unknown,
        All, // For texture creation: This tells the function to load texture of every type.
        Background,
        Character,
        Logo
    }

}
