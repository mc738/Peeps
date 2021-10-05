using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Peeps.Monitoring.WebApp.Components.Structure
{
    public class PageControlBase: ComponentBase, IDisposable
    {
        
        [Parameter]
        public RenderFragment ChildContent { get; set; }
        
        public PageItem ActivePage { get; set; }

        public List<PageItem> Pages = new ();

        string containerStyle = "";

        internal void AddPage(PageItem page)
        {
            Pages.Add(page);
            if (Pages.Count == 1)
                ActivePage = page;
            StateHasChanged();
        }

        public string GenerateContainerStyle() => "tab-content";
        
        public string GetButtonClass(PageItem page)
        {
            return page == ActivePage ? "btn-primary" : "btn-secondary";
        }
        public void ActivatePage(PageItem page)
        {
            ActivePage = page;
        }

        public void CloseTab(PageItem page)
        {
            Pages.Remove(page);
        }
        
        public void Dispose()
        {
        }
    }
}