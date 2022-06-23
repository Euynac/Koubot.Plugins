﻿using System.Drawing;
using Castle.Core.Internal;
using Koubot.SDK.API;
using Koubot.SDK.PluginInterface;
using Koubot.SDK.System;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Extensions;
using Koubot.Tool.Random;
using Koubot.Tool.String;
using SixLabors.ImageSharp.Processing;
using ThoughtWorks.QRCode.Codec;
using ThoughtWorks.QRCode.Codec.Data;

namespace KouFunctionPlugin
{
    [KouPluginClass("img", "图片处理",
        Introduction = "图片系统衍生插件",
        Author = "7zou",
        PluginType = PluginType.Function)]
    public class KouImagePlugin : KouPlugin<KouImagePlugin>
    {
        [KouPluginParameter(ActivateKeyword = "rotate", Name = "图片顺时针旋转(度数)")]
        public double RotateDegree { get; set; }

        [KouPluginParameter(ActivateKeyword = "grey", Name = "变成灰度图")]
        public bool ToGrey { get; set; }

        [KouPluginParameter(ActivateKeyword = "flip", Name = "翻转（默认水平，可选垂直）", DefaultContent = "水平")]
        public FlipType? Flip { get; set; }

        [KouPluginParameter(ActivateKeyword = "resize", Name = "尺寸改变（百分比）",
            Min = 0.01, Max = 2)]
        public double ResizePercent { get; set; }

        [KouPluginParameter(ActivateKeyword = "blur", Name = "高斯模糊", Max = 100, Min = 1, DefaultContent = "3")]
        public double Blur { get; set; }

        [KouPluginParameter(Name = "宽度设置",
            Min = 1, Max = 3000)]
        public int? Width { get; set; }

        [KouPluginParameter(Name = "高度设置",
            Min = 1, Max = 3000)]
        public int? Height { get; set; }

        [KouPluginParameter(ActivateKeyword = "背景颜色", Name = "更改背景颜色")]
        public KouColor? SetBackgroundColor { get; set; }
        [KouPluginParameter(ActivateKeyword = "字体颜色")]
        public KouColor? SetFontColor { get; set; }

        [KouPluginParameter(ActivateKeyword = "透明", Name = "将指定颜色背景转换为透明底", DefaultContent = "白色")]
        public KouColor? ToTransparent { get; set; }

        [KouPluginParameter(ActivateKeyword = "filter", Name = "使用滤镜", Help = "目前有二值化、复古、油画三种")]
        public Filters? UseFilter { get; set; }
        [KouPluginParameter(ActivateKeyword = "png", Name = "保存为png", Authority = Authority.BotManager)]
        public bool SaveToPng { get; set; }
        [KouPluginParameter(ActivateKeyword = "gif", Name = "保存为gif")]
        public bool SaveToGif { get; set; }

        [KouPluginParameter(ActivateKeyword = "jpg", Name = "保存为jpg")]
        public bool SaveToJpg { get; set; }

        [KouPluginParameter(ActivateKeyword = "speed", Name = "设定gif速度(1-300)", Min = 1, Max = 300)]
        public int? SpeedUpGifFactor { get; set; }

        [KouPluginParameter(ActivateKeyword = "fontSize", Name = "字体大小", Max = 100, Min = 1)]
        public int? FontSize { get; set; }

        [KouPluginParameter(ActivateKeyword = "half", Name = "gif帧丢弃一半")]
        public bool HalfGifFrame { get; set; }
        public enum Filters
        {
            None,
            [KouEnumName("二值化")]
            AdaptiveThreshold,
            [KouEnumName("油画")]
            FilterOilPaint,
            [KouEnumName("复古")]
            FilterVignette
        }
        public enum FlipType
        {
            None,
            [KouEnumName("水平", "horizontal")]
            Horizontal,
            [KouEnumName("垂直", "vertical")]
            Vertical
        }

