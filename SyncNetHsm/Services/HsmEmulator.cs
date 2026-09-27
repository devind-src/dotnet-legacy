using SyncNet.Common;
using SyncNet.Cryptography;
using SyncNet.Helpers;
using SyncNet.Library;
using SyncNet.Logging;
using SyncNet.Models;
using SyncNet.Models.HsmDeviceModel;
using System;

namespace SyncNet.Services
{
    public class HsmEmulator
    {
        public enum EnumKeyType
        {
            Clear,
            ZMK,
            ZPK,
            ZAK,
            ZEK,
            TMK,
            TPK,
            TAK
        }

        public enum EnumComponentType
        {
            Clear,
            Encrypted
        }


        private readonly CustomLogger _logger;

        private string LMK_1;
        private string LMK_2;

        public HsmEmulator(CustomLogger logger)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            LMK_1 = Resources.Keys.DES.HSM.LMK_1;
            LMK_2 = Resources.Keys.DES.HSM.LMK_2;
        }

        public FormKeyZMKResponse FormKeyZMK(string[] component, EnumComponentType component_type)
        {
            string key_xor;

            //decrypt component
            if (component_type == EnumComponentType.Encrypted)
            {
                for (int i = 0; i < component.Length; i++)
                {
                    component[i] = DesAlgorithm.DecryptHex(component[i], LMK_1);
                }
            }

            switch (component.Length)
            {
                case 1:
                    key_xor = component[0];
                    break;
                case 2:
                    key_xor = NbMath.DoOperand(component[0], component[1], NbMath.EnumOperand.XOR);
                    break;
                default:
                    key_xor = NbMath.DoOperand(component[0], component[1], NbMath.EnumOperand.XOR);

                    for (int i = 2; i < component.Length; i++)
                    {
                        key_xor = NbMath.DoOperand(key_xor, component[i], NbMath.EnumOperand.XOR);
                    }

                    break;
            }

            //encrypt under lmk & generate kcv
            FormKeyZMKResponse result = new()
            {
                zmk_under_lmk = DesAlgorithm.EncryptHex(key_xor, LMK_1),
                key_check_value = DesAlgorithm.EncryptHex("".PadLeft(16, '0'), key_xor)
            };

            //get 8 digit
            if (result.key_check_value.Length > 8)
                result.key_check_value = result.key_check_value.Substring(0, 8);

            //success
            BaseResponse b = DataHelper.GetBaseResponseObj(ErrMessage.Success);
            result.resp_code = b.resp_code;
            result.resp_message = b.resp_message;

            return result;
        }
        public FormKeyTMKResponse FormKeyTMK(string[] component, EnumComponentType component_type)
        {
            string key_xor;

            //decrypt component
            if (component_type == EnumComponentType.Encrypted)
            {
                for (int i = 0; i < component.Length; i++)
                {
                    component[i] = DesAlgorithm.DecryptHex(component[i], LMK_1);
                }
            }

            switch (component.Length)
            {
                case 1:
                    key_xor = component[0];
                    break;
                case 2:
                    key_xor = NbMath.DoOperand(component[0], component[1], NbMath.EnumOperand.XOR);
                    break;
                default:
                    key_xor = NbMath.DoOperand(component[0], component[1], NbMath.EnumOperand.XOR);

                    for (int i = 2; i < component.Length; i++)
                    {
                        key_xor = NbMath.DoOperand(key_xor, component[i], NbMath.EnumOperand.XOR);
                    }

                    break;
            }

            //encrypt under lmk & generate kcv
            FormKeyTMKResponse result = new()
            {
                tmk_under_lmk = DesAlgorithm.EncryptHex(key_xor, LMK_1),
                key_check_value = GenerateKCV(key_xor, EnumKeyType.Clear)
            };

            //success
            BaseResponse b = DataHelper.GetBaseResponseObj(ErrMessage.Success);
            result.resp_code = b.resp_code;
            result.resp_message = b.resp_message;

            return result;
        }
        public GenerateComponentResponse GenerateComponent(int key_length)
        {
            GenerateComponentResponse result = new GenerateComponentResponse();

            //generate random hex
            string clear_key = NbSystem.getRandomHexNumber(key_length);
            string key_check_value = GenerateKCV(clear_key, EnumKeyType.Clear);

            result.key_component = clear_key;
            result.key_check_value = key_check_value;

            //success
            BaseResponse b = DataHelper.GetBaseResponseObj(ErrMessage.Success);
            result.resp_code = b.resp_code;
            result.resp_message = b.resp_message;

            return result;
        }
        public GenerateZPKResponse GenerateZPK(string zmk_encrypted)
        {
            GenerateZPKResponse result = new GenerateZPKResponse();

            //generate random hex
            string clear_key = NbSystem.getRandomHexNumber(zmk_encrypted.Length);
            result.key_check_value = GenerateKCV(clear_key, EnumKeyType.Clear);

            //decrypt zmk
            string clear_zmk = DesAlgorithm.DecryptHex(zmk_encrypted, LMK_1);

            //encrypt under lmk/zmk
            result.key_under_lmk = DesAlgorithm.EncryptHex(clear_key, LMK_1);
            result.key_under_zmk = DesAlgorithm.EncryptHex(clear_key, clear_zmk);

            //success
            BaseResponse b = DataHelper.GetBaseResponseObj(ErrMessage.Success);
            result.resp_code = b.resp_code;
            result.resp_message = b.resp_message;

            return result;
        }
        public GenerateZAKResponse GenerateZAK(string zmk_encrypted)
        {
            GenerateZAKResponse result = new GenerateZAKResponse();

            //generate random hex
            string clear_key = NbSystem.getRandomHexNumber(zmk_encrypted.Length);
            result.key_check_value = GenerateKCV(clear_key, EnumKeyType.Clear);

            //decrypt zmk
            string clear_zmk = DesAlgorithm.DecryptHex(zmk_encrypted, LMK_1);

            //encrypt under lmk/zmk
            result.zak_under_lmk = DesAlgorithm.EncryptHex(clear_key, LMK_1);
            result.zak_under_zmk = DesAlgorithm.EncryptHex(clear_key, clear_zmk);

            //success
            BaseResponse b = DataHelper.GetBaseResponseObj(ErrMessage.Success);
            result.resp_code = b.resp_code;
            result.resp_message = b.resp_message;

            return result;
        }
        public GenerateZEKResponse GenerateZEK(string zmk_encrypted)
        {
            GenerateZEKResponse result = new GenerateZEKResponse();

            //generate random hex
            string clear_key = NbSystem.getRandomHexNumber(zmk_encrypted.Length);
            result.key_check_value = GenerateKCV(clear_key, EnumKeyType.Clear);

            //decrypt zmk
            string clear_zmk = DesAlgorithm.DecryptHex(zmk_encrypted, LMK_1);

            //encrypt under lmk/zmk
            result.zek_under_lmk = DesAlgorithm.EncryptHex(clear_key, LMK_1);
            result.zek_under_zmk = DesAlgorithm.EncryptHex(clear_key, clear_zmk);

            //success
            BaseResponse b = DataHelper.GetBaseResponseObj(ErrMessage.Success);
            result.resp_code = b.resp_code;
            result.resp_message = b.resp_message;

            return result;
        }
        public GenerateTPKResponse GenerateTPK(string tmk_encrypted)
        {
            GenerateTPKResponse result = new GenerateTPKResponse();

            //generate random hex
            string clear_key = NbSystem.getRandomHexNumber(tmk_encrypted.Length);
            result.key_check_value = GenerateKCV(clear_key, EnumKeyType.Clear);

            //decrypt tmk
            string clear_tmk = DesAlgorithm.DecryptHex(tmk_encrypted, LMK_1);

            //encrypt under lmk/zmk
            result.key_under_lmk = DesAlgorithm.EncryptHex(clear_key, LMK_1);
            result.key_under_tmk = DesAlgorithm.EncryptHex(clear_key, clear_tmk);

            //success
            BaseResponse b = DataHelper.GetBaseResponseObj(ErrMessage.Success);
            result.resp_code = b.resp_code;
            result.resp_message = b.resp_message;

            return result;
        }
        public GenerateTAKResponse GenerateTAK(string tmk_encrypted)
        {
            GenerateTAKResponse result = new GenerateTAKResponse();

            //generate random hex
            string clear_key = NbSystem.getRandomHexNumber(tmk_encrypted.Length);
            result.key_check_value = GenerateKCV(clear_key, EnumKeyType.Clear);

            //decrypt tmk
            string clear_tmk = DesAlgorithm.DecryptHex(tmk_encrypted, LMK_1);

            //encrypt under lmk/zmk
            result.tak_under_lmk = DesAlgorithm.EncryptHex(clear_key, LMK_1);
            result.tak_under_zmk = DesAlgorithm.EncryptHex(clear_key, clear_tmk);

            //success
            BaseResponse b = DataHelper.GetBaseResponseObj(ErrMessage.Success);
            result.resp_code = b.resp_code;
            result.resp_message = b.resp_message;

            return result;
        }
        public GenerateMACResponse GenerateMAC(string key_encrypted, string base64)
        {
            //decrypt key
            string clear_key = DesAlgorithm.DecryptHex(key_encrypted, LMK_1);
            byte[] data = Convert.FromBase64String(base64);

            //calculate mac
            string mac = MacGenerator.CalculateMAC(ConvertHelper.BytesToHex(data), clear_key,
                MacGenerator.PaddingMethod.Method2);

            GenerateMACResponse result = new();
            result.mac = mac;

            //success
            BaseResponse b = DataHelper.GetBaseResponseObj(ErrMessage.Success);
            result.resp_code = b.resp_code;
            result.resp_message = b.resp_message;

            return result;
        }
        public string GenerateKCV(string key, EnumKeyType keyType)
        {
            string kcv = "000000";
            string clear_key;

            switch (keyType)
            {
                case EnumKeyType.Clear:
                    kcv = DesAlgorithm.EncryptHex("".PadLeft(16, '0'), key);
                    break;

                case EnumKeyType.ZMK:
                case EnumKeyType.ZPK:
                case EnumKeyType.ZAK:
                case EnumKeyType.ZEK:
                    clear_key = DesAlgorithm.DecryptHex(key, LMK_1);
                    kcv = DesAlgorithm.EncryptHex("".PadLeft(16, '0'), clear_key);
                    break;

                case EnumKeyType.TMK:
                case EnumKeyType.TPK:
                case EnumKeyType.TAK:
                    clear_key = DesAlgorithm.DecryptHex(key, LMK_1);
                    kcv = DesAlgorithm.EncryptHex("".PadLeft(16, '0'), clear_key);
                    break;

                default:
                    kcv = "000000";
                    break;
            }

            //get 6 digit
            if (kcv.Length > 6) kcv = kcv.Substring(0, 6);

            return kcv;
        }
        public TranslatePinblockResponse TranslatePinblock(TranslatePinblockRequest req)
        {
            TranslatePinblockResponse result = new();

            //clear zpk source
            string clear_zpk_source = DesAlgorithm.DecryptHex(req.key_under_lmk_source, LMK_1);

            //clear zpk dest
            string clear_zpk_dest = DesAlgorithm.DecryptHex(req.key_under_lmk_dest, LMK_1);

            //decrypt pinblock
            string clear_pinblock = DesAlgorithm.DecryptHex(req.source_pinblock, clear_zpk_source);

            //validate pinblock
            if (string.IsNullOrEmpty(clear_pinblock) == true || clear_pinblock.Length != 16) return result;

            //validate pin length
            int pin_length = clear_pinblock.Substring(0, 2).ToNumber();
            if (pin_length != 4 && pin_length != 6) return result;

            //encrypt pinblock
            result.dest_pinblock = DesAlgorithm.EncryptHex(clear_pinblock, clear_zpk_dest);

            //success
            BaseResponse b = DataHelper.GetBaseResponseObj(ErrMessage.Success);
            result.resp_code = b.resp_code;
            result.resp_message = b.resp_message;

            return result;
        }
        public TranslatePinblockResponse TranslatePinblockTerminal(TranslatePinblockRequest req)
        {
            TranslatePinblockResponse result = new();

            //clear tpk source
            string clear_tpk_source = DesAlgorithm.DecryptHex(req.key_under_lmk_source, LMK_1);

            //clear zpk dest
            string clear_zpk_dest = DesAlgorithm.DecryptHex(req.key_under_lmk_dest, LMK_1);

            //decrypt pinblock
            string clear_pinblock = DesAlgorithm.DecryptHex(req.source_pinblock, clear_tpk_source);

            //validate pinblock
            if (string.IsNullOrEmpty(clear_pinblock) == true || clear_pinblock.Length != 16) return result;

            //validate pin length
            int pin_length = clear_pinblock.Substring(0, 2).ToNumber();
            if (pin_length != 4 && pin_length != 6) return result;

            //encrypt pinblock
            result.dest_pinblock = DesAlgorithm.EncryptHex(clear_pinblock, clear_zpk_dest);

            //success
            BaseResponse b = DataHelper.GetBaseResponseObj(ErrMessage.Success);
            result.resp_code = b.resp_code;
            result.resp_message = b.resp_message;

            return result;
        }
        public TranslateKeyResponse TranslateKeyToLmk(TranslateKeyRequest req)
        {
            TranslateKeyResponse result = new TranslateKeyResponse();

            //clear zmk
            string clear_zmk = DesAlgorithm.DecryptHex(req.zmk_under_lmk, LMK_1);

            //clear zpk
            string clear_zpk = DesAlgorithm.DecryptHex(req.key_under_zmk, clear_zmk);

            //encrypt under zmk
            result.key_under_lmk = DesAlgorithm.EncryptHex(clear_zpk, LMK_1);
            result.key_check_value = GenerateKCV(clear_zpk, EnumKeyType.Clear);

            //success
            BaseResponse b = DataHelper.GetBaseResponseObj(ErrMessage.Success);
            result.resp_code = b.resp_code;
            result.resp_message = b.resp_message;

            return result;
        }
    }
}
