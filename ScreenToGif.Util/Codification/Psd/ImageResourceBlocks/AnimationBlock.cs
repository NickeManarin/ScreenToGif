using System.IO;
using System.Text;

namespace ScreenToGif.Util.Codification.Psd.ImageResourceBlocks;

internal class AnimationBlock : IImageResource
{
    public List<int> FrameDelays { get; set; } = new();

    public ushort Identifier { get; set; } = 4000;

    public string Name { get; set; } = "";

    public byte[] Content
    {
        get
        {
            using (var stream = new MemoryStream())
            {
                stream.WriteBytes(Encoding.ASCII.GetBytes("8BIM")); //Chunk type, 4 bytes.
                stream.WriteUInt16(BitHelper.ConvertEndian(Identifier)); //Image Resource Id, 2 bytes.
                stream.WritePascalString(Name); //Image Resource Name, pascal string (length + string + padding).

                stream.WriteUInt32(BitHelper.ConvertEndian((uint)999)); //Unpadded size, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("8BIM")); //Chunk signature, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("AnDs")); //Chunk type, 4 bytes.

                stream.WriteUInt32(BitHelper.ConvertEndian((uint)EncodedObject.Length)); //The length of the following data, 4 bytes.
                stream.WriteBytes(EncodedObject);  //ImageResource data, XX bytes.

                return stream.ToArray();
            }
        }
    }

