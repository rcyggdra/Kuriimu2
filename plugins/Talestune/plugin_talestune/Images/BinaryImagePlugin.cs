using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_talestune.Images
{
    public class BinaryImagePlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("1F93EA6E-D04A-46DD-9396-10160285A8CF");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public BinaryImagePlugin()
        {
            Metadata = new PluginMetadata("BinaryImage", "LITTOMA", "Binary image format in Petit Noval Series");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            BinaryImage image = new BinaryImage();
            var fs = await fileSystem.OpenFileAsync(filePath);
            try
            {
                image.Load(fs);
                return true;
            }catch (Exception ex)
            {
                return false;
            }
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new BinaryImageState();
        }
    }
}
