using AutoMapper;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using TransIp.Api.Dto;
using DnsEntry = TransIp.Api.Dto.DnsEntry;
using Domain = TransIp.Api.Dto.Domain;
using DomainAction = TransIp.Api.Dto.DomainAction;
using DomainBranding = TransIp.Api.Dto.DomainBranding;
using DomainCheckResult = TransIp.Api.Dto.DomainCheckResult;
using Nameserver = TransIp.Api.Dto.Nameserver;
using Tld = TransIp.Api.Dto.Tld;
using WhoisContact = TransIp.Api.Dto.WhoisContact;

namespace TransIp.Api
{
	/// <summary>
	/// The service for domain related tasks.
	/// </summary>
	public class DomainService : ClientBase<Remote.DomainServicePortTypeClient, Remote.DomainServicePortType>, IDomainService
	{
		/// <summary>
		/// Static constructor.
		/// </summary>
		static DomainService()
		{
			Mapper.Initialize(cfg => {
				cfg.CreateMap<DnsEntry, Remote.DnsEntry>();
				cfg.CreateMap<Remote.DnsEntry, DnsEntry>();

				cfg.CreateMap<Remote.Domain, Domain>()
					.ForMember(x => x.IsLocked, opt => opt.ResolveUsing(x => x.isLockedSpecified ? x.isLocked : (bool?)null));
				cfg.CreateMap<Domain, Remote.Domain>()
					.ForMember(x => x.isLocked, opt => opt.ResolveUsing(x => x.IsLocked.GetValueOrDefault(false)))
					.ForMember(x => x.isLockedSpecified, opt => opt.ResolveUsing(x => x.IsLocked.HasValue));

				cfg.CreateMap<Remote.DomainAction, DomainAction>();
				cfg.CreateMap<DomainAction, Remote.DomainAction>();

				cfg.CreateMap<Remote.DomainBranding, DomainBranding>();
				cfg.CreateMap<DomainBranding, Remote.DomainBranding>();

				cfg.CreateMap<Remote.DomainCheckResult, DomainCheckResult>();

				cfg.CreateMap<Remote.Nameserver, Nameserver>();
				cfg.CreateMap<Nameserver, Remote.Nameserver>();

				cfg.CreateMap<Remote.Tld, Tld>()
					.ForMember(x => x.CapabilityList, opt => opt.MapFrom(x => x.capabilities))
					.ForMember(x => x.Capabilities, opt => opt.Ignore());
				cfg.CreateMap<Tld, Remote.Tld>()
					.ForMember(x => x.capabilities, opt => opt.MapFrom(x => x.CapabilityList));

				cfg.CreateMap<Remote.WhoisContact, WhoisContact>()
					.ForMember(x => x.CompanyNumber, opt => opt.MapFrom(x => x.companyKvk));
				cfg.CreateMap<WhoisContact, Remote.WhoisContact>()
					.ForMember(x => x.companyKvk, opt => opt.MapFrom(x => x.CompanyNumber));
			});
		}

		/// <summary>
		/// Create a new client for communicating with the TransIP domain service.
		/// </summary>
		/// <param name="login">The login name from TransIP.</param>
		/// <param name="mode">The mode.</param>
		/// <param name="privateKey">The private key.</param>
		public DomainService(string login, ClientMode mode, string privateKey)
			: base("DomainService", "https://api.transip.nl/soap/?service=DomainService", login, mode, privateKey)
		{
		}

		protected override Remote.DomainServicePortTypeClient CreateClient(Binding binding, EndpointAddress address)
		{
			return new Remote.DomainServicePortTypeClient(binding, address);
		}

		/// <summary>
		/// Checks the availability of multiple domains.
		/// </summary>
		/// <param name="domainNames">The domain names to check for availability. A maximum of 20 domainNames at once can be checked.</param>
		/// <returns>A list of DomainCheckResult objects, holding the domainName and the status per result.</returns>
		public DomainCheckResult[] BatchCheckAvailability(string[] domainNames)
		{
			SetSignatureCookies("batchCheckAvailability", new object[] {domainNames});
			return Mapper.Map<DomainCheckResult[]>(Client.batchCheckAvailability(domainNames));
		}

		/// <summary>
		/// Checks the availability of a domain.
		/// </summary>
		/// <param name="domainName">The domain name to check for availability</param>
		/// <returns>The availability status of the domain name.</returns>
		public AvailabilityStatus CheckAvailability(string domainName)
		{
			SetSignatureCookies("checkAvailability", new object[] {domainName});
			return (AvailabilityStatus) Enum.Parse(typeof (AvailabilityStatus), Client.checkAvailability(domainName), true);
		}

