using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace JianProject.CysTerra {
	public static class JianParser {
		public static IEnumerable<JianEvent> Parse(JsonElement root, Func<string, bool>? accept) {
			if (root.ValueKind != JsonValueKind.Object) {
				yield break;
			}

			var type = FirstText(root, "type");
			if (type == "heartbeat" || type == "pong" || type == "error" || type == "starting" || type == "initial") {
				yield break;
			}

			if (type == "all") {
				foreach (var prop in root.EnumerateObject()) {
					if (!prop.Name.StartsWith("source", StringComparison.Ordinal)) {
						continue;
					}
					var agency = AgencyFromSourceKey(prop.Name);
					if (prop.Value.ValueKind != JsonValueKind.Object) {
						continue;
					}
					var data = prop.Value.TryGetProperty("Data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Object
						? dataEl
						: prop.Value;
					var evt = Map(agency, data);
					if (evt != null && (accept == null || accept(evt.Agency))) {
						yield return evt;
					}
				}
				yield break;
			}

			if (type != null && type.EndsWith("list_response", StringComparison.Ordinal)) {
				yield break;
			}

			if (string.IsNullOrEmpty(type) || IsStationFeed(type)) {
				yield break;
			}

			var payload = root.TryGetProperty("Data", out var dataNode) && dataNode.ValueKind == JsonValueKind.Object
				? dataNode
				: root;
			var single = Map(type, payload);
			if (single != null && (accept == null || accept(single.Agency))) {
				yield return single;
			}
		}

		static JianEvent? Map(string agency, JsonElement data) {
			agency = NormalizeAgency(agency, data);
			if (agency.Length == 0 || IsStationFeed(agency) || SkipAgency(agency)) {
				return null;
			}
			if (Flag(data, "isTraining", "is_training", "training")) {
				return null;
			}

			var kind = Classify(agency);
			// 海啸不用 FE placename_zh（会变成「日本本州东岸近海」叠在官方震源名上）
			var placeZh = kind == JianKind.Tsunami
				? null
				: FirstText(data, "placename_zh", "placeName_zh", "place_name_zh");
			var place = FirstText(data, "placeName", "Hypocenter", "hypocenter", "place", "epicenter", "location", "Place");
			var forward = FirstText(data, "agency");
			var title = FirstText(data, "title", "Title");
			var headline = FirstText(data, "headline", "Headline");
			var description = FirstText(data, "description", "Description");
			var comment = FirstText(data, "comment", "Comment");
			var infoType = FirstText(data, "infoTypeName", "infoType", "alertflg");
			var magnitude = Num(data, "magnitude", "Magunitude", "Magnitude");
			var magType = FirstText(data, "magnitudeType", "MagnitudeType", "magType");
			var depth = DepthKm(data);
			var latitude = Num(data, "latitude", "Latitude");
			var longitude = Num(data, "longitude", "Longitude");
			// 海啸：优先发震时刻 shockTime（对齐官方详情时间）；其它源仍用 originTime
			var origin = kind == JianKind.Tsunami
				? Time(data, "shockTime", "originTime", "time", "Time", "reportTime")
				: Time(data, "originTime", "shockTime", "time", "Time", "reportTime");
			var intensity = FirstText(data, "maxIntensity", "MaxIntensity", "intensity", "epiIntensity");
			var detail = !string.IsNullOrEmpty(headline)
				? headline
				: (!string.IsNullOrEmpty(description) ? FirstLine(description) : DetailText(data, kind));
			var serial = Int(data, "serial", "number", "Serial", "updates", "reportNum", "ReportNum");
			var level = FirstText(data, "level", "Level");
			var expire = kind == JianKind.Tsunami
				? Time(data, "endTime", "expireTime", "expire_ms", "validUntil", "until", "ValidDateTime", "validDateTime")
				: null;
			if (kind == JianKind.Tsunami && JianTestFlags.IgnoreTsunamiExpire) {
				expire = null; // 临时手测：不过期
			}
			var tsunamiAreas = kind == JianKind.Tsunami ? ParseTsunamiAreas(data) : null;
			var cancel = Flag(data, "isCancel", "isCanceled", "isCancelled", "canceled", "cancelled", "is_cancel", "cancel")
				|| Contains(infoType, "取消")
				|| Contains(title, "取消");
			if (kind == JianKind.Tsunami && !cancel) {
				var active = 0;
				if (tsunamiAreas != null) {
					foreach (var a in tsunamiAreas) {
						if (!a.Cleared && !IsTsunamiNone(a.Kind)) {
							active++;
						}
					}
				}
				var clearedFlag = Flag(data, "cleared");
				if ((clearedFlag && active == 0)
					|| (tsunamiAreas != null && tsunamiAreas.Count > 0 && active == 0)
					|| (Contains(title, "解除") && active == 0)
					|| Contains(level, "解除")) {
					cancel = true;
				}
			}
			var final = Flag(data, "isFinal", "is_final", "final", "isLast", "isLastInfo");
			var assumption = Flag(data, "isAssumption", "is_assumption", "assumption", "isAssumed", "isPLUM", "isPlum", "is_plum")
				|| Contains(place, "仮定")
				|| Contains(place, "調査中")
				|| Contains(place, "调查中");
			var warn = Flag(data, "isWarn", "is_warn", "warn")
				|| Contains(infoType, "警報") || Contains(infoType, "警报")
				|| Contains(title, "警報") || Contains(title, "警报")
				|| (infoType != null && infoType.Equals("Warning", StringComparison.OrdinalIgnoreCase));
			var reviewed = ParseReviewed(agency, kind, infoType, data);
			var verified = ParseVerified(agency, data);

			if (kind == JianKind.Other && place == null && title == null && magnitude == null && detail == null) {
				return null;
			}

			var eventId = FirstText(data, "id", "EventID", "eventId", "event_id");
			var unit = eventId ?? BuildUnit(agency, origin, latitude, longitude, place, title);
			var fingerprint = string.Join("|",
				agency, unit, serial?.ToString(CultureInfo.InvariantCulture) ?? "",
				cancel ? "1" : "0", final ? "1" : "0", warn ? "1" : "0",
				reviewed is true ? "R" : reviewed is false ? "A" : "",
				verified is true ? "V" : verified is false ? "U" : "",
				Fmt(magnitude), Fmt(depth), Fmt(latitude), Fmt(longitude),
				place ?? "", placeZh ?? "", forward ?? "", title ?? "", intensity ?? "", infoType ?? "", detail ?? "", description ?? "", comment ?? "",
				expire?.Ticks.ToString(CultureInfo.InvariantCulture) ?? "");

			return new JianEvent {
				Kind = kind,
				Agency = agency,
				UnitId = unit,
				Serial = serial,
				IsCancel = cancel,
				IsFinal = final,
				IsAssumption = assumption,
				IsWarn = warn,
				IsReviewed = reviewed,
				IsVerified = verified,
				Place = place,
				PlaceZh = placeZh,
				ForwardAgency = forward,
				Title = title,
				InfoType = infoType,
				Headline = headline,
				Description = description,
				Comment = comment,
				MagnitudeType = magType,
				Magnitude = magnitude,
				DepthKm = depth,
				Latitude = latitude,
				Longitude = longitude,
				OriginUtc = origin,
				ExpireUtc = expire,
				Intensity = intensity,
				Detail = detail,
				TsunamiAreas = tsunamiAreas,
				Fingerprint = fingerprint,
			};
		}

		/// <summary>
		/// CENC：对齐 Fan（autoFlag/infoTypeName 含「正式」）与 Wolfx（type=reviewed）。
		/// 能判定时写 true/false；非 CENC 速报返回 null。
		/// </summary>
		static bool? ParseReviewed(string agency, JianKind kind, string? infoType, JsonElement data) {
			var head = agency.Trim().ToLowerInvariant().Replace('_', '-').Split(':')[0];
			if (kind != JianKind.Bulletin || head != "cenc") {
				return null;
			}
			var autoFlag = FirstText(data, "autoFlag", "AutoFlag");
			if (autoFlag == "I") {
				return true;
			}
			if (autoFlag == "A") {
				return false;
			}
			if (Contains(infoType, "正式") || Contains(infoType, "人工")) {
				return true;
			}
			if (Contains(infoType, "自动") || Contains(infoType, "自動")) {
				return false;
			}
			foreach (var key in new[] { "type", "Type", "status", "Status", "evaluationStatus" }) {
				var eval = FirstText(data, key);
				if (string.IsNullOrEmpty(eval)) {
					continue;
				}
				var t = eval.Trim().ToLowerInvariant();
				if (t is "reviewed" or "manual" or "final"
					|| t.Contains("reviewed") || t.Contains("正式") || t.Contains("人工")) {
					return true;
				}
				if (t is "automatic" or "auto"
					|| t.Contains("automatic") || t.Contains("自动") || t.Contains("自動")) {
					return false;
				}
			}
			// Fan：未标正式则按自动测定展示
			return false;
		}

		static bool? ParseVerified(string agency, JsonElement data) {
			var head = agency.Trim().ToLowerInvariant().Replace('_', '-').Split(':')[0];
			if (head != "hko") {
				return null;
			}
			var status = FirstText(data, "verificationStatus", "VerificationStatus", "verified");
			if (string.IsNullOrEmpty(status)) {
				return false;
			}
			var s = status.Trim();
			if (s is "Y" or "y" or "1" or "true" or "True" or "TRUE" or "yes") {
				return true;
			}
			return false;
		}

		static string NormalizeAgency(string agency, JsonElement data) {
			agency = agency.Trim();
			if (agency.Equals("cea-pr", StringComparison.OrdinalIgnoreCase)) {
				var province = FirstText(data, "province");
				if (!string.IsNullOrEmpty(province)) {
					return "cea-pr:" + province;
				}
			}
			return agency;
		}

		static JianKind Classify(string agency) {
			var a = agency.Trim().ToLowerInvariant().Replace('_', '-');
			var head = a.Split(':')[0];
			if (head == "jma-tsunami") {
				return JianKind.Tsunami;
			}
			if (head is "jma-eew" or "cea" or "cea-pr" or "cwa-eew" or "sa" or "kma-eew"
				|| head.EndsWith("-eew", StringComparison.Ordinal)
				|| head.StartsWith("cea-pr", StringComparison.Ordinal)) {
				return JianKind.EarlyWarning;
			}
			if (IsKnownBulletin(head)) {
				return JianKind.Bulletin;
			}
			return JianKind.Other;
		}

		static bool IsKnownBulletin(string head) => head is
			"cenc" or "cenc-fjdzj" or "ningxia" or "yunnan" or "shanxi" or "beijing" or "cwa" or "jma" or "hko"
			or "kma" or "tmd" or "mmd" or "bmkg" or "geonet" or "usgs" or "emsc" or "gfz" or "bcsf" or "ingv"
			or "usp" or "nrcan" or "afad" or "ipma" or "noa" or "sed" or "scsn" or "ipgp" or "infp" or "isc"
			or "knmi" or "ncedc" or "lmu" or "koeri" or "gsras" or "csn" or "phivolcs" or "ssn" or "ga"
			or "igepn" or "peru";

		static bool SkipAgency(string agency) {
			var head = agency.Trim().ToLowerInvariant().Replace('_', '-').Split(':')[0];
			if (head is "weather" or "early-est" or "earlyest" or "cenc-ir") {
				return true;
			}
			if (head.Contains("volcano")) {
				return true;
			}
			return head.EndsWith("-tsunami", StringComparison.Ordinal) && head != "jma-tsunami";
		}

		static bool IsStationFeed(string type) {
			var a = type.Trim().ToLowerInvariant();
			return a is "kmoni" or "s-net" or "kma-station" or "trem-station" or "pa-station" or "trem" or "pa";
		}

		static string AgencyFromSourceKey(string key) {
			const string wide = "source\uFF1A";
			if (key.StartsWith(wide, StringComparison.Ordinal)) {
				return key.Substring(wide.Length);
			}
			if (key.StartsWith("source:", StringComparison.Ordinal)) {
				return key.Substring("source:".Length);
			}
			var idx = key.IndexOf('\uFF1A');
			if (idx >= 0 && idx + 1 < key.Length) {
				return key.Substring(idx + 1);
			}
			idx = key.IndexOf(':');
			return idx >= 0 && idx + 1 < key.Length ? key.Substring(idx + 1) : key;
		}

		static string BuildUnit(string agency, DateTime? origin, double? lat, double? lon, string? place, string? title) {
			var sb = new StringBuilder(agency);
			sb.Append('|');
			if (origin != null) {
				sb.Append(origin.Value.Ticks.ToString(CultureInfo.InvariantCulture));
			}
			sb.Append('|');
			sb.Append(Fmt(lat));
			sb.Append('|');
			sb.Append(Fmt(lon));
			sb.Append('|');
			sb.Append(place ?? title ?? "");
			return sb.ToString();
		}

		static string? DetailText(JsonElement data, JianKind kind) {
			if (kind == JianKind.Tsunami && data.TryGetProperty("area", out var area)) {
				if (area.ValueKind == JsonValueKind.String) {
					return area.GetString();
				}
				if (area.ValueKind == JsonValueKind.Array) {
					var parts = new List<string>();
					foreach (var item in area.EnumerateArray()) {
						if (item.ValueKind == JsonValueKind.String) {
							var s = item.GetString();
							if (!string.IsNullOrWhiteSpace(s)) {
								parts.Add(s.Trim());
							}
							continue;
						}
						if (item.ValueKind != JsonValueKind.Object) {
							continue;
						}
						var name = FirstText(item, "name");
						var kindName = FirstText(item, "kind");
						if (string.IsNullOrEmpty(name)) {
							continue;
						}
						parts.Add(string.IsNullOrEmpty(kindName) ? name : name + " " + kindName);
						if (parts.Count >= 8) {
							break;
						}
					}
					if (parts.Count > 0) {
						return string.Join("\u3001", parts);
					}
				}
			}
			return FirstText(data, "description", "text", "content", "message", "level");
		}

		static IReadOnlyList<JianTsunamiArea>? ParseTsunamiAreas(JsonElement data) {
			if (!data.TryGetProperty("area", out var area) || area.ValueKind != JsonValueKind.Array) {
				return null;
			}
			var list = new List<JianTsunamiArea>();
			foreach (var item in area.EnumerateArray()) {
				if (item.ValueKind != JsonValueKind.Object) {
					continue;
				}
				var name = FirstText(item, "name") ?? "";
				if (name.Length == 0) {
					continue;
				}
				var kindName = FirstText(item, "kind") ?? "";
				var kindCode = FirstText(item, "kindCode", "KindCode") ?? "";
				var code = FirstText(item, "code") ?? "";
				var cleared = Flag(item, "cleared") || IsTsunamiNone(kindName) || kindCode == "00";
				var maxH = Num(item, "maxHeight", "MaxHeight", "height");
				var maxDesc = FirstText(item, "maxHeightDesc", "MaxHeightDesc") ?? "";
				list.Add(new JianTsunamiArea {
					Code = code,
					Name = name,
					Kind = kindName,
					KindCode = kindCode,
					Cleared = cleared,
					MaxHeightM = maxH,
					MaxHeightDesc = maxDesc,
				});
			}
			return list.Count > 0 ? list : null;
		}

		static bool IsTsunamiNone(string? kind) {
			if (string.IsNullOrEmpty(kind)) {
				return false;
			}
			return kind == "津波なし"
				|| kind.Contains("解除", StringComparison.Ordinal)
				|| kind.Contains("切替", StringComparison.Ordinal)
				|| kind.Contains("失効", StringComparison.Ordinal);
		}

		static string? FirstLine(string? text) {
			if (string.IsNullOrWhiteSpace(text)) {
				return null;
			}
			var t = text.Replace("\r\n", "\n").Trim();
			var nl = t.IndexOf('\n');
			return nl < 0 ? t : t.Substring(0, nl).Trim();
		}

		static string? FirstText(JsonElement obj, params string[] keys) {

			if (obj.ValueKind != JsonValueKind.Object) {
				return null;
			}
			foreach (var key in keys) {
				if (!obj.TryGetProperty(key, out var v)) {
					continue;
				}
				var text = v.ValueKind switch {
					JsonValueKind.String => v.GetString(),
					JsonValueKind.Number => v.GetRawText(),
					JsonValueKind.True => "true",
					JsonValueKind.False => "false",
					_ => null,
				};
				text = text?.Trim();
				if (!string.IsNullOrEmpty(text) && text != "\u2014") {
					return text;
				}
			}
			return null;
		}

		static double? Num(JsonElement obj, params string[] keys) {
			if (obj.ValueKind != JsonValueKind.Object) {
				return null;
			}
			foreach (var key in keys) {
				if (!obj.TryGetProperty(key, out var v)) {
					continue;
				}
				if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var n)) {
					return n;
				}
				if (v.ValueKind == JsonValueKind.String &&
					double.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var s)) {
					return s;
				}
			}
			return null;
		}

		static int? Int(JsonElement obj, params string[] keys) {
			var n = Num(obj, keys);
			if (n == null) {
				return null;
			}
			var v = (int)n.Value;
			return v > 0 ? v : null;
		}

		static double? DepthKm(JsonElement data) {
			if (data.ValueKind != JsonValueKind.Object) {
				return null;
			}
			foreach (var key in new[] { "depth", "Depth" }) {
				if (!data.TryGetProperty(key, out var v)) {
					continue;
				}
				if (v.ValueKind == JsonValueKind.String) {
					var s = v.GetString()?.Trim();
					if (s is "\u5F88\u6D45" or "\u3054\u304F\u6D45\u3044" or "\u6975\u6D45") {
						return 0;
					}
				}
			}
			return Num(data, "depth", "Depth");
		}

		static DateTime? Time(JsonElement obj, params string[] keys) {
			if (obj.ValueKind != JsonValueKind.Object) {
				return null;
			}
			foreach (var key in keys) {
				if (!obj.TryGetProperty(key, out var v)) {
					continue;
				}
				if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var n)) {
					var ms = NormalizeEpochMs(n);
					if (ms > 0) {
						return DateTime.UnixEpoch.AddMilliseconds(ms);
					}
				}
				if (v.ValueKind == JsonValueKind.String) {
					var s = v.GetString()?.Trim();
					if (string.IsNullOrEmpty(s)) {
						continue;
					}
					if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var sn)) {
						var ms = NormalizeEpochMs(sn);
						if (ms > 0) {
							return DateTime.UnixEpoch.AddMilliseconds(ms);
						}
					}
					if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt)) {
						return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
					}
				}
			}
			return null;
		}

		static long NormalizeEpochMs(double n) {
			if (n <= 0) {
				return 0;
			}
			if (n > 1_000_000_000_000d) {
				return (long)n;
			}
			if (n > 1_000_000_000d) {
				return (long)(n * 1000d);
			}
			return 0;
		}

		static bool Flag(JsonElement obj, params string[] keys) {
			if (obj.ValueKind != JsonValueKind.Object) {
				return false;
			}
			foreach (var key in keys) {
				if (!obj.TryGetProperty(key, out var v)) {
					continue;
				}
				switch (v.ValueKind) {
					case JsonValueKind.True:
						return true;
					case JsonValueKind.Number:
						if (v.TryGetDouble(out var n) && n != 0) {
							return true;
						}
						break;
					case JsonValueKind.String:
						var s = v.GetString()?.Trim();
						if (s is "1" or "true" or "True" or "TRUE" or "yes" or "cancel" or "canceled" or "cancelled"
							or "\u6700\u7D42" or "\u6700\u7EC8" or "\u6700\u7D42\u5831" or "\u6700\u7EC8\u62A5" or "\u53D6\u6D88") {
							return true;
						}
						break;
				}
			}
			return false;
		}

		static bool Contains(string? text, string needle) =>
			text != null && text.Contains(needle, StringComparison.Ordinal);

		static string Fmt(double? n) =>
			n?.ToString("G", CultureInfo.InvariantCulture) ?? "";
	}
}
