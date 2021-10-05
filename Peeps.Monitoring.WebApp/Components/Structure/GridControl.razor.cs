using System;
using Microsoft.AspNetCore.Components;

namespace Peeps.Monitoring.WebApp.Components.Structure
{
    public class GridControlBase: ComponentBase, IDisposable
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }
        
        public void Dispose()
        {
        }
    }
}