using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace GIFserver
{
    public class HTTPServers
    {
        //static ManualResetEvent requestValid = new ManualResetEvent(false);
        private object fileWriteLock = new object();
        private short Port;
        private HttpListener listener;
        private string address;
        private Cache cache;
        private Index index;

        volatile private int numOfRequests;
        volatile private int numBadRequests;
        volatile private int numNotFoundReq;
        volatile private int numFoundInCache;
        volatile private int numFoundByIndex;
        volatile private int numFoundInDirectory;

        private string loggFileName = "Loggs.txt";
        private string serverRoot = "../../../GIFs";
        //private string serverRoot= @"D:\";
        public HTTPServers(string address, short port = 5050)
        {
            this.Port = port;
            this.address = address;
            this.listener = new HttpListener();
            this.listener.Prefixes.Add("http://" + address + ":" + this.Port.ToString() + "/");
            this.cache = new Cache(5);
            this.index = new Index(100);

            numOfRequests = 0;
            numBadRequests = 0;
            numNotFoundReq = 0;
            numFoundInCache = 0;
            numFoundByIndex = 0;
            numFoundInDirectory = 0;
        }


        public  void Start(System.Threading.CancellationToken token)
        {
            this.listener.Start();
            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                Interlocked.Increment(ref numOfRequests);
                //ThreadPool.QueueUserWorkItem(HandleRequest, context);
                Task t1 = HandleRequest(context);

                if (token.IsCancellationRequested)
                {
                    break;
                }
            }
            Console.WriteLine("Server not listening anymore");
            Report();
        }
        private async Task HandleRequest(object? objContext)
        {
            HttpListenerContext context=(HttpListenerContext)objContext;
            Verify(context);
        }

        private void Verify(HttpListenerContext context)
        {
            string gifName=string.Empty;
            bool valid = true;
            if (context.Request.HttpMethod != "GET") { valid = false;  }

            if (context.Request.AcceptTypes != null)
            {
                bool validPom = false;
                foreach(var type in context.Request.AcceptTypes)
                {
                    if (type.Contains("image/gif") || type.Contains("*/*"))
                    {
                        validPom = true;
                        break;
                    }
                }
                valid = valid && validPom;
            }
            else
            {
                valid = false;
            }

            if (context.Request.RawUrl == null || context.Request.RawUrl == "/") 
            {
                valid = false;
            }
            else
            {
                gifName = context.Request.RawUrl;
                gifName=gifName.Remove(0, 1);
                if (gifName.EndsWith(".gif"))
                {
                   gifName= gifName.Substring(0, gifName.Length - 4);
                }
            }

            if (valid)
            {
                LookForGif(gifName,context);
            }
            else
            {
                //ThreadPool.QueueUserWorkItem(Loggs,new object[] {false,false,false,context.Request});
                Task t1=Loggs(new object[] {false,false,false,false,context.Request});
                Response(400,context);
                Interlocked.Increment(ref numBadRequests);
            }
            

        }


        private void LookForGif(string gifName, HttpListenerContext context)
        {
            byte[] gif = new byte[0];
            if (cache.Find(gifName, ref gif))
            {
                Interlocked.Increment(ref this.numFoundInCache);
                //ThreadPool.QueueUserWorkItem(Loggs, new object[] { true, true, true, context.Request });
                Task t1 = Loggs(new object[] { true, true, true,false, context.Request });
                Response(200, context, gif.Length, gif);
                return;
            }


            string? filePath=LookIndex(gifName);
            if (filePath != null)
            {
                Interlocked.Increment(ref this.numFoundByIndex);
                Interlocked.Increment(ref this.numFoundInDirectory);
                gif = File.ReadAllBytes(filePath);
                Task t2 = Loggs(new object[] { true, true, false,false, context.Request });
                Task t3 = Task.Run(() => cache.Add(new object[] { gifName, gif }));
                Response(200, context, gif.Length, gif);
                return;
            }


            filePath = LookDirectory(gifName, this.serverRoot);

            if (filePath == null)
            {
                Interlocked.Increment(ref this.numNotFoundReq);
                //ThreadPool.QueueUserWorkItem(Loggs, new object[] { true, false, false, context.Request });
                Task t4 = Loggs(new object[] { true, false, false,false, context.Request });
                Response(404, context);
                return;
            }
            Interlocked.Increment(ref this.numFoundInDirectory);
            gif = File.ReadAllBytes(filePath);
            //ThreadPool.QueueUserWorkItem(Loggs, new object[] { true, true, false, context.Request });
            Task t5 = Loggs(new object[] { true, true, false,false, context.Request });
            //ThreadPool.QueueUserWorkItem(cache.Add,new object[] {gifName,gif});
            Task t6 = Task.Run(() => cache.Add(new object[] { gifName, gif }));
            Task t7= Task.Run(()=> index.Add(gifName,filePath));
            Response(200, context, gif.Length, gif);
        }
        private string? LookIndex(string gifName)
        {
           return this.index.Find(gifName);
        }
        private string? LookDirectory(string gifName,string rootDir)
        {
            string? result = null;
            //string? filePath = Directory.GetFiles(serverRoot, gifName + ".gif", SearchOption.AllDirectories).FirstOrDefault();
            foreach (string f in Directory.EnumerateFiles(rootDir))
            {
                if (f.EndsWith(gifName + ".gif"))
                {
                    result = f;
                    break;
                }
            }
            if (result != null)
                return result;
            foreach (string d in Directory.EnumerateDirectories(rootDir))
            {
                result= LookDirectory(gifName, d);
                if(result!=null) return result;
            }
            return null;

        }

        private async Task Loggs(object? instance)
        {
            string text = String.Empty;
            object[] objects=instance as object[];
            bool valid = (bool)objects[0];
            bool found = (bool)objects[1];
            bool inCache = (bool)objects[2];
            bool inIndex = (bool)objects[3];
            HttpListenerRequest request = (HttpListenerRequest)objects[4];
            if(!valid)
            {
                text = @"--Invalid request received at " + DateTime.Now.ToString() + "\n";
            }else
            if (valid && !found)
            {
                text=@"--Valid request but gif not found at " + DateTime.Now.ToString() + "\n";
            }else
            if(valid&&found&&!inCache&&!inIndex)
            {
                text=@"--Valid request gif found in directory at " + DateTime.Now.ToString() + "\n";
            }else
            if (valid&&found&&!inCache&&inIndex)
            {
                text=@"--Valid request gif found with index at "+DateTime.Now.ToString()+"\n";
            }else
            if(valid&&found&&inCache)
            {
                text=@"--Valid request gif found in cache at " + DateTime.Now.ToString() + "\n";
            }
            else
            {
                text = @"--Unkown error at" + DateTime.Now.ToString() + "\n"; 
            }
            text += $"Request:{request.HttpMethod} {request.RawUrl} {request.ProtocolVersion}\n";
            text += "Accept: ";
            if (request.AcceptTypes != null)
            {
                foreach (var types in request.AcceptTypes)
                {
                    text += types + " ";
                }
            }
            text += "\n\n";


            lock (fileWriteLock)
            WriteFile(text);
        }

        private void WriteFile(string text)
        {
            if (!File.Exists(this.loggFileName))
            {
                File.Create(this.loggFileName);
                TextWriter tw=new StreamWriter(this.loggFileName);
                tw.Write(text);
                tw.Close();
            }
            else if(File.Exists(this.loggFileName)) 
            {
                TextWriter tw = new StreamWriter(this.loggFileName,true);
                tw.Write(text);
                tw.Close();
            }
        }
        private void Response(int responseCode,HttpListenerContext context,int len=0, byte[] content=null)
        {
            string body = string.Empty;
            var response = context.Response;
            response.StatusCode = responseCode;
            
            if(responseCode == 400)
            {
                response.ContentType = "text/html";
                body = @"<html>
                    <head><title>Bad Request</title></head>
                    <body>Bad request was made</body>
                                </html>";
                try
                {
                    response.OutputStream.Write(Encoding.ASCII.GetBytes(body));
                    response.Close();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                    return;
                }
                return;
            }
            else
            if (responseCode == 404)
            {
                response.ContentType = "text/html";
                body = @"<html>
                    <head><title>Not Found</title></head>
                    <body>Gif Not Found</body>
                                </html>";
                try
                {
                    response.OutputStream.Write(Encoding.ASCII.GetBytes(body));
                    response.Close();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                    return;
                }
                return;
            }
            else
            if (responseCode == 200)
            {
                response.ContentType = "image/gif";
                response.ContentLength64 = len;
                try
                {
                    response.OutputStream.Write(content);
                    response.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                    return;
                }
                return;
            }
           
        }
      
        public void Report()
        {
            Console.WriteLine($"{this.numOfRequests} requests were received");
            Console.WriteLine($"{this.numBadRequests} were bad requests");
            Console.WriteLine($"{this.numNotFoundReq} were not found");
            Console.WriteLine($"Of {this.numOfRequests - this.numBadRequests - this.numNotFoundReq} found requests, {this.numFoundInCache} were found in cache and {this.numFoundInDirectory} were found in directorty");
            Console.WriteLine(@$"Of {this.numOfRequests - this.numBadRequests - this.numNotFoundReq} found requests, {this.numFoundByIndex} were found using index structure and {this.numOfRequests - this.numBadRequests - this.numNotFoundReq - this.numFoundByIndex-this.numFoundInCache}
had to be searched for");
        }


      
    }
}
