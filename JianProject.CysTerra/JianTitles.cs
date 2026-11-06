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
