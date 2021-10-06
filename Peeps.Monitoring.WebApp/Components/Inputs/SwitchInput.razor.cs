using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Peeps.Monitoring.WebApp.Components.Inputs
{
    public class SwitchInputBase: ComponentBase, IDisposable
    {
        bool _value = false;

        [Parameter]
        public string Label { get; set; }

        [Parameter]
        public bool Required { get; set; } = false;

        [Parameter]
        public string ErrorMessage { get; set; } = "Error";

        [Parameter]
        public bool Valid { get; set; }

        [Parameter]
        public bool ShowMessage { get; set; } = true;

        [Parameter]
        public bool Value
        {
            get => _value;
            set
            {
                if (Required)
                {
                    ErrorMessage = "Required";
                    Valid = false;
                }

                if (this._value != value)
                {
                    this._value = value;
                    ValueChanged.InvokeAsync(value);
                }
            }

        }

        [Parameter]
        public EventCallback<bool> ValueChanged { get; set; }
        
        async Task Update()
        {
            await ValueChanged.InvokeAsync(_value);
        }

        public void Dispose()
        {
        }
    }
}