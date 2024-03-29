﻿using Chaos.NaCl;
using System;
using System.IO;

namespace Suinet.Wallet
{
    public class Ed25519KeyPair : IKeyPair
    {
        public string PublicKeyBase64 { get; private set; }
        public string PrivateKeyBase64 { get; private set; }
        public string PublicKeyAsSuiAddress { get; private set; }

        public byte[] PublicKey { get; private set; }
        public byte[] PrivateKey { get; private set; }

        public Ed25519KeyPair(byte[] publicKey, byte[] privateKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;

            PublicKeyBase64 = CryptoBytes.ToBase64String(publicKey);
            PrivateKeyBase64 = CryptoBytes.ToBase64String(privateKey);

            PublicKeyAsSuiAddress = ToSuiAddress(publicKey);
        }

        public string ToSuiAddress(byte[] publicKeyBytes)
        {
            var hashAlgorithm = new Org.BouncyCastle.Crypto.Digests.Sha3Digest(256);
            var addressBytes = new byte[publicKeyBytes.Length + 1];
            addressBytes[0] = 0x00;

            Array.Copy(publicKeyBytes, 0, addressBytes, 1, publicKeyBytes.Length);
            hashAlgorithm.BlockUpdate(addressBytes, 0, addressBytes.Length);

            byte[] result = new byte[64];
            hashAlgorithm.DoFinal(result, 0);

            string hashString = BitConverter.ToString(result);
            hashString = hashString.Replace("-", "").ToLowerInvariant();
            return "0x" + hashString.Substring(0, 40);
        }

        public string Sign(string base64message)
        {
            return CryptoBytes.ToBase64String(Sign(CryptoBytes.FromBase64String(base64message)));
        }


        public byte[] Sign(byte[] message)
        {
            var signature = new byte[64];

            // See: sui/crates/sui-types/src/intent.rs 
            // This is currently hardcoded with [IntentScope::TransactionData = 0, Version::V0 = 0, AppId::Sui = 0]
            var INTENT_BYTES = new byte[] { 0, 0, 0 };

            var messageWithIntent = new byte[INTENT_BYTES.Length + message.Length];
            Buffer.BlockCopy(INTENT_BYTES, 0, messageWithIntent, 0, INTENT_BYTES.Length);
            Buffer.BlockCopy(message, 0, messageWithIntent, INTENT_BYTES.Length, message.Length);

            Ed25519.Sign(new ArraySegment<byte>(signature), new ArraySegment<byte>(messageWithIntent), new ArraySegment<byte>(PrivateKey));
            return signature;
        }
    }

}
