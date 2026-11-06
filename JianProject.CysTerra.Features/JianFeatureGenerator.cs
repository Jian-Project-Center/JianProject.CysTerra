using Cryville.Common.Compat;
using Cryville.EEW.Features;
using JianProject.CysTerra;
using Cryville.Measure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using static Cryville.EEW.TagTypeKeys;
using Cryville.EEW;

namespace JianProject.CysTerra.Features {
	public sealed class JianFeatureGenerator : IGenerator<JianEvent, Feature?> {
		static readonly Unit Kilometre = Units.Metre.WithPrefix(MetricPrefixes.Kilo);
		readonly JianSource _source;

		public JianFeatureGenerator(JianSource source) {
			_source = source;
		}

		public Feature? Generate(JianEvent e, ref CultureInfo culture) {
			ThrowHelper.ThrowIfNull(e);
			if (JianSources.Resolve(e.Agency) != _source) {
				return null;
			}

			if (e.Kind == JianKind.Tsunami) {
				return GenerateTsunami(e, culture);
			}

			var kind = e.Kind switch {
				JianKind.EarlyWarning => ReportForecast,
				JianKind.Bulletin => ReportObservation,
				_ => Cryville.EEW.TagTypeKeys.Report,
			};

			var ongoing = e.Kind != JianKind.Bulletin && !e.IsCancel && !e.IsFinal;

			var feature = new Feature {
				{ Is, e.Kind is JianKind.EarlyWarning or JianKind.Bulletin ? Earthquake : kind },
				{ Ongoing, ongoing },
				{ Source, e.Agency },
			};

			if (e.Kind is JianKind.EarlyWarning or JianKind.Bulletin) {
				feature[Subject] = kind;
			}

			var placeName = PlaceName(e);
			if (placeName != null) {
				feature[Name] = placeName;
			}

			if (!string.IsNullOrEmpty(e.Detail)) {
				feature[Description] = e.Detail;
			}
			if (!string.IsNullOrEmpty(e.Title) && e.Title != e.Place) {
				feature[Comment] = e.Title;
			}

			if (e.OriginUtc is DateTime origin) {
				feature[Time] = new DateTimeOffset(DateTime.SpecifyKind(origin, DateTimeKind.Utc));
			}

			if (e.Magnitude is double mag) {
				var magKey = e.Agency.StartsWith("jma", StringComparison.OrdinalIgnoreCase) ? MagnitudeJMA : Magnitude;
				feature[magKey] = new Quantity(mag, Units.Dimensionless);
			}

			if (e.Latitude is double lat && e.Longitude is double lon) {
				var at = new Feature(new Point(lon, lat)) {
					{ Is, e.IsAssumption ? HypocenterAssumed : Hypocenter },
				};
				if (e.DepthKm is double depth) {
					at[HypocenterDepth] = new Quantity(depth, Kilometre);
				}
				feature[At] = at;
			}

			if (!string.IsNullOrEmpty(e.Intensity)) {
				var intensityKey = JianIntensity.KeyForAgency(e.Agency);
				feature[intensityKey] = JianIntensity.RawValue(intensityKey, e.Intensity);
			}

			return feature;
		}