		/// <summary>
		/// Gets the whois of a domain name.
		/// </summary>
		/// <param name="domainName">The domain name to get the whois for.</param>
		/// <returns>The whois data for the domain.</returns>
		public string GetWhois(string domainName)
		{
			SetSignatureCookies("getWhois", new object[] {domainName});
			return Client.getWhois(domainName);
		}

		/// <summary>
		/// Gets the names of all domains in your account.
		/// </summary>
		/// <returns>A list of all domains in your account.</returns>
		public string[] GetDomainNames()
		{
			SetSignatureCookies("getDomainNames", new object[0]);
			return Client.getDomainNames();
		}

		/// <summary>
		/// Gets information about a domainName.
		/// </summary>
		/// <param name="domainName">The domain name to get the information for.</param>
		/// <returns>A domain object holding the data for the requested domainName.</returns>
		public Domain GetInfo(string domainName)
		{
			SetSignatureCookies("getInfo", new object[] {domainName});
			return Mapper.Map<Domain>(Client.getInfo(domainName));
		}

		/// <summary>
		/// Registers a domain name, will automatically create and sign a proposition for it.
		/// </summary>
		/// <param name="domain">The domain object holding information about the domain that needs to be registered.</param>
		/// <remarks>Requires "readwrite" mode.</remarks>
		public void Register(Domain domain)
		{
			SetSignatureCookies("register", new object[] {domain});
			Client.register(Mapper.Map<Remote.Domain>(domain));
		}

		/// <summary>
		/// Cancels a domain name, will automatically create and sign a cancellation document.
		/// Please note that domains with webhosting cannot be cancelled through the API.
		/// </summary>
		/// <param name="domainName">The domain name that needs to be cancelled.</param>
		/// <param name="endTime">The time to cancel the domain.</param>
		public void Cancel(string domainName, CancellationTime endTime)
		{
			var endTimeStr = endTime.ToString().ToUpper();

			SetSignatureCookies("cancel", new object[] {domainName, endTimeStr});
			Client.cancel(domainName, endTimeStr);
		}

		/// <summary>
		/// Transfers a domain with changing the owner, not all TLDs support this (e.g. nl).
		/// </summary>
		/// <param name="domain">The Domain object holding information about the domain that needs to be transfered.</param>
		/// <param name="authCode">The authorization code for domains needing this for transfers (e.g. .com or .org transfers). Leave empty when n/a.</param>
		public void TransferWithOwnerChange(Domain domain, string authCode)
		{
			SetSignatureCookies("transferWithOwnerChange", new object[] {domain, authCode});
			Client.transferWithOwnerChange(Mapper.Map<Remote.Domain>(domain), authCode);
		}

		/// <summary>
		/// Transfers a domain without changing the owner.
		/// </summary>
		/// <param name="domain">The Domain object holding information about the domain that needs to be transfered.</param>
		/// <param name="authCode">The authorization code for domains needing this for transfers (e.g. .com or .org transfers). Leave empty when n/a.</param>
		public void TransferWithoutOwnerChange(Domain domain, string authCode)
		{
			SetSignatureCookies("transferWithoutOwnerChange", new object[] {domain, authCode});
			Client.transferWithoutOwnerChange(Mapper.Map<Remote.Domain>(domain), authCode);
		}

		/// <summary>
		/// Starts a nameserver change for this domain, will replace all existing nameservers with the new nameservers.
		/// </summary>
		/// <param name="domainName">The domain name to change the nameservers for.</param>
		/// <param name="nameservers">The list of new nameservers for this domain.</param>
		public void SetNameservers(string domainName, Nameserver[] nameservers)
		{
			SetSignatureCookies("setNameservers", new object[] {domainName, nameservers});
			Client.setNameservers(domainName, Mapper.Map<Remote.Nameserver[]>(nameservers));
		}

		/// <summary>
		/// Lock this domain in real time.
		/// </summary>
		/// <param name="domainName">The domain name to set the lock for.</param>
		public void SetLock(string domainName)
		{
			SetSignatureCookies("setLock", new object[] {domainName});
			Client.setLock(domainName);
		}

		/// <summary>
		/// Unlocks this domain in real time.
		/// </summary>
		/// <param name="domainName">The domain name to unlock.</param>
		public void UnsetLock(string domainName)
		{
			SetSignatureCookies("unsetLock", new object[] {domainName});
			Client.unsetLock(domainName);
		}

