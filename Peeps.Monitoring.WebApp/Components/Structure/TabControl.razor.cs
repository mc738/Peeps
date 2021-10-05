using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Peeps.Monitoring.WebApp.Components.Structure
{
    public class TabControlBase: ComponentBase, IDisposable
    {
        
        [Parameter]
        public RenderFragment ChildContent { get; set; }
        
        public TabItem ActivePage { get; set; }

        public List<TabItem> Pages = new ();

        string containerStyle = "";

        internal void AddPage(TabItem tabPage)
        {
            Pages.Add(tabPage);
            if (Pages.Count == 1)
                ActivePage = tabPage;
            StateHasChanged();
        }

        public string GenerateContainerStyle() => "tab-content";
        
        public string GetButtonClass(TabItem page)
        {
            return page == ActivePage ? "btn-primary" : "btn-secondary";
        }
        public void ActivatePage(TabItem page)
        {
            ActivePage = page;
        }

        public void CloseTab(TabItem page)
        {
            Pages.Remove(page);
        }
        
        public void Dispose()
        {
        }
    }
}