		/// <summary>
		/// 对齐官方 JMAAtom.Features：
		/// Includes=预报区；Subject=Earthquake(震中+Mj)；Comment=解说；根上不放 Magnitude/At。
		/// </summary>
		static Feature GenerateTsunami(JianEvent e, CultureInfo culture) {
			var ongoing = !e.IsCancel && (JianTestFlags.IgnoreTsunamiExpire || e.ExpireUtc is not DateTime exp || exp > DateTime.UtcNow);
			// 对齐官方：根上不放 Name/Time（否则详情标题下变成「海啸预报+日期」）；档位名只在 Report.Title / 粉条
			var root = new Feature {
				{ Is, Cryville.EEW.TagTypeKeys.Report },
				{ Ongoing, ongoing },
				{ Source, e.Agency },
			};

			var areaItems = new Collection<Feature>();
			if (e.TsunamiAreas != null) {
				foreach (var a in e.TsunamiAreas) {
					if (a.Cleared) {
						continue;
					}
					var item = new Feature {
						{ Is, ReportForecast },
						{ At, ResolveAreaFeature(a, culture) },
						{ Severity, TsunamiSeverity(a) },
					};
					var height = TsunamiHeightValue(a);
					if (height != null) {
						item[TsunamiHeight] = height;
					}
					areaItems.Add(item);
				}
			}

			var includes = new Collection<Feature>();
			var forecast = new Feature {
				{ Is, ReportForecast },
				{ Includes, areaItems },
			};
			var outline = JmaOfficialTsunamiAreas.Outline;
			if (outline != null) {
				forecast[At] = outline;
			}
			includes.Add(forecast);
			root[Includes] = includes;

			// 官方文档 Feature Generator 示例：Name 挂在 Hypocenter（Point）上，不要挂 Earthquake
			// 否则宿主会对裸 Point 再跑 ILocationNamer（FE 格网 →「日本本州东岸近海」）叠第二行
			// 见 https://docs.eew.cryville.world/docs/feature-generator.html
			if (e.Latitude is double lat && e.Longitude is double lon) {
				var placeName = PlaceName(e);
				var hypo = new Feature(new Point(lon, lat)) {
					{ Is, Hypocenter },
				};
				if (placeName != null) {
					hypo[Name] = placeName;
				}
				if (e.DepthKm is double depth) {
					hypo[HypocenterDepth] = new Quantity(depth, Kilometre);
				}
				var eq = new Feature {
					{ Is, Earthquake },
					{ At, hypo },
				};
				if (e.OriginUtc is DateTime ot) {
					eq[Time] = new DateTimeOffset(DateTime.SpecifyKind(ot, DateTimeKind.Utc));
				}
				if (e.Magnitude is double mag) {
					eq[MagnitudeJMA] = new Quantity(mag, Units.Dimensionless);
				}
				root[Subject] = new Collection<Feature> { eq };
			}

			// 解说：官方详情标题下方是描述（Description/Comment），不是档位名+时间
			LocalizableCollection<string> LocalizeText(string text) {
				var t = text.Trim();
				var block = new LocalizableCollection<string> {
					new Localized<string>(t, culture),
				};
				try {
					var und = SharedCultures.Get("und");
					if (!Equals(und, culture)) {
						block.Add(new Localized<string>(t, und));
					}
				}
				catch {
					// ignore
				}
				try {
					var ja = SharedCultures.Get("ja");
					if (!Equals(ja, culture)) {
						block.Add(new Localized<string>(t, ja));
					}
				}
				catch {
					// ignore
				}
				return block;
			}
			var narrative = JianTitles.TsunamiNarrative(e);
			if (!string.IsNullOrEmpty(narrative)) {
				var block = LocalizeText(narrative);
				root[Description] = block;
				root[Comment] = new Collection<LocalizableCollection<string>> { block };
			}
			return root;
		}


		/// <summary>对齐官方 GenerateFromTsunamiHeight：未満/若干 → [0, h) Interval。</summary>
		static object? TsunamiHeightValue(JianTsunamiArea a) {
			var desc = a.MaxHeightDesc ?? "";
			var kind = a.Kind ?? "";
			var below = kind.Contains("若干", StringComparison.Ordinal)
				|| desc.Contains("未満", StringComparison.Ordinal);
			double? metres = a.MaxHeightM;
			if (metres is null or <= 0) {
				if (!below) {
					return null;
				}
				metres = 0.2;
			}
			var hi = new QuantityInc(metres.Value, 0.05, Units.Metre);
			if (below) {
				return new Interval<QuantityInc>(
					new QuantityInc(0.0, Units.Metre),
					hi,
					IntervalEndpointTypes.RightOpen);
			}
			if (desc.EndsWith("以上", StringComparison.Ordinal) || desc.EndsWith("超", StringComparison.Ordinal)) {
				return new Interval<QuantityInc>(
					hi,
					new QuantityInc(double.PositiveInfinity, Units.Metre),
					desc.EndsWith("超", StringComparison.Ordinal)
						? IntervalEndpointTypes.LeftOpen
						: IntervalEndpointTypes.None);
			}
			return hi;
		}

