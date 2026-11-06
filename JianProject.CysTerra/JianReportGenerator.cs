using Cryville.Common.Compat;
using Cryville.EEW.Report;
using System;
using System.Globalization;
using Cryville.EEW;
using static Cryville.EEW.TagTypeKeys;

namespace JianProject.CysTerra {
	public sealed class JianReportGenerator : IContextedGenerator<JianEvent, IReportGeneratorContext, ReportModel?> {
		static readonly TagTypeKey TagQualityCenc = "Quality:CENC";
		static readonly TagTypeKey TagQualityJmaEew = "Quality:JMAEEW";
		static readonly TagTypeKey TagSerial = "Serial";

		readonly JianSource _source;

		public JianReportGenerator(JianSource source) {
			_source = source;
		}

		public ReportModel? Generate(JianEvent e, IReportGeneratorContext? context, ref CultureInfo culture) {
			ThrowHelper.ThrowIfNull(e);
			if (JianSources.Resolve(e.Agency) != _source) {
				return null;
			}
			context ??= EmptyReportGeneratorContext.Instance;

			using var lres = new LocalizedResource("", ref culture);
			var res = lres.RootMessageStringSet;
			var titleKey = JianTitles.TitleKey(e);
			var accuracy = JianTitles.AccuracyOrder(e);
			var result = new ReportModel {
				Source = JianSources.AuthorityLabel(culture, e.Agency, e.ForwardAgency),
				Title = res.GetStringRequired(titleKey),
				TimeZone = TimeZoneInfo.Utc,
				UtcIssueTime = DateTime.UtcNow,
			};
			if (e.Latitude is double nameLat && e.Longitude is double nameLon) {
				context.NameLocationTo(result, nameLat, nameLon, SharedCultures.Get("zh-CN"), culture);
			}
			if (result.Location == null) {
				var fallback = e.PlaceZh ?? e.Place ?? e.Title;
				if (!string.IsNullOrEmpty(fallback)) {
					result.Location = fallback;
					result.LocationSpecificity = e.PlaceZh != null ? 3 : 8;
				}
			}

			if (e.OriginUtc is DateTime origin) {
				result.Time = new DateTimeOffset(DateTime.SpecifyKind(origin, DateTimeKind.Utc));
			}

			if (e.Kind == JianKind.EarlyWarning || e.Kind == JianKind.Tsunami) {
				var issued = e.OriginUtc ?? DateTime.UtcNow;
				result.InvalidatedTime = new DateTimeOffset(
					DateTime.SpecifyKind(issued, DateTimeKind.Utc) + TimeSpan.FromMinutes(e.IsFinal || e.IsCancel ? 10 : 3));
			}
			if (e.IsCancel) {
				result.InvalidatedTime = new DateTimeOffset(DateTime.UtcNow);
			}

			result.GroupKeys.Add(new ReportUnitKey(e.Agency, e.UnitId));
			result.RevisionKey = BuildRevisionKey(e);

			if (e.Latitude is double lat && e.Longitude is double lon && e.Magnitude is double mag
				&& e.DepthKm is double depth && e.OriginUtc is DateTime hypoTime) {
				result.GroupKeys.Add(new HypocenterGroupKey(
					(float)lat, (float)lon, DateTime.SpecifyKind(hypoTime, DateTimeKind.Utc), (float)mag, (float)depth));
			}

			var jmaAssumptionOnly = JianTitles.IsJmaEew(e.Agency) && e.IsAssumption
				&& (e.Magnitude is null || e.Magnitude is <= 1.0);
			if (jmaAssumptionOnly) {
				// 对齐 Wolfx：低精度假定震源时只标 Quality，不铺震级/深度
				var qualityKey = e.Magnitude is null ? "PropertyQualityValueLevel" : "PropertyQualityValuePLUM";
				result.Properties.Add(new(
					TagQualityJmaEew,
					null,
					res.GetStringRequired(qualityKey),
					-1f) { AccuracyOrder = accuracy });
				return result;
			}

			if (!string.IsNullOrEmpty(e.Intensity)) {
				var intensityKey = JianIntensity.KeyForAgency(e.Agency);
				result.Properties.Add(new(
					intensityKey,
					res.GetStringRequired("PropertyIntensity"),
					e.Intensity,
					context.SeverityScheme,
					JianIntensity.RawValue(intensityKey, e.Intensity)) { AccuracyOrder = accuracy });
			}

			if (e.Magnitude is double magnitude) {
				result.Properties.Add(new(
					Magnitude,
					res.GetStringRequired("PropertyMagnitude"),
					magnitude.ToString("0.0", culture),
					context.SeverityScheme,
					magnitude) { AccuracyOrder = accuracy });
			}

			if (e.DepthKm is double depthKm) {
				result.Properties.Add(new(
					HypocenterDepth,
					res.GetStringRequired("PropertyDepth"),
					string.Format(culture, res.GetStringRequired("PropertyDepthValue"), depthKm.ToString("0.0", culture)),
					context.SeverityScheme,
					depthKm) { AccuracyOrder = accuracy });
			}

			if (JianTitles.EmitsSerial(e) && e.Serial is int serial) {
				result.Properties.Add(new(
					TagSerial,
					res.GetStringRequired("PropertySerial"),
					serial.ToString(culture),
					context.SeverityScheme,
					serial) { AccuracyOrder = 40 });
			}

			// Fan / Wolfx CENC：Quality:CENC → 正式测定 / 自动测定
			if (JianTitles.IsCencBulletin(e) && e.IsReviewed is bool reviewed) {
				result.Properties.Add(new(
					TagQualityCenc,
					null,
					res.GetStringRequired(reviewed ? "PropertyQualityValueReviewed" : "PropertyQualityValueAutomatic"),
					-1f) { AccuracyOrder = accuracy });
			}

			if (JianTitles.IsJmaEew(e.Agency) && e.IsAssumption) {
				result.Properties.Add(new(
					TagQualityJmaEew,
					null,
					res.GetStringRequired("PropertyQualityValueLevel"),
					-1f) { AccuracyOrder = accuracy });
			}

			if (!string.IsNullOrEmpty(e.Detail) && e.Detail != e.Place && e.Detail != e.Title && e.Detail != e.PlaceZh) {
				result.Predicate = e.Detail;
			}
			else if (e.IsAssumption) {
				result.Predicate = res.GetStringRequired("Assumption");
			}

			return result;
		}

