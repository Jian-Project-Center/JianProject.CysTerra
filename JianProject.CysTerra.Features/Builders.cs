using Cryville.EEW.ComponentModel;
using Cryville.EEW.Features;
using JianProject.CysTerra;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cryville.EEW;

namespace JianProject.CysTerra.Features {
	/// <summary>按数据源导出独立事件详情构建器。</summary>
	public abstract class JianFeatureSourceBuilder : IBuilder<IGenerator<Feature>> {
		protected abstract JianSource Source { get; }

		public string? GetName([NotNull] ref CultureInfo? culture) =>
			SharedResources.SourceName(Source.ToString(), ref culture);

		public IGenerator<Feature> Build(ref CultureInfo? culture) =>
			(IGenerator<Feature>)(IGenerator<Feature?>)new JianFeatureGenerator(Source);
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianCeaFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Cea;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianCeaPrFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.CeaPr;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianCwaEewFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.CwaEew;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianJmaEewFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.JmaEew;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianJmaTsunamiFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.JmaTsunami;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianSaFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Sa;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianKmaEewFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.KmaEew;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianCencFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Cenc;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianCencFjdzjFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.CencFjdzj;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianNingxiaFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Ningxia;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianYunnanFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Yunnan;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianShanxiFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Shanxi;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianBeijingFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Beijing;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianCwaFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Cwa;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianJmaFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Jma;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianHkoFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Hko;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianKmaFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Kma;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianTmdFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Tmd;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianMmdFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Mmd;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianBmkgFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Bmkg;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianGeonetFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Geonet;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianUsgsFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Usgs;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianEmscFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Emsc;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianGfzFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Gfz;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianBcsfFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Bcsf;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianIngvFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Ingv;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianUspFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Usp;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianAfadFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Afad;
	}

	[Export(typeof(IBuilder<IGenerator<Feature>>))]
	public sealed class JianGaFeatureBuilder : JianFeatureSourceBuilder {
		protected override JianSource Source => JianSource.Ga;
	}
}
