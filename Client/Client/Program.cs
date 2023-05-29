


using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace Client;
//enum GIFS
//{
//    catmotor=0,
//    catbroom=1,
//    husky=2,
//    pug=3,
//    scarydog=4,
//    donaldduck=5
//}
class Client
{
    private static HttpClient[] client;
    private static string[] gifs=new string[6];
  
    public static async Task Main(string[] args)
    {
        gifs[0] = "catmotor";
        gifs[1] = "catbroom";
        gifs[2] = "husky";
        gifs[3] = "pug";
        gifs[4] = "scarydog";
        gifs[5] = "donaldduck";

        int n = 1000;
        client = new HttpClient[n];

        Random rnd = new Random();
        Task[] tasks= new Task[n];
        var pocetak=Stopwatch.GetTimestamp();
        for (int i = 0; i < n; i++)
        {
            int pom = rnd.Next(6);
            client[i] = new HttpClient();
            client[i].BaseAddress = new Uri("http://localhost:5050/");
            client[i].DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("image/gif"));
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, gifs[pom]);
            // var res=await client[i].SendAsync(request);
            tasks[i]=client[i].SendAsync(request);


           // Console.WriteLine(res.StatusCode);
        }
        var poslatiSvi=Stopwatch.GetTimestamp();
        //Console.WriteLine("sent all");
        await Task.WhenAll(tasks);
        var kraj=Stopwatch.GetTimestamp();
        //Console.WriteLine("done");
        //Console.ReadLine();
        Console.WriteLine(Stopwatch.GetElapsedTime(pocetak, poslatiSvi));
        Console.WriteLine(Stopwatch.GetElapsedTime(pocetak,kraj));
    }

}