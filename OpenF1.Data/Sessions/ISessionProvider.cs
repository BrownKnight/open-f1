namespace OpenF1.Data;

public interface ISessionProvider
{
    ValueTask<string> GetSessionName();
}
