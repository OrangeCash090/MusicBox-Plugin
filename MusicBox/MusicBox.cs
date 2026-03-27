using MusicBox.Utils;
using OnixRuntime.Plugin;

namespace MusicBox {
	public class MusicBox : OnixPluginBase {
		public static MusicBox Instance { get; private set; } = null!;
		public static MusicBoxConfig Config { get; private set; } = null!;

		private MidiPlayer? _player;

		public MusicBox(OnixPluginInitInfo initInfo) : base(initInfo) {
			Instance = this;
			base.DisablingShouldUnloadPlugin = false;
			
			#if DEBUG
			//base.WaitForDebuggerToBeAttached();
			#endif
		}

		protected override void OnLoaded() {
			Config = new MusicBoxConfig(PluginDisplayModule, true);
			AssetHelper.AssetPath = PluginAssetsPath + "\\";
			
			_player = new MidiPlayer();
			_player.Initialize(Config);
		}

		protected override void OnEnabled() { }

		protected override void OnDisabled() { }

		protected override void OnUnloaded() {
			_player?.Dispose();
		}
	}
}

// ideas:
// piano viewer
// midi input connector and visualizer (naudio)