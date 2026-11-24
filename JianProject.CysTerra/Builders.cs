using Cryville.Common.Compat;
using Cryville.EEW.ComponentModel;
using Cryville.EEW.Report;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cryville.EEW;

namespace JianProject.CysTerra {
	[Export(typeof(IBuilder<ISourceWorker>))]
	public sealed class JianWsWorkerBuilder : IBuilder<JianWsWorker>, INotifyPropertyChanged {
		public string? GetName([NotNull] ref CultureInfo? culture) => SharedResources.SourceName(ref culture);

		/// <summary>固定订阅 /all；不在设置 UI 展示（对齐 FAN Studio）。</summary>
		[JsonSerializedProperty]
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public string? WebSocketUri { get; set; } = "wss://api.sismotide.top/all";

		[JsonSerializedProperty]
		[LocalizableDisplayName("PNAccessKey")]
		[LocalizableDescription("PDAccessKey")]
		public string? AccessKey { get; set; }

		[JsonSerializedProperty]
		[LocalizableDisplayName("PNEndpointMode")]
		public JianEndpointMode EndpointMode { get; set; } = JianEndpointMode.Auto;

		public event PropertyChangedEventHandler? PropertyChanged;

		public JianWsWorker Build(ref CultureInfo? culture) {
			var uriText = string.IsNullOrWhiteSpace(WebSocketUri) ? "wss://api.sismotide.top/all" : WebSocketUri;
			if (!Uri.TryCreate(uriText, UriKind.Absolute, out var baseUri)
				|| (baseUri.Scheme != "ws" && baseUri.Scheme != "wss")) {
				throw new SourceWorkerClientException("Invalid WebSocket URI: " + uriText);
			}
			if (string.IsNullOrWhiteSpace(AccessKey)) {
				throw new SourceWorkerClientException("认证密钥不能为空。");
			}
			JianEndpoint.Reset(EndpointMode);
			baseUri = JianEndpoint.Rewrite(baseUri, EndpointMode);
			return new JianWsWorker(baseUri, AccessKey.Trim(), EndpointMode, refresh => {
				if (string.Equals(AccessKey, refresh, StringComparison.Ordinal)) {
					return;
				}
				AccessKey = refresh;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccessKey)));
			});
		}
	}

	/// <summary>按数据源导出独立事件摘要构建器。</summary>
	public abstract class JianReportSourceBuilder : IBuilder<IGenerator<ReportModel>> {
		protected abstract JianSource Source { get; }

		public string? GetName([NotNull] ref CultureInfo? culture) =>
			SharedResources.SourceName(Source.ToString(), ref culture);

		public IGenerator<ReportModel> Build(ref CultureInfo? culture) =>
			(IGenerator<ReportModel>)(IGenerator<ReportModel?>)new JianReportGenerator(Source);
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianCeaReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Cea;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianCeaPrReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.CeaPr;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianCwaEewReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.CwaEew;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianJmaEewReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.JmaEew;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianJmaTsunamiReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.JmaTsunami;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianSaReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Sa;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianKmaEewReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.KmaEew;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianCencReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Cenc;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianCencFjdzjReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.CencFjdzj;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianNingxiaReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Ningxia;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianYunnanReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Yunnan;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianShanxiReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Shanxi;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianBeijingReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Beijing;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianCwaReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Cwa;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianJmaReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Jma;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianHkoReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Hko;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianKmaReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Kma;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianTmdReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Tmd;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianMmdReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Mmd;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianBmkgReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Bmkg;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianGeonetReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Geonet;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianUsgsReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Usgs;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianEmscReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Emsc;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianGfzReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Gfz;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianBcsfReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Bcsf;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianIngvReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Ingv;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianUspReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Usp;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianAfadReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Afad;
	}

	[Export(typeof(IBuilder<IGenerator<ReportModel>>))]
	public sealed class JianGaReportBuilder : JianReportSourceBuilder {
		protected override JianSource Source => JianSource.Ga;
	}
}