        [KouPluginFunction(Name = "返回使用参数处理后的图片", EnableAutoNext = true)]
        public dynamic? Default(KouImage image)
        {
            if (!CurCommand.ExplicitParameterExecutionList.IsNullOrEmpty())
            {
                var imageToMutate = image;
                if (!image.LocalExists())
                    image.SaveAsTemporary(out imageToMutate);
                if (!ToMutateImage(imageToMutate, out var mutatedImage)) return ConveyMessage;
                return mutatedImage;
            }
            return image;
        }

        [KouPluginFunction(ActivateKeyword = "gqrcode", Name = "生成二维码")]
        public object GenerateQRCode(string content)
        {
            var encoder = new QRCodeEncoder();
            var img = encoder.Encode(content);
            using var tmpStream = new MemoryStream();
            img.Save(tmpStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            tmpStream.Seek(0, SeekOrigin.Begin);
            return new KouMutateImage(tmpStream).SaveTemporarily();
        }
        [KouPluginFunction(ActivateKeyword = "pqrcode", Name = "解析二维码", EnableAutoNext = true)]
        public object ParseQRCode(KouImage code)
        {
            var imageToMutate = code;
            if (!code.LocalExists())
                code.SaveAsTemporary(out imageToMutate);
            if(imageToMutate == null) return "图片保存失败";
            using var mutating = imageToMutate.StartMutate()!;
            var saved = mutating.SaveTemporarily(null, KouImageFormat.Bmp);
            var encoder = new QRCodeDecoder();
            return encoder.decode(new QRCodeBitmapImage(new Bitmap(saved.FileUri.LocalPath)));
        }

        [KouPluginFunction(ActivateKeyword = "info", Name = "图片信息", EnableAutoNext = true)]
        public string? Info(KouImage image)
        {
            var url = image.IsNetworkFile ? image.FileUri.ToString() : null;
            image.SaveAsTemporary(out var temporaryImage);
            if (temporaryImage == null)
            {
                if (image.ErrorCode == KouImage.Error.NetworkFileSizeTooBig)
                    return image.ErrorMsg;
                return "读取图片失败";
            }

            using var mutateImage = temporaryImage.StartMutate();
            return $"{url?.Be($"网络路径：{url}\n")}{mutateImage?.ToString()}";
        }

        [KouPluginFunction(ActivateKeyword = "文字转图片", Name = "文字转图片", SupportedParameters = new []{nameof(FontSize), nameof(SetBackgroundColor), nameof(SetFontColor)})]
        public object StringToImg([KouPluginArgument(Name = "要转换的内容")] string content)
        {
            if (content.Length > 1000) return "暂不支持过大的内容";
            var fontSize = FontSize ?? 10;
            using var img = new KouMutateImage(content, new KouMutateImage.KouTextOptions(){FontSize = fontSize});
            return img.SaveTemporarily();
        }


        [KouPluginFunction(ActivateKeyword = "frame", Name = "获取图片帧", EnableAutoNext = true)]
        public object? FrameCount([KouPluginArgument(Name = "指定帧数")] int at, [KouPluginArgument(Name = "GIF图片")] KouImage image)
        {
            var imageToMutate = image;
            if (!image.LocalExists())
                image.SaveAsTemporary(out imageToMutate);
            if (!ToMutateImage(imageToMutate!, out var mutatedImage)) return ConveyMessage;
            using var mutating = imageToMutate!.StartMutate();
            if (mutating == null) return "mutate image failed";
            var frames = mutating.GetFrames().ToList();
            if (at == 0) return frames.RandomGetOne().SaveTemporarily();
            var frame = frames.ElementAtOrDefault(at - 1);
            if (frame == null) return $"无法获取第{at}帧，该图片总共有{frames.Count}帧";
            return frame.SaveTemporarily();
        }

