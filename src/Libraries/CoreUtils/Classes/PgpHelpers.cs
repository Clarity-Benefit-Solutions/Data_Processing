using System;
using System.IO;
using System.Linq;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO;

/*
 * 
 * 
 * PGPEncryptDecrypt.EncryptFile(inputFileName, 
               outputFileName,
               recipientKeyFileName,
               shouldArmor,
               shouldCheckIntegrity);
Decrypt a file:



 * */
namespace CoreUtils.Classes
{

    // BouncyCastle Code copied from https://stackoverflow.com/questions/10209291/pgp-encrypt-and-decrypt

    public static class PGPEncryptDecrypt
    {
        private const int BufferSize = 0x10000; // should always be power of 2

        #region Encrypt

        /*
         * Encrypt the file.
         */

        public static void EncryptFile(string inputFile, string outputFile, string publicKeyFile, bool armor,
            bool withIntegrityCheck)
        {
            using (Stream publicKeyStream = File.OpenRead(publicKeyFile))
            {
                var encKey = ReadPublicKey(publicKeyStream);

                using (var bOut = new MemoryStream())
                {
                    var comData = new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip);
                    PgpUtilities.WriteFileToLiteralData(comData.Open(bOut), PgpLiteralData.Binary,
                        new FileInfo(inputFile));

                    comData.Close();
                    var cPk = new PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.Cast5, withIntegrityCheck,
                        new SecureRandom());

                    cPk.AddMethod(encKey);
                    var bytes = bOut.ToArray();

                    using (Stream outputStream = File.Create(outputFile))
                    {
                        if (armor)
                            using (var armoredStream = new ArmoredOutputStream(outputStream))
                            {
                                using (var cOut = cPk.Open(armoredStream, bytes.Length))
                                {
                                    cOut.Write(bytes, 0, bytes.Length);
                                }
                            }
                        else
                            using (var cOut = cPk.Open(outputStream, bytes.Length))
                            {
                                cOut.Write(bytes, 0, bytes.Length);
                            }
                    }
                }
            }
        }

        #endregion Encrypt

        #region Encrypt and Sign

        /*
         * Encrypt and sign the file pointed to by unencryptedFileInfo and
         */

        public static void EncryptAndSign(string inputFile, string outputFile, string publicKeyFile,
            string privateKeyFile, string passPhrase, bool armor)
        {
            var encryptionKeys = new PgpEncryptionKeys(publicKeyFile, privateKeyFile, passPhrase);

            if (!File.Exists(inputFile))
                throw new FileNotFoundException(string.Format("Input file [{0}] does not exist.", inputFile));

            if (!File.Exists(publicKeyFile))
                throw new FileNotFoundException(string.Format("Public Key file [{0}] does not exist.", publicKeyFile));

            if (!File.Exists(privateKeyFile))
                throw new FileNotFoundException(string.Format("Private Key file [{0}] does not exist.",
                    privateKeyFile));

            if (string.IsNullOrEmpty(passPhrase))
                throw new ArgumentNullException("Invalid Pass Phrase.");

            if (encryptionKeys == null)
                throw new ArgumentNullException("Encryption Key not found.");

            using (Stream outputStream = File.Create(outputFile))
            {
                if (armor)
                    using (var armoredOutputStream = new ArmoredOutputStream(outputStream))
                    {
                        OutputEncrypted(inputFile, armoredOutputStream, encryptionKeys);
                    }
                else
                    OutputEncrypted(inputFile, outputStream, encryptionKeys);
            }
        }

        private static void OutputEncrypted(string inputFile, Stream outputStream, PgpEncryptionKeys encryptionKeys)
        {
            using (var encryptedOut = ChainEncryptedOut(outputStream, encryptionKeys))
            {
                var unencryptedFileInfo = new FileInfo(inputFile);
                using (var compressedOut = ChainCompressedOut(encryptedOut))
                {
                    var signatureGenerator = InitSignatureGenerator(compressedOut, encryptionKeys);
                    using (var literalOut = ChainLiteralOut(compressedOut, unencryptedFileInfo))
                    {
                        using (var inputFileStream = unencryptedFileInfo.OpenRead())
                        {
                            WriteOutputAndSign(compressedOut, literalOut, inputFileStream, signatureGenerator);
                            inputFileStream.Close();
                        }
                    }
                }
            }
        }

        private static void WriteOutputAndSign(Stream compressedOut, Stream literalOut, FileStream inputFile,
            PgpSignatureGenerator signatureGenerator)
        {
            var length = 0;
            var buf = new byte[BufferSize];
            while ((length = inputFile.Read(buf, 0, buf.Length)) > 0)
            {
                literalOut.Write(buf, 0, length);
                signatureGenerator.Update(buf, 0, length);
            }

            signatureGenerator.Generate().Encode(compressedOut);
        }

        private static Stream ChainEncryptedOut(Stream outputStream, PgpEncryptionKeys m_encryptionKeys)
        {
            PgpEncryptedDataGenerator encryptedDataGenerator;
            encryptedDataGenerator =
                new PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.TripleDes, new SecureRandom());
            encryptedDataGenerator.AddMethod(m_encryptionKeys.PublicKey);
            return encryptedDataGenerator.Open(outputStream, new byte[BufferSize]);
        }

        private static Stream ChainCompressedOut(Stream encryptedOut)
        {
            var compressedDataGenerator = new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip);
            return compressedDataGenerator.Open(encryptedOut);
        }

        private static Stream ChainLiteralOut(Stream compressedOut, FileInfo file)
        {
            var pgpLiteralDataGenerator = new PgpLiteralDataGenerator();
            return pgpLiteralDataGenerator.Open(compressedOut, PgpLiteralData.Binary, file);
        }

        private static PgpSignatureGenerator InitSignatureGenerator(Stream compressedOut,
            PgpEncryptionKeys m_encryptionKeys)
        {
            const bool IsCritical = false;
            const bool IsNested = false;
            var tag = m_encryptionKeys.SecretKey.PublicKey.Algorithm;
            var pgpSignatureGenerator = new PgpSignatureGenerator(tag, HashAlgorithmTag.Sha1);
            pgpSignatureGenerator.InitSign(PgpSignature.BinaryDocument, m_encryptionKeys.PrivateKey);
            foreach (string userId in m_encryptionKeys.SecretKey.PublicKey.GetUserIds())
            {
                var subPacketGenerator = new PgpSignatureSubpacketGenerator();
                subPacketGenerator.SetSignerUserId(IsCritical, userId);
                pgpSignatureGenerator.SetHashedSubpackets(subPacketGenerator.Generate());
                // Just the first one!
                break;
            }

            pgpSignatureGenerator.GenerateOnePassVersion(IsNested).Encode(compressedOut);
            return pgpSignatureGenerator;
        }

        #endregion Encrypt and Sign

        #region Decrypt

        /*
        * decrypt a given stream.
        */

        public static void Decrypt(string inputfile, string privateKeyFile, string passPhrase, string outputFile)
        {
            if (!File.Exists(inputfile))
                throw new FileNotFoundException(string.Format("Encrypted File [{0}] not found.", inputfile));

            if (!File.Exists(privateKeyFile))
                throw new FileNotFoundException(string.Format("Private Key File [{0}] not found.", privateKeyFile));

            if (string.IsNullOrEmpty(outputFile))
                throw new ArgumentNullException("Invalid Output file path.");

            using (Stream inputStream = File.OpenRead(inputfile))
            {
                using (Stream keyIn = File.OpenRead(privateKeyFile))
                {
                    Decrypt(inputStream, keyIn, passPhrase, outputFile);
                }
            }
        }

        /*
        * decrypt a given stream.
        */

        public static void Decrypt(Stream inputStream, Stream privateKeyStream, string passPhrase, string outputFile)
        {
            PgpObjectFactory pgpF = null;
            PgpEncryptedDataList enc = null;
            PgpObject o = null;
            PgpPrivateKey sKey = null;
            PgpPublicKeyEncryptedData pbe = null;
            PgpSecretKeyRingBundle pgpSec = null;

            pgpF = new PgpObjectFactory(PgpUtilities.GetDecoderStream(inputStream));
            // find secret key
            pgpSec = new PgpSecretKeyRingBundle(PgpUtilities.GetDecoderStream(privateKeyStream));

            if (pgpF != null)
                o = pgpF.NextPgpObject();

            // the first object might be a PGP marker packet.
            if (o is PgpEncryptedDataList)
                enc = (PgpEncryptedDataList) o;
            else
                enc = (PgpEncryptedDataList) pgpF.NextPgpObject();

            // decrypt
            foreach (PgpPublicKeyEncryptedData pked in enc.GetEncryptedDataObjects())
            {
                sKey = FindSecretKey(pgpSec, pked.KeyId, passPhrase.ToCharArray());

                if (sKey != null)
                {
                    pbe = pked;
                    break;
                }
            }

            if (sKey == null)
                throw new ArgumentException("Secret key for message not found.");

            PgpObjectFactory plainFact = null;

            using (var clear = pbe.GetDataStream(sKey))
            {
                plainFact = new PgpObjectFactory(clear);
            }

            var message = plainFact.NextPgpObject();

            if (message is PgpCompressedData)
            {
                var cData = (PgpCompressedData) message;
                PgpObjectFactory of = null;

                using (var compDataIn = cData.GetDataStream())
                {
                    of = new PgpObjectFactory(compDataIn);
                }

                message = of.NextPgpObject();
                if (message is PgpOnePassSignatureList)
                {
                    message = of.NextPgpObject();
                    PgpLiteralData Ld = null;
                    Ld = (PgpLiteralData) message;
                    using (Stream output = File.Create(outputFile))
                    {
                        var unc = Ld.GetInputStream();
                        Streams.PipeAll(unc, output);
                    }
                }
                else
                {
                    PgpLiteralData Ld = null;
                    Ld = (PgpLiteralData) message;
                    using (Stream output = File.Create(outputFile))
                    {
                        var unc = Ld.GetInputStream();
                        Streams.PipeAll(unc, output);
                    }
                }
            }
            else if (message is PgpLiteralData)
            {
                var ld = (PgpLiteralData) message;
                var outFileName = ld.FileName;

                using (Stream fOut = File.Create(outputFile))
                {
                    var unc = ld.GetInputStream();
                    Streams.PipeAll(unc, fOut);
                }
            }
            else if (message is PgpOnePassSignatureList)
            {
                throw new PgpException("Encrypted message contains a signed message - not literal data.");
            }
            else
            {
                throw new PgpException("Message is not a simple encrypted file - type unknown.");
            }

            #region commented code

            //if (pbe.IsIntegrityProtected())
            //{
            //  if (!pbe.Verify())
            //    msg = "message failed integrity check.";
            //  //Console.Error.WriteLine("message failed integrity check");
            //  else
            //    msg = "message integrity check passed.";
            //  //Console.Error.WriteLine("message integrity check passed");
            //}
            //else
            //{
            //  msg = "no message integrity check.";
            //  //Console.Error.WriteLine("no message integrity check");
            //}

            #endregion commented code
        }

        #endregion Decrypt

        #region Private helpers

        /*
        * A simple routine that opens a key ring file and loads the first available key suitable for encryption.
        */

        private static PgpPublicKey ReadPublicKey(Stream inputStream)
        {
            inputStream = PgpUtilities.GetDecoderStream(inputStream);

            var pgpPub = new PgpPublicKeyRingBundle(inputStream);

            // we just loop through the collection till we find a key suitable for encryption, in the real
            // world you would probably want to be a bit smarter about this.
            // iterate through the key rings.
            foreach (PgpPublicKeyRing kRing in pgpPub.GetKeyRings())
            foreach (PgpPublicKey k in kRing.GetPublicKeys())
                if (k.IsEncryptionKey)
                    return k;

            throw new ArgumentException("Can't find encryption key in key ring.");
        }

        /*
        * Search a secret key ring collection for a secret key corresponding to keyId if it exists.
        */

        private static PgpPrivateKey FindSecretKey(PgpSecretKeyRingBundle pgpSec, long keyId, char[] pass)
        {
            var pgpSecKey = pgpSec.GetSecretKey(keyId);

            if (pgpSecKey == null)
                return null;

            return pgpSecKey.ExtractPrivateKey(pass);
        }

        #endregion Private helpers
    }


    public class PgpEncryptionKeys
    {
        /// <summary>
        ///     Initializes a new instance of the EncryptionKeys class.
        ///     Two keys are required to encrypt and sign data. Your private key and the recipients public key.
        ///     The data is encrypted with the recipients public key and signed with your private key.
        /// </summary>
        /// <param name="publicKeyPath">The key used to encrypt the data</param>
        /// <param name="privateKeyPath">The key used to sign the data.</param>
        /// <param name="passPhrase">The (your) password required to access the private key</param>
        /// <exception cref="ArgumentException">Public key not found. Private key not found. Missing password</exception>
        public PgpEncryptionKeys(string publicKeyPath, string privateKeyPath, string passPhrase)
        {
            if (!File.Exists(publicKeyPath))
                throw new ArgumentException("Public key file not found", "publicKeyPath");
            if (!File.Exists(privateKeyPath))
                throw new ArgumentException("Private key file not found", "privateKeyPath");
            if (string.IsNullOrEmpty(passPhrase))
                throw new ArgumentException("passPhrase is null or empty.", "passPhrase");
            PublicKey = ReadPublicKey(publicKeyPath);
            SecretKey = ReadSecretKey(privateKeyPath);
            PrivateKey = ReadPrivateKey(passPhrase);
        }

        public PgpPublicKey PublicKey { get; }

        public PgpPrivateKey PrivateKey { get; }

        public PgpSecretKey SecretKey { get; }

        #region Private Key

        private PgpPrivateKey ReadPrivateKey(string passPhrase)
        {
            var privateKey = SecretKey.ExtractPrivateKey(passPhrase.ToCharArray());
            if (privateKey != null)
                return privateKey;
            throw new ArgumentException("No private key found in secret key.");
        }

        #endregion Private Key

        #region Secret Key

        private PgpSecretKey ReadSecretKey(string privateKeyPath)
        {
            using (Stream keyIn = File.OpenRead(privateKeyPath))
            {
                using (var inputStream = PgpUtilities.GetDecoderStream(keyIn))
                {
                    var secretKeyRingBundle = new PgpSecretKeyRingBundle(inputStream);
                    var foundKey = GetFirstSecretKey(secretKeyRingBundle);
                    if (foundKey != null)
                        return foundKey;
                }
            }

            throw new ArgumentException("Can't find signing key in key ring.");
        }

        /// <summary>
        ///     Return the first key we can use to encrypt.
        ///     Note: A file can contain multiple keys (stored in "key rings")
        /// </summary>
        private PgpSecretKey GetFirstSecretKey(PgpSecretKeyRingBundle secretKeyRingBundle)
        {
            foreach (PgpSecretKeyRing kRing in secretKeyRingBundle.GetKeyRings())
            {
                var key = kRing.GetSecretKeys()
                    .Cast<PgpSecretKey>()
                    .Where(k => k.IsSigningKey)
                    .FirstOrDefault();
                if (key != null)
                    return key;
            }

            return null;
        }

        #endregion Secret Key

        #region Public Key

        private PgpPublicKey ReadPublicKey(string publicKeyPath)
        {
            using (Stream keyIn = File.OpenRead(publicKeyPath))
            {
                using (var inputStream = PgpUtilities.GetDecoderStream(keyIn))
                {
                    var publicKeyRingBundle = new PgpPublicKeyRingBundle(inputStream);
                    var foundKey = GetFirstPublicKey(publicKeyRingBundle);
                    if (foundKey != null)
                        return foundKey;
                }
            }

            throw new ArgumentException("No encryption key found in public key ring.");
        }

        private PgpPublicKey GetFirstPublicKey(PgpPublicKeyRingBundle publicKeyRingBundle)
        {
            foreach (PgpPublicKeyRing kRing in publicKeyRingBundle.GetKeyRings())
            {
                var key = kRing.GetPublicKeys()
                    .Cast<PgpPublicKey>()
                    .Where(k => k.IsEncryptionKey)
                    .FirstOrDefault();
                if (key != null)
                    return key;
            }

            return null;
        }

        #endregion Public Key
    }

}