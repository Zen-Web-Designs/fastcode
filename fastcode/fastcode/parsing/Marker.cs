namespace fastcode.parsing
{
    //really could be a struct, but I'm more confortable setting it up this way.
    public class Marker
    {
        public int Index { get; set; }
        public int Row { get; set; }    //keep track of row and collumn for debugging (syntax hilighting).
        public int Collumn { get; set; }

        public Marker(int index, int col, int row)
        {
            this.Index = index;
            this.Collumn = col;
            this.Row = row;
        }
    }
}
