using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ServerManagerTool.Common.Lib
{
	public static class SortBehavior
	{
		public static readonly DependencyProperty CanUserSortColumnsProperty = DependencyProperty.RegisterAttached("CanUserSortColumns", typeof(bool), typeof(SortBehavior), new FrameworkPropertyMetadata(OnCanUserSortColumnsChanged));
        public static readonly DependencyProperty CanUseSortProperty = DependencyProperty.RegisterAttached("CanUseSort", typeof(bool), typeof(SortBehavior), new FrameworkPropertyMetadata(true));
        public static readonly DependencyProperty SortDirectionProperty = DependencyProperty.RegisterAttached("SortDirection", typeof(ListSortDirection?), typeof(SortBehavior));
        public static readonly DependencyProperty SortExpressionProperty = DependencyProperty.RegisterAttached("SortExpression", typeof(string), typeof(SortBehavior));
        public static readonly DependencyProperty IsDefaultSortProperty = DependencyProperty.RegisterAttached("IsDefaultSort", typeof(bool), typeof(SortBehavior), new FrameworkPropertyMetadata(false));

        [AttachedPropertyBrowsableForType(typeof(ListView))]
		public static bool GetCanUserSortColumns(ListView element)
		{
			return (bool)(element?.GetValue(CanUserSortColumnsProperty) ?? false);
		}

		[AttachedPropertyBrowsableForType(typeof(ListView))]
		public static void SetCanUserSortColumns(ListView element, bool value)
		{
			element?.SetValue(CanUserSortColumnsProperty, value);
		}

		[AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
		public static bool GetCanUseSort(GridViewColumn element)
		{
			return (bool)(element?.GetValue(CanUseSortProperty) ?? false);
		}

		[AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
		public static void SetCanUseSort(GridViewColumn element, bool value)
		{
			element?.SetValue(CanUseSortProperty, value);
		}

		[AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
		public static ListSortDirection? GetSortDirection(GridViewColumn element)
		{
			return (ListSortDirection?)element.GetValue(SortDirectionProperty);
		}

		[AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
		public static void SetSortDirection(GridViewColumn element, ListSortDirection? value)
		{
			element?.SetValue(SortDirectionProperty, value);
		}

		[AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
		public static string GetSortExpression(GridViewColumn element)
		{
			return (string)element?.GetValue(SortExpressionProperty);
		}

		[AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
		public static void SetSortExpression(GridViewColumn element, string value)
		{
			element?.SetValue(SortExpressionProperty, value);
		}

        [AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
        public static bool GetIsDefaultSort(GridViewColumn element)
        {
            return (bool)(element?.GetValue(IsDefaultSortProperty) ?? false);
        }

        [AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
        public static void SetIsDefaultSort(GridViewColumn element, bool value)
        {
            element?.SetValue(IsDefaultSortProperty, value);
        }

        private static void OnCanUserSortColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var listView = (ListView)d;
			if ((bool)e.NewValue)
			{
				listView.AddHandler(GridViewColumnHeader.ClickEvent, (RoutedEventHandler)OnColumnHeaderClick);
				if (listView.IsLoaded)
				{
					DoInitialSort(listView);
				}
				else
				{
					listView.Loaded += OnLoaded;
				}
			}
			else
			{
				listView.RemoveHandler(GridViewColumnHeader.ClickEvent, (RoutedEventHandler)OnColumnHeaderClick);
			}
		}

		private static void OnLoaded(object sender, RoutedEventArgs e)
		{
			var listView = (ListView)e.Source;
			listView.Loaded -= OnLoaded;
			DoInitialSort(listView);
		}

		private static void DoInitialSort(ListView listView)
		{
			var gridView = (GridView)listView.View;
			var column = gridView.Columns.FirstOrDefault(c => GetIsDefaultSort(c));
			if (column != null)
			{
				DoSort(listView, column);
			}
		}

		private static void OnColumnHeaderClick(object sender, RoutedEventArgs e)
		{
			var columnHeader = e.OriginalSource as GridViewColumnHeader;
			if (columnHeader != null && GetCanUseSort(columnHeader.Column))
			{
				DoSort((ListView)e.Source, columnHeader.Column);
			}
		}

		private static void DoSort(ListView listView, GridViewColumn newColumn)
		{
            var gridView = (GridView)listView.View;

            var sortDescriptions = listView.Items.SortDescriptions;
			var newDirection = ListSortDirection.Ascending;

			var propertyPath = ResolveSortExpression(newColumn);
			if (propertyPath != null)
			{
				if (sortDescriptions.Count > 0)
				{
					if (sortDescriptions[0].PropertyName == propertyPath)
					{
						newDirection = GetSortDirection(newColumn) == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
					}
					else
					{
						foreach (var column in gridView.Columns.Where(c => GetSortDirection(c) != null))
						{
							SetSortDirection(column, null);
						}
					}

					sortDescriptions.Clear();
				}

				sortDescriptions.Add(new SortDescription(propertyPath, newDirection));
                SetSortDirection(newColumn, newDirection);

                // check if there is a default sort column
                var defaultColumn = gridView.Columns.FirstOrDefault(c => GetIsDefaultSort(c));
                if (defaultColumn != null)
                {
                    var defaultPropertyPath = ResolveSortExpression(defaultColumn);
                    if (defaultPropertyPath != null && !defaultPropertyPath.Equals(propertyPath))
                    {
                        sortDescriptions.Add(new SortDescription(defaultPropertyPath, ListSortDirection.Ascending));
                    }
                }
            }
		}

		private static string ResolveSortExpression(GridViewColumn column)
		{
			var propertyPath = GetSortExpression(column);
			if (propertyPath == null)
			{
				var binding = column.DisplayMemberBinding as Binding;
				return binding != null ? binding.Path.Path : null;
			}

			return propertyPath;
		}
	}
}