    private byte[] EncodedObject
    {
        get
        {
            using (var stream = new MemoryStream())
            {
                stream.WriteUInt32(BitHelper.ConvertEndian((uint)16)); //Descriptor version, 4 bytes.
                stream.WriteUInt32(BitHelper.ConvertEndian((uint)1)); //UnknownField1, 4 bytes.
                stream.WriteUInt32(BitHelper.ConvertEndian((uint)0)); //UnknownField2, 4 bytes.
                stream.WriteUInt16(BitHelper.ConvertEndian((ushort)0)); //UnknownField3, 2 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("null")); //NullField, 4 bytes.
                stream.WriteUInt32(BitHelper.ConvertEndian((uint)3)); //NumberOfProperties, 4 bytes.

                //AfSt
                stream.WriteUInt32(BitHelper.ConvertEndian((uint)0)); //NullField, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("AfSt")); //Signature, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("long")); //Value type, 4 bytes.
                stream.WriteUInt32(BitHelper.ConvertEndian((uint)0)); //Value, 4 bytes.

                #region FrIn

                stream.WriteUInt32(BitHelper.ConvertEndian((uint)0)); //NullField, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("FrIn")); //Signature, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("VlLs")); //Value type, 4 bytes.

                //FrIn -> VlLs (List)
                stream.WriteUInt32(BitHelper.ConvertEndian((uint)FrameDelays.Count)); //Number of entries of the list (frames), 4 bytes.

                foreach (var frame in FrameDelays)
                {
                    //FrIn -> VlLs (List) -> Objc
                    stream.WriteBytes(Encoding.ASCII.GetBytes("Objc")); //Value type, 4 bytes.
                    stream.WriteUInt32(BitHelper.ConvertEndian((uint)1)); //UnknownField1, 4 bytes.
                    stream.WriteUInt32(BitHelper.ConvertEndian((uint)0)); //UnknownField2, 4 bytes.
                    stream.WriteUInt16(BitHelper.ConvertEndian((ushort)0)); //UnknownField3, 2 bytes.
                    stream.WriteBytes(Encoding.ASCII.GetBytes("null")); //NullField, 4 bytes.
                    stream.WriteUInt32(BitHelper.ConvertEndian((uint)3)); //NumberOfProperties, 4 bytes.
                    stream.WriteBytes(Encoding.ASCII.GetBytes("    ")); //NullField, 4 bytes.

                    //FrIn -> VlLs (List) -> Objc -> FrID
                    stream.WriteBytes(Encoding.ASCII.GetBytes("FrID")); //Signature, 4 bytes.
                    stream.WriteBytes(Encoding.ASCII.GetBytes("long")); //Value type, 4 bytes.
                    stream.WriteUInt32(BitHelper.ConvertEndian((uint)1263123368)); //Value, 4 bytes. //TODO

                    //FrIn -> VlLs (List) -> Objc -> FrDl
                    stream.WriteBytes(Encoding.ASCII.GetBytes("FrDl")); //Signature, 4 bytes.
                    stream.WriteBytes(Encoding.ASCII.GetBytes("long")); //Value type, 4 bytes.
                    stream.WriteUInt32(BitHelper.ConvertEndian((uint)7)); //Value, 4 bytes. //TODO

                    //FrIn -> VlLs (List) -> Objc -> FrGA
                    stream.WriteBytes(Encoding.ASCII.GetBytes("FrDl")); //Signature, 4 bytes.
                    stream.WriteBytes(Encoding.ASCII.GetBytes("long")); //Value type, 4 bytes.
                    stream.WriteInt64(BitHelper.ConvertEndian((long)30)); //Value, 4 bytes. //TODO
                }

                #endregion

                #region FSts

                stream.WriteUInt32(BitHelper.ConvertEndian((uint)0)); //NullField, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("FSts")); //Signature, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("VlLs")); //Value type, 4 bytes.

                //FSts -> VlLs (List)
                stream.WriteUInt32(BitHelper.ConvertEndian((uint)1)); //Number of entries of the list, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("Objc")); //Value type, 4 bytes.
                stream.WriteUInt32(BitHelper.ConvertEndian((uint)1)); //UnknownField1, 4 bytes.
                stream.WriteUInt32(BitHelper.ConvertEndian((uint)0)); //UnknownField2, 4 bytes.
                stream.WriteUInt16(BitHelper.ConvertEndian((ushort)0)); //UnknownField3, 2 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("null")); //NullField, 4 bytes.
                stream.WriteUInt32(BitHelper.ConvertEndian((uint)4)); //NumberOfProperties, 4 bytes.

                //FSts -> VlLs (List) -> Objc -> FsID
                stream.WriteBytes(Encoding.ASCII.GetBytes("    ")); //NullField, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("FsID")); //Signature, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("long")); //Value type, 4 bytes.
                stream.WriteUInt32(BitHelper.ConvertEndian((uint)0)); //Value, 4 bytes. //TODO

                //FSts -> VlLs (List) -> Objc -> AFrm
                stream.WriteBytes(Encoding.ASCII.GetBytes("    ")); //NullField, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("AFrm")); //Signature, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("long")); //Value type, 4 bytes.
                stream.WriteUInt32(BitHelper.ConvertEndian((uint)3)); //Value, 4 bytes. //TODO

                //FSts -> VlLs (List) -> Objc -> FsFr
                stream.WriteBytes(Encoding.ASCII.GetBytes("    ")); //NullField, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("FsFr")); //Signature, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("VlLs")); //Value type, 4 bytes.

                //FSts -> VlLs (List) -> Objc -> FsFr -> VlLs (List)
                stream.WriteUInt32(BitHelper.ConvertEndian((uint)FrameDelays.Count)); //Number of entries of the list (frames), 4 bytes.

                foreach (var frame in FrameDelays) //Frame IDs
                {
                    stream.WriteBytes(Encoding.ASCII.GetBytes("long")); //Value type, 4 bytes.
                    stream.WriteUInt32(BitHelper.ConvertEndian((uint)1263123368)); //Value, 4 bytes. //TODO

                    //1263123368
                    //1263140175
                    //1263156982
                    //1263173789
                }

                //FSts -> VlLs (List) -> Objc -> LCnt
                stream.WriteBytes(Encoding.ASCII.GetBytes("    ")); //NullField, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("LCnt")); //Signature, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes("long")); //Value type, 4 bytes.
                stream.WriteUInt32(BitHelper.ConvertEndian((uint)1)); //Value, 4 bytes. //TODO

                #endregion

                return stream.ToArray();
            }
        }
    }

    public AnimationBlock()
    {
        //Get data from all frames and generate this block.
    }
}