		static IReportRevisionKey BuildRevisionKey(JianEvent e) {
			if (JianTitles.IsCencBulletin(e)) {
				return new ReviewedRevisionKey(e.IsReviewed == true);
			}
			if (JianTitles.IsHko(e)) {
				return new VerifiedRevisionKey(e.IsVerified == true);
			}
			if (e.Kind == JianKind.Bulletin) {
				return new EmptyRevisionKey();
			}
			return new ReportRevisionKey(e.Serial ?? 0, e.IsCancel, e.IsFinal);
		}

		sealed record ReportUnitKey(string Agency, string UnitId) : IReportUnitKey { }

		sealed record EmptyRevisionKey : IReportRevisionKey {
			public bool IsComparableWith(IReportRevisionKey obj) => obj is EmptyRevisionKey;
			public int CompareTo(IReportRevisionKey? obj) => 0;
		}

		sealed record ReviewedRevisionKey(bool IsReviewed) : IReportRevisionKey {
			public bool IsComparableWith(IReportRevisionKey obj) => obj is ReviewedRevisionKey;
			public int CompareTo(IReportRevisionKey? obj) {
				if (obj is not ReviewedRevisionKey other) {
					throw new ArgumentException("Mismatched revision key type.");
				}
				return IsReviewed.CompareTo(other.IsReviewed);
			}
		}

		sealed record VerifiedRevisionKey(bool IsVerified) : IReportRevisionKey {
			public bool IsComparableWith(IReportRevisionKey obj) => obj is VerifiedRevisionKey;
			public int CompareTo(IReportRevisionKey? obj) {
				if (obj is not VerifiedRevisionKey other) {
					throw new ArgumentException("Mismatched revision key type.");
				}
				return IsVerified.CompareTo(other.IsVerified);
			}
		}

		sealed record ReportRevisionKey(int Number, bool Cancel, bool Final) : IReportRevisionKey {
			public int? Serial => Number > 0 ? Number : null;
			public bool IsCancellation => Cancel;
			public bool IsFinalRevision => Final;

			public bool IsComparableWith(IReportRevisionKey obj) => obj is ReportRevisionKey;
			public int CompareTo(IReportRevisionKey? obj) {
				if (obj is not ReportRevisionKey other) {
					throw new ArgumentException("Mismatched revision key type.");
				}
				var cmp = Number.CompareTo(other.Number);
				if (cmp != 0) {
					return cmp;
				}
				if (Cancel != other.Cancel) {
					return Cancel ? 1 : -1;
				}
				return 0;
			}
		}
	}
}
