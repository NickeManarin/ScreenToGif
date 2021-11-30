namespace ScreenToGif.Util.Codification.Psd.AditionalLayers;

interface IMetadata : IAditionalLayerInfo
{
    string Signature { get; }

    bool CopyOnSheetDuplication { get; }
}