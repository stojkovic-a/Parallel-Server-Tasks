

using System.ComponentModel;

namespace GIFserver;

class Program
{
    static BackgroundWorker bw;

    public static void Main(string[] args)
    {

        int numberOfProcessors=Environment.ProcessorCount;
        ThreadPool.SetMinThreads(numberOfProcessors, 10*numberOfProcessors);
        ThreadPool.SetMaxThreads(10 * numberOfProcessors, 100 * numberOfProcessors);
        
        Thread.CurrentThread.IsBackground = false;

        bw = new BackgroundWorker()
        {
            WorkerSupportsCancellation = true,
            WorkerReportsProgress = false,
        };
        
        HTTPServers server = new HTTPServers("localhost", 5050);
        bw.DoWork += server.Start;
        bw.RunWorkerCompleted += server.Stop;
        bw.RunWorkerAsync(argument: bw);
        TakeCommands();
    }

    static void TakeCommands()
    {

        string command;
        do
        {
            command = Console.ReadLine();
            if (command != "quit")
            {
                Console.WriteLine("Invalid Command");
            }
            else
            {
                bw.CancelAsync();
            }
        } while (command != "quit");
        Console.ReadLine();
    }
}