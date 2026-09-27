using SyncNet.Common;
using SyncNet.Constants;
using SyncNet.Helpers;
using SyncNet.Logging;
using SyncNet.Models;
using SyncNet.Models.HsmDeviceModel;
using System;
using System.Threading.Tasks;


namespace SyncNet.Services
{
    public class HsmThales
    {
        private readonly CustomLogger _logger;
        private readonly ApiClient _apiClient;

        public HsmThales(ApiClient apiClient, CustomLogger logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }
        public async Task<GenerateZPKResponse> GenerateZPK(string zmk_under_lmk)
        {
            GenerateZPKResponse result = new();
            BaseResponse baseRsp;

            try
            {
                string keyScheme = ";XU1"; //ANSI X9.17

                //hsm request
                string hsm_request =
                    HsmCommand.GetHeader() +
                    HsmCommand.GenerateZPK +
                    zmk_under_lmk.AddKeyScheme() +
                    keyScheme;

                //send to device
                XTcpResponse hsm_response;
                if (AppConfig.HsmConnectionMode == ConnType.Persistent)
                    hsm_response = await _apiClient.SendAsync(hsm_request);
                else
                    hsm_response = await _apiClient.ConnectAndSend(hsm_request);

                //validate tcp request
                if (hsm_response.is_success == false)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.CustomError + hsm_response.error_message);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //extract hsm response
                HsmResponse hsmRsp = HsmCommand.GetResponse(hsm_response.resp_data);

                //validate hsm response
                if (hsmRsp.error_code != "00")
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.ResponseFailed.Replace("%RC%", hsmRsp.error_code));
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //success
                int keyLength = zmk_under_lmk.GetKeyLength();
                int idx = (keyLength == 16 ? 0 : 1);

                result.key_under_zmk = hsmRsp.data.Substring(idx, keyLength);
                result.key_under_lmk = hsmRsp.data.Substring(keyLength + 2, keyLength);
                result.key_check_value = hsmRsp.data.Substring((keyLength * 2) + 2, 6).PadRight(16, '0');

                //success
                baseRsp = ErrMessage.GetBaseResponse(ErrMessage.Success);
                result.resp_code = baseRsp.resp_code;
                result.resp_message = baseRsp.resp_message;
            }
            catch (Exception ex)
            {
                baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.GeneralError);
                result.resp_code = baseRsp.resp_code;
                result.resp_message = baseRsp.resp_message;

