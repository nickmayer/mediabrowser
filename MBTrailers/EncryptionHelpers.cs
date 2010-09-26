using System;
using System.Text;
using System.Security.Cryptography;

namespace WebProxy {
    public static class EncryptionHelpers {

        static MD5CryptoServiceProvider md5Provider = new MD5CryptoServiceProvider();

        public static string GetMD5String(this string str) {
            lock (md5Provider) {
                return (new Guid(md5Provider.ComputeHash(Encoding.Unicode.GetBytes(str))).ToString("N"));
            }
        }
    }
}
