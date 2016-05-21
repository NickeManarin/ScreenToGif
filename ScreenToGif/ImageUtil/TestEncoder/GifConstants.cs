using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.ImageUtil.TestEncoder
{
    internal class GifConstants
    {
        /// <summary>
        /// Extension Introducer.
        /// </summary>          
        internal const byte ExtensionIntroducer = 0x21;

        /// <summary>
        /// Block Terminator
        /// </summary>
        internal const byte Terminator = 0;

        /// <summary>
        /// Application Extension Label.
        /// </summary>
        internal const byte ApplicationExtensionLabel = 0xFF;

        /// <summary>
        /// Comment Label.
        /// </summary>
        internal const byte CommentLabel = 0xFE;

        /// <summary>
        /// Image Descriptor Label.
        /// </summary>
        internal const byte ImageDescriptorLabel = 0x2C;

        /// <summary>
        /// Plain Text Label.
        /// </summary>
        internal const byte PlainTextLabel = 0x01;

        /// <summary>
        /// Graphic Control Label.
        /// </summary>
        internal const byte GraphicControlLabel = 0xF9;

        /// <summary>
        /// End of file.
        /// </summary>
        internal const byte EndIntroducer = 0x3B;
    }
}
