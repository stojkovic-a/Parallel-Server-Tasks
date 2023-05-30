using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIFserver
{
    public class Index
    {
        static ReaderWriterLockSlim rws=new ReaderWriterLockSlim();
        private Dictionary<string, string> nameToPath;
        private int capacity;

        public Index(int cap)
        {
            this.capacity= cap;
            this.nameToPath= new Dictionary<string, string>();
        }


        public void Add(string name,string path)
        {
            if (nameToPath.Count != this.capacity)
            {
                rws.EnterWriteLock();
                this.nameToPath.TryAdd(name, path);
                rws.ExitWriteLock();
            }
        }

        public string? Find(string name)
        {
            string? path;
            rws.EnterReadLock();
            nameToPath.TryGetValue(name, out path);
            rws.ExitReadLock();
            return path;
        }
    }

}
