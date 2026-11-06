using System;

namespace JianProject.CysTerra {
	/// <summary>
	/// 卡片 / TTS 标题键：对齐 Fan Studio、Wolfx 官方拓展的分源文案。
	/// </summary>
	public static class JianTitles {
		public static string AgencyHead(string agency) {
			var head = agency.Trim().ToLowerInvariant().Replace('_', '-');
			var colon = head.IndexOf(':');
			return colon >= 0 ? head.Substring(0, colon) : head;
		}

		public static bool IsJmaEew(string agency) => AgencyHead(agency) == "jma-eew";

		public static bool IsCencBulletin(JianEvent e) =>
			e.Kind == JianKind.Bulletin && AgencyHead(e.Agency) == "cenc";

		public static bool IsHko(JianEvent e) => AgencyHead(e.Agency) == "hko";

		/// <summary>官方：仅预警续报写 Serial；CENC 速报 / ShakeAlert 不写。</summary>
		public static bool EmitsSerial(JianEvent e) {
			if (e.Kind != JianKind.EarlyWarning || e.Serial is not > 0) {
				return false;
			}
			return AgencyHead(e.Agency) != "sa";
		}

		public static string TitleKey(JianEvent e) {
			if (e.IsCancel) {
				return e.Kind == JianKind.Tsunami ? "TitleTsunamiCancel" : "TitleCancel";
			}
			if (e.Kind == JianKind.EarlyWarning && IsJmaEew(e.Agency)) {
				return e.IsWarn ? "TitleJmaEewWarning" : "TitleJmaEewForecast";
			}
			return e.Kind switch {
				// Fan CEA/CWA/KMA：固定「地震预警」，续报不改标题
				JianKind.EarlyWarning => "TitleEew",
				JianKind.Bulletin => IsHko(e) ? "TitleReport" : "TitleBulletin",
				JianKind.Tsunami => "TitleTsunami",
				JianKind.Weather => "TitleWeather",
				_ => "TitleOther",
			};
		}

		/// <summary>
		/// 对齐官方 JMAAtom：卡片标题用最高档（津波予報/注意报/警报…），不用笼统「海啸情报」。
		/// </summary>
		public static string TsunamiGradeTitle(JianEvent e, bool preferZh) {
			if (e.IsCancel) {
				return preferZh ? "海啸情报解除" : "津波予報解除";
			}
			var raw = e.Title ?? "";
			if (e.TsunamiAreas != null) {
				foreach (var a in e.TsunamiAreas) {
					if (a.Cleared || string.IsNullOrEmpty(a.Kind)) {
						continue;
					}
					if (GradeRank(a.Kind) > GradeRank(raw)) {
						raw = a.Kind;
					}
				}
			}
			if (preferZh) {
				if (ContainsAny(raw, "大津波", "大海啸")) {
					return "大海啸警报";
				}
				if (ContainsAny(raw, "津波警報", "海啸警报") && !ContainsAny(raw, "注意")) {
					return "海啸警报";
				}
				if (ContainsAny(raw, "注意")) {
					return "海啸注意报";
				}
				if (ContainsAny(raw, "予報", "预报")) {
					return "海啸预报";
				}
				return "海啸情报";
			}
			if (ContainsAny(raw, "大津波")) {
				return "大津波警報";
			}
			if (ContainsAny(raw, "津波警報") && !ContainsAny(raw, "注意")) {
				return "津波警報";
			}
			if (ContainsAny(raw, "注意")) {
				return "津波注意報";
			}
			if (ContainsAny(raw, "予報")) {
				return "津波予報";
			}
			return string.IsNullOrEmpty(raw) ? "津波予報" : raw;
		}

		static int GradeRank(string s) {
			if (ContainsAny(s, "大津波", "大海啸")) {
				return 4;
			}
			if (ContainsAny(s, "津波警報", "海啸警报") && !ContainsAny(s, "注意")) {
				return 3;
			}
			if (ContainsAny(s, "注意")) {
				return 2;
			}
			if (ContainsAny(s, "予報", "预报")) {
				return 1;
			}
			return 0;
		}

		static bool ContainsAny(string text, params string[] needles) {
			foreach (var n in needles) {
				if (text.Contains(n, StringComparison.Ordinal)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// 详情/列表解说：只用正文 description（或 headline），不含 WarningComment；
		/// 若上游仍把附加文拼进 description，截到「＜」/换行前（对齐官方一句）。
		/// </summary>
		public static string? TsunamiNarrative(JianEvent e) {
			var raw = FirstNonEmpty(e.Description, e.Headline);
			if (raw == null) {
				return null;
			}
			return TrimTsunamiNarrative(raw);
		}

		static string? FirstNonEmpty(params string?[] values) {
			foreach (var v in values) {
				if (!string.IsNullOrWhiteSpace(v)) {
					return v.Trim();
				}
			}
			return null;
		}

		static string TrimTsunamiNarrative(string s) {
			s = UnescapeNewlines(s);
			var cut = s.IndexOf('＜');
			if (cut < 0) {
				cut = s.IndexOf('<');
			}
			if (cut > 0) {
				s = s.Substring(0, cut);
			}
			var nl = s.IndexOfAny(['\n', '\r']);
			if (nl > 0) {
				s = s.Substring(0, nl);
			}
			return s.Trim();
		}

		/// <summary>上游/库内偶发把换行存成字面量 \n / \\n，先还原再截句。</summary>
		internal static string UnescapeNewlines(string s) {
			if (string.IsNullOrEmpty(s)) {
				return s;
			}
			// 多层转义逐级剥到真换行
			while (s.Contains("\\\\n", StringComparison.Ordinal)) {
				s = s.Replace("\\\\n", "\\n", StringComparison.Ordinal);
			}
			s = s.Replace("\\r\\n", "\n", StringComparison.Ordinal);
			s = s.Replace("\\n", "\n", StringComparison.Ordinal);
			s = s.Replace("\\r", "\r", StringComparison.Ordinal);
			return s;
		}

		/// <summary>CENC：正式测定精度更高（对齐 Fan AccuracyOrder 10 / 30）。</summary>
		public static int AccuracyOrder(JianEvent e) {
			if (e.Kind == JianKind.EarlyWarning) {
				return 70;
			}
			if (IsCencBulletin(e)) {
				return e.IsReviewed == true ? 10 : 30;
			}
			if (IsHko(e)) {
				return e.IsVerified == true ? 10 : 30;
			}
			if (e.Kind == JianKind.Bulletin) {
				return 10;
			}
			return 30;
		}
	}
}
