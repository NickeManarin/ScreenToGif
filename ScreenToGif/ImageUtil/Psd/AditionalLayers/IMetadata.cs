namespace ScreenToGif.ImageUtil.Psd.AditionalLayers
{
    interface IMetadata : IAditionalLayerInfo
    {
        string Signature { get; }

        bool CopyOnSheetDuplication { get; }
    }
}