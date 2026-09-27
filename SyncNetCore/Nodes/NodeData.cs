using System;
using System.Net;

namespace SyncNet.Nodes
{
    public class NodeData : IDisposable
    {
        public SyncNet.Message.Request MsgRequest;
        public EndPoint ep;
        public DateTime TimeStamp;
        public int MaxExpired;

        public NodeData()
        {
            this.TimeStamp = DateTime.Now;
            this.MaxExpired = 60 * 5;//default 5 minutes

            MsgRequest = new SyncNet.Message.Request();
        }

        public void Dispose()
        {
            MsgRequest = null;
            ep = null;
        }

        public bool IsExpired()
        {
            bool ret = false;
            TimeSpan d = DateTime.Now.Subtract(this.TimeStamp);

            if (d.TotalSeconds >= this.MaxExpired)
                ret = true;

            return ret;
        }
    }
}
