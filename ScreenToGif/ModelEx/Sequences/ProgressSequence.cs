namespace ScreenToGif.ModelEx.Sequences
{
    public class ProgressSequence : SizeableSequence
    {
        public enum Modes : int
        {
            Test,
            Bar
        }

        public Modes ProgressMode { get; set; }

        //Color.
        //Bar percentage.
        //How to calculate the correct text to be shown?


        public ProgressSequence()
        {
            Type = Types.Progress;
        }
    }
}