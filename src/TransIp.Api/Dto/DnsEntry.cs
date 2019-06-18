using System.Runtime.Serialization;

namespace TransIp.Api.Dto
{
	/// <summary>
	/// Models A DnsEntry.
	/// </summary>
	[DataContract]
	public class DnsEntry
	{
		/// <summary>
		/// The name of the dns entry, for example '@' or 'www'.
		/// </summary>
		[DataMember(Name = "name")]
		public string Name { get; set; }

		/// <summary>
		/// The expiration period of the dns entry, in seconds. For example 86400 for a day of expiration.
		/// </summary>
		[DataMember(Name = "expire")]
		public int Expire { get; set; }

		/// <summary>
		/// The type of dns entry, for example A, MX or CNAME.
		/// </summary>
		[DataMember(Name = "type")]
		public DnsEntryType Type { get; set; }

		/// <summary>
		/// The content of of the dns entry, for example '10 mail', '127.0.0.1' or 'www'.
		/// </summary>
		[DataMember(Name = "content")]
		public string Content { get; set; }

		public override string ToString() => $"{Name} IN {Expire} {Type} {Content}";
	}

	/// <summary>
	/// The available DNS entry types.
	/// </summary>
	public enum DnsEntryType
	{
		A,
		AAAA,
		CNAME,
		MX,
		NS,
		TXT,
		SRV,
		TLSA,
		CAA
	}
}