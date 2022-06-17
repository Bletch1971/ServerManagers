using ServerManagerTool.Common.Model;

namespace ServerManagerTool.Lib.ViewModel
{
    public class GameDataFileList : SortableObservableCollection<GameDataFile>
    {
        public override string ToString()
        {
            return $"{nameof(GameDataFile)} - {Count}";
        }
    }
}
