using Cryville.EEW.Features;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace JianProject.CysTerra.Features {
	/// <summary>
	/// 运行时复用官方 Cryville.EEW.JMA.Features 的预报区几何（依赖用户已安装 JMA.Map + JMA.Features）。
	/// 对齐 JMAAtom.Features 的 JMAAreaTsunamiFeatures.Instance 用法。
	/// </summary>
	internal static class JmaOfficialTsunamiAreas {
		static bool _tried;
		static IReadOnlyDictionary<string, Feature>? _areas;
		static Feature? _outline;

		public static Feature? Outline {
			get {
				Ensure();
				return _outline;
			}
		}

		public static bool TryGetArea(string code, out Feature? feature) {
			Ensure();
			feature = null;
			if (string.IsNullOrEmpty(code) || _areas == null) {
				return false;
			}
			if (_areas.TryGetValue(code, out var f)) {
				feature = f;
				return true;
			}
			return false;
		}

		static void Ensure() {
			if (_tried) {
				return;
			}
			_tried = true;
			try {
				Type? type = null;
				foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
					type = asm.GetType("Cryville.EEW.JMA.Features.JMAAreaTsunamiFeatures");
					if (type != null) {
						break;
					}
				}
				if (type == null) {
					foreach (var asmName in new[] {
						"Cryville.EEW.JMA.Features",
						"Cryville.EEW.JMA.Features, Version=0.8.2.0, Culture=neutral, PublicKeyToken=null",
					}) {
						try {
							var asm = Assembly.Load(asmName);
							type = asm.GetType("Cryville.EEW.JMA.Features.JMAAreaTsunamiFeatures");
							if (type != null) {
								break;
							}
						}
						catch {
							// 未安装官方扩展
						}
					}
				}
				if (type == null) {
					return;
				}
				var instProp = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
				var inst = instProp?.GetValue(null);
				if (inst == null) {
					return;
				}
				var areasProp = type.GetProperty("Areas", BindingFlags.Public | BindingFlags.Instance);
				var outlineProp = type.GetProperty("Outline", BindingFlags.Public | BindingFlags.Instance);
				if (areasProp?.GetValue(inst) is IEnumerable raw) {
					var map = new Dictionary<string, Feature>(StringComparer.Ordinal);
					foreach (var item in raw) {
						if (item == null) {
							continue;
						}
						// KeyValuePair<string, Feature> or dictionary entry
						var itemType = item.GetType();
						var keyProp = itemType.GetProperty("Key");
						var valProp = itemType.GetProperty("Value");
						if (keyProp == null || valProp == null) {
							continue;
						}
						var key = keyProp.GetValue(item) as string;
						if (valProp.GetValue(item) is Feature feat && !string.IsNullOrEmpty(key)) {
							map[key] = feat;
						}
					}
					if (map.Count > 0) {
						_areas = map;
					}
				}
				_outline = outlineProp?.GetValue(inst) as Feature;
			}
			catch {
				_areas = null;
				_outline = null;
			}
		}
	}
}
