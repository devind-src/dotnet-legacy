using System.Collections.Generic;
using SyncNetApi.Dtos.Tools;

namespace SyncNetApi.Services.Tools
{
    public interface IToolsService
    {
        string DesEncrypt(string valueHex, string keyHex);
        string DesDecrypt(string valueHex, string keyHex);
        string CalculatePinBlock(string format, string pan, string pin, string keyHex);
        IReadOnlyList<IccTlvEntryDto> DecodeIccData(string hex);
        string EncryptCredential(string value, string configType);
        string DecryptCredential(string value, string configType);
    }
}
