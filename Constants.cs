namespace CryRcon;

public static class Constants
{
    public const int RPC_MAGIC = 0x18181818;

    public const int RPC_SERVER_IN_SESSION = 0;
    public const int RPC_SERVER_CHALLENGE = 1;
    public const int RPC_SERVER_AUTH_SUCCESS = 2;
    public const int RPC_SERVER_AUTH_FAILED = 3;
    public const int RPC_SERVER_COMMAND_RESULT = 4;
    public const int RPC_SERVER_AUTH_TIMEOUT = 5;

    public const int RPC_CLIENT_AUTH = 0;
    public const int RPC_CLIENT_COMMAND = 1;
}
