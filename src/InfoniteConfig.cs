using Newtonsoft.Json;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace Infonite {
    [ConfigFile]
    public class InfoniteConfig {
        private bool discover;

        private bool refresherator;

        private InfoniteConfig() {
            discover = false;
            refresherator = true;
        }

        public static InfoniteConfig Instance { get; protected internal set; } = new InfoniteConfig();

        [JsonProperty]
        [Option("Discover", "Discover all possible content on construction")]
        public bool Discover {
            get => discover;
            set {
                discover = value;
                POptions.WriteSettings(this);
            }
        }

        public static void OnLoad()
        {
            PUtil.InitLibrary();
            var config = POptions.ReadSettings<InfoniteConfig>();
            if (config == null)
            {
                config = new InfoniteConfig();
                POptions.WriteSettings(config);
            }

            Instance = config;
        }

        [JsonProperty]
        [Option("Refresherator", "Prevent rot in the infinite fridge")]
        public bool Refresherator {
            get => refresherator;
            set {
                refresherator = value;
                POptions.WriteSettings(this);
            }
        }
    }
}