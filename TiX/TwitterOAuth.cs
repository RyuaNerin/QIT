namespace TiX
{
    internal static class TwitterOAuthKey
    {
        private const string AppKeySecret = "";

        public static string AppKey    { get; } = AppKeySecret.Substring(0, AppKeySecret.IndexOf('_'));
        public static string AppSecret { get; } = AppKeySecret.Substring(AppKeySecret.LastIndexOf('_') + 1);
    }
}
