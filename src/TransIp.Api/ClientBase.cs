using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace TransIp.Api
{
	/// <summary>
	/// The base client for TransIP services.
	/// </summary>
	public abstract class ClientBase<TClientType, TChannelType>
		where TChannelType : class
		where TClientType : ClientBase<TChannelType>
	{
		private BasicHttpBinding _binding;
		private readonly string _uri;
		private readonly string _privateKey;

		/// <summary>
		/// Gets the service client.
		/// </summary>
		public TClientType Client { get; private set; }

		/// <summary>
		/// Gets the name of the service.
		/// </summary>
		public string ServiceName { get; set; }

		/// <summary>
		/// Gets the login.
		/// </summary>
		public string Login { get; private set; }

		/// <summary>
		/// Gets the mode.
		/// </summary>
		public ClientMode Mode { get; private set; }

		/// <summary>
		/// Gets the cookie container.
		/// </summary>
		public CookieContainer Cookies { get; private set; }

		private Binding BasicHttpBinding
		{
			get
			{
				if (_binding != null) {
					return _binding;
				}
				_binding = new BasicHttpBinding
				{
					MaxReceivedMessageSize = int.MaxValue,
					AllowCookies = false,
					Security = {Mode = BasicHttpSecurityMode.Transport}
				};
				return _binding;
			}
		}

		/// <summary>
		/// Creates a new client for communicating with TransIP services.
		/// </summary>
		/// <param name="serviceName">The name of the service.</param>
		/// <param name="uri">The URI of the service.</param>
		/// <param name="login">The login name from TransIP.</param>
		/// <param name="mode">The mode.</param>
		/// <param name="privateKey">The private key.</param>
		protected ClientBase(string serviceName, string uri, string login, ClientMode mode, string privateKey)
		{
			_uri = uri;
			_privateKey = privateKey;
			ServiceName = serviceName;
			Login = login;
			Mode = mode;
			Cookies = new CookieContainer();

			Client = CreateClient(BasicHttpBinding, new EndpointAddress(_uri));
			Client.ChannelFactory.Endpoint.EndpointBehaviors.Add(new CookieEndpointBehavior(Cookies, _uri));

			// Set the static cookies.
			AddCookie("login", Login);
			AddCookie("mode", Mode.ToString().ToLower());
		}

		/// <summary>
		/// When implemented, should create the client using the given arguments.
		/// </summary>
		/// <param name="binding">The binding.</param>
		/// <param name="address">The address.</param>
		/// <returns>The created client.</returns>
		protected abstract TClientType CreateClient(Binding binding, EndpointAddress address);

		/// <summary>
		/// Update the signature cookies for the given
		/// </summary>
		/// <param name="method">The name of the method being called.</param>
		/// <param name="args">The passed arguments.</param>
		protected void SetSignatureCookies(string method, object[] args)
		{
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

			var nonce = Guid.NewGuid().ToString("N");

			AddCookie("timestamp", timestamp.ToString());
			AddCookie("nonce", nonce);
			AddCookie("signature", EncryptionHelper.Encode(EncryptionHelper.Sign(_privateKey, args.Concat(new object[]
			{
				new KeyValuePair<string, string>("__method", method),
				new KeyValuePair<string, string>("__service", ServiceName),
				new KeyValuePair<string, string>("__hostname", "api.transip.nl"),
				new KeyValuePair<string, string>("__timestamp", timestamp.ToString()),
				new KeyValuePair<string, string>("__nonce", nonce)
			}).ToArray())));
		}

		private void AddCookie(string name, string value)
		{
			Cookies.Add(new Cookie(name, value, "/", new Uri(_uri).DnsSafeHost));
		}
	}

	/// <summary>
	/// The different modes in which the client can operate.
	/// </summary>
	public enum ClientMode
	{
		/// <summary>
		/// Only allow reading.
		/// </summary>
		ReadOnly,

		/// <summary>
		/// Allow both reading and writing.
		/// </summary>
		ReadWrite
	}
}