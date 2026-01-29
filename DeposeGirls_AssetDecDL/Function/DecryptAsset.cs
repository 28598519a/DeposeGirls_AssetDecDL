
using System.Text;

public static class DecryptAsset
{
    /// <summary>
    /// RC4 implement
    /// </summary>
    public static class ARC4
    {
        public static byte[] Eecrypt(byte[] data, byte[] key)
        {
            return Decrypt(data, key);
        }

        public static byte[] Decrypt(byte[] data, byte[] key)
        {
            byte[] S = new byte[256];
            for (int i = 0; i < 256; i++)
                S[i] = (byte)i;

            int j = 0;
            for (int i = 0; i < 256; i++)
            {
                j = (j + S[i] + key[i % key.Length]) & 0xFF;
                Swap(S, i, j);
            }

            byte[] result = new byte[data.Length];
            int iIndex = 0, jIndex = 0;
            for (int k = 0; k < data.Length; k++)
            {
                iIndex = (iIndex + 1) & 0xFF;
                jIndex = (jIndex + S[iIndex]) & 0xFF;
                Swap(S, iIndex, jIndex);
                byte t = S[(S[iIndex] + S[jIndex]) & 0xFF];
                result[k] = (byte)(data[k] ^ t);
            }

            return result;
        }

        private static void Swap(byte[] array, int i, int j)
        {
            byte temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }

    public static bool CheckSign(byte[] data)
    {
        string signed = "8Jdn";
        long data_size = data.Length;

        if (data_size > signed.Length)
        {
            byte[] tmp = new byte[signed.Length];
            Array.Copy(data, tmp, signed.Length);
            if (Encoding.UTF8.GetString(tmp) == signed)
                return true;
        }
        return false;
    }

    public static byte[]? Jdn_Decrypt(byte[] data)
    {
        // 檢查前 4 個 byte 是否為 b'8Jdn'
        if (data.Length < 9 || !CheckSign(data))
            return null;

        // 從第 5 個 byte 開始讀取 size（4 bytes, little endian）
        int size = BitConverter.ToInt32(data, 5); // little-endian

        if (data.Length < 9 + size)
            return null;

        // 取出要解密的部分 (僅檔案開頭被加密)
        byte[] encrypted = new byte[size];
        Array.Copy(data, 9, encrypted, 0, size);

        // 從 9 + size 開始剩下的部分
        byte[] sourceData = new byte[data.Length - (9 + size)];
        Array.Copy(data, 9 + size, sourceData, 0, sourceData.Length);

        // RC4 key
        byte[] key = new byte[]
        {
            0xEB,0x45,0x8B,0xA5,0x2B,0x05,0xCB,0x65,
            0x6B,0xC5,0x0B,0x25,0xAB,0x85,0x4B,0xE5
        };

        byte[] decrypted = ARC4.Decrypt(encrypted, key);

        // decrypted + sourcedata
        byte[] result = new byte[decrypted.Length + sourceData.Length];
        Array.Copy(decrypted, 0, result, 0, decrypted.Length);
        Array.Copy(sourceData, 0, result, decrypted.Length, sourceData.Length);

        return result;
    }
}
