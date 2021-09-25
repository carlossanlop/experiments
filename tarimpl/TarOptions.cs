using System.Text;

namespace tarimpl
{
    public class TarOptions
    {
        public TarMode Mode { get; init; } = TarMode.Read;
        public bool LeaveOpen { get; init; } = false;
        public Encoding EntryNameEncoding { get; init; } = Encoding.ASCII; 

        public TarOptions()
        {
        }
    }
}
