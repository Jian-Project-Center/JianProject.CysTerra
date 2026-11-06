using Cryville.Common.Compat;
using JianProject.CysTerra;
using Cryville.EEW.TTS;
using System.Globalization;
using System.Text;
using Cryville.EEW;

namespace JianProject.CysTerra.TTS {
	/// <summary>按单一数据源生成 TTS；Agency 不匹配时返回 null。</summary>
	public sealed class JianTtsGenerator : IContextedGenerator<JianEvent, ITTSMessageGeneratorContext, TTSEntry?> {
		readonly JianSource _source;

		public JianTtsGenerator(JianSource source) {
			_source = source;
		}

		public TTSEntry? Generate(JianEvent e, ITTSMessageGeneratorContext? context, ref CultureInfo culture) {
			ThrowHelper.ThrowIfNull(e);
			if (JianSources.Resolve(e.Agency) != _source) {
				return null;
			}
			using var lres = new LocalizedResource("", ref culture);
			var res = lres.RootMessageStringSet;

			var spoken = new StringBuilder();
			spoken.Append(string.Format(culture, res.GetStringRequired("TtsLead"), JianSources.AuthoritySpeechLabel(culture, e.Agency, e.ForwardAgency)));
			spoken.Append(res.GetStringRequired(JianTitles.TitleKey(e)));
			if (JianTitles.IsCencBulletin(e) && e.IsReviewed is bool reviewed) {
				var type = res.GetStringRequired(reviewed ? "TypeReviewed" : "TypeAutomatic");
				spoken.Append(string.Format(culture, res.GetStringRequired("TtsCencType"), type));
			}
			var place = e.PlaceZh ?? e.Place ?? e.Title;
			if (!string.IsNullOrEmpty(place)) {
				spoken.Append(string.Format(culture, res.GetStringRequired("TtsPlace"), place));
			}
			if (e.Magnitude is double mag) {
				spoken.Append(string.Format(culture, res.GetStringRequired("TtsMagnitude"), mag.ToString("0.0", culture)));
			}
			if (e.DepthKm is double depth) {
				spoken.Append(string.Format(culture, res.GetStringRequired("TtsDepth"), depth.ToString("0", culture)));
			}
			if (!string.IsNullOrEmpty(e.Intensity)) {
				spoken.Append(string.Format(culture, res.GetStringRequired("TtsIntensity"), e.Intensity));
			}
			if (!string.IsNullOrEmpty(e.Detail) && e.Kind == JianKind.Tsunami) {
				spoken.Append(string.Format(culture, res.GetStringRequired("TtsDetail"), e.Detail));
			}

			// CysTerra 文档：数值越小优先级越高；普通 0，紧急 -100。
			var priority = e.Kind switch {
				JianKind.EarlyWarning => e.IsCancel ? -50 : -100,
				JianKind.Tsunami => -90,
				JianKind.Weather => 0,
				JianKind.Bulletin => 0,
				_ => 10,
			};

			var repeatTitle = JianSources.AuthoritySpeechLabel(culture, e.Agency, e.ForwardAgency);
			if (string.IsNullOrWhiteSpace(repeatTitle)) {
				repeatTitle = res.GetStringRequired("TtsRepeatTitle");
			}
			return new TTSEntry(culture, repeatTitle, spoken.ToString(), priority, SoundFor(e));
		}

		static string? SoundFor(JianEvent e) {
			if (e.Kind == JianKind.EarlyWarning) {
				if (e.IsCancel) {
					return "eew_update_cancel";
				}
				if (e.IsFinal) {
					return "eew_update_final";
				}
				if (e.Serial is int serial && serial > 1) {
					return "eew_update";
				}
				return IntensitySound(e.Intensity);
			}
			if (e.Kind == JianKind.Bulletin) {
				return "eq";
			}
			return "ev";
		}

		static string IntensitySound(string? intensity) {
			if (string.IsNullOrEmpty(intensity)) {
				return "eew_2";
			}
			var t = intensity.Trim();
			if (t.Contains("7") || t.Contains("烈")) {
				return "eew_5";
			}
			if (t.Contains("6") || t.Contains("强")) {
				return "eew_4";
			}
			if (t.Contains("5") || t.Contains("中")) {
				return "eew_3";
			}
			if (t.Contains("4") || t.Contains("弱")) {
				return "eew_2";
			}
			return "eew_1";
		}
	}
}
