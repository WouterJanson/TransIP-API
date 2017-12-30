using System.Net;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace TransIp.Api
{
	internal class CookieEndpointBehavior : IEndpointBehavior
	{
		private readonly CookieContainer _cookieContainer;
		private readonly string _uri;

		public CookieEndpointBehavior(CookieContainer cookieContainer, string uri)
		{
			_cookieContainer = cookieContainer;
			_uri = uri;
		}

		public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
		{
		}

		public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
		{
			clientRuntime.ClientMessageInspectors.Add(new CookieMessageInspector(_cookieContainer, _uri));
		}

		public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
		{
		}

		public void Validate(ServiceEndpoint endpoint)
		{
		}
	}
}