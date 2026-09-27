using ApiChannel.Common;
using Microsoft.AspNetCore.Http;
using SyncNet.Cryptography;
using System;

namespace ApiChannel.Library
{
    internal class AuthManager
    {
        public static bool IsCredenValid(HttpContext ctx, string json)
        {
            bool bval = false;

            try
            {
                //header
                string client_id = ctx.Request.Headers["X-client-id"];
                string timestamp = ctx.Request.Headers["X-timestamp"];
                string signature = ctx.Request.Headers["X-signature"];

                //validate client id
                if (client_id != ApiConfig.ClientID) return bval;

                //construct string
                string rawurl = ctx.Request.Path;
                string strtosign = $"{rawurl}:{client_id}:{timestamp}:{json}";

                //calculate hash
                byte[] bytes = HashProvider.ComputeHMACSHA512Hash(strtosign, ApiConfig.SecretKey);
                string hash = Convert.ToBase64String(bytes);

                //compare signature
                bval = signature.Equals(hash);
            }
            catch (Exception ex)
            {
                throw new Exception("Error validating credentials", ex);
            }

            return bval;
        }
    }
}
