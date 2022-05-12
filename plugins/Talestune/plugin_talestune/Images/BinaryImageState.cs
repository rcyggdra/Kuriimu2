using Kanvas;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace plugin_talestune.Images
{
    public class BinaryImageState : IImageState, ILoadFiles, ISaveFiles
    {
        private BinaryImage image;
        public bool ContentChanged => Images.Any(x => x.ImageInfo.ContentChanged);

        public BinaryImageState()
        {
            image = new BinaryImage();
        }

        public EncodingDefinition EncodingDefinition => BinaryImageSupport.GetEncodingDefinition();

        public IList<IKanvasImage> Images { get; private set; }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = new List<IKanvasImage>() { new KanvasImage(EncodingDefinition, image.Load(fileStream)) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            image.Save(fileStream, Images[0].ImageInfo);

            return Task.CompletedTask;
        }
    }
}