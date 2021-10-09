using System;
using Microsoft.AspNetCore.Components;

namespace Peeps.Monitoring.WebApp.Components.Structure
{
    public class GridItemBase: ComponentBase, IDisposable
    {
        [CascadingParameter]
        private GridControl Parent { get; set; }

        [Parameter] public int Column { get; set; } = 1;

        [Parameter] public int Row { get; set; } = 1;

        [Parameter] public int ColumnSpan { get; set; }

        [Parameter] public int RowSpan { get; set; }

        [Parameter] public string ExtraClasses { get; set; } = string.Empty;
        
        public string GridClasses =>
            String.Join(" ", $"g-c-{Column} g-r-{Row} g-c-s-{Column} g-c-e-{Column + ColumnSpan} g-r-s-{Row} g-r-e-{Row + RowSpan}", ExtraClasses);
        
        [Parameter]
        public RenderFragment ChildContent { get; set; }
        
        //public string ColumnStart

        public void Dispose()
        {
        }
    }
}