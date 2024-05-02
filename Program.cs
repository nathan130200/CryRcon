using CryRcon;

Dictionary<char, ConsoleColor> colorFromID = new()
{
    ['0'] = ConsoleColor.Black,
    ['1'] = ConsoleColor.White,
    ['2'] = ConsoleColor.Blue,
    ['3'] = ConsoleColor.Green,
    ['4'] = ConsoleColor.Red,
    ['5'] = ConsoleColor.DarkCyan,
    ['6'] = ConsoleColor.Yellow,
    ['7'] = ConsoleColor.Magenta,
    ['8'] = ConsoleColor.DarkYellow,
    ['9'] = ConsoleColor.Gray
};

var options = new Dictionary<string, string>();

if (args.Contains("--help"))
    goto _help;

foreach (var arg in args)
{
    if (arg.StartsWith("--"))
    {
        var temp = arg.Substring(2);
        var ofs = temp.IndexOf('=');

        if (ofs == -1)
            options[temp] = string.Empty;
        else
        {
            var key = temp[0..ofs];
            var value = temp[(ofs + 1)..];
            options[key] = value;

            Console.WriteLine(" Set option '{0}': {1}", key, value);
        }
    }
}

Console.WriteLine();

goto _next;

_help:
{
    var argv = Environment.GetCommandLineArgs().FirstOrDefault("Rcon");

    Console.WriteLine($"\n\n{Path.GetFileName(argv)} [...options]" +
        $"\n   --host=[ipv4|ipv6|fqdn] - Specify rcon server to connect." +
        $"\n   --port=[1-65535] - Specify rcon server port to connect." +
        $"\n   --password=[value] - Specify rcon server password." +
        $"\n");

    Console.WriteLine();

    Environment.Exit(1);
}

_next:

ushort rconPort;

if (!options.ContainsKey("host"))
{
    Console.WriteLine("ERROR: Rcon server address is required.");
    Environment.Exit(2);
    return;
}

string rconHost = options["host"];

if (Uri.CheckHostName(rconHost) == UriHostNameType.Unknown)
{
    Console.WriteLine("ERROR: Rcon server address is invalid.");
    Environment.Exit(3);
    return;
}

if (!options.ContainsKey("port"))
{
    Console.WriteLine("ERROR: Rcon port is required.");
    Environment.Exit(4);
    return;
}

if (!ushort.TryParse(options["port"], out rconPort) || rconPort == 0)
{
    Console.WriteLine("ERROR: Rcon port is invalid.");
    Environment.Exit(5);
    return;
}

var client = new Client(rconHost, rconPort, options.GetValueOrDefault("password"));

var task = client.ConnectAsync();

Console.ForegroundColor = ConsoleColor.DarkYellow;
Console.Write("Connecting to rcon server");

Console.ForegroundColor = ConsoleColor.Gray;
Console.Write("...");

var lhs = Console.CursorLeft;

do
{
    Console.Write('.');
    await Task.Delay(1000);
}
while (!task.IsCompleted);

var rhs = Console.CursorLeft;

Console.CursorLeft = lhs;

for (int i = lhs; i < rhs; i++)
    Console.Write(' ');

Console.CursorLeft = lhs;

if (task.IsFaulted)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(" Connection failed!");
    Console.ForegroundColor = ConsoleColor.Gray;
    Environment.Exit(6);
    return;
}
else
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(" Connected!\n");
}

while (client.IsConnected)
{
    await Task.Delay(1);

    if (client.IsAuthenticated)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write("> ");
        var line = Console.ReadLine();
        var result = await client.SendCommandAsync(line);
        Console.WriteLine();

        if (!string.IsNullOrEmpty(result))
            WriteLine(result);
    }
}

Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine("RCON client disconnected.");
Console.ForegroundColor = ConsoleColor.Gray;

void WriteLine(string s)
{
    for (int i = 0; i < s.Length; i++)
    {
        var c = s[i];

        if (c == '$')
        {
            if (i + 1 < s.Length)
            {
                var p = s[++i];

                if (p == '$')
                {
                    Console.Write("$$");
                    continue;
                }

                if (!colorFromID.TryGetValue(p, out var value))
                    value = ConsoleColor.White;

                Console.ForegroundColor = value;
            }
        }
        else
            Console.Write(c);
    }
}