using System;
using System.Collections.Generic;

namespace JianProject.CysTerra {
	public enum JianKind {
		EarlyWarning,
		Bulletin,
		Tsunami,
		Weather,
		Other,
	}

	/// <summary>JMA 海啸预报区一行（上游 area[]）。</summary>
	public sealed class JianTsunamiArea {
		public string Code { get; init; } = "";
		public string Name { get; init; } = "";
		public string Kind { get; init; } = "";
		public string KindCode { get; init; } = "";
		public bool Cleared { get; init; }
		/// <summary>预计浪高数值（米），上游 maxHeight。</summary>
		public double? MaxHeightM { get; init; }
		/// <summary>浪高文案，上游 maxHeightDesc。</summary>
		public string MaxHeightDesc { get; init; } = "";
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
		public string? Headline { get; init; }
		public string? Description { get; init; }
		/// <summary>解说/备注，上游 comment。</summary>
		public string? Comment { get; init; }
		public string? MagnitudeType { get; init; }
		public double? Magnitude { get; init; }
		public double? DepthKm { get; init; }
		public double? Latitude { get; init; }
		public double? Longitude { get; init; }
		public DateTime? OriginUtc { get; init; }
		/// <summary>海啸预报有效截止（上游 endTime / ValidDateTime）；注意报/警报常无。</summary>
		public DateTime? ExpireUtc { get; init; }
		public string? Intensity { get; init; }
		public string? Detail { get; init; }
		/// <summary>JMA 海啸预报区；非海啸为 null。</summary>
		public IReadOnlyList<JianTsunamiArea>? TsunamiAreas { get; init; }
		/// <summary>快照/首帧静默灌库：进列表与地图，TTS 跳过。</summary>
		public bool IsCatchUp { get; set; }
		public string Fingerprint { get; init; } = "";
	}
}
