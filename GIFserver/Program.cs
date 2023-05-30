

using System.ComponentModel;

namespace GIFserver;

class Program
{
    public static void Main(string[] args)
    {
        
        Thread.CurrentThread.IsBackground = false;
        var cts = new CancellationTokenSource();
        CancellationToken token= cts.Token;
     
        
        HTTPServers server = new HTTPServers("localhost", 5050);
        Task serverRun =  Task.Run(()=>server.Start(token));
        TakeCommands(cts,server);
    }

    static void TakeCommands(CancellationTokenSource cts,HTTPServers s)
    {

        string command;
        do
        {
            command = Console.ReadLine();
            if (command == "quit")
            {
                cts.Cancel();
            }
            else if(command=="report")
            {
                s.Report();
            }
            else
            {
                Console.WriteLine("Unknown command");
            }
        } while (command != "quit");
        Console.ReadLine();
        Console.WriteLine("Server stopped");
    }
}