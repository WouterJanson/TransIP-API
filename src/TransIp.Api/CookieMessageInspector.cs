using System;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace TransIp.Api
{
	internal class CookieMessageInspector : IClientMessageInspector
	{
		private readonly CookieContainer _cookieContainer;
		public string Uri { get; set; }

		public CookieMessageInspector(CookieContainer cookieContainer)
		{
			_cookieContainer = cookieContainer;
			Uri = "http://tempuri.org";
		}

		public CookieMessageInspector(CookieContainer cookieContainer, string uri)
		{
			_cookieContainer = cookieContainer;
			Uri = uri;
		}

		public void AfterReceiveReply(ref Message reply, object correlationState)
		{
            if (!(reply.Properties[HttpResponseMessageProperty.Name] is HttpResponseMessageProperty httpResponse)) {
                return;
            }

			var cookie = httpResponse.Headers[HttpResponseHeader.SetCookie];

			if (!string.IsNullOrEmpty(cookie)) {
				_cookieContainer.SetCookies(new Uri(Uri), cookie);
			}
		}

		public object BeforeSendRequest(ref Message request, IClientChannel channel)
		{
			// The HTTP request object is made available in the outgoing message only when
			// the Visual Studio Debugger is attacched to the running process
			if (!request.Properties.ContainsKey(HttpRequestMessageProperty.Name)) {
				request.Properties.Add(HttpRequestMessageProperty.Name, new HttpRequestMessageProperty());
			}

			var httpRequest = (HttpRequestMessageProperty) request.Properties[HttpRequestMessageProperty.Name];
			httpRequest.Headers.Add(HttpRequestHeader.Cookie, _cookieContainer.GetCookieHeader(new Uri(Uri)));

			return null;
		}
	}
}