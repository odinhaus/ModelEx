using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;

namespace Altus.Core.Licensing
{
    public abstract class LicensedComponent : InitializableComponent, ILicensedComponent
    {
        public override void Initialize(string name, params string[] args)
        {
            Name = name;
            Arguments = args;
            ILicenseManager mgr = App.Instance.Shell.GetComponent<ILicenseManager>();
            if (mgr == null)
            {
                App.Instance.Shell.ComponentChanged += Shell_ComponentChanged;
            }
            else
            {
                CheckLicensing(mgr.GetLicenses(), args);
            }
        }

        void Shell_ComponentChanged(object sender, CompositionContainerComponentEventArgs e)
        {
            if (e.Change == CompositionContainerComponentChange.Add
                && e.Component is ILicenseManager)
            {
                CheckLicensing(((ILicenseManager)e.Component).GetLicenses(), this.Arguments);
            }
        }

        bool _isLicensingChecked = false;
        protected void CheckLicensing(ILicense[] licenses, params string[] args)
        {
            if (_isLicensingChecked) return;
            _isLicensingChecked = true;

            ApplyLicensing(licenses, args);
            IsEnabled = IsLicensed(this);
            if (IsEnabled)
            {
                IsInitialized = OnInitialize(args);
            }
        }

        public void ApplyLicensing(ILicense[] licenses, params string[] args)
        {
            OnApplyLicensing(licenses, args);
        }

        protected abstract void OnApplyLicensing(ILicense[] licenses, params string[] args);

        public bool IsLicensed(object component)
        {
            return OnIsLicensed(component);
        }

        protected abstract bool OnIsLicensed(object component);
    }
}
