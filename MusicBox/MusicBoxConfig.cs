using OnixRuntime.Api.Inputs;
using OnixRuntime.Api.OnixClient;
namespace MusicBox {
    public partial class MusicBoxConfig : OnixModuleSettingRedirector {
	    [Value("None")]
	    [Name("Song Directory", "The Directory which holds the MIDI files you want to select from.")]
	    public partial OnixTextbox SongDirectory { get; set; }
	    
	    [Value(InputKey.Type.Ctrl)]
	    [Name("Toggle UI Key", "The key used to toggle between the MusicBox UI and the Game UI. Default is Ctrl.")]
	    public partial InputKey ToggleUIKey { get; set; }
    }
}