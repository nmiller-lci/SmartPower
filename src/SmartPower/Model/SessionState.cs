namespace SmartPower.Model
{
    public enum SessionState
    {
        Created,
        Connecting,
        Connected,
        Error,
        Verifying,
        Verified,
        Ended,
        Skipped
    }
}