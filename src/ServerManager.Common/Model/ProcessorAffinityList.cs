using ServerManagerTool.Common.Utils;
using System.Linq;
using System.Numerics;

namespace ServerManagerTool.Common.Model
{
    public class ProcessorAffinityList : SortableObservableCollection<ProcessorAffinityItem>
    {
        private bool _allProcessors;

        public ProcessorAffinityList(BigInteger affinityValue)
        {
            AllProcessors = true;
            PopulateAffinities(affinityValue);
        }

        public bool AllProcessors
        {
            get { return this._allProcessors; }
            set
            {
                this._allProcessors = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(AllProcessors)));
            }
        }

        public BigInteger AffinityValue
        {
            get
            {
                if (AllProcessors || this.Count(i => i.Selected) == this.Count)
                    return BigInteger.Zero;

                var affinity = BigInteger.Zero;
                foreach (var value in this.Where(i => i.Selected).Select(i => i.AffinityValue))
                    affinity += value;
                return affinity;
            }
        }

        private void PopulateAffinities(BigInteger affinityValue)
        {
            var list = ProcessUtils.GetProcessorAffinityList();
            var index = 0;

            if (!ProcessUtils.IsProcessorAffinityValid(affinityValue))
                affinityValue = BigInteger.Zero;

            foreach (var item in list)
            {
                if (item == 0)
                    continue;

                this.Add(new ProcessorAffinityItem() { Selected = affinityValue == BigInteger.Zero || ((affinityValue & item) == item), AffinityValue = item, Description = $"{index}" });
                index++;
            }

            var affinity = BigInteger.Zero;
            if (this.Count(i => i.Selected) != this.Count)
            {
                foreach (var value in this.Where(i => i.Selected).Select(i => i.AffinityValue))
                    affinity += value;
            }
            AllProcessors = affinity == BigInteger.Zero;
        }
    }
}
