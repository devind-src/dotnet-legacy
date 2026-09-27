using Microsoft.AspNetCore.Http;
using SyncNet.Message;
using System.Net;
using System.Threading.Tasks;
using static SyncNet.AppProcessor;

namespace SyncNet
{
    public interface IAppProcessor
    {
        //internal
        Task ProcessMsgFromSinkNode(string NodeName, Request MsgRequest);
        Task ProcessMsgFromSourceNode(string NodeName, string ConnectionName, Response MsgResponse);

        //external
        Task ProcessMsgFromRemoteTcp(string NodeName, string ConnectionName, byte[] Bytes, int TotalBytes, EndPoint RemoteEP);
        Task ProcessMsgFromRemoteWsServer(string NodeName, string ConnectionName, string WSMessage, HttpContext Ctx);
        Task ProcessMsgFromRemoteWsClient(string NodeName, string ConnectionName, Request MsgOriginal, string WSMessage, HttpStatusCode StatusCode);
        Task OnError(string NodeName, string ConnectionName, Request MsgOriginal, string ErrorMessage);

        //timer
        Task TimerAutoSignon(string NodeName);
        Task TimerEcho(string NodeName);
        Task TimerKeyExchange(string NodeName);

        //command
        Task Resync();
        Task NetworkManagement(EnumNtwrkMgmt cmd, string NodeName, string Param);
    }
}
