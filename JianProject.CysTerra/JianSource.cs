using Cryville.EEW.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Cryville.EEW;

namespace JianProject.CysTerra {
	public enum JianSource {
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Browsable(false)]
		None,
		[LocalizableDisplayName(nameof(Cea), Path = ["SourceNameTypes"])] Cea,
		[LocalizableDisplayName(nameof(CeaPr), Path = ["SourceNameTypes"])] CeaPr,
		[LocalizableDisplayName(nameof(CwaEew), Path = ["SourceNameTypes"])] CwaEew,
		[LocalizableDisplayName(nameof(JmaEew), Path = ["SourceNameTypes"])] JmaEew,
		[LocalizableDisplayName(nameof(JmaTsunami), Path = ["SourceNameTypes"])] JmaTsunami,
		[LocalizableDisplayName(nameof(Sa), Path = ["SourceNameTypes"])] Sa,
		[LocalizableDisplayName(nameof(KmaEew), Path = ["SourceNameTypes"])] KmaEew,
		[LocalizableDisplayName(nameof(Cenc), Path = ["SourceNameTypes"])] Cenc,
		[LocalizableDisplayName(nameof(CencFjdzj), Path = ["SourceNameTypes"])] CencFjdzj,
		[LocalizableDisplayName(nameof(Ningxia), Path = ["SourceNameTypes"])] Ningxia,
		[LocalizableDisplayName(nameof(Yunnan), Path = ["SourceNameTypes"])] Yunnan,
		[LocalizableDisplayName(nameof(Shanxi), Path = ["SourceNameTypes"])] Shanxi,
		[LocalizableDisplayName(nameof(Beijing), Path = ["SourceNameTypes"])] Beijing,
		[LocalizableDisplayName(nameof(Cwa), Path = ["SourceNameTypes"])] Cwa,
		[LocalizableDisplayName(nameof(Jma), Path = ["SourceNameTypes"])] Jma,
		[LocalizableDisplayName(nameof(Hko), Path = ["SourceNameTypes"])] Hko,
		[LocalizableDisplayName(nameof(Kma), Path = ["SourceNameTypes"])] Kma,
		[LocalizableDisplayName(nameof(Tmd), Path = ["SourceNameTypes"])] Tmd,
		[LocalizableDisplayName(nameof(Mmd), Path = ["SourceNameTypes"])] Mmd,
		[LocalizableDisplayName(nameof(Bmkg), Path = ["SourceNameTypes"])] Bmkg,
		[LocalizableDisplayName(nameof(Geonet), Path = ["SourceNameTypes"])] Geonet,
		[LocalizableDisplayName(nameof(Usgs), Path = ["SourceNameTypes"])] Usgs,
		[LocalizableDisplayName(nameof(Emsc), Path = ["SourceNameTypes"])] Emsc,
		[LocalizableDisplayName(nameof(Gfz), Path = ["SourceNameTypes"])] Gfz,
		[LocalizableDisplayName(nameof(Bcsf), Path = ["SourceNameTypes"])] Bcsf,
		[LocalizableDisplayName(nameof(Ingv), Path = ["SourceNameTypes"])] Ingv,
		[LocalizableDisplayName(nameof(Usp), Path = ["SourceNameTypes"])] Usp,
		[LocalizableDisplayName(nameof(Afad), Path = ["SourceNameTypes"])] Afad,
		[LocalizableDisplayName(nameof(Ga), Path = ["SourceNameTypes"])] Ga,
	}

	public static class JianSources {
		public static HashSet<JianSource> DefaultAll() {
			var set = new HashSet<JianSource>();
			foreach (JianSource value in System.Enum.GetValues(typeof(JianSource))) {
				if (value == JianSource.None) {
					continue;
				}
				set.Add(value);
			}
			return set;
		}

		/// <summary>是否在本扩展处理列表内（枚举已映射的源）。</summary>
		public static bool IsHandled(string agency) => Flag(agency) != JianSource.None;

		public static bool Allows(ISet<JianSource> filter, string agency) {
			if (filter == null || filter.Count == 0) {
				return false;
			}
			var flag = Flag(agency);
			return flag != JianSource.None && filter.Contains(flag);
		}

		public static string AuthorityLabel(CultureInfo culture, string agency, string? forward) {
			return AuthorityName(culture, agency, forward, "AuthorityNames");
		}

		/// <summary>TTS 用机构全称（卡片仍用 AuthorityNames 缩写）。</summary>
		public static string AuthoritySpeechLabel(CultureInfo culture, string agency, string? forward) {
			return AuthorityName(culture, agency, forward, "AuthoritySpeechNames");
		}

		static string AuthorityName(CultureInfo culture, string agency, string? forward, string setKey) {
			using var lres = new LocalizedResource("", ref culture);
			var res = lres.RootMessageStringSet;
			var name = res.GetStringSetRequired(setKey).GetString(Flag(agency).ToString())
				?? res.GetStringSetRequired("AuthorityNames").GetString(Flag(agency).ToString())
				?? agency;
			var head = Head(agency);
			if (head == "emsc" && !string.IsNullOrWhiteSpace(forward)
				&& !forward.Equals("EMSC", StringComparison.OrdinalIgnoreCase)) {
				return string.Format(culture, res.GetStringRequired("AuthorityNameForwarded"), name, forward.Trim());
			}
			return name;
		}

		static string Head(string agency) {
			var head = agency.Trim().ToLowerInvariant().Replace('_', '-');
			var colon = head.IndexOf(':');
			return colon >= 0 ? head.Substring(0, colon) : head;
		}

		/// <summary>将上游 type/agency 映射到筛选项枚举。</summary>
		public static JianSource Resolve(string agency) => Flag(agency);

		static JianSource Flag(string agency) {
			return Head(agency) switch {
				"cea" => JianSource.Cea,
				"cea-pr" => JianSource.CeaPr,
				"cwa-eew" => JianSource.CwaEew,
				"jma-eew" => JianSource.JmaEew,
				"jma-tsunami" => JianSource.JmaTsunami,
				"sa" => JianSource.Sa,
				"kma-eew" => JianSource.KmaEew,
				"cenc" => JianSource.Cenc,
				"cenc-fjdzj" => JianSource.CencFjdzj,
				"ningxia" => JianSource.Ningxia,
				"yunnan" => JianSource.Yunnan,
				"shanxi" => JianSource.Shanxi,
				"beijing" => JianSource.Beijing,
				"cwa" => JianSource.Cwa,
				"jma" => JianSource.Jma,
				"hko" => JianSource.Hko,
				"kma" => JianSource.Kma,
				"tmd" => JianSource.Tmd,
				"mmd" => JianSource.Mmd,
				"bmkg" => JianSource.Bmkg,
				"geonet" => JianSource.Geonet,
				"usgs" => JianSource.Usgs,
				"emsc" => JianSource.Emsc,
				"gfz" => JianSource.Gfz,
				"bcsf" => JianSource.Bcsf,
				"ingv" => JianSource.Ingv,
				"usp" => JianSource.Usp,
				"afad" => JianSource.Afad,
				"ga" => JianSource.Ga,
				_ => JianSource.None,
			};
		}
	}
}
