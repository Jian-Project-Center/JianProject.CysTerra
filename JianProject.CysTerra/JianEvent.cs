using System;

namespace JianProject.CysTerra {
	public enum JianKind {
		EarlyWarning,
		Bulletin,
		Tsunami,
		Weather,
		Other,
	}

	/// <summary>
	/// One event taken from a Jian Project API frame. Absent upstream fields stay null.
	/// </summary>
	public sealed class JianEvent {
		public JianKind Kind { get; init; }
		public string Agency { get; init; } = "";
		public string UnitId { get; init; } = "";
		public int? Serial { get; init; }
		public bool IsCancel { get; init; }
		public bool IsFinal { get; init; }
		public bool IsTraining { get; init; }
		public bool IsAssumption { get; init; }
		/// <summary>JMA 紧急地震速报（警报）；其它源通常为 false。</summary>
		public bool IsWarn { get; init; }
		/// <summary>CENC：正式测定 true / 自动测定 false；非 CENC 为 null。</summary>
		public bool? IsReviewed { get; init; }
		/// <summary>HKO：verificationStatus=Y 时为 true。</summary>
		public bool? IsVerified { get; init; }
		public string? Place { get; init; }
		public string? PlaceZh { get; init; }
		public string? ForwardAgency { get; init; }
		public string? Title { get; init; }
		public string? InfoType { get; init; }
		public double? Magnitude { get; init; }
		public double? DepthKm { get; init; }
		public double? Latitude { get; init; }
		public double? Longitude { get; init; }
		public DateTime? OriginUtc { get; init; }
		public string? Intensity { get; init; }
		public string? Detail { get; init; }
		public string Fingerprint { get; init; } = "";
	}
}