		/// <summary>
		/// Sets the DnEntries for this Domain, will replace all existing dns entries with the new entries.
		/// </summary>
		/// <param name="domainName">The domain mame to change the dns entries for.</param>
		/// <param name="dnsEntries">The list of new DnsEntries for this domain.</param>
		public void SetDnsEntries(string domainName, DnsEntry[] dnsEntries)
		{
			SetSignatureCookies("setDnsEntries", new object[] {domainName, dnsEntries});
			Client.setDnsEntries(domainName, Mapper.Map<Remote.DnsEntry[]>(dnsEntries));
		}

		/// <summary>
		/// Starts an owner change of a Domain, brings additional costs with the following TLDs:
		/// .nl
		/// .be
		/// .eu
		/// </summary>
		/// <param name="domainName">The domainName to change the owner for.</param>
		/// <param name="registrantWhoisContact">The new contact data for this.</param>
		public void SetOwner(string domainName, WhoisContact registrantWhoisContact)
		{
			SetSignatureCookies("setOwner", new object[] {domainName, registrantWhoisContact});
			Client.setOwner(domainName, Mapper.Map<Remote.WhoisContact>(registrantWhoisContact));
		}

		/// <summary>
		/// Starts a contact change of a domain, this will replace all existing contacts.
		/// </summary>
		/// <param name="domainName">The domainName to change the contacts for.</param>
		/// <param name="contacts">The list of new contacts for this domain.</param>
		public void SetContacts(string domainName, WhoisContact[] contacts)
		{
			SetSignatureCookies("setContacts", new object[] {domainName, contacts});
			Client.setContacts(domainName, Mapper.Map<Remote.WhoisContact[]>(contacts));
		}

		/// <summary>
		/// Get TransIP supported TLDs.
		/// </summary>
		/// <returns>Array of Tld objects.</returns>
		public Tld[] GetAllTldInfos()
		{
			SetSignatureCookies("getAllTldInfos", new object[0]);
			return Mapper.Map<Tld[]>(Client.getAllTldInfos());
		}

		/// <summary>
		/// Get info about a specific TLD.
		/// </summary>
		/// <param name="tldName">The tld to get information about.</param>
		/// <returns>Tld object with info about this Tld.</returns>
		public Tld GetTldInfo(string tldName)
		{
			SetSignatureCookies("getTldInfo", new object[] {tldName});
			return Mapper.Map<Tld>(Client.getTldInfo(tldName));
		}

		/// <summary>
		/// Gets info about the action this domain is currently running.
		/// </summary>
		/// <param name="domainName">Name of the domain.</param>
		/// <returns>If this domain is currently running an action, a corresponding DomainAction with info about the action will be returned.</returns>
		public DomainAction GetCurrentDomainAction(string domainName)
		{
			SetSignatureCookies("getCurrentDomainAction", new object[] {domainName});
			return Mapper.Map<DomainAction>(Client.getCurrentDomainAction(domainName));
		}

		/// <summary>
		/// Retries a failed domain action with new domain data. The Domain#name field must contain
		/// the name of the Domain, the nameserver, contacts, dnsEntries fields contain the new data for this domain.
		/// Set a field to null to not change the data.
		/// </summary>
		/// <param name="domain">The domain with data to retry.</param>
		public void RetryCurrentDomainActionWithNewData(Domain domain)
		{
			SetSignatureCookies("retryCurrentDomainActionWithNewData", new object[] {domain});
			Client.retryCurrentDomainActionWithNewData(Mapper.Map<Remote.Domain>(domain));
		}

		/// <summary>
		/// Retry a transfer action with a new authcode.
		/// </summary>
		/// <param name="domain">The domain to try the transfer with a different authcode for.</param>
		/// <param name="newAuthCode">New authorization code to try.</param>
		public void RetryTransferWithDifferentAuthCode(Domain domain, string newAuthCode)
		{
			SetSignatureCookies("retryTransferWithDifferentAuthCode", new object[] {domain, newAuthCode});
			Client.retryTransferWithDifferentAuthCode(Mapper.Map<Remote.Domain>(domain), newAuthCode);
		}

		/// <summary>
		/// Cancels a failed domain action.
		/// </summary>
		/// <param name="domain">The domain to cancel the action for.</param>
		public void CancelDomainAction(Domain domain)
		{
			SetSignatureCookies("cancelDomainAction", new object[] {domain});
			Client.cancelDomainAction(Mapper.Map<Remote.Domain>(domain));
		}
	}

	/// <summary>
	/// The time of cancellation.
	/// </summary>
	public enum CancellationTime
	{
		/// <summary>
		/// End of contract.
		/// </summary>
		End,

		/// <summary>
		/// As soon as possible.
		/// </summary>
		Immediately
	}
}