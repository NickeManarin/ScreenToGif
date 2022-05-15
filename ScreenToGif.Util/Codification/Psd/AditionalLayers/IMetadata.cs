namespace ScreenToGif.Util.Codification.Psd.AdditionalLayers;

interface IMetadata : IAdditionalLayerInfo
{
    string Signature { get; }

    bool CopyOnSheetDuplication { get; }
}