namespace ScreenToGif.Domain.Models;

public class Property
{
    public string NameSpace { get; set; }
        
    public string Key { get; set; }
        
    public string Type { get; set; }

    public string Value { get; set; }
        
    public List<Property> Attributes { get; set; } = new();
        
    public List<Property> Children { get; set; } = new();


    public override string ToString()
    {
        return $"NameSpace: {NameSpace}" + Environment.NewLine +
               $"Key: {Key} " + Environment.NewLine +
               $"Type: {Type} " + Environment.NewLine +
               $"Value: {Value} " + Environment.NewLine +
               (Attributes?.Count > 0 ? ("Attributes >>>> " + Environment.NewLine) : "") +
               (Attributes?.Count > 0 ? ($"{Attributes?.Aggregate("", (p, n) => p + (p.Length > 0 ? Environment.NewLine : "") + n)} " + Environment.NewLine) : "") +
               (Attributes?.Count > 0 ? ("End attributes <<<< " + Environment.NewLine) : "") +
               (Children?.Count > 0 ? ("Children >>>> " + Environment.NewLine) : "") +
               (Children?.Count > 0 ? ($"Children: {Children?.Aggregate("", (p, n) => p + (p.Length > 0 ? Environment.NewLine : "") + n)} " + Environment.NewLine) : "") +
               (Children?.Count > 0 ? ("End children <<<< " + Environment.NewLine) : "");
    }
}