		static Feature ResolveAreaFeature(JianTsunamiArea a, CultureInfo culture) {
			if (!string.IsNullOrEmpty(a.Code) && JmaOfficialTsunamiAreas.TryGetArea(a.Code, out var official) && official != null) {
				return official;
			}
			var names = new LocalizableCollection<string>();
			if (!string.IsNullOrEmpty(a.Name)) {
				names.Add(new Localized<string>(a.Name, culture));
				try {
					names.Add(new Localized<string>(a.Name, SharedCultures.Get("ja")));
				}
				catch {
					// ignore
				}
			}
			var fallback = new Feature(UnknownGeometry.Instance) {
				{ Is, PlaceInfoArea },
				{ AreaLevel, 4 },
				{ Subject, Tsunami },
			};
			if (!string.IsNullOrEmpty(a.Code)) {
				fallback[Ref] = a.Code;
			}
			if (names.Count > 0) {
				fallback[Name] = names;
			}
			return fallback;
		}

		static double TsunamiSeverity(JianTsunamiArea a) {
			var code = a.KindCode?.Trim() ?? "";
			if (code is "52" or "53") {
				return 1.25;
			}
			if (code.Length >= 1) {
				if (code[^1] == '0') {
					return -1.0;
				}
				switch (code[0]) {
					case '5':
						return 1.0;
					case '6':
						return 0.75;
					case '7':
						return 0.0;
				}
			}
			var kind = a.Kind ?? "";
			if (kind.Contains("大津波", StringComparison.Ordinal) || kind.Contains("大海啸", StringComparison.Ordinal)) {
				return 1.25;
			}
			if (kind.Contains("津波警報", StringComparison.Ordinal) || kind.Contains("海啸警报", StringComparison.Ordinal)) {
				return 1.0;
			}
			if (kind.Contains("注意", StringComparison.Ordinal)) {
				return 0.75;
			}
			if (kind.Contains("予報", StringComparison.Ordinal) || kind.Contains("预报", StringComparison.Ordinal)) {
				return 0.0;
			}
			return -1.0;
		}

		/// <summary>
		/// 海啸：只用官方 Place。须覆盖 UI 文化（zh/und/ja），否则中文界面找不到 Name
		/// 会对裸震中再跑 FE ILocationNamer（「日本本州东岸近海」）。
		/// </summary>
		static object? TsunamiPlaceName(JianEvent e) {
			if (e.Kind != JianKind.Tsunami || string.IsNullOrEmpty(e.Place)) {
				return null;
			}
			var names = new LocalizableCollection<string>();
			void Add(string cultureName) {
				try {
					names.Add(new Localized<string>(e.Place, SharedCultures.Get(cultureName)));
				}
				catch {
					// ignore unsupported culture
				}
			}
			Add("und");
			Add("ja");
			Add("zh");
			Add("zh-CN");
			Add("zh-Hans");
			if (names.Count == 0) {
				names.Add(new Localized<string>(e.Place, SharedCultures.Get("en-US")));
			}
			return names;
		}

		static object? PlaceName(JianEvent e) {
			if (e.Kind == JianKind.Tsunami) {
				return TsunamiPlaceName(e);
			}
			var zh = e.PlaceZh;
			var ja = e.Place;
			if (string.IsNullOrEmpty(zh) && string.IsNullOrEmpty(ja)) {
				return null;
			}
			var names = new LocalizableCollection<string>();
			if (!string.IsNullOrEmpty(zh)) {
				names.Add(new Localized<string>(zh, SharedCultures.Get("zh-CN")));
			}
			if (!string.IsNullOrEmpty(ja) && ja != zh) {
				try {
					names.Add(new Localized<string>(ja, SharedCultures.Get("ja")));
				}
				catch {
					names.Add(new Localized<string>(ja, SharedCultures.Get("en-US")));
				}
			}
			return names;
		}
	}
}
