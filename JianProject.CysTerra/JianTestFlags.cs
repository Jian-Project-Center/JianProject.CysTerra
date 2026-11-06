namespace JianProject.CysTerra {
	/// <summary>
	/// 本地手测开关。测完海啸解说后务必将 IgnoreTsunamiExpire 改回 false。
	/// </summary>
	public static class JianTestFlags {
		/// <summary>true：忽略海啸 endTime，快照仍灌库、详情不过期。</summary>
		public const bool IgnoreTsunamiExpire = false;
	}
}
