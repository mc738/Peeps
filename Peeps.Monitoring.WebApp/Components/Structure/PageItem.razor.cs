using System;
using Microsoft.AspNetCore.Components;

namespace Peeps.Monitoring.WebApp.Components.Structure
{
    public class PageItemBase: ComponentBase, IDisposable
    {
        [CascadingParameter]
        private PageControl Parent { get; set; }

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }
        
        [Parameter]
        public string Text { get; set; }

        protected override void OnInitialized()
        {
            if (Parent == null)
                throw new ArgumentNullException(nameof(Parent), "PageItem must exist within a PageControl");

            base.OnInitialized();

            Parent.AddPage((PageItem)(object)this);
        }

        public bool IsActive()
        {
            return Parent.ActivePage == (PageItem)(object)this;
        }
        
        public void Dispose()
        {
        }
    }
}