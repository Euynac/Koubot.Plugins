using Castle.Core.Internal;
using System;
using Koubot.Tool.Extensions;

namespace KouFunctionPlugin.Pixiv
{
    public partial class PixivWork
    {
        public enum SizeType
        {
            Original,
            Regular,
            /// <summary>
            /// 540x540
            /// </summary>
            Small,
            /// <summary>
            /// 250x250
            /// </summary>
            Thumb,
            /// <summary>
            /// 48x48
            /// </summary>
            Mini,
        }

        /// <summary>
        /// Get current work img url.
        /// </summary>
        /// <param name="type">Size type of img</param>
        /// <param name="proxy">Proxy to fetch img resource</param>
        /// <param name="square">if crop to square or not</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public string GetUrl(SizeType type = SizeType.Small, bool square = false, string proxy = "i.pixiv.cat")
        {
            var datePath = UploadDate.ToString("yyyy/MM/dd/HH/mm/ss");
            var resizeDsc = square ? "square" : "master";
            var extension = type == SizeType.Original ? Ext : "jpg";
            var sizeDsc = "";
            var imgSizeTypeDsc = type == SizeType.Original ? "img-original" : "img-master";
            switch (type)
            {
                case SizeType.Original:
                case SizeType.Regular:
                    sizeDsc = imgSizeTypeDsc;
                    break;
                case SizeType.Small:
                    sizeDsc = $"c/540x540_70/{imgSizeTypeDsc}";
                    break;
                case SizeType.Thumb:
                    sizeDsc = $"c/250x250_80_a2/{imgSizeTypeDsc}";
                    break;
                case SizeType.Mini:
                    sizeDsc = $"c/48x48/{imgSizeTypeDsc}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            var suffixAppend = resizeDsc.IsNullOrEmpty() ? "" : $"_{resizeDsc}1200";
            return
                $"https://{proxy}/{sizeDsc}/img/{datePath}/{Pid}_p{P}{suffixAppend}.{extension}";
        }
    }
}