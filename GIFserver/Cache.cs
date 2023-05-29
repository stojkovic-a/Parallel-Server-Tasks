using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace GIFserver
{
    public class Cache
    {
        static ReaderWriterLockSlim rws=new ReaderWriterLockSlim();
        private Dictionary<string, Byte[]> cache;
        private int capacity;
        private string[] FIFO;
        private int first,current;

        public Cache(int cap=5) {
            this.capacity = cap;
            this.cache= new Dictionary<string, Byte[]>();
            this.FIFO = new string[capacity];
            this.first = 0;
            this.current = 0;
        }

        public void Add(object? NameAndGif)
        {
            object[] objects = NameAndGif as object[];
            string key = (string)objects[0];
            byte[] value = (byte[])objects[1];


            if (cache.Count == this.capacity)
            {
                Remove(FIFO[first]);
                first = (++first) % this.capacity;
              
            }
            rws.EnterWriteLock();
            this.cache.TryAdd(key, value);
            FIFO[current] = key;
            current=((++current)%this.capacity);
            rws.ExitWriteLock();
        }

        public void Remove(string key)
        {
            rws.EnterWriteLock();
            this.cache.Remove(key);
            rws.ExitWriteLock();

        }

        public bool Find(string key,  ref byte[] gif)
        {
            bool found = false;
            rws.EnterReadLock();
            found= cache.TryGetValue(key, out gif);
            rws.ExitReadLock();
            return found;
        }
    }
}
