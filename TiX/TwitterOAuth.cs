namespace TiX
{
    internal static class TwitterOAuthKey
    {
        private const string AppKeySecret = "LS0tLS1CRUdJTiBQR1AgTUVTU0FHRS0tLS0tDQoNCmhFNERuSHhrd1ozT1ZJY1NBUWRBNzJ0aThJaXdqVTBoU1ArRVRDOGJaYnZUYXdPd3pPdWYrY0xhazVFQjhGc2cNCkFEa0UyWnE3Qkk0bEQ4STNxRkRNL1kzWjhIcmthUWw4blZrd0JIYStnN3JTZlFHcUlZNENxOW0yRVF3cVB0bmINCnQvcTlvbTNsY2hTcyswZUxKSkRqV2ZXUDJqY3lOVXRLYlVDbElVcWNEZndWTm1YM1c2S1pmVHJ0VkJORm8wWXkNCksrMCtaNURkSUorcVdWYWxoT1hUdUNoVkZ5Q05RZ3MzVXA3bFdtemlFZ1ZhY3ZJTThpWWhYaDFWUjVDSkJ3NlINClFCVDQwNjRKRUVEUWJKLzJJWXQxDQo9RnNTeQ0KLS0tLS1FTkQgUEdQIE1FU1NBR0UtLS0tLQ0K";

        public static string AppKey    { get; } = AppKeySecret.Substring(0, AppKeySecret.IndexOf('_'));
        public static string AppSecret { get; } = AppKeySecret.Substring(AppKeySecret.LastIndexOf('_') + 1);
    }
}
