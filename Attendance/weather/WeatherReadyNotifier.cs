namespace Attendance.Weather
{
    public static class WeatherReadyNotifier
    {
        public static TaskCompletionSource<bool> ReadySignal { get; } = new();
    }
}
