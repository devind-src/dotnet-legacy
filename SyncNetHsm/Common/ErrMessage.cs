using SyncNet.Models;

namespace SyncNet.Common
{
    public class ErrMessage
    {
        public const string Success = "00:Success";
        public const string BadRequest = "01:Bad Request";
        public const string GeneralError = "02:General error";
        public const string InvalidPinblock = "03:Invalid pinblock";
        public const string InterfaceNotFound = "04:Interface not found";
        public const string TerminalNotFound = "05:Terminal not found";
        public const string RequestHsmError = "06:Request to HSM failed";
        public const string KeyCheckValueError = "07:Translate key failed, kcv does not match";

        public const string ResponseFailed = "95:Response error RC %RC% from device";
        public const string TerminalNotFoundCustom = "96:Terminal %TID% not found";
        public const string InterfaceNotFoundCustom = "97:Interface %NODE% not found";

        public const string CustomError = "99:";
        public static BaseResponse GetBaseResponse(string msg)
        {
            BaseResponse ret = new BaseResponse();

            try
            {
                string[] arr = msg.Split(':');

                ret.resp_code = arr[0];
                ret.resp_message = arr[1];
            }
            catch { }

            return ret;
        }
    }
}
