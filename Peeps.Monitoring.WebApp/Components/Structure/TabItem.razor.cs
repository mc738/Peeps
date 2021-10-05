using System;
using Microsoft.AspNetCore.Components;

namespace Peeps.Monitoring.WebApp.Components.Structure
{
    public class TabItemBase: ComponentBase, IDisposable
    {
        [CascadingParameter]
        private TabControl Parent { get; set; }

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public bool Closable { get; set; } = false;

        [Parameter]
        public string Text { get; set; }

        protected override void OnInitialized()
        {
            if (Parent == null)
                throw new ArgumentNullException(nameof(Parent), "TabPage must exist within a TabControl");

            base.OnInitialized();

            Parent.AddPage((TabItem)(object)this);
        }

        public bool IsActive()
        {
            return Parent.ActivePage == (TabItem)(object)this;
        }
        
        public void Dispose()
        {
        }
    }
}