                _logger.Log(ex.Message);
            }

            return result;
        }
        public async Task<GenerateTPKResponse> GenerateTPK(string tmk_under_lmk)
        {
            GenerateTPKResponse result = new();
            BaseResponse baseRsp;

            try
            {
                string keyScheme = ";XU0"; //ANSI X9.17

                //hsm request
                string hsm_request =
                    HsmCommand.GetHeader() +
                    HsmCommand.GenerateTPK +
                    tmk_under_lmk.AddKeyScheme() +
                    keyScheme;

                //send to device
                XTcpResponse hsm_response;
                if (AppConfig.HsmConnectionMode == ConnType.Persistent)
                    hsm_response = await _apiClient.SendAsync(hsm_request);
                else
                    hsm_response = await _apiClient.ConnectAndSend(hsm_request);

                //validate
                if (hsm_response.is_success == false)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.CustomError + hsm_response.error_message);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //extract hsm response
                HsmResponse hsmRsp = HsmCommand.GetResponse(hsm_response.resp_data);

                //validate hsm response
                if (hsmRsp.error_code != "00")
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.ResponseFailed.Replace("%RC%", hsmRsp.error_code));
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //success
                int keyLength = tmk_under_lmk.GetKeyLength();
                int idx = (keyLength == 16 ? 0 : 1);

                result.key_under_tmk = hsmRsp.data.Substring(idx, keyLength);
                result.key_under_lmk = hsmRsp.data.Substring(keyLength + 2, keyLength);
                result.key_check_value = "0000000000000000"; //tdk ada kcv dari real hsm


                //generate key check value
                #region get key check value
                GenerateKCVRequest kcvRequest = new();
                kcvRequest.key_type = "02";
                kcvRequest.key_component = result.key_under_lmk.RemoveKeyScheme();

                //send to hsm
                GenerateKCVResponse kcvResponse = await GenerateKCV(kcvRequest);
                result.key_check_value = kcvResponse.key_check_value;
                #endregion


                //success
                BaseResponse b = ErrMessage.GetBaseResponse(ErrMessage.Success);
                result.resp_code = b.resp_code;
                result.resp_message = b.resp_message;
            }
            catch (Exception ex)
            {
                baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.GeneralError);
                result.resp_code = baseRsp.resp_code;
                result.resp_message = baseRsp.resp_message;

                _logger.Log(ex.Message);
            }

            return result;
        }
        public async Task<GenerateKCVResponse> GenerateKCV(GenerateKCVRequest req)
        {
            GenerateKCVResponse result = new();
            BaseResponse baseRsp;

            try
            {
                //0 single, 1 double, 2 triple
                string key_length = ((req.key_component.Length / 16) - 1).ToString();

                //hsm request
                string hsm_request =
                    HsmCommand.GetHeader() +
                    HsmCommand.GenerateKCV +
                    req.key_type +
                    key_length +
                    req.key_component.AddKeyScheme();

                //send to device
                XTcpResponse hsm_response;
                if (AppConfig.HsmConnectionMode == ConnType.Persistent)
                    hsm_response = await _apiClient.SendAsync(hsm_request);
                else
                    hsm_response = await _apiClient.ConnectAndSend(hsm_request);

                //validate
                if (hsm_response.is_success == false)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.CustomError + hsm_response.error_message);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //extract hsm response
                HsmResponse hsmRsp = HsmCommand.GetResponse(hsm_response.resp_data);

                //validate hsm response
                if (hsmRsp.error_code != "00")
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.ResponseFailed.Replace("%RC%", hsmRsp.error_code));
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //result key check value
                result.key_check_value = hsmRsp.data;

                //success
                BaseResponse b = ErrMessage.GetBaseResponse(ErrMessage.Success);
                result.resp_code = b.resp_code;
                result.resp_message = b.resp_message;
            }
            catch (Exception ex)
            {
                baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.GeneralError);
                result.resp_code = baseRsp.resp_code;
                result.resp_message = baseRsp.resp_message;

                _logger.Log(ex.Message);
            }

            return result;
        }
        public async Task<TranslateKeyResponse> TranslateKeyToLmk(TranslateKeyRequest req)
        {
            TranslateKeyResponse result = new();
            BaseResponse baseRsp;

            try
            {
                //validate zmk length
                int lenA = req.zmk_under_lmk.GetKeyLength();
                if (lenA != 16 && lenA != 32 && lenA != 48)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.BadRequest);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //validate zpk length
                int lenB = req.key_under_zmk.GetKeyLength();
                if (lenB != 16 && lenB != 32 && lenB != 48)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.BadRequest);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //hsm request
                string hsm_request =
                    HsmCommand.GetHeader() +
                    HsmCommand.TranslateKeyToLmk +
                    req.zmk_under_lmk +
                    req.key_under_zmk;

                //send to device
                XTcpResponse hsm_response;
                if (AppConfig.HsmConnectionMode == ConnType.Persistent)
                    hsm_response = await _apiClient.SendAsync(hsm_request);
                else
                    hsm_response = await _apiClient.ConnectAndSend(hsm_request);

                //validate
                if (hsm_response.is_success == false)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.CustomError + hsm_response.error_message);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //extract hsm response
                HsmResponse hsmRsp = HsmCommand.GetResponse(hsm_response.resp_data);

                //validate hsm response
                if (hsmRsp.error_code != "00")
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.ResponseFailed.Replace("%RC%", hsmRsp.error_code));
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                int keyLength = req.key_under_zmk.GetKeyLength();
                int idx = (lenA == 16 ? 0 : 1);

                //extract response
                result.key_under_lmk = hsmRsp.data.Substring(idx, lenA);
                result.key_check_value = hsmRsp.data.Substring(idx + lenA).PadRight(16, '0');

                //success
                BaseResponse b = ErrMessage.GetBaseResponse(ErrMessage.Success);
                result.resp_code = b.resp_code;
                result.resp_message = b.resp_message;
            }
            catch (Exception ex)
            {
                baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.GeneralError);
                result.resp_code = baseRsp.resp_code;
                result.resp_message = baseRsp.resp_message;

                _logger.Log(ex.Message);
            }

            return result;
        }
        public async Task<TranslatePinblockResponse> TranslatePinblock(TranslatePinblockRequest req)
        {
            TranslatePinblockResponse result = new();
            BaseResponse baseRsp;

            try
            {
                //validate zpk source length
                int lenA = req.key_under_lmk_source.GetKeyLength();
                if (lenA != 16 && lenA != 32 && lenA != 48)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.BadRequest);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //validate zpk dest length
                int lenB = req.key_under_lmk_dest.GetKeyLength();
                if (lenB != 16 && lenB != 32 && lenB != 48)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.BadRequest);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //validate source pinblock
                if (req.source_pinblock.Length != 16)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.BadRequest);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //hsm request
                string hsm_request =
                    HsmCommand.GetHeader() +
                    HsmCommand.TranslatePinblock +
                    req.key_under_lmk_source.AddKeyScheme() +
                    req.key_under_lmk_dest.AddKeyScheme() +
                    "12" + //max pin length 
                    req.source_pinblock +
                    "01" + //source pinblock format
                    "01" + //dest pinblock format
                   req.account_number.PadLeftZero(12);

                //send to device
                XTcpResponse hsm_response;
                if (AppConfig.HsmConnectionMode == ConnType.Persistent)
                    hsm_response = await _apiClient.SendAsync(hsm_request);
                else
                    hsm_response = await _apiClient.ConnectAndSend(hsm_request);

                //validate
                if (hsm_response.is_success == false)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.CustomError + hsm_response.error_message);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //extract hsm response
                HsmResponse hsmRsp = HsmCommand.GetResponse(hsm_response.resp_data);

                //validate hsm response
                if (hsmRsp.error_code != "00")
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.ResponseFailed.Replace("%RC%", hsmRsp.error_code));
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //extract data
                string pin_len = hsmRsp.data.Substring(0, 2);
                string dest_pinblock = hsmRsp.data.Substring(2, 16);
                string dest_pinblock_format = hsmRsp.data.Substring(18, 2);

                //translate pinblock
                result.dest_pinblock = dest_pinblock;

                //validate dest pinblock
                if (string.IsNullOrEmpty(result.dest_pinblock) == true)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.InvalidPinblock);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //success
                BaseResponse b = ErrMessage.GetBaseResponse(ErrMessage.Success);
                result.resp_code = b.resp_code;
                result.resp_message = b.resp_message;
            }
            catch (Exception ex)
            {
                baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.GeneralError);
                result.resp_code = baseRsp.resp_code;
                result.resp_message = baseRsp.resp_message;

                _logger.Log(ex.Message);
            }

            return result;
        }
        public async Task<TranslatePinblockResponse> TranslatePinblockTerminal(TranslatePinblockRequest req)
        {
            TranslatePinblockResponse result = new();
            BaseResponse baseRsp;

            try
            {
                //validate tpk source length
                int lenA = req.key_under_lmk_source.GetKeyLength();
                if (lenA != 16 && lenA != 32 && lenA != 48)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.BadRequest);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //validate zpk dest length
                int lenB = req.key_under_lmk_dest.GetKeyLength();
                if (lenB != 16 && lenB != 32 && lenB != 48)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.BadRequest);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //validate source pinblock
                if (req.source_pinblock.Length != 16)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.BadRequest);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //hsm request
                string hsm_request =
                    HsmCommand.GetHeader() +
                    HsmCommand.TranslatePinblockFromTPKToZPK +
                    req.key_under_lmk_source.AddKeyScheme() +
                    req.key_under_lmk_dest.AddKeyScheme() +
                    "12" + //max pin length 
                    req.source_pinblock +
                    "01" + //source pinblock format
                    "01" + //dest pinblock format
                    req.account_number.PadLeftZero(12);

                //send to device
                XTcpResponse hsm_response;
                if (AppConfig.HsmConnectionMode == ConnType.Persistent)
                    hsm_response = await _apiClient.SendAsync(hsm_request);
                else
                    hsm_response = await _apiClient.ConnectAndSend(hsm_request);

                //validate
                if (hsm_response.is_success == false)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.CustomError + hsm_response.error_message);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //extract hsm response
                HsmResponse hsmRsp = HsmCommand.GetResponse(hsm_response.resp_data);

                //validate hsm response
                if (hsmRsp.error_code != "00")
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.ResponseFailed.Replace("%RC%", hsmRsp.error_code));
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //extract data
                string pin_len = hsmRsp.data.Substring(0, 2);
                string dest_pinblock = hsmRsp.data.Substring(2, 16);
                string dest_pinblock_format = hsmRsp.data.Substring(18, 2);

                //translate key
                result.dest_pinblock = dest_pinblock;

                //validate dest pinblock
                if (string.IsNullOrEmpty(result.dest_pinblock) == true)
                {
                    baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.InvalidPinblock);
                    result.resp_code = baseRsp.resp_code;
                    result.resp_message = baseRsp.resp_message;

                    return result;
                }

                //success
                BaseResponse b = ErrMessage.GetBaseResponse(ErrMessage.Success);
                result.resp_code = b.resp_code;
                result.resp_message = b.resp_message;
            }
            catch (Exception ex)
            {
                baseRsp = DataHelper.GetBaseResponseObj(ErrMessage.GeneralError);
                result.resp_code = baseRsp.resp_code;
                result.resp_message = baseRsp.resp_message;

                _logger.Log(ex.Message);
            }

            return result;
        }
    }
}