        [KouPluginFunction(ActivateKeyword = "变透明gif", Name = "将透明的png变成透明gif", EnableAutoNext = true)]
        public object? PngToTwoFrameGifToBeTransparent([KouPluginArgument(Name = "透明图片")] KouImage image)
        {
            var imageToMutate = image;
            if (!image.LocalExists())
                image.SaveAsTemporary(out imageToMutate);
            if (!ToMutateImage(imageToMutate!, out var mutatedImage)) return ConveyMessage;
            using var mutating = mutatedImage!.StartMutate();
            if (mutating == null) return "mutate image failed";
            //mutating.ImageBuffer.Frames.AddFrame(mutating.ImageBuffer.Frames.First());
            return mutating.SaveTemporarily(100, KouImageFormat.Gif);
        }


        [KouPluginFunction(ActivateKeyword = "ocr", Name = "OCR提取图片中的文字", EnableAutoNext = true, NeedCoin = 100)]
        public object? OCR([KouPluginArgument(Name = "包含文字的图片")] KouImage image)
        {
            if (!image.SaveAsTemporary(out var savedLocalImage)) return "保存为临时图片失败";
            using var mutate = savedLocalImage.StartMutate();
            var base64 = mutate?.ToBase64String();
            if(base64 == null) return "图片转b64失败";
            var result = TencentOcrAPI.Call(base64);
            if (result == null)
            {
                return "OCR服务调用失败";
            }

            return result;

        }

        private bool ToMutateImage(KouImage image, out KouImage mutatedImage)
        {
            mutatedImage = null;
            var defaultSaveFormat = KouImageFormat.Jpeg;
            using var mutateImage = image.StartMutate();
            if (mutateImage == null) return ReturnConveyError("无法处理图片");
            if (RotateDegree != 0)
            {
                RotateDegree %= 360;
                mutateImage.Rotate(RotateDegree);
            }

            if (ToGrey)
            {
                mutateImage.FilterGreyscale();
            }

            if (SpeedUpGifFactor != null)
            {
                mutateImage.GifEachFrameDelay(SpeedUpGifFactor.Value);
                defaultSaveFormat = KouImageFormat.Gif;
            }

            if (Flip != null)
            {
                mutateImage.Flip(Flip == FlipType.Vertical ? FlipMode.Vertical : FlipMode.Horizontal);
            }

            if (ResizePercent != 0)
            {
                mutateImage.Resize(ResizePercent);
            }

            if (HalfGifFrame)
            {
                mutateImage.GifHalfDiscardFrame();
                defaultSaveFormat = KouImageFormat.Gif;
            }
            if (Width != null || Height != null)
            {
                var setWidth = Width ?? mutateImage.Width;
                var setHeight = Height ?? mutateImage.Height;
                mutateImage.Resize(setWidth, setHeight);
            }

            if (UseFilter != null)
            {
                switch (UseFilter)
                {
                    case Filters.AdaptiveThreshold:
                        mutateImage.FilterAdaptiveThreshold();
                        break;
                    case Filters.FilterOilPaint:
                        mutateImage.FilterOilPaint();
                        break;
                    case Filters.FilterVignette:
                        mutateImage.FilterVignette();
                        break;
                }
            }
            if (Blur != 0)
            {
                mutateImage.FilterGaussianBlur(Blur);
            }
            if (ToTransparent != null)
            {
                var source= ToTransparent.Value;
                mutateImage.BackgroundToTransparent(source);
                defaultSaveFormat = KouImageFormat.Gif;
            }

            if (SetBackgroundColor != null)
            {
                mutateImage.SetBackgroundColor(SetBackgroundColor.Value);
            }
         


            var format = SaveToGif ? KouImageFormat.Gif : SaveToPng ? KouImageFormat.Png : SaveToJpg ? KouImageFormat.Jpeg : defaultSaveFormat;
            mutatedImage = mutateImage.SaveTemporarily(format: format);

            return true;
        }
    }
}