using System;
using Microsoft.AspNetCore.Components;

namespace Peeps.Monitoring.WebApp.Components.Inputs
{
    public class TextInputBase: ComponentBase, IDisposable
    {
        private string _value = String.Empty;

        [Parameter]
        public string Label { get; set; }
        
        [Parameter]
        public string PlaceHolder { get; set; }
        
        //[Parameter]
        //public bool MultiLine { get; set; } = false;
        
        [Parameter]
        public string Value
        {
            get => _value;
            set
            {
                //if (value == String.Empty /*&& Required*/)
                //{
                //    ErrorMessage = "Required";
                //    Valid = false;
                //}

                if (_value != value)
                {
                    _value = value;
                    ValueChanged.InvokeAsync(value);
                }
            }

        }
        
        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }
        
        
        public void Dispose()
        {
        }
        
        async System.Threading.Tasks.Task Update()
        {
            await ValueChanged.InvokeAsync(_value);
        }
    }
}