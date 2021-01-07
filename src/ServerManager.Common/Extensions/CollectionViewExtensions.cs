using System.ComponentModel;

namespace ServerManagerTool.Common.Extensions
{
    public static class CollectionViewExtensions
    {
        public static void ToggleSorting(this ICollectionView view, string property, ListSortDirection defaultDirection = ListSortDirection.Ascending)
        {
            for (int i = 0; i < view.SortDescriptions.Count; i++)
            {
                var sortDescription = view.SortDescriptions[i];
                if (sortDescription.PropertyName == property)
                {
                    view.SortDescriptions.RemoveAt(i);
                    return;
                }
            }

            view.SortDescriptions.Add(new SortDescription() { PropertyName = property, Direction = defaultDirection });
        }

        public static void ToggleSortDirection(this ICollectionView view, string property, ListSortDirection defaultDirection = ListSortDirection.Ascending)
        {
            for (int i = 0; i < view.SortDescriptions.Count; i++)
            {
                var sortDescription = view.SortDescriptions[i];
                if (sortDescription.PropertyName == property)
                {
                    if (sortDescription.Direction == ListSortDirection.Ascending)
                    {
                        view.SortDescriptions[i] = new SortDescription() { PropertyName = property, Direction = ListSortDirection.Descending };
                    }
                    else
                    {
                        view.SortDescriptions[i] = new SortDescription() { PropertyName = property, Direction = ListSortDirection.Ascending };
                    }

                    return;
                }
            }

            view.SortDescriptions.Add(new SortDescription() { PropertyName = property, Direction = defaultDirection });
        }
    }
}
