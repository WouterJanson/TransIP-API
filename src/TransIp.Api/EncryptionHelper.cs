using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Security.Application;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace TransIp.Api
{
	/// <summary>
	/// Contains any method related to the encryption of API requests.
	/// </summary>
	public static class EncryptionHelper
	{
		private static readonly Regex PrivateKeyRegex =
			new Regex(@"-----BEGIN (RSA )?PRIVATE KEY-----(.*)-----END (RSA )?PRIVATE KEY-----",
				RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
		
		private static readonly Regex EscapeRegex = new Regex(@"%..", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Signs the given arguments with the given private key.
		/// </summary>
		/// <param name="privateKey">The private key.</param>
		/// <param name="args">The arguments.</param>
		/// <returns>The signature.</returns>
		public static string Sign(string privateKey, object[] args)
		{
			var matches = PrivateKeyRegex.Matches(privateKey);
			if (matches.Count == 0) {
				throw new Exception("Invalid private key.");
			}

			var digest = Sha512Asn1(EncodeArguments(args));
			var signature = Encrypt(digest, privateKey);

			return Convert.ToBase64String(signature);
		}

		private static byte[] Sha512Asn1(string data)
		{
			var signature = new[]
			{
				0x30, 0x51, 0x30,
				0x0d, 0x06, 0x09,
				0x60, 0x86, 0x48,
				0x01, 0x65, 0x03,
				0x04, 0x02, 0x03,
				0x05, 0x00, 0x04,
				0x40
			};

			var hashAlg = SHA512.Create();
			var hash = hashAlg.ComputeHash(System.Text.Encoding.ASCII.GetBytes(data));

			return signature.Select(x => (byte) x).Concat(hash).ToArray();
		}

		private static byte[] Encrypt(byte[] digest, string key)
		{
			var keyReader = new StringReader(key);
			var pemReader = new PemReader(keyReader);

			var pemObject = pemReader.ReadObject();
			ICipherParameters cipherParameters;
			switch (pemObject) {
				case RsaPrivateCrtKeyParameters parameters:
					cipherParameters = parameters;
					break;
				case AsymmetricCipherKeyPair _:
					var keyPair = (AsymmetricCipherKeyPair) pemObject;
					cipherParameters = keyPair.Private;
					break;
				default:
					throw new Exception("Unsupported private key format. Got object of type '" + pemObject.GetType() + "' from PEM reader.");
			}

			var cipher = CipherUtilities.GetCipher("RSA/None/PKCS1Padding");
			cipher.Init(true, cipherParameters);

			return cipher.DoFinal(digest);
		}

		private static string EncodeArguments(object args, string keyPrefix = null)
		{
			if (!CanEnumerate(args)) {
				return Encode(args);
			}

			var encodedData = new List<string>();
			foreach (var arg in Enumerate(args)) {
				var encodedKey = keyPrefix == null ? Encoder.UrlEncode(arg.Key) : keyPrefix + "[" + Encoder.UrlEncode(arg.Key) + "]";

				if (CanEnumerate(arg.Value)) {
					encodedData.Add(EncodeArguments(arg.Value, encodedKey));
				} else {
					encodedData.Add(encodedKey + "=" + Encode(arg.Value));
				}
			}

			return string.Join("&", encodedData);
		}

		/// <summary>
		/// Encodes the given object using its "ToString" implementation.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <returns></returns>
		public static string Encode(object obj)
		{
			var result = obj != null ? Encoder.UrlEncode(obj.ToString()) : "";
			result = result.Replace("%7E", "~"); // Not sure if this is necessary.
			result = EscapeRegex.Replace(result, match => match.Value.ToUpper());

			return result;
		}

		private static bool CanEnumerate(object arg)
		{
			switch (arg) {
				case null:
					return false;
				case IEnumerable _ when !(arg is string):
					return true;
			}

			return arg.GetType().GetCustomAttributes(typeof (DataContractAttribute), false).Any();
		}

		private static IDictionary<string, object> Enumerate(object arg)
		{
			var result = new Dictionary<string, object>();

			if (arg is IEnumerable enumerable && !(enumerable is string)) {
				var counter = 0;
				foreach (var obj in enumerable) {
					if (obj is KeyValuePair<string, string> keyValuePair) {
						result.Add(keyValuePair.Key, keyValuePair.Value);
					} else {
						result.Add(counter.ToString(), obj);
					}
					counter++;
				}
			} else if (arg.GetType().GetCustomAttributes(typeof (DataContractAttribute), false).Any()) {
				foreach (var member in arg.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance)) {
					var attr = member.GetCustomAttribute<DataMemberAttribute>();
					if (attr != null) {
						result.Add(attr.Name ?? member.Name,
							member is FieldInfo ? ((FieldInfo) member).GetValue(arg) : ((PropertyInfo) member).GetValue(arg));
					}
				}
			}

			return result;
		}
	}
}