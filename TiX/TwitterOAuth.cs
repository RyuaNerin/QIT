namespace TiX
{
    internal static class TwitterOAuthKey
    {
        private const string AppKeySecret = "LS0tLS1CRUdJTiBQR1AgTUVTU0FHRS0tLS0tDQoNCmhFNERuSHhrd1ozT1ZJY1NBUWRBalRZV2V4WmJEOXphK3JLMFhNdWR5NzNTWjJKVXQ4VmROVnV0bWJ6a0Iya2cNCjAxNnFJN2s4RDdKSUhUK3I5dUVFK0xQd3VrTG5XQUE2VVY1TnBORTFxTGJTZkFHVGNxSjA1UTRFRVdONGFkaUsNCkprNjJuS2tSTVFtbWZ4TS8wOXJyWENNcW1oWXF6MG4yTExKekVheXRoVENYaHhOY3M4Y2RnaWFzb2d1WGVHQmMNCjR5UW1scEtsN05yMGo3enlGTTRNcjBZaHhPdUh0bUpmc3hXRHVISUJ0Ukh6MmZTK0t4bjBmZEtGTW40cjZXcVANCmZVSVJBY3ZxTTdGclBlREh4ZFU9DQo9MnVzbg0KLS0tLS1FTkQgUEdQIE1FU1NBR0UtLS0tLQ0K";

        public static string AppKey    { get; } = AppKeySecret.Substring(0, AppKeySecret.IndexOf('_'));
        public static string AppSecret { get; } = AppKeySecret.Substring(AppKeySecret.LastIndexOf('_') + 1);
